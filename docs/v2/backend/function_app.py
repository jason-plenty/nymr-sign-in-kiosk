# ---------------------------------------------------------------------------
# NYMR Construction Site Sign-In — Azure Functions Backend (Python v2 model)
# ---------------------------------------------------------------------------
# Bridge 42, Grosmont — with Medical Self-Declaration Workflow
#
# Endpoints:
#   GET  /api/register           — today's sign-in/out/denied lists
#   POST /api/signin             — initial sign-in (name, org, signature)
#   POST /api/confirm-fit        — medical YES path (fit for duty)
#   POST /api/declare-not-fit    — medical NO path (generates code, emails Phil)
#   POST /api/submit-site-code   — validate one-time code (after Phil call)
#   POST /api/signout            — sign a person out
#   GET  /api/export             — download today's CSV
#   POST /api/send-daily-email   — manually trigger the daily email
#   Timer (23:00 daily)          — auto-send the daily email
# ---------------------------------------------------------------------------

import azure.functions as func
import json
import logging
import os
import csv
import io
import uuid
import random
import string
from datetime import datetime, timezone
from azure.data.tables import TableServiceClient, TableClient

app = func.FunctionApp()

# ---------------------------------------------------------------------------
# CONFIG — set these in Azure App Settings (or local.settings.json)
# ---------------------------------------------------------------------------
TABLE_NAME = "SiteRegister"
PARTITION_KEY_FORMAT = "%Y-%m-%d"

# Phil Sash — Site Controller
SITE_CONTROLLER_NAME = "Phil Sash"
SITE_CONTROLLER_EMAIL = "bridge42@nymr.co.uk"
SITE_CONTROLLER_PHONE = "07483 990 436"


def _get_table_client() -> TableClient:
    conn = os.environ["AzureWebJobsStorage"]
    service = TableServiceClient.from_connection_string(conn)
    service.create_table_if_not_exists(TABLE_NAME)
    return service.get_table_client(TABLE_NAME)


def _today_pk() -> str:
    return datetime.now(timezone.utc).strftime(PARTITION_KEY_FORMAT)


def _uk_time_now() -> str:
    from dateutil import tz
    return datetime.now(tz.gettz("Europe/London")).strftime("%H:%M")


def _uk_date_now() -> str:
    from dateutil import tz
    return datetime.now(tz.gettz("Europe/London")).strftime("%d/%m/%Y")


def _generate_site_code() -> str:
    """Generate a readable one-time site code like B42-A7X3."""
    chars = string.ascii_uppercase + string.digits
    part1 = "B42"  # Bridge 42 prefix
    part2 = ''.join(random.choices(chars, k=4))
    return f"{part1}-{part2}"


def _json_response(data, status=200):
    return func.HttpResponse(
        json.dumps(data), mimetype="application/json", status_code=status
    )


def _error_response(msg, status=500):
    return func.HttpResponse(
        json.dumps({"error": msg}), mimetype="application/json", status_code=status
    )


# ---------------------------------------------------------------------------
# Classify records into signed-in, signed-out, denied
# ---------------------------------------------------------------------------
def _get_todays_records(table: TableClient):
    pk = _today_pk()
    entities = list(table.query_entities(f"PartitionKey eq '{pk}'"))
    signed_in = []
    signed_out = []
    denied = []

    for e in entities:
        status = e.get("Status", "pending")
        record = {
            "id": e["RowKey"],
            "name": e.get("Name", ""),
            "org": e.get("Org", ""),
            "dateIn": e.get("DateIn", ""),
            "timeIn": e.get("TimeIn", ""),
            "timeOut": e.get("TimeOut", ""),
            "signature": "",  # Don't send signature data back in list calls
            "medicalStatus": e.get("MedicalStatus", ""),
            "additionalInfo": e.get("AdditionalInfo", ""),
            "siteCode": e.get("SiteCode", ""),
            "status": status,
        }

        if status == "denied":
            denied.append(record)
        elif record["timeOut"]:
            signed_out.append(record)
        elif status in ("on-site", "on-site-conditional"):
            signed_in.append(record)
        # "pending" status records are mid-flow and not shown in lists

    signed_in.sort(key=lambda r: r["timeIn"])
    signed_out.sort(key=lambda r: r["timeOut"])
    denied.sort(key=lambda r: r["timeIn"])
    return signed_in, signed_out, denied


# ---------------------------------------------------------------------------
# GET /api/register
# ---------------------------------------------------------------------------
@app.route(route="register", methods=["GET"], auth_level=func.AuthLevel.ANONYMOUS)
def get_register(req: func.HttpRequest) -> func.HttpResponse:
    try:
        table = _get_table_client()
        signed_in, signed_out, denied = _get_todays_records(table)
        return _json_response({
            "signedIn": signed_in,
            "signedOut": signed_out,
            "denied": denied,
        })
    except Exception as e:
        logging.error(f"get_register error: {e}")
        return _error_response(str(e))


# ---------------------------------------------------------------------------
# POST /api/signin — Step 1: initial sign-in (creates record in "pending")
# ---------------------------------------------------------------------------
@app.route(route="signin", methods=["POST"], auth_level=func.AuthLevel.ANONYMOUS)
def post_signin(req: func.HttpRequest) -> func.HttpResponse:
    try:
        body = req.get_json()
        name = body.get("name", "").strip()
        org = body.get("org", "").strip()
        signature = body.get("signature", "")

        if not name or not org:
            return _error_response("Name and organisation are required.", 400)

        table = _get_table_client()
        row_key = str(uuid.uuid4())
        date_in = _uk_date_now()
        time_in = _uk_time_now()

        entity = {
            "PartitionKey": _today_pk(),
            "RowKey": row_key,
            "Name": name,
            "Org": org,
            "DateIn": date_in,
            "TimeIn": time_in,
            "TimeOut": "",
            "Signature": signature[:500000] if signature else "",
            "Status": "pending",          # awaiting medical declaration
            "MedicalStatus": "",          # fit | not-fit | conditional
            "AdditionalInfo": "",
            "SiteCode": "",
            "SiteCodeGenerated": "",      # the code we generated (for validation)
        }
        table.create_entity(entity)

        return _json_response({"id": row_key, "dateIn": date_in, "timeIn": time_in})
    except Exception as e:
        logging.error(f"post_signin error: {e}")
        return _error_response(str(e))


# ---------------------------------------------------------------------------
# POST /api/confirm-fit — Step 2a: YES path (medically fit)
# ---------------------------------------------------------------------------
@app.route(route="confirm-fit", methods=["POST"], auth_level=func.AuthLevel.ANONYMOUS)
def post_confirm_fit(req: func.HttpRequest) -> func.HttpResponse:
    try:
        body = req.get_json()
        record_id = body.get("id", "")
        additional_info = body.get("additionalInfo", "").strip()
        site_code = body.get("siteCode", "").strip()

        if not record_id:
            return _error_response("Record ID required.", 400)

        table = _get_table_client()
        pk = _today_pk()
        entity = table.get_entity(partition_key=pk, row_key=record_id)

        entity["Status"] = "on-site"
        entity["MedicalStatus"] = "fit"
        entity["AdditionalInfo"] = additional_info
        entity["SiteCode"] = site_code
        table.update_entity(entity, mode="merge")

        return _json_response({"ok": True, "status": "on-site", "medicalStatus": "fit"})
    except Exception as e:
        logging.error(f"confirm_fit error: {e}")
        return _error_response(str(e))


# ---------------------------------------------------------------------------
# POST /api/declare-not-fit — Step 2b: NO path
# Generates a one-time site code and emails Phil Sash
# ---------------------------------------------------------------------------
@app.route(route="declare-not-fit", methods=["POST"], auth_level=func.AuthLevel.ANONYMOUS)
def post_declare_not_fit(req: func.HttpRequest) -> func.HttpResponse:
    try:
        body = req.get_json()
        record_id = body.get("id", "")
        additional_info = body.get("additionalInfo", "").strip()

        if not record_id:
            return _error_response("Record ID required.", 400)

        table = _get_table_client()
        pk = _today_pk()
        entity = table.get_entity(partition_key=pk, row_key=record_id)

        # Generate one-time code
        code = _generate_site_code()

        entity["Status"] = "denied"
        entity["MedicalStatus"] = "not-fit"
        entity["AdditionalInfo"] = additional_info
        entity["SiteCodeGenerated"] = code
        table.update_entity(entity, mode="merge")

        # Email Phil with the code and person details
        _send_not_fit_email(
            person_name=entity.get("Name", ""),
            person_org=entity.get("Org", ""),
            time_in=entity.get("TimeIn", ""),
            date_in=entity.get("DateIn", ""),
            additional_info=additional_info,
            site_code=code,
        )

        return _json_response({"ok": True, "status": "denied"})
    except Exception as e:
        logging.error(f"declare_not_fit error: {e}")
        return _error_response(str(e))


# ---------------------------------------------------------------------------
# POST /api/submit-site-code — Step 3: validate code after Phil conversation
# ---------------------------------------------------------------------------
@app.route(route="submit-site-code", methods=["POST"], auth_level=func.AuthLevel.ANONYMOUS)
def post_submit_site_code(req: func.HttpRequest) -> func.HttpResponse:
    try:
        body = req.get_json()
        record_id = body.get("id", "")
        submitted_code = body.get("siteCode", "").strip().upper()
        additional_info = body.get("additionalInfo", "").strip()

        if not record_id or not submitted_code:
            return _error_response("Record ID and site code are required.", 400)

        table = _get_table_client()
        pk = _today_pk()
        entity = table.get_entity(partition_key=pk, row_key=record_id)

        # Validate the code
        expected_code = entity.get("SiteCodeGenerated", "").strip().upper()
        if not expected_code or submitted_code != expected_code:
            return _error_response("Invalid site code. Please check with Phil Sash.", 403)

        # Code is valid — grant conditional entry
        entity["Status"] = "on-site-conditional"
        entity["MedicalStatus"] = "conditional"
        entity["AdditionalInfo"] = additional_info
        entity["SiteCode"] = submitted_code
        table.update_entity(entity, mode="merge")

        return _json_response({
            "ok": True,
            "status": "on-site-conditional",
            "medicalStatus": "conditional",
        })
    except Exception as e:
        logging.error(f"submit_site_code error: {e}")
        return _error_response(str(e))


# ---------------------------------------------------------------------------
# POST /api/signout
# ---------------------------------------------------------------------------
@app.route(route="signout", methods=["POST"], auth_level=func.AuthLevel.ANONYMOUS)
def post_signout(req: func.HttpRequest) -> func.HttpResponse:
    try:
        body = req.get_json()
        record_id = body.get("id", "")
        if not record_id:
            return _error_response("Record ID required.", 400)

        table = _get_table_client()
        pk = _today_pk()
        entity = table.get_entity(partition_key=pk, row_key=record_id)
        entity["TimeOut"] = _uk_time_now()
        table.update_entity(entity, mode="merge")

        return _json_response({"ok": True, "timeOut": entity["TimeOut"]})
    except Exception as e:
        logging.error(f"post_signout error: {e}")
        return _error_response(str(e))


# ---------------------------------------------------------------------------
# GET /api/export — CSV download (enhanced with medical status)
# ---------------------------------------------------------------------------
@app.route(route="export", methods=["GET"], auth_level=func.AuthLevel.ANONYMOUS)
def export_csv(req: func.HttpRequest) -> func.HttpResponse:
    try:
        table = _get_table_client()
        pk = _today_pk()
        entities = list(table.query_entities(f"PartitionKey eq '{pk}'"))

        output = io.StringIO()
        writer = csv.writer(output)
        writer.writerow([
            "Name", "Organisation", "Date", "Time In", "Time Out",
            "Medical Status", "Site Code", "Additional Information", "Status",
        ])

        for e in entities:
            status = e.get("Status", "")
            time_out = e.get("TimeOut", "")
            if status in ("on-site", "on-site-conditional") and not time_out:
                time_out = "STILL ON SITE"
            elif status == "denied" and not time_out:
                time_out = "ENTRY DENIED"

            medical = e.get("MedicalStatus", "")
            if medical == "fit":
                medical_display = "Medically Fit - No Conditions"
            elif medical == "conditional":
                medical_display = "Conditional Entry - Authorised by Site Controller"
            elif medical == "not-fit":
                medical_display = "Not Fit - Entry Denied"
            else:
                medical_display = "Pending"

            writer.writerow([
                e.get("Name", ""),
                e.get("Org", ""),
                e.get("DateIn", ""),
                e.get("TimeIn", ""),
                time_out,
                medical_display,
                e.get("SiteCode", ""),
                e.get("AdditionalInfo", ""),
                status,
            ])

        csv_bytes = output.getvalue().encode("utf-8-sig")
        filename = f"site-register-bridge42-{_today_pk()}.csv"

        return func.HttpResponse(
            csv_bytes, mimetype="text/csv",
            headers={"Content-Disposition": f'attachment; filename="{filename}"'},
        )
    except Exception as e:
        logging.error(f"export_csv error: {e}")
        return _error_response(str(e))


# ---------------------------------------------------------------------------
# Email helpers
# ---------------------------------------------------------------------------
def _get_sendgrid_client():
    from sendgrid import SendGridAPIClient
    api_key = os.environ.get("SENDGRID_API_KEY")
    if not api_key:
        raise ValueError("SENDGRID_API_KEY not configured.")
    return SendGridAPIClient(api_key)


def _get_from_email():
    return os.environ.get("DAILY_EMAIL_FROM", "noreply@nymr.co.uk")


def _send_not_fit_email(person_name, person_org, time_in, date_in, additional_info, site_code):
    """Email Phil Sash when someone declares not fit — includes the one-time code."""
    from sendgrid.helpers.mail import Mail

    site_name = os.environ.get("SITE_NAME", "Bridge 42 Construction Site")
    body_lines = [
        f"MEDICAL SELF-DECLARATION ALERT — {site_name}",
        f"",
        f"The following person could NOT confirm the medical self-declaration:",
        f"",
        f"  Name:         {person_name}",
        f"  Organisation: {person_org}",
        f"  Date:         {date_in}",
        f"  Time:         {time_in}",
        f"",
        f"Additional information provided:",
        f"  {additional_info if additional_info else '(none provided)'}",
        f"",
        f"----------------------------------------------------",
        f"ONE TIME USE SITE CODE:  {site_code}",
        f"----------------------------------------------------",
        f"",
        f"If you authorise this person to enter site after discussion,",
        f"give them the code above. They will enter it on the kiosk",
        f"to gain access. This creates an audit trail showing:",
        f"",
        f"  a) A conversation with the Site Controller occurred",
        f"  b) You elected to permit their entry",
        f"  c) Their condition is recorded against their entry",
        f"",
        f"If you do NOT authorise entry, no action is needed — they",
        f"are recorded as denied.",
        f"",
        f"— NYMR Site Sign-In System",
    ]

    message = Mail(
        from_email=_get_from_email(),
        to_emails=SITE_CONTROLLER_EMAIL,
        subject=f"⚠ Medical Alert — {person_name} — {site_name} — {date_in}",
        plain_text_content="\n".join(body_lines),
    )

    sg = _get_sendgrid_client()
    response = sg.send(message)
    logging.info(f"Not-fit email sent to {SITE_CONTROLLER_EMAIL}: {response.status_code}")


def _send_daily_email():
    """Build CSV and send daily summary to Phil Sash."""
    from sendgrid.helpers.mail import (
        Mail, Attachment, FileContent, FileName, FileType, Disposition,
    )
    import base64

    to_addrs = os.environ.get("DAILY_EMAIL_TO", SITE_CONTROLLER_EMAIL).split(",")
    site_name = os.environ.get("SITE_NAME", "Bridge 42 Construction Site")

    table = _get_table_client()
    pk = _today_pk()
    entities = list(table.query_entities(f"PartitionKey eq '{pk}'"))

    on_site = [e for e in entities if e.get("Status", "") in ("on-site", "on-site-conditional") and not e.get("TimeOut")]
    signed_out = [e for e in entities if e.get("TimeOut")]
    denied = [e for e in entities if e.get("Status") == "denied"]
    conditional = [e for e in entities if e.get("Status") == "on-site-conditional"]

    # Build CSV
    csv_output = io.StringIO()
    writer = csv.writer(csv_output)
    writer.writerow(["Name", "Organisation", "Date", "Time In", "Time Out", "Medical Status", "Site Code", "Additional Information", "Status"])
    for e in entities:
        status = e.get("Status", "")
        time_out = e.get("TimeOut", "")
        if status in ("on-site", "on-site-conditional") and not time_out:
            time_out = "STILL ON SITE"
        elif status == "denied":
            time_out = "ENTRY DENIED"

        medical = e.get("MedicalStatus", "")
        if medical == "fit":
            md = "Medically Fit - No Conditions"
        elif medical == "conditional":
            md = "Conditional Entry - Authorised by Site Controller"
        elif medical == "not-fit":
            md = "Not Fit - Entry Denied"
        else:
            md = "Pending"

        writer.writerow([
            e.get("Name", ""), e.get("Org", ""), e.get("DateIn", ""),
            e.get("TimeIn", ""), time_out, md, e.get("SiteCode", ""),
            e.get("AdditionalInfo", ""), status,
        ])
    csv_data = csv_output.getvalue()

    # Build email body
    date_str = _uk_date_now()
    body = [
        f"DAILY SITE REGISTER — {site_name}",
        f"Date: {date_str}",
        f"",
        f"SUMMARY",
        f"  Total sign-ins:         {len(entities)}",
        f"  Currently on site:      {len(on_site)}",
        f"  Signed out:             {len(signed_out)}",
        f"  Entry denied:           {len(denied)}",
        f"  Conditional entries:    {len(conditional)}",
    ]

    if on_site:
        body += ["", "⚠ STILL ON SITE (not signed out):"]
        for e in on_site:
            body.append(f"  - {e.get('Name')} ({e.get('Org')}) — in at {e.get('TimeIn')} — {e.get('MedicalStatus', 'unknown')}")

    if conditional:
        body += ["", "⚠ CONDITIONAL ENTRIES (authorised by Site Controller):"]
        for e in conditional:
            body.append(f"  - {e.get('Name')} ({e.get('Org')}) — Code: {e.get('SiteCode')} — Info: {e.get('AdditionalInfo', 'none')}")

    if denied:
        body += ["", "⛔ DENIED ENTRY:"]
        for e in denied:
            body.append(f"  - {e.get('Name')} ({e.get('Org')}) — {e.get('TimeIn')} — Info: {e.get('AdditionalInfo', 'none')}")

    body += ["", "Full CSV register attached.", "", "— NYMR Site Sign-In System"]

    message = Mail(
        from_email=_get_from_email(),
        to_emails=[a.strip() for a in to_addrs],
        subject=f"Site Register — {site_name} — {date_str}",
        plain_text_content="\n".join(body),
    )

    encoded = base64.b64encode(csv_data.encode("utf-8-sig")).decode()
    attachment = Attachment(
        FileContent(encoded),
        FileName(f"site-register-bridge42-{_today_pk()}.csv"),
        FileType("text/csv"),
        Disposition("attachment"),
    )
    message.attachment = attachment

    sg = _get_sendgrid_client()
    response = sg.send(message)
    logging.info(f"Daily email sent: {response.status_code}")
    return response.status_code


# ---------------------------------------------------------------------------
# POST /api/send-daily-email — manual trigger
# ---------------------------------------------------------------------------
@app.route(route="send-daily-email", methods=["POST"], auth_level=func.AuthLevel.ANONYMOUS)
def manual_send_email(req: func.HttpRequest) -> func.HttpResponse:
    try:
        status = _send_daily_email()
        return _json_response({"ok": True, "sendgridStatus": status})
    except Exception as e:
        logging.error(f"send_daily_email error: {e}")
        return _error_response(str(e))


# ---------------------------------------------------------------------------
# Timer trigger — 23:00 daily
# ---------------------------------------------------------------------------
@app.timer_trigger(
    schedule="0 0 23 * * *",
    arg_name="timer",
    run_on_startup=False,
)
def daily_email_timer(timer: func.TimerRequest) -> None:
    logging.info("Daily email timer fired.")
    try:
        _send_daily_email()
        logging.info("Daily email sent successfully.")
    except Exception as e:
        logging.error(f"daily_email_timer error: {e}")

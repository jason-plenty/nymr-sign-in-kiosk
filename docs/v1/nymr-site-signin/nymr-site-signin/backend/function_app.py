# ---------------------------------------------------------------------------
# NYMR Construction Site Sign-In — Azure Functions Backend (Python v2 model)
# ---------------------------------------------------------------------------
# Endpoints:
#   GET  /api/register         — today's sign-in/out lists
#   POST /api/signin           — sign a person in
#   POST /api/signout          — sign a person out
#   GET  /api/export           — download today's CSV
#   POST /api/send-daily-email — manually trigger the daily email
#   Timer (23:00 daily)        — auto-send the daily email
# ---------------------------------------------------------------------------

import azure.functions as func
import json
import logging
import os
import csv
import io
import uuid
from datetime import datetime, timezone
from azure.data.tables import TableServiceClient, TableClient

app = func.FunctionApp()

# ---------------------------------------------------------------------------
# CONFIG — set these in Azure App Settings (or local.settings.json)
# ---------------------------------------------------------------------------
# AzureWebJobsStorage          — connection string for Table Storage
# SENDGRID_API_KEY              — SendGrid API key
# DAILY_EMAIL_TO                — comma-separated recipient addresses
# DAILY_EMAIL_FROM              — verified sender address in SendGrid
# SITE_NAME                     — e.g. "Pickering Construction Site"

TABLE_NAME = "SiteRegister"
PARTITION_KEY_FORMAT = "%Y-%m-%d"   # one partition per day


def _get_table_client() -> TableClient:
    """Return a TableClient for the SiteRegister table."""
    conn = os.environ["AzureWebJobsStorage"]
    service = TableServiceClient.from_connection_string(conn)
    service.create_table_if_not_exists(TABLE_NAME)
    return service.get_table_client(TABLE_NAME)


def _today_pk() -> str:
    return datetime.now(timezone.utc).strftime(PARTITION_KEY_FORMAT)


def _uk_time_now() -> str:
    """Return current UK time as HH:MM string."""
    from dateutil import tz
    return datetime.now(tz.gettz("Europe/London")).strftime("%H:%M")


def _uk_date_now() -> str:
    from dateutil import tz
    return datetime.now(tz.gettz("Europe/London")).strftime("%d/%m/%Y")


def _get_todays_records(table: TableClient):
    """Fetch all records for today, split into signed-in and signed-out."""
    pk = _today_pk()
    entities = list(table.query_entities(f"PartitionKey eq '{pk}'"))
    signed_in = []
    signed_out = []
    for e in entities:
        record = {
            "id": e["RowKey"],
            "name": e.get("Name", ""),
            "org": e.get("Org", ""),
            "dateIn": e.get("DateIn", ""),
            "timeIn": e.get("TimeIn", ""),
            "timeOut": e.get("TimeOut", ""),
            "signature": e.get("Signature", ""),
        }
        if record["timeOut"]:
            signed_out.append(record)
        else:
            signed_in.append(record)
    # Sort: signed-in by time in (earliest first), signed-out by time out
    signed_in.sort(key=lambda r: r["timeIn"])
    signed_out.sort(key=lambda r: r["timeOut"])
    return signed_in, signed_out


# ---------------------------------------------------------------------------
# GET /api/register
# ---------------------------------------------------------------------------
@app.route(route="register", methods=["GET"], auth_level=func.AuthLevel.ANONYMOUS)
def get_register(req: func.HttpRequest) -> func.HttpResponse:
    try:
        table = _get_table_client()
        signed_in, signed_out = _get_todays_records(table)
        return func.HttpResponse(
            json.dumps({"signedIn": signed_in, "signedOut": signed_out}),
            mimetype="application/json",
        )
    except Exception as e:
        logging.error(f"get_register error: {e}")
        return func.HttpResponse(json.dumps({"error": str(e)}), status_code=500)


# ---------------------------------------------------------------------------
# POST /api/signin
# ---------------------------------------------------------------------------
@app.route(route="signin", methods=["POST"], auth_level=func.AuthLevel.ANONYMOUS)
def post_signin(req: func.HttpRequest) -> func.HttpResponse:
    try:
        body = req.get_json()
        name = body.get("name", "").strip()
        org = body.get("org", "").strip()
        signature = body.get("signature", "")

        if not name or not org:
            return func.HttpResponse(
                json.dumps({"error": "Name and organisation are required."}),
                status_code=400,
            )

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
            "Signature": signature[:500000] if signature else "",  # cap at ~500KB
        }
        table.create_entity(entity)

        return func.HttpResponse(
            json.dumps({"id": row_key, "dateIn": date_in, "timeIn": time_in}),
            mimetype="application/json",
        )
    except Exception as e:
        logging.error(f"post_signin error: {e}")
        return func.HttpResponse(json.dumps({"error": str(e)}), status_code=500)


# ---------------------------------------------------------------------------
# POST /api/signout
# ---------------------------------------------------------------------------
@app.route(route="signout", methods=["POST"], auth_level=func.AuthLevel.ANONYMOUS)
def post_signout(req: func.HttpRequest) -> func.HttpResponse:
    try:
        body = req.get_json()
        record_id = body.get("id", "")
        if not record_id:
            return func.HttpResponse(
                json.dumps({"error": "Record ID required."}), status_code=400
            )

        table = _get_table_client()
        pk = _today_pk()

        entity = table.get_entity(partition_key=pk, row_key=record_id)
        entity["TimeOut"] = _uk_time_now()
        table.update_entity(entity, mode="merge")

        return func.HttpResponse(
            json.dumps({"ok": True, "timeOut": entity["TimeOut"]}),
            mimetype="application/json",
        )
    except Exception as e:
        logging.error(f"post_signout error: {e}")
        return func.HttpResponse(json.dumps({"error": str(e)}), status_code=500)


# ---------------------------------------------------------------------------
# GET /api/export — CSV download
# ---------------------------------------------------------------------------
@app.route(route="export", methods=["GET"], auth_level=func.AuthLevel.ANONYMOUS)
def export_csv(req: func.HttpRequest) -> func.HttpResponse:
    try:
        table = _get_table_client()
        signed_in, signed_out = _get_todays_records(table)
        all_records = signed_out + [
            {**r, "timeOut": "STILL ON SITE"} for r in signed_in
        ]

        output = io.StringIO()
        writer = csv.writer(output)
        writer.writerow(["Name", "Organisation", "Date", "Time In", "Time Out"])
        for r in all_records:
            writer.writerow([r["name"], r["org"], r["dateIn"], r["timeIn"], r["timeOut"]])

        csv_bytes = output.getvalue().encode("utf-8-sig")
        filename = f"site-register-{_today_pk()}.csv"

        return func.HttpResponse(
            csv_bytes,
            mimetype="text/csv",
            headers={"Content-Disposition": f'attachment; filename="{filename}"'},
        )
    except Exception as e:
        logging.error(f"export_csv error: {e}")
        return func.HttpResponse(json.dumps({"error": str(e)}), status_code=500)


# ---------------------------------------------------------------------------
# Daily email helper (used by both manual trigger and timer)
# ---------------------------------------------------------------------------
def _send_daily_email():
    """Build CSV and send via SendGrid."""
    from sendgrid import SendGridAPIClient
    from sendgrid.helpers.mail import (
        Mail, Attachment, FileContent, FileName, FileType, Disposition,
    )
    import base64

    api_key = os.environ.get("SENDGRID_API_KEY")
    to_addrs = os.environ.get("DAILY_EMAIL_TO", "").split(",")
    from_addr = os.environ.get("DAILY_EMAIL_FROM", "noreply@nymr.co.uk")
    site_name = os.environ.get("SITE_NAME", "Construction Site")

    if not api_key or not to_addrs[0]:
        raise ValueError("SENDGRID_API_KEY and DAILY_EMAIL_TO must be configured.")

    table = _get_table_client()
    signed_in, signed_out = _get_todays_records(table)
    all_records = signed_out + [{**r, "timeOut": "STILL ON SITE"} for r in signed_in]

    # Build CSV
    output = io.StringIO()
    writer = csv.writer(output)
    writer.writerow(["Name", "Organisation", "Date", "Time In", "Time Out"])
    for r in all_records:
        writer.writerow([r["name"], r["org"], r["dateIn"], r["timeIn"], r["timeOut"]])
    csv_data = output.getvalue()

    # Build email
    date_str = _uk_date_now()
    still_on = len(signed_in)
    body_lines = [
        f"Daily site register for {site_name} — {date_str}",
        f"",
        f"Total sign-ins today: {len(all_records)}",
        f"Signed out: {len(signed_out)}",
        f"Still on site: {still_on}",
    ]
    if still_on > 0:
        body_lines.append("")
        body_lines.append("⚠ THE FOLLOWING PEOPLE HAVE NOT SIGNED OUT:")
        for r in signed_in:
            body_lines.append(f"  - {r['name']} ({r['org']}) — signed in at {r['timeIn']}")

    message = Mail(
        from_email=from_addr,
        to_emails=[a.strip() for a in to_addrs],
        subject=f"Site Register — {site_name} — {date_str}",
        plain_text_content="\n".join(body_lines),
    )

    # Attach CSV
    encoded = base64.b64encode(csv_data.encode("utf-8-sig")).decode()
    attachment = Attachment(
        FileContent(encoded),
        FileName(f"site-register-{_today_pk()}.csv"),
        FileType("text/csv"),
        Disposition("attachment"),
    )
    message.attachment = attachment

    sg = SendGridAPIClient(api_key)
    response = sg.send(message)
    logging.info(f"SendGrid response: {response.status_code}")
    return response.status_code


# ---------------------------------------------------------------------------
# POST /api/send-daily-email — manual trigger
# ---------------------------------------------------------------------------
@app.route(route="send-daily-email", methods=["POST"], auth_level=func.AuthLevel.ANONYMOUS)
def manual_send_email(req: func.HttpRequest) -> func.HttpResponse:
    try:
        status = _send_daily_email()
        return func.HttpResponse(
            json.dumps({"ok": True, "sendgridStatus": status}),
            mimetype="application/json",
        )
    except Exception as e:
        logging.error(f"send_daily_email error: {e}")
        return func.HttpResponse(json.dumps({"error": str(e)}), status_code=500)


# ---------------------------------------------------------------------------
# Timer trigger — runs at 23:00 UK time daily
# ---------------------------------------------------------------------------
@app.timer_trigger(
    schedule="0 0 23 * * *",       # 23:00 UTC (adjust if Function App TZ set)
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

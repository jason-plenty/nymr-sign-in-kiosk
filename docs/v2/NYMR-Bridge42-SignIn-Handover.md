# NYMR Bridge 42 — Site Sign-In Kiosk

## Developer Handover — Jason Plenty

**Author:** Chris Flynn, IT Manager & Data Protection Lead  
**Date:** 13 April 2026  
**Version:** 2.0  
**Status:** Ready for Development / Deployment

---

## 1. Overview

A web-based sign-in/sign-out kiosk for managing visitor and contractor access to the Bridge 42 construction site at Grosmont. Runs full-screen on an iPad in Safari, positioned at the site entrance.

### What It Does

1. Displays the Bridge 42 site safety briefing (project overview, PPE, hazards, track safety, emergency info, site rules, and declaration)
2. Captures name, organisation, and finger-scrawled signature
3. Presents a medical self-declaration screen with a clear YES/NO choice
4. **If YES (fit):** records the person as ON SITE with "Medically Fit — No Conditions", collects optional additional info and an optional one-time site code, shows a confirmation screen
5. **If NO (not fit):** marks them as DENIED, auto-generates a one-time site code, emails Phil Sash (Site Controller) with the code and the person's details, presents Phil's phone number and email as tappable links, and provides a code-entry field so that if Phil authorises them after a phone call, they can enter the code and gain conditional entry
6. One-tap sign-out screen listing everyone on site
7. Log of everyone who signed out plus anyone denied entry that day
8. Daily CSV email at 23:00 to bridge42@nymr.co.uk, plus manual download and email buttons
9. CSV includes medical status, site codes, additional info, and entry status for full audit trail

### The Audit Trail (One-Time Site Code System)

The one-time site code mechanism creates a verifiable chain of evidence:

- **a)** A conversation between the person and the Site Controller has occurred (the code was generated when they declared not-fit, and Phil received it by email)
- **b)** The Site Controller elected to permit their entry (by giving them the code verbally)
- **c)** Their medical condition is recorded against their entry in the CSV data (status = "conditional", additional info captured)

### Key Design Principles

- **Fat-finger friendly** — all touch targets 48px minimum, most 60px+
- **Minimal cognitive load** — clear linear flow, obvious buttons, no logins
- **High contrast** — safety yellow/black for sign-in, blue for medical, red for sign-out
- **Multi-device sync** — multiple iPads poll backend every 30 seconds
- **iPad optimised** — full-screen web app meta tags, no pull-to-refresh, touch signature pad

---

## 2. User Workflow

```
┌─────────────────────┐
│  1. SIGN IN SCREEN  │
│  Read safety brief  │
│  Enter name, org,   │
│  signature          │
│  Tap "SIGN IN"      │
└─────────┬───────────┘
          ▼
┌─────────────────────────┐
│  2. MEDICAL DECLARATION │
│  Read all statements    │
│                         │
│  ┌─────────┐ ┌────────┐│
│  │  YES ✅  │ │ NO ❌  ││
│  └────┬────┘ └───┬────┘│
└───────┼──────────┼──────┘
        ▼          ▼
┌──────────────┐  ┌────────────────────────┐
│ 3a. FIT FORM │  │ 3b. NOT PERMITTED      │
│ Add info     │  │ Contact Phil Sash      │
│ Optional     │  │ 📞 07483 990 436       │
│ site code    │  │ ✉ phil.sash@nymr.co.uk │
│ (System emails go to   │
│  bridge42@nymr.co.uk)  │
│ Tap PROCEED  │  │ (Email auto-sent with  │
└──────┬───────┘  │  one-time site code)   │
       ▼          │                        │
┌──────────────┐  │ Enter site code from   │
│ 4. CONFIRMED │  │ Phil if authorised     │
│ ON SITE      │  │ Tap SUBMIT CODE        │
│ "Fit - No    │  └───────────┬────────────┘
│  Conditions" │              ▼
└──────────────┘  ┌────────────────────────┐
                  │ 4. CONFIRMED ON SITE   │
                  │ "Conditional Entry —   │
                  │  Authorised by Site    │
                  │  Controller"           │
                  └────────────────────────┘
```

---

## 3. Architecture

```
┌──────────────────────────────┐
│     iPad (Safari, kiosk)     │
│   React SPA (Vite build)     │
└──────────┬───────────────────┘
           │ HTTPS
           ▼
┌──────────────────────────────┐
│  Azure Static Web App        │
│  (hosts the built frontend)  │
│                              │
│  Managed Functions backend ──┼──► Azure Table Storage
│  (Python 3.11)               │       (SiteRegister table)
│                              │
│  Not-fit alert email ────────┼──► SendGrid → bridge42@nymr.co.uk
│  Daily summary email ────────┼──► SendGrid → bridge42@nymr.co.uk
│  Timer trigger (23:00) ──────┤
└──────────────────────────────┘
```

### Component Summary

| Component | Technology | Location in Repo |
|-----------|-----------|-----------------|
| Frontend | React 18 + Vite | `frontend/` |
| Backend API | Azure Functions v2 (Python 3.11) | `backend/` |
| Data Store | Azure Table Storage | Auto-created by backend |
| Email | SendGrid | Called from backend |
| Deployment | GitHub Actions → Azure Static Web Apps | `.github/workflows/` |

---

## 4. Project Structure

```
nymr-site-signin/
├── .github/workflows/deploy.yml
├── frontend/
│   ├── public/favicon.svg
│   ├── src/
│   │   ├── main.jsx               ← React entry point
│   │   ├── App.jsx                 ← Main kiosk UI (6 screens)
│   │   ├── SignaturePad.jsx        ← Touch signature canvas
│   │   └── api.js                  ← API client
│   ├── index.html                  ← HTML shell (iPad meta tags)
│   ├── vite.config.js
│   ├── staticwebapp.config.json
│   └── package.json
├── backend/
│   ├── function_app.py             ← All Azure Function endpoints
│   ├── requirements.txt
│   ├── host.json
│   └── local.settings.json         ← DO NOT COMMIT
├── .gitignore
└── HANDOVER.md
```

---

## 5. API Endpoints

### GET /api/register

Returns today's three lists: signed-in, signed-out, denied.

**Response:**
```json
{
  "signedIn": [{
    "id": "uuid", "name": "...", "org": "...",
    "dateIn": "13/04/2026", "timeIn": "09:15", "timeOut": "",
    "medicalStatus": "fit", "status": "on-site"
  }],
  "signedOut": [{...}],
  "denied": [{...}]
}
```

### POST /api/signin

Step 1 — creates record with status "pending".

**Body:** `{ "name": "...", "org": "...", "signature": "data:image/png;base64,..." }`

**Response:** `{ "id": "uuid", "dateIn": "...", "timeIn": "..." }`

### POST /api/confirm-fit

Step 2a — YES path. Sets status to "on-site", medicalStatus to "fit".

**Body:** `{ "id": "uuid", "additionalInfo": "...", "siteCode": "..." }`

### POST /api/declare-not-fit

Step 2b — NO path. Sets status to "denied", generates one-time code, emails bridge42@nymr.co.uk.

**Body:** `{ "id": "uuid", "additionalInfo": "..." }`

### POST /api/submit-site-code

Step 3 — validates code Phil gave them. If valid, sets status to "on-site-conditional".

**Body:** `{ "id": "uuid", "siteCode": "B42-A7X3", "additionalInfo": "..." }`

**Response (success):** `{ "ok": true, "status": "on-site-conditional" }`  
**Response (invalid):** `403 { "error": "Invalid site code..." }`

### POST /api/signout

**Body:** `{ "id": "uuid" }`

### GET /api/export

Downloads today's CSV. Columns: Name, Organisation, Date, Time In, Time Out, Medical Status, Site Code, Additional Information, Status.

### POST /api/send-daily-email

Manually triggers the daily summary email to bridge42@nymr.co.uk.

---

## 6. Data Storage — Azure Table Storage

### Schema (SiteRegister table)

| Field | Type | Description |
|-------|------|-------------|
| PartitionKey | string | Date `YYYY-MM-DD` |
| RowKey | string | UUID per sign-in |
| Name | string | Visitor name |
| Org | string | Organisation |
| DateIn | string | UK date `DD/MM/YYYY` |
| TimeIn | string | UK time `HH:MM` |
| TimeOut | string | UK time or empty |
| Signature | string | Base64 PNG (max ~500KB) |
| Status | string | `pending` / `on-site` / `on-site-conditional` / `denied` |
| MedicalStatus | string | `fit` / `not-fit` / `conditional` |
| AdditionalInfo | string | Free text from the user |
| SiteCode | string | Code entered by the user |
| SiteCodeGenerated | string | Code generated by the system (for validation) |

### Status Values Explained

| Status | Meaning |
|--------|---------|
| `pending` | Signed in but hasn't completed medical declaration yet |
| `on-site` | Medically fit, authorised, on site |
| `on-site-conditional` | Initially not fit, but authorised by Phil via one-time code |
| `denied` | Not fit, has not been authorised by Phil |

---

## 7. Email Configuration

### Two types of email are sent:

**1. Not-Fit Alert (immediate)** — sent to Phil the moment someone taps "NO" on the medical declaration. Contains their name, org, time, additional info, and the one-time site code. Phil can then call them back and read out the code if he authorises entry.

**2. Daily Summary (23:00)** — full register with counts, warnings about people still on site, conditional entries, and denied entries. CSV attached.

### Required App Settings

| Setting | Value |
|---------|-------|
| `SENDGRID_API_KEY` | `SG.your-key` |
| `DAILY_EMAIL_TO` | `bridge42@nymr.co.uk` |
| `DAILY_EMAIL_FROM` | `noreply@nymr.co.uk` (verified in SendGrid) |
| `SITE_NAME` | `Bridge 42 Construction Site` |
| `WEBSITE_TIME_ZONE` | `GMT Standard Time` |

---

## 8. CSV Output Format

The exported CSV includes full audit data:

```
Name, Organisation, Date, Time In, Time Out, Medical Status, Site Code, Additional Information, Status
John Smith, CML, 13/04/2026, 08:30, 16:45, Medically Fit - No Conditions, , , on-site
Jane Doe, ABC Ltd, 13/04/2026, 09:00, ENTRY DENIED, Not Fit - Entry Denied, , Taking medication, denied
Bob Jones, NYMR, 13/04/2026, 09:15, 12:30, Conditional Entry - Authorised by Site Controller, B42-A7X3, Blood pressure medication, on-site-conditional
```

---

## 9. Local Development Setup

### Prerequisites

- Node.js 18+ and npm
- Python 3.11
- Azure Functions Core Tools v4
- Azurite (local storage emulator)

### Step 1 — Storage Emulator

```bash
azurite --silent --location ./azurite-data
```

### Step 2 — Backend

```bash
cd backend
python -m venv .venv
source .venv/bin/activate    # Windows: .venv\Scripts\activate
pip install -r requirements.txt
func start
```

### Step 3 — Frontend

```bash
cd frontend
npm install
npm run dev
```

### Step 4 — Test

Open `http://localhost:5173`. Note: the not-fit email won't send in local dev unless you have a real SendGrid key in `local.settings.json`. The rest of the flow works fully locally.

---

## 10. Deployment to Azure

### Azure Static Web Apps (Recommended)

1. Azure Portal → **Static Web Apps** → **Create** → Free plan
2. Connect GitHub repo
3. Set: App location = `frontend`, API location = `backend`, Output = `dist`
4. Add app settings (Section 7 above)
5. Optional: custom domain `signin.nymr.co.uk`

### Timer Trigger Workaround

Azure SWA managed Functions don't support timer triggers. Options:

1. **Logic App** — Recurrence trigger at 23:00 → HTTP POST to `/api/send-daily-email` (cheapest)
2. **Standalone Function App** — deploy backend separately (see v1.0 handover)
3. **Manual** — tap "EMAIL REPORT TO BRIDGE 42" button on the sign-out screen at end of day

---

## 11. iPad Kiosk Setup

1. Open Safari → navigate to app URL
2. **Share → Add to Home Screen** (creates full-screen web app)
3. Open from home screen icon
4. Enable **Guided Access** (Settings → Accessibility → Guided Access) to lock iPad to this app
5. **Auto-Lock: Never**, **Do Not Disturb: On**, **Rotation Lock: On** (portrait)
6. Connect to reliable Wi-Fi

---

## 12. Editing Content

### Safety Briefing

The briefing text is in `SAFETY_SECTIONS` and `DECLARATION_TEXT` constants at the top of `frontend/src/App.jsx`. Edit, commit, push — auto-deploys.

### Medical Declaration

The medical statements are in `MEDICAL_SECTIONS` at the top of `frontend/src/App.jsx`.

### Site Controller Contact

Phil's details are in both `frontend/src/App.jsx` (UI) and `backend/function_app.py` (email). Update both if the contact changes.

---

## 13. Future Enhancements

- **Admin PIN** — protect sign-out screen and export behind a PIN
- **Photo capture** — iPad camera on sign-in for site security
- **Editable briefing** — store briefing in Table Storage, admin endpoint to update without deploy
- **Multi-site** — partition by site + date for future NYMR construction projects
- **Offline mode** — service worker to queue sign-ins during connectivity drops
- **QR badge scan** — regular contractors get a QR badge for fast sign-in
- **GDPR retention** — scheduled purge of records older than configured period
- **Blob storage for signatures** — move signature PNGs to Azure Blob Storage

---

## 14. Troubleshooting

| Problem | Likely Cause | Fix |
|---------|-------------|-----|
| "Sign-in failed" | Backend unreachable | Check Function App health; check iPad Wi-Fi |
| Phil didn't receive alert email | SendGrid misconfigured | Verify `SENDGRID_API_KEY` and sender verification; check bridge42@nymr.co.uk inbox |
| Invalid site code | Typo or wrong code | Check the email Phil received; codes are case-insensitive |
| Names not appearing on sign-out | Polling lag | Wait 30 seconds; check `/api/register` directly |
| "Pending" records in CSV | Person abandoned mid-flow | Expected — they started sign-in but didn't complete medical step |
| CSV garbled characters | Encoding | Export uses `utf-8-sig` — works in Notepad and Excel |

---

## 15. Security & GDPR

- No authentication by design — unattended kiosk on construction site
- Add Azure SWA IP restrictions if internet-facing
- Signature data encrypted at rest by Azure Storage default encryption
- One-time codes are short-lived (valid only for the day they're generated — partition key)
- Data collected: name, organisation, signature, medical self-declaration status, additional info
- Lawful basis: legitimate interest in construction site safety management (CDM Regulations 2015)
- Recommended retention: duration of project plus 6 years (limitation period for personal injury)

---

## 16. Contacts

| Role | Name | Contact |
|------|------|---------|
| Project Owner / IT | Chris Flynn | chris.flynn@nymr.co.uk |
| Developer | Jason Plenty | (internal) |
| Site Controller | Phil Sash | phil.sash@nymr.co.uk / 07483 990 436 |
| System Emails | Bridge 42 Mailbox | bridge42@nymr.co.uk |

---

*End of handover document.*

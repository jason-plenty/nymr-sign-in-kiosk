# NYMR Construction Site Sign-In Kiosk

## Developer Handover — Jason Plenty

**Author:** Chris Flynn, IT Manager & Data Protection Lead  
**Date:** 25 March 2026  
**Version:** 1.0  
**Status:** Ready for Development / Deployment

---

## 1. Overview

This is a web-based sign-in/sign-out kiosk for managing visitor and contractor access to NYMR construction sites. It will run full-screen on an iPad in Safari, positioned at the site entrance.

### What It Does

- Displays a static safety briefing that visitors must read before entering
- Captures name, organisation, and a finger-scrawled signature on sign-in
- Shows a confirmation with the person's name, date, and time of entry
- Provides a one-tap sign-out screen listing everyone currently on site
- Shows a log of everyone who signed out that day
- Emails a CSV summary of the day's register at 23:00 automatically
- Allows manual CSV download and manual email trigger for site managers

### Key Design Principles

- **Fat-finger friendly** — all touch targets are minimum 48px, most are 60px+
- **Minimal cognitive load** — two screens only, obvious navigation, no logins
- **High contrast** — construction-safety yellow/black theme, red for sign-out
- **Multi-device sync** — multiple iPads poll the backend every 30 seconds
- **Offline-tolerant** — errors are surfaced clearly, retry is straightforward

---

## 2. Architecture

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
│  Timer trigger (23:00) ──────┼──► SendGrid API
│                              │       (daily CSV email)
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

### Why These Choices

- **Azure Static Web Apps** — free tier available, built-in API proxy to Functions, SSL included, custom domain support, no App Service Plan needed
- **Azure Table Storage** — dirt cheap, no schema, partitioned by date so each day is isolated. Uses the same storage account as the Function App (no extra resource needed)
- **SendGrid** — already in use for Voiceflow agent emails, no new vendor

---

## 3. Project Structure

```
nymr-site-signin/
├── .github/
│   └── workflows/
│       └── deploy.yml              ← GitHub Actions CI/CD
├── frontend/
│   ├── public/
│   │   └── favicon.svg
│   ├── src/
│   │   ├── main.jsx                ← React entry point
│   │   ├── App.jsx                 ← Main kiosk UI (both screens)
│   │   ├── SignaturePad.jsx         ← Touch signature canvas component
│   │   └── api.js                  ← API client (fetch wrapper)
│   ├── index.html                  ← HTML shell (iPad meta tags)
│   ├── vite.config.js              ← Vite config (dev proxy to Functions)
│   ├── staticwebapp.config.json    ← Azure SWA routing config
│   └── package.json
├── backend/
│   ├── function_app.py             ← All Azure Function endpoints
│   ├── requirements.txt            ← Python dependencies
│   ├── host.json                   ← Functions host config
│   └── local.settings.json         ← Local dev settings (DO NOT COMMIT)
├── .gitignore
└── README.md                       ← This file
```

---

## 4. API Endpoints

All endpoints are anonymous (no auth) — the app is designed for an unattended kiosk on a private network. If internet-facing access control is needed later, Azure SWA supports IP restrictions and auth providers.

### GET /api/register

Returns today's sign-in and sign-out lists.

**Response:**
```json
{
  "signedIn": [
    {
      "id": "uuid",
      "name": "John Smith",
      "org": "NYMR",
      "dateIn": "25/03/2026",
      "timeIn": "09:15",
      "timeOut": "",
      "signature": "data:image/png;base64,..."
    }
  ],
  "signedOut": [
    {
      "id": "uuid",
      "name": "Jane Doe",
      "org": "ABC Contractors",
      "dateIn": "25/03/2026",
      "timeIn": "08:30",
      "timeOut": "12:45",
      "signature": "..."
    }
  ]
}
```

### POST /api/signin

**Body:**
```json
{
  "name": "John Smith",
  "org": "NYMR",
  "signature": "data:image/png;base64,..."
}
```

**Response:**
```json
{
  "id": "uuid",
  "dateIn": "25/03/2026",
  "timeIn": "09:15"
}
```

### POST /api/signout

**Body:**
```json
{
  "id": "uuid-of-the-signin-record"
}
```

**Response:**
```json
{
  "ok": true,
  "timeOut": "16:30"
}
```

### GET /api/export

Returns a CSV file download of today's full register.

### POST /api/send-daily-email

Manually triggers the daily summary email. Same logic as the 23:00 timer.

---

## 5. Data Storage — Azure Table Storage

The backend uses a single table called `SiteRegister`. Each row is one sign-in event.

### Schema

| Field | Type | Description |
|-------|------|-------------|
| PartitionKey | string | Date in `YYYY-MM-DD` format (one partition per day) |
| RowKey | string | UUID (unique per sign-in) |
| Name | string | Visitor name |
| Org | string | Organisation |
| DateIn | string | UK date `DD/MM/YYYY` |
| TimeIn | string | UK time `HH:MM` |
| TimeOut | string | UK time `HH:MM` or empty if still on site |
| Signature | string | Base64-encoded PNG (max ~500KB) |

### Data Lifecycle

- Each day gets its own partition key, so queries are fast
- Old data is never automatically deleted — it accumulates (Table Storage is very cheap: ~£0.05/GB/month)
- If you want to purge old data, write a scheduled function that deletes partitions older than N days

---

## 6. Email — SendGrid Configuration

The daily email is sent at 23:00 UTC via a timer-triggered Azure Function. It can also be triggered manually from the sign-out screen.

### Email Content

- **Subject:** `Site Register — {SITE_NAME} — {date}`
- **Body:** Summary counts (total sign-ins, signed out, still on site) plus a warning list of anyone who hasn't signed out
- **Attachment:** CSV file with all records

### Required App Settings

| Setting | Example | Description |
|---------|---------|-------------|
| `SENDGRID_API_KEY` | `SG.xxxx` | Your SendGrid API key |
| `DAILY_EMAIL_TO` | `chris.flynn@nymr.co.uk,site.manager@nymr.co.uk` | Comma-separated recipients |
| `DAILY_EMAIL_FROM` | `noreply@nymr.co.uk` | Must be a verified sender in SendGrid |
| `SITE_NAME` | `Pickering Construction Site` | Used in email subject and body |

### Timer Timezone

The timer trigger in `function_app.py` is set to `0 0 23 * * *` (23:00 UTC). Since the Function App should be set to UK timezone, add this app setting:

```
WEBSITE_TIME_ZONE = GMT Standard Time
```

This ensures the timer fires at 23:00 UK time (adjusting for BST automatically).

---

## 7. Local Development Setup

### Prerequisites

- Node.js 18+ and npm
- Python 3.11
- Azure Functions Core Tools v4 (`npm install -g azure-functions-core-tools@4`)
- Azure Storage Emulator or Azurite (`npm install -g azurite`)

### Step 1 — Start the Storage Emulator

```bash
azurite --silent --location ./azurite-data
```

This provides local Table Storage on the default connection string `UseDevelopmentStorage=true`.

### Step 2 — Start the Backend

```bash
cd backend
python -m venv .venv
source .venv/bin/activate          # Windows: .venv\Scripts\activate
pip install -r requirements.txt
func start
```

The Functions host will start on `http://localhost:7071`.

### Step 3 — Start the Frontend

```bash
cd frontend
npm install
npm run dev
```

Vite starts on `http://localhost:5173` and proxies `/api/*` requests to the Functions host (configured in `vite.config.js`).

### Step 4 — Open in Browser

Navigate to `http://localhost:5173`. The app should work end-to-end with local storage.

---

## 8. Deployment to Azure

### Option A — Azure Static Web Apps (Recommended)

This is the simplest path. One resource, one deployment, frontend + backend together.

#### 8a.1 — Create the Static Web App

1. In the Azure Portal, search for **Static Web Apps** and click **Create**
2. Choose the **Free** plan (sufficient for this use case)
3. Connect to the GitHub repo containing this project
4. Set:
   - **App location:** `frontend`
   - **API location:** `backend`
   - **Output location:** `dist`
5. Azure will auto-generate a GitHub Actions workflow (you can use the one already in `.github/workflows/deploy.yml` instead — just paste the deployment token into the repo secret `AZURE_STATIC_WEB_APPS_API_TOKEN`)

#### 8a.2 — Configure App Settings

In the Azure Portal, navigate to the Static Web App → **Configuration** → **Application settings** and add:

| Name | Value |
|------|-------|
| `SENDGRID_API_KEY` | `SG.your-key` |
| `DAILY_EMAIL_TO` | `chris.flynn@nymr.co.uk` |
| `DAILY_EMAIL_FROM` | `noreply@nymr.co.uk` |
| `SITE_NAME` | `NYMR Pickering Construction Site` |

#### 8a.3 — Custom Domain (Optional)

If you want this on e.g. `signin.nymr.co.uk`:

1. Static Web App → **Custom domains** → **Add**
2. Add a CNAME record: `signin.nymr.co.uk` → `<your-swa>.azurestaticapps.net`
3. Azure will auto-provision an SSL certificate

#### 8a.4 — IMPORTANT: Timer Trigger Limitation

Azure Static Web Apps managed Functions do **not** support timer triggers. The daily email timer will need one of these workarounds:

**Option 1 (simplest):** Use Azure Logic Apps to call `POST /api/send-daily-email` at 23:00 daily. Create a Logic App with a Recurrence trigger → HTTP action pointing at your SWA URL.

**Option 2:** Deploy the backend as a standalone Azure Function App instead (see Option B below) and point the frontend API proxy at it.

**Option 3:** Site manager manually clicks "Email Report Now" at end of day.

### Option B — Separate Function App + Static Web App

If you need the timer trigger to work natively:

1. Create an **Azure Function App** (Python 3.11, Consumption plan, UK West)
2. Deploy the `backend/` folder to it via VS Code Azure Functions extension or `func azure functionapp publish <name>`
3. Create an **Azure Static Web App** for the frontend only
4. In `staticwebapp.config.json`, remove the API platform config and add a route redirect:

```json
{
  "routes": [
    {
      "route": "/api/*",
      "rewrite": "https://your-function-app.azurewebsites.net/api/*"
    }
  ]
}
```

5. Configure CORS on the Function App to allow the Static Web App origin

---

## 9. iPad Kiosk Setup

### Safari Full-Screen (Guided Access)

1. Open Safari on the iPad and navigate to the app URL
2. Tap **Share → Add to Home Screen** — this creates an app icon and removes the Safari chrome
3. Open the home screen icon — it launches as a full-screen web app (the `apple-mobile-web-app-capable` meta tag enables this)
4. Enable **Guided Access** (Settings → Accessibility → Guided Access) to lock the iPad to this app
5. Triple-click the Home/Side button to start Guided Access

### Recommended iPad Settings

- **Auto-Lock:** Never (Settings → Display & Brightness)
- **Do Not Disturb:** On (prevents notification interruptions)
- **Rotation Lock:** On (portrait recommended for this layout)
- **Wi-Fi:** Connected to the site network with reliable internet

---

## 10. Editing the Safety Briefing

The briefing text is a constant at the top of `frontend/src/App.jsx`:

```javascript
const BRIEFING_TEXT = `Welcome to the NYMR Construction Site.
...
`;
```

Edit this text, commit, push — the GitHub Action will auto-deploy.

If Chris needs to edit the briefing without a code deploy, a future enhancement would be to store the briefing text in Table Storage and add an admin endpoint to update it.

---

## 11. Future Enhancements (Out of Scope for v1)

These are ideas Chris has flagged for potential future iterations:

- **Admin PIN** — protect the sign-out screen and CSV export behind a simple PIN
- **Photo capture** — use the iPad camera to take a photo on sign-in (useful for site security)
- **Editable briefing** — admin screen to update the safety briefing text without redeploying
- **Multi-site** — support multiple construction sites from one deployment (partition by site + date)
- **Offline mode** — service worker to queue sign-ins when connectivity drops, sync when restored
- **QR badge scan** — regular contractors get a QR badge for one-tap sign-in
- **GDPR data retention** — scheduled purge of records older than a configurable period
- **Signature storage** — move signature images to Azure Blob Storage instead of Table Storage to avoid row size limits

---

## 12. Troubleshooting

| Problem | Likely Cause | Fix |
|---------|-------------|-----|
| "Sign-in failed" error | Backend not running or network issue | Check Function App is healthy in Azure Portal; check iPad has internet |
| Names not appearing on sign-out screen | Polling lag | Wait 30 seconds or pull-to-refresh; check `/api/register` directly |
| Daily email not sent | Timer not firing (SWA limitation) or SendGrid misconfigured | Check Function App logs; verify `SENDGRID_API_KEY` is set; use Logic App workaround |
| Signature not working | Canvas touch events blocked | Ensure no iPad accessibility gestures are overriding touch on the canvas element |
| CSV shows garbled characters | Encoding issue | The export uses `utf-8-sig` encoding which Notepad and Excel handle correctly |
| "Still on site" in CSV | Person forgot to sign out | Expected behaviour — the email also warns about un-signed-out people |

---

## 13. Security Notes

- The app has **no authentication** by design — it's a kiosk on a construction site
- If exposed to the internet, consider adding Azure SWA IP restrictions to limit access to the site network
- Signature data (base64 PNG) is stored in Table Storage — it's not encrypted at rest beyond Azure's default storage encryption
- The SendGrid API key is stored in Azure App Settings (encrypted at rest by Azure)
- No personal data beyond name, organisation, and signature is collected — GDPR-compliant for legitimate interest in site safety management

---

## 14. Contacts

| Role | Name | Reach |
|------|------|-------|
| Project Owner / IT Manager | Chris Flynn | chris.flynn@nymr.co.uk |
| Developer | Jason Plenty | (internal) |

---

*End of handover document.*

// ------------------------------------------------------------------
// api.js — API wrapper for the NYMR Site Sign-In backend
// ------------------------------------------------------------------

const BASE = '';

async function request(method, path, body = null) {
  const opts = {
    method,
    headers: { 'Content-Type': 'application/json' },
  };
  if (body) opts.body = JSON.stringify(body);

  const res = await fetch(`${BASE}${path}`, opts);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`API ${method} ${path} failed (${res.status}): ${text}`);
  }
  return res.json();
}

/** Fetch today's register (signed-in + signed-out + denied lists) */
export function getRegister() {
  return request('GET', '/api/register');
}

/** Sign a person in (initial step — before medical declaration) */
export function signIn({ name, org, signature }) {
  return request('POST', '/api/signin', { name, org, signature });
}

/** Confirm medical fitness (YES path) */
export function confirmFit({ id, additionalInfo, siteCode }) {
  return request('POST', '/api/confirm-fit', { id, additionalInfo, siteCode });
}

/** Declare NOT fit — triggers email to Phil with one-time code */
export function declareNotFit({ id, additionalInfo }) {
  return request('POST', '/api/declare-not-fit', { id, additionalInfo });
}

/** Submit one-time site code (after Phil conversation) */
export function submitSiteCode({ id, siteCode, additionalInfo }) {
  return request('POST', '/api/submit-site-code', { id, siteCode, additionalInfo });
}

/** Sign a person out by their record ID */
export function signOut(id) {
  return request('POST', '/api/signout', { id });
}

/** Download today's CSV */
export async function exportCSV() {
  const res = await fetch(`${BASE}/api/export`);
  if (!res.ok) throw new Error('Export failed');
  return res.blob();
}

/** Trigger the end-of-day email manually */
export function sendDailyEmail() {
  return request('POST', '/api/send-daily-email');
}

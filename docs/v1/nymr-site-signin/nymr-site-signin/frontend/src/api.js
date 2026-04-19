// ------------------------------------------------------------------
// api.js — thin wrapper around the Azure Functions backend
// Base URL is '' in production (same-origin via Static Web App proxy).
// In local dev, Vite proxies /api/* to http://localhost:7071.
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

/** Fetch today's register (signed-in + signed-out lists) */
export function getRegister() {
  return request('GET', '/api/register');
}

/** Sign a person in */
export function signIn({ name, org, signature }) {
  return request('POST', '/api/signin', { name, org, signature });
}

/** Sign a person out by their record ID */
export function signOut(id) {
  return request('POST', '/api/signout', { id });
}

/** Trigger the CSV export (returns a download URL or blob) */
export async function exportCSV() {
  const res = await fetch(`${BASE}/api/export`);
  if (!res.ok) throw new Error('Export failed');
  return res.blob();
}

/** Trigger the end-of-day email manually */
export function sendDailyEmail() {
  return request('POST', '/api/send-daily-email');
}

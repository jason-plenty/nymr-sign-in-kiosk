import { useState, useEffect, useRef, useCallback } from 'react';
import SignaturePad from './SignaturePad';
import { getRegister, signIn, signOut, exportCSV, sendDailyEmail } from './api';

// ------------------------------------------------------------------
// Configuration — edit the briefing text here
// ------------------------------------------------------------------
const BRIEFING_TEXT = `Welcome to the NYMR Construction Site.

Before entering this site you MUST read and understand the following:

\u2022 All visitors must report to the Site Manager on arrival.
\u2022 Hard hats, high-visibility clothing and safety footwear must be worn at all times beyond this point.
\u2022 Do not operate any machinery or equipment unless authorised.
\u2022 Report all accidents, incidents and near-misses immediately.
\u2022 Be aware of overhead works, moving plant and uneven surfaces.
\u2022 Emergency assembly point is at the main car park entrance.
\u2022 First aid kit is located in the site office.

By signing in below you confirm you have read and understood these requirements.`;

// ------------------------------------------------------------------
// Shared style tokens
// ------------------------------------------------------------------
const C = {
  yellow: '#FFD100',
  yellowDark: '#E6BC00',
  black: '#1a1a1a',
  dark: '#2d2d2d',
  mid: '#555',
  light: '#f0f0ec',
  white: '#ffffff',
  red: '#D32F2F',
  redDark: '#B71C1C',
  green: '#2E7D32',
  blue: '#1565C0',
};
const FONT = "'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, sans-serif";

// ------------------------------------------------------------------
// Helpers
// ------------------------------------------------------------------
function todayStr() {
  const d = new Date();
  return `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`;
}
function timeStr() {
  const d = new Date();
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}

// ------------------------------------------------------------------
// Press-style button helper (visual feedback for touch)
// ------------------------------------------------------------------
function pressProps(bg, activeBg) {
  return {
    onMouseDown: (e) => (e.currentTarget.style.background = activeBg),
    onMouseUp: (e) => (e.currentTarget.style.background = bg),
    onTouchStart: (e) => (e.currentTarget.style.background = activeBg),
    onTouchEnd: (e) => (e.currentTarget.style.background = bg),
  };
}

// ------------------------------------------------------------------
// App
// ------------------------------------------------------------------
export default function App() {
  const [screen, setScreen] = useState('signin'); // signin | signout
  const [name, setName] = useState('');
  const [org, setOrg] = useState('');
  const [signedInList, setSignedInList] = useState([]);
  const [signedOutList, setSignedOutList] = useState([]);
  const [confirmation, setConfirmation] = useState(null);
  const [toast, setToast] = useState(null);
  const [loading, setLoading] = useState(false);
  const sigPadRef = useRef(null);
  const nameInputRef = useRef(null);
  const pollRef = useRef(null);

  // ---- data fetch ----
  const refresh = useCallback(async () => {
    try {
      const data = await getRegister();
      setSignedInList(data.signedIn || []);
      setSignedOutList(data.signedOut || []);
    } catch (err) {
      console.error('Refresh failed:', err);
    }
  }, []);

  // Initial load + 30-second polling (so multiple iPads stay in sync)
  useEffect(() => {
    refresh();
    pollRef.current = setInterval(refresh, 30000);
    return () => clearInterval(pollRef.current);
  }, [refresh]);

  // ---- toast helper ----
  const showToast = (msg, duration = 4000) => {
    setToast(msg);
    setTimeout(() => setToast(null), duration);
  };

  // ---- sign in handler ----
  const handleSignIn = async () => {
    if (!name.trim()) { showToast('Please enter your name.'); return; }
    if (!org.trim()) { showToast('Please enter your organisation.'); return; }
    if (sigPadRef.current && sigPadRef.current.isEmpty()) {
      showToast('Please provide your signature.');
      return;
    }
    setLoading(true);
    try {
      const sig = sigPadRef.current ? sigPadRef.current.toDataURL() : null;
      const result = await signIn({ name: name.trim(), org: org.trim(), signature: sig });
      setConfirmation({
        name: name.trim(),
        time: result.timeIn || timeStr(),
        date: result.dateIn || todayStr(),
      });
      setName('');
      setOrg('');
      if (sigPadRef.current) sigPadRef.current.clear();
      await refresh();
      setTimeout(() => setConfirmation(null), 6000);
      // Refocus name input for next person
      if (nameInputRef.current) nameInputRef.current.focus();
    } catch (err) {
      showToast('Sign-in failed. Please try again.');
      console.error(err);
    }
    setLoading(false);
  };

  // ---- sign out handler ----
  const handleSignOut = async (id, personName) => {
    if (!window.confirm(`Sign out ${personName}?`)) return;
    setLoading(true);
    try {
      await signOut(id);
      await refresh();
      showToast(`${personName} signed out at ${timeStr()}.`);
    } catch (err) {
      showToast('Sign-out failed. Please try again.');
      console.error(err);
    }
    setLoading(false);
  };

  // ---- CSV export ----
  const handleExport = async () => {
    try {
      const blob = await exportCSV();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `site-register-${todayStr().replace(/\//g, '-')}.csv`;
      a.click();
      URL.revokeObjectURL(url);
      showToast('CSV downloaded.');
    } catch (err) {
      showToast('Export failed.');
      console.error(err);
    }
  };

  // ---- email trigger ----
  const handleEmail = async () => {
    try {
      await sendDailyEmail();
      showToast('Daily email sent.');
    } catch (err) {
      showToast('Email send failed.');
      console.error(err);
    }
  };

  // ================================================================
  // RENDER — SIGN-IN SCREEN
  // ================================================================
  if (screen === 'signin') {
    return (
      <div style={{ fontFamily: FONT, minHeight: '100vh', background: C.light, display: 'flex', flexDirection: 'column' }}>
        {/* HEADER */}
        <div style={{ background: C.black, borderBottom: `6px solid ${C.yellow}`, padding: '16px 20px', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <div>
            <h1 style={{ color: C.yellow, fontSize: '26px', fontWeight: 800, margin: 0, letterSpacing: '1px', textTransform: 'uppercase' }}>
              ⚠ Site Sign-In Register
            </h1>
            <p style={{ color: '#ccc', margin: '4px 0 0', fontSize: '14px' }}>{todayStr()}</p>
          </div>
          <div style={{ background: C.yellow, color: C.black, fontWeight: 800, fontSize: '14px', padding: '8px 16px', borderRadius: '6px' }}>
            {signedInList.length} ON SITE
          </div>
        </div>

        {/* TOAST / CONFIRMATION */}
        {confirmation && (
          <div style={{ background: C.green, color: C.white, padding: '18px 20px', fontSize: '22px', fontWeight: 700, textAlign: 'center' }}>
            ✓ {confirmation.name} — Signed in at {confirmation.time} on {confirmation.date}
          </div>
        )}
        {toast && !confirmation && (
          <div style={{ background: '#ff9800', color: C.white, padding: '14px 20px', fontSize: '18px', fontWeight: 600, textAlign: 'center' }}>
            {toast}
          </div>
        )}

        {/* BODY */}
        <div style={{ padding: '16px 20px', flex: 1, overflowY: 'auto', WebkitOverflowScrolling: 'touch' }}>
          {/* BRIEFING */}
          <div style={{ background: C.white, border: `3px solid ${C.black}`, borderRadius: '10px', padding: '20px', marginBottom: '16px' }}>
            <h2 style={{ margin: '0 0 12px', fontSize: '20px', fontWeight: 800, textTransform: 'uppercase', borderBottom: `3px solid ${C.yellow}`, paddingBottom: '8px', color: C.black }}>
              📋 Site Safety Briefing
            </h2>
            <pre style={{ fontFamily: FONT, fontSize: '15px', lineHeight: 1.6, color: C.dark, whiteSpace: 'pre-wrap', margin: 0 }}>
              {BRIEFING_TEXT}
            </pre>
          </div>

          {/* SIGN-IN FORM */}
          <div style={{ background: C.white, border: `3px solid ${C.black}`, borderRadius: '10px', padding: '20px', marginBottom: '16px' }}>
            <h2 style={{ margin: '0 0 16px', fontSize: '20px', fontWeight: 800, textTransform: 'uppercase', borderBottom: `3px solid ${C.yellow}`, paddingBottom: '8px', color: C.black }}>
              ✍ Sign In Below
            </h2>

            <label style={{ display: 'block', fontSize: '16px', fontWeight: 700, color: C.dark, marginBottom: '6px' }}>YOUR NAME</label>
            <input
              ref={nameInputRef}
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. John Smith"
              autoComplete="off"
              style={{ width: '100%', boxSizing: 'border-box', padding: '16px', fontSize: '20px', border: '3px solid #333', borderRadius: '8px', marginBottom: '14px', background: '#fffef5' }}
            />

            <label style={{ display: 'block', fontSize: '16px', fontWeight: 700, color: C.dark, marginBottom: '6px' }}>COMPANY / ORGANISATION</label>
            <input
              type="text"
              value={org}
              onChange={(e) => setOrg(e.target.value)}
              placeholder="e.g. NYMR, ABC Contractors"
              autoComplete="off"
              style={{ width: '100%', boxSizing: 'border-box', padding: '16px', fontSize: '20px', border: '3px solid #333', borderRadius: '8px', marginBottom: '14px', background: '#fffef5' }}
            />

            <label style={{ display: 'block', fontSize: '16px', fontWeight: 700, color: C.dark, marginBottom: '6px' }}>YOUR SIGNATURE</label>
            <SignaturePad ref={sigPadRef} />
            <button
              onClick={() => sigPadRef.current && sigPadRef.current.clear()}
              style={{ marginTop: '6px', background: 'none', border: 'none', color: C.blue, fontSize: '14px', fontWeight: 600, cursor: 'pointer', padding: '4px 0' }}
            >
              Clear Signature
            </button>

            <button
              onClick={handleSignIn}
              disabled={loading}
              style={{
                display: 'block', width: '100%', marginTop: '16px', padding: '22px',
                fontSize: '26px', fontWeight: 800, textTransform: 'uppercase', letterSpacing: '1px',
                color: C.black, background: C.yellow, border: `3px solid ${C.black}`,
                borderRadius: '10px', cursor: 'pointer', opacity: loading ? 0.6 : 1,
              }}
              {...pressProps(C.yellow, C.yellowDark)}
            >
              {loading ? 'SIGNING IN...' : '✓ SIGN IN NOW'}
            </button>
          </div>
        </div>

        {/* SIGN-OUT NAVIGATION — fixed at bottom, unmissable */}
        <div style={{ background: C.red, borderTop: `6px solid ${C.redDark}`, padding: '16px 20px', textAlign: 'center', flexShrink: 0 }}>
          <p style={{ color: 'rgba(255,255,255,0.85)', fontSize: '15px', margin: '0 0 10px', fontWeight: 600 }}>
            LEAVING THE SITE? TAP THE BUTTON BELOW TO SIGN OUT
          </p>
          <button
            onClick={() => { setScreen('signout'); refresh(); }}
            style={{
              padding: '18px 40px', fontSize: '22px', fontWeight: 800, textTransform: 'uppercase',
              letterSpacing: '1px', color: C.white, background: C.redDark,
              border: '3px solid rgba(255,255,255,0.5)', borderRadius: '10px', cursor: 'pointer',
            }}
          >
            🚪 GO TO SIGN OUT →
          </button>
        </div>
      </div>
    );
  }

  // ================================================================
  // RENDER — SIGN-OUT SCREEN
  // ================================================================
  return (
    <div style={{ fontFamily: FONT, minHeight: '100vh', background: C.light, display: 'flex', flexDirection: 'column' }}>
      {/* HEADER */}
      <div style={{ background: C.red, borderBottom: `6px solid ${C.redDark}`, padding: '16px 20px', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div>
          <h1 style={{ color: C.white, fontSize: '26px', fontWeight: 800, margin: 0, letterSpacing: '1px', textTransform: 'uppercase' }}>
            🚪 Site Sign-Out
          </h1>
          <p style={{ color: 'rgba(255,255,255,0.7)', margin: '4px 0 0', fontSize: '14px' }}>
            {todayStr()} — Tap your name to sign out
          </p>
        </div>
        <button
          onClick={() => setScreen('signin')}
          style={{ background: C.white, color: C.red, fontWeight: 800, fontSize: '14px', padding: '10px 18px', borderRadius: '8px', border: 'none', cursor: 'pointer', textTransform: 'uppercase' }}
        >
          ← BACK TO SIGN IN
        </button>
      </div>

      {/* TOAST */}
      {toast && (
        <div style={{ background: C.green, color: C.white, padding: '14px 20px', fontSize: '18px', fontWeight: 600, textAlign: 'center' }}>
          {toast}
        </div>
      )}

      <div style={{ padding: '16px 20px', flex: 1, overflowY: 'auto', WebkitOverflowScrolling: 'touch' }}>
        {/* CURRENTLY ON SITE */}
        <div style={{ background: C.white, border: `3px solid ${C.black}`, borderRadius: '10px', padding: '20px', marginBottom: '16px' }}>
          <h2 style={{ margin: '0 0 16px', fontSize: '20px', fontWeight: 800, textTransform: 'uppercase', borderBottom: `3px solid ${C.yellow}`, paddingBottom: '8px', color: C.black }}>
            👷 Currently On Site ({signedInList.length})
          </h2>

          {signedInList.length === 0 ? (
            <p style={{ color: C.mid, fontSize: '18px', textAlign: 'center', padding: '24px 0', fontStyle: 'italic' }}>
              Nobody is currently signed in.
            </p>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
              {signedInList.map((person) => (
                <div
                  key={person.id}
                  style={{
                    display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                    background: '#f8f8f5', border: '2px solid #ddd', borderRadius: '8px', padding: '14px 16px',
                  }}
                >
                  <div style={{ flex: 1 }}>
                    <div style={{ fontSize: '20px', fontWeight: 700, color: C.black }}>{person.name}</div>
                    <div style={{ fontSize: '14px', color: C.mid, marginTop: '2px' }}>
                      {person.org} — In since {person.timeIn}
                    </div>
                  </div>
                  <button
                    onClick={() => handleSignOut(person.id, person.name)}
                    disabled={loading}
                    style={{
                      padding: '16px 28px', fontSize: '18px', fontWeight: 800, textTransform: 'uppercase',
                      color: C.white, background: C.red, border: 'none', borderRadius: '8px',
                      cursor: 'pointer', flexShrink: 0, marginLeft: '12px', minWidth: '140px',
                    }}
                    {...pressProps(C.red, C.redDark)}
                  >
                    SIGN OUT
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* SIGNED OUT TODAY */}
        <div style={{ background: C.white, border: '3px solid #ccc', borderRadius: '10px', padding: '20px', marginBottom: '16px' }}>
          <h2 style={{ margin: '0 0 16px', fontSize: '18px', fontWeight: 700, textTransform: 'uppercase', color: C.mid, borderBottom: '2px solid #ddd', paddingBottom: '8px' }}>
            ✓ Signed Out Today ({signedOutList.length})
          </h2>

          {signedOutList.length === 0 ? (
            <p style={{ color: '#aaa', fontSize: '16px', textAlign: 'center', padding: '12px 0', fontStyle: 'italic' }}>
              No one has signed out yet today.
            </p>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
              {signedOutList.map((person, i) => (
                <div key={i} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '10px 14px', borderBottom: '1px solid #eee', fontSize: '16px' }}>
                  <span style={{ fontWeight: 600, color: C.dark }}>{person.name}</span>
                  <span style={{ color: C.mid, fontSize: '14px' }}>
                    {person.org} — In: {person.timeIn} → Out: {person.timeOut}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* ADMIN — CSV + EMAIL */}
        <div style={{ background: C.white, border: '2px solid #ccc', borderRadius: '10px', padding: '16px 20px', textAlign: 'center', marginBottom: '20px' }}>
          <p style={{ color: C.mid, fontSize: '14px', margin: '0 0 12px' }}>
            End of day — download report or trigger email
          </p>
          <div style={{ display: 'flex', gap: '12px', justifyContent: 'center', flexWrap: 'wrap' }}>
            <button onClick={handleExport} style={{ padding: '14px 24px', fontSize: '16px', fontWeight: 700, color: C.white, background: C.blue, border: 'none', borderRadius: '8px', cursor: 'pointer' }}>
              📥 DOWNLOAD CSV
            </button>
            <button onClick={handleEmail} style={{ padding: '14px 24px', fontSize: '16px', fontWeight: 700, color: C.white, background: C.green, border: 'none', borderRadius: '8px', cursor: 'pointer' }}>
              📧 EMAIL REPORT NOW
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

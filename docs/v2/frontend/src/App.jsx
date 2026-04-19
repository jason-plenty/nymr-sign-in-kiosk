import { useState, useEffect, useRef, useCallback } from 'react';
import SignaturePad from './SignaturePad';
import {
  getRegister, signIn, confirmFit, declareNotFit,
  submitSiteCode, signOut, exportCSV, sendDailyEmail,
} from './api';

// ------------------------------------------------------------------
// SAFETY BRIEFING TEXT — Bridge 42, Grosmont
// ------------------------------------------------------------------
const SAFETY_SECTIONS = [
  {
    title: 'Project Overview',
    content: `Project: Bridge 42 Temporary Propping Works
Location: Grosmont, North Yorkshire Moors Railway (NYMR)
Client: NYMR Trust
Main Contractor: Construction Marine LTD

Scope of Works:
\u2022 Installation of temporary propping to the brick arch intrados
\u2022 River damming and construction of temporary foundations
\u2022 Scaffolding and access installations
\u2022 Crane operations for lifting and positioning of propping elements
\u2022 Monitoring of bridge stability and levels
\u2022 Associated site setup and reinstatement works

The worksite is designated as a High Street Environment within a TIII Possession, meaning the railway is blocked to all train movements and under the control of the NYMR Engineering Supervisor (ES).`,
  },
  {
    title: 'Site Boundaries',
    content: `The limits of the worksite are clearly defined by:
\u2022 Heras fencing
\u2022 Existing stone parapet walls
\u2022 Existing wooden boundary fencing

Unauthorised access beyond these boundaries is strictly prohibited.`,
  },
  {
    title: 'Access & Egress',
    content: `Access to and from the site is strictly controlled via:
1. South East Gate \u2014 pedestrian access via the public footpath
2. South Level Crossing (Grosmont LX) Gates \u2014 for plant, deliveries, and emergency access

All personnel must attend this induction and sign in and out of the site. Visitors and delivery vehicles must be escorted at all times.`,
  },
  {
    title: 'Mandatory PPE',
    content: `\u2022 Safety helmet
\u2022 High-visibility clothing
\u2022 Safety boots
\u2022 Gloves
\u2022 Eye protection

Additional PPE (e.g., life jackets, harnesses, or hearing protection) must be worn as required by specific tasks.`,
  },
  {
    title: 'Key Site Hazards',
    content: `\u2022 Working near water \u2014 risk of drowning; life-saving equipment provided
\u2022 Lifting operations \u2014 obey exclusion zones and banksman instructions
\u2022 Public interface \u2014 maintain vigilance near footpaths and local businesses
\u2022 Heavy plant movements \u2014 follow designated routes and site speed limits
\u2022 Structural stability \u2014 do not interfere with the bridge or propping system`,
  },
  {
    title: 'Track Safety Risk',
    content: `\u2022 Don't step on sleepers \u2014 risk of slips & falls, likely to be slippery when damp. Use ballast
\u2022 Avoid Points and Point Rodding \u2014 risk of trips, falls, trapping of foot
\u2022 Don't step on rails \u2014 risk of slips and falls
\u2022 Don't move points \u2014 obey Engineering Supervisor instructions
\u2022 Remain within the worksite or public areas \u2014 strictly no access to any other track or lineside area`,
  },
  {
    title: 'Emergency Arrangements',
    content: `\u2022 Emergency Services: Dial 999 or 112
\u2022 Assembly Point: Near the South East Gate
\u2022 Emergency Access Route: Via Grosmont Level Crossing

All accidents, incidents, and near misses must be reported immediately to the Site Manager.`,
  },
  {
    title: 'Environmental Controls',
    content: `\u2022 Spill kits are available; report any pollution immediately
\u2022 Waste must be disposed of in designated areas
\u2022 Protect the river from contamination at all times
\u2022 Respect the local community by minimising noise and disturbance`,
  },
  {
    title: 'Site Rules',
    content: `\u2022 The site operates as a controlled construction environment
\u2022 Use of the local pub is strictly prohibited
\u2022 No more than two workers are permitted in local shops at any one time to minimise disruption to the community
\u2022 Smoking is not permitted anywhere on site
\u2022 A designated smoking area is provided within the fenced compound in the car park \u2014 this is the only location where smoking is allowed
\u2022 Alcohol and drugs are strictly prohibited
\u2022 Follow all instructions from the Engineering Supervisor and Site Manager
\u2022 Maintain good housekeeping and respect neighbouring properties`,
  },
];

const DECLARATION_TEXT = `I confirm that I have received and understood the NYMR Bridge 42 Site Introduction Briefing and agree to comply with all site rules and safety requirements.

I also accept that a separate briefing will be provided by CML before I am authorised to enter site or carry out any activity in relation to this project.`;

const CLOSING_QUOTE = `"NYMR say Safety, respect for the local community, and protection of the railway asset are paramount. If in doubt, stop work and seek guidance from the Site Manager."`;

// ------------------------------------------------------------------
// MEDICAL DECLARATION TEXT
// ------------------------------------------------------------------
const MEDICAL_SECTIONS = [
  {
    title: 'Medication and Fitness for Duty Today at Bridge 42 Grosmont',
    items: [
      'I am not taking any medication that may impair my ability to perform my duties safely.',
      'I am not under the influence of Drugs or Alcohol.',
      'I confirm that I am fit to resume my normal duties, including any safety-critical tasks associated with my role.',
    ],
  },
  {
    title: 'Symptoms Affecting Safety',
    intro: 'I confirm that I am free from symptoms that could impair my performance, such as:',
    items: [
      'Fatigue or excessive drowsiness',
      'Dizziness or blackouts',
      'Impaired vision or hearing',
      'Reduced mobility or coordination',
      'Effects of drugs or alcohol',
    ],
  },
];

// ------------------------------------------------------------------
// Style tokens
// ------------------------------------------------------------------
const C = {
  yellow: '#FFD100', yellowDark: '#E6BC00',
  black: '#1a1a1a', dark: '#2d2d2d', mid: '#555', light: '#f0f0ec', white: '#ffffff',
  red: '#D32F2F', redDark: '#B71C1C',
  green: '#2E7D32', greenDark: '#1B5E20',
  blue: '#1565C0', blueDark: '#0D47A1',
  orange: '#E65100',
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
function pressProps(bg, activeBg) {
  return {
    onMouseDown: (e) => (e.currentTarget.style.background = activeBg),
    onMouseUp: (e) => (e.currentTarget.style.background = bg),
    onTouchStart: (e) => (e.currentTarget.style.background = activeBg),
    onTouchEnd: (e) => (e.currentTarget.style.background = bg),
  };
}

// ------------------------------------------------------------------
// Shared UI components
// ------------------------------------------------------------------
function Card({ children, border = C.black, style = {} }) {
  return (
    <div style={{ background: C.white, border: `3px solid ${border}`, borderRadius: '10px', padding: '20px', marginBottom: '16px', ...style }}>
      {children}
    </div>
  );
}
function SectionHeading({ children, accent = C.yellow }) {
  return (
    <h2 style={{ margin: '0 0 16px', fontSize: '20px', fontWeight: 800, textTransform: 'uppercase', borderBottom: `3px solid ${accent}`, paddingBottom: '8px', color: C.black }}>
      {children}
    </h2>
  );
}
function BigButton({ onClick, bg, activeBg, children, disabled = false, style = {} }) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      style={{
        display: 'block', width: '100%', padding: '22px', fontSize: '24px', fontWeight: 800,
        textTransform: 'uppercase', letterSpacing: '1px', color: C.white, background: bg,
        border: 'none', borderRadius: '10px', cursor: 'pointer',
        opacity: disabled ? 0.5 : 1, ...style,
      }}
      {...pressProps(bg, activeBg)}
    >
      {children}
    </button>
  );
}
function TextArea({ value, onChange, placeholder, rows = 3 }) {
  return (
    <textarea
      value={value}
      onChange={(e) => onChange(e.target.value)}
      placeholder={placeholder}
      rows={rows}
      style={{
        width: '100%', boxSizing: 'border-box', padding: '14px', fontSize: '18px',
        border: '3px solid #333', borderRadius: '8px', background: '#fffef5',
        fontFamily: FONT, resize: 'vertical',
      }}
    />
  );
}
function InputField({ value, onChange, placeholder, label }) {
  return (
    <>
      {label && <label style={{ display: 'block', fontSize: '16px', fontWeight: 700, color: C.dark, marginBottom: '6px' }}>{label}</label>}
      <input
        type="text"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        autoComplete="off"
        style={{
          width: '100%', boxSizing: 'border-box', padding: '16px', fontSize: '20px',
          border: '3px solid #333', borderRadius: '8px', marginBottom: '14px', background: '#fffef5',
        }}
      />
    </>
  );
}

// ==================================================================
// MAIN APP
// ==================================================================
export default function App() {
  // Screens: signin | medical | fit-confirmed | not-permitted | site-code-entry | signout
  const [screen, setScreen] = useState('signin');
  const [name, setName] = useState('');
  const [org, setOrg] = useState('');
  const [signedInList, setSignedInList] = useState([]);
  const [signedOutList, setSignedOutList] = useState([]);
  const [deniedList, setDeniedList] = useState([]);
  const [currentPerson, setCurrentPerson] = useState(null); // the person going through the flow
  const [additionalInfo, setAdditionalInfo] = useState('');
  const [siteCode, setSiteCode] = useState('');
  const [confirmation, setConfirmation] = useState(null);
  const [toast, setToast] = useState(null);
  const [loading, setLoading] = useState(false);
  const sigPadRef = useRef(null);
  const nameInputRef = useRef(null);
  const pollRef = useRef(null);

  const refresh = useCallback(async () => {
    try {
      const data = await getRegister();
      setSignedInList(data.signedIn || []);
      setSignedOutList(data.signedOut || []);
      setDeniedList(data.denied || []);
    } catch (err) {
      console.error('Refresh failed:', err);
    }
  }, []);

  useEffect(() => {
    refresh();
    pollRef.current = setInterval(refresh, 30000);
    return () => clearInterval(pollRef.current);
  }, [refresh]);

  const showToast = (msg, duration = 4000) => {
    setToast(msg);
    setTimeout(() => setToast(null), duration);
  };

  // Reset flow state for next user
  const resetFlow = () => {
    setCurrentPerson(null);
    setAdditionalInfo('');
    setSiteCode('');
    setName('');
    setOrg('');
    if (sigPadRef.current) sigPadRef.current.clear();
    setScreen('signin');
    if (nameInputRef.current) nameInputRef.current.focus();
  };

  // ---- STEP 1: Sign In (name, org, signature) ----
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
      setCurrentPerson({
        id: result.id,
        name: name.trim(),
        org: org.trim(),
        dateIn: result.dateIn || todayStr(),
        timeIn: result.timeIn || timeStr(),
      });
      setScreen('medical');
    } catch (err) {
      showToast('Sign-in failed. Please try again.');
      console.error(err);
    }
    setLoading(false);
  };

  // ---- STEP 2a: Confirm FIT ----
  const handleConfirmFit = async () => {
    setLoading(true);
    try {
      await confirmFit({
        id: currentPerson.id,
        additionalInfo: additionalInfo.trim(),
        siteCode: siteCode.trim(),
      });
      setConfirmation(currentPerson);
      await refresh();
      setScreen('fit-confirmed');
    } catch (err) {
      showToast('Failed to confirm. Please try again.');
      console.error(err);
    }
    setLoading(false);
  };

  // ---- STEP 2b: Declare NOT FIT ----
  const handleNotFit = async () => {
    setLoading(true);
    try {
      await declareNotFit({
        id: currentPerson.id,
        additionalInfo: additionalInfo.trim(),
      });
      await refresh();
      setScreen('not-permitted');
    } catch (err) {
      showToast('Failed to process. Please try again.');
      console.error(err);
    }
    setLoading(false);
  };

  // ---- STEP 3: Submit site code (after Phil conversation) ----
  const handleSubmitSiteCode = async () => {
    if (!siteCode.trim()) { showToast('Please enter the Site Code given to you by Phil Sash.'); return; }
    setLoading(true);
    try {
      const result = await submitSiteCode({
        id: currentPerson.id,
        siteCode: siteCode.trim(),
        additionalInfo: additionalInfo.trim(),
      });
      if (result.ok) {
        setConfirmation(currentPerson);
        await refresh();
        setScreen('fit-confirmed');
      } else {
        showToast(result.error || 'Invalid site code. Please check with Phil Sash.');
      }
    } catch (err) {
      showToast('Invalid site code or server error. Please check with Phil Sash.');
      console.error(err);
    }
    setLoading(false);
  };

  // ---- Sign Out ----
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
    }
  };

  const handleEmail = async () => {
    try {
      await sendDailyEmail();
      showToast('Daily email sent to Phil Sash.');
    } catch (err) {
      showToast('Email send failed.');
    }
  };

  // Shared header
  const Header = ({ bg, borderColor, title, subtitle, badge }) => (
    <div style={{ background: bg, borderBottom: `6px solid ${borderColor}`, padding: '16px 20px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexShrink: 0 }}>
      <div>
        <h1 style={{ color: C.white, fontSize: '24px', fontWeight: 800, margin: 0, letterSpacing: '1px', textTransform: 'uppercase' }}>{title}</h1>
        {subtitle && <p style={{ color: 'rgba(255,255,255,0.7)', margin: '4px 0 0', fontSize: '14px' }}>{subtitle}</p>}
      </div>
      {badge}
    </div>
  );

  // Toast bar
  const ToastBar = () => toast ? (
    <div style={{ background: '#ff9800', color: C.white, padding: '14px 20px', fontSize: '18px', fontWeight: 600, textAlign: 'center', flexShrink: 0 }}>
      {toast}
    </div>
  ) : null;

  // ================================================================
  // SCREEN: SIGN IN
  // ================================================================
  if (screen === 'signin') {
    return (
      <div style={{ fontFamily: FONT, minHeight: '100vh', background: C.light, display: 'flex', flexDirection: 'column' }}>
        <Header
          bg={C.black}
          borderColor={C.yellow}
          title={"\u26A0 Bridge 42 \u2014 Site Sign-In"}
          subtitle={todayStr()}
          badge={
            <div style={{ background: C.yellow, color: C.black, fontWeight: 800, fontSize: '14px', padding: '8px 16px', borderRadius: '6px' }}>
              {signedInList.length} ON SITE
            </div>
          }
        />
        <ToastBar />

        <div style={{ padding: '16px 20px', flex: 1, overflowY: 'auto', WebkitOverflowScrolling: 'touch' }}>
          {/* SAFETY BRIEFING */}
          <Card>
            <SectionHeading>\uD83D\uDCCB Site Safety Briefing \u2014 Bridge 42, Grosmont</SectionHeading>
            {SAFETY_SECTIONS.map((sec, i) => (
              <div key={i} style={{ marginBottom: '16px' }}>
                <h3 style={{ fontSize: '16px', fontWeight: 700, color: C.black, margin: '0 0 6px', textTransform: 'uppercase' }}>{sec.title}</h3>
                <pre style={{ fontFamily: FONT, fontSize: '14px', lineHeight: 1.5, color: C.dark, whiteSpace: 'pre-wrap', margin: 0 }}>{sec.content}</pre>
              </div>
            ))}
            <div style={{ background: '#FFF8E1', border: `2px solid ${C.yellow}`, borderRadius: '8px', padding: '14px', marginTop: '12px' }}>
              <p style={{ fontSize: '14px', lineHeight: 1.6, color: C.dark, margin: 0, fontWeight: 600 }}>{DECLARATION_TEXT}</p>
            </div>
            <div style={{ background: '#E8F5E9', border: `2px solid ${C.green}`, borderRadius: '8px', padding: '14px', marginTop: '12px' }}>
              <p style={{ fontSize: '14px', lineHeight: 1.6, color: C.dark, margin: 0, fontStyle: 'italic' }}>{CLOSING_QUOTE}</p>
            </div>
          </Card>

          {/* SIGN-IN FORM */}
          <Card>
            <SectionHeading>\u270D Sign In Below</SectionHeading>
            <InputField value={name} onChange={setName} placeholder="e.g. John Smith" label="YOUR NAME" />
            <InputField value={org} onChange={setOrg} placeholder="e.g. NYMR, CML, ABC Contractors" label="COMPANY / ORGANISATION" />
            <label style={{ display: 'block', fontSize: '16px', fontWeight: 700, color: C.dark, marginBottom: '6px' }}>YOUR SIGNATURE</label>
            <SignaturePad ref={sigPadRef} />
            <button
              onClick={() => sigPadRef.current && sigPadRef.current.clear()}
              style={{ marginTop: '6px', background: 'none', border: 'none', color: C.blue, fontSize: '14px', fontWeight: 600, cursor: 'pointer', padding: '4px 0' }}
            >
              Clear Signature
            </button>
            <div style={{ marginTop: '16px' }}>
              <BigButton onClick={handleSignIn} bg={C.yellow} activeBg={C.yellowDark} disabled={loading} style={{ color: C.black, border: `3px solid ${C.black}` }}>
                {loading ? 'PROCESSING...' : '\u2713 I HAVE READ THE BRIEFING \u2014 SIGN IN'}
              </BigButton>
            </div>
          </Card>
        </div>

        {/* Sign-out footer */}
        <div style={{ background: C.red, borderTop: `6px solid ${C.redDark}`, padding: '16px 20px', textAlign: 'center', flexShrink: 0 }}>
          <p style={{ color: 'rgba(255,255,255,0.85)', fontSize: '15px', margin: '0 0 10px', fontWeight: 600 }}>
            LEAVING THE SITE? TAP BELOW TO SIGN OUT
          </p>
          <button
            onClick={() => { setScreen('signout'); refresh(); }}
            style={{ padding: '18px 40px', fontSize: '22px', fontWeight: 800, textTransform: 'uppercase', letterSpacing: '1px', color: C.white, background: C.redDark, border: '3px solid rgba(255,255,255,0.5)', borderRadius: '10px', cursor: 'pointer' }}
          >
            \uD83D\uDEAA GO TO SIGN OUT \u2192
          </button>
        </div>
      </div>
    );
  }

  // ================================================================
  // SCREEN: MEDICAL DECLARATION
  // ================================================================
  if (screen === 'medical') {
    return (
      <div style={{ fontFamily: FONT, minHeight: '100vh', background: C.light, display: 'flex', flexDirection: 'column' }}>
        <Header
          bg={C.blue}
          borderColor={C.blueDark}
          title={"\uD83C\uDFE5 Medical Self-Declaration"}
          subtitle={`${currentPerson?.name} \u2014 ${currentPerson?.org}`}
        />

        <div style={{ padding: '16px 20px', flex: 1, overflowY: 'auto', WebkitOverflowScrolling: 'touch' }}>
          <Card border={C.blue}>
            <SectionHeading accent={C.blue}>Daily Medical Self-Declaration</SectionHeading>
            <p style={{ fontSize: '16px', color: C.dark, lineHeight: 1.6, margin: '0 0 16px', fontWeight: 600 }}>
              Please read each statement carefully. By tapping "YES, I CONFIRM" you are declaring that ALL of the following apply to you today:
            </p>

            {MEDICAL_SECTIONS.map((sec, i) => (
              <div key={i} style={{ marginBottom: '20px' }}>
                <h3 style={{ fontSize: '16px', fontWeight: 700, color: C.black, margin: '0 0 8px', textTransform: 'uppercase' }}>{sec.title}</h3>
                {sec.intro && <p style={{ fontSize: '15px', color: C.dark, margin: '0 0 8px' }}>{sec.intro}</p>}
                {sec.items.map((item, j) => (
                  <div key={j} style={{ display: 'flex', alignItems: 'flex-start', gap: '10px', padding: '8px 12px', background: i === 0 ? '#E3F2FD' : '#FFF3E0', borderRadius: '6px', marginBottom: '6px' }}>
                    <span style={{ fontSize: '18px', flexShrink: 0 }}>{i === 0 ? '\u2705' : '\u26A0\uFE0F'}</span>
                    <span style={{ fontSize: '15px', color: C.dark, lineHeight: 1.5 }}>{item}</span>
                  </div>
                ))}
              </div>
            ))}
          </Card>

          {/* ACTION BUTTONS — big, obvious, unmissable */}
          <Card border={C.green} style={{ background: '#E8F5E9' }}>
            <p style={{ fontSize: '16px', fontWeight: 700, color: C.dark, margin: '0 0 12px', textAlign: 'center' }}>
              Can you confirm ALL of the above statements?
            </p>
            <BigButton onClick={() => setScreen('fit-form')} bg={C.green} activeBg={C.greenDark}>
              \u2705 YES, I CONFIRM \u2014 I AM FIT FOR DUTY
            </BigButton>
          </Card>

          <Card border={C.red} style={{ background: '#FFEBEE' }}>
            <BigButton onClick={() => { setAdditionalInfo(''); handleNotFit(); }} bg={C.red} activeBg={C.redDark} disabled={loading}>
              {loading ? 'PROCESSING...' : '\u274C NO, I CANNOT CONFIRM'}
            </BigButton>
            <p style={{ fontSize: '14px', color: C.mid, textAlign: 'center', margin: '10px 0 0' }}>
              You will be asked to contact the Site Controller before entry is permitted.
            </p>
          </Card>

          <div style={{ textAlign: 'center', padding: '10px 0 20px' }}>
            <button onClick={resetFlow} style={{ background: 'none', border: 'none', color: C.mid, fontSize: '14px', cursor: 'pointer', textDecoration: 'underline' }}>
              Cancel and return to sign-in
            </button>
          </div>
        </div>
      </div>
    );
  }

  // ================================================================
  // SCREEN: FIT FORM — additional info + optional site code
  // ================================================================
  if (screen === 'fit-form') {
    return (
      <div style={{ fontFamily: FONT, minHeight: '100vh', background: C.light, display: 'flex', flexDirection: 'column' }}>
        <Header
          bg={C.green}
          borderColor={C.greenDark}
          title={"\u2705 Medically Fit \u2014 Confirmed"}
          subtitle={`${currentPerson?.name} \u2014 ${currentPerson?.org}`}
        />

        <div style={{ padding: '16px 20px', flex: 1, overflowY: 'auto', WebkitOverflowScrolling: 'touch' }}>
          <Card border={C.green}>
            <SectionHeading accent={C.green}>Almost Done</SectionHeading>
            <p style={{ fontSize: '16px', color: C.dark, margin: '0 0 16px', lineHeight: 1.5 }}>
              You have confirmed you are fit for duty. Please complete the fields below and tap <strong>PROCEED TO SITE</strong> to finalise your sign-in.
            </p>

            <label style={{ display: 'block', fontSize: '16px', fontWeight: 700, color: C.dark, marginBottom: '6px' }}>
              PLEASE ADD ANY ADDITIONAL INFORMATION
            </label>
            <p style={{ fontSize: '13px', color: C.mid, margin: '0 0 8px' }}>
              Optional \u2014 e.g. specific tasks today, vehicle reg, expected duration on site
            </p>
            <TextArea value={additionalInfo} onChange={setAdditionalInfo} placeholder="Enter any additional information here (optional)" />

            <div style={{ marginTop: '16px' }}>
              <label style={{ display: 'block', fontSize: '16px', fontWeight: 700, color: C.dark, marginBottom: '6px' }}>
                ONE TIME USE SITE CODE
              </label>
              <p style={{ fontSize: '13px', color: C.mid, margin: '0 0 8px' }}>
                If you have been given a one-time site code by the Site Controller, enter it here. Otherwise leave blank.
              </p>
              <input
                type="text"
                value={siteCode}
                onChange={(e) => setSiteCode(e.target.value.toUpperCase())}
                placeholder="e.g. B42-A7X3"
                autoComplete="off"
                style={{
                  width: '100%', boxSizing: 'border-box', padding: '16px', fontSize: '22px',
                  border: '3px solid #333', borderRadius: '8px', background: '#fffef5',
                  fontFamily: 'monospace', letterSpacing: '3px', textAlign: 'center',
                }}
              />
            </div>

            <div style={{ marginTop: '20px' }}>
              <BigButton onClick={handleConfirmFit} bg={C.green} activeBg={C.greenDark} disabled={loading}>
                {loading ? 'PROCESSING...' : '\u2705 PROCEED TO SITE'}
              </BigButton>
            </div>
          </Card>

          <div style={{ textAlign: 'center', padding: '10px 0 20px' }}>
            <button onClick={() => setScreen('medical')} style={{ background: 'none', border: 'none', color: C.mid, fontSize: '14px', cursor: 'pointer', textDecoration: 'underline' }}>
              \u2190 Back to medical declaration
            </button>
          </div>
        </div>
      </div>
    );
  }

  // ================================================================
  // SCREEN: FIT CONFIRMED — success, you may proceed
  // ================================================================
  if (screen === 'fit-confirmed') {
    return (
      <div style={{ fontFamily: FONT, minHeight: '100vh', background: C.light, display: 'flex', flexDirection: 'column' }}>
        <Header bg={C.green} borderColor={C.greenDark} title={"\u2705 Authorised \u2014 You May Enter Site"} />

        <div style={{ padding: '20px', flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center' }}>
          <Card border={C.green} style={{ maxWidth: '600px', width: '100%', textAlign: 'center', background: '#E8F5E9' }}>
            <div style={{ fontSize: '80px', marginBottom: '10px' }}>\u2705</div>
            <h2 style={{ fontSize: '28px', fontWeight: 800, color: C.green, margin: '0 0 10px' }}>
              WELCOME TO SITE
            </h2>
            <p style={{ fontSize: '22px', fontWeight: 700, color: C.black, margin: '0 0 6px' }}>
              {confirmation?.name}
            </p>
            <p style={{ fontSize: '16px', color: C.mid, margin: '0 0 6px' }}>
              {confirmation?.org}
            </p>
            <p style={{ fontSize: '18px', color: C.dark, margin: '0 0 20px' }}>
              Signed in at <strong>{confirmation?.timeIn}</strong> on <strong>{confirmation?.dateIn}</strong>
            </p>
            <div style={{ background: C.white, border: `2px solid ${C.green}`, borderRadius: '8px', padding: '14px', marginBottom: '16px' }}>
              <p style={{ fontSize: '14px', color: C.dark, margin: 0, fontWeight: 600 }}>
                You are recorded as ON SITE and medically fit for duty. Remember to SIGN OUT when you leave.
              </p>
            </div>
            <BigButton onClick={resetFlow} bg={C.green} activeBg={C.greenDark} style={{ maxWidth: '400px', margin: '0 auto' }}>
              DONE \u2014 NEXT PERSON
            </BigButton>
          </Card>
        </div>
      </div>
    );
  }

  // ================================================================
  // SCREEN: NOT PERMITTED — contact Phil Sash
  // ================================================================
  if (screen === 'not-permitted') {
    return (
      <div style={{ fontFamily: FONT, minHeight: '100vh', background: C.light, display: 'flex', flexDirection: 'column' }}>
        <Header bg={C.orange} borderColor={C.red} title={"\u26D4 Entry Not Permitted"} subtitle={`${currentPerson?.name} \u2014 ${currentPerson?.org}`} />

        <div style={{ padding: '16px 20px', flex: 1, overflowY: 'auto', WebkitOverflowScrolling: 'touch' }}>
          {/* Alert */}
          <Card border={C.red} style={{ background: '#FFEBEE' }}>
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: '60px', marginBottom: '8px' }}>\u26D4</div>
              <h2 style={{ fontSize: '22px', fontWeight: 800, color: C.red, margin: '0 0 12px' }}>
                YOU ARE NOT AUTHORISED TO ENTER SITE
              </h2>
              <p style={{ fontSize: '16px', color: C.dark, lineHeight: 1.6, margin: '0 0 4px' }}>
                Because you could not confirm the medical self-declaration, you must speak with the <strong>Site Controller</strong> before entry can be considered.
              </p>
            </div>
          </Card>

          {/* Contact Phil */}
          <Card border={C.blue}>
            <SectionHeading accent={C.blue}>\uD83D\uDCDE Contact Phil Sash \u2014 Site Controller</SectionHeading>
            <p style={{ fontSize: '16px', color: C.dark, margin: '0 0 16px', lineHeight: 1.5 }}>
              Please contact Phil using one of the methods below. He will discuss your situation and may issue you a <strong>One Time Use Site Code</strong> if he authorises your entry.
            </p>

            <a href="tel:07483990436" style={{ textDecoration: 'none', display: 'block', marginBottom: '10px' }}>
              <div style={{
                background: C.green, color: C.white, padding: '18px 20px', borderRadius: '10px',
                fontSize: '22px', fontWeight: 800, textAlign: 'center',
              }}>
                \uD83D\uDCDE CALL PHIL: 07483 990 436
              </div>
            </a>

            <a href="mailto:phil.sash@nymr.co.uk" style={{ textDecoration: 'none', display: 'block' }}>
              <div style={{
                background: C.blue, color: C.white, padding: '18px 20px', borderRadius: '10px',
                fontSize: '18px', fontWeight: 700, textAlign: 'center',
              }}>
                \u2709 EMAIL: phil.sash@nymr.co.uk
              </div>
            </a>

            <div style={{ background: '#FFF8E1', border: `2px solid ${C.yellow}`, borderRadius: '8px', padding: '14px', marginTop: '16px' }}>
              <p style={{ fontSize: '14px', color: C.dark, margin: 0 }}>
                \u2139\uFE0F An email has been automatically sent to Phil with your details and a one-time site code. If Phil authorises your entry, he will give you the code to enter below.
              </p>
            </div>
          </Card>

          {/* Additional Info */}
          <Card>
            <label style={{ display: 'block', fontSize: '16px', fontWeight: 700, color: C.dark, marginBottom: '6px' }}>
              PLEASE ADD ANY ADDITIONAL INFORMATION
            </label>
            <p style={{ fontSize: '13px', color: C.mid, margin: '0 0 8px' }}>
              Describe your condition or circumstances so Phil can make an informed decision.
            </p>
            <TextArea value={additionalInfo} onChange={setAdditionalInfo} placeholder="e.g. I take daily blood pressure medication but it does not impair my work" rows={4} />
          </Card>

          {/* One-Time Site Code Entry */}
          <Card border={C.green} style={{ background: '#E8F5E9' }}>
            <SectionHeading accent={C.green}>\uD83D\uDD10 Enter One Time Use Site Code</SectionHeading>
            <p style={{ fontSize: '15px', color: C.dark, margin: '0 0 12px', lineHeight: 1.5 }}>
              If Phil has given you authorisation, enter the <strong>One Time Use Site Code</strong> below:
            </p>
            <input
              type="text"
              value={siteCode}
              onChange={(e) => setSiteCode(e.target.value.toUpperCase())}
              placeholder="ENTER CODE HERE"
              autoComplete="off"
              style={{
                width: '100%', boxSizing: 'border-box', padding: '18px', fontSize: '28px',
                border: '3px solid ' + C.green, borderRadius: '8px', background: C.white,
                fontFamily: 'monospace', letterSpacing: '4px', textAlign: 'center',
                fontWeight: 800,
              }}
            />
            <div style={{ marginTop: '16px' }}>
              <BigButton onClick={handleSubmitSiteCode} bg={C.green} activeBg={C.greenDark} disabled={loading}>
                {loading ? 'VERIFYING...' : '\uD83D\uDD13 SUBMIT CODE \u2014 REQUEST ENTRY'}
              </BigButton>
            </div>
          </Card>

          <div style={{ textAlign: 'center', padding: '10px 0 20px' }}>
            <button onClick={resetFlow} style={{ background: 'none', border: 'none', color: C.mid, fontSize: '14px', cursor: 'pointer', textDecoration: 'underline' }}>
              Cancel and return to sign-in
            </button>
          </div>
        </div>
      </div>
    );
  }

  // ================================================================
  // SCREEN: SIGN OUT
  // ================================================================
  return (
    <div style={{ fontFamily: FONT, minHeight: '100vh', background: C.light, display: 'flex', flexDirection: 'column' }}>
      <Header
        bg={C.red}
        borderColor={C.redDark}
        title={"\uD83D\uDEAA Site Sign-Out"}
        subtitle={`${todayStr()} \u2014 Tap your name to sign out`}
        badge={
          <button
            onClick={() => setScreen('signin')}
            style={{ background: C.white, color: C.red, fontWeight: 800, fontSize: '14px', padding: '10px 18px', borderRadius: '8px', border: 'none', cursor: 'pointer', textTransform: 'uppercase' }}
          >
            \u2190 BACK TO SIGN IN
          </button>
        }
      />
      <ToastBar />

      <div style={{ padding: '16px 20px', flex: 1, overflowY: 'auto', WebkitOverflowScrolling: 'touch' }}>
        {/* Currently On Site */}
        <Card>
          <SectionHeading>\uD83D\uDC77 Currently On Site ({signedInList.length})</SectionHeading>
          {signedInList.length === 0 ? (
            <p style={{ color: C.mid, fontSize: '18px', textAlign: 'center', padding: '24px 0', fontStyle: 'italic' }}>
              Nobody is currently signed in.
            </p>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
              {signedInList.map((person) => (
                <div key={person.id} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', background: '#f8f8f5', border: '2px solid #ddd', borderRadius: '8px', padding: '14px 16px' }}>
                  <div style={{ flex: 1 }}>
                    <div style={{ fontSize: '20px', fontWeight: 700, color: C.black }}>{person.name}</div>
                    <div style={{ fontSize: '14px', color: C.mid, marginTop: '2px' }}>
                      {person.org} \u2014 In since {person.timeIn}
                      {person.medicalStatus && (
                        <span style={{
                          marginLeft: '8px', padding: '2px 8px', borderRadius: '4px', fontSize: '12px', fontWeight: 700,
                          background: person.medicalStatus === 'fit' ? '#C8E6C9' : '#FFF9C4',
                          color: person.medicalStatus === 'fit' ? C.green : C.orange,
                        }}>
                          {person.medicalStatus === 'fit' ? 'FIT' : 'CONDITIONAL'}
                        </span>
                      )}
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
        </Card>

        {/* Signed Out Today */}
        <Card border="#ccc">
          <h2 style={{ margin: '0 0 16px', fontSize: '18px', fontWeight: 700, textTransform: 'uppercase', color: C.mid, borderBottom: '2px solid #ddd', paddingBottom: '8px' }}>
            \u2713 Signed Out Today ({signedOutList.length})
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
                    {person.org} \u2014 In: {person.timeIn} \u2192 Out: {person.timeOut}
                  </span>
                </div>
              ))}
            </div>
          )}
        </Card>

        {/* Denied Entry Today */}
        {deniedList.length > 0 && (
          <Card border={C.orange}>
            <h2 style={{ margin: '0 0 16px', fontSize: '18px', fontWeight: 700, textTransform: 'uppercase', color: C.orange, borderBottom: `2px solid ${C.orange}`, paddingBottom: '8px' }}>
              \u26D4 Denied Entry Today ({deniedList.length})
            </h2>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
              {deniedList.map((person, i) => (
                <div key={i} style={{ padding: '10px 14px', borderBottom: '1px solid #eee', fontSize: '16px' }}>
                  <span style={{ fontWeight: 600, color: C.dark }}>{person.name}</span>
                  <span style={{ color: C.mid, fontSize: '14px', marginLeft: '8px' }}>
                    {person.org} \u2014 {person.timeIn}
                  </span>
                </div>
              ))}
            </div>
          </Card>
        )}

        {/* Admin — CSV + Email */}
        <Card border="#ccc" style={{ textAlign: 'center' }}>
          <p style={{ color: C.mid, fontSize: '14px', margin: '0 0 12px' }}>
            End of day \u2014 download or email site register to bridge42@nymr.co.uk
          </p>
          <div style={{ display: 'flex', gap: '12px', justifyContent: 'center', flexWrap: 'wrap' }}>
            <button onClick={handleExport} style={{ padding: '14px 24px', fontSize: '16px', fontWeight: 700, color: C.white, background: C.blue, border: 'none', borderRadius: '8px', cursor: 'pointer', flex: '1 1 auto', minWidth: '180px' }}>
              \uD83D\uDCE5 DOWNLOAD CSV
            </button>
            <button onClick={handleEmail} style={{ padding: '14px 24px', fontSize: '16px', fontWeight: 700, color: C.white, background: C.green, border: 'none', borderRadius: '8px', cursor: 'pointer', flex: '1 1 auto', minWidth: '180px' }}>
              \uD83D\uDCE7 EMAIL REPORT TO BRIDGE 42
            </button>
          </div>
        </Card>
      </div>
    </div>
  );
}

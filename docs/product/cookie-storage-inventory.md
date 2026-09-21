# Cookie and storage inventory

**Status:** Shipped (engineering evidence)  
**Last reviewed:** 2026-09-21  
**Authority:** Tummly Launch Reconciliation — Cookies/accessibility P0  
**Related:** [Cookie Policy](./marketing-site.md#cookie-consent), [analytics.md](./analytics.md), [security-and-rbac.md](./security-and-rbac.md)

## Scope

First-party keys set by the Tummly web app, plus Google Analytics cookies when analytics consent is granted. Operator-only prefs are listed for completeness; they are out of the public a11y QA surface.

## First-party storage

| Key / name | Storage | Purpose | Category | Set by | Consent | Cleared when |
|------------|---------|---------|----------|--------|---------|--------------|
| `tummly-cookie-consent` | localStorage | Analytics preference (`analytics`, `updatedAt`) | Essential (preference) | `cookieConsentStore` (`COOKIE_CONSENT_KEY`) | Always on (stores choice) | Never automatic; user can change via Cookie settings |
| `tummly-auth` | localStorage | JWT session (Zustand persist: token, refreshToken, role, accountType) | Essential | `authStore` (`AUTH_PERSIST_KEY`) | Always on | Sign-out / clear session |
| `token` / `role` | localStorage | Legacy auth keys (migrated then removed) | Essential (legacy) | `authStore` migrate (`LEGACY_TOKEN_KEY` / `LEGACY_ROLE_KEY`) | Always on | Removed after migrate / sign-out |
| `deviceToken` | localStorage | Trusted device for Sign-in OTP skip | Essential | `authHelpers` (`DEVICE_TOKEN_KEY`) | Always on | Not cleared on sign-out (30-day trust) |
| `selectedLocationId` | localStorage | Last chosen workspace location | Essential | `authHelpers` (`SELECTED_LOCATION_KEY`); also `sessionRouting` / login flows via `persistSelectedLocation` | Always on | Not cleared on sign-out; overwritten when a new location is persisted |
| `tummly.signupSession` | sessionStorage | Self-service Pilot signup token | Essential | `signupSession.ts` (`SIGNUP_SESSION_KEY`) | Always on | Browser session end / `clearSignupSessionToken` |

## Operator-only (out of public a11y surface)

| Key / name | Storage | Purpose | Category | Set by | Consent | Cleared when |
|------------|---------|---------|----------|--------|---------|--------------|
| `tummly-theme` | localStorage | Operator appearance (light/dark/system) | Essential (UI pref) | `operatorAppearance.ts` (`OPERATOR_APPEARANCE_STORAGE_KEY`) via next-themes | Always on | User changes theme |
| `tummly-operator-sidebar-collapsed` | localStorage | Sidebar collapsed pin | Essential (UI pref) | `sidebarCollapsed.ts` (`OPERATOR_SIDEBAR_COLLAPSED_KEY`) | Always on | User toggles |
| `tummly-operator-sidebar-settings-expanded` | localStorage | Settings nav expanded | Essential (UI pref) | `sidebarSettingsExpanded.ts` (`OPERATOR_SIDEBAR_SETTINGS_EXPANDED_KEY`) | Always on | User toggles |
| `tummly-operator-setup-checklist-open` | localStorage | Setup checklist open state | Essential (UI pref) | `setupChecklistOpen.ts` (`OPERATOR_SETUP_CHECKLIST_OPEN_KEY`) | Always on | User toggles |
| `tummly.operator.activate-tummly-dialog.dismissed` | sessionStorage | Activate Pilot dialog dismiss | Essential (UI pref) | `activateTummlyPilotDialogGate.ts` (`ACTIVATE_TUMMLY_PILOT_DIALOG_DISMISS_KEY`) | Always on | Session end / cleared on sign-out via `clearActivateTummlyPilotDialogDismissed` |

## Third-party (conditional)

| Key / name | Storage | Purpose | Category | Set by | Consent | Cleared when |
|------------|---------|---------|----------|--------|---------|--------------|
| `_ga`, `_ga_*`, `_gid` (typical GA) | Cookie | Google Analytics measurement | Analytics (optional) | gtag.js after `setAnalyticsConsent(true)` | Requires analytics accept | Reject / disable sets `ga-disable-{MEASUREMENT_ID}`; cookies may remain until browser expiry — analytics must not run while consent is false |

## Consent gate

- Banner until `cookieConsentStore.analytics` is non-null.
- Accept all → `setAnalyticsConsent(true)` → `initGoogleAnalytics`.
- Reject non-essential / analytics off → `disableGoogleAnalytics` (fail closed: no page views / events).
- Evidence: `src/stores/cookieConsentStore.ts`, `src/lib/analytics.ts`, `src/components/common/GoogleAnalytics.tsx`.

## Review notes

- No advertising cookies at launch.
- Inventory is engineering evidence; Legal Cookie Policy copy stays separate.
- Spot-check 2026-09-21: `initGoogleAnalytics`, `trackPageView`, and `trackEvent` all no-op when `consentGranted` is false (`src/lib/analytics.ts`). `GoogleAnalytics` only calls `trackPageView` when `analytics === true`. No Blocker; no code change.
- `activationRequired` appears as an API/session-routing **field**, not a first-party storage write. Production `src` never calls `localStorage.setItem("activationRequired", …)`; only `sessionRouting.test.ts` sets it as a negative control. Omitted from the table for that reason.
- No `document.cookie` writes found in first-party `src` TypeScript; GA cookies come from gtag.js after consent.

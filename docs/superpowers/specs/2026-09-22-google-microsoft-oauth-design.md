# Google and Microsoft OAuth (Sign-up + Sign-in)

Date: 2026-09-22  
Status: draft for review

## Goal

Wire the existing Continue with Google and Continue with Microsoft buttons on marketing Sign-in and Sign-up so operators can start Self-service Pilot or return Sign-in without a local password when they use social only.

## Decisions (locked)

| Topic | Choice |
|-------|--------|
| Scope | Sign-up **and** Sign-in |
| Password | Social Sign-up skips local password; social-only users have no `PasswordHash` |
| Sign-in OTP | Same rules as password Sign-in (First Sign-in / no trusted device → OTP; trusted device may skip) |
| Existing email (no link) | Block; clear message to use email/password (no auto-link) |
| Unknown email on Sign-in | Start Self-service Pilot Sign-up for that email |
| Terms | After provider return, Terms / Privacy confirm step before Guest Loop |
| OAuth pattern | Backend Authorization Code redirect (Approach 1) |
| Staff | Admin / Support: reject social; password-only |
| Providers | Google and Microsoft only |

## Scope

### In

- Backend OAuth start + callback for Google and Microsoft.
- One-time exchange token → session / OTP challenge / signup session.
- `UserExternalLogin` storage; nullable `User.PasswordHash` for social-only.
- PendingSignup social fields; Terms accept after OAuth; status `Verified` (skip email verification OTP/link).
- Wire `AuthSocialContinueButtons`; OAuth complete + Terms routes.
- Guest Loop account step without password when session is social.
- Tests for match rules, Terms → Verified, OTP/trust parity, blocked reuse.

### Out

- Auto-link of Google/Microsoft to an existing password account.
- Setting a password later in Settings (may follow later).
- Apple, Auth0/B2C broker, frontend GIS/MSAL token post.
- Admin / Support social Sign-in.
- Changing password Sign-in, forgot/reset, or team invite accept beyond clear copy where social-only hits reset (nice-to-have if cheap).

## Architecture

### Pattern

SPA never holds OAuth client secrets.

```text
Click Continue with Google|Microsoft
  → GET /api/auth/external/{provider}/start?...
  → Provider consent
  → GET /api/auth/external/{provider}/callback
  → Backend decides outcome
  → Redirect to frontend with one-time exchange token
       · Existing operator → /login/oauth/complete
       · New email → /signup/oauth/terms
       · Conflict → /login or /signup with oauthError=account_exists
  → POST /api/auth/external/exchange  (Sign-in)
  → or POST /api/auth/external/accept-terms then Guest Loop (Sign-up)
```

### Matching rules (callback, order)

1. Find `UserExternalLogin` by `(Provider, ProviderSubject)` → Sign-in path.
2. Else if email matches an existing `User` → block (`account_exists`). No auto-link.
3. Else if email matches Admin or Support → reject social (password-only).
4. Else if email matches a **Complete** PendingSignup → block (`account_exists`).
5. Else → Sign-up path (Terms → create or resume incomplete PendingSignup for that email; set social fields; after Terms → `Verified`).

Trust only provider emails that are present and marked verified. Reject otherwise.

When Guest Loop provisioning creates the `User` from a social PendingSignup, insert `UserExternalLogin` from `AuthProvider` + `ProviderSubject` in the same transaction as account create.

### Data model

**`UserExternalLogin` (new)**

| Column | Notes |
|--------|--------|
| `Id` | PK |
| `UserId` | FK → Users |
| `Provider` | `Google` \| `Microsoft` |
| `ProviderSubject` | Stable subject from provider |
| `CreatedAtUtc` | |

Unique index on `(Provider, ProviderSubject)`. Unique or filter so one provider link per user per provider as needed.

**`User`**

- `PasswordHash` becomes nullable. Password Sign-in and reset require a hash; social-only users have null.

**`PendingSignup`**

| Column | Notes |
|--------|--------|
| `AuthProvider` | nullable; `Google` \| `Microsoft` when social |
| `ProviderSubject` | nullable; set for social |
| `PasswordHash` | remains null for social |
| `TermsAccepted` | set true on accept-terms |
| `Status` | social start lands at `Verified` after Terms (skip `EmailPending` OTP) |
| `EmailVerifiedAt` | set when Terms accepted after social |

Short-lived store for OAuth `state` and exchange tokens (DB table preferred for multi-instance): TTL ~5–10 minutes, single use.

### Backend surface

| Endpoint | Purpose |
|----------|---------|
| `GET /api/auth/external/{provider}/start` | Build `state`, redirect to provider |
| `GET /api/auth/external/{provider}/callback` | Exchange code; redirect to frontend with outcome + one-time token |
| `POST /api/auth/external/exchange` | Consume one-time token; return universal-login-shaped payload (JWT **or** OTP challenge) including `deviceToken` / `rememberDevice` |
| `POST /api/auth/external/accept-terms` | Consume signup one-time token + Terms flag; create/update PendingSignup `Verified`; return signup `sessionToken` |

Config (env / appsettings): Google client id/secret, Microsoft client id/secret, redirect URIs, frontend public base URL.

Service: `ExternalAuthService` (or equivalent) owns start, callback, exchange, accept-terms; reuses existing OTP / trusted-device / activation routing from AuthService where Sign-in succeeds.

### Frontend

- `AuthSocialContinueButtons`: navigate to start URL; remove Coming soon / no-op stubs.
- `/login/oauth/complete`: exchange token; feed existing LoginPage step machine (OTP, activation, workspace).
- `/signup/oauth/terms`: Terms + Privacy checkbox + Continue; accept-terms; save signup session; enter Guest Loop (skip verify email).
- Guest Loop account step: social session → hide password / confirm password; prefill name/email when available; require first/last name if missing.

## User journeys

### Returning operator (linked)

Sign-in or Sign-up button → provider → exchange → same post-auth steps as password Sign-in (OTP / trust / activation / workspace).

### New operator

Either page → provider → Terms confirm → PendingSignup `Verified` → Guest Loop without password → plan → provision → later Sign-in via same provider (OTP rules apply).

### Conflict

Provider email already has a Tummly password account (or other complete account) and is not linked to this provider → block with: account exists; sign in with email and password.

## Errors

| Case | Behaviour |
|------|-----------|
| User cancels at provider | Soft return to Sign-in or Sign-up; no writes |
| Invalid / expired state or exchange token | Generic failure; try again |
| Email exists, not linked | `account_exists` copy |
| Locked / not approved / activation expired | Same as password Sign-in |
| Staff email via social | Reject; use password Sign-in |
| Terms not checked | Block Continue on Terms step |

## Security

- Validate `state`; one-time exchange tokens; HTTPS-only redirect URIs.
- Reject missing or unverified provider email.
- No JWT as a long-lived query param; use short-lived exchange token then normal session storage.
- Client secrets server-side only.

## Testing

- Unit: match rules; Terms → Verified + null password; OTP/trust parity; blocked reuse; staff reject.
- Integration: start → mocked callback → exchange / accept-terms; expired token.
- Frontend: buttons hit start; Terms gate; Guest Loop hides password for social; login complete reuses OTP steps.

## Config / ops (implementers)

- Register redirect URIs in Google Cloud and Microsoft Entra app registrations for each environment.
- Document env var names in `.env.example` (no secrets committed).

## Relation to prior docs

- Replaces “UI only / coming soon” for social in self-serve signup and Sign-in product docs once shipped.
- Does not remove email+password path.

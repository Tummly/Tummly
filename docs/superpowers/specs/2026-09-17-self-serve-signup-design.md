# Self-serve Signup → Guest Loop → Choose plan

Date: 2026-09-17  
Status: draft for review

> **OAuth:** Google / Microsoft OAuth in this doc is superseded by [2026-09-22-google-microsoft-oauth-design.md](./2026-09-22-google-microsoft-oauth-design.md).

## Goal

Replace the retired Request Trial path with one self-serve operator Signup flow. The user verifies email (OTP), completes Guest Loop onboarding (single or multi by location count), chooses a plan, then gets a real account only after Essential confirm or Pro Revolut payment success. After provisioning animations, the user signs in.

## Figma references

File: [Tummly — Marketing Website](https://www.figma.com/design/UP1DqyGGGrxx80Dp7jReTT/Tummly---Marketing-Website)

| Screen | Node |
|---|---|
| Signup | `4974:20563` |
| Verify your email (inside signup) | `4974:21543` |
| Guest Loop — create password | `4974:22427` |
| Guest Loop — restaurant | `4974:22970` |
| Guest Loop — ready | `4974:23777` |
| Choose plan | `4974:23046` |
| Feature lists dialog | `4974:23571` |

## Decisions

- Product path: replace Request Trial (not a parallel trial path).
- Approach: **pending signup session** — create the operator account only after Essential confirm or Pro webhook success.
- Essential (free) → create + provision at once. Pro (paid) → Revolut checkout (same pattern as Settings → Manage Plan) → webhook → create + provision → Sign in.
- Full stack in this work.
- Google / Microsoft buttons: UI only (“coming soon”); email path is live.
- Verify: **6-digit OTP on the page** (Figma copy says “link”; layout stays; OTP is required).
- Signup + Verify chrome: **centered modal** over blurred Guest Loop / dashboard backdrop (not AuthShell).
- Retire invite setup routes `/register/single` and `/register/multi`.
- Keep Sign-in, forgot/reset password, and team invite accept.
- Plan map for primary cards: **Essential → Pilot**, **Pro → Growth**. Feature dialog uses full Manage Plan catalog (Pilot / Starter / Growth / Group).

## Scope

### In

- Public Signup + Verify modals and APIs (pending signup + OTP).
- Guest Loop onboarding restyle and wiring to pending signup (single if 1 location, multi if >1).
- Choose plan UI + feature dialog from Manage Plan comparison / catalog.
- Essential create path and Pro Revolut + webhook create path.
- Provisioning UI (reuse Guest Loop phase animations) then route to Sign in.
- Retire Request Trial UI/handoff and invite operator setup routes.
- Home / marketing CTAs point to Signup.

### Out

- Real Google / Microsoft OAuth.
- Keeping trial admin review → invite-token operator setup as a create path.
- Changes to signed-in Manage Plan for existing accounts beyond reuse of checkout / comparison patterns.
- AuthShell restyle of Signup/Verify (modal chrome instead).

## User journey

```text
Home / marketing CTA
  → /signup  (modal: email + terms + Create account; social UI-only)
  → /signup/verify  (modal: OTP + resend + different email)
  → Guest Loop shell
       1. Create password (name, password, terms)
       2. Restaurant / group details
            · location count = 1  → single path
            · location count > 1 → multi path (group + locations)
       3. Ready teaser (pre-plan; no real account yet)
  → Choose plan
       · Essential → confirm → create account + provisioning → /login
       · Pro → Revolut → webhook confirms → create + provisioning → /login
  → Sign in with the new account
```

## Architecture

### PendingSignup

Server record for the journey until account create:

| Area | Content |
|---|---|
| Identity | email, emailVerifiedAt, OTP state |
| Credentials | password hash (set on Guest Loop step 1; not valid for login until account exists) |
| Profile | full name, restaurant/group, locations, category, phones, links |
| Path | `Single` or `Multi` from location count |
| Plan | chosen plan id + cadence; Revolut order / payment intent for Pro |
| Status | `EmailPending` → `Verified` → `OnboardingComplete` → `AwaitingPayment` (Pro) or `Provisioning` → `Complete` / `Abandoned` |

### APIs

1. `POST /auth/signup` — email + terms → create/resume pending, send OTP.
2. `POST /auth/signup/verify-otp` and resend OTP.
3. `POST /auth/signup/onboarding` — save password + restaurant/group steps; auth via signup session token.
4. `POST /auth/signup/choose-plan`
   - Essential (Pilot) → start create; return provisioning handle.
   - Paid (Growth default for Pro card; also Starter/Group from dialog) → Revolut pay URL (Manage Plan pattern).
5. `GET /auth/signup/provisioning-status` — poll until ready.
6. Revolut webhook for signup orders → create operator account from PendingSignup (reuse provisioning core from today’s `setup-account`, without invite token).

### Frontend

- `SignupModalShell` — blurred backdrop + centered card for Signup and Verify.
- Reuse `GuestLoopShell` and step components; restyle to Figma; drive single vs multi from location count.
- Choose plan page; feature dialog reuses Manage Plan comparison / catalog presentation.
- Signup session: opaque token (memory + durable cookie or query) so refresh can resume.

### Plan mapping

| UI | ManagePlanId | Payment |
|---|---|---|
| Essential card | Pilot | None — create immediately |
| Pro card | Growth | Revolut |
| Feature dialog picks | Pilot / Starter / Growth / Group | Pilot free; others Revolut |

## Screens

### Signup (`/signup`)

Centered modal: logo, “Create your Tummly account”, work email, terms, Create account, Or, Google/Microsoft (UI only), Sign in link. Use existing form primitives and `AuthSocialContinueButtons`.

### Verify (`/signup/verify`)

Same shell. Title/body with email, **OTP entry**, Resend verification email, Use a different email → clear session and return to Signup. Success → Guest Loop step 1.

### Guest Loop

1. Create password — email read-only, full name, password, confirm, terms.
2. Restaurant (single) or group + locations (multi). Collect location count when needed to branch (add field if Figma omits it).
3. Ready teaser — Continue to Choose plan (no real provision yet).

### Choose plan

Essential / Pro cards per Figma. Default billing cadence is **monthly** (annual available in the feature dialog if Manage Plan already exposes it). “See all feature lists” opens large dialog from Manage Plan comparison. Primary card CTAs: Essential = Pilot free; Pro = Growth paid.

### Provisioning

Same phase animations as today’s Guest Loop ready step. Then navigate to `/login?setup=complete`.

### Retire

- Replace Hero Request Trial form with Signup CTA.
- `/register/single` and `/register/multi` redirect to `/signup`.

## Errors and edge cases

**Signup / verify**

- Email already has an account → field error + Sign in link.
- Pending already exists for email → resume verify / resend (no duplicate pending row).
- Bad / expired OTP → inline error; resend with cooldown / attempt limits.
- Missing session on verify → `/signup`.
- Different email → clear session, Signup.

**Onboarding**

- Invalid / abandoned pending → restart Signup.
- Step validation stays on the form (current Guest Loop patterns).
- Refresh → resume last saved step via signup token.

**Choose plan / pay**

- Essential create failure → error on Choose plan; retry allowed.
- Pro Revolut cancel / fail → Choose plan with message; status stays onboarding-complete.
- Webhook delay → provisioning UI polls; soft “confirming payment” copy; no Sign-in until create succeeds.
- Webhook success but create fails → safe error + idempotent retry create from pending.
- Double-submit → one account max per pending signup.

**After create**

- Login with password only when status is `Complete`.
- Duplicate email race at create → fail closed with Sign in / support path.

## Testing

### Automated

- PendingSignup status transitions (verify → onboard → Essential create; Pro pay → webhook → create).
- Single vs multi from location count.
- Idempotent Essential choose-plan and Pro webhook create.
- OTP cooldown / bad code / email-in-use.
- Frontend helpers: paths, Essential→Pilot / Pro→Growth map, resume-step logic.

### Manual

- Signup + Verify against Figma (with OTP field).
- Guest Loop single and multi → Choose plan + feature dialog.
- Essential → provisioning → Sign in works.
- Pro → Revolut → return → webhook → provisioning → Sign in.
- Cancel / fail Revolut → Choose plan, retry works.
- Home has Signup CTA, not Request Trial.
- Old register routes redirect or are gone.
- Sign-in, forgot password, team invite still work.

## Success criteria

- One self-serve path replaces trial + invite operator setup.
- No operator account before Essential confirm or Pro payment success.
- Manage Plan catalog powers the feature dialog; paid checkout matches Manage Plan patterns.
- After provision, the user signs in with the new account.
- Social buttons visible but non-functional (coming soon).

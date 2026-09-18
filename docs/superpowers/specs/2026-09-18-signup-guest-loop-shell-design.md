# Signup Guest Loop shell + direct Pilot provision

Date: 2026-09-18  
Status: approved for plan  
Supersedes (in part): [2026-09-17-self-serve-signup-design.md](./2026-09-17-self-serve-signup-design.md) — Guest Loop chrome, Choose plan, multi-location signup, and Ready teaser.

## Goal

Align post-verify Signup with Marketing Website Figma Guest Loop chrome, and simplify the journey: three onboarding steps → always provision **Pilot** → brief provisioning UI → Sign in. Remove Choose plan from the Signup path.

## Figma references

File: [Tummly — Marketing Website](https://www.figma.com/design/UP1DqyGGGrxx80Dp7jReTT/Tummly---Marketing-Website)

| Step | Screen | Node |
|---|---|---|
| 1 of 3 | Your account | `4974:22427` |
| 2 of 3 | Restaurant | `4974:22970` |
| 3 of 3 | Location | `4974:23777` |

Signup + Verify stay on **SignupModalShell** (blurred modal). Unchanged by this spec.

## Decisions (locked)

| Topic | Choice |
|---|---|
| Plan after onboarding | Always **Pilot** (Essential). No Choose plan UI. |
| Shell | **Restyle `GuestLoopShell`** to Figma (onboarding + provisioning). |
| Name fields | First + last in UI; join to `fullName` on save. No new DB columns. |
| Country / Timezone | Show read-only defaults (United Kingdom, Europe/London). **Do not persist.** |
| Phone | On **step 2** (Restaurant). Maps to `primaryPhone` / location phone as today. |
| Terms on step 1 | **No** checkbox. Signup modal terms remain the acceptance. |
| Locations | **One location only.** Retire number-of-locations and multi signup path. |
| Provision API | Fold Pilot provision into final onboarding save (not a separate choose-plan call from the client). |
| Paid / Revolut signup | Leave existing choose-plan / pay session code in place but **unused** by the Signup UI. Out of cleanup scope. |

## User journey

```text
/signup (modal) → /signup/verify (OTP)
  → /signup/onboarding
       1. Account — email read-only, first name, last name, password, confirm
       2. Restaurant — restaurant/group name*, restaurant type*, website optional, phone
       3. Location — location name*, address*, city/town*, postcode*,
          country + timezone read-only; CTA “Set up your account”
  → POST onboarding (full payload) → server saves + provisions Pilot
  → /signup/provisioning (brief animation + poll) → /login?setup=complete
```

`/signup/choose-plan` is removed from the product path (redirect to onboarding or provisioning by session status).

## Shell design

Restyle `GuestLoopShell` to match Figma (all callers that still use it, including signup onboarding and provisioning):

- Outer grey frame with ~20px padding (`AuthShell`-like outer feel).
- Inner full-width `#fafafa` panel, ~12px radius.
- Header inside panel: Marketing logo left; “Having trouble?” + underlined Contact support right (`HELP_CENTRE_CONTACT_URL`).
- Main content centered, ~473px max width; progress segments + step copy from Figma.
- Footer **outside** the panel: reuse `AuthFooter` pattern (© 2026 Tummly; Help Centre, Terms, Privacy, Cookie settings).
- **Remove** from this shell: site `Navbar`, `AuthFormAccent` kitchen art, `GuestLoopSupportFooter` / `GuestLoopLegalFooter` stack.

Back control: Figma uses in-form **Back** buttons on steps 2–3 (not the old top back chrome). Prefer those; drop or hide `GuestLoopBackButton` for signup when in-form Back exists.

## Onboarding steps (UI)

### 1 — Your account (`4974:22427`)

- Progress: segment 1 green.
- Copy: “1 of 3 · Your account”, “Set up your account”, Figma body.
- Fields: work email (read-only, filled), first name, last name, password, confirm password (show/hide as existing primitives).
- CTA: Continue (primary, not full width per Figma).
- No terms checkbox.

### 2 — Restaurant (`4974:22970`)

- Progress: segments 1–2 green.
- Copy: “2 of 3 · Restaurant”, “Tell us about your restaurant”, Figma body.
- Fields: Restaurant or group name*; Restaurant type* (label; keep existing `BUSINESS_CATEGORY_OPTIONS` values); Website — Optional; phone (**optional**, same `optionalMobileSchema` rules as today).
- CTAs: Back + Continue (Continue flexes).

### 3 — Location (`4974:23777`)

- Progress: all three green (third may show full green fill per Figma).
- Copy: “3 of 3 · Location”, “Set up your first Location”, Figma body.
- Fields: Location name*; Address*; City / town* + Postcode* row; Country* and Timezone read-only defaults.
- Reuse existing address/postcode helpers where they fit; city remains a normal text field (no live city dropdown API in this pass).
- CTAs: Back + “Set up your account” (primary).

Wizard state stays client-side until step 3 submit (same as today’s single save). No partial server save for steps 1–2 required.

## Backend

### Final onboarding save provisions Pilot

Extend `SaveOnboardingAsync` (or a clearly named internal path it calls) so that when status is `Verified` (or re-save from `OnboardingComplete` / idempotent provision cases as needed):

1. Validate payload; require **exactly one** location.
2. Persist password hash, profile, `OnboardingJson`, `AccountType = Single`.
3. Set `ChosenPlan = Pilot`, `ChosenCadence = monthly` (or existing Pilot default).
4. Move to `Provisioning` and call `ProvisionFromPendingAsync` (reuse logic from today’s Pilot branch of `ChoosePlanAsync`).
5. Return session response with status reflecting provisioned / provisioning / complete as today’s provision path does.

Idempotency: if already `Complete` / already provisioned for this pending row, return success without creating a second account (mirror Pilot choose-plan idempotency).

### Validation changes

- Reject `Locations.Count != 1` for signup onboarding.
- No multi `AccountType` from signup.

### Choose-plan endpoint

- Keep for now (paid path unused by UI).
- Frontend must not navigate to choose-plan.
- `ResolveLastStepHint`: map former `choose-plan` hint to `provisioning` (or `location` / complete) so resume never points at a removed screen.

### Routes / redirects

- Remove or redirect `/signup/choose-plan` → `/signup/onboarding` if `Verified`, else `/signup/provisioning` if mid/post provision, else `/signup`.
- Drop choose-plan cards / feature dialog from the Signup product surface (files may be deleted or left unreferenced).

## Frontend modules

| Area | Change |
|---|---|
| `GuestLoopShell` | Figma chrome restyle |
| `SignupOnboardingPage` | Three-step wizard only; no multi; no Ready teaser; step 3 → save → `/signup/provisioning` |
| Step components | Adapt or replace Guest Loop steps for Figma fields/copy (first/last name; restaurant-only step; location-only step) |
| `SignupChoosePlanPage` + cards/dialog | Remove from routes / product path |
| `SignupProvisioningPage` | Keep brief animation + poll; use restyled shell |
| Schemas | Split step field sets; firstName/lastName UI fields; drop `numLocations` / agree from signup step 1 |

## Errors and edge cases

- Step validation stays on the form (existing wizard patterns).
- Step 3 save/provision failure → inline error on Location step; retry allowed; no orphan duplicate accounts (idempotent provision).
- Refresh mid-wizard (before step 3 submit) → session still `Verified` with no password hash; return to step 1 with email from session; unsaved step fields are lost (no partial server save). After a successful step 3 submit, route by status to provisioning or Sign in; retry provision if needed (idempotent).
- Missing session → `/signup`.
- Email already Complete → Sign in path as today.

## Testing

### Automated

- `SaveOnboarding` with one location → Pilot provisioned / Complete (or Provisioning then Complete).
- `SaveOnboarding` with 0 or 2+ locations → rejected.
- Idempotent second save/provision for same pending.
- Frontend: first+last → `fullName`; path helpers no longer route to choose-plan.

### Manual

- Chrome matches Figma on steps 1–3 and provisioning (header, footer, panel).
- Full path: Signup → Verify → 3 steps → provisioning → Sign in as Pilot.
- Back between steps preserves form values.
- `/signup/choose-plan` redirects safely.
- Register single/multi redirects still work; shell change does not break Sign-in / AuthShell.

## Out of scope

- Persisting country / timezone.
- Deleting Revolut / paid signup backend.
- Changing signed-in Manage Plan or billing.
- Live city autocomplete dropdown.
- Changing Signup / Verify modal chrome.
- Reintroducing multi-location at signup.

## Success criteria

- Post-verify UI uses Figma Guest Loop shell.
- Three steps match the three Figma frames (fields and chrome).
- No Choose plan in the Signup journey.
- One location; always Pilot; user reaches Sign in after brief provisioning.
- Signup modal terms remain the only terms acceptance before account create.

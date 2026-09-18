# Signup Guest Loop Shell + Direct Pilot Provision Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restyle `GuestLoopShell` to Figma, replace signup onboarding with a three-step Account → Restaurant → Location wizard (one location), remove Choose plan from the product path, and provision Pilot on final onboarding save.

**Architecture:** Client keeps wizard state until step 3, then `POST /Signup/onboarding` saves the profile, sets Pilot, and calls `ProvisionFromPendingAsync` (same create path as today’s Essential choose-plan). `GuestLoopShell` becomes the Figma full-page chrome for onboarding and provisioning. `/signup/choose-plan` redirects by session status; paid choose-plan API stays unused by Signup UI.

**Tech Stack:** ASP.NET Core, xUnit, React, React Router, react-hook-form, Zod, Vitest, existing form primitives (`FormFloatingInput`, `FormFloatingSelect`, `FormAddressPostcodeFields`), `AuthFooter`, Lucide/shadcn as already used.

**Spec:** [docs/superpowers/specs/2026-09-18-signup-guest-loop-shell-design.md](../specs/2026-09-18-signup-guest-loop-shell-design.md)

## Global Constraints

- Always provision **Pilot** after step 3; no Choose plan UI.
- Exactly **one** location at signup; no `numLocations`; no multi signup path.
- First + last name in UI → join to `fullName` on save; no new DB columns.
- Country / Timezone: read-only United Kingdom / Europe/London; do not persist.
- Phone on **step 2** (optional mobile schema); no terms checkbox on step 1.
- Restyle **`GuestLoopShell`** (not a parallel shell); Signup/Verify stay on `SignupModalShell`.
- Leave Revolut / paid `ChoosePlanAsync` code in place; do not delete in this plan.
- Prefer existing UI primitives over one-off Figma SVGs.
- Report to the human in ASD-STE100 Simplified Technical English.
- Commit only when the human asks to commit (skip Step “Commit” unless asked).
- Subagents: Cursor Auto / `inherit` only (no Opus / GPT model overrides).

---

## File map

| File | Role |
|---|---|
| `backend/.../Services/SignupService.cs` | SaveOnboarding → require 1 location → Pilot provision; fix last-step hint |
| `backend/.../Tests/Services/SignupServiceTests.cs` | Update save tests; reject 2+ locations |
| `backend/.../Tests/Services/SignupProvisionTests.cs` | Add SaveOnboarding-provisions-Pilot coverage |
| `src/components/guest-loop/GuestLoopShell.tsx` | Figma chrome restyle |
| `src/components/guest-loop/GuestLoopSignupProgress.tsx` | 3-segment bar + “N of 3 · Label” (new) |
| `src/components/guest-loop/guestLoopSteps.ts` | Signup steps: Account / Restaurant / Location |
| `src/schemas/signupOnboarding.ts` | Signup wizard Zod + defaults + `joinSignupFullName` (new) |
| `src/schemas/signupOnboarding.test.ts` | Unit tests for name join + step schemas (new) |
| `src/components/signup/SignupAccountStep.tsx` | Step 1 UI (new) |
| `src/components/signup/SignupRestaurantStep.tsx` | Step 2 UI (new) |
| `src/components/signup/SignupLocationStep.tsx` | Step 3 UI (new) |
| `src/pages/auth/SignupOnboardingPage.tsx` | Three-step wizard only; save → provisioning |
| `src/pages/auth/SignupChoosePlanRedirectPage.tsx` | Status-based redirect (new; replaces choose-plan page) |
| `src/pages/auth/SignupProvisioningPage.tsx` | Drop paid-intent UX if dead; keep poll + shell |
| `src/pages/routes/AppRoutes.tsx` | Wire choose-plan → redirect component |
| `src/lib/signupSession.ts` / tests | Optional path helper for choose-plan redirect |
| Delete or unreference | `SignupChoosePlanPage.tsx`, `SignupChoosePlanCards.tsx`, `SignupPlanFeaturesDialog.tsx` (after route swap) |

---

### Task 1: Backend — SaveOnboarding provisions Pilot

**Files:**
- Modify: `backend/TummlyBackend/Services/SignupService.cs`
- Modify: `backend/TummlyBackend/Tests/Services/SignupServiceTests.cs`
- Modify: `backend/TummlyBackend/Tests/Services/SignupProvisionTests.cs`

**Interfaces:**
- Consumes: `IProvisioningService.ProvisionFromPendingAsync(Guid pendingSignupId)`, `BillingSubscriptionPlans.Pilot`
- Produces: `SaveOnboardingAsync` returns session with status `Complete` (or `Provisioning` then Complete after provision) and creates the Pilot account when given exactly one location; throws when location count ≠ 1

- [ ] **Step 1: Write failing tests**

In `SignupServiceTests.cs`, change `SaveOnboarding_OneLocation_SetsSingle` expectations: after save, status is `Complete` (or provision path’s final status), `ChosenPlan` is Pilot, `AccountType` is `Single`. Replace `SaveOnboarding_TwoLocations_SetsMulti` with `SaveOnboarding_TwoLocations_Throws` that expects an exception and no Multi account type.

In `SignupProvisionTests.cs`, add:

```csharp
[Fact]
public async Task SaveOnboarding_Verified_ProvisionsPilotAndCompletes()
{
    // Seed Verified pending (same helper pattern as SignupServiceTests.SeedVerifiedPendingAsync)
    // Call SaveOnboardingAsync with one location + valid password/profile
    // Assert: Users has email, pending.Status == Complete, BillingAccount.SubscriptionPlan == Pilot
}

[Fact]
public async Task SaveOnboarding_AlreadyComplete_IsIdempotent()
{
    // Seed Verified → SaveOnboarding once → SaveOnboarding again with same payload
    // Assert: exactly one User and one Restaurant
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~SignupServiceTests.SaveOnboarding|FullyQualifiedName~SignupProvisionTests.SaveOnboarding"
```

Expected: FAIL (still `OnboardingComplete` / Multi accepted).

- [ ] **Step 3: Implement SaveOnboarding provision path**

In `SignupService.SaveOnboardingAsync`:

1. After existing Verified / OnboardingComplete gate, if `pending.Status == Complete`, return `ToSessionResponse(pending)` (idempotent).
2. `ValidateOnboardingPayload`: require `dto.Locations.Count == 1` (throw `"Exactly one location is required."` if not).
3. Persist hash, profile, `AccountType = "Single"`, `OnboardingJson` as today.
4. Set `ChosenPlan = BillingSubscriptionPlans.Pilot`, `ChosenCadence = "monthly"`, `Status = Provisioning`, `SaveChangesAsync`.
5. `await _provisioningService.ProvisionFromPendingAsync(pending.Id)`.
6. Reload pending (or rely on provision updating status) and return `ToSessionResponse(pending)`.

Update `ResolveLastStepHint`: `OnboardingComplete` → `"provisioning"` (not `"choose-plan"`). Prefer `location` only if you keep a non-provisioning OnboardingComplete path; after this task OnboardingComplete should be rare.

Keep `ChoosePlanAsync` unchanged for unused paid path and provisioning retry.

- [ ] **Step 4: Run tests to verify they pass**

Run the same `dotnet test` filter. Expected: PASS.

- [ ] **Step 5: Commit** (only if human asked)

```bash
git add backend/TummlyBackend/Services/SignupService.cs backend/TummlyBackend.Tests/Services/SignupServiceTests.cs backend/TummlyBackend.Tests/Services/SignupProvisionTests.cs
git commit -m "$(cat <<'EOF'
feat(signup): provision Pilot on onboarding save

EOF
)"
```

---

### Task 2: Restyle `GuestLoopShell`

**Files:**
- Modify: `src/components/guest-loop/GuestLoopShell.tsx`
- Reuse: `src/components/auth/AuthFooter.tsx`, `src/components/marketing/MarketingLogo.tsx`, `HELP_CENTRE_CONTACT_URL` from `@/config/support`

**Interfaces:**
- Consumes: existing `GuestLoopShellProps` (`children`, `className`, `contentAlign`, `contentMaxWidthClassName`, `hideFooters`, back props)
- Produces: Same export `GuestLoopShell`; visual layout matches Figma (grey pad, `#fafafa` rounded panel, logo + support header, AuthFooter outside panel). Back button props may no-op for signup (steps use in-form Back); keep prop for callers that still pass them or hide when unused.

- [ ] **Step 1: Replace shell layout**

Rewrite `GuestLoopShell` structure to:

```tsx
<div className={cn("flex min-h-dvh flex-col bg-[#cbcbcb] p-5", className)}>
  <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-xl bg-[#fafafa]">
    <header className="flex shrink-0 items-center justify-between px-10 pt-10">
      <Link to="/"><MarketingLogo onLight width={145} height={37} /></Link>
      <p className="m-0 flex items-center gap-2.5 text-base text-[#141414]">
        Having trouble?{" "}
        <Link to={HELP_CENTRE_CONTACT_URL} className="text-sm font-medium underline">
          Contact support
        </Link>
      </p>
    </header>
    <main className="… flex-1 center children with contentMaxWidthClassName …">
      {/* optional GuestLoopBackButton only if showBackButton && onBack */}
      {children}
    </main>
  </div>
  {!hideFooters ? <AuthFooter /> : null}
</div>
```

Remove: `Navbar`, `AuthFormAccent`, `GuestLoopSupportFooter`, `GuestLoopLegalFooter`.

Tune padding to Figma (~40px panel pad). Outer grey ≈ `#cbcbcb` / AuthShell `#c9d1cb` — prefer closest design token if one exists; otherwise use the Figma-adjacent hex and note in PR.

Default `contentMaxWidthClassName` to `max-w-[473px]` for signup; choose-plan callers go away; provisioning may keep a wider max if needed via prop.

- [ ] **Step 2: Manual visual check**

Run the app, open `/signup/onboarding` (or any page still wrapping `GuestLoopShell`). Confirm: no site navbar, logo + contact support, footer outside panel.

- [ ] **Step 3: Commit** (only if human asked)

---

### Task 3: Signup onboarding schema + name helper

**Files:**
- Create: `src/schemas/signupOnboarding.ts`
- Create: `src/schemas/signupOnboarding.test.ts`

**Interfaces:**
- Produces:

```ts
export function joinSignupFullName(firstName: string, lastName: string): string

export type SignupOnboardingFormValues = {
  token: string
  email: string
  firstName: string
  lastName: string
  password: string
  confirmPassword: string
  restaurantName: string
  businessCategory: string
  businessLink: string
  phone: string
  locationName: string
  address: string
  city: string
  postcode: string
  addressOverridden?: boolean
  country: string // UI only, default "United Kingdom"
  timezone: string // UI only, default "Europe/London"
}

export const signupOnboardingDefaultValues: SignupOnboardingFormValues
export const signupAccountStepSchema // email, firstName, lastName, password, confirmPassword
export const signupRestaurantStepSchema // restaurantName, businessCategory, businessLink, phone
export const signupLocationStepSchema // locationName, address, city, postcode
export const signupAccountStepFields / signupRestaurantStepFields / signupLocationStepFields
export function toSignupOnboardingPayload(values: SignupOnboardingFormValues): SignupOnboardingPayload
```

`toSignupOnboardingPayload` joins names, sets `groupName` from `restaurantName`, `primaryPhone` / `locationPhone` from `phone`, `localContact` from joined full name, **omits** country/timezone, always one location.

- [ ] **Step 1: Write failing Vitest tests**

```ts
import { describe, expect, it } from "vitest"
import {
  joinSignupFullName,
  signupAccountStepSchema,
  toSignupOnboardingPayload,
  signupOnboardingDefaultValues,
} from "./signupOnboarding"

describe("joinSignupFullName", () => {
  it("joins trimmed first and last", () => {
    expect(joinSignupFullName("  Ada ", " Lovelace ")).toBe("Ada Lovelace")
  })
})

describe("signupAccountStepSchema", () => {
  it("rejects empty first name", () => {
    const result = signupAccountStepSchema.safeParse({
      email: "a@b.com",
      firstName: "",
      lastName: "L",
      password: "Password1!",
      confirmPassword: "Password1!",
    })
    expect(result.success).toBe(false)
  })
})

describe("toSignupOnboardingPayload", () => {
  it("maps one location and joined fullName", () => {
    const payload = toSignupOnboardingPayload({
      ...signupOnboardingDefaultValues,
      token: "t",
      email: "a@b.com",
      firstName: "Ada",
      lastName: "Lovelace",
      password: "Password1!",
      confirmPassword: "Password1!",
      restaurantName: "Cafe",
      businessCategory: "cafe",
      locationName: "Main",
      address: "1 High St",
      city: "London",
      postcode: "SW1A 1AA",
    })
    expect(payload.fullName).toBe("Ada Lovelace")
    expect(payload.locations).toHaveLength(1)
    expect(payload).not.toHaveProperty("country")
  })
})
```

- [ ] **Step 2: Run tests — expect FAIL**

Run: `npx vitest run src/schemas/signupOnboarding.test.ts`

- [ ] **Step 3: Implement schema module**

Reuse `emailSchema`, `passwordSchema`, `optionalMobileSchema`, `optionalUrlSchema`, UK postcode regex from `accountSetupSingle.ts` / primitives. Do **not** include `agree` or `numLocations`.

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 4: Progress chrome + three step components

**Files:**
- Create: `src/components/guest-loop/GuestLoopSignupProgress.tsx`
- Modify: `src/components/guest-loop/guestLoopSteps.ts` (add `SIGNUP_ONBOARDING_STEPS`)
- Create: `src/components/signup/SignupAccountStep.tsx`
- Create: `src/components/signup/SignupRestaurantStep.tsx`
- Create: `src/components/signup/SignupLocationStep.tsx`

**Interfaces:**
- Consumes: `signupOnboarding` schemas/fields; `BUSINESS_CATEGORY_OPTIONS`; form primitives; `useGuestLoopStepCanSubmit` / validation feedback
- Produces: presentational steps with props `{ form, onContinue, onBack?, isSubmitting? }` and internal progress via `activeStep: 1 | 2 | 3`

- [ ] **Step 1: Add `SIGNUP_ONBOARDING_STEPS`**

```ts
export const SIGNUP_ONBOARDING_STEPS = [
  { number: 1, label: "Your account" },
  { number: 2, label: "Restaurant" },
  { number: 3, label: "Location" },
] as const
```

- [ ] **Step 2: Implement `GuestLoopSignupProgress`**

Left-aligned block: three 6px rounded segments (`bg-[#14a74a]` for completed/active index, else `#e5e5e5`), then `"${n} of 3 · ${label}"` (18px), title (36px medium), description (16px). Match Figma spacing (gap ~30px header block, ~12px text stack).

- [ ] **Step 3: Implement three step components**

**Account:** read-only email floating field; first name; last name; password + confirm with existing show/hide pattern from `GuestLoopPasswordStep`; strength meter optional if easy to reuse; Continue only (not full width — `w-auto` primary Button). No terms.

**Restaurant:** name; `FormFloatingSelect` label “Restaurant type*” with `BUSINESS_CATEGORY_OPTIONS`; website optional; phone optional; Back + Continue row (`Button` outline + primary flex-1).

**Location:** location name; address/postcode via `FormAddressPostcodeFields` (include city); read-only Country / Timezone fields (disabled inputs or read-only floating fields bound to defaults); helper line under timezone from Figma; Back + “Set up your account”.

Use shadcn `Button` variants; reuse `GuestLoopStepButton` only if it matches Figma; otherwise `Button` directly per AGENTS UI rules.

- [ ] **Step 4: Smoke-render via temporary story or onboarding page stub** (optional) — prefer wiring in Task 5.

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 5: Rewrite `SignupOnboardingPage`

**Files:**
- Modify: `src/pages/auth/SignupOnboardingPage.tsx`

**Interfaces:**
- Consumes: `getSignupSession`, `saveSignupOnboarding`, `toSignupOnboardingPayload`, three step components, `GuestLoopShell`
- Produces: steps 1→2→3; step 3 calls save then `navigate("/signup/provisioning", { replace: true })`; gate: Verified only for wizard; Provisioning/Complete → provisioning or login; no multi / Ready teaser / choose-plan

- [ ] **Step 1: Replace page body**

- Single `useForm<SignupOnboardingFormValues>` with zod resolver on full schema or step schemas via existing `validateWizardStep`.
- Gate: no token → `/signup`; Complete → login; post-onboarding statuses (`Provisioning`, `Complete`) → provisioning/login; not Verified → `/signup`.
- Reset email (and empty names) from session; do not require password hash for step resume (always start step 1 on refresh while Verified).
- `handleFinish`: validate location step → `saveSignupOnboarding(token, toSignupOnboardingPayload(values))` → navigate provisioning; on error set `form.setError("root", …)`.
- Wrap in `GuestLoopShell` with `showBackButton={false}` (in-form Back only), `contentMaxWidthClassName="max-w-[473px]"`.

Remove imports of multi form, Ready teaser, location-count branching, `GUEST_LOOP_MULTI_STEPS`.

- [ ] **Step 2: Manual path check**

With API running: verify → fill 3 steps → land on provisioning → login works as Pilot.

- [ ] **Step 3: Commit** (only if human asked)

---

### Task 6: Remove Choose plan from product path + tidy provisioning

**Files:**
- Create: `src/pages/auth/SignupChoosePlanRedirectPage.tsx`
- Modify: `src/pages/routes/AppRoutes.tsx`
- Modify: `src/pages/auth/SignupProvisioningPage.tsx`
- Delete (after unreferenced): `SignupChoosePlanPage.tsx`, `SignupChoosePlanCards.tsx`, `SignupPlanFeaturesDialog.tsx`
- Modify if needed: `src/lib/signupPlanMap.ts` — leave if still imported by tests; otherwise delete dead exports in a follow-up only if unused

**Interfaces:**
- Redirect page: read session → Verified → `/signup/onboarding`; Provisioning/AwaitingPayment/OnboardingComplete → `/signup/provisioning`; Complete → login; else `/signup`
- Provisioning: keep status poll; for Pilot retry prefer `chooseSignupPlan(session, { planId: "Pilot" })` still (API remains) **or** re-POST onboarding only if you still have form state (you will not) — **keep Pilot choose-plan retry** for failed mid-provision. Remove Revolut cancel / paid-intent messaging if it only served Choose plan return URLs, or keep harmless.

- [ ] **Step 1: Add redirect page and swap route**

```tsx
// SignupChoosePlanRedirectPage — useEffect gate like other signup pages
```

In `AppRoutes`, replace `SignupChoosePlanPage` element with `SignupChoosePlanRedirectPage`.

- [ ] **Step 2: Simplify provisioning copy/paths**

Remove navigation to `/signup/choose-plan`. Ensure failure CTA retries Pilot via `chooseSignupPlan` or “return to onboarding” only when status still `Verified`.

- [ ] **Step 3: Delete unused choose-plan UI files** once grep shows no imports.

- [ ] **Step 4: Grep cleanup**

```bash
rg "choose-plan|SignupChoosePlan|SignupPlanFeatures" src
```

Expected: redirect route path string only (and maybe provisioning retry).

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 7: Regression pass

**Files:** none new

- [ ] **Step 1: Automated**

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~Signup"
npx vitest run src/schemas/signupOnboarding.test.ts src/lib/signupSession.test.ts src/lib/signupPlanMap.test.ts
```

Expected: PASS (update `signupPlanMap` tests only if file still exists and is required).

- [ ] **Step 2: Manual checklist**

- [ ] Shell matches Figma on steps 1–3 (header, footer, panel, progress).
- [ ] No Choose plan screen; `/signup/choose-plan` redirects.
- [ ] One location; phone on step 2; country/timezone read-only on step 3.
- [ ] Provisioning → Sign in as Pilot.
- [ ] Sign-in / AuthShell unchanged; `/register/single` still redirects to `/signup`.

---

## Spec coverage self-review

| Spec requirement | Task |
|---|---|
| Restyle GuestLoopShell | Task 2 |
| Steps 1–3 Figma fields/copy | Tasks 3–5 |
| first+last → fullName | Task 3 |
| Phone on restaurant step | Tasks 3–4 |
| No terms on step 1 | Tasks 3–4 |
| Country/timezone read-only, not persisted | Tasks 3–4 |
| One location; no multi / numLocations | Tasks 1, 3, 5 |
| Save onboarding provisions Pilot | Task 1 |
| Remove Choose plan UI; redirect | Task 6 |
| Provisioning then login | Tasks 5–6 |
| Leave paid API | Task 1 (unchanged ChoosePlanAsync) |
| Signup modal unchanged | (no task — out of scope) |

## Placeholder / consistency check

- No TBD steps; Pilot cadence fixed to `"monthly"`.
- Status after save: tests assert `Complete` after successful provision (same as `ChoosePlan_Pilot_CreatesUserAndCompletes`).
- Retry uses existing `ChoosePlanAsync` Pilot for mid-provision failures.

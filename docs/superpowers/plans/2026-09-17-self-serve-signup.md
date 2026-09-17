# Self-serve Signup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Request Trial with self-serve Signup → OTP verify → Guest Loop → Choose plan → Essential free create or Pro Revolut pay → provision → Sign in, using a pending signup session so the operator account exists only after Essential confirm or Pro payment success.

**Architecture:** New `PendingSignup` entity + signup session token. OTP reuses `OtpVerification` patterns. Guest Loop UI saves into pending signup (no invite token). Choose plan maps Essential→Pilot / Pro→Growth; feature dialog reuses Manage Plan catalog. Essential calls `ProvisionFromPendingAsync`. Pro starts Revolut with purpose `signup_plan` (PendingSignupId on intent; RestaurantId nullable). Webhook creates account then frontend polls provisioning status and shows Guest Loop phase UI before `/login?setup=complete`.

**Tech Stack:** ASP.NET Core, EF Core, xUnit, React, React Router, react-hook-form, Zod, Vitest, shadcn `Button` / `Dialog` / `Checkbox` / OTP, Tailwind, existing Revolut pay-session helpers.

**Spec:** [docs/superpowers/specs/2026-09-17-self-serve-signup-design.md](../specs/2026-09-17-self-serve-signup-design.md)

## Global Constraints

- Pending signup approach only — no operator `User` / `Restaurant` until Essential confirm or Pro webhook success.
- Essential → `Pilot` (free). Pro card → `Growth` (Revolut). Feature dialog may select Pilot / Starter / Growth / Group (Pilot free; others Revolut).
- Default cadence: **monthly**.
- Google / Microsoft: UI stubs only (reuse / adapt `AuthSocialContinueButtons`).
- Signup + Verify: **centered modal** over blurred Guest Loop backdrop — not AuthShell.
- Verify includes **6-digit OTP** even though Figma copy says “link”.
- Retire Request Trial home path and `/register/single` + `/register/multi` (redirect to `/signup`).
- Keep Sign-in, forgot/reset password, team invite accept.
- Prefer existing UI primitives (Lucide, shadcn) over one-off Figma SVGs.
- Commit only when the human asks to commit.
- Report to the human in ASD-STE100 Simplified Technical English.
- Subagents: Cursor Auto only (no Opus / GPT model overrides).

---

## File map

| File | Role |
|---|---|
| `backend/.../Models/PendingSignup.cs` | Pending signup row + status constants |
| `backend/.../Models/RevolutOrderIntent.cs` | Add `SignupPlan` purpose; optional `PendingSignupId`; nullable `RestaurantId` for signup |
| `backend/.../Data/ApplicationDbContext.cs` | DbSet + config |
| `backend/.../Migrations/*_AddPendingSignup.cs` | EF migration |
| `backend/.../DTOs/Signup/*.cs` | Request/response DTOs |
| `backend/.../Interfaces/ISignupService.cs` | Signup service contract |
| `backend/.../Services/SignupService.cs` | Create/verify/onboard/choose-plan/status |
| `backend/.../Interfaces/IProvisioningService.cs` | Add `ProvisionFromPendingAsync` |
| `backend/.../Services/GuestLoopProvisioningService.cs` | Provision from `PendingSignup` + plan |
| `backend/.../Services/SignupPaySessionService.cs` | Revolut checkout for signup paid plans |
| `backend/.../Controllers/SignupController.cs` | `/api/Signup/*` endpoints |
| `backend/.../Services/RevolutWebhookService.cs` (or applier) | Handle `signup_plan` → provision |
| `backend/TummlyBackend.Tests/Services/SignupServiceTests.cs` | Unit/integration tests |
| `backend/TummlyBackend.Tests/Services/SignupProvisionTests.cs` | Essential + webhook provision |
| `src/api/signupApi.ts` | Frontend API client |
| `src/lib/signupSession.ts` | Token storage + path helpers |
| `src/lib/signupSession.test.ts` | Unit tests |
| `src/lib/signupPlanMap.ts` | Essential→Pilot, Pro→Growth |
| `src/lib/signupPlanMap.test.ts` | Unit tests |
| `src/schemas/signup.ts` | Zod schemas |
| `src/components/signup/SignupModalShell.tsx` | Blurred backdrop + card |
| `src/components/signup/SignupForm.tsx` | Email + terms form |
| `src/components/signup/SignupVerifyStep.tsx` | OTP + resend + different email |
| `src/pages/auth/SignupPage.tsx` | `/signup` |
| `src/pages/auth/SignupVerifyPage.tsx` | `/signup/verify` |
| `src/pages/auth/SignupOnboardingPage.tsx` | Guest Loop wizard on pending session |
| `src/pages/auth/SignupChoosePlanPage.tsx` | Choose plan + feature dialog |
| `src/pages/auth/SignupProvisioningPage.tsx` | Phase animations + poll + login |
| `src/components/signup/SignupChoosePlanCards.tsx` | Essential / Pro cards |
| `src/components/signup/SignupPlanFeaturesDialog.tsx` | Manage Plan comparison dialog |
| `src/components/home/HeroTrialForm.tsx` / Hero | Replace with Signup CTA |
| `src/pages/routes/AppRoutes.tsx` | New routes; register redirects |
| `src/components/guest-loop/*` | Restyle + location-count branch; soft-wire to signup APIs |

---

### Task 1: PendingSignup model + migration

**Files:**
- Create: `backend/TummlyBackend/Models/PendingSignup.cs`
- Modify: `backend/TummlyBackend/Data/ApplicationDbContext.cs`
- Create: EF migration `AddPendingSignup`
- Modify: `backend/TummlyBackend/Models/RevolutOrderIntent.cs` (purpose + nullable restaurant + PendingSignupId)

**Interfaces:**
- Produces:
  - `PendingSignup` with `Id` (Guid), `SessionToken` (Guid, unique), `Email`, `Status`, OTP counters, `PasswordHash?`, profile JSON or columns, `AccountType` (`Single`|`Multi`), `ChosenPlan`, `ChosenCadence`, `RevolutOrderId?`, timestamps
  - `PendingSignupStatuses`: `EmailPending`, `Verified`, `OnboardingComplete`, `AwaitingPayment`, `Provisioning`, `Complete`, `Abandoned`
  - `RevolutOrderIntentPurposes.SignupPlan = "signup_plan"`
  - `RevolutOrderIntent.PendingSignupId` (`Guid?`), `RestaurantId` nullable for signup intents

- [ ] **Step 1: Write failing model/shape test**

Create `backend/TummlyBackend.Tests/Services/PendingSignupModelTests.cs`:

```csharp
[Fact]
public void PendingSignupStatuses_AreStableStrings()
{
    Assert.Equal("EmailPending", PendingSignupStatuses.EmailPending);
    Assert.Equal("Verified", PendingSignupStatuses.Verified);
    Assert.Equal("OnboardingComplete", PendingSignupStatuses.OnboardingComplete);
    Assert.Equal("AwaitingPayment", PendingSignupStatuses.AwaitingPayment);
    Assert.Equal("Provisioning", PendingSignupStatuses.Provisioning);
    Assert.Equal("Complete", PendingSignupStatuses.Complete);
    Assert.Equal("Abandoned", PendingSignupStatuses.Abandoned);
}

[Fact]
public void RevolutOrderIntentPurposes_IncludesSignupPlan()
{
    Assert.Equal("signup_plan", RevolutOrderIntentPurposes.SignupPlan);
}
```

- [ ] **Step 2: Run test — expect FAIL**

Run: `dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter PendingSignupModelTests`

Expected: FAIL (types missing)

- [ ] **Step 3: Add model + DbContext + migration**

```csharp
public static class PendingSignupStatuses
{
    public const string EmailPending = "EmailPending";
    public const string Verified = "Verified";
    public const string OnboardingComplete = "OnboardingComplete";
    public const string AwaitingPayment = "AwaitingPayment";
    public const string Provisioning = "Provisioning";
    public const string Complete = "Complete";
    public const string Abandoned = "Abandoned";
}

public class PendingSignup
{
    public Guid Id { get; set; }
    public Guid SessionToken { get; set; }
    [MaxLength(200)] public string Email { get; set; } = "";
    [MaxLength(32)] public string Status { get; set; } = PendingSignupStatuses.EmailPending;
    public int OtpResendCount { get; set; }
    public DateTime? LastOtpSentAt { get; set; }
    public bool TermsAccepted { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    [MaxLength(200)] public string? PasswordHash { get; set; }
    [MaxLength(150)] public string? FullName { get; set; }
    [MaxLength(16)] public string? AccountType { get; set; } // Single | Multi
    [MaxLength(32)] public string? ChosenPlan { get; set; } // Pilot|Starter|Growth|Group
    [MaxLength(16)] public string? ChosenCadence { get; set; } // monthly|annual
    public string? OnboardingJson { get; set; }
    [MaxLength(128)] public string? RevolutOrderId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
```

Wire `DbSet<PendingSignup>`, unique index on `SessionToken`. Extend `RevolutOrderIntent`: add `PendingSignupId`, make `RestaurantId` nullable, add `SignupPlan` purpose constant.

Add EF migration and apply locally.

- [ ] **Step 4: Run test — expect PASS**

Run: `dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter PendingSignupModelTests`

Expected: PASS

- [ ] **Step 5: Commit** (only if human asks)

---

### Task 2: SignupService — start + OTP verify/resend

**Files:**
- Create: `backend/TummlyBackend/DTOs/Signup/StartSignupDto.cs`, `VerifySignupOtpDto.cs`, `SignupSessionResponse.cs`
- Create: `backend/TummlyBackend/Interfaces/ISignupService.cs`
- Create: `backend/TummlyBackend/Services/SignupService.cs`
- Create: `backend/TummlyBackend/Controllers/SignupController.cs`
- Register DI in `Program.cs`
- Test: `backend/TummlyBackend.Tests/Services/SignupServiceTests.cs`

**Interfaces:**
- Produces:
  - `Task<SignupSessionResponse> StartAsync(StartSignupDto dto)` — `{ sessionToken, email, status }`
  - `Task<SignupSessionResponse> VerifyOtpAsync(VerifySignupOtpDto dto)`
  - `Task ResendOtpAsync(string email)` — 60s cooldown, max 5 resends → Abandoned
  - Controller routes: `POST /api/Signup/start`, `POST /api/Signup/verify-otp`, `POST /api/Signup/resend-otp`
- Consumes: `OtpVerification`, `GenerateOtp`, email OTP sender, `Users` uniqueness check

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public async Task StartAsync_CreatesPending_AndSendsOtp()
{
    var result = await _sut.StartAsync(new StartSignupDto
    {
        Email = "owner@example.com",
        TermsAccepted = true,
    });
    Assert.Equal(PendingSignupStatuses.EmailPending, result.Status);
    Assert.NotEqual(Guid.Empty, result.SessionToken);
    Assert.True(await _db.PendingSignups.AnyAsync(x => x.Email == "owner@example.com"));
}

[Fact]
public async Task StartAsync_EmailInUse_Throws()
{
    await Assert.ThrowsAsync<Exception>(() =>
        _sut.StartAsync(new StartSignupDto { Email = "taken@example.com", TermsAccepted = true }));
}

[Fact]
public async Task VerifyOtpAsync_ValidCode_SetsVerified()
{
    var session = await _sut.VerifyOtpAsync(/* email + otp from OtpVerifications */);
    Assert.Equal(PendingSignupStatuses.Verified, session.Status);
}
```

Mirror trial OTP rules: 10 min expiry, 60s resend, 5 resend max → Abandoned. Resume existing `EmailPending` row for same email (no duplicate).

- [ ] **Step 2: Run tests — expect FAIL**

Run: `dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter SignupServiceTests`

Expected: FAIL

- [ ] **Step 3: Implement service + controller**

`StartAsync`: normalize email; reject if `Users` has email; upsert `PendingSignup` in `EmailPending`/`Abandoned` (reset Abandoned); mint OTP; email OTP; return session token.

`VerifyOtpAsync`: validate OTP like `TrialService.VerifyOtpAsync` but set `PendingSignup.Status = Verified` and `EmailVerifiedAt` (do **not** create TrialRequest).

Return `sessionToken` on verify so the client can persist it.

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit** (only if human asks)

---

### Task 3: SignupService — onboarding save + resume

**Files:**
- Modify: `ISignupService` / `SignupService`
- Create: DTOs `SaveSignupOnboardingDto` (password + profile fields aligned with single/multi setup)
- Test: extend `SignupServiceTests.cs`

**Interfaces:**
- Produces:
  - `Task<SignupSessionResponse> SaveOnboardingAsync(Guid sessionToken, SaveSignupOnboardingDto dto)`
  - `Task<SignupResumeResponse> GetBySessionAsync(Guid sessionToken)` — status + email + last step hint + saved profile (no password)
- Rules:
  - Require status ≥ `Verified`
  - Hash password with BCrypt into `PasswordHash` (never return hash)
  - Set `AccountType` from location count: `1` → `Single`, else `Multi`
  - Persist onboarding payload in `OnboardingJson` (serialize fields needed by `ProvisionFromPendingAsync`)
  - When restaurant/group steps complete → `OnboardingComplete`

- [ ] **Step 1: Failing tests for single/multi branch and resume**

```csharp
[Fact]
public async Task SaveOnboarding_OneLocation_SetsSingle()
{
    Assert.Equal("Single", pending.AccountType);
    Assert.Equal(PendingSignupStatuses.OnboardingComplete, pending.Status);
}

[Fact]
public async Task SaveOnboarding_TwoLocations_SetsMulti()
{
    Assert.Equal("Multi", pending.AccountType);
}
```

- [ ] **Step 2: Run — expect FAIL**
- [ ] **Step 3: Implement SaveOnboarding + GetBySession + `GET /api/Signup/session`**
- [ ] **Step 4: Run — expect PASS**
- [ ] **Step 5: Commit** (only if human asks)

---

### Task 4: ProvisionFromPending + Essential choose-plan

**Files:**
- Modify: `IProvisioningService`, `GuestLoopProvisioningService`
- Modify: `SignupService` choose-plan
- Test: `SignupProvisionTests.cs`

**Interfaces:**
- Produces:
  - `Task ProvisionFromPendingAsync(Guid pendingSignupId)` — creates User (use existing `PasswordHash`), Restaurant, locations, GuestLoop, billing for chosen plan (`Pilot` for Essential), marks pending `Complete`, idempotent if already `Complete`
  - `Task<ChoosePlanResult> ChoosePlanAsync(Guid sessionToken, string planId, string cadence)`
    - Pilot → set plan, status `Provisioning`, call `ProvisionFromPendingAsync`, return `{ mode: "provisioned" }`
    - Paid → Task 9

Billing: for Pilot use existing `CreateDefaultBillingAccount` pilot defaults. For paid plans set subscription plan fields to match Manage Plan / pricebook (same as post-upgrade state).

- [ ] **Step 1: Failing test**

```csharp
[Fact]
public async Task ChoosePlan_Pilot_CreatesUserAndCompletes()
{
    var result = await _signup.ChoosePlanAsync(token, "Pilot", "monthly");
    Assert.Equal("provisioned", result.Mode);
    Assert.True(await _db.Users.AnyAsync(u => u.Email == email));
    Assert.Equal(PendingSignupStatuses.Complete, pending.Status);
}

[Fact]
public async Task ChoosePlan_Pilot_IsIdempotent()
{
    await _signup.ChoosePlanAsync(token, "Pilot", "monthly");
    await _signup.ChoosePlanAsync(token, "Pilot", "monthly");
    Assert.Equal(1, await _db.Users.CountAsync(u => u.Email == email));
}
```

- [ ] **Step 2: Run — FAIL**
- [ ] **Step 3: Implement `ProvisionFromPendingAsync` by extracting shared create logic from invite `ProvisionAsync` (password from pending hash; no TrialRequest/invite checks). Wire Essential choose-plan.**
- [ ] **Step 4: Run — PASS**
- [ ] **Step 5: Commit** (only if human asks)

---

### Task 5: Frontend signup session helpers + API client

**Files:**
- Create: `src/lib/signupPlanMap.ts`, `src/lib/signupPlanMap.test.ts`
- Create: `src/lib/signupSession.ts`, `src/lib/signupSession.test.ts`
- Create: `src/api/signupApi.ts`
- Create: `src/schemas/signup.ts`

**Interfaces:**
- Produces:
  - `mapSignupCardToPlanId(card: "essential" | "pro"): "Pilot" | "Growth"`
  - `buildSignupVerifyPath(email: string): string` → `/signup/verify?email=...`
  - `saveSignupSessionToken(token: string)` / `readSignupSessionToken()` / `clearSignupSessionToken()` (sessionStorage key `tummly.signupSession`)
  - API: `startSignup`, `verifySignupOtp`, `resendSignupOtp`, `saveSignupOnboarding`, `getSignupSession`, `chooseSignupPlan`, `getSignupProvisioningStatus`

- [ ] **Step 1: Failing Vitest**

```ts
import { describe, expect, it } from "vitest"
import { mapSignupCardToPlanId } from "./signupPlanMap"
import { buildSignupVerifyPath } from "./signupSession"

describe("signupPlanMap", () => {
  it("maps essential to Pilot and pro to Growth", () => {
    expect(mapSignupCardToPlanId("essential")).toBe("Pilot")
    expect(mapSignupCardToPlanId("pro")).toBe("Growth")
  })
})

describe("buildSignupVerifyPath", () => {
  it("encodes email", () => {
    expect(buildSignupVerifyPath("A@B.com")).toBe(
      "/signup/verify?email=A%40B.com"
    )
  })
})
```

- [ ] **Step 2: Run — FAIL**

Run: `npx vitest run src/lib/signupPlanMap.test.ts src/lib/signupSession.test.ts`

- [ ] **Step 3: Implement helpers + thin axios client**
- [ ] **Step 4: Run — PASS**
- [ ] **Step 5: Commit** (only if human asks)

---

### Task 6: Signup + Verify modal pages

**Files:**
- Create: `src/components/signup/SignupModalShell.tsx`
- Create: `src/components/signup/SignupForm.tsx`
- Create: `src/components/signup/SignupVerifyStep.tsx`
- Create: `src/pages/auth/SignupPage.tsx`, `SignupVerifyPage.tsx`
- Modify: `src/pages/routes/AppRoutes.tsx`
- Adapt: `AuthSocialContinueButtons` for side-by-side layout if needed (`layout="row" | "stack"`)

**Interfaces:**
- `SignupModalShell({ children })` — full viewport blurred Guest Loop backdrop image + dark overlay + centered white card (~582px)
- Signup form → `startSignup` → save token → navigate verify
- Verify: OTP (`InputOTP`), resend, different email → clear token → `/signup`
- Social buttons: no-op / title “Coming soon”

- [ ] **Step 1: Add routes + shell + forms matching Figma; wire APIs**
- [ ] **Step 2: Manual check `/signup` and `/signup/verify` in browser**
- [ ] **Step 3: Commit** (only if human asks)

Backdrop asset: export from Figma node `4974:20563` / ready screen, commit under `src/assets/images/signup-modal-backdrop.png`, register in critical images if required for LCP.

---

### Task 7: Signup onboarding page (Guest Loop)

**Files:**
- Create: `src/pages/auth/SignupOnboardingPage.tsx` (route `/signup/onboarding`)
- Modify: `GuestLoopPasswordStep`, `GuestLoopRestaurantStep`, `GuestLoopGroupStep`, `GuestLoopLocationsStep` as needed for Figma copy + location count
- Reuse: `GuestLoopShell`, step footer/progress
- Do **not** call `/auth/setup-account`

**Interfaces:**
- Gate: require `sessionToken` + status `Verified` or later via `getSignupSession`; else `/signup`
- Steps mirror register single/multi but persist via `saveSignupOnboarding`
- After last profile step → navigate `/signup/choose-plan` (ready teaser may be a step that only continues)

- [ ] **Step 1: Implement wizard with single/multi branch from location count field**
- [ ] **Step 2: Manual single + multi path to choose-plan**
- [ ] **Step 3: Commit** (only if human asks)

---

### Task 8: Choose plan UI + features dialog

**Files:**
- Create: `src/pages/auth/SignupChoosePlanPage.tsx`
- Create: `src/components/signup/SignupChoosePlanCards.tsx`
- Create: `src/components/signup/SignupPlanFeaturesDialog.tsx`
- Reuse presentation from `src/lib/operatorBillingCredits/managePlanPresentation.ts` (`MANAGE_PLAN_CATALOG`, `MANAGE_PLAN_COMPARISON_ROWS`)

**Interfaces:**
- Essential / Pro cards per Figma; CTAs call `chooseSignupPlan(planId, "monthly")`
- Dialog: large comparison using Manage Plan rows; selecting a plan triggers same `chooseSignupPlan`
- Pilot result → `/signup/provisioning`
- Paid result → `window.location.assign(checkoutUrl)`

- [ ] **Step 1: Build UI + wire choose-plan responses**
- [ ] **Step 2: Manual Essential path to provisioning route**
- [ ] **Step 3: Commit** (only if human asks)

---

### Task 9: Pro Revolut signup pay + webhook provision

**Files:**
- Create: `backend/TummlyBackend/Services/SignupPaySessionService.cs`
- Modify: `SignupService.ChoosePlanAsync` for paid plans
- Modify: Revolut webhook applier to handle `signup_plan`
- Test: `SignupProvisionTests` paid path (fake Revolut / intent apply)

**Interfaces:**
- Paid choose-plan: set `ChosenPlan`/`ChosenCadence`, status `AwaitingPayment`, create Revolut order + `RevolutOrderIntent` with `Purpose = SignupPlan`, `PendingSignupId`, `RestaurantId = null`, return `{ mode: "checkout", checkoutUrl }`
- Webhook success for `signup_plan`: idempotent `ProvisionFromPendingAsync` with paid billing plan applied
- `GET /api/Signup/provisioning-status?sessionToken=` → `{ status, ready: bool }`

Return URL after Revolut: `/signup/provisioning` (token from sessionStorage).

- [ ] **Step 1: Failing tests for checkout intent + webhook create**
- [ ] **Step 2: Implement pay session + webhook branch**
- [ ] **Step 3: Tests PASS**
- [ ] **Step 4: Commit** (only if human asks)

---

### Task 10: Provisioning page UI

**Files:**
- Create: `src/pages/auth/SignupProvisioningPage.tsx`
- Reuse: `GuestLoopReadyStep` phase UI / `runProvisioningPhases` pattern — poll `getSignupProvisioningStatus` instead of calling setup-account

**Interfaces:**
- While `AwaitingPayment` → show confirming-payment copy + keep phases idle/loading
- When `Provisioning`/`Complete` → run phase animations then navigate `/login?setup=complete`
- On failure → retry CTA calling choose-plan again for Pilot, or re-poll for paid

- [ ] **Step 1: Implement page + poll loop**
- [ ] **Step 2: Manual Essential end-to-end to login**
- [ ] **Step 3: Commit** (only if human asks)

---

### Task 11: Retire Request Trial + register routes

**Files:**
- Modify: `src/components/home/Hero.tsx` / `HeroTrialForm.tsx` — replace form with Signup CTA (button → `/signup`)
- Modify: marketing CTAs / `scrollToRequestTrial` callers → `/signup` where appropriate
- Modify: `AppRoutes.tsx` — `register/single` and `register/multi` → `<Navigate to="/signup" replace />`
- Leave TrialController in backend for now (admin may still reference) but stop marketing entry
- Do not use `/verify-email` (AuthShell trial) for this path — Signup verify is `/signup/verify`

- [ ] **Step 1: Replace home CTA; add redirects**
- [ ] **Step 2: Manual: home → signup; old register URLs redirect**
- [ ] **Step 3: Commit** (only if human asks)

---

### Task 12: End-to-end verification checklist

- [ ] **Step 1: Automated**

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~Signup"
npx vitest run src/lib/signupPlanMap.test.ts src/lib/signupSession.test.ts
```

Expected: PASS

- [ ] **Step 2: Manual script**

1. `/signup` → email → OTP → onboarding single → Choose Essential → provisioning → login works
2. Multi path (2+ locations) reaches choose-plan
3. Pro → Revolut sandbox → return → webhook → provisioning → login
4. Cancel Revolut → back on choose-plan, retry
5. Social buttons do not navigate
6. `/register/single` redirects to `/signup`
7. Sign-in / forgot password / team invite still work

- [ ] **Step 3: Fix gaps found in checklist**
- [ ] **Step 4: Commit** (only if human asks)

---

## Spec coverage self-review

| Spec requirement | Task |
|---|---|
| Pending signup session | 1–3 |
| Signup + OTP verify APIs | 2 |
| Modal Signup/Verify UI | 6 |
| Guest Loop single/multi | 3, 7 |
| Choose plan + Manage Plan dialog | 8 |
| Essential → Pilot free create | 4, 8, 10 |
| Pro → Revolut → webhook create | 9–10 |
| Provisioning animations → login | 10 |
| Retire trial + register routes | 11 |
| Social UI only | 6 |
| Essential→Pilot / Pro→Growth map | 5 |
| Errors / idempotency | 2, 4, 9 |
| Tests | 1–5, 9, 12 |

## Consistency notes

- `OnboardingJson` schema must match what `ProvisionFromPendingAsync` reads — define one TypeScript type + C# DTO shared by save and provision.
- `RestaurantId` nullable only for `signup_plan` intents; existing intents keep restaurant required at write sites.
- Do not reuse `/verify-email` (trial/AuthShell) for this path — use `/signup/verify`.

# WS3b One Pilot start, no-plan gates, Admin extension, plan emails Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The 30-day Pilot starts at exactly one server event and is recorded once, Free accounts cannot run live Guest Loop actions, Admin can extend a Pilot within fixed limits, and Pilot / paid-plan emails go out once after their real events.

**Architecture:** A new `PilotStartService.StartPilotAsync` is the only writer of the Pilot clock (`BillingAccount.PilotStartedAtUtc` + `PilotPeriodEnd` + owner/member `ActivationExpiresAt`). Paid and Free accounts carry no clock (null expiry = "no clock" in `ActivationState`). A small `OperatorPlanGate` adds `plan_required` next to the existing billing lock gate. QR codes are minted `Paused` without a plan and activated within cap when a plan starts. Admin extension moves to fixed 14 / 30-day steps with count, total and idempotency kept on `BillingAccount` (one EF migration).

**Tech Stack:** .NET 10 / EF Core 8.0.8 (xUnit 2, InMemory, `TummlyWebApplicationFactory`), React Email (`emails/`, `npm run email:export`), React + Vite + TS (vitest), shadcn/ui.

**Spec:** `.scratch/ws3b-pilot-noplan/PRD.md` in the main checkout (`/run/media/salman/D/Freelance/Tummly/.scratch/…`; `.scratch` is gitignored). Rules R1–R6, acceptance A1–A8. Audit: `.scratch/eng-completion-oct26/audit-2026-10-26.md` § ENG03. Doc 08 copy: `/home/salman/Downloads/TUMMLY_MOHAMED_FINAL_EXECUTION_R3/02_WEBSITE_PAGE_SPECS/08_PRICING_AND_CUSTOM_PLAN_R3.docx` (extract at `/tmp/builddocs/08_pricing.txt`).

## Global Constraints

- Worktree `.worktrees/ws3b-pilot-noplan`, branch `ws3b-pilot-noplan` from `origin/qa` (7281ef72 or later). One PR to `qa`.
- This branch holds the **only** open EF migration slot. **One** migration, generated with the tool: `cd backend/TummlyBackend && DOTNET_ROLL_FORWARD=Major dotnet ef migrations add WS3bPilotStartExtension`. Never hand-write `*.Designer.cs` or the snapshot. Run `backend/TummlyBackend/scripts/check-migration-designer-pairs.sh`, `check-migrations-list-vs-files.sh`, `check-restaurant-user-setnull-fks.sh` (all must pass). Follow `CODING_STANDARDS.md` (cascade / Designer rules).
- Task 1 records the base failing lists on this branch base. Watch each new test fail before writing code. Full suites: **no new failures** against those lists.
- Exact strings (doc 08): plan gate message `Choose a Pilot or paid plan to continue.` (N09.15); Pilot start answer `When you successfully complete setup for the Pilot account you selected. Your setup confirmation shows the start and end dates. No QR scan or physical delivery is needed.` (N10.19); Pilot end answer `There is no automatic charge or paid renewal. Live features pause when the Pilot ends. Choose a paid plan to continue, with account history retained under the applicable retention rules.` (N10.05); Pilot terms `No payment card. No automatic paid renewal. Pilot allowances are once, not monthly.` (N02.46).
- Codes: `plan_required`, `pilot_already_used`, `pilot_extension_invalid_days`, `pilot_extension_not_allowed`, `pilot_extension_limit`. Audit action `operator.extend_pilot`.
- Email: `plan-started` headline / subject `Your Tummly {{plan_name}} plan has started`; body line `Your Tummly {{plan_name}} plan for {{restaurant_name}} is now active.`; same layout as `emails/emails/pilot-started.tsx` (Figma Guest-Loop-MVP 6852:41801); no end-date line.
- UI (AGENTS.md): reuse `src/components/ui/` (`Button`, `Select`, `Dialog`, `Table`, `Badge`); no raw `<button>`; a `Select` inside a `Dialog` must render above the dialog (portal + z-index as in existing dialogs). Font family per `docs/agents/typography.md` (functional, WebApp).
- WS3a (parallel branch) also edits `src/content/marketing/pricingPage.ts` and `faqsPage.ts` (price lines). Touch only the Pilot entries named in Task 10 to keep the merge small.
- Do not log a full email or phone. Frontend style: no semicolons, double quotes. Do not run `npx prettier`.

## Review Focus

1. **Free account that already used a Pilot asks for Pilot again** (e.g. Pilot ended, account moved to Free). It must be refused with `pilot_already_used`, no new clock, no new credits. Test: Task 4 `StartPilot_AlreadyUsed_OnFree_RefusesWithoutChange`.
2. **Paid account that converted from Pilot, after the old Pilot end date.** The kept `PilotPeriodEnd` must not soft-lock it on the next lifecycle tick and the owner must not get Pilot-ending emails. Test: Task 6 `Tick_PaidAfterPilot_KeptEndDate_StaysActive` and `ActivationNotifications_SkipConvertedOwner`.
3. **Plan start on an account whose operator paused capture at a Location.** Plan start must not re-activate codes the operator paused (Location capture `Paused`), and must stop at the per-Location cap. Test: Task 4 `ActivatePlanPausedCodes_SkipsPausedLocation_StopsAtCap`.
4. **Two Admin clicks on "Extend" (double submit, same key) or a retry after a timeout.** Exactly one extension. Test: Task 8 `ExtendPilot_SameKeyTwice_ExtendsOnce`.
5. **Team member of a Pilot account.** Members copy the owner's `ActivationExpiresAt`; after extension or conversion the member value must follow, or the member gets wrong Pilot notices. Test: Task 4 `StartPilot_SyncsOwnerAndMembersClock` and Task 8 `ExtendPilot_SyncsMembersClock`.

---

## File map

| File | Responsibility |
|---|---|
| `backend/TummlyBackend/Models/BillingAccount.cs` | New fields `PilotStartedAtUtc`, `PilotExtensionCount`, `PilotExtensionDaysTotal`, `LastPilotExtensionKey` |
| `backend/TummlyBackend/Migrations/<ts>_WS3bPilotStartExtension*.cs` | Generated migration + backfill SQL |
| `backend/TummlyBackend/Services/ActivationState.cs`, `ActivationGate.cs` | Null expiry = no Pilot clock (not "expired") |
| `backend/TummlyBackend/Interfaces/IPilotStartService.cs`, `Services/PilotStartService.cs` (new) | Only Pilot-clock writer + pilot-started email |
| `backend/TummlyBackend/Helpers/OperatorPlanGate.cs` (new) | `plan_required` check + message |
| `backend/TummlyBackend/Services/CaptureQrLifecycleService.cs` | Activate plan-paused codes within cap; lock gate on restore / activate-capture |
| `backend/TummlyBackend/Services/QrCodeProvisioningService.cs` | Mint `Paused` without plan |
| `backend/TummlyBackend/Services/GuestLoopProvisioningService.cs`, `SignupService.cs`, `BillingCreditsService.cs`, `AuthService.cs` | Callers rewired to `StartPilotAsync`; no clock for paid / Free; activation code stops Pilot |
| `backend/TummlyBackend/Services/RevolutOrderCompletedApplier.cs`, `BillingAccountLifecycleService.cs` | Keep Pilot history; clear user clocks; plan-started email; QR activation |
| `emails/emails/plan-started.tsx`, `backend/TummlyBackend/Helpers/EmailTemplates/PlanStartedEmailTemplate.cs`, `Assets/emails/templates/plan-started.html` (export) | Paid welcome email |
| `backend/TummlyBackend/Services/AdminService.cs`, `Controllers/AdminController.cs`, `DTOs/Admin/ExtendActivationDto.cs`, `DTOs/Admin/AdminPilotAccountDto.cs` (new) | Bounded extension + Pilot list |
| `src/components/dashboard/admin/AdminPilotsPanel.tsx` (new), `Dashboard.tsx`, `src/api/adminApi.ts`, `src/types/admin.ts`, `src/lib/admin/adminPilotExtensionPresentation.ts` (new) | Admin Pilots tab |
| `src/content/marketing/pricingPage.ts`, `faqsPage.ts`, `src/lib/operatorBillingCredits/managePlanPresentation.ts`, `src/content/helpCentre/articles.ts` | Copy |

### Readers of `ActivationExpiresAt` / `PilotPeriodEnd` (checked on `qa` 7281ef72)

| Reader | Today | After WS3b |
|---|---|---|
| `Services/ActivationState.cs:22-28` `IsActivationExpired` | `ActivatedAt` set + null expiry = **expired** | null expiry = not expired (no clock) — Task 3 |
| `Services/ActivationGate.cs:27-33` | null expiry → **Block** "Your 30 day free trial is over" | remove that branch; allow — Task 3 |
| `Services/ActivationNotificationProducer.cs:46-56` | all users with expiry get Pilot notices (paid owners too) | paid / Free have null → skipped; converted owner cleared — Tasks 3, 6 |
| `Services/BillingAccountLifecycleService.cs:315-335` `EnsurePilotPeriodEndAsync` | Pilot with null end copies owner expiry | unchanged (StartPilot sets both) |
| `BillingAccountLifecycleService.cs:356` `AdvanceUnpaidPilot` | gated on plan == Pilot | unchanged; a kept end on paid plan is ignored |
| `BillingAccountLifecycleService.cs:~150` `ActivatePaidPlanAsync`, `RevolutOrderCompletedApplier.cs:509, 569` `ApplyPaymentFields` / custom cycle, `BillingCreditsService.cs:1897` `ApplyPaidSignupBilling` | set `PilotPeriodEnd = null` | keep value (history) — Task 6 |
| `BillingCreditsService.cs:156-161` Manage Plan label | `PilotPeriodEnd ?? owner expiry`, only when Pilot | shows start + end — Task 10 |
| `CreditThresholdEvaluator.cs:125` | reset label, only when `isPilot` | unchanged |
| `Helpers/BillingPlanSnapshotHelper.cs:20-31, 66+`, `AccountWorkspaceService.cs:559`, `RestaurantController.cs:270` | legacy name-based fallback only when no `BillingAccount` | unchanged (every account has one) |
| `TeamInvitationAcceptService.cs:329, 387` | member copies owner expiry | unchanged; owner/member sync in Tasks 4, 6, 8 |
| `AuthService.cs:1089-1117, 1463` session payload; `AdminService.cs:338` admin DTO | echo value | unchanged (null for paid / Free) |
| Frontend `src/lib/operatorHome/activationPeriod.ts:26-40` | null → badge hidden | unchanged |

---

### Task 1: Worktree and baseline

**Files:** none in product code. Create `.scratch/ws3b-pilot-noplan/baseline-failing-backend-<sha>.txt` and `baseline-failing-frontend-<sha>.txt` in the main checkout.

- [ ] **Step 1:** `git worktree add .worktrees/ws3b-pilot-noplan -b ws3b-pilot-noplan origin/qa`; record the base SHA.
- [ ] **Step 2:** In the worktree: `cd backend && dotnet test TummlyBackend.Tests 2>&1 | tee /tmp/ws3b-base-backend.log`; write the sorted failed test names (`grep -E "^\s+Failed " … | sed 's/^\s*Failed //;s/ \[.*//' | sort -u`) to the backend base file. Expected ≈ 118 names (the qa list after #70).
- [ ] **Step 3:** `npm ci && npx vitest run 2>&1 | tee /tmp/ws3b-base-frontend.log`; write failed test names to the frontend base file (expected: `assistantWaitGerunds` only).
- [ ] **Step 4:** No commit (files are in `.scratch`).

### Task 2: Pilot fields and migration

**Files:**
- Modify: `backend/TummlyBackend/Models/BillingAccount.cs` (after `PilotPeriodEnd`, line ~150)
- Create (generated): `Migrations/<ts>_WS3bPilotStartExtension.cs`, `.Designer.cs`, snapshot
- Test: `backend/TummlyBackend.Tests/Services/BillingAccountPilotFieldsTests.cs`

**Interfaces:**
- Produces: `DateTime? PilotStartedAtUtc`; `int PilotExtensionCount` (default 0); `int PilotExtensionDaysTotal` (default 0); `string? LastPilotExtensionKey` (`[MaxLength(64)]`).

- [ ] **Step 1: Failing test** `BillingAccount_NewPilotFields_DefaultToEmpty`: new account → `PilotStartedAtUtc == null`, counts 0, key null; round-trips through InMemory save/load.
- [ ] **Step 2: Run** → FAIL (compile).
- [ ] **Step 3: Add the fields.** Generate the migration. Add to `Up` after the column adds:
  `migrationBuilder.Sql("UPDATE BillingAccounts SET PilotStartedAtUtc = DATEADD(day, -30, PilotPeriodEnd) WHERE PilotStartedAtUtc IS NULL AND PilotPeriodEnd IS NOT NULL AND SubscriptionPlan = N'Pilot'");`
  Read the generated `Up` / `Down`: only 4 column adds + the backfill; `Down` drops the 4 columns.
- [ ] **Step 4: Run** the test → PASS; run the 3 migration scripts → pass.
- [ ] **Step 5: Commit** `feat: Pilot start and extension fields (WS3b migration)`.

### Task 3: No Pilot clock for paid and Free; activation code stops starting a Pilot

**Files:**
- Modify: `Services/ActivationState.cs:16-28`, `Services/ActivationGate.cs:20-35`
- Modify: `Services/GuestLoopProvisioningService.cs:735-737` (`CreateOperatorAccountAsync` user init)
- Modify: `Services/AuthService.cs:1222-1252` (`ActivateAccountAsync`)
- Test: `Services/ActivationStateTests.cs`, `Services/ActivationGateTests.cs`, `Services/GuestLoopProvisioningServiceTests.cs`, `Services/ActivationNotificationProducerTests.cs`, new `Services/AuthServiceActivateAccountTests.cs` (or the existing activation test file)

**Interfaces:**
- Produces: `ActivationState.IsActivationExpired(s) = s.ActivatedAt != null && s.ActivationExpiresAt.HasValue && s.ActivationExpiresAt.Value <= DateTime.UtcNow`; `IsWithinActivationPeriod(s) = s.ActivatedAt != null && (!s.ActivationExpiresAt.HasValue || s.ActivationExpiresAt.Value > DateTime.UtcNow)`.

- [ ] **Step 1: Change / add tests**
  - Rename `IsActivationExpired_WhenActivatedWithoutExpires_TreatsAsExpired` → `IsActivationExpired_WhenActivatedWithoutExpires_IsFalse` (assert false).
  - `GetStatusDetail_ActivatedWithoutExpires_ReturnsExpired` → `…_ReturnsActive` (assert `"active"`).
  - `Decide_ActivatedWithoutExpires_BlocksAsExpired` → `Decide_ActivatedWithoutExpires_Allows` (Outcome Allow, Reason None).
  - `ProvisionFromPending_PaidPlan_NoActivationExpiry`: paid pending signup → owner `ActivatedAt != null`, `ActivationExpiresAt == null`; `BillingAccount.PilotStartedAtUtc == null`, `PilotPeriodEnd == null`.
  - `ProvisionFromPending_FreePlan_NoActivationExpiry`: same for Free.
  - `ActivateAccount_DoesNotMintPilotOrSetExpiry`: user with `ActivatedAt == null`, valid code → `ActivatedAt` set, `ActivationExpiresAt == null`, no `MintPilotAtActivationAsync` call (fake ledger records zero calls).
  - `Process_SkipsUsersWithoutExpiry` (notification producer): owner with null expiry → no notice.
- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3: Implement.** `ActivationState` as above; `ActivationGate.Decide` drops the `!HasValue → Block` branch (keep the `Expired → Allow` path). `CreateOperatorAccountAsync`: `ActivationExpiresAt = null` at user creation (the Pilot path sets it in Task 4). `ActivateAccountAsync`: set `ActivatedAt` only; remove the expiry write and the per-restaurant mint loop (keep cache / code checks and `TickSelectedRestaurantAsync`).
- [ ] **Step 4: Run** the five test classes → PASS; `--filter "FullyQualifiedName~Activation|FullyQualifiedName~Signup|FullyQualifiedName~Provision"` → no new failures.
- [ ] **Step 5: Commit** `fix: paid and Free accounts have no Pilot clock; activation code does not start a Pilot`.

### Task 4: One Pilot start event

**Files:**
- Create: `Interfaces/IPilotStartService.cs`, `Services/PilotStartService.cs`; register in `Program.cs` (scoped, next to `IActivationGate` ~1264)
- Modify: `Services/CaptureQrLifecycleService.cs` (+ `Interfaces/ICaptureQrLifecycleService.cs`) — new method
- Modify callers: `Services/GuestLoopProvisioningService.cs` (`ApplyPilotSignupBillingAsync` 352-416 → thin wrapper; trial path in `CreateOperatorAccountAsync` 850-873; post-commit email 939-946), `Services/BillingCreditsService.cs:557-618` (Free → Pilot), `Services/SignupService.cs:283-300` (`RetryProvisionAsync`), `Interfaces/IProvisioningService.cs`
- Test: new `Services/PilotStartServiceTests.cs`; `Services/GuestLoopProvisioningServiceTests.cs`; `Services/SignupServiceTests.cs`; `Services/BillingCreditsServiceTests.cs`; `Services/CaptureQrLifecycleServiceTests.cs`

**Interfaces:**
- Produces: `public sealed record PilotStartResult(bool Started, string? RefusedCode, DateTime? StartedAtUtc, DateTime? EndsAtUtc)`.
- Produces: `IPilotStartService.StartPilotAsync(int restaurantId, CancellationToken cancellationToken = default) -> Task<PilotStartResult>` and `IPilotStartService.NotifyStartedAsync(int restaurantId, PilotStartResult result, CancellationToken cancellationToken = default) -> Task` (sends pilot-started only when `result.Started`; swallows send errors like today's `TrySendPilotStartedEmailAsync`).
- Produces: `ICaptureQrLifecycleService.ActivatePlanPausedCodesAsync(int restaurantId, CancellationToken cancellationToken = default) -> Task<int>` (count activated).
- Produces: `IProvisioningService.EnsurePilotForPendingAsync(Guid pendingSignupId) -> Task`.
- Consumes: `ICreditLedger.MintPilotAtActivationAsync(int restaurantId)` (existing; `pilot_already_minted` = OK); `IComplimentaryStarterShopOrderService.EnsureAllActiveLocationsAsync` + `IPrintReadyQrMaterialsWork.RequestShopOrderEnsureAsync` (moved from the 3 callers into the service, after commit).

Rules in `StartPilotAsync`:
1. Load `BillingAccount` for update (reuse the UPDLOCK pattern of `BillingAccountLifecycleService.LoadForUpdateAsync`).
2. `PilotStartedAtUtc != null && SubscriptionPlan == Pilot` → `Started=false, RefusedCode=null`, existing dates (idempotent).
3. `PilotStartedAtUtc != null && SubscriptionPlan != Pilot` → `RefusedCode="pilot_already_used"`, no change.
4. Else: `BillingCreditsService.ApplyPilotSignupBilling(account, now)`; `PilotStartedAtUtc = now`; owner **and** all active members of the restaurant: `ActivationExpiresAt = PilotPeriodEnd`; save; mint Pilot credits (`pilot_already_minted` accepted, other failure → throw); `ActivatePlanPausedCodesAsync`.
5. Join `_context.Database.CurrentTransaction` when present (trial path runs inside `CreateOperatorAccountAsync`'s transaction); else open and commit its own.

Rules in `ActivatePlanPausedCodesAsync`: codes with `Status == Paused` at Locations with `CaptureLocationStatus == Active` and not listed in `CaptureLocationPauseRestoreQrCodeIdsJson`; oldest `CreatedAt` first; stop at the per-Location cap from `TryResolveActiveQrCap` (count existing Active first). Returns 0 when the cap is unresolvable.

- [ ] **Step 1: Failing tests** (`PilotStartServiceTests`)
  - `StartPilot_FirstTime_SetsStartEndCreditsOnce`: Free account → `Started`, `PilotStartedAtUtc == now`, `PilotPeriodEnd == now+30d`, plan `Pilot`, status `Pilot`, `RenewalDateUtc == null` (R1.8, no automatic paid renewal), one Pilot credit mint.
  - `StartPilot_Twice_ReturnsSameDatesNoSecondMint`.
  - `StartPilot_AlreadyUsed_OnFree_RefusesWithoutChange` (Review Focus 1): `PilotStartedAtUtc` set, plan Free → `RefusedCode == "pilot_already_used"`, plan stays Free, no mint.
  - `StartPilot_SyncsOwnerAndMembersClock` (Review Focus 5).
  - `StartPilot_InsideAmbientTransaction_DoesNotOpenSecond` (InMemory: assert no exception and data visible after outer commit; SQL Server lane optional).
  - `NotifyStarted_SendsOnlyWhenStarted`: fake `IEmailService` → 1 call for `Started=true`, 0 for idempotent / refused.
- [ ] **Step 2: Failing tests** (`CaptureQrLifecycleServiceTests`)
  - `ActivatePlanPausedCodes_ActivatesWithinCap`: Pilot cap 5 per Location, 7 paused codes → 5 Active.
  - `ActivatePlanPausedCodes_SkipsPausedLocation_StopsAtCap` (Review Focus 3).
- [ ] **Step 3: Failing caller tests**
  - `ProvisionFromPending_Pilot_StartsPilotOnce` (GuestLoopProvisioningServiceTests): Pilot pending → start set; pilot-started email sent once.
  - `RetryProvision_CompletePilotWithoutStart_StartsPilot` (SignupServiceTests): pending `Complete`, Pilot chosen, `PilotStartedAtUtc == null` → after retry start set; second retry → unchanged.
  - `ProvisionTrialInvite_StartsPilot` (GuestLoopProvisioningServiceTests, `ProvisionAsync`).
  - `ChangePlan_FreeToPilot_UsesStartPilot` and `ChangePlan_FreeToPilot_AlreadyUsed_Throws409Code` (BillingCreditsServiceTests): second asserts `InvalidOperationException` message `pilot_already_used` (controller maps it to 409 — check `BillingCreditsController` plan-change catch and add the mapping if it is not generic).
- [ ] **Step 4: Run** → FAIL.
- [ ] **Step 5: Implement** the service, the QR method and the 4 caller changes. `ApplyPilotSignupBillingAsync` becomes: resolve restaurant → `StartPilotAsync` → `NotifyStartedAsync`. Trial path: replace the inline `ApplyPilotSignupBilling` + mint with `StartPilotAsync` inside the transaction; call `NotifyStartedAsync` after commit (replace `provisionedPilotEnd` logic). Delete the now-unused private `TrySendPilotStartedEmailAsync` copies (GuestLoopProvisioningService 418-442, BillingCreditsService 2548-2580).
- [ ] **Step 6: Run** the new and touched classes → PASS; `--filter "FullyQualifiedName~Pilot|FullyQualifiedName~Signup|FullyQualifiedName~Provision|FullyQualifiedName~BillingCredits|FullyQualifiedName~CaptureQr"` → no new failures.
- [ ] **Step 7: Commit** `feat: one Pilot start event (StartPilotAsync)`.

### Task 5: `plan-started` email template

**Files:**
- Create: `emails/emails/plan-started.tsx` (copy structure of `pilot-started.tsx`)
- Create (export): `backend/TummlyBackend/Assets/emails/templates/plan-started.html` via `npm run email:export`
- Create: `backend/TummlyBackend/Helpers/EmailTemplates/PlanStartedEmailTemplate.cs`
- Modify: `Interfaces/IEmailService.cs` (~195), `Services/EmailService.cs` (after `SendPilotStartedEmailAsync` ~1058-1082), every `IEmailService` fake in tests (compile-driven)
- Modify: `emails/README.md` table (one row)
- Test: `backend/TummlyBackend.Tests/Helpers/ReactEmailTemplateTests.cs`

**Interfaces:**
- Produces: `PlanStartedEmailTemplate.Subject(string planName) -> string` = `$"Your Tummly {planName} plan has started"`; `PlanStartedEmailTemplate.Generate(IWebHostEnvironment environment, string firstName, string planName, string restaurantName, string dashboardUrl, string privacyUrl, string companyDetailsUrl, string logoUrl) -> string`.
- Produces: `IEmailService.SendPlanStartedEmailAsync(string toEmail, string firstName, string planName, string restaurantName) -> Task`.
- Template tokens: `{{first_name}}`, `{{plan_name}}`, `{{restaurant_name}}`, `{{dashboard_url}}`, `{{privacy_url}}`, `{{company_details_url}}`, `{{logo_url}}`. Body = pilot-started body with the two Pilot lines replaced by the Global Constraints copy and the "Your Pilot ends on" line removed.

- [ ] **Step 1: Failing test** `PlanStarted_Generate_FillsPlanNameAndRestaurant`: `planName "Growth"`, restaurant `Mehmet's Grill` → contains `Your Tummly Growth plan has started`, `Your Tummly Growth plan for Mehmet&#39;s Grill is now active.`, `Go to Tummly`; does not contain `{{`, `Pilot ends`, `30-day`. `Subject("Group") == "Your Tummly Group plan has started"`.
- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3:** Write the TSX, run `npm run email:export`, write the C# template (same `ReactEmailHtmlLoader.Load` pattern), add the `EmailService` method.
- [ ] **Step 4: Run** the test → PASS; `npm run email:export` leaves no other diff (`git status` shows only the new html).
- [ ] **Step 5: Commit** `feat: plan-started email for paid plans`.

### Task 6: Paid activation keeps Pilot history, clears clocks, activates QR, sends plan-started once

**Files:**
- Modify: `Services/RevolutOrderCompletedApplier.cs` — `ApplyPaymentFields` (~486-512), `ApplyCustomPlanCycleAsync` (~529-650), `ApplyAsync` setup branch (~238-262)
- Modify: `Services/BillingAccountLifecycleService.cs` `ActivatePaidPlanAsync` (~145-160); `Services/BillingCreditsService.cs` `ApplyPaidSignupBilling` (~1870-1905)
- Create: `Helpers/PilotClockSync.cs`
- Test: `Services/RevolutOrderCompletedApplierTests.cs`, `Services/BillingAccountLifecycleServiceTests.cs`, `Services/ActivationNotificationProducerTests.cs`

**Interfaces:**
- Produces: `PilotClockSync.ClearForRestaurantAsync(ApplicationDbContext context, int restaurantId, CancellationToken cancellationToken) -> Task` (owner + members: `ActivationExpiresAt = null`; no save) and `PilotClockSync.SetForRestaurantAsync(ApplicationDbContext context, int restaurantId, DateTime endsAtUtc, CancellationToken cancellationToken) -> Task` (Task 4 and Task 8 use it).
- Consumes: `IEmailService.SendPlanStartedEmailAsync` (Task 5); `ICaptureQrLifecycleService.ActivatePlanPausedCodesAsync` (Task 4).

- [ ] **Step 1: Failing tests**
  - `Apply_SetupFromPilot_KeepsPilotStartAndEnd`: Pilot account (start, end set) → setup payment → plan Growth, `PilotStartedAtUtc` and `PilotPeriodEnd` unchanged, owner `ActivationExpiresAt == null`.
  - `Apply_CustomFirstPaid_KeepsPilotHistory` (custom cycle path).
  - `Tick_PaidAfterPilot_KeptEndDate_StaysActive` (Review Focus 2): plan Growth, status Active, `PilotPeriodEnd` in the past → `TickAsync` → status `Active`, `SoftLockEnteredAt == null`.
  - `ActivationNotifications_SkipConvertedOwner` (Review Focus 2).
  - `Apply_Setup_SendsPlanStartedOnce`: setup → one `SendPlanStartedEmailAsync("…", …, "Growth", …)`; redelivery of the same order (claim already applied) → still one; renewal `cycle_billing` with closed pending → zero; top-up / Shop intents → zero.
  - `Apply_Setup_ActivatesPausedQrWithinCap`: Free account with Paused codes → setup Growth → codes Active within Growth cap.
- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3: Implement.** Remove `PilotPeriodEnd = null` from `ApplyPaymentFields`, `ApplyCustomPlanCycleAsync`, `ActivatePaidPlanAsync`, `ApplyPaidSignupBilling`; call `PilotClockSync.ClearForRestaurantAsync` in the same save. In the `isSetup` branch (after `SaveChangesAsync`, before the included mint) call `ActivatePlanPausedCodesAsync`; after the invoice email send `SendPlanStartedEmailAsync` only when `isSetup` and the pending session was open before this apply (capture `var wasOpen = pending.IsOpen` before `ApplyPaymentFields`). Custom path: same, only when `isFirstPaid`. Plan name = `billingAccount.SubscriptionPlan` (`Starter` / `Growth` / `Group` / `Custom`). Email errors are caught and logged (type only), never thrown.
- [ ] **Step 4: Run** → PASS; `--filter "FullyQualifiedName~Revolut|FullyQualifiedName~Lifecycle|FullyQualifiedName~Activation"` → no new failures.
- [ ] **Step 5: Commit** `feat: paid activation keeps Pilot history and sends plan-started once`.

### Task 7: Free (no-plan) gates

**Files:**
- Create: `Helpers/OperatorPlanGate.cs`
- Modify: `Helpers/OperatorBillingLockGate.cs` (`Forbidden` message mapping, line 87-99)
- Modify: `Services/QrCodeProvisioningService.cs:23-62` (+ callers compile only: `GuestLoopProvisioningService.cs:835`, `OwnedLocationInsertService.cs:244`, `LocationsLifecycleWriteService.cs:92`, `PrintReadyQrMaterialsService.cs:76`)
- Modify: `Services/CampaignDraftService.cs` (`CreateAsync` ~45-82, `PatchAsync` ~178-212), `Services/OffersCatalogService.cs` (`CreateDraftAsync` 112-142, `UpdateAsync` 179-196, `ResumeAsync` 805-820, `DuplicateAsync` 921-938 + the create-active path used by `OffersController.cs:144`), `Services/CampaignScheduleCommitService.cs:495-512`, `Services/ClearCampaignSendStartGate.cs:~79`, `Services/BilledAiActionCoordinator.cs:~56-66` + `Helpers/BilledAiActionHttpResponses.cs:47-56`, the Assistant pre-check in `Services/AssistantConversationService.cs` (~9097-9150), `Controllers/CaptureLocationsController.cs:233-300` (thank-you Offer save, `FailClosed` branch)
- Modify: `Services/CaptureQrLifecycleService.cs` `RestoreAsync` 417+ and `ActivateLocationCaptureAsync` 550+ (add `EvaluatePaidWriteDenyAsync` like `CreateDigitalGuestLinkAsync` 115-123)
- Test: new `Helpers/OperatorPlanGateTests.cs`; `Services/QrCodeProvisioningServiceTests.cs`; `Integration/CampaignDraftEndpointsTests.cs`; `Integration/OffersCatalogEndpointsTests.cs`; `Services/CaptureQrLifecycleServiceTests.cs`

**Interfaces:**
- Produces: `OperatorPlanGate.PlanRequiredCode = "plan_required"`, `OperatorPlanGate.PlanRequiredMessage = "Choose a Pilot or paid plan to continue."`, `OperatorPlanGate.HasPlan(BillingAccount account) -> bool` (`SubscriptionPlan != BillingSubscriptionPlans.Free`), `OperatorPlanGate.EnsurePlanAndPaidWriteForLocationAsync(ApplicationDbContext context, int locationId, CancellationToken cancellationToken = default) -> Task` (throws `OperatorBillingLockedException(PlanRequiredCode)` when no plan, else delegates to `OperatorBillingLockGate.EnsurePaidWriteAllowedForLocationAsync`), `OperatorPlanGate.EvaluatePlanDeny(BillingAccount account) -> string?`.
- `OperatorBillingLockGate.Forbidden(code)` and `BilledAiActionHttpResponses` set `message = code == OperatorPlanGate.PlanRequiredCode ? OperatorPlanGate.PlanRequiredMessage : code`.
- `QrCodeProvisioningService.MintDefaultQrCodesAsync(RestaurantLocation location)` keeps its signature; it loads the restaurant's `BillingAccount` (via `location.RestaurantId` or `location.Restaurant`; tracked entity during signup) and sets `Status = HasPlan ? Active : Paused`.

- [ ] **Step 1: Failing tests**
  - `MintDefault_FreeAccount_MintsPaused`, `MintDefault_PilotAccount_MintsActive` (QrCodeProvisioningServiceTests).
  - `CreateDraft_FreePlan_Returns403PlanRequired` (CampaignDraftEndpointsTests): body `code == "plan_required"`, `message == "Choose a Pilot or paid plan to continue."`.
  - `CreateOffer_FreePlan_Returns403PlanRequired`, `UpdateOffer_FreePlan_Returns403PlanRequired` (OffersCatalogEndpointsTests).
  - `SetThankYouOffer_FreePlan_Returns403PlanRequired` (replaces today's empty 409; WS1 QA side finding).
  - `ScheduleCommit_FreePlan_DeniedPlanRequired`; `BilledAi_FreePlan_ReturnsPlanRequired` (coordinator unit).
  - `FreePlan_ReadEndpoints_StillOk`: GET feedback list, guests list, Billing overview → 200 (R3.5).
  - `Restore_SoftLock_DeniedBillingLocked`, `ActivateLocationCapture_SoftLock_DeniedBillingLocked` (CaptureQrLifecycleServiceTests, R2.1).
- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3: Implement** the gate and wire every listed write path (replace `EnsurePaidWriteAllowedForLocationAsync` with `EnsurePlanAndPaidWriteForLocationAsync` in Campaign draft and Offer writes only — exports keep the old call). Send / AI deny: return `plan_required` before the lock code when `!HasPlan`.
- [ ] **Step 4: Run** → PASS; `--filter "FullyQualifiedName~Campaign|FullyQualifiedName~Offer|FullyQualifiedName~Qr|FullyQualifiedName~Assistant|FullyQualifiedName~BilledAi"` → no new failures (update existing tests that seed a Free account but expect a write to succeed: seed `Pilot` instead and note each in the commit message).
- [ ] **Step 5: Commit** `feat: Free accounts get paused QR codes and plan_required on live writes`.

### Task 8: Admin Pilot extension (backend)

**Files:**
- Modify: `DTOs/Admin/ExtendActivationDto.cs` → `{ int Days; string? IdempotencyKey }` (remove `ExpiresAt`)
- Create: `DTOs/Admin/AdminPilotAccountDto.cs`
- Modify: `Services/BillingAccountLifecycleService.cs` (replace `ExtendPilotActivationAsync` 176-215; + interface `IBillingAccountLifecycle`, `NoOpBillingAccountLifecycle` test double)
- Modify: `Services/AdminService.cs:467-530` (`ExtendActivationAsync` → `ExtendPilotAsync`), new `ListPilotAccountsAsync`; interface
- Modify: `Controllers/AdminController.cs:203-260` (+ new `GET pilot-accounts`)
- Modify: `Models/AdminAuditEvent.cs:33` (add `OperatorExtendPilot = "operator.extend_pilot"`; keep the old constant for history)
- Test: `Services/BillingAccountLifecycleServiceTests.cs`, new `Integration/AdminPilotExtensionEndpointsTests.cs`

**Interfaces:**
- Produces: `IBillingAccountLifecycle.ExtendPilotAsync(int restaurantId, int days, string? idempotencyKey, DateTime now, CancellationToken cancellationToken = default) -> Task<PilotExtensionResult>`; `public sealed record PilotExtensionResult(string Outcome, string? Code, DateTime? OldEndUtc, DateTime? NewEndUtc, int CountAfter, int DaysTotalAfter)` with `Outcome` ∈ `"extended" | "replayed" | "refused"`.
- Produces: `IAdminService.ExtendPilotAsync(int userId, ExtendActivationDto dto, int? actorAdminUserId, string actorIdentity) -> Task<PilotExtensionResult?>` (null = operator / restaurant not found); `IAdminService.ListPilotAccountsAsync() -> Task<IReadOnlyList<AdminPilotAccountDto>>`.
- `AdminPilotAccountDto { int RestaurantId; int OwnerUserId; string RestaurantName; string OwnerEmail; string BillingStatus; DateTime? PilotStartedAtUtc; DateTime? PilotPeriodEnd; int ExtensionCount; int ExtensionDaysTotal; bool CanExtend; string? CannotExtendReason }` (accounts with `SubscriptionPlan == Pilot`, newest start first).
- HTTP: `POST /api/admin/operators/{userId}/extend-activation` body `{ days, idempotencyKey }` → 200 `{ success: true, data: AdminPilotAccountDto }`; 400 `pilot_extension_invalid_days`; 409 `pilot_extension_not_allowed` / `pilot_extension_limit` with `message`; 404 not found. `GET /api/admin/pilot-accounts` → `{ success: true, data: [...] }`. Both Admin role only (class attribute); Support → 403.

Rules: days ∈ {14, 30}. Allowed only when plan `Pilot` and status `Pilot` or `Soft lock` (Dormant, paid, Free → `pilot_extension_not_allowed`). `CountAfter ≤ 2` and `DaysTotalAfter ≤ 90` else `pilot_extension_limit`. Same `idempotencyKey` as `LastPilotExtensionKey` → `replayed`, no change. New end = `max(PilotPeriodEnd, now) + days`; status back to `Pilot`; clear `SoftLockEnteredAt`, `PilotSoftLockNotified`, `PilotDormantNotified`; `PilotStartedAtUtc` unchanged; `PilotClockSync.SetForRestaurantAsync(newEnd)`. Audit only on `extended`: `DetailJson = {"days":14,"countAfter":1,"totalAfter":14,"oldEnd":"…","newEnd":"…"}` (ISO 8601 UTC).

- [ ] **Step 1: Failing tests** (lifecycle unit): `ExtendPilot_Active14_MovesEndFromCurrentEnd`; `ExtendPilot_SoftLocked30_MovesEndFromNow_LiftsLock`; `ExtendPilot_InvalidDays7_Refused`; `ExtendPilot_Third_RefusedLimit`; `ExtendPilot_TotalOver90_RefusedLimit` (30+30 then 30 → count rule hits first; also seed count 1, total 75, then 30 → limit); `ExtendPilot_Dormant_RefusedNotAllowed`; `ExtendPilot_PaidPlan_RefusedNotAllowed`; `ExtendPilot_SameKeyTwice_ExtendsOnce` (Review Focus 4); `ExtendPilot_KeepsStart`; `ExtendPilot_SyncsMembersClock` (Review Focus 5).
- [ ] **Step 2: Failing tests** (endpoints): `Extend_AsAdmin_200_WritesOneAuditRow` (action `operator.extend_pilot`, detail has `countAfter` 1, no email in detail); `Extend_AsSupport_403`; `Extend_Days7_400`; `Extend_Third_409`; `ListPilotAccounts_ReturnsPilotOnly_WithCanExtend`.
- [ ] **Step 3: Run** → FAIL.
- [ ] **Step 4: Implement.** Remove `ActivationCodeHelper.ComputeDefaultExtensionExpiresAt` if unused after the change.
- [ ] **Step 5: Run** → PASS; `--filter "FullyQualifiedName~Admin|FullyQualifiedName~Lifecycle"` → no new failures.
- [ ] **Step 6: Commit** `feat: bounded Admin Pilot extension (14/30 days, max two, 90 total)`.

### Task 9: Admin Pilots tab (frontend)

**Files:**
- Modify: `src/types/admin.ts` (replace `ExtendActivationPayload` with `{ days: 14 | 30; idempotencyKey: string }`; add `AdminPilotAccount`)
- Modify: `src/api/adminApi.ts:182-194` (`extendActivation` → `extendPilot`), add `getPilotAccounts`
- Create: `src/lib/admin/adminPilotExtensionPresentation.ts` + `.test.ts`
- Create: `src/components/dashboard/admin/AdminPilotsPanel.tsx`
- Modify: `src/components/dashboard/admin/Dashboard.tsx` (new tab `pilots`, label `Pilots`)

**Interfaces:**
- Produces: `getPilotAccounts(): Promise<AdminPilotAccount[]>`; `extendPilot(userId: number, payload: { days: 14 | 30; idempotencyKey: string }): Promise<AdminPilotAccount>`.
- Produces: `buildPilotRowPresentation(account: AdminPilotAccount, now: Date): { startLabel: string; endLabel: string; extensionsLabel: string; canExtend: boolean; disabledReason: string | null }` — labels `d MMM yyyy` en-GB; `extensionsLabel` = `"1 of 2 · 14 of 90 days"`; `disabledReason` from server `cannotExtendReason`.

- [ ] **Step 1: Failing vitest** `adminPilotExtensionPresentation.test.ts`: allowed row → `canExtend true`, `disabledReason null`, `extensionsLabel "0 of 2 · 0 of 90 days"`; limit row → `canExtend false`, reason text passed through; null dates → `"—"`.
- [ ] **Step 2: Run** `npx vitest run src/lib/admin/adminPilotExtensionPresentation.test.ts` → FAIL.
- [ ] **Step 3: Implement** helper, API, panel: `Table` of accounts (restaurant, owner email, status `Badge`, start, end, extensions); per row `Button` "Extend Pilot" (disabled + `Tooltip` reason when not allowed) → `Dialog` with `Select` (14 days / 30 days; content renders above the dialog), "Extend" `Button`; one `crypto.randomUUID()` key per dialog open; success `toast` + row refresh; error `toast` with server `message`.
- [ ] **Step 4: Run** the test → PASS; `npx tsc --noEmit` → no new errors; `npm run build` → OK.
- [ ] **Step 5: Commit** `feat: Admin Pilots tab with bounded Extend Pilot`.

### Task 10: Pilot copy and Manage Plan dates

**Files:**
- Modify: `src/content/marketing/pricingPage.ts:603-614` (`pilot-start`, `pilot-end` answers), `src/content/marketing/faqsPage.ts:126-131` (`when-pilot-starts` answer) and its `when-pilot-ends` answer, `src/lib/operatorBillingCredits/managePlanPresentation.ts:378-385` (`pilot-start` paragraphs), `src/content/helpCentre/articles.ts:133-148` (`activation-code` article)
- Modify: `backend/TummlyBackend/Services/BillingCreditsService.cs:156-161` (renewal label) + `DTOs/BillingCredits/BillingCreditsDtos.cs` (add `PilotStartedAtUtc`, `PilotEndsAtUtc`)
- Test: new `src/content/marketing/pilotCopy.test.ts`; `Services/BillingCreditsServiceTests.cs`

- [ ] **Step 1: Failing vitest** `pilotCopy.test.ts`: the `pilot-start` / `when-pilot-starts` answers equal the N10.19 string exactly; the `pilot-end` / `when-pilot-ends` answers equal N10.05 exactly; a scan of `src/**/*.{ts,tsx}` (excluding tests) finds none of `first real production guest scan`, `first valid production guest scan`, `confirmed delivered`, `redeem the Activation Code`.
- [ ] **Step 2: Failing xunit** `Overview_Pilot_LabelShowsStartAndEnd`: label `"Pilot started 26 Oct 2026 · ends 25 Nov 2026"` (`UkDateLabels.Format`), DTO dates set; paid account with kept Pilot dates → label is the renewal label (no Pilot text).
- [ ] **Step 3: Run** → FAIL.
- [ ] **Step 4: Implement.** `managePlanPresentation` `pilot-start` paragraphs = [N10.19, N02.46]. Help article `activation-code`: title `Account activation explained` stays; body says the Pilot starts when setup for a selected Pilot completes (N10.19 sentence), the code is not a Pilot trigger, and Admin can extend an active or ended Pilot by 14 or 30 days (no "unlock your dashboard" claim).
- [ ] **Step 5: Run** → PASS.
- [ ] **Step 6: Commit** `fix: Pilot start copy follows doc 08; Manage Plan shows Pilot dates`.

### Task 11: Full verification and PR notes

- [ ] **Step 1:** Full backend suite → failing names ⊆ the Task 1 base file (list any base test that now passes). `npx vitest run` → only base failures. `npx tsc --noEmit` → no new errors. `npm run build` → OK. Three migration scripts → pass.
- [ ] **Step 2:** `rg -n "PilotPeriodEnd = null" backend/TummlyBackend --glob '!Migrations/**'` → no match. `rg -n "MintPilotAtActivationAsync" backend/TummlyBackend/Services` → only `PilotStartService`.
- [ ] **Step 3:** PR body to `qa`: summary per rule R1–R6; migration name; backfill note (start = end − 30 days, best effort); the Admin tab replaces "operator detail" (no operator detail page exists); QA steps for A8 (Pilot / sandbox paid / Free accounts, dates, QR states, denied actions, mailbox records, extension + denials). Do not merge; ask Salman.

---

## Self-review notes

- Spec coverage: R1.1–R1.2 T2/T4 · R1.3 T4 · R1.4 T3 · R1.5 T3 (+ reader table) · R1.6 T4 · R1.7 T6 · R1.8 T6 (existing; `RenewalDateUtc = null` on Pilot kept, tested via T4 first test) · R1.9 T10 (Manage Plan; no separate setup-confirmation screen exists in `src`) · R2.1 T7 · R3.1–R3.5 T7 (R3.2 T4/T6) · R4.1–R4.7 T8 · R4.8 T9 (Admin tab, see deviation) · R5 T10 · R6.1 T4 · R6.2 T5 · R6.3 T6 · R6.4 untouched · R6.5 T6 (renewal / failure → zero sends).
- Deviations from the PRD (report to Salman): (1) migration adds a 4th column `LastPilotExtensionKey` for R4.5 idempotency; (2) R4.8 "operator detail" does not exist — a new Admin "Pilots" tab + `GET /api/admin/pilot-accounts`; (3) `ActivationGate` loses its "activated without expiry = blocked" guard (needed for R1.5).

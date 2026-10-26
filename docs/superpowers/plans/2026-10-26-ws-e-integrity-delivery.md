# WS-E Delivery log, Twilio two-way SMS and STOP, ledger guard, role scope remainder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every guest reply has a true, operator-visible delivery state (email through Resend events, SMS through a Twilio status callback); a STOP reply to the shared UK number withdraws SMS marketing and blocks SMS for every restaurant that texted that phone; top-up grants cannot mint twice; and the last role-scope gaps (redeem role text, Assistant "all" actions, checklist acks) are closed.

**Architecture:** One channel-neutral delivery state on `FeedbackGuestResponse`, advanced only through `GuestReplyDeliveryStateMachine.TryAdvance` (forward-only, first final wins) by the email worker, the Resend webhook, the reply SMS path and a new Twilio status endpoint. STOP handling moves from a per-restaurant number map to a phone lookup over SMS send history, setting `MasterGuest.SmsStoppedAtUtc`, which every SMS send path and Campaign eligibility check. One helper `GuestSmsText` builds the final SMS text (restaurant prefix, Campaign footer) and is the only input to segment counts. One EF migration carries the new columns, backfill and a unique filtered ledger index.

**Tech Stack:** .NET 10 / EF Core 8.0.8 (xUnit 2, InMemory `TummlyWebApplicationFactory`, Testcontainers SQL Server lane), Twilio C# SDK (`RequestValidator`, `MessageResource.CreateAsync`), Resend webhooks (existing Svix check), React + Vite + TS (vitest), shadcn/ui.

**Spec:** `.scratch/ws-e-integrity-delivery/PRD.md` in the main checkout (`/run/media/salman/D/Freelance/Tummly/.scratch/…`; `.scratch` is gitignored). Rules R1–R9, acceptance A1–A12. Doc 17 copy: `/tmp/builddocs-scripts/17_INBOX_RECOVERY_R3.txt`.

## Global Constraints

- Worktree `.worktrees/ws-e-integrity-delivery`, branch `ws-e-integrity-delivery` from `qa` (35f7f0e3 or later). Merge to `qa` locally at the end (user decides).
- This branch holds the **only** open EF migration slot. **One** migration, generated with the tool: `cd backend/TummlyBackend && DOTNET_ROLL_FORWARD=Major dotnet ef migrations add WSEDeliveryStopLedger`. Never hand-write `*.Designer.cs` or the snapshot. Run `backend/TummlyBackend/scripts/check-migration-designer-pairs.sh` and `check-migrations-list-vs-files.sh` (both pass). Follow `CODING_STANDARDS.md` (cascade / Designer rules).
- Task 1 records the base failing lists on this branch base. Watch each new test fail before writing code. Full suites: **no new failures** against those lists.
- Exact strings (copy verbatim):
  - Pending `Delivery status pending.` · Accepted `Reply accepted for sending.` · Delivered `Provider reports delivered.` · Failed `The reply was not confirmed as sent. Check its status before trying again.` · Unavailable `Delivery status is unavailable. This does not mean the message was delivered.`
  - History heading `Service reply history`; columns `Sent or requested`, `Channel`, `Masked recipient`, `Status`, `Sent by`; empty `No service replies have been recorded for this feedback.`; footer `Messages sent through Tummly are shown here. Incoming replies are not shown in this view.`
  - Duplicate (code `duplicate_reply`) `This reply has already been submitted. Check its delivery status.`
  - SMS stopped (code `sms_stopped`, HTTP 409) `This message cannot be sent for the selected purpose or recipient. Review the reason.`
  - Role refusal `You do not have permission to use this feature.` · Scope (code `scope_not_allowed`, HTTP 403) `This item is outside your authorised Location scope.`
  - Campaign SMS footer: a newline then `Reply STOP to stop texts.` · SMS prefix `{Restaurant.Name}: `
- STOP keywords unchanged: STOP, STOPALL, UNSUBSCRIBE, CANCEL, END, QUIT. No confirmation SMS. No START.
- Webhook URLs are built from `PublicApi:BaseUrl` (existing config key) + `/api/webhooks/twilio/sms-status` or `/api/webhooks/twilio/sms-inbound`.
- Email retry limit setting: `GuestResponseEmailDelivery:MaxAttempts`, default `5`.
- Never log a full email or phone (`LogContactMask`). No secrets in code or tests (tests use a fake auth token).
- UI (AGENTS.md): reuse `src/components/ui/` (`Table`, `Badge`); no raw `<button>`; design tokens; font family per `docs/agents/typography.md` (functional, WebApp). Frontend style: no semicolons, double quotes. Do not run `npx prettier`.
- Test commands: backend `cd backend && dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~<Name>"`; frontend `npx vitest run <path>`; typecheck `npm run typecheck`; build `npm run build`.

## Review Focus

1. **Resend `email.delivered` arrives before the worker saves Accepted** (webhook faster than the DB write). The reply must end Delivered, not be pulled back to Accepted. Test: Task 3 `TryAdvance_AcceptedAfterDelivered_NoChange` and Task 4 `Resend_DeliveredBeforeWorkerAccepted_EndsDelivered`.
2. **STOP from a guest whose phone is stored in a different format** (`07911…` vs `+44 7911…`). The lookup must match on E.164 like the existing withdraw service. Test: Task 6 `Stop_LegacyNationalFormatPhone_Matches`.
3. **A Campaign scheduled before STOP fires after STOP** (retry or late fire). The stopped guest must be excluded at fire and retry, not only at schedule. Test: Task 6 `CampaignFire_GuestStoppedAfterSchedule_Excluded`.
4. **Operator reply SMS near a segment boundary** (body 150 chars + restaurant prefix). Reservation and charged units must use the final text, so the hold is not short. Test: Task 7 `ReplySms_PrefixCrossesSegment_ReservesTwo`.
5. **Twilio retries the same status callback** (same SID, same status, twice) or sends `sent` after `delivered`. No error, no state change. Test: Task 5 `StatusCallback_RepeatAndLateSent_NoChange`.

---

## File map

| File | Responsibility |
|---|---|
| `backend/TummlyBackend/Models/GuestReplyDeliveryState.cs` (new) | Enum Pending=0, Accepted=1, Delivered=2, Failed=3, Unavailable=4 |
| `backend/TummlyBackend/Models/FeedbackGuestResponse.cs` | `ProviderMessageId`, `DeliveryState`, `DeliveryStateAtUtc` |
| `backend/TummlyBackend/Models/MasterGuest.cs` | `SmsStoppedAtUtc` |
| `backend/TummlyBackend/Data/ApplicationDbContext.cs` | Index on `ProviderMessageId`; unique filtered ledger index |
| `backend/TummlyBackend/Migrations/<ts>_WSEDeliveryStopLedger*.cs` | Generated migration + backfill + duplicate pre-check |
| `backend/TummlyBackend/Helpers/GuestReplyDeliveryStateMachine.cs` (new) | `TryAdvance` + DTO string mapping |
| `backend/TummlyBackend/Services/GuestResponseEmailDeliveryWork.cs`, `EmailService.cs`, `Interfaces/IEmailService.cs` | Provider id, Failed on not-eligible, attempt limit |
| `backend/TummlyBackend/Services/ResendWebhookService.cs` | delivered / bounced / failed → reply state |
| `backend/TummlyBackend/Services/TwilioRecoveryGuestSmsDelivery.cs`, `RecoveryGuestSmsDeliveryResult` type, `TestingRecoveryGuestSmsDelivery.cs` | SID, status callback |
| `backend/TummlyBackend/Controllers/TwilioSmsStatusController.cs`, `Services/TwilioSmsStatusService.cs` (new) | Status callback |
| `backend/TummlyBackend/Helpers/TwilioWebhookUrl.cs` (new) | Public URL for signing |
| `backend/TummlyBackend/Services/TwilioSmsInboundService.cs`, `Controllers/TwilioSmsInboundController.cs`, `Configurations/TwilioSettings.cs` | STOP rule A; remove `InboundNumberRestaurants` |
| `backend/TummlyBackend/Services/GuestFormPermissionApplyService.cs` | Clear `SmsStoppedAtUtc` on new SMS grant |
| `backend/TummlyBackend/Helpers/GuestSmsText.cs` (new) | Final SMS text |
| `backend/TummlyBackend/Services/FeedbackGuestResponsesService.cs`, `FeedbackRecoveryOffersService.cs`, `BulkRecoveryBatchService.cs`, `GuestReplyExceptions.cs`, `Controllers/FeedbackController.cs` | Stopped refusal, final text, SMS state |
| `backend/TummlyBackend/Services/CampaignEligibilityService.cs`, `CampaignFireService.cs`, `Helpers/CampaignCreditEstimate.cs`, `Services/CampaignBillingClose.cs`, `CampaignsListService.cs`, `CampaignLifecycleService.cs`, `CampaignScheduleCommitService.cs` | Stopped exclusion; final text in counts |
| `backend/TummlyBackend/Services/RevolutOrderCompletedApplier.cs` | Unique violation = already minted |
| `backend/TummlyBackend/Services/RestaurantPermissionHelper.cs` | Role refusal text |
| `backend/TummlyBackend/Services/AssistantConversationService.cs` | All-scope rule in `LoadOwnedConversationAsync` + `TryAuthorizeScopeAsync` |
| `backend/TummlyBackend/Controllers/ChecklistAcksController.cs` | Locations View / Manage on that Location |
| `backend/TummlyBackend/DTOs/Feedback/*` (`FeedbackGuestResponseItemDto`, `FeedbackActivityEventDto`), `Helpers/FeedbackActivityHistory.cs` | `deliveryState`, `deliveryStateAt`, `guestResponseId` |
| `src/types/dashboard.ts`, `src/lib/operatorFeedback/serviceReplyHistoryPresentation.ts` (new), `src/components/dashboard/operator/Feedback/ServiceReplyHistorySection.tsx` (new), `FeedbackDetailsDrawer.tsx` | Reply history UI |
| `src/lib/operatorFeedback/recoveryCreditChromePresentation.ts`, `recoveryComposerPresentation.ts` | Prefix in estimate; `sms_stopped` message |
| `src/lib/operatorHome/createFinishSettingUpAcksModule.ts` | Hide checklist on 403 |

---

### Task 1: Base lists, data model and migration

**Files:** Models above, `Data/ApplicationDbContext.cs`, new migration, `TummlyBackend.Tests/Integration/WseMigrationBackfillTests.cs` (new, SQL Server lane: `TummlyBackend.Tests/SqlServer/WseMigrationSqlTests.cs`).

**Interfaces:**
- Produces: `enum GuestReplyDeliveryState { Pending = 0, Accepted = 1, Delivered = 2, Failed = 3, Unavailable = 4 }`; `FeedbackGuestResponse.ProviderMessageId` (string?, max 128), `.DeliveryState` (GuestReplyDeliveryState, default Pending), `.DeliveryStateAtUtc` (DateTime?); `MasterGuest.SmsStoppedAtUtc` (DateTime?).

- [ ] **Step 1:** Create the worktree. Run the full backend and frontend suites on the base; save the failing test names to `.superpowers/sdd/<plan-workspace>/base-failing-backend.txt` and `base-failing-frontend.txt` (names cut at the first space, like `.scratch/baseline-failing-backend-7281ef72.txt`).
- [ ] **Step 2:** Add the enum and fields. In `ApplicationDbContext`: filtered index `FeedbackGuestResponses(ProviderMessageId)` where `[ProviderMessageId] IS NOT NULL`; **unique** filtered index `CreditLedgerEntries(RestaurantId, SourcePaymentRef, EntryType)` where `[SourcePaymentRef] IS NOT NULL AND [EntryType] = N'<CreditLedgerEntryTypes.TopupAllocation value>'` (read the constant's string value; keep the existing non-unique index).
- [ ] **Step 3:** Generate `WSEDeliveryStopLedger`. In `Up`, before the unique index: SQL that raises an error (`THROW 51000, 'WSE: duplicate TopupAllocation rows exist; resolve before migrating.', 1`) if any `(RestaurantId, SourcePaymentRef, EntryType)` TopupAllocation group has count > 1. After the columns: backfill (PRD R1.7) with one `UPDATE … SET DeliveryState = CASE WHEN Channel = <sms int> THEN 4 WHEN EmailDeliveryStatus = <Accepted int> THEN 1 ELSE 0 END, DeliveryStateAtUtc = COALESCE(EmailDeliveredAt, CreatedAt)`. Read the int values from `FeedbackGuestResponseChannel` and `GuestResponseEmailDeliveryStatus`; write them as literals in the SQL.
- [ ] **Step 4:** SQL Server lane tests in `WseMigrationSqlTests`: `Migration_BackfillsDeliveryState` (seed an email Accepted, an email Pending and an SMS row through raw SQL at the previous migration, migrate, assert 1 / 0 / 4) — if the fixture cannot migrate in two steps, test the backfill SQL by running the same SQL text against seeded rows; `Ledger_SecondTopupAllocationSameRef_Throws` (insert two TopupAllocation rows same restaurant/ref → `DbUpdateException`); `Ledger_SameRefDifferentEntryType_Allowed`.
- [ ] **Step 5:** Run the migration check scripts and the new tests. Commit `feat(data): WS-E delivery state, SMS stop and top-up uniqueness`.

### Task 2: Top-up mint treats a unique violation as already minted (R8)

**Files:** `Services/RevolutOrderCompletedApplier.cs` (~895–935), `TummlyBackend.Tests/SqlServer/TopupConcurrentApplySqlTests.cs` (new), `TummlyBackend.Tests/Services/RevolutOrderCompletedApplierTests.cs`.

**Interfaces:** Consumes the unique index from Task 1.

- [ ] **Step 1:** Failing tests: SQL lane `TwoConcurrentTopupApplies_OneAllocation` (two scopes apply the same open top-up intent in parallel → exactly 1 `TopupAllocation` row, no unhandled exception); unit `ApplyTopup_UniqueViolationOnMint_TreatedAsMinted` (fake ledger throws the unique `DbUpdateException` → intent still closes, no rethrow).
- [ ] **Step 2:** Wrap the mint; on `DbUpdateException` whose inner SQL error number is 2601 or 2627 and the row already exists (re-query), continue as already minted; any other exception rethrows. Detach the failed entity from the context before continuing.
- [ ] **Step 3:** Tests pass. Commit `fix(billing): top-up mint is once-only under concurrency`.

### Task 3: Delivery state machine and email worker (R1)

**Files:** `Helpers/GuestReplyDeliveryStateMachine.cs` (new), `Services/GuestResponseEmailDeliveryWork.cs`, `Services/EmailService.cs`, `Interfaces/IEmailService.cs`, the worker settings class (`GuestResponseEmailDelivery` options; add `MaxAttempts = 5`), `TummlyBackend.Tests/Helpers/GuestReplyDeliveryStateMachineTests.cs` (new), `TummlyBackend.Tests/Services/GuestResponseEmailDeliveryServiceTests.cs`.

**Interfaces:**
- Produces: `static bool GuestReplyDeliveryStateMachine.TryAdvance(FeedbackGuestResponse row, GuestReplyDeliveryState next, DateTime atUtc)` — returns true and sets `DeliveryState` / `DeliveryStateAtUtc` only on a forward move (rank Pending 0 < Accepted/Unavailable 1 < Delivered/Failed 2; Unavailable → Accepted not allowed; Delivered/Failed final). `static string GuestReplyDeliveryStateMachine.ToDto(GuestReplyDeliveryState s)` → `pending|accepted|delivered|failed|unavailable`.
- Produces: `Task<string?> IEmailService.SendGuestResponseEmailAsync(...)` now returns the Resend email id (null if the provider gives none).

- [ ] **Step 1:** Failing unit tests: `TryAdvance_PendingToAccepted_Moves`, `TryAdvance_AcceptedToDelivered_Moves`, `TryAdvance_AcceptedAfterDelivered_NoChange`, `TryAdvance_FailedAfterDelivered_NoChange`, `TryAdvance_DeliveredAfterFailed_NoChange`, `TryAdvance_UnavailableToDelivered_Moves`, `TryAdvance_UnavailableToAccepted_NoChange`, `ToDto_AllValues`.
- [ ] **Step 2:** Failing worker tests: `Worker_Accepted_SetsProviderIdAndAccepted` (fake email service returns `"re_123"` → `ProviderMessageId == "re_123"`, `DeliveryState == Accepted`); `Worker_NotEligible_SetsFailed` (→ `DeliveryState == Failed`, no email sent, worker does not claim it again); `Worker_FiveFailures_SetsFailedAndStops` (provider throws 5 times → `Failed`; 6th drain sends nothing; `MaxAttempts` from settings).
- [ ] **Step 3:** Implement. Keep `EmailDeliveryStatus` as the queue: add `Abandoned = 3` to `GuestResponseEmailDeliveryStatus` (stored as int, no schema change); not-eligible and max-attempts set `Abandoned`, which the claim query already skips because it claims only `Pending`. New rows from `SendAsync` / Recovery Offer email set `DeliveryState = Pending`.
- [ ] **Step 4:** Tests pass. Commit `feat(replies): delivery state machine and truthful email worker states`.

### Task 4: Resend delivered / bounced / failed (R2)

**Files:** `Services/ResendWebhookService.cs`, `TummlyBackend.Tests/Services/ResendWebhookServiceTests.cs`.

**Interfaces:** Consumes `TryAdvance`.

- [ ] **Step 1:** Failing tests: `Resend_Delivered_SetsDelivered`, `Resend_Bounced_SetsFailed`, `Resend_Failed_SetsFailed`, `Resend_UnknownEmailId_NoChange` (200), `Resend_OtherType_NoChange`, `Resend_DeliveredBeforeWorkerAccepted_EndsDelivered` (row Pending with `ProviderMessageId` set → delivered event → Delivered; then worker-style `TryAdvance(Accepted)` → still Delivered). Existing `email.opened` Campaign tests stay green.
- [ ] **Step 2:** Add event constants `email.delivered`, `email.bounced`, `email.failed`. Look up `FeedbackGuestResponses` by `ProviderMessageId == data.email_id`; event time from `created_at` when parseable, else now.
- [ ] **Step 3:** Tests pass. Commit `feat(replies): Resend delivery events update reply state`.

### Task 5: Twilio SID, status callback and signed URLs (R3)

**Files:** `Services/TwilioRecoveryGuestSmsDelivery.cs`, the `RecoveryGuestSmsDeliveryResult` type, `Services/TestingRecoveryGuestSmsDelivery.cs`, `Helpers/TwilioWebhookUrl.cs` (new), `Controllers/TwilioSmsStatusController.cs` + `Services/TwilioSmsStatusService.cs` (new; register in `Program.cs`), `Controllers/TwilioSmsInboundController.cs`, `Services/FeedbackGuestResponsesService.cs` (SMS branch ~324–360), `TummlyBackend.Tests/Integration/TwilioSmsStatusEndpointsTests.cs` (new), `TwilioSmsInboundEndpointsTests.cs`.

**Interfaces:**
- Produces: `RecoveryGuestSmsDeliveryResult.Accepted { AcceptedSegments, ProviderMessageId (string?), StatusCallbackSet (bool) }`.
- Produces: `static string? TwilioWebhookUrl.For(IConfiguration config, string path)` → `{PublicApi:BaseUrl trimmed of trailing /}{path}` or null when the base URL is empty.

- [ ] **Step 1:** Failing tests (fake auth token in test config; compute signatures with `Twilio.Security.RequestValidator` / the SDK's signature helper over the exact URL): `StatusCallback_Delivered_SetsDelivered`, `StatusCallback_Undelivered_SetsFailed`, `StatusCallback_Failed_SetsFailed`, `StatusCallback_Sent_NoChange`, `StatusCallback_RepeatAndLateSent_NoChange`, `StatusCallback_BadSignature_403`, `StatusCallback_MissingSignature_403`, `StatusCallback_UnknownSid_204`, `StatusCallback_SignedAgainstPublicBaseUrl_NotRequestHost` (request host differs from `PublicApi:BaseUrl` → accepted); `Inbound_SignedAgainstPublicBaseUrl_Accepted`; `ReplySms_Accepted_StoresSidAndAccepted`; `ReplySms_NoCallbackConfigured_Unavailable`; `ReplySms_ProviderFailed_SetsFailed`.
- [ ] **Step 2:** `SendAsync` passes `statusCallback: new Uri(url)` when `TwilioWebhookUrl.For(config, "/api/webhooks/twilio/sms-status")` is not null; returns `message.Sid`. Testing delivery returns `ProviderMessageId = "SM-test-" + guid`, `StatusCallbackSet = true`.
- [ ] **Step 3:** Status endpoint `POST /api/webhooks/twilio/sms-status` (`[AllowAnonymous]`, form). Validate against `TwilioWebhookUrl.For(...)` (fallback: request URL when base empty). Map `MessageStatus` per PRD R3.3. Match `FeedbackGuestResponses.ProviderMessageId == MessageSid`. Unknown → 204; handled → 204.
- [ ] **Step 4:** Inbound controller signs against `TwilioWebhookUrl.For(config, "/api/webhooks/twilio/sms-inbound")` with the same fallback.
- [ ] **Step 5:** Reply SMS path: after Accepted → `ProviderMessageId = sid`, `TryAdvance(StatusCallbackSet ? Accepted : Unavailable)`; Failed → `TryAdvance(Failed)` when a row exists. Tests pass. Commit `feat(sms): Twilio SID, status callback and public-URL signatures`.

### Task 6: Shared number STOP rule A and SMS block (R4)

**Files:** `Services/TwilioSmsInboundService.cs`, `Configurations/TwilioSettings.cs` (remove `InboundNumberRestaurants`), `Services/GuestFormPermissionApplyService.cs`, `Services/GuestReplyExceptions.cs` (add `GuestReplySmsStoppedException`), `Services/FeedbackGuestResponsesService.cs`, `FeedbackRecoveryOffersService.cs`, `BulkRecoveryBatchService.cs`, `Controllers/FeedbackController.cs`, `Services/CampaignEligibilityService.cs` (~420–450), tests in `TummlyBackend.Tests/Services/TwilioSmsInboundServiceTests.cs`, `TwilioSmsInboundEndpointsTests.cs`, `TummlyBackend.Tests/Integration/GuestReplySmsStoppedTests.cs` (new), Campaign eligibility / fire tests.

**Interfaces:**
- Consumes `IGuestInitiatedMarketingWithdrawService.WithdrawForRestaurantContactAsync(restaurantId, e164, isEmail: false, SmsMarketing, LocationGuestPermissionLedgerSources.SmsStop, ct)`.
- Produces: `GuestReplySmsStoppedException` → HTTP 409 `{ success: false, code: "sms_stopped", message: <exact> }`.

- [ ] **Step 1:** Failing STOP tests: `Stop_TextedByR1AndR2_WithdrawsAndStopsBoth_NotR3` (R3 has the guest but never texted); `Stop_CampaignSmsOnly_Counts` (a `CampaignRecipientDelivery` sms with provider id counts); `Stop_UnknownPhone_NoChange`; `Stop_NonKeyword_NoChange`; `Stop_LegacyNationalFormatPhone_Matches`; `Stop_Twice_KeepsFirstStoppedAt`.
- [ ] **Step 2:** Implement R4.3: E.164 from `From`; candidate `MasterGuest` rows matched the same way as `GuestInitiatedMarketingWithdrawService` (E.164 / last-9 then exact check); keep those with an SMS `FeedbackGuestResponse` (via `Feedback.LocationGuest.MasterGuestId`) or an sms `CampaignRecipientDelivery` with `ProviderMessageId` (via `LocationGuest.MasterGuestId`); per distinct restaurant call withdraw, set `SmsStoppedAtUtc ??= now`. Ignore the `To` number. Remove the map and its startup warning.
- [ ] **Step 3:** Failing block tests: `ReplySms_StoppedGuest_409SmsStopped_NoReserve` (credit balance unchanged, no row); `RecoveryOfferSms_StoppedGuest_409`; `BulkRecovery_StoppedRow_ReportedPerRow_OthersSend`; `ReplyEmail_StoppedGuest_Allowed`; `CampaignEligibility_StoppedGuest_ExcludedFromSms`; `CampaignFire_GuestStoppedAfterSchedule_Excluded`; `GuestForm_NewSmsGrant_ClearsStop`; `GuestForm_EmailGrant_DoesNotClearStop`.
- [ ] **Step 4:** Implement: check `MasterGuest.SmsStoppedAtUtc` before any reservation in both SMS send paths (inside the guest-reply lock, next to the R1/R2 checks); bulk maps the exception to its per-row failure; controller maps to 409; Campaign SMS channel state treats a stopped guest as not eligible (so schedule and fire both exclude); Guest Form clears on an SMS marketing grant. Tests pass. Commit `feat(sms): shared-number STOP withdraws and blocks SMS per texted restaurant`.

### Task 7: Final SMS text and segment counts (R5)

**Files:** `Helpers/GuestSmsText.cs` (new), `Services/FeedbackGuestResponsesService.cs` (~267, ~324, ~590), `FeedbackRecoveryOffersService.cs` (SMS body build), `CampaignFireService.cs` (~360–395), `Helpers/CampaignCreditEstimate.cs`, `Services/CampaignBillingClose.cs`, `CampaignsListService.cs` (~477), `CampaignLifecycleService.cs` (~563), `CampaignScheduleCommitService.cs` (~184), `src/lib/operatorFeedback/recoveryCreditChromePresentation.ts`, tests `TummlyBackend.Tests/Helpers/GuestSmsTextTests.cs` (new) + affected service tests + `recoveryCreditChromePresentation.test.ts`.

**Interfaces:**
- Produces: `static string GuestSmsText.ForReply(string restaurantName, string body)` → `$"{name.Trim()}: {body}"`; `static string GuestSmsText.ForCampaign(string restaurantName, string body)` → `ForReply(...) + "\nReply STOP to stop texts."`. Blank name → no prefix.
- Frontend: `estimateRecoverySmsSegments(body: string, restaurantName?: string | null): number` uses `${name}: ${body}` when a name is given.

- [ ] **Step 1:** Failing tests: `ForReply_AddsPrefix`, `ForReply_BlankName_NoPrefix`, `ForCampaign_AddsPrefixAndFooter`; `ReplySms_PrefixCrossesSegment_ReservesTwo` (150-char body, name "The Golden Fork" → reservation 2, provider receives the prefixed text); `ReplySms_DuplicateRuleStillComparesOperatorBody` (same operator body twice → `duplicate_reply`); `CampaignFire_SmsBodyHasPrefixAndFooter`; `CampaignEstimate_UsesFinalText`; `CampaignsList_SmsPartsUseFinalText`; frontend `estimateRecoverySmsSegments adds the restaurant prefix`.
- [ ] **Step 2:** Implement: stored `FeedbackGuestResponse.Body` and `Campaign.MessageBody` stay the operator text; only the provider call and every segment count use `GuestSmsText`. Pass the restaurant name into `CampaignCreditEstimate.EstimateUnits` / `AcceptedUnitsForDelivery` callers (load `Restaurant.Name` where the campaign is loaded). The frontend composer passes the restaurant name it already shows (if none is available in the module, pass `null`).
- [ ] **Step 3:** Tests pass. Commit `feat(sms): restaurant prefix and STOP footer in the sent text and estimates`.

### Task 8: Reply history UI and text changes (R6, R7.1)

**Files:** `DTOs/Feedback/FeedbackGuestResponseDtos.cs` (`FeedbackGuestResponseItemDto` + `DeliveryState`, `DeliveryStateAt`), `FeedbackActivityEventDto` (+ `GuestResponseId`, `DeliveryState`, `DeliveryStateAt`), `Helpers/FeedbackActivityHistory.cs` (`ToActivityEvent(FeedbackGuestResponseItemDto)`), `Services/FeedbackGuestResponsesService.cs` (`ToItemDto`), `FeedbackRecoveryOffersService.cs` (`ToGuestResponseItemDto`), `FeedbackRespondAndRecordService.cs`, `Services/GuestReplyExceptions.cs` (duplicate text), `src/types/dashboard.ts`, `src/lib/operatorFeedback/serviceReplyHistoryPresentation.ts` + `.test.ts` (new), `src/components/dashboard/operator/Feedback/ServiceReplyHistorySection.tsx` (new), `FeedbackDetailsDrawer.tsx` (add the section above "Activity history"), `src/lib/operatorFeedback/recoveryComposerPresentation.ts` + test, backend tests asserting the old duplicate text.

**Interfaces:**
- Produces (API): activity events of kind `guest_response_sent` carry `guestResponseId: number`, `deliveryState: "pending"|"accepted"|"delivered"|"failed"|"unavailable"`, `deliveryStateAt: string|null`.
- Produces (TS): `serviceReplyHistoryRows(events: FeedbackDetailsActivityEventDto[]): ServiceReplyHistoryRow[]` (newest first; `{ id, sentAtLabel, channelLabel: "Email"|"SMS", maskedRecipient, statusText, sentBy }`); `SERVICE_REPLY_HISTORY_COPY` with the exact strings.

- [ ] **Step 1:** Failing backend tests: `FeedbackDetail_GuestResponseEvent_HasDeliveryState` (seed Delivered row → event `deliveryState == "delivered"`); update duplicate-text assertions to the new exact text (watch them fail first).
- [ ] **Step 2:** Failing frontend tests: one per state text; `empty history returns no rows and the empty copy`; `rows are newest first`; `maskedRecipient is passed through unchanged`; composer `409 sms_stopped shows the server message`.
- [ ] **Step 3:** Implement the section with `Table` from `src/components/ui/table.tsx` (or a stacked list using the drawer `Section` if the table does not fit the drawer width — keep the five labels), footer note under the list, empty text when no rows. Status text uses muted colour for pending/unavailable, destructive token for failed.
- [ ] **Step 4:** Tests, `npm run typecheck` (no new errors vs base count), commit `feat(feedback): service reply history with delivery status`.

### Task 9: Role text, Assistant all-scope actions, checklist acks (R7.2, R9)

**Files:** `Services/RestaurantPermissionHelper.cs` (`EvaluateArea`), `Services/AssistantConversationService.cs` (`LoadOwnedConversationAsync`, `TryAuthorizeScopeAsync`, SendTurn new-conversation scope), `Controllers/AssistantConversationsController.cs` (403 mapping if needed), `Controllers/ChecklistAcksController.cs`, `src/lib/operatorHome/createFinishSettingUpAcksModule.ts` + test, tests `TummlyBackend.Tests/Integration/OffersRedeemRoleTextTests.cs` (new), `AssistantConversationScopeEndpointsTests.cs`, `ChecklistAcksEndpointsTests.cs`.

**Interfaces:** Consumes `IRestaurantPermissionHelper.AuthorizeLocationAsync` / `AuthorizeUserAsync`, `HasAllLocationsAccessAsync`.

- [ ] **Step 1:** Failing tests: `BillingAdmin_RedeemCheck_403PermissionText`; `BillingAdmin_RedeemConfirm_403PermissionText`; `OtherTenantLocation_RedeemCheck_403LocationText`; for a partial-scope user on an "all" conversation: `Send_AllScope_NotFound`, `Retry_AllScope_NotFound`, `ApplyScope_AllScope_NotFound`, `Archive_AllScope_NotFound`, `ClearDraft_AllScope_NotFound`, `Delete_AllScope_NotFound`; `SwitchToAll_PartialUser_403ScopeNotAllowed`; `NewConversationAll_PartialUser_403ScopeNotAllowed`; `OwnerAllScope_ActionsStillWork`; checklist: `ChecklistGet_UnassignedLocation_403`, `ChecklistPost_ViewOnly_403`, `ChecklistGet_Staff_403`, `ChecklistPost_AssignedManager_200`; frontend `getChecklistAcks 403 hides the checklist without an error`.
- [ ] **Step 2:** `EvaluateArea`: when the role cell does not meet the minimum → `DenyWith("You do not have permission to use this feature.")` (both `denyLocation` values); Location-set and tenant denials keep their text. Update other tests that asserted the old text for a role cause (list them in the report).
- [ ] **Step 3:** Assistant: `LoadOwnedConversationAsync` returns null for an "all" conversation when `HasAllLocationsAccessAsync(AiAssistant View set)` is false (every action already maps null to not-found); `TryAuthorizeScopeAsync` refuses scope "all" without all-Location access with `scope_not_allowed` → 403 exact text.
- [ ] **Step 4:** Checklist: GET `AuthorizeLocationAsync(User, Locations, View, locationId)`, POST with `Manage`; map denials via `ToHttpResult()`. Frontend treats 403 as "hidden".
- [ ] **Step 5:** Tests pass. Commit `fix(scope): role refusal text, Assistant all-scope actions, checklist acks scope`.

### Task 10: Full verification and QA handoff

**Files:** `.superpowers/sdd/<plan-workspace>/qa-handoff.md` (new, not committed).

- [ ] **Step 1:** Full backend suite, full frontend suite, `npm run typecheck`, `npm run build`, migration scripts. Compare failing names with the Task 1 lists: no new names.
- [ ] **Step 2:** Write `qa-handoff.md` for Salman with the exact steps for D1 (Resend events), D2 (Twilio UK number purchase, inbound webhook URL `https://api.qa.tummly.com/api/webhooks/twilio/sms-inbound`, app settings `TwilioSettings__RecoveryFromNumber`, `PublicApi__BaseUrl=https://api.qa.tummly.com`), D3, and the A12 QA script.
- [ ] **Step 3:** Commit any test fixture updates; report.

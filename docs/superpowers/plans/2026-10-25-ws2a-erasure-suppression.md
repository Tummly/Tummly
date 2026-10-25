# WS2a Erasure and suppression (+ WS1 concurrent-submit fix) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Guest delete and the 90-day purge complete on SQL Server and leave no guest contact behind, a minimal hashed suppression record keeps each withdrawal, logs carry no full contact, and two concurrent Guest Form submits write one Feedback with no 500.

**Architecture:** Claims and Campaign rows lose their guest link (nullable FK, filtered unique indexes) instead of blocking the delete. One shared delete helper (`LocationGuestHardDelete`) stages suppression rows, unlinks, anonymises Feedback and removes the guest. A new `GuestContactSuppressionService` (HMAC key from config) writes rows on delete and on unknown-contact STOP / unsubscribe, and replays them into the ledger when the same contact comes back. `SubmitFeedback` serialises per restaurant + contact with `sp_getapplock`. A new SQL Server test lane (Testcontainers) proves what InMemory cannot.

**Tech Stack:** .NET 10 / EF Core 8.0.8 (xUnit 2, InMemory, `TummlyWebApplicationFactory`), new `Testcontainers.MsSql` + `Xunit.SkippableFact` (SQL Server 2022 image), React + Vite + TS (vitest).

**Spec:** `.scratch/ws2a-erasure-suppression/PRD.md` in the main checkout (`/run/media/salman/D/Freelance/Tummly/.scratch/…`; `.scratch` is gitignored). Rules R1–R5, acceptance A1–A7.

## Global Constraints

- Worktree `.worktrees/ws2a-erasure-suppression`, branch `ws2a-erasure-suppression` from `origin/qa` 1b5e87ae. One PR to `qa`.
- **One** EF migration, generated with the tool: `cd backend/TummlyBackend && DOTNET_ROLL_FORWARD=Major dotnet ef migrations add WS2aGuestErasureSuppression`. Never hand-write `*.Designer.cs` or snapshot. Run `backend/TummlyBackend/scripts/check-migration-designer-pairs.sh`, `check-migrations-list-vs-files.sh`, `check-restaurant-user-setnull-fks.sh` (all must pass).
- Watch each new test fail before you write code. Full suites: **no new failures** against the base list `/run/media/salman/D/Freelance/Tummly/.scratch/ws2a-erasure-suppression/baseline-failing-backend-1b5e87ae.txt` (119 names, full run at 1b5e87ae on 25 Oct) and frontend base (`assistantWaitGerunds`, `createFeedbackDetailsModule`).
- Exact strings: display name `Deleted guest`; audit action `guest.delete`; audit target type `location_guest`; ledger source `suppression` with label `Earlier withdrawal`; lock-timeout message `Please try again.`; config key `GuestSuppression:HashKey` (env `GuestSuppression__HashKey`).
- Suppression row: no name and no plain contact. Contact key = `email:<trim+lower>` or `phone:<E.164>` (via `PhoneNumberHelper.TryNormalizeToE164`, region GB). E.164 replaces PRD R2.2 "digits" because STOP arrives as E.164 and guests type `07…`; the PRD is updated to match.
- Do not log a full email or phone. Do not print secrets. Do not run `npx prettier`. Frontend style: no semicolons, double quotes.
- Do not change the API field `GuestName` or the public wire shape of the Guest Form submit (except the new 503 body).
- Sub-agents: Cursor Auto only.

## Review Focus

1. **Same contact deleted at two locations of one restaurant in one purge.** Both guests stage a row for the same (restaurant, kind, hash). The purge must save one row, not hit the unique index. Test: Task 4 `Purge_TwoLocationGuestsSameContact_WritesOneSuppressionRowPerKind` (SQL Server).
2. **Claim of a deleted guest redeemed at its own location.** Check and confirm must succeed (not `wrong_location`) and show `Deleted guest`. Test: Task 2 `Redeem_UnlinkedClaim_UsesOfferLocation`.
3. **Campaign resume after a recipient is deleted mid-fire.** The unlinked frozen row is skipped, billing close still counts the accepted delivery. Test: Task 2 `Fire_SkipsUnlinkedFrozenRecipient_KeepsAcceptedDeliveryCount`.
4. **STOP from `+447…` for a guest who typed `07…`.** Today it matches nobody. It must withdraw that guest. Test: Task 5 `WithdrawForRestaurantContact_MatchesNationalFormatGuest`.
5. **New contact submits while the same contact has a suppression row and ticks marketing.** The suppression `withdraw` (old date) and the new `grant` (now) both land in one SaveChanges; current state must be Granted, Feedback follow-up must stay Withdrawn if it was suppressed. Test: Task 6 `NewGuest_SuppressedAndTicked_GrantsMarketing_KeepsFollowUpWithdrawn`.

---

### Task 1: Baseline and SQL Server test lane

**Files:**
- Modify: `backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj` (add `Testcontainers.MsSql` 4.x, `Xunit.SkippableFact` 1.5.x, `Microsoft.EntityFrameworkCore.SqlServer` 8.0.8 if not transitively present)
- Create: `backend/TummlyBackend.Tests/SqlServer/SqlServerFixture.cs`, `SqlServerCollection.cs`, `SqlServerSmokeTests.cs`

**Interfaces:**
- Produces: `[Collection(SqlServerCollection.Name)]` (`Name = "SqlServer"`); `SqlServerFixture : IAsyncLifetime` with `bool IsAvailable`, `string? SkipReason`, `string ConnectionString`, `ApplicationDbContext CreateContext()`, `WebApplicationFactory<Program> CreateFactory()` (= `new TummlyWebApplicationFactory().WithWebHostBuilder(...)` that removes the InMemory `DbContextOptions<ApplicationDbContext>` / `ApplicationDbContext` descriptors and adds `UseSqlServer(ConnectionString)`). Tests call `Skip.IfNot(fixture.IsAvailable, fixture.SkipReason)` inside `[SkippableFact]`.

- [x] **Step 1: Base failing list** — done by the controller: 119 names in the base list file (Global Constraints).
- [ ] **Step 2: Write `SqlServerSmokeTests.Migrations_ApplyOnEmptyDatabase`**: asserts `GetPendingMigrations()` is empty after the fixture ran `MigrateAsync()`, and that `OfferIssues`, `CampaignFrozenRecipients`, `CampaignRecipientDeliveries` exist.
- [ ] **Step 3: Implement the fixture.** Image `mcr.microsoft.com/mssql/server:2022-latest`. `InitializeAsync`: try to start the container; on any Docker error set `IsAvailable = false`, `SkipReason = "Docker not available: <type>"` and return. Create database `tummly_ws2a`, run `MigrateAsync()` **once** for the collection. Tests isolate by seeding their own restaurant (no per-test database).
- [ ] **Step 4: Run** `cd backend && dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~SqlServer"` → PASS (or Skipped without Docker). If a historic migration fails on an empty database, stop and report it (do not edit old migrations).
- [ ] **Step 5: Commit** `test: add SQL Server test lane (Testcontainers)`.

### Task 2: Claims and Campaign rows can have no guest (schema + reads)

**Files:**
- Modify: `Models/OfferIssue.cs`, `Models/CampaignFrozenRecipient.cs`, `Models/CampaignRecipientDelivery.cs` (`int? LocationGuestId`)
- Create: `Models/GuestContactSuppression.cs`, `Helpers/DeletedGuestDisplay.cs`
- Modify: `Data/ApplicationDbContext.cs` (filters + new entity + `DbSet<GuestContactSuppression> GuestContactSuppressions`)
- Create (generated): `Migrations/<ts>_WS2aGuestErasureSuppression.cs` + `.Designer.cs`, snapshot
- Modify (compile- and grep-driven; list is the starting set): `Services/OfferIssueService.cs` (`FindIssueByClaimCodeAsync`, `EvaluateRedeemGateAsync`, `BuildPreview`, lines ~49, ~637), `Services/OfferVoidRequestService.cs:93-100,365`, `Services/OfferLifecycleService.cs:41,116,160,342,374`, `Services/ReportsOffersService.cs:414-430`, `Services/CampaignFireService.cs:145-180,240,459,672`, `Services/CampaignLifecycleService.cs:522,628,722,766`, `Services/ResendWebhookService.cs:179`, `Services/GuestProfileService.cs:207`, plus every other compile error
- Test: `backend/TummlyBackend.Tests/Services/UnlinkedGuestRowsTests.cs` (InMemory), `backend/TummlyBackend.Tests/SqlServer/UnlinkedGuestRowsSqlTests.cs`

**Interfaces:**
- Produces: `DeletedGuestDisplay.Name = "Deleted guest"`; `DeletedGuestDisplay.NameOrDeleted(LocationGuest? guest) -> string` (trimmed name, or `Deleted guest` when guest is null).
- Produces: `GuestContactSuppression { int Id; int RestaurantId; Restaurant? Restaurant; LocationGuestPermissionKind PermissionKind; string ContactHash (Required, MaxLength 64); DateTime WithdrawnAtUtc; string Source (Required, MaxLength 32); DateTime CreatedAtUtc }`. `PermissionKind` uses the same wire-string conversion as `LocationGuestPermissionLedgerEntry`. FK → `Restaurants` `Cascade` (single path, no Users FK). Unique index `(RestaurantId, PermissionKind, ContactHash)`.
- Produces: `GuestContactSuppressionSources { GuestDelete = "guest-delete"; RetentionPurge = "retention-purge"; EmailUnsubscribe = "email-unsubscribe"; SmsStop = "sms-stop" }` (in the model file).

- [ ] **Step 1: Write failing tests (InMemory)**
  - `Redeem_UnlinkedClaim_UsesOfferLocation`: Claim with `LocationGuestId = null`, CatalogOffer at location L → check at L returns preview with `GuestName == "Deleted guest"`; confirm succeeds; check at other location → `wrong_location`.
  - `VoidRequest_UnlinkedClaim_UsesOfferLocation`: create void request for that Claim at L → not NotFound.
  - `ClaimList_UnlinkedClaim_ShowsDeletedGuest`: offer details claim list item has `GuestName == "Deleted guest"`, `GuestId == null`.
  - `RecentRedemptions_IncludesUnlinkedClaim`: redeemed unlinked Claim appears in Reports Offers recent redemptions with `Deleted guest` (today the inner join drops it).
  - `Fire_SkipsUnlinkedFrozenRecipient_KeepsAcceptedDeliveryCount`: Campaign with 2 frozen rows, one unlinked + 1 accepted unlinked delivery → fire sends only to the linked guest; billing close / accepted count still includes the unlinked accepted delivery.
- [ ] **Step 2: Write failing SQL test** `UnlinkedGuestRowsSqlTests.TwoUnlinkedRowsInOneCampaign_Accepted`: insert two frozen recipients and two deliveries (same channel) with `LocationGuestId = null` in one Campaign, and two Campaign Claims with null guest → SaveChanges succeeds. (Fails until the migration exists.)
- [ ] **Step 3: Run** both test classes → FAIL.
- [ ] **Step 4: Model + context.** Nullable FKs; delete behaviour unchanged. Filters: `IX_CampaignFrozenRecipients_CampaignId_LocationGuestId` → `[LocationGuestId] IS NOT NULL`; `IX_CampaignRecipientDeliveries_CampaignId_LocationGuestId_Channel` → `[LocationGuestId] IS NOT NULL`; `IX_OfferIssues_CampaignId_LocationGuestId` → `[CampaignId] IS NOT NULL AND [LocationGuestId] IS NOT NULL`; `IX_OfferIssues_ThankYou_LocationGuestId_CatalogOfferId` → `[Source] = N'guest_form_thank_you' AND [LocationGuestId] IS NOT NULL`. Add the suppression entity. Generate the migration (command in Global Constraints); read the generated `Up`/`Down` and confirm it only alters these columns/indexes and creates the table.
- [ ] **Step 5: Fix reads.** Rules: redeem / void location = `issue.LocationGuest?.RestaurantLocation ?? issue.CatalogOffer!.RestaurantLocation` (Include `CatalogOffer.RestaurantLocation.Restaurant`; workspace status from the same location); names via `DeletedGuestDisplay.NameOrDeleted`; joins on guest become left joins; Campaign fire / lifecycle / webhook queries filter `LocationGuestId != null` before building guest-id sets (drop only); accepted-count and billing queries do not filter by guest.
- [ ] **Step 6: Run** the two new classes → PASS; run `--filter "FullyQualifiedName~Offer|FullyQualifiedName~Campaign|FullyQualifiedName~Reports"` → no new failures vs base. Run the three migration scripts → pass.
- [ ] **Step 7: Commit** `feat: Claims and Campaign rows keep history without a guest`.

### Task 3: Contact key, hasher, suppression service

**Files:**
- Create: `Helpers/GuestContactKey.cs`, `Interfaces/IGuestContactHasher.cs`, `Services/GuestContactHasher.cs`, `Interfaces/IGuestContactSuppressionService.cs`, `Services/GuestContactSuppressionService.cs`
- Modify: `Program.cs` (register; fail start when key missing outside Development/Testing), `appsettings.Development.json` (dev key value `dev-only-guest-suppression-key`)
- Test: `Helpers/GuestContactKeyTests.cs`, `Services/GuestContactHasherTests.cs`, `Services/GuestContactSuppressionServiceTests.cs`

**Interfaces:**
- Produces: `GuestContactKey.TryCreate(string? contact, ContactType type, out string key) -> bool` (`email:` + trim/lower; `phone:` + E.164; false for Unknown / invalid / blank); `GuestContactKey.FromMasterGuest(MasterGuest master) -> string?` (email first, else `Mobile` then `NormalizedPhone` through E.164; null when none parse).
- Produces: `IGuestContactHasher.Hash(string contactKey) -> string` (lower-case hex HMAC-SHA256, 64 chars). `GuestContactHasher(string key)` ctor; `GuestContactHasher.FromConfiguration(IConfiguration, IHostEnvironment) -> GuestContactHasher` — key `GuestSuppression:HashKey`; missing + env Development/Testing → fixed key `tests-only-guest-suppression-key`; missing otherwise → `InvalidOperationException("GuestSuppression:HashKey is required.")`. Registered singleton; `Program.cs` resolves it once after `builder.Build()` so a missing key stops start.
- Produces: `IGuestContactSuppressionService` (scoped):
  - `Task StageAsync(int restaurantId, string contactKey, LocationGuestPermissionKind kind, DateTime withdrawnAtUtc, string source, CancellationToken ct = default)` — upsert by (restaurant, kind, hash) across DB **and** `Local`; keeps max `WithdrawnAtUtc`; no SaveChanges.
  - `Task<IReadOnlyList<GuestContactSuppression>> FindAsync(int restaurantId, string contactKey, CancellationToken ct = default)`.

- [ ] **Step 1: Write failing tests.** Key: `"  Alex@Example.COM "` → `email:alex@example.com`; `07123 456789` and `+447123456789` → same `phone:+447123456789`; Unknown → false. Hasher: same input → same 64-char hex; different key → different hash; `FromConfiguration` throws in `Production` without key and works in `Testing`. Service: two `StageAsync` for the same key in one context → one row in `Local`, later date kept; stage when a DB row exists with a later date → date not lowered; `FindAsync` returns only that restaurant's rows; stored row has no `@` and no digits-run of the contact.
- [ ] **Step 2: Run** → FAIL. **Step 3: Implement.** **Step 4: Run** → PASS; run `Integration/SecurityHeadersTests` (any factory test) → API still starts in Testing.
- [ ] **Step 5: Commit** `feat: hashed guest contact suppression service`.

### Task 4: Guest delete and purge (R1.1–R1.4, R2.4a)

**Files:**
- Modify: `Helpers/LocationGuestHardDelete.cs`, `Services/LocationGuestDeleteService.cs`, `Services/GuestRetentionPurgeService.cs`, `Models/AdminAuditEvent.cs` (`AdminAuditActions.GuestDelete = "guest.delete"`, `AdminAuditTargetTypes.LocationGuest = "location_guest"`)
- Test: `Services/LocationGuestDeleteServiceTests.cs`, `Services/GuestRetentionPurgeServiceTests.cs`, `SqlServer/GuestDeleteSqlTests.cs`

**Interfaces:**
- Consumes: Task 2 nullable FKs + `DeletedGuestDisplay.Name`; Task 3 `GuestContactKey.FromMasterGuest`, `IGuestContactSuppressionService.StageAsync`.
- Produces: `LocationGuestHardDelete.ApplyAsync(ApplicationDbContext context, IGuestContactSuppressionService suppressions, LocationGuest guest, string suppressionSource, DateTime nowUtc, CancellationToken ct = default) -> Task<LocationGuestHardDeleteCounts>`; `record LocationGuestHardDeleteCounts(int Feedbacks, int Claims, int FrozenRecipients, int Deliveries)`. `LocationGuestDeleteService` ctor adds `IAdminAuditService audit`, `IGuestContactSuppressionService suppressions`.

- [ ] **Step 1: Write failing tests (InMemory).**
  - `Delete_UnlinksClaimsAndCampaignRows`: guest with Claim, frozen recipient, delivery → rows remain, `LocationGuestId == null`.
  - `Delete_AnonymisesFeedback_KeepsComment`: `GuestName == "Deleted guest"`, `GuestContact == ""`, `ContactType == Unknown`, comment / sentiment / workflow status unchanged.
  - `Delete_WritesSuppressionOnlyForWithdrawnKinds`: ledger EmailMarketing withdraw at T1, FeedbackFollowUp grant → one row (EmailMarketing, `WithdrawnAtUtc == T1`, source `guest-delete`).
  - `Delete_LegacyOptedOutWithoutLedger_UsesDeleteTime`: `MarketingPreference = OptedOut`, empty ledger → row with `WithdrawnAtUtc == now`.
  - `Delete_WritesAuditWithoutContact`: one `guest.delete` event, target `location_guest`, `ActorIdentity == "user:{id}"`, `DetailJson` has counts + `locationId`, contains neither the email nor the name.
  - `Purge_WritesSuppressionWithRetentionSource`.
  - `Respond_DeletedGuestFeedback_Rejected` (R1.8): after delete, `ServiceReplyEligibility.IsAllowedAsync` is false for Email and SMS; the Feedback details `recoveryActions.respondEnabled` is false (frontend shows the existing `No valid contact method available.`).
- [ ] **Step 2: Write failing SQL tests** `GuestDeleteSqlTests`: `Delete_GuestWithClaimAndCampaignRows_Completes`; `Purge_GuestWithClaimAndCampaignRows_Completes` (Dormant > 90 days); `Purge_TwoLocationGuestsSameContact_WritesOneSuppressionRowPerKind`.
- [ ] **Step 3: Run** → FAIL (SQL tests fail with FK `REFERENCE constraint` today — record the message in the PR as X1 proof).
- [ ] **Step 4: Implement.** Order in `ApplyAsync` (PRD R1.2): read effective states (`ResolveEffectiveStates` with `MarketingPreference`) and latest withdraw `OccurredAt` per kind → `StageAsync` for each Withdrawn kind (date = latest withdraw, or `nowUtc` when only the legacy preference says withdrawn; skip when `FromMasterGuest` is null) → unlink three tables → anonymise + unlink Feedback → existing removals. Delete service: `suppressionSource = guest-delete`, append audit, SaveChanges once. Purge: `retention-purge`, keep its audit.
- [ ] **Step 5: Run** the three classes → PASS; `--filter "FullyQualifiedName~Guest"` → no new failures.
- [ ] **Step 6: Commit** `fix: guest delete and purge complete and leave no contact (G3.1, G3.2, G3.3)`.

### Task 5: Unknown-contact STOP / unsubscribe writes a row; STOP matches `07…` guests (R2.4b)

**Files:**
- Modify: `Services/GuestInitiatedMarketingWithdrawService.cs` (`WithdrawForRestaurantContactAsync`)
- Test: `Services/GuestInitiatedMarketingWithdrawServiceTests.cs` (create if missing), `Integration/TwilioSmsInboundEndpointsTests.cs`, `Integration/PublicUnsubscribeEndpointsTests.cs`

**Interfaces:**
- Consumes: `GuestContactKey`, `IGuestContactSuppressionService` (ctor injection).

- [ ] **Step 1: Write failing tests.** `WithdrawForRestaurantContact_MatchesNationalFormatGuest` (guest typed `07123456789`, STOP From `+447123456789` → guest withdrawn); `WithdrawForRestaurantContact_NoGuest_WritesSuppressionRow` (email form + SMS STOP; source `email-unsubscribe` / `sms-stop`; `GuestsTouched == 0`); `WithdrawForRestaurantContact_GuestFound_WritesNoSuppressionRow`.
- [ ] **Step 2: Run** → FAIL. **Step 3: Implement.** Phone match: compare the E.164 of the input with the E.164 of each candidate `MasterGuest.Mobile` / `NormalizedPhone`. Candidates are narrowed in SQL by the last 9 digits, then the comparison is done in memory. No guests → `StageAsync` + SaveChanges.
- [ ] **Step 4: Run** → PASS; Twilio + unsubscribe test classes no new failures.
- [ ] **Step 5: Commit** `fix: keep STOP/unsubscribe for unknown contacts; STOP matches national-format numbers`.

### Task 6: Replay suppression for a returning contact; no Feedback follow-up re-grant (R2.5–R2.7, R3)

**Files:**
- Modify: `Services/GuestUpsertService.cs`, `Services/GuestFormPermissionApplyService.cs`, `Helpers/LocationGuestChannelPermissionGate.cs` (`LedgerEventsForGuestFormSubmit`), `Models/LocationGuestPermissionLedgerEntry.cs` (`LocationGuestPermissionLedgerSources.Suppression = "suppression"`), `Helpers/LocationGuestPermissionPresentation.cs` (label `Earlier withdrawal`)
- Test: `Integration/GuestUpsertOnFeedbackSubmitTests.cs`, `Integration/GuestFormPermissionAndChannelEligibilityTests.cs`, `Helpers/LocationGuestChannelPermissionGateTests.cs`

**Interfaces:**
- Consumes: `GuestContactKey.TryCreate`, `IGuestContactSuppressionService.FindAsync`, `ILocationGuestPermissionLedgerService.RecordEvent(LocationGuest, …)` navigation overload.
- Produces: `LedgerEventsForGuestFormSubmit(Restaurant restaurant, bool marketingConsentGranted, ContactType contactType, LocationGuestPermissionState currentMarketingState = NotRecorded, LocationGuestPermissionState currentFeedbackFollowUpState = NotRecorded)`.

- [ ] **Step 1: Write failing tests.**
  - Gate: FFU enabled + current FFU Withdrawn → no FFU grant event; NotRecorded / Granted → grant as today.
  - `NewGuest_WithSuppression_GetsWithdrawEntriesWithOriginalDates` (source `suppression`, `OccurredAt == WithdrawnAtUtc`; profile label `Earlier withdrawal`).
  - `NewGuest_SuppressedAndTicked_GrantsMarketing_KeepsFollowUpWithdrawn` (Review Focus 5).
  - `NewGuest_SuppressedNoTick_StaysWithdrawn`.
  - `ExistingGuest_FollowUpWithdrawnByOperator_NotRegrantedOnSubmit`.
  - `ExistingGuest_IgnoresSuppressionRows` (replay only on create).
- [ ] **Step 2: Run** → FAIL. **Step 3: Implement.** `GuestUpsertService`: on new `LocationGuest` only, `FindAsync` by restaurant + key, `RecordEvent(locationGuest nav, restaurantLocationId, kind, Withdraw, Suppression, row.WithdrawnAtUtc)`. `GuestFormPermissionApplyService`: compute effective states for **all** kinds; for a new guest (`Id == 0`) derive states from tracked `LedgerEntries.Local` for that guest (latest `OccurredAt` per kind) instead of all-NotRecorded; pass marketing and FFU states to the gate.
- [ ] **Step 4: Run** → PASS; `--filter "FullyQualifiedName~Guest|FullyQualifiedName~Permission"` → no new failures.
- [ ] **Step 5: Commit** `feat: returning contact keeps earlier withdrawals; follow-up not re-granted`.

### Task 7: No full contact in logs (R4)

**Files:**
- Create: `Helpers/LogContactMask.cs`
- Modify: `Services/AuthService.cs:1511`, `Services/EmailService.cs:110`, `Services/TeamPermissionsService.cs:1045-1048`, `Services/TrialService.cs:412`, `Services/TwilioRecoveryGuestSmsDelivery.cs` (33-40, 80-84, remove private `MaskForLogs`), `Services/TwilioVerifySmsService.cs` (38-41, 72-76, 120-124, remove private `MaskForLogs`), `Services/TwilioSmsInboundService.cs` (use helper), `Helpers/EmailDispatch.cs:28`
- Test: `Helpers/LogContactMaskTests.cs`, `Helpers/LogTemplateContactGuardTests.cs`

**Interfaces:**
- Produces: `LogContactMask.Email(string? email) -> string` (`a•••@example.com`; blank → `••••`); `LogContactMask.Phone(string? phone) -> string` (`••••6789`); `LogContactMask.Scrub(string? text) -> string` (replaces every email-like and every 7+-digit phone-like substring with its mask).

- [ ] **Step 1: Write failing tests.** Mask outputs for the examples above. Guard test: scan `backend/TummlyBackend/**/*.cs` except `Migrations/`; fail on any non-interpolated string literal containing `{Email}`, `{ToEmail}`, `{Phone}`, `{PhoneNumber}`, `{Mobile}`, `{To}`, `{Recipient}`, `{GuestContact}`, `{Contact}` (message lists file:line).
- [ ] **Step 2: Run** → FAIL. **Step 3: Implement.** Placeholders become `{MaskedEmail}` / `{MaskedPhone}` with masked values. Twilio `ApiException` lines and `EmailDispatch` / `TeamPermissionsService` failure lines no longer pass the exception object: log `{ExceptionType}`, provider code (`ex.Code` for Twilio) and `LogContactMask.Scrub(ex.Message)`.
- [ ] **Step 4: Run** → PASS; `--filter "FullyQualifiedName~Twilio|FullyQualifiedName~Email|FullyQualifiedName~Auth|FullyQualifiedName~Trial|FullyQualifiedName~Team"` → no new failures.
- [ ] **Step 5: Commit** `fix: mask contacts in logs (G3.5)`.

### Task 8: Concurrent Guest Form submit (R5, WS1 defects A, B)

**Files:**
- Create: `Helpers/GuestFormSubmitLock.cs`
- Modify: `Controllers/ScanController.cs` (`SubmitFeedback`, from the duplicate check to the Feedback save)
- Test: `SqlServer/GuestFormConcurrentSubmitSqlTests.cs`, `Integration/SmartGuestLinkEndpointsTests.cs` (or the existing Guest Form submit test class)

**Interfaces:**
- Consumes: `GuestContactKey.TryCreate`; `SqlServerFixture.CreateFactory()`.
- Produces: `GuestFormSubmitLock.TryAcquireAsync(ApplicationDbContext context, int restaurantId, string contactKey, CancellationToken ct = default) -> Task<bool>` — no-op `true` when `!context.Database.IsSqlServer()`; else `sp_getapplock @Resource = 'guest-form:{restaurantId}:{sha256 hex of contactKey}', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000`, true when result ≥ 0. Requires an open transaction.

- [ ] **Step 1: Write failing SQL tests** (factory over SQL Server; reCAPTCHA secret unset in Testing; restaurant with live thank-you Offer):
  - `ConcurrentFirstSubmits_NewContact_OneFeedback_No500`: 2 parallel identical POSTs `/api/scan/{token}/feedback` (start together with a `Barrier`/`Task.WhenAll`) → both 200, 1 Feedback, 1 MasterGuest, 1 Claim, same `claimCode` in both bodies.
  - `ConcurrentSubmits_ExistingGuest_OneFeedback`: guest exists → 2 parallel → 1 new Feedback, same Claim.
  - Repeat each 5 times in the test (loop) to make the race likely.
- [ ] **Step 2: Run** → FAIL (expect 500 / 2 Feedbacks). **Find the cause of A** with the test (why the existing `DbUpdateException` retry did not recover) and write it in the commit message and the PR.
- [ ] **Step 3: Implement.** After reCAPTCHA and before the duplicate check: `GuestContactKey.TryCreate`; begin a transaction on relational providers; `TryAcquireAsync` false → rollback, return `503 {success:false, message:"Please try again."}`. Duplicate check, upsert, permissions, Feedback insert, SaveChanges, commit inside. Classification notify and thank-you Claim issue stay after commit. Keep the retry loop only if the cause analysis shows it is still needed; otherwise remove it.
- [ ] **Step 4: Write test** `SubmitFeedback_LockTimeout_Returns503` (InMemory: inject a lock seam or call through a test double so `TryAcquireAsync` returns false) and the existing `SubmitFeedback_DuplicateWithinWindow_WritesNothingNew` still passes.
- [ ] **Step 5: Run** → PASS; `--filter "FullyQualifiedName~Scan|FullyQualifiedName~GuestForm|FullyQualifiedName~SmartGuestLink|FullyQualifiedName~SqlServer"` → no new failures.
- [ ] **Step 6: Commit** `fix: serialise concurrent Guest Form submits per contact (WS1 A, B)`.

### Task 9: Whole-branch verification, PR, QA evidence

- [ ] **Step 1:** Full backend suite → failing names ⊆ the base list file (list any base test that now passes). Frontend `npm test` → only the 2 base failures. `npm run build` → OK. Three migration scripts → pass.
- [ ] **Step 2:** Update PRD R2.2 (E.164) and the map row; write `.scratch/ws2a-erasure-suppression/` follow-up note: `FeedbackGuestResponse.Body` may hold the first name (G3.7 / D5).
- [ ] **Step 3 (ask Salman first):** set secret `GuestSuppression__HashKey` (random 32 bytes, base64) on `ca-tummly-qa-api`; do not print it.
- [ ] **Step 4 (ask Salman first):** push, open PR to `qa` (body ends with the Claude Code line), link it in T3 Code.
- [ ] **Step 5 (after merge + deploy):** confirm Ready revision = Latest (`scripts/probe-qa-api-revision.sh`), then A7 on QA: race repeat; unsubscribe-form withdraw for the race guest; delete it (Claim TUM-BPAMYK row stays, guest NULL); one suppression row; re-submit same contact → profile shows Withdrawn, `Earlier withdrawal`; Log Analytics query for the race email / number in new lines → none. Record in `.scratch/ws2a-erasure-suppression/qa-evidence-<date>.md`.

# WS2b Contact masking and role isolation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** No ordinary operator surface (API, export, search, AI input, UI) carries a full guest email or mobile, for any role including Owner; full contact stays only in the Owner-only, audited guest data export; Home, Assistant list and workspace controls respect area and Location scope; Staff have no Settings.

**Architecture:** One server helper `GuestContactMask` (the existing `MaskDestination` format) is applied where each operator response is shaped; raw contact stays inside services for eligibility, sends and redaction. Guest search on contact becomes exact-match only. Guest edit gets explicit `replaceEmail` / `replacePhone` flags so the browser never needs the raw value. Staff lose Settings through the existing chrome fields (`accountWorkspaceAccess`, `locationsAccess` = `"none"`), which the existing Dashboard redirect already enforces for direct URLs.

**Tech Stack:** .NET 10 / EF Core 8 (xUnit 2, InMemory, `TummlyWebApplicationFactory`), React + Vite + TS (vitest, source-guard tests with `node:fs`).

**Spec:** `.scratch/ws2b-masking-roles/PRD.md` in the main checkout (`/run/media/salman/D/Freelance/Tummly/.scratch/…`; `.scratch` is gitignored). Rules R1–R7, acceptance A1–A8. Audit: `.scratch/eng-completion-oct26/audit-2026-10-26.md` § ENG10.

## Global Constraints

- Worktree `.worktrees/ws2b-masking-roles`, branch `ws2b-masking-roles` from `origin/qa` 7281ef72. One PR to `qa`. Copy this plan into the worktree in Task 1 and commit it with Task 1.
- **No EF migration.** No change under `backend/TummlyBackend/Migrations/`. WS3b holds the migration slot.
- CI does not run tests. Task 1 records the base failing lists on the branch base. Full suites at the end: **no new failures** against those lists. Watch each new test fail before you write code.
- Mask format (copy exactly from `FeedbackGuestResponseMapping.MaskDestination`, `Helpers/FeedbackGuestResponseMapping.cs:46-84`): email `a••••@example.com` (local length ≤ 1 → `•@domain`; no `@` / empty side → `••••`); phone `••••` + last 4 digits (fewer than 4 digits → `••••`); Unknown → `••••`. A non-blank input never returns an empty string (frontend capability checks test `trim() !== ""`).
- Audit: action `guest.data_export`, target type `restaurant`, `ActorIdentity = "user:{id}"`, `DetailJson = {"format":"xlsx|csv","rows":N}`. No contact, no name.
- Error copy (exact): non-owner export `Only the account owner can export guest data.`
- Commits per task are allowed on the branch. Do not log a full email or phone. Do not run `npx prettier`. Frontend style: no semicolons, double quotes.
- UI rules (AGENTS.md): reuse `src/components/ui` (`Button`, `FloatingLabelInput`, `CheckboxLabel`); no new raw `<button>` or ad-hoc controls; no new Figma (decision 26 Oct: keep layouts).
- Chrome omit rule (CODING_STANDARDS.md "Operator chrome access"): only explicit `"none"` hides; omit keeps owner chrome.
- Report text for the user: ASD-STE100.
- Sub-agents: Claude Haiku 5.5, effort high (AGENTS.md).

## Review Focus

1. **Name-only edit of a guest who has email and mobile.** With masked values in the form, a naive save sends `null` and clears both contacts. Expected: contacts unchanged. Test: Task 10 `PatchIdentity_NameOnly_KeepsEmailAndPhone`.
2. **Malformed stored contact** (`"bob"`, `"12"`). Mask must return `••••`, not empty, so Respond / recovery capability stays "Email available". Test: Task 2 `MaskEmail_Malformed_ReturnsDots`.
3. **Contact search with spaces, case or national format.** `"07700 900456"`, `"+44 7700 900456"`, `"ALEX@Example.COM"` must find the guest; `"0770090"` and `"example.com"` must find nothing. Test: Task 4 `Search_ExactContactForms_MatchAndPartialsDoNot`.
4. **Old client or bookmark sends `includeGuestContact=true`.** Export must still contain only masked contact. Test: Task 5 `Export_IncludeGuestContactTrue_StillMasked`.
5. **Single-guest "Export guest record"** (profile → `/api/guests/export?guestIds=`) must be masked like the list export. Test: Task 3 `Export_SelectedGuest_IsMasked`.

---

### Task 1: Baseline and shared role/scope seed

**Files:**
- Create: `backend/TummlyBackend.Tests/Integration/Support/OperatorScopeSeed.cs`
- Create: `docs/superpowers/plans/2026-10-26-ws2b-masking-roles.md` (copy of this file)

**Interfaces:**
- Produces: `OperatorScopeSeed.SeedAsync(TummlyWebApplicationFactory factory, string memberRole, bool namedInScopeOnly = true) -> Task<OperatorScopeFixture>`.
- Produces: `sealed record OperatorScopeFixture(string OwnerJwt, string MemberJwt, int OwnerUserId, int MemberUserId, int RestaurantId, int InScopeLocationId, int OutOfScopeLocationId, int OtherTenantLocationId, int EmailGuestId, int PhoneGuestId, int OutOfScopeGuestId, int EmailFeedbackId, int PhoneFeedbackId, string GuestEmail, string GuestMobile)`. `GuestEmail = $"ws2b-{Guid:N}@example.com"`, `GuestMobile = "07700900987"` (MasterGuest `Mobile = "07700900987"`, `NormalizedPhone = "07700900987"`). Email guest + phone guest + one Feedback each (with `GuestContact`, `ContactType`, `LocationGuestId`) at the in-scope Location; one guest at the out-of-scope Location; a second restaurant (other tenant) with one Location. Pattern: copy `SeedOwnerAndMemberAsync` (`Integration/RestaurantWidePermissionEndpointsTests.cs:342-494`) incl. `jwtService.GenerateToken(id, email, role)`.

- [ ] **Step 1: Worktree.** `git worktree add .worktrees/ws2b-masking-roles -b ws2b-masking-roles origin/qa`.
- [ ] **Step 2: Backend base list.** `cd backend && dotnet test TummlyBackend.Tests --logger "trx;LogFileName=ws2b-base.trx"`; then `grep -o '<UnitTestResult [^>]*outcome="Failed"' TummlyBackend.Tests/TestResults/ws2b-base.trx | sed -E 's/.*testName="([^"]*)".*/\1/' | sort -u > /run/media/salman/D/Freelance/Tummly/.scratch/ws2b-masking-roles/baseline-failing-backend-7281ef72.txt`. Record the count in the ledger.
- [ ] **Step 3: Frontend base list.** `npx vitest run 2>&1 | grep -E "^ (FAIL|×)" > /run/media/salman/D/Freelance/Tummly/.scratch/ws2b-masking-roles/baseline-failing-frontend-7281ef72.txt` (expected: `assistantWaitGerunds` only). Also `npm run typecheck` → record error count.
- [ ] **Step 4: Write `OperatorScopeSeed`** and a smoke test `OperatorScopeSeedTests.Seed_CreatesTwoTenantsAndScopedMember` (member JWT `GET /api/guests?locationId={InScope}` → 200 for `PermissionRoles.LocationManager`).
- [ ] **Step 5: Run** the smoke test → PASS.
- [ ] **Step 6: Commit** `test: shared operator scope seed for WS2b`.

### Task 2: `GuestContactMask` helper (R1.1)

**Files:**
- Create: `backend/TummlyBackend/Helpers/GuestContactMask.cs`
- Modify: `Helpers/FeedbackGuestResponseMapping.cs:43-84` (delete `MaskDestination`), `Helpers/FeedbackGuestResponseComposer.cs:125`, `Helpers/BulkRecoveryBatchMapping.cs:56-61`
- Test: `backend/TummlyBackend.Tests/Helpers/GuestContactMaskTests.cs`

**Interfaces:**
- Produces: `GuestContactMask.Mask(ContactType contactType, string contact) -> string` (old `MaskDestination` body, unchanged); `GuestContactMask.MaskEmail(string? email) -> string?`; `GuestContactMask.MaskPhone(string? phone) -> string?` (null / blank → null; else `Mask(Email|Phone, value)`).

- [ ] **Step 1: Write failing tests:** `Mask_Email_KeepsFirstCharAndDomain` (`"alex@example.com"` → `"a••••@example.com"`), `Mask_ShortLocal` (`"a@b.com"` → `"•@b.com"`), `MaskEmail_Malformed_ReturnsDots` (`"bob"` → `"••••"`), `MaskPhone_LastFour` (`"07700 900987"` → `"••••0987"`), `MaskPhone_TooShort` (`"12"` → `"••••"`), `MaskEmail_NullOrBlank_ReturnsNull`, `Mask_Unknown_ReturnsDots`.
- [ ] **Step 2: Run** `dotnet test --filter GuestContactMaskTests` → FAIL (type missing).
- [ ] **Step 3: Implement**; switch the two callers to `GuestContactMask.Mask`; delete `MaskDestination`.
- [ ] **Step 4: Run** `--filter "GuestContactMaskTests|FeedbackGuestResponse|BulkRecovery"` → PASS / no new failures.
- [ ] **Step 5: Commit** `refactor: one guest contact mask helper`.

### Task 3: Guests list, CSV export, profile (R2.1–R2.3)

**Files:**
- Modify: `Services/GuestsListService.cs:589-592` (`ShapedGuestRow.Email/Mobile` = masked; status derivations at 598-608 keep `masterGuest.*`), `Services/GuestProfileService.cs:278-282` (`profileSummary.email/mobile` masked)
- Test: `Integration/GuestsListEndpointsTests.cs`, `Integration/GuestProfileDetailEndpointsTests.cs` (update raw-value assertions to masked), new `Integration/GuestContactMaskingEndpointsTests.cs`

**Interfaces:**
- Consumes: Task 1 `OperatorScopeSeed`; Task 2 `GuestContactMask.MaskEmail/MaskPhone`.

- [ ] **Step 1: Write failing tests** (`GuestContactMaskingEndpointsTests`, Owner JWT): `GuestsList_ReturnsMaskedContact` (body text has no `GuestEmail`, no `"07700900987"`; row `email == MaskEmail(GuestEmail)`); `GuestsExport_ReturnsMaskedContact` (CSV text); `Export_SelectedGuest_IsMasked` (`guestIds={EmailGuestId}`); `GuestProfile_ReturnsMaskedContact` (`profileSummary.email`, `profileSummary.mobile`); `GuestProfile_ContactEligibility_StillAvailable` (email row status unchanged vs before: not `not_provided`).
- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3: Implement.** Mask at the shaping points only.
- [ ] **Step 4: Run** the new class + updated classes → PASS.
- [ ] **Step 5: Commit** `fix: mask guest contact in guests list, export and profile`.

### Task 4: Exact-match contact search; search subtitle (R3, R2.4)

**Files:**
- Create: `backend/TummlyBackend/Helpers/GuestContactSearchTerm.cs`
- Modify: `Helpers/GuestsListQueryComposer.cs:173-203` (`ApplySearch`), `Services/GlobalSearchService.cs:548-552` (`ToGuestHit` subtitle masked)
- Test: `Helpers/GuestContactSearchTermTests.cs`, `Integration/GuestsListEndpointsTests.cs:431` (rewrite `GetGuests_SearchMatchesNameEmailOrMobile` → `GetGuests_SearchMatchesNameOrExactContact`), `Integration/GlobalSearchEndpointsTests.cs` (subtitle assertions → masked)

**Interfaces:**
- Produces: `GuestContactSearchTerm.Parse(string query) -> GuestContactSearchTerm`; `record GuestContactSearchTerm(string NameTerm, string? ExactEmail, IReadOnlyList<string> PhoneDigitForms)`. `ExactEmail` = trim + lower when `GuestFormContactValidate.TryResolve(query, out Email)`; `PhoneDigitForms` when `PhoneNumberHelper.TryNormalizeToE164(query, "GB", out e164)`: `[e164 without "+", "0" + national significant number]` (e.g. `+447700900456` → `["447700900456", "07700900456"]`; `MasterGuest.NormalizedPhone` is digits-only of what was typed — `GuestUpsertService.NormalizePhone`).
- `ApplySearch` new predicate: `Name.ToLower().Contains(nameTerm)` OR (`ExactEmail != null` AND `NormalizedEmail == ExactEmail`) OR (`PhoneDigitForms` non-empty AND `PhoneDigitForms.Contains(NormalizedPhone)`). No `Contains` on any contact column.

- [ ] **Step 1: Write failing tests.** Unit: `Parse_Email_CaseFolded`, `Parse_UkMobile_National_And_Intl_SameForms`, `Parse_PartialNumber_NoPhoneForms` (`"0770090"`), `Parse_Domain_NoEmail` (`"example.com"`). Integration `Search_ExactContactForms_MatchAndPartialsDoNot`: `q=07700 900456`, `q=+44 7700 900456`, `q=ALEX@Example.COM` → 1 row each; `q=0770090`, `q=example.com`, `q=900456` → 0 rows; `q=opt` (name) → 1 row. Global search: `Search_GuestSubtitle_IsMasked`, `Search_PartialEmail_DoesNotMatchGuest`.
- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3: Implement.** Both callers pass the raw query to `GuestContactSearchTerm.Parse` inside `ApplySearch` (signature unchanged: `ApplySearch(IQueryable<LocationGuest>, string normalizedQuery)`).
- [ ] **Step 4: Run** `--filter "GuestContactSearchTerm|GuestsListEndpoints|GlobalSearch"` → PASS / no new failures.
- [ ] **Step 5: Commit** `fix: guest contact search is exact-match only; mask search subtitle`.

### Task 5: Feedback list, detail, export; export dialog (R2.5, R2.6)

**Files:**
- Modify: `Controllers/FeedbackController.cs:138` (recent list), `:760` (detail) → `guestContact = GuestContactMask.Mask(f.ContactType, f.GuestContact ?? "")` (blank stays `""`); `:448`, `:562` keep the `includeGuestContact` parameter but ignore it (comment: kept for old clients).
- Modify: `Services/FeedbackInboxListService.cs:253-259` (always `ExportBaseHeaders + ExportContactHeaders`), `:1158`, `:1217-1230` (always add name + masked email / masked mobile cells); `DTOs/Feedback/FeedbackExportDtos.cs:24` (delete `IncludeGuestContact`)
- Check: any Feedback inbox list DTO that carries `GuestContact` (grep `FeedbackInboxListService` row projection) → mask the same way.
- Modify (frontend): `src/components/dashboard/operator/Feedback/FeedbackExportDialog.tsx:244-269` (remove contact summary row and `CheckboxLabel`), `FeedbackPage.tsx:433`, `src/lib/operatorFeedback/createOperatorFeedbackPageModule.ts` (remove `exportIncludeGuestContact` state, `setExportIncludeGuestContact`), `feedbackExportQueryParams.ts:13,33-42`, `src/api/dashboardApi.ts:866` (drop the param) and their tests.
- Test: `Integration/FeedbackExportEndpointsTests.cs`, `Integration/FeedbackDetailsEndpointsTests.cs`, `GuestContactMaskingEndpointsTests.cs`

- [ ] **Step 1: Write failing tests:** `FeedbackRecent_ReturnsMaskedContact` (`GET /api/feedback?locationId=`), `FeedbackDetail_ReturnsMaskedContact`, `Export_AlwaysHasMaskedContactColumns` (headers end with `Guest,Email,Mobile`; no raw value), `Export_IncludeGuestContactTrue_StillMasked`, `ExportSingle_IsMasked`. Frontend: `createOperatorFeedbackPageModule.test.ts` — export request has no `includeGuestContact` key.
- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run** backend Feedback classes + `npx vitest run src/lib/operatorFeedback src/api` → PASS / no new failures.
- [ ] **Step 5: Commit** `fix: mask guest contact in Feedback list, detail and exports`.

### Task 6: Home latest-activity and performance scope; checklist (R2.7, R6.1)

**Files:**
- Modify: `Controllers/HomeLatestActivityController.cs` (inject `IRestaurantPermissionHelper`; replace `_ownedLocation.ResolveAsync` gate)
- Modify: `Controllers/HomePerformanceController.cs` (gate `AuthorizeLocationAsync(User, OperatorAreaIds.Reports, PermissionLevel.View, locationId)` → `ToHttpResult()`)
- Test: `Integration/HomeScopeEndpointsTests.cs`

**Rules:**
- Latest activity: `feedbackDecision = AuthorizeLocationAsync(User, Feedback, View, locationId)`, `guestsDecision = … Guests …`. Location not found / not assigned / other tenant → the decision's 403/404 (use `ToHttpResult()` of `feedbackDecision` when both deny for a Location reason). Area deny only → 200 with only the kinds the actor may view (`items: []` when none). Feedback items: `guestContact` masked (Task 2).
- Checklist-acks (`Controllers/ChecklistAcksController.cs`): keep as is — it already enforces assigned-Location scope via `OwnedLocationService.ResolveAsync` (scoped ids) and holds no guest data. Add a deny test only.

- [ ] **Step 1: Write failing tests** (seed from Task 1): `LatestActivity_MasksContact` (Owner); `LatestActivity_UnassignedLocation_403` (LocationManager, `OutOfScopeLocationId`); `LatestActivity_OtherTenant_Denied`; `LatestActivity_MarketingRole_OnlyGuestJoinedItems` (no `kind == "feedback"`); `LatestActivity_StaffRole_EmptyItems`; `Performance_StaffRole_403`; `Performance_LocationManager_Assigned_200`; `ChecklistAcks_UnassignedLocation_403`.
- [ ] **Step 2: Run** → FAIL. **Step 3: Implement.** **Step 4: Run** `--filter "HomeScopeEndpoints|HomeLatestActivity|HomePerformance"` → PASS / no new failures.
- [ ] **Step 5: Commit** `fix: Home activity and performance follow area and Location scope`.

### Task 7: Assistant — no raw contact to the model; conversation list scope (R2.8, R6.2)

**Files:**
- Test only for R2.8: `backend/TummlyBackend.Tests/Helpers/AssistantContactPromptGuardTests.cs`
- Modify: `Controllers/AssistantController.cs:80-110` (`ListConversations`), `Services/AssistantConversationService.cs:386-410` (`ListAsync`), its interface
- Test: `Integration/AssistantConversationScopeEndpointsTests.cs`

**Findings (verified on `qa`):** `AssistantFeedbackRetrieve` / `AssistantGuestsRetrieve` load raw contact only for status derivation and `ContactRedactionTokens`; evidence rows carry no contact; `AssistantLiveAnswerStructuredOutput.BuildEvidencePayload` (`Helpers/AssistantLiveAnswerStructuredOutput.cs:~1266-1320`) serialises rows via `PromptFeedbackRow` / `PromptGuestRow` and does not include `ContactRedactionTokens`. So R2.8 needs a guard test, not a code change.

**Interfaces:**
- Produces: `ListAsync(int ownerUserId, bool archived, IReadOnlyCollection<int> allowedLocationIds, CancellationToken ct = default)`. Rows: `ScopeKind == all` → kept (the all-scope gate already requires an AiAssistant location set, `AssistantController.cs:363-375`); single scope → kept only when `OwnedLocationId ∈ allowedLocationIds`. Controller passes `AuthorizeLocationSetAsync(User, OperatorAreaIds.AiAssistant, PermissionLevel.View).LocationIds`.

- [ ] **Step 1: Write failing tests.** Guard: `EvidencePayload_ContainsNoRawContact` — build `AssistantFeedbackEvidence` / `AssistantGuestsEvidence` through the two retrieve services on an InMemory context seeded with `alex.guard@example.com` and `07700900321` (also as `ContactRedactionTokens`), call `BuildEvidencePayload(...)`, `ToJsonString()` contains neither value. (This passes today; it must stay green — write it first and confirm PASS, it is a regression lock.) Scope: `ConversationList_HidesSingleScopeForUnassignedLocation` (seed a conversation with `OwnedLocationId = OutOfScopeLocationId`, member LocationManager named in-scope only → not listed; in-scope one listed).
- [ ] **Step 2: Run** → guard PASS, scope FAIL. **Step 3: Implement** scope. **Step 4: Run** `--filter "AssistantContactPromptGuard|AssistantConversationScope|AssistantEndpoints"` → no new failures.
- [ ] **Step 5: Commit** `fix: Assistant conversation list follows Location scope; lock no-contact prompt payload`.

### Task 8: Guest data export — Owner only, audited (R4)

**Files:**
- Modify: `Services/GuestDataExportService.cs:24-28` → `ExportAsync(int restaurantId, int actorUserId, string? format)`; owner check after the restaurant load (`restaurant.OwnerUserId != actorUserId` → `(null, "Only the account owner can export guest data.", 403)`); after a successful build `_audit.Append(...)` + `SaveChangesAsync`; interface `Interfaces/IGuestDataExportService.cs`
- Modify: `DTOs/AccountWorkspace/GuestDataExportDtos.cs` (add `int RowCount`), `Models/AdminAuditEvent.cs` (`AdminAuditActions.GuestDataExport = "guest.data_export"`)
- Modify: `Controllers/AccountWorkspaceController.cs:313-366` (pass `userId`)
- Modify (frontend): `src/components/dashboard/operator/AccountWorkspace/AccountWorkspacePage.tsx:1593-1602` (`disabled={snap.isSaving || !snap.isAccountOwner}`; show existing `dangerZoneOwnerOnlyHelper`-style helper text under the button: new copy key `exportGuestDataOwnerOnlyHelper: "Only the account owner can export guest data."` in `src/lib/operatorAccountWorkspace/accountWorkspacePresentation.ts:345`)
- Test: `Integration/AccountWorkspaceEndpointsTests.cs` (or new `GuestDataExportEndpointsTests.cs`)

- [ ] **Step 1: Write failing tests:** `GuestDataExport_Owner_200_WritesOneAuditWithoutContact` (one `guest.data_export` row, `DetailJson` contains `"rows"` and no `GuestEmail`); `GuestDataExport_AdminWithManage_403` (message exact); `GuestDataExport_Staff_403`; `GuestDataExport_Owner_ContainsFullContact` (rights path stays full: file text contains `GuestEmail`).
- [ ] **Step 2: Run** → FAIL. **Step 3: Implement.** **Step 4: Run** → PASS; `npx vitest run src/lib/operatorAccountWorkspace` → no new failures.
- [ ] **Step 5: Commit** `fix: guest data export is Owner-only and audited`.

### Task 9: Staff Settings and workspace controls (R6.3, R7.1, R7.4, R7.5)

**Files:**
- Modify: `Helpers/DefaultPermissionMatrix.cs` Staff column (`[OperatorAreaIds.AccountWorkspace] = PermissionLevel.NoAccess`; Locations stays `Scoped` because `GET /api/restaurant/locations` gates on Locations View and the location switcher needs it)
- Modify: `Controllers/RestaurantController.cs:195-213` — after `permissionRole` is known: `if (permissionRole == PermissionRoles.Staff) locationsAccess = "none";` (Settings > Locations chrome only)
- Modify: `Controllers/AccountWorkspaceController.cs:246`, `:285` (`pause` / `resume` gate `PermissionLevel.Manage`)
- Modify test: `Integration/RestaurantWidePermissionEndpointsTests.cs:51-66` (`AccountWorkspace_Returns200_WhenNamedLocationScopeAndView` → use `PermissionRoles.LocationManager`; add `AccountWorkspace_Staff_Returns403`)
- Test: `Integration/StaffSettingsEndpointsTests.cs`

**Note (verified):** the frontend already redirects a denied area on direct URL (`src/components/dashboard/operator/Dashboard.tsx:374-388` with `resolveOperatorSidebarActiveId`, `src/lib/operatorHome/operatorDashboardPaths.ts:355-399`, which maps `settings/account-workspace` and `settings/locations[/:id]`). Chrome `"none"` is therefore enough for R7.1 / R7.2; Task 10 adds the frontend test.

- [ ] **Step 1: Write failing tests:** `StaffLocationsPayload_HidesSettingsChrome` (`GET /api/restaurant/locations` as Staff → 200, `locationsAccess == "none"`, `accountWorkspaceAccess == "none"`, `locations` non-empty); `OwnerLocationsPayload_KeepsManageChrome`; `AccountWorkspace_Staff_Returns403`; `Pause_ViewRole_Returns403` (LocationManager); `StaffRedeemCheck_StillAllowed` (`POST /api/offers/redeem/check` as Staff at in-scope Location → not 403; reuse body shape from `Integration/OffersRedeemEndpointsTests.cs`).
- [ ] **Step 2: Run** → FAIL. **Step 3: Implement.** **Step 4: Run** `--filter "StaffSettings|RestaurantWidePermission|AccountWorkspace|OffersRedeem|TeamPermissions"` → PASS / no new failures.
- [ ] **Step 5: Commit** `fix: Staff have no Settings; workspace pause/resume need Manage`.

### Task 10: Guest edit "Replace" without the raw value (R5.3)

**Files:**
- Modify: `DTOs/Guests/GuestIdentityPatchDtos.cs:3-12` (add `bool ReplaceEmail`, `bool ReplacePhone`), `Services/GuestIdentityUpdateService.cs:29-190` (when a flag is false, keep the current `Email`/`NormalizedEmail` (or `Mobile`/`NormalizedPhone`), skip its validation, uniqueness check and `changedFields`; the "at least one contact" check uses the effective result)
- Modify (frontend): `src/lib/operatorGuestProfile/guestIdentityForm.ts:8-24, 62-114` (draft adds `emailReplacing: boolean`, `phoneReplacing: boolean`, `emailMasked: string`, `phoneMasked: string`; payload adds `replaceEmail`, `replacePhone`; validation treats a not-replaced field as present when its masked value is non-empty), `createOperatorGuestProfilePageModule.ts:357-365` (`draftFromProfile`: `email: ""`, `phone: ""`, masked from `profileSummary`, both `replacing: false`) + a `startReplaceContact(field: "email" | "phone")` action, `src/components/dashboard/operator/GuestProfile/GuestEditPage.tsx:129-142` (not replacing → `FloatingLabelInput readOnly value={masked}` + `Button variant="link"` "Replace"; replacing → empty editable input as today), `src/api/dashboardApi.ts:1233` (send the flags)
- Test: `Integration/GuestIdentityPatchEndpointsTests.cs` (create or extend the existing PATCH test class), `src/lib/operatorGuestProfile/guestIdentityForm.test.ts`, `createOperatorGuestProfilePageModule.test.ts`

- [ ] **Step 1: Write failing tests.** Backend: `PatchIdentity_NameOnly_KeepsEmailAndPhone` (`ReplaceEmail=false`, `ReplacePhone=false`, `Email=null`); `PatchIdentity_ReplaceEmail_UpdatesEmail`; `PatchIdentity_ReplaceEmailWithBlank_ClearsWhenPhoneExists`; `PatchIdentity_ReplaceBothBlank_400` (existing message). Frontend: `validateGuestIdentityDraft` — not replacing + masked present → `ok`, payload `replaceEmail: false, email: null`; replacing + invalid → `errors.email`; `draftFromProfile` never copies the profile email into `draft.email`.
- [ ] **Step 2: Run** → FAIL. **Step 3: Implement.** **Step 4: Run** backend identity tests + `npx vitest run src/lib/operatorGuestProfile` → PASS.
- [ ] **Step 5: Commit** `fix: guest edit replaces contact without reading the raw value`.

### Task 11: Frontend contact links, client search, Locations actions, Staff routes (R3.5, R5.1, R5.2, R7.1–R7.3)

**Files:**
- Modify: `src/components/dashboard/operator/Guests/GuestsSmartGroupsSection.tsx:436-452` (plain text in `<span className={GUESTS_TABLE_LOCATION_CLASS}>`; drop `formatPhoneForDisplay` for the masked mobile), `src/components/dashboard/operator/Guests/GuestDetailsDrawer.tsx:310-314` (no `href`), `src/lib/operatorGuests/createGuestDetailsModule.ts:~280-290` (drop the raw `email` used for `href`), `src/lib/operatorGuests/buildGuestsViewModel.ts:61-74` (`guestMatchesSearch` → name only)
- Modify: `src/components/dashboard/operator/Dashboard.tsx:610-620, 660-700` (add `locationsAccess` to `DashboardOutletContext` and the `Outlet` context), `src/components/dashboard/operator/Locations/LocationsPage.tsx:214-233` (render Add / Import only when `locationsAccess !== "view" && locationsAccess !== "none"` — omit keeps them)
- Test: `src/components/dashboard/operator/Guests/guestContactChrome.test.ts` (source guard, pattern `src/lib/operatorShop/shopOutOfScopeChrome.test.ts`), `src/lib/operatorGuests/buildGuestsViewModel.test.ts`, `src/lib/operatorHome/operatorAreaChromeAccess.test.ts`, `src/components/dashboard/operator/Locations/locationsWriteChrome.test.ts` (source guard)

- [ ] **Step 1: Write failing tests:** source guard — `GuestsSmartGroupsSection.tsx` and `GuestDetailsDrawer.tsx` do not match `/mailto:|tel:/`; `buildOperatorGuestsViewModel` with `searchQuery: "example.com"` returns 0 rows while the guest email contains it, and name search still matches; chrome — `isDeniedOperatorSidebarActiveId(resolveOperatorSidebarActiveId("/single-dashboard/settings/locations/7"), buildOperatorSidebarAreaAccess({...allManage, locationsAccess: "none"}))` is `true`, same for `settings/account-workspace`, and `"offers"` stays allowed for a Staff-shaped access map; Locations guard — `LocationsPage.tsx` gates `copy.addLocation` / `copy.importLocations` on `locationsAccess`.
- [ ] **Step 2: Run** `npx vitest run <files>` → FAIL. **Step 3: Implement.** **Step 4: Run** → PASS; `npm run typecheck` → no new errors.
- [ ] **Step 5: Commit** `fix: no guest mailto/tel; name-only client search; Locations writes need Manage`.

### Task 12: Raw-contact sweep, full suites, PR (R2.9, A1, A5)

**Files:**
- Test: `Integration/GuestContactMaskingEndpointsTests.cs` (add the sweep)

- [ ] **Step 1: Write the sweep test** `OrdinarySurfaces_NeverReturnRawContact` as `[Theory]` over roles `Owner`, `LocationManager`, `Marketing` (member JWT; Owner uses `OwnerJwt`) × GET surfaces: `/api/guests?locationId=`, `/api/guests/export?locationId=`, `/api/guests/{EmailGuestId}?locationId=`, `/api/guests/{EmailGuestId}/feedbacks?locationId=`, `/api/guests/{EmailGuestId}/activity?locationId=`, `/api/feedback?locationId=`, `/api/feedback/{EmailFeedbackId}?locationId=`, `/api/feedback/export?locationId=&format=csv&scope=current`, `/api/home/latest-activity?locationId=`, `/api/search?locationId=&q={GuestEmail}`. For each 200 response: body text contains neither `GuestEmail` nor `GuestMobile` nor `"447700900987"`. Non-200 is allowed (role without area). Add `UnassignedAndOtherTenant_Denied` `[Theory]` over the same list with `OutOfScopeLocationId` and `OtherTenantLocationId` for LocationManager → status 403 or 404.
- [ ] **Step 2: Run.** Any leak found → fix it in the owning service with `GuestContactMask` (R2.9) and re-run.
- [ ] **Step 3: Full suites.** Backend trx run (Task 1 command, file `ws2b-final.trx`) → compare names with the base list: no new failures. `npx vitest run` → only base failures. `npm run typecheck`, `npm run build` → no new errors.
- [ ] **Step 4: Commit** `test: sweep ordinary surfaces for raw guest contact`; push; open PR to `qa` titled `WS2b: guest contact masking and role isolation` with test results and the QA A8 checklist (captures of guests list, profile, search, Feedback, CSV as Owner; Staff URL probe; export audit row).

# Admin Panel Spec V1.1 — implementation lanes

**Spec:** `Tummly_Internal_Admin_Panel_Functional_Spec_V1_1_26Oct2026.docx`. Plain-text copy: `/run/media/salman/D/Freelance/Tummly/.scratch/admin-panel-spec-v1-1/spec.txt` (line numbers below refer to this file).
**Audit (gap register, per requirement ID):** `/run/media/salman/D/Freelance/Tummly/.scratch/admin-panel-spec-v1-1/audit-2026-10-28.md`
**Base:** `origin/qa` at `249b5d76`. Integration branch: `feat/admin-spec-v1-1`.

Each task below is one **lane**. One implementer subagent owns one lane, in its own worktree and branch. Lanes run in parallel. The controller merges lanes into `feat/admin-spec-v1-1`.

## Global Constraints (bind every lane)

1. **Code is the source of truth.** `docs/`, `docs/adr/`, `.scratch/` and plan files can be stale. Read code before you change it.
2. **Scope is the lane only.** Do not fix other lanes' items. Do not refactor outside the files the lane needs.
3. **Out of scope everywhere (blocked by Director / legal / provider decision):** new staff roles (Finance, Compliance, Fulfilment) (O05); Pricebook editor workflow changes (O01); Revolut mid-term proration changes (O02); Group self-serve cap above 30 (provider variation); privacy case register (ADM-16); legal CMS (ADM-17); interest CSV export (O07 template); print label templates (ADM-11-04/05); customer Shop page regrouping (ADM-09-09, needs Figma from Mohamed); flipping `PUBLIC_TRUST_CLAIMS_APPROVED`.
4. **Server owns every rule.** New admin endpoints use `[Authorize(Roles = "Admin")]`. UI hiding is never the only check.
5. **Audit every consequential admin action** through `IAdminAuditService.Append(new AdminAuditAppendRequest(...))` (`backend/TummlyBackend/Interfaces/IAdminAuditService.cs`). Add new action names as constants in `AdminAuditActions` (`backend/TummlyBackend/Models/AdminAuditEvent.cs`). Put before/after values and the reason in `DetailJson`.
6. **No secrets and no guest PII** in admin responses or audit detail: no API keys, tokens, card data, raw guest email/phone. Provider references are shown redacted (for example last 6 characters).
7. **Idempotent writes.** Follow the `Idempotency-Key` header pattern in `AdminController.cs` `payment-refunds` (around line 673-691) for new money/credit/state writes.
8. **EF migrations:** generate with `dotnet tool run dotnet-ef migrations add <LaneN_Name>` from `backend/TummlyBackend` (run `dotnet tool restore` once). Never hand-write `*.Designer.cs` or snapshot edits. At most ONE migration per lane. Follow `CODING_STANDARDS.md` (no multiple cascade paths, SQL Server error 1785). The controller may regenerate the migration at merge time if snapshots conflict.
9. **Frontend:** reuse `src/components/ui/` primitives (`Button`, `Badge`, `Table`, `Dialog`, `Drawer`, `Select`, `Textarea`, `Tabs`, etc.) and follow the existing admin panels in `src/components/dashboard/admin/` for layout, loading, empty and error states. Fonts follow `docs/agents/typography.md`. Menus inside a drawer or dialog must render above it. No raw `<button>`. Each admin action shows loading, validation error, success, permission denial (403), duplicate (409) and failure states.
10. **Money:** store pence (int). Show pounds with 2 decimals (`£9.99`).
11. **Tests:** backend xUnit in `backend/TummlyBackend.Tests` (service tests under `Services/`, endpoint tests under `Integration/` using `TummlyWebApplicationFactory`; see `AdminPilotExtensionEndpointsTests.cs`). Frontend Vitest for logic in `src/lib/admin/*.test.ts`. Run focused tests while you work. Before the final commit run: `dotnet build backend/TummlyBackend.Tests`, the lane's backend tests, `npm run typecheck`, `npm run lint`, and `npx vitest run <touched test files>`. Do not run the full SQL Server collection unless your lane touches it (memory is limited; other lanes run in parallel).
12. **Git:** commit on your lane branch only, small conventional commits (`fix(shop): ...`, `feat(admin): ...`). End each commit message with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Never push, merge, rebase onto other branches, or touch other worktrees.

---

### Task 1: Lane 1 — Staff seed security and email suppression

Requirement IDs: ADM-08-01, ADM-18-02 (seed), ADM-13-03.

1. **Default staff logins.** `backend/TummlyBackend/Program.cs` (around lines 1732-1763, inside `InitializeDatabaseAsync`) creates `admin@tummly.com` / `Admin@123` when the `Admins` table is empty and `support@tummly.com` / `Support@123` when no Support row exists. This runs on every environment except Testing.
   - Change: seed staff accounts only when the password comes from configuration: `StaffSeed:AdminEmail`, `StaffSeed:AdminPassword`, `StaffSeed:SupportEmail`, `StaffSeed:SupportPassword`. In the `Development` environment only, fall back to the current hard-coded values. In any other environment with no configured password, do not create the account and log one warning (no password in the log).
   - Existing rows are never changed.
   - Add the keys (empty values) to the appropriate `appsettings*.json` / `.env.example` pattern used by the backend, if one exists.
   - Tests: unit-test the decision logic (extract a small static helper, e.g. `StaffSeedPolicy`) for Development / Production / QA with and without config.
2. **Bounce and complaint suppression.** `backend/TummlyBackend/Services/ResendWebhookService.cs` handles `email.bounced` and `email.failed` only for guest-reply state (lines ~20-23, ~142-143). `email.complained` is not handled.
   - Change: for `email.bounced` (hard bounce only, if Resend gives a bounce type; otherwise all bounces) and new `email.complained`, write a marketing suppression for that recipient through the existing `GuestContactSuppressionService` (`backend/TummlyBackend/Services/GuestContactSuppressionService.cs`), scoped the same way unsubscribe does it (read `PublicUnsubscribeController.cs` ~84-110 and how campaign recipients map to a restaurant/guest via `CampaignRecipientDelivery`). Also withdraw Email marketing permission in the permission ledger if unsubscribe does so; mirror unsubscribe exactly, with a distinct source/reason (`bounce`, `complaint`).
   - Idempotent: a repeated webhook does not create a second suppression or ledger row.
   - Never re-grant. Keep existing guest-reply state updates.
   - Tests: webhook service tests for bounce, complaint, duplicate event, unknown email id.

### Task 2: Lane 2 — Shop catalogue governance

Requirement IDs: ADM-09-03, ADM-09-04, ADM-09-05, ADM-09-08, ADM-09-10.

Files: `backend/TummlyBackend/Services/AdminShopCatalogService.cs`, `AdminShopInterestProductService.cs`, `Controllers/AdminShopCatalogController.cs`, `Controllers/AdminShopInterestController.cs`, `src/components/dashboard/admin/AdminShopCatalogPanel.tsx`, `AdminShopInterestPanel.tsx`, `src/lib/admin/adminShopCatalogAvailability.ts`, `src/components/dashboard/operator/Shop/ShopCatalogItemCard.tsx`.

1. **Catalogue change history (09-08).** Audit create, edit (text, packs, prices, delivery, supplier/cost), publish, unpublish, delete, tag and image changes. `DetailJson` holds changed fields with old and new values (only changed fields). Availability audit (already exists ~545-553) must also include old value. Add an optional `approvalReference` string (max 200) to the availability request and store it in the audit detail.
2. **Readiness gate (09-04).** Turning `AvailableInShop` ON is refused server-side (400 with a clear code, e.g. `sku_not_ready`) unless: SKU is Published, has ≥1 image, has ≥1 pack with price > 0, delivery price set, and `SupplierName` is non-empty. Turning OFF is always allowed. Show the refusal reason in the UI.
3. **Confirmation (09-03).** UI: the availability switch opens a confirmation dialog (product, new state, optional approval reference) before the request.
4. **Promote does not enable checkout (09-10).** `AdminShopInterestProductService.cs` ~398 sets `sku.AvailableInShop = true` on promote. Change: promote creates/publishes the SKU with `AvailableInShop = false`. Making it buyable goes through the availability switch and its readiness gate. Audit promote and demote (actor, product key, SKU id). Promote must refuse (400) an incomplete record (no image, no pack/price). UI copy states that a promoted product stays "Coming soon" until switched on.
5. **Price display (09-05).** `ShopCatalogItemCard.tsx` ~92 renders `£{price.toFixed(0)}`. Show 2 decimals (`£9.99`). Check other Shop price renders that use `toFixed(0)` for money and fix the same way.
6. Tests: service tests for gate, promote, audit detail; vitest for any extracted presentation logic.

### Task 3: Lane 3 — Shop orders and print jobs

Requirement IDs: ADM-10-02, ADM-10-04, ADM-10-06, ADM-10-09, ADM-11-02, ADM-11-07 (audit part), ADM-09-08 (print title snapshot).

Files: `Services/AdminShopOrderFulfilmentService.cs`, `Controllers/AdminShopOrdersController.cs`, `DTOs/Admin/AdminShopOrderDtos.cs`, `Services/ComplimentaryStarterShopOrderService.cs`, `PrintReadyQrMaterials/StarterQrMaterialTypes.cs`, `Services/PrintReadyQrMaterialsService.cs`, `src/components/dashboard/admin/AdminShopOrderDetailDrawer.tsx`, `src/api/adminShopOrdersApi.ts`.

1. **Order detail (10-02).** Add to the admin order detail DTO and drawer: ship-to name and address (already stored for CSV, see `AdminShopOrderFulfilmentService.cs` ~31-33), delivery pence, net/VAT/gross snapshot if stored on the order, payment source (paid via Revolut / complimentary £0), redacted provider order reference (last 6 chars), and per-line SKU, title snapshot, pack, quantity, unit and line price.
2. **Fulfilment audit (10-04).** The fulfilment `PATCH` (~107-215) is not audited. Audit every status change with from/to status, actor, optional note (max 500). Add the optional `note` to the request. Keep the existing allowed-transition rules.
3. **Complimentary Counter only (10-06, 10-09).** `ComplimentaryStarterShopOrderService.cs` ~177-195 adds one £0 line per SKU from `QueryPublishedStarterSkus`. The spec says each newly verified production Location receives exactly ONE £0 QR Counter Display order line (pack of 1). Change the complimentary order to add only the QR Counter Display SKU (identify it by its placement recipe key; read `ShopMaterialsCatalogSeed.cs` for the stable key). If no Counter Display SKU is published and available, create no complimentary order and log a warning. Do not change Capture QR mint behaviour (`StarterQrMaterialTypes.LoadAsync`). Add a test that proves a second starter SKU being published does not add a second line. In the lane report, describe the likely cause of the 26 Oct legacy-lines order and a SQL query that finds complimentary orders with more than one line (do not run it against any real DB).
4. **Print title from snapshot (09-08).** Print paths read the live catalogue title (`PrintReadyQrMaterialsService.cs` ~1559-1600). Use `ShopOrderLine.TitleSnapshot` for settled orders.
5. **Print retry audit and JobId (11-07, 11-02).** Audit print asset retry (~575-636) like approve (~812). Show the derived JobId in the admin print preview.
6. Tests: service tests for each change.

### Task 4: Lane 4 — Register interest records

Requirement IDs: ADM-12-01, ADM-12-02, ADM-12-03, ADM-12-04, ADM-12-05.

Files: `Models/ShopInterest.cs`, `Shop/.../ShopInterestCatalogSeed.cs`, `Services/ShopInterestService.cs`, `Services/AdminShopInterestProductService.cs`, `Controllers/ShopInterestController.cs`, `Controllers/AdminShopInterestController.cs`, `Data/ApplicationDbContext.cs` (~2914-2918), `src/components/dashboard/admin/AdminShopInterestPanel.tsx`, `src/api/adminShopInterestApi.ts`.

1. **Exact names (12-01).** The seven concepts must read exactly: Receipt Label; Delivery Bag Seal; Standard Packaging Label; Drink Cup Seal; Pizza Box Label; Customer Club Poster; Tray Liner. Fix the seed (`ShopInterestCatalogSeed.cs` ~21-51) and make sure existing DB rows get the corrected titles (seed upsert or data step in the migration). Keep product keys stable.
2. **Soft withdrawal (12-03).** Withdrawal currently hard-deletes (`ShopInterestService.cs` ~142-171). Add `WithdrawnAtUtc` and `WithdrawnByUserId` to registrations; withdrawal sets them. Registering again after withdrawal creates a new active row.
3. **Dedupe (12-02).** Filtered unique index on active rows (product + restaurant + location + user, where `WithdrawnAtUtc IS NULL`). Handle the unique-violation race by returning the existing active row (success, not 500).
4. **Counts (12-04).** All counts (customer and admin) count active rows only, labelled "Active registrations". Zero-count concepts still appear.
5. **Admin detail (12-05).** Admin list shows state (Active / Withdrawn), Workspace, Location, user, registered date, withdrawn date. Mask the user email by default (e.g. `ja***@domain.com`). Filter: Active only (default) / All.
6. **Marketing boundary (12-07)** must stay true: no consent, campaign or quote side effects.
7. One migration. Tests for withdraw, re-register, duplicate, counts, masking helper.

### Task 5: Lane 5 — Pilot lifecycle and extensions

Requirement IDs: ADM-06-01, ADM-06-02, ADM-06-05, ADM-06-06.

Files: `Models/BillingAccount.cs` (~147-171), `Services/BillingAccountLifecycleService.cs` (~300-400), `Services/AdminService.cs` (~500-631), `Controllers/AdminController.cs` (~203-296), `DTOs/Admin/ExtendActivationDto.cs`, `DTOs/Admin/AdminPilotAccountDto.cs`, `Services/PilotStartService.cs`, `src/components/dashboard/admin/AdminPilotsPanel.tsx`, `src/lib/admin/adminPilotExtensionPresentation.ts`, `src/api/adminApi.ts`.

1. **Immutable original term (06-01).** Add `PilotOriginalEndUtc` (nullable). Set it once when the Pilot starts (`PilotStartService.cs`), never change it later. Migration backfills existing Pilot rows: `PilotPeriodEnd - PilotExtensionDaysTotal days` where `PilotPeriodEnd` is not null. Extensions keep changing only `PilotPeriodEnd` (the effective expiry).
2. **Reason required (06-05).** `ExtendActivationDto` gets required `Reason` (trimmed, 5-500 chars). Store it in the audit detail. Reason taxonomy is TO CONFIRM (O04): free text for now.
3. **Stale-version check (06-06).** Request carries `expectedExtensionCount`. If it differs from the stored count, refuse with 409 `stale_pilot_version` (unless it is an idempotent replay of the same key).
4. **Audit refusals and replays (06-06).** Every refused extension (wrong state, limit, stale, invalid days, converted) writes an audit event with `Succeeded = false` and the refusal code. Idempotent replay writes nothing new.
5. **Admin Pilot view (06-01, 06-02, 06-05).** `AdminPilotAccountDto` adds: original start, original end, effective end, days used, extension count and days, extension history (from audit events: date, days, actor, reason), conversion state (converted to paid plan + date if known), and Pilot allowance balances (AI / Email / SMS: granted once, used, remaining) from the existing credit ledger services. Panel shows these. The extend dialog shows a preview before confirm: current range, days used, prior extensions, resulting new end date (computed with the same rule as the server: from current end if in the future, else from now), and the reason field. Server stays the authority.
6. Keep existing rules: 14 or 30 days, max 2, cap. Post-expiry policy is TO CONFIRM: keep current behaviour.
7. Tests: lifecycle service tests (original end immutable, stale, refusal audit, reason validation), endpoint test, vitest for preview computation.

### Task 6: Lane 6 — Offer capacity and credit corrections

Requirement IDs: ADM-04-03, ADM-07-07 (partial), ADM-18-03.

1. **+3 active Offers per paid Group extra Location (04-03).** Spec §6 (spec.txt lines 654): one Group Location ordinal 6-500 adds +2 seats, +10 QR, +3 Offers. Seats (`TeamMemberCapGate.cs` ~17) and QR (`Billing/PlanEntitlements/WorkspaceQrPool.cs` ~17) already add per extra Location. Offers do not: `Billing/ActiveOfferCapGate.cs` ~96-101 and `Services/PlanEntitlementsSnapshotService.cs` ~112 use the plan base only. Add +3 per paid extra Group Location, using the same extra-location count source as seats/QR. Use one shared constant. Check the custom plan quote path (`Services/CustomPlanQuoteService.cs` ~137) and the entitlements snapshot so customer-facing caps match the gate. Tests at 5, 6 and 7 Locations.
2. **Credit adjustment idempotency and evidence (07-07, 18-03).** `POST /api/admin/credit-adjustments` and `/credit-adjustments/reversals` (`AdminController.cs` ~540-670, `Services/CreditLedgerService.cs` ~269, ~584-700, `DTOs/Admin/StaffManualCreditAdjustmentDtos.cs`): require an `Idempotency-Key` header (same pattern as payment refunds). A replay with the same key returns the first result and writes no new ledger entry. A different body with the same key returns 409. Make the evidence reference required (non-empty, max 500). Audit detail includes channel, delta, balance before and after, reason, evidence.
3. Do NOT add a plan-policy block for Pilot grants (TO CONFIRM in spec). Do NOT build a UI for credit adjustments.
4. Tests for both items.

### Task 7: Lane 7 — Customers directory and account profile (read-only)

Requirement IDs: ADM-02-01, ADM-02-02, ADM-02-03, ADM-02-05, ADM-03-01, ADM-03-03, ADM-05-01, ADM-05-03, ADM-05-04, ADM-05-07, ADM-07-01, ADM-08-02 (view).

Read-only. No write actions in this lane.

1. **Backend** — new `AdminAccountsController` at `api/admin/accounts`, `[Authorize(Roles = "Admin")]`, service `AdminAccountDirectoryService`.
   - `GET /api/admin/accounts?search=&plan=&billingStatus=&createdFrom=&createdTo=&skip=0&take=50` (take max 100). Search matches Workspace (restaurant) ID exactly or name/group name contains. Row: restaurant id, display name, plan, billing status, created date, Pilot end or current period end, active Location count, owner display name. Returns total count.
   - `GET /api/admin/accounts/{restaurantId}` profile:
     - Identity: legal/display name, owner name and email, created date, billing account id.
     - Lifecycle: plan, term (monthly/annual), billing status, Pilot start/end, current period start/end, scheduled change (target plan/term + effective date) and cancel-at, soft-lock/dormant dates. Use the stored server fields; do not compute cosmetic states.
     - Locations: id, name, lifecycle status, created/activated date, assigned manager count. Counts: included, paid extra, active, total.
     - Capacity: seats used/cap (active members + pending invites), QR active/cap, Offers active/cap — from the same services the operator Billing page uses (`PlanEntitlementsSnapshotService`, gates). Pending invites list (masked email, sent, expires).
     - Credits: per channel (AI, Email, SMS): available, reserved, included vs prepaid, nearest expiry — from existing credit ledger services.
     - Payments: last 20 Revolut order intents for the restaurant (id, kind, amount pence, currency, state, created, redacted provider reference).
     - Invoices / credit notes: list from `TummlyVatInvoice` (number, type, date, total) — metadata only.
     - Recent audit: last 20 `AdminAuditEvent` rows for this restaurant.
   - Never expose guest data. Use `AsNoTracking`.
2. **Frontend** — new admin tab "Customers" in `src/components/dashboard/admin/Dashboard.tsx`: search + filters + paged table; row opens a profile drawer with sections above. API client `src/api/adminAccountsApi.ts`. Statuses use `Badge`.
3. Tests: service tests (search, paging, isolation: profile for restaurant A never contains B's rows), endpoint test for 401/403/404, vitest for presentation helpers.

### Task 8: Lane 8 — Overview, environment and audit log

Requirement IDs: ADM-01-01..05, ADM-13-01, ADM-18-01, ADM-18-04, ADM-18-06.

1. **Audit log tab (18-01).** UI for existing `GET /api/admin/audit-events` (`AdminController.cs` ~753, filters action, restaurantId, from, to, skip, take). Table: time, action, actor, target, restaurant, succeeded; row expands to pretty `DetailJson`. Add `CorrelationId` (string, max 64) to `AdminAuditEvent`, filled by `AdminAuditService` from `HttpContext.TraceIdentifier` when a request exists (inject `IHttpContextAccessor`). Add `targetId` filter. One migration.
2. **Environment endpoint (18-06, 13-01, 18-04).** `GET /api/admin/environment`: environment name, commit SHA from config `Build:CommitSha` (null = "unknown"), API start time, latest applied EF migration id, provider readiness without secrets: Revolut configured + mode (sandbox/live, reuse logic from `/health/revolut` in `Program.cs` ~1543), Resend configured + sender address domain, Twilio configured + from number masked, VAT mode active. A provider with missing config shows "not configured", never "healthy".
3. **Environment banner (01-05).** Staff shell (`src/layouts/StaffDashboardLayout.tsx`) shows a visible banner on non-production (QA, development) with the environment name from the endpoint; `src/lib/env.ts` has `VITE_APP_ENV`.
4. **Overview tab (01-01..04).** New first tab "Overview" (make it the default). Backend `GET /api/admin/overview` returns each count with its own `available` flag (a failed query returns `available: false`, never 0) and `generatedAtUtc`: Workspaces by billing status; shop orders needing action (paid, not dispatched; and orders flagged for review); print assets Failed; Pilots ending in next 7 days; open Help Centre queries; failed admin audit events in last 7 days. UI cards show count, "as of" time (Europe/London), environment, and a link that opens the matching tab with the filter applied where the tab supports it. No guest data.
5. Tests: service tests for overview availability flag and correlation id; vitest for presentation.

### Task 9: Lane 9 — Trust indicators: model, draft, publish

Requirement IDs: ADM-15-01..08. Spec lines 502-533 and §8 seed list (lines 700-749).

1. **Model.** `TrustIndicator` (ItemNumber 1-15 unique, immutable; Title; EvidenceClass enum: `VerifiedPublicRecord`, `VerifiedSelfAssessment`, `Roadmap`, `OwnControlQaPending`) with draft fields: icon key, image URL, alt text, tooltip, CTA type (`None`, `ExternalProof`, `InternalPage`), CTA target, enabled flag, evidence URL, issuer, issued date, expiry date, review date, scope, last edited by/at. `TrustIndicatorPublication`: version (int, increasing), published at/by, approved-by, reason, JSON snapshot of all 15. Seed the 15 exact titles and evidence classes from spec §8 in canonical order, enabled = false.
2. **Workflow (15-02, 15-03).** Admin endpoints under `api/admin/trust-indicators`: list draft + latest published; edit draft (audit with old/new values; never changes published output); submit for review (records submitter); publish = approve + publish in one atomic transaction creating a new version. **Four-eyes rule:** the publisher must not be the last draft editor or the submitter (409 `self_approval_denied`). Failed publish leaves the prior version unchanged. Editor/publisher roles are TO CONFIRM (O05/O08): the four-eyes rule stands in for now.
3. **Evidence gating (15-05, 15-08).** Publish refuses (400) when an enabled indicator with CTA `ExternalProof` has no evidence URL, or is `Roadmap` / `OwnControlQaPending` with a CTA or "verified" wording, or is past expiry. Public read excludes expired items (neutral state).
4. **Public read (15-04, 15-06).** `GET /api/public/trust-indicators` returns the latest published, enabled, non-expired items only, in item order. Do NOT change the public website rendering and do NOT flip `PUBLIC_TRUST_CLAIMS_APPROVED` (`src/content/marketing/publicTrustClaims.ts`). Add only an API client function for later use.
5. **Admin UI.** New tab "Trust": table of 15 (item, title, class, enabled, draft vs published difference marker), edit drawer, submit, publish dialog with reason and a draft-vs-published diff.
6. **Audit (15-07)** every edit, submit, publish, refusal.
7. One migration. Tests: seed order/titles, four-eyes, gating, public read excludes draft/disabled/expired, failed publish keeps prior version.

# WS3a One server quote, price bands, 1–500 self-service Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every price on the website, signup and checkout comes from the server catalogue or the server quote; extra Locations use the five doc 08 bands; 1–500 Locations are self-service (31–500 through the Custom Plan) with no silent clamp.

**Architecture:** Backend first. One pure helper (`LocationBandPricing`) owns band limits and slot maths; the quote service and the catalogue mapper call it. The standard Group add-Location flow stays at band 1 (≤ 30 Locations). Band rates are catalogue unit-rate rows (data, no migration); a startup upgrade publishes one new catalogue version with the provisional rates. The frontend loses all price arithmetic: one reducer + hook (`useServerCustomPlanQuote`) drives the Custom Plan calculator, the signup review and the For Groups calculator; one hook (`usePublicPlanPrices`) drives plan cards. Signup submit carries a quote hash; the server rejects a stale one with `quote_changed`.

**Tech Stack:** .NET 10 / EF Core (xUnit, InMemory, `TummlyWebApplicationFactory`), React + Vite + TS (vitest, node env, `*.test.ts` only).

**Spec:** `.scratch/ws3a-server-quote/PRD.md` (rules R1–R4, acceptance A1–A8). Copy source: Mohamed doc 08 R3 (`/home/salman/Downloads/TUMMLY_MOHAMED_FINAL_EXECUTION_R3/02_WEBSITE_PAGE_SPECS/08_PRICING_AND_CUSTOM_PLAN_R3.docx`), FAQ doc 10 R3 N03.13, For Groups doc 06 R3 N08.02. Audit: `.scratch/eng-completion-oct26/audit-2026-10-26.md` § ENG02.

## Global Constraints

- Branch `ws3a-server-quote` from local `qa` (7281ef72 or later), in worktree `.worktrees/ws3a-server-quote` (`git worktree add .worktrees/ws3a-server-quote -b ws3a-server-quote qa`). PR to `qa`.
- **No EF migration and no model-snapshot change.** WS3b holds the migration slot. Band rates are `CommercialCustomUnitRateRow` data only.
- Task 0 records the baseline failing lists on the branch base. Full suites at the end: **no new failures** against those lists. Base tests that this WS repairs or deletes leave the list; record each in the PR.
- Band rates are **PROVISIONAL** (Director / Mohamed table pending). They live in one place in code (`LocationBandPricing.ProvisionalRates`) and in catalogue data; a later change is data only.
- Band limits (code constants, doc 08 N10.25): slots 6–30, 31–50, 51–100, 101–250, 251–500. Slot n = the n-th Location; slots 1–5 are in the Group base.
- Rate keys: band 1 = existing unit rate `location` (quote) and existing `ExtraLocations` row (add-Location flow); bands 2–5 = `extra_location_band_2` … `extra_location_band_5` (`UnitQuantity` 1). Provisional pence: monthly 3700 / 3500 / 3300 / 3100; annual 37740 / 35700 / 33660 / 31620.
- Error codes (exact): `invalid_location_count`, `custom_plan_contact_sales` (existing), `invalid_pack_quantity`, `sms_pack_requires_approval`, `catalog_incomplete`, `quote_changed`.
- Exact doc 08 copy (no edits): loading `Updating your quote…`; failed `We could not load a current quote. Your selections are saved. Try again.` + button `Try again`; changed `Your quote has changed. Review the updated total before continuing.`; restore failure `We could not restore your selected plan. Review your choices before continuing.`; no selection `Choose a Pilot or paid plan to continue.`; N06 `Enter a whole number of Locations.`, `Maximum online plan size is 500 Locations`, `Reduce the count to continue online, or contact us about your requirements.`, `Contact us` → `/help-center/contact?topic=multi-location`, `Add at least 1 Location to customise your estimate.`; N09.09 `No payment is taken until you confirm your subscription and payment details at checkout.`
- Temporary tax text until Director D7, only when `vatRateBps > 0`: `Prices exclude VAT. VAT is added at checkout.` No other VAT wording in `src`.
- Do not touch Pilot-trigger copy (`faqsPage.ts` `when-pilot-starts`, `pricingPage.ts:607`): WS3b owns it. Expect small merge conflicts in `faqsPage.ts` / `pricingPage.ts`; keep both sides.
- UI: reuse `src/components/ui/*` (`Button`, `Skeleton`, `Spinner`, `Alert`) and existing marketing components; no one-off controls. Typography per `docs/agents/typography.md` (marketing: headline Plus Jakarta Sans, functional Helvetica Neue). No new Figma: keep current layouts.
- Do not run `npx prettier`. Frontend style: no semicolons, double quotes.
- Commits: one per task, message in the task. Co-author line per session attribution.

## Review Focus

1. **Signup chooses Group with 31+ Locations.** Expected: saved as a `custom` subscription with the banded period amount, never a Group subscription with extras above 30 (Revolut would renew those at £39). Test: Task 4 `SaveOnboarding_Group31Locations_SavedAsCustomWithBandedAmount`.
2. **Annual totals across several bands.** Per-slot annual pence summed per band must give exactly 16691800 for 500; no per-line rounding drift. Test: Task 2 `Quote_Annual500_Exact`.
3. **Fractional or negative numbers in JSON** (`"locations": 2.5`, `"packs": {"offers_5": -1}`). Expected: 400 with our code, not an ASP.NET ProblemDetails. Test: Task 2 `QuoteEndpoint_FractionalLocations_ReturnsInvalidLocationCount`.
4. **Fast typing in the Location input.** An older quote response that arrives after a newer one must not replace it. Test: Task 7 `quoteReducer_IgnoresStaleResponse`.
5. **Signup intent saved before deploy (no quote hash).** Expected: onboarding re-quotes and saves; no `quote_changed` loop. Test: Task 5 `SaveOnboarding_NoQuoteHash_RequotesWithoutReject`.

---

### Task 0: Worktree and baseline

**Files:**
- Create: `.scratch/ws3a-server-quote/baseline-failing-backend-<sha>.txt`, `.scratch/ws3a-server-quote/baseline-failing-frontend-<sha>.txt` (main tree, untracked)

- [ ] **Step 1:** `git worktree add .worktrees/ws3a-server-quote -b ws3a-server-quote qa`; `cd .worktrees/ws3a-server-quote && npm ci && (cd emails && npm ci)`.
- [ ] **Step 2:** Backend: `cd backend && dotnet test TummlyBackend.Tests 2>&1 | grep -E "^\s+Failed " | awk '{print $2}' | sort -u > <main>/.scratch/ws3a-server-quote/baseline-failing-backend-$(git rev-parse --short HEAD).txt`. Expected: about 118 names (WS2a list).
- [ ] **Step 3:** Frontend: `npx vitest run 2>&1 | grep -E "^ (FAIL|×)" | sort -u > <main>/.scratch/ws3a-server-quote/baseline-failing-frontend-$(git rev-parse --short HEAD).txt`. Expected: `assistantWaitGerunds` only.
- [ ] **Step 4:** No commit (files are outside the branch).

---

### Task 1: Band helper and seeded band rates (R1.1–R1.3, R1.5)

**Files:**
- Create: `backend/TummlyBackend/Billing/CustomPlan/LocationBandPricing.cs`
- Modify: `backend/TummlyBackend/Models/CommercialCatalog.cs:15-41` (add band keys to `CommercialCustomUnitRateKeys`, keep `All` order, append bands)
- Modify: `backend/TummlyBackend/Billing/Pricebook/CommercialCatalogSeeder.cs:86-88, 137` (keep Group `SelfServeMaxLocations = 30` — standard Group add-Location cap, Task 4; add band 2–5 rows after `location`. Verify the quote does not use `SelfServeMaxLocations` to reject 31–500; the quote max is `CustomPlanConfig.MaxLocations` = 500)
- Test: `backend/TummlyBackend.Tests/Helpers/LocationBandPricingTests.cs`

**Interfaces:**
- Produces:
  - `CommercialCustomUnitRateKeys.ExtraLocationBand2..5` = `"extra_location_band_2"` … `"extra_location_band_5"`.
  - `public sealed record LocationBand(int Index, int FirstSlot, int LastSlot, string RateKey)`; `LocationBandPricing.Bands` (5 rows; band 1 `RateKey = CommercialCustomUnitRateKeys.Location`).
  - `public sealed record LocationBandRate(int Index, int MonthlyNetPence, int? AnnualNetPence)`.
  - `LocationBandPricing.ProvisionalRates` (bands 2–5, values in Global Constraints).
  - `public static IReadOnlyList<LocationBandSlice> Slice(int firstSlot, int lastSlot)` → `LocationBandSlice(int BandIndex, int SlotCount, int FirstSlot, int LastSlot)`; empty when `lastSlot < firstSlot`; slots < 6 ignored.
  - `public static bool TryPrice(int firstSlot, int lastSlot, string cadence, IReadOnlyDictionary<int, LocationBandRate> ratesByBand, out int netPence, out int? missingBand)` — sums `CustomPlanLineNet.ForCadence(cadence, slice.SlotCount, rate.Monthly, rate.Annual, 1)` per slice; returns false with the first missing band index.
  - `public static int BandIndexForSlot(int slot)` (6–30 → 1 … 251–500 → 5; throws `ArgumentOutOfRangeException` outside 6–500).

- [ ] **Step 1: Write the failing tests**
  - `Slice_6To500_FiveBands`: counts 25, 20, 50, 150, 250.
  - `Slice_29To32_SplitsBand1And2`: `[(1, 2, 29, 30), (2, 2, 31, 32)]`.
  - `TryPrice_Monthly_6To500` with band 1 = (3900, 39800) + provisional → `netPence == 1616500` (16364 − 199 = £16,165).
  - `TryPrice_Annual_6To500` → `netPence == 16488800` (annual 500 total 16691800 − base 203000).
  - `TryPrice_MissingBand3_ReturnsFalse` for 6–60 → `missingBand == 3`; for 6–50 → true.
  - `BandIndexForSlot` theory: 6→1, 30→1, 31→2, 50→2, 51→3, 100→3, 101→4, 250→4, 251→5, 500→5; 5 and 501 throw.
  - `Seeder_FromSnapshot_AddsBandRates`: `FromSnapshot(pack)` has rate rows `extra_location_band_2..5` with the provisional pence and Group `SelfServeMaxLocations == 30` (unchanged).
- [ ] **Step 2:** `cd backend && dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~LocationBandPricingTests"` → FAIL (type missing).
- [ ] **Step 3:** Implement the helper, keys and seeder rows. `ProvisionalRates` is the only place the numbers appear in backend code (seeder reads it).
- [ ] **Step 4:** Same command → PASS.
- [ ] **Step 5:** `git commit -m "feat: Location price bands helper and provisional band rates"`

---

### Task 2: Quote service — bands, validation, no clamp, SMS 5000, quote hash (R1.4–R1.7, R2.1, R2.2, R2.4, R3.3)

**Files:**
- Modify: `backend/TummlyBackend/DTOs/Admin/CustomPlanQuoteDtos.cs:6-80, 118-125` (request `decimal Locations`, `Dictionary<string, decimal>? Packs`; result `QuoteHash`, `VatRateBps`; pack meta `MonthlyNetPence`, `AnnualNetPence` (int?))
- Modify: `backend/TummlyBackend/Services/CustomPlanQuoteService.cs:21-305` (validation before any maths; remove `Math.Max(1, …)` at :73 and `Math.Clamp` at :301; replace the single `extra:location` block :160-192 with band lines)
- Modify: `backend/TummlyBackend/Billing/CustomPlan/CustomPlanConfig.cs` (add `[JsonPropertyName("quoteHash")] string? QuoteHash`; doc comment for packs "0..MaxPackCount, no clamp")
- Create: `backend/TummlyBackend/Billing/CustomPlan/CustomPlanQuoteHash.cs`
- Modify: `backend/TummlyBackend/Services/AdminCommercialCatalogService.cs:285-293` (pack meta prices via `CustomPlanLineNet.Monthly/Annual(packSize, rate)`; null when the rate row is missing)
- Modify: `backend/TummlyBackend/Services/SignupService.cs:789-799` (map `decimal` → `int` after validation; see Task 5 for the hash check)
- Test: `backend/TummlyBackend.Tests/Services/CustomPlanQuoteServiceTests.cs`, `backend/TummlyBackend.Tests/Integration/PublicCommercialCatalogEndpointsTests.cs` (create; use `TummlyWebApplicationFactory`, as in `OffersCatalogEndpointsTests.cs`)

**Interfaces:**
- Consumes: `LocationBandPricing.Slice/TryPrice` (Task 1).
- Produces:
  - Quote lines: base line unchanged; one line per band slice, `Key = "extra:location:band_{n}"`, `Label = "{count} additional Location{s} ({first}–{last})"`, `Quantity = count`, `NetPence` = period net.
  - `CustomPlanQuoteResult.QuoteHash` (string, 32 lower-hex) = `CustomPlanQuoteHash.Compute(result)`: SHA-256 of `"{CatalogPublicId}|{Cadence}|{BasePlan}|{Locations}|{k=v;… sorted by key, counts > 0}|{PeriodNetPence}"`, first 32 hex chars.
  - `CustomPlanQuoteResult.VatRateBps` = published `version.VatRateBps`.
  - Validation (in this order, all `CustomPlanQuoteException`): `Locations` not whole or `< 1` → `invalid_location_count` ("Enter a whole number of Locations."); `> 500` → `custom_plan_contact_sales`; unknown pack → `invalid_pack` (existing); pack not whole, `< 0` or `> CustomPlanPacks.MaxPackCount` → `invalid_pack_quantity`; `sms_5000 > 0` and base plan ≠ `group` → `sms_pack_requires_approval`; a slice needs a missing band rate → `catalog_incomplete`.

- [ ] **Step 1: Write the failing tests** (seed = `CommercialCatalogSeeder.FromSnapshot`, which now has bands)
  - `Quote_MonthlyFixtures` theory (Locations → `MonthlyNetPence`): 5→19900, 6→23800, 30→117400, 31→121100, 50→191400, 51→194900, 100→366400, 101→369700, 250→861400, 251→864500, 500→1636400. Assert `PeriodNetPence` equal for monthly.
  - `Quote_Annual500_Exact`: annual, 500 → `PeriodNetPence == 16691800`.
  - `Quote_31Locations_HasTwoBandLines`: keys `extra:location:band_1` (qty 25, 97500) and `extra:location:band_2` (qty 1, 3700).
  - `Quote_InvalidLocations` theory 0, -1, 2.5 → code `invalid_location_count`; 501 → `custom_plan_contact_sales`; `result.Locations` never differs from the request for valid input.
  - `Quote_InvalidPackQuantity` theory -1, 1.5, 100 → `invalid_pack_quantity`.
  - `Quote_Sms5000_GroupOk_GrowthRejected`: 5 Locations + `sms_5000: 1` OK; 2 Locations + `sms_5000: 1` → `sms_pack_requires_approval`.
  - `Quote_MissingBand3_Over50Fails_Under50Ok`: delete `extra_location_band_3` row; 50 OK, 51 → `catalog_incomplete`.
  - `Quote_HashStable_AndChangesWithPrice`: same request twice → same hash; change band-2 monthly to 3600 → different hash.
  - Integration `QuoteEndpoint_FractionalLocations_ReturnsInvalidLocationCount`: POST `/api/public/commercial-catalog/custom-plan/quote` body `{"cadence":"monthly","locations":2.5,"packs":{}}` → 400, `code == "invalid_location_count"`.
  - Integration `CustomUnitRates_PacksCarryServerPrices`: GET `/api/public/commercial-catalog/custom-unit-rates` → pack `team_users_5` has `monthlyNetPence == 1500`.
  - Update existing `Quote_SixLocationsAddsExtraLocationLine` to the new line key.
- [ ] **Step 2:** `dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~CustomPlanQuoteServiceTests|FullyQualifiedName~PublicCommercialCatalogEndpointsTests"` → FAIL.
- [ ] **Step 3:** Implement. Base plan selection (`MinBasePlanForLocations`) unchanged. Credits and users added per extra Location stay `additionalLocations × ExtraLocations row` (R1.6).
- [ ] **Step 4:** Same command → PASS. Also run `--filter "FullyQualifiedName~Signup"` → no new failures.
- [ ] **Step 5:** `git commit -m "feat: server quote uses Location bands, rejects invalid counts, adds quote hash"`

---

### Task 3: Publish band rates to existing databases (R1.3)

**Decision:** startup upgrade, not a manual Admin action. QA (and later production) gets the same published version without a person; Admin can still edit band rows afterwards in the existing catalogue panel (it lists unit rates generically, `AdminCommercialCatalogPanel.tsx:346`).

**Files:**
- Create: `backend/TummlyBackend/Billing/Pricebook/CommercialCatalogBandRatesUpgrade.cs`
- Modify: `backend/TummlyBackend/Program.cs:1575-1578` (call after `CommercialCatalogSeeder.EnsureSeededAsync`)
- Test: `backend/TummlyBackend.Tests/Services/CommercialCatalogBandRatesUpgradeTests.cs`

**Interfaces:**
- Produces: `public static Task<bool> EnsureAsync(ApplicationDbContext db, DateTime nowUtc, ILogger logger, CancellationToken ct = default)` — returns true when it published.
  - No-op when the latest Published version already has all four band keys, or when a version with `PublicId == "TUMMLY-UK-GBP-BANDS-20261026"` exists.
  - Else: `CommercialCatalogMapper.CloneAsDraft(published, nowUtc)`, add missing band rows from `LocationBandPricing.ProvisionalRates`, keep Group `SelfServeMaxLocations` unchanged, `PublicId = "TUMMLY-UK-GBP-BANDS-20261026"`, `Notes = "Location bands (provisional, doc 08 R3)"`, Status Published, previous Published → Superseded, one `SaveChanges`. Existing Draft rows untouched. Log Information with old and new PublicId.

- [ ] **Step 1: Write the failing tests**
  - `EnsureAsync_PublishedWithoutBands_PublishesNewVersion`: seed a version without band rows; after call: exactly one Published (new id), old one Superseded, 4 band rows, Group `SelfServeMaxLocations` unchanged (30), other plan / pack / rate rows equal to the old ones.
  - `EnsureAsync_Idempotent`: second call returns false, version count unchanged.
  - `EnsureAsync_KeepsAdminEditedLocationRate`: old `location` monthly 4100 → new version `location` monthly 4100.
  - `EnsureAsync_LeavesDraftAlone`: an existing Draft keeps its id and rows.
- [ ] **Step 2:** `dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~CommercialCatalogBandRatesUpgradeTests"` → FAIL.
- [ ] **Step 3:** Implement and wire in `Program.cs` inside the existing best-effort `try`.
- [ ] **Step 4:** Same command → PASS.
- [ ] **Step 5:** `git commit -m "feat: startup publishes provisional Location band rates once"`

---

### Task 4: 31–500 Locations through Custom Plan; standard Group add-Location stays at 30 (R1.8, R2.3)

Decision 26 Oct: a standard Group subscription renews extra Locations through one fixed Revolut variation (`RevolutPlanVariationKeys.ForExtraLocation`, £39 / £398 × quantity), so it cannot carry bands. Slots 6–30 are all band 1, so the fixed variation is exact up to 30. 31–500 Locations use the Custom Plan, whose renewal charges the snapshotted banded `PeriodNetPence` (`CustomPlanCycleBillingService`, `CustomPlanCycleRenewalJob`).

**Files:**
- Keep: `backend/TummlyBackend/Billing/PlanEntitlements/LocationCap.cs` (`GroupSelfServeMax = 30`, `CustomMax = 500` unchanged). Add an XML comment that names the reason (fixed Revolut variation).
- Keep: `backend/TummlyBackend/Services/ExtraGroupLocationService.cs` pricing (band 1 = `ExtraLocations` row). No band logic here.
- Modify: `backend/TummlyBackend/Services/SignupService.cs` (~`:772-841` onboarding save, `ResolveChosenPlan` `:849-866`): when the saved custom config / quote has Locations > `LocationCap.GroupSelfServeMax`, the chosen plan must be `custom`; a request with plan `group` and Locations > 30 → save as `custom` with the server-quoted banded `PeriodNetPence` (never a Group subscription with extras above 30).
- Modify: the frontend add-Location cap state (`src/api/locationsWriteApi.ts:25` handles `location_cap_reached`; find the Manage Plan / Settings > Locations "add Location" UI that shows it): for a Group account at 30, show `Need more than 30 Locations? Build your own plan.` with a link to `/custom-plan`. Reuse `Button` (`variant="link"`) / existing alert; no new component.
- Test: `backend/TummlyBackend.Tests/Services/SignupServiceCustomPlanTests.cs` (create or extend the existing signup onboarding tests), `backend/TummlyBackend.Tests/Helpers/LocationCapTests.cs` (create), frontend presentation test next to the cap message helper.

**Interfaces:**
- Consumes: `CustomPlanQuoteService` banded quote (Task 2).
- Produces: `custom` subscription for every signup with 31–500 Locations; `location_cap_reached` stays the standard Group signal at 30.

- [ ] **Step 1: Write the failing tests**
  - `LocationCap_Group_StaysAt30`: Group, extras 25 → 30; extras 40 → 30. `LocationCap_Custom_500`: custom 500 → 500; 600 → 500.
  - `SaveOnboarding_Group31Locations_SavedAsCustomWithBandedAmount`: plan `group`, 31 Locations, monthly → subscription plan `custom`, `PeriodNetPence` = 121100.
  - `SaveOnboarding_Custom500Annual_PeriodNetPence`: 500 Locations annual → 16691800.
  - `SaveOnboarding_Group30Locations_StaysGroup`: 30 Locations → `group` (fixed variation path unchanged).
  - Frontend: cap message for Group at 30 returns the exact copy and `/custom-plan` href; other plans unchanged.
- [ ] **Step 2:** `dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~LocationCapTests|FullyQualifiedName~SignupServiceCustomPlan"` → FAIL; `npx vitest run <cap test>` → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Same commands → PASS; `--filter "FullyQualifiedName~ExtraGroupLocation|FullyQualifiedName~PlanEntitlements|FullyQualifiedName~Signup"` → no new failures.
- [ ] **Step 5:** `git commit -m "feat: 31-500 Locations via Custom Plan; Group add-Location stays at 30"`

---

### Task 5: Signup rejects a changed quote (R3.3, A4)

**Files:**
- Modify: `backend/TummlyBackend/Services/SignupService.cs:771-841` (`ApplyChosenPlanAsync`)
- Modify: `backend/TummlyBackend/Controllers/SignupController.cs:94-122` (catch `CustomPlanQuoteException` before `Exception`)
- Test: `backend/TummlyBackend.Tests/Services/SignupServiceTests.cs` (service-level cases) and `backend/TummlyBackend.Tests/Integration/SignupOnboardingCustomPlanTests.cs` (create; only the controller `code` mapping case)

**Interfaces:**
- Consumes: `CustomPlanConfig.QuoteHash`, `CustomPlanQuoteResult.QuoteHash` (Task 2).
- Produces: when the decoded config has a non-empty `QuoteHash` and it differs from the fresh server quote hash → throw `CustomPlanQuoteException("quote_changed", "Your quote has changed. Review the updated total before continuing.")`. Missing hash → current behaviour (re-quote and save). Controller returns `400 { success: false, code, message }` for `CustomPlanQuoteException`; other exceptions unchanged.

- [ ] **Step 1: Write the failing tests**
  - `SaveOnboarding_MatchingQuoteHash_Saves`: get a quote, send its hash in `customPlanConfigJson` → 200, stored config `PeriodNetPence` equals the quote.
  - `SaveOnboarding_StaleQuoteHash_ReturnsQuoteChanged`: quote, then publish a catalogue with band-2 monthly 3600, submit 31 Locations with the old hash → 400 `code == "quote_changed"`; pending signup plan / config unchanged.
  - `SaveOnboarding_NoQuoteHash_RequotesWithoutReject` → 200.
  - `SaveOnboarding_InvalidLocations_ReturnsCode`: config `locations: 0` → 400 `code == "invalid_location_count"`.
- [ ] **Step 2:** `dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~SignupServiceTests.SaveOnboarding|FullyQualifiedName~SignupOnboardingCustomPlanTests"` → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Same command → PASS.
- [ ] **Step 5:** `git commit -m "feat: signup rejects a changed custom plan quote"`

---

### Task 6: Webhook timestamp window (R4.1)

**Files:**
- Create: `backend/TummlyBackend/Services/RevolutWebhookTimestamp.cs`
- Modify: `backend/TummlyBackend/Services/RevolutWebhookReceiver.cs:57-69` (check freshness at receipt, after the signature check, same `BadSignature` result). **Do not** add it to `RevolutWebhookService` (:108-120): the inbox worker processes later and the Testing path uses fixed old timestamps.
- Test: `backend/TummlyBackend.Tests/Services/RevolutWebhookTimestampTests.cs`, `backend/TummlyBackend.Tests/Services/RevolutWebhookReceiverTests.cs` (create; see `RevolutWebhookInboxWorkTests.cs` for receiver setup)

**Interfaces:**
- Produces: `public static bool IsFresh(string? timestamp, DateTimeOffset nowUtc, TimeSpan tolerance)`. Value > 10^11 → milliseconds, else seconds. Non-numeric / empty → false. `|now − ts| ≤ tolerance`. Receiver uses `TimeSpan.FromMinutes(5)` and the injected clock.

- [ ] **Step 1: Write the failing tests**
  - `IsFresh` theory: now; now − 4 min (ms); now − 4 min (s); now + 4 min → true. now − 6 min; now + 6 min; `""`; `"abc"` → false.
  - `Receive_OldTimestamp_ReturnsBadSignature` (non-Testing environment stub, valid signature, timestamp now − 6 min) → `BadSignature`, no inbox row.
- [ ] **Step 2:** `dotnet test TummlyBackend.Tests --filter "FullyQualifiedName~RevolutWebhookTimestamp|FullyQualifiedName~RevolutWebhookReceiverTests"` → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Same filter → PASS; `--filter "FullyQualifiedName~RevolutWebhook"` → no new failures.
- [ ] **Step 5:** `git commit -m "fix: reject Revolut webhooks older than 5 minutes at receipt"`

---

### Task 7: Frontend quote state, API types and copy (R3.4–R3.7)

**Files:**
- Modify: `src/api/adminCommercialCatalogApi.ts:252-470` (`CustomPlanQuoteResult` + `quoteHash: string`, `vatRateBps: number`; `PublicCatalogPackMetaDto` + `monthlyNetPence: number | null`, `annualNetPence: number | null`; `quotePublicCustomPlan` error keeps `code`)
- Create: `src/content/marketing/customPlanQuoteCopy.ts` (all doc 08 strings in Global Constraints + tax text; one exported const object `CUSTOM_PLAN_QUOTE_COPY`)
- Create: `src/lib/marketingCustomPlan/customPlanQuoteState.ts`
- Create: `src/lib/marketingCustomPlan/useServerCustomPlanQuote.ts`
- Test: `src/lib/marketingCustomPlan/customPlanQuoteState.test.ts`

**Interfaces:**
- Produces (`customPlanQuoteState.ts`):
  - `export type LocationInputCheck = "empty" | "not_integer" | "over_limit" | "ok"`; `export function checkLocationInput(value: number | null): LocationInputCheck` (null/NaN → empty; `< 1` → empty; non-integer → not_integer; `> 500` → over_limit).
  - `export type QuoteState = { status: "idle" } | { status: "loading"; requestId: number; last: CustomPlanQuoteResult | null } | { status: "ready"; quote: CustomPlanQuoteResult } | { status: "failed"; last: CustomPlanQuoteResult | null; code: string | null }`.
  - `export type QuoteAction = { type: "request"; requestId: number } | { type: "success"; requestId: number; quote: CustomPlanQuoteResult } | { type: "failure"; requestId: number; code: string | null }`; `export function quoteReducer(state: QuoteState, action: QuoteAction): QuoteState` — success / failure with a `requestId` other than the current loading id are ignored.
  - `export function quoteView(input: { check: LocationInputCheck; state: QuoteState; changed: boolean }): { message: string | null; showRetry: boolean; showContact: boolean; continueEnabled: boolean; taxText: string | null }` — `continueEnabled` only when `check === "ok"` and status `ready`; messages from `CUSTOM_PLAN_QUOTE_COPY`; failure code `custom_plan_contact_sales` → over-limit copy + `showContact`; `invalid_location_count` → not-integer copy; other failures → failed copy + `showRetry`; `changed` → changed copy (Continue enabled once the fresh quote is `ready`: the review step is the renewed review); `taxText` = tax text when `quote.vatRateBps > 0`.
  - `export function formatPenceGbp(pence: number): string` moves here from `customPlanEstimate.ts` (re-export from the old path is not allowed; update imports).
- Produces (`useServerCustomPlanQuote.ts`): `export function useServerCustomPlanQuote(input: { cadence: BillingCadence; basePlan: CustomPlanBasePlanKey | null; locations: number | null; packs: Partial<Record<CustomPlanPackKey, number>> }, options?: { debounceMs?: number }): { state: QuoteState; check: LocationInputCheck; retry: () => void }` — debounce 250 ms, increasing `requestId`, no request when `check !== "ok"`.

- [ ] **Step 1: Write the failing tests** (`customPlanQuoteState.test.ts`)
  - `checkLocationInput`: null → empty, 0 → empty, 2.5 → not_integer, 501 → over_limit, 500 → ok.
  - `quoteReducer_IgnoresStaleResponse`: request 1, request 2, success 1 → still loading id 2; success 2 → ready with quote 2.
  - `quoteView`: loading → `"Updating your quote…"`, Continue false; failed (code null) → failed copy, `showRetry`, Continue false; ready → Continue true; not_integer → `"Enter a whole number of Locations."`; over_limit → `"Maximum online plan size is 500 Locations"`, `showContact`; empty → `"Add at least 1 Location to customise your estimate."`; ready + `vatRateBps 2000` → `taxText == "Prices exclude VAT. VAT is added at checkout."`; `vatRateBps 0` → null.
- [ ] **Step 2:** `npx vitest run src/lib/marketingCustomPlan/customPlanQuoteState.test.ts` → FAIL.
- [ ] **Step 3:** Implement module, hook, copy and API type changes.
- [ ] **Step 4:** Same command → PASS; `npm run typecheck` → no new errors.
- [ ] **Step 5:** `git commit -m "feat: server quote state, hook and doc 08 quote copy"`

---

### Task 8: Calculator and signup use only the server quote; delete browser maths (R3.1, R3.4–R3.7, A6)

**Files:**
- Modify: `src/components/marketing/custom-plan/CustomPlanCalculator.tsx:106-260, 296-345, 555` (state from `useServerCustomPlanQuote`; pack prices from `packs[].monthlyNetPence`; Location messages + Contact us from `quoteView`; Continue disabled unless `continueEnabled`; `CustomPlanCalculatorContinue` carries `quote: CustomPlanQuoteResult` instead of `estimate`)
- Modify: `src/components/marketing/custom-plan/CustomPlanSummaryCard.tsx` (prop `quote: CustomPlanQuoteResult`; render server `lines`; `taxText` prop)
- Modify: `src/components/signup/SignupReviewPlanStep.tsx:100-261` (no fallback; states from `quoteView`; Continue enabled only when ready; new prop `quoteChanged: boolean`; restore-failure / no-selection copy; save `quoteHash` with the config; replace the static VAT note with `taxText` + N09.09)
- Modify: `src/pages/auth/SignupOnboardingPage.tsx:160-175, 327` (on error `code === "quote_changed"` set `quoteChanged` and stay on review)
- Modify: `src/api/signupApi.ts:157-165` (surface `code` from a 400 body on the thrown error)
- Modify: `src/lib/signupPlanIntent.ts:64-90` (no `Math.max/Math.floor` clamp: an invalid count drops the config → restore-failure state; keep `quoteHash`)
- Modify: `src/lib/marketingCustomPlan/customPlanEstimate.ts` (delete `estimateCustomPlanV2`, `lineNetPence`, `packMonthlyNetPence`, `EstimateCustomPlanV2Args`, `CustomPlanV2Estimate`, `ANNUAL_FALLBACK_FACTOR`, the catalogue-not-loaded fallback in `minBasePlanForLocations` (:302-306, return `null` when no plans); `CustomPlanV2ConfigInput` + `quoteHash?: string`; `toOnboardingCustomPlanConfigJson` passes `quoteHash`, no clamp)
- Modify: `src/components/signup/SignupCustomisePlanStep.tsx`, `src/schemas/signupOnboarding.ts:213-220` (follow the type changes)
- Modify: `src/content/marketing/customPlanPage.ts:30-41` (remove price fallbacks `£15/month`, `£12/month`, `£6/month`; `lockHint` = doc 08 empty helper)
- Delete / update: `src/lib/marketingCustomPlan/customPlanEstimate.test.ts` (keep encode/decode and pack tests; delete estimate tests)
- Test: `src/lib/marketingCustomPlan/noBrowserPricing.test.ts`, `src/lib/signupPlanIntent.test.ts`

**Interfaces:**
- Consumes: Task 7 hook, view and copy; Task 2 `quoteHash`, `vatRateBps`, pack prices; Task 5 `quote_changed`.

- [ ] **Step 1: Write the failing tests**
  - `noBrowserPricing.test.ts` (reads files with `node:fs`, recursive over `src/`, skips `*.test.ts`): no match for `/estimateCustomPlanV2|packMonthlyNetPence|ANNUAL_FALLBACK_FACTOR|\* 0\.85/`; no match for `/VAT is added at checkout when applicable/`; no `self-serve limit` / `30-Location`.
  - `signupPlanIntent.test.ts`: config `locations: 0` → normalised intent has no config; config with `quoteHash: "abc"` round-trips through encode / decode.
- [ ] **Step 2:** `npx vitest run src/lib/marketingCustomPlan/noBrowserPricing.test.ts src/lib/signupPlanIntent.test.ts` → FAIL.
- [ ] **Step 3:** Implement. Keep layouts; swap data sources only. Loading uses `Skeleton` or the copy line, not a new spinner component.
- [ ] **Step 4:** Same command → PASS; `npx vitest run src/lib/marketingCustomPlan src/lib/signup* src/schemas` → no new failures; `npm run typecheck` → no new errors; `npm run build` OK.
- [ ] **Step 5:** `git commit -m "feat: Custom Plan and signup review use only the server quote"`

---

### Task 9: Marketing price sources bind to the catalogue (R3.2, R3.8)

**Files:**
- Create: `src/lib/marketingCustomPlan/usePublicPlanPrices.ts`
- Create: `src/lib/marketingCustomPlan/publicPlanPrices.ts` (pure formatter) + `publicPlanPrices.test.ts`
- Modify: `src/content/marketing/pricingPage.ts:74-170` (remove paid `monthlyPrice` / `annualPrice` literals; Pilot stays `£0`), `:488-492` (remove the Group "£39/month each" compare row), `:673` (doc 08 N10.24 answer), add / replace answers N10.30 and N11.08 where the pricing FAQ holds them
- Modify: `src/components/marketing/pricing/PricingHeroPlans.tsx`, `PricingComparePlans.tsx` (prices from the hook; loading / failed copy once above the grid)
- Modify: `src/content/marketing/aiAssistantPage.ts:139-190`, `src/components/marketing/ai-assistant/AiAssistantPlans.tsx` (same; Home reuses it via `homePage.ts:353`)
- Modify: `src/content/marketing/forGroupsPage.ts:280-300` (remove `monthlyPrice`, `annualPrice`, `monthlyExtraLocationPrice`, `annualExtraLocationPrice`, `priceAmount`, `priceSubline`; `maxLocations: 500`; body = doc 06 N08.02)
- Modify: `src/components/marketing/for-groups/ForGroupsPricing.tsx:90-112` (`useServerCustomPlanQuote({ cadence, basePlan: "group", locations, packs: {} })`; total = `quote.periodNetPence`; over 500 → contact copy)
- Modify: `src/content/marketing/faqsPage.ts:139-147` (`how-much-cost`: mark `catalogBinding: "plan-cost"`), `src/components/marketing/faqs/FaqsTopicSection.tsx` (render bound answer)

**Interfaces:**
- Consumes: `fetchPublicCustomUnitRates`, `formatPenceGbp` (Task 7).
- Produces:
  - `export type PublicPlanPrice = { monthly: string; annual: string }`; `export function toPublicPlanPrices(plans: PublicCatalogPlanSummaryDto[]): Partial<Record<"Starter" | "Growth" | "Group", PublicPlanPrice>>` (`£39` / `£398`, no pence when whole pounds).
  - `export function buildPlanCostAnswer(prices): string | null` — doc 10 FAQ.N03.13 shape: `Starter is £39/month or £398/year; Growth is £99/month or £1,010/year; Group is £199/month or £2,030/year before selected extras. Compare the current quote before confirming.`; null when any plan is missing (UI then shows only `Compare the current quote before confirming.`).
  - `export function usePublicPlanPrices(): { status: "loading" | "ready" | "failed"; prices: Partial<Record<…, PublicPlanPrice>> }` (one fetch per page load, module-level promise cache).

- [ ] **Step 1: Write the failing tests** (`publicPlanPrices.test.ts`)
  - seeded DTO plans (3900/39800, 9900/101000, 19900/203000) → `{ Starter: { monthly: "£39", annual: "£398" }, Growth: { monthly: "£99", annual: "£1,010" }, Group: { monthly: "£199", annual: "£2,030" } }`.
  - `buildPlanCostAnswer` → exact sentence above; missing Group → null.
  - Extend `noBrowserPricing.test.ts`: `src/content/marketing/{pricingPage,aiAssistantPage,forGroupsPage}.ts` contain no `£39`, `£99`, `£199`, `£398`, `£1,010`, `£2,030`, `2030`, `398` price literals (Pilot `£0` allowed).
- [ ] **Step 2:** `npx vitest run src/lib/marketingCustomPlan` → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Same command → PASS; `npx vitest run` (full) → no new failures vs Task 0; `npm run typecheck`; `npm run build` OK.
- [ ] **Step 5:** `git commit -m "feat: marketing plan prices and For Groups calculator from the server"`

---

### Task 10: Full suites, PR, QA evidence (A8)

- [ ] **Step 1:** Backend full suite → compare with the Task 0 list; no new names. Frontend full suite → same.
- [ ] **Step 2:** `git push -u origin ws3a-server-quote`; open PR to `qa` with: rules covered, provisional-rate note, startup upgrade note (new PublicId `TUMMLY-UK-GBP-BANDS-20261026`), repaired / removed base tests, QA checklist below. Link the PR in the thread.
- [ ] **Step 3 (after deploy, QA):** run the 02.1 matrix against `https://api.qa.tummly.com/api/public/commercial-catalog/custom-plan/quote` (0, 1, 2, 3, 4, 5, 6, 30, 31, 50, 51, 100, 101, 250, 251, 500, 501, -1, 2.5; monthly and annual) and save request / response pairs to `.scratch/ws3a-server-quote/qa-evidence-<date>.md`; capture loading, failed, changed and over-500 states; capture Pricing, For Groups, AI Assistant and Home prices from the server; confirm `GET /api/admin/commercial-catalog/published` shows the bands version.

---

## Self-review notes

- Spec coverage: R1.1–R1.3 T1/T3; R1.4–R1.7 T2; R1.8 T4; R2.1–R2.2 T2; R2.3 T4; R2.4 T2 (top-up path already enforces Group or `AllowSms5000TopUp` in `BillingCreditsService:2268-2280`; no change, existing tests); R3.1 T8/T9; R3.2 T8/T9; R3.3 T2/T5/T8; R3.4–R3.7 T7/T8; R3.8 T9; R4.1 T6. A1–A8 map to T2, T4, T4, T5, T2, T8/T9, T6, T10.
- PRD R1.2 says band 1 = `ExtraLocations` row. The quote actually prices extra Locations from unit rate `location` (`CustomPlanQuoteService.cs:160-192`); the add-Location flow uses the `ExtraLocations` row. Both stay band 1 in their own flow (both seeded 3900 / 39800). Admin must edit both together.

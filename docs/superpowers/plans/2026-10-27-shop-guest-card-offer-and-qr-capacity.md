# Shop Guest Card Offer and QR Capacity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bind the card-offer requirement to QR Guest Cards only, never open the dialog on Shop load, make QR codes at shop fulfilment, and make Shop checkout obey the active-QR plan limit with safe reservations.

**Architecture:** Server rules first (card-offer rule by QR type, availability on every entry path, QR-path guard). Then one shared capacity service used by Capture and Shop under the existing `BillingAccounts` `UPDLOCK`. Shop `/pay` writes a `ShopQrSlotReservation`, payment completion converts it into an Active QR code, and a sweeper reconciles stale reservations with Revolut before release. The frontend follows: the offer dialog opens only on a Guest Card purchase, and a "QR code limit reached" dialog appears.

**Tech Stack:** .NET 10 / EF Core / SQL Server (`Testcontainers.MsSql` for lock tests), xUnit + `SkippableFact`; React + TypeScript + Vitest, shadcn/ui.

**Spec:** `docs/superpowers/specs/2026-10-27-shop-guest-card-offer-and-qr-capacity-design.md`

## Global Constraints

- Card-offer rule key: `QrType.GuestCard` (recipe QR type). Never a SKU string on the server.
- Frontend constant: `SHOP_GUEST_CARD_SKU_ID = "qr-guest-cards"`.
- Error codes (exact): `offer_card_offer_required` (409), `sku_not_available` (existing), `qr_limit_reached` (409, body `{ code, message, limit }`), `sku_has_no_qr_path` (409), `reorder_no_available_lines` (409).
- Reservation statuses (exact): `open`, `converted`, `released`, `review`. "Held" = `open` or `review`.
- Reconcile deadline: `CreatedAtUtc + 30 min`. Backoff after a non-final result: 5 min, 15 min, 60 min, then 60 min. Move to `review` 24 h after `CreatedAtUtc`.
- Order review reason: `qr_capacity_unavailable_after_payment`. No automatic refund anywhere.
- Limit dialog copy (exact): title `QR code limit reached`; body `Your plan allows N active QR codes at this location. You can use an existing active QR code or upgrade your plan to add another.`; buttons `Back to order`, `Review plan options`, `Manage QR codes`.
- Pause warning copy (exact, when `hasShopPrintedMaterials`): `Printed materials from your Tummly Shop orders use this QR code. Guests who scan them will not reach your form while it is paused.`
- Keep `RestaurantLocation.OfferCardCatalogOfferId`, the `/offer-card-offer` routes, and `CatalogOfferStatus.AttachSourceOfferCard` names unchanged.
- EF: generate migrations with `dotnet ef migrations add`. Never hand-write `*.Designer.cs`. New FKs use `NoAction` or `Restrict` (CODING_STANDARDS.md, error 1785).
- UI: shadcn primitives (`Dialog`, `Button`), Operator design tokens, *functional* typeface for body and buttons (`docs/agents/typography.md`).
- Backend test command: `dotnet test backend/TummlyBackend.Tests --filter "FullyQualifiedName~<Name>"`. Frontend: `npx vitest run <path>`, plus `npm run typecheck` and `npm run lint` before each frontend commit.
- Commit trailer: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. A cart that holds Guest Cards when Admin sets them back to Coming soon → checkout from cart returns `sku_not_available` and the cart line stays visible (test in Task 1).
2. Switch location while the offer dialog is open → the dialog closes and the new location's attach state loads (test in Task 9).
3. One order mixes a reused type (Counter Display) and a new type (Guest Cards) at limit − 1 → pay succeeds and writes exactly one reservation (test in Task 5).
4. Only an **Archived** GuestCard code exists → it is neither reused nor reactivated, and a new code is made (test in Task 6).
5. The plan limit drops (downgrade) between pay and payment completion → the held slot still converts and the order fulfils (test in Task 6).

---

### Task 1: Rebind the card-offer rule to Guest Cards (server)

**Files:**
- Create: `backend/TummlyBackend/Shop/ShopCardOfferRules.cs`
- Modify: `backend/TummlyBackend/Services/ShopCartService.cs:60-80`
- Modify: `backend/TummlyBackend/Services/ShopOrderPlaceService.cs:176-187` (remove literal check; apply rule after `PriceLines`)
- Modify: `backend/TummlyBackend/Controllers/ShopOrdersController.cs:373` (`PayOrder`, before `_paySessions.StartAsync`; map `OfferCardOfferRequiredException.ErrorCode` to 409 in `PlaceOrder` too)
- Modify: `backend/TummlyBackend/Services/PrintReadyQrMaterialsService.cs:825-841`
- Test: `backend/TummlyBackend.Tests/Integration/ShopCartEndpointsTests.cs`, `Integration/ShopOrderPlaceEndpointsTests.cs`, `Integration/AdminShopOrderFulfilmentEndpointsTests.cs`, `Services/ComplimentaryStarterShopOrderServiceTests.cs`

**Interfaces:**
- Produces: `static class ShopCardOfferRules { static bool RequiresCardOffer(ShopCatalogDetailDto sku); static Task<bool> HasAttachableOfferAsync(ApplicationDbContext db, int restaurantId, int locationId, CancellationToken ct); static Task<string?> FirstPayBlockAsync(ApplicationDbContext db, IMaterialsCatalog catalog, ShopOrder order, CancellationToken ct) }`. `FirstPayBlockAsync` returns `"sku_not_available"`, `"offer_card_offer_required"`, or `null`.
- "Attachable" = the offer exists, `RestaurantLocationId == locationId`, and status is Draft or Active (same rule as `ShopOfferCardOfferService.SetAsync`).

- [ ] **Step 1: Write failing tests**
  - `CounterDisplay_CartPlacePay_SucceedWithoutAttachedOffer`
  - `GuestCards_Available_CartWithoutOffer_Returns409OfferRequired` (set `AvailableInShop = true` on `qr-guest-cards` in arrange)
  - `GuestCards_Available_PlaceWithoutOffer_Returns409OfferRequired`
  - `GuestCards_ComingSoon_Cart_Returns400SkuNotAvailable`
  - `GuestCards_OfferDetachedBetweenPlaceAndPay_PayReturns409`
  - `FromCart_GuestCardsMadeUnavailableAfterAdd_PlaceReturnsSkuNotAvailable` (Review Focus 1)
  - `CounterDisplay_PaidOrderPdf_ReadyWithoutAttachedOffer` (fulfilment test; assert asset status `Ready`)
  - `StarterKit_CounterPdf_ReadyWithoutAttachedOffer`
  - `GuestCard_ShopOrderAsset_StaysPreparingWithoutOffer`
- [ ] **Step 2: Run and confirm they fail.** `dotnet test backend/TummlyBackend.Tests --filter "FullyQualifiedName~ShopCartEndpointsTests|FullyQualifiedName~ShopOrderPlaceEndpointsTests|FullyQualifiedName~AdminShopOrderFulfilmentEndpointsTests|FullyQualifiedName~ComplimentaryStarterShopOrderServiceTests"`. Expected: the new tests fail.
- [ ] **Step 3: Implement `ShopCardOfferRules`, then use it in cart, place, pay and print.** In `EnsureOneWithinLockAsync`, change the gate condition from `QrType.OfferCard` to `QrType.GuestCard`. Update the existing test `AdminShopOrderFulfilmentEndpointsTests` (`:170`, "counter display gated before attach") so it asserts the new behaviour. Do not delete it.
- [ ] **Step 4: Run the same filter. Expected: PASS.** Then run the full backend suite once: `dotnet test backend/TummlyBackend.Tests`.
- [ ] **Step 5: Commit** `fix(shop): bind card-offer rule to Guest Cards and free Counter Display PDF`

### Task 2: Availability on recommendations, reorder and Starter Kit

**Files:**
- Modify: `backend/TummlyBackend/Services/ShopLocationRecommendationsService.cs:208-227`
- Modify: `backend/TummlyBackend/Services/ShopOrderCancelReorderService.cs:117-160`, its result DTO (`ShopReorderPrefillDto`), and `ShopOrdersController` reorder mapping
- Modify: `backend/TummlyBackend/PrintReadyQrMaterials/StarterQrMaterialTypes.cs:18-32`, `Services/ComplimentaryStarterShopOrderService.cs:252-260`
- Test: `Integration/ShopLocationRecommendationsEndpointsTests.cs`, `Integration/ShopOrderCancelReorderEndpointsTests.cs`, `Services/ComplimentaryStarterShopOrderServiceTests.cs`

**Interfaces:**
- Produces: `ShopReorderPrefillDto.UnavailableLines : IReadOnlyList<ShopReorderUnavailableLineDto>` with `{ string SkuId; string Title; }`. Wire name `unavailableLines`.

- [ ] **Step 1: Write failing tests**
  - `Recommendations_SkipUnavailableSkus` (prompt "tables" + GuestCard activity ≥ 5; assert no `table-tents` and no `qr-guest-cards` lines)
  - `Reorder_DropsUnavailableLines_ReturnsThemInUnavailableLines`
  - `Reorder_AllLinesUnavailable_Returns409ReorderNoAvailableLines`
  - `StarterKit_ExcludesStarterFlaggedSkuWhenUnavailable`
- [ ] **Step 2: Run and confirm they fail.**
- [ ] **Step 3: Implement.** Filter on `detail.AvailableInShop` in each path.
- [ ] **Step 4: Run. Expected: PASS.**
- [ ] **Step 5: Commit** `fix(shop): keep Coming soon products out of recommendations, reorder and starter kit`

### Task 3: QR path flag data and Admin activation guard

**Files:**
- Modify: `backend/TummlyBackend/Shop/MaterialsCatalog/ShopMaterialsCatalogSeed.cs:33-72` (set `MintDefault = true` for `table-tent`, `table-talker`, `window-sticker`, `qr-guest-cards`)
- Create: EF migration `ShopMintOnFulfilmentFlags` (data-only `UPDATE` that sets `MintOnShopFulfilment = 1` for SKU ids `table-tents`, `table-talker`, `window-stickers`, `qr-guest-cards`)
- Create: `backend/TummlyBackend/Shop/ShopQrPathRules.cs`
- Modify: `backend/TummlyBackend/Services/AdminShopCatalogService.cs:489-525` (`SetAvailableInShopAsync`), `Services/AdminShopInterestProductService.cs:387`
- Test: `Services/AdminShopCatalogServiceTests.cs`, `Shop/ShopMaterialsCatalogSeedTests.cs`

**Interfaces:**
- Produces: `static class ShopQrPathRules { static Task<bool> HasQrPathAsync(ApplicationDbContext db, ShopMaterialSku sku, CancellationToken ct) }`. True when the SKU's recipe has `IncludeInComplimentaryStarter` or `sku.MintOnShopFulfilment`.

- [ ] **Step 1: Write failing tests**
  - `SetAvailable_NoQrPath_Returns409SkuHasNoQrPath`
  - `SetAvailable_MintFlagTrue_Succeeds`
  - `Seed_NonStarterRecipes_DefaultMintOnFulfilmentTrue`
  - `InterestPromote_NoQrPath_Returns409`
- [ ] **Step 2: Run and confirm they fail.**
- [ ] **Step 3: Implement the guard and seed defaults. Then generate the migration:** `cd backend/TummlyBackend && dotnet ef migrations add ShopMintOnFulfilmentFlags`. Put the `UPDATE` in `Up` (`migrationBuilder.Sql`) and the reverse in `Down`.
- [ ] **Step 4: Run. Expected: PASS.** Also run `bash backend/TummlyBackend/scripts/check-migration-designer-pairs.sh` (or the repo's equivalent CI script). Expected: no errors.
- [ ] **Step 5: Commit** `feat(shop): QR path flag defaults and Admin availability guard`

### Task 4: Shared capacity service, placement resolver, reservation table

**Files:**
- Create: `backend/TummlyBackend/Models/ShopQrSlotReservation.cs` (columns per spec 4.4.1) and `ShopQrSlotReservationStatuses` constants
- Modify: `backend/TummlyBackend/Data/ApplicationDbContext.cs` (DbSet, unique index `(ShopOrderId, QrType)`, index `(RestaurantLocationId, QrType, Status)`, FKs `NoAction`)
- Create: EF migration `ShopQrSlotReservations`
- Create: `backend/TummlyBackend/Services/ActiveQrCapacity.cs` + `Interfaces/IActiveQrCapacity.cs`
- Create: `backend/TummlyBackend/Services/QrPlacementResolver.cs` + `Interfaces/IQrPlacementResolver.cs`
- Modify: `backend/TummlyBackend/Services/CaptureQrLifecycleService.cs:845-1000` (move `DenyIfActiveQrWouldExceedAsync`, `LoadBillingAccountForUpdateAsync` and `TryResolveActiveQrCap` into `ActiveQrCapacity`, then call it)
- Modify: `Program.cs` (DI, scoped)
- Test: `Services/ActiveQrCapacityTests.cs`, `Services/QrPlacementResolverTests.cs`. The existing Capture lifecycle tests must stay green.

**Interfaces:**
- Produces:
  - `interface IActiveQrCapacity`:
    - `Task<BillingAccount?> LockBillingAccountAsync(int restaurantId, CancellationToken ct)`. The caller owns the transaction.
    - `Task<ActiveQrAllowance?> GetAllowanceAsync(BillingAccount? account, int locationId, CancellationToken ct)`. Returns null when entitlements are unavailable.
    - `Task<int> CountUsedAsync(int locationId, CancellationToken ct)`. Uses the spec 4.4.2 formula: Active codes + distinct QrType among held reservations with no Active code of that type.
    - `Task<bool> FitsAsync(int restaurantId, int locationId, int increment, CancellationToken ct)`. Current per-location cap plus account-extra pool logic, unchanged.
  - `sealed record ActiveQrAllowance(int PerLocationCap, int AccountExtra)`.
  - `interface IQrPlacementResolver { Task<QrPlacementResolution> ResolveAsync(int locationId, QrType qrType, CancellationToken ct) }`.
  - `abstract record QrPlacementResolution`, with `sealed record Reuse(int QrCodeId)` and `sealed record NeedsSlot(int? ReactivateQrCodeId)`. Oldest by `CreatedAt`. Archived codes (`ArchivedAt != null`) are never chosen.
  - `CaptureQrLifecycleService` keeps the same `QrLifecycleResult.ActiveQrCapReached(perLocCap, current)` outcomes.

- [ ] **Step 1: Write failing tests**
  - `CountUsed_ActiveOnly_EqualsActiveCount`
  - `CountUsed_TwoHeldReservationsSameType_CountOnce`
  - `CountUsed_HeldReservationWhereActiveCodeExists_NotCounted`
  - `CountUsed_ReviewStatusCountsAsHeld`
  - `CountUsed_ReleasedAndConverted_NotCounted`
  - `Resolve_ActiveExists_ReturnsOldestReuse`
  - `Resolve_OnlyPaused_ReturnsNeedsSlotWithOldestPausedId`
  - `Resolve_OnlyArchived_ReturnsNeedsSlotNull`
  - `Resolve_None_ReturnsNeedsSlotNull`
- [ ] **Step 2: Run and confirm they fail.**
- [ ] **Step 3: Add the model and DbContext config. Generate the migration:** `dotnet ef migrations add ShopQrSlotReservations`. Implement both services, then switch Capture to `IActiveQrCapacity`.
- [ ] **Step 4: Run** the new tests and `--filter "FullyQualifiedName~CaptureQrLifecycle"`. Expected: PASS, with no existing Capture test changed.
- [ ] **Step 5: Commit** `feat(capture): shared active-QR capacity service and placement resolver`

### Task 5: Capacity at place and reservation at pay

**Files:**
- Create: `backend/TummlyBackend/Services/ShopQrSlotReservationService.cs` + `Interfaces/IShopQrSlotReservationService.cs`
- Modify: `Services/ShopOrderPlaceService.cs` (check only, after pricing)
- Modify: `Controllers/ShopOrdersController.cs` `PayOrder` (call `ReserveForPayAsync` before `_paySessions.StartAsync`; map the result to 409 `qr_limit_reached`)
- Test: `Integration/ShopOrderPlaceEndpointsTests.cs`, `Integration/ShopOrderPayCapacityEndpointsTests.cs` (new), `SqlServer/ShopQrCapacityConcurrentSqlTests.cs` (new, `[Collection(SqlServerCollection.Name)]`, `SkippableFact`, pattern from `TopupConcurrentApplySqlTests`)

**Interfaces:**
- Consumes: `IActiveQrCapacity`, `IQrPlacementResolver`, `PrintReadyQrMaterialsService.ResolveOrderedQrTypes`.
- Produces: `interface IShopQrSlotReservationService`:
  - `Task<QrCapacityCheck> CheckForPlaceAsync(int restaurantId, int locationId, IReadOnlyCollection<QrType> types, CancellationToken ct)`
  - `Task<QrCapacityCheck> ReserveForPayAsync(ShopOrder order, CancellationToken ct)`. This method opens its own transaction and takes `LockBillingAccountAsync`. `ReconcileAfterUtc = now + 30 min`.
  - `Task ReleaseForOrderAsync(Guid shopOrderId, CancellationToken ct)`. Sets `open` → `released`.
  - `sealed record QrCapacityCheck(bool Ok, int Limit)`. `Limit` = the per-location cap shown in the dialog.
- "Needed" counts only distinct `NeedsSlot` types that have no held reservation for the same (location, type). Idempotency: an existing `(ShopOrderId, QrType)` row in `open` adds nothing.

- [ ] **Step 1: Write failing tests**
  - `Place_AtLimit_NeedsNewGuestCardQr_Returns409QrLimitReachedWithLimit`
  - `Pay_AtLimit_NeedsNewQr_Returns409_AndNoRevolutOrderCreated` (fake merchant records zero `CreateOrderAsync` calls)
  - `Pay_ReorderAtLimit_ReusesActive_SucceedsAndWritesNoReservation`
  - `Pay_MixedReuseAndNew_AtLimitMinusOne_WritesExactlyOneReservation` (Review Focus 3)
  - `Pay_Repeated_SameOrder_OneReservationOneIntent`
  - `Pay_NeedsSlotWithExistingHeldReservationForSameKey_SucceedsAtLimit`
  - SQL: `TwoConcurrentPays_DifferentTypes_LastSlot_ExactlyOneSucceeds`
  - SQL: `TwoConcurrentPays_SameType_LastSlot_BothSucceedOneSlot`
  - SQL: `ConcurrentCaptureActivateAndShopPay_LastSlot_ExactlyOneSucceeds`
- [ ] **Step 2: Run and confirm they fail.** The SQL tests skip without Docker. Run them where Docker is available and record the result in the commit message.
- [ ] **Step 3: Implement the service and wire place and pay.** If Revolut session creation throws after the reservation commit, leave the reservation `open`.
- [ ] **Step 4: Run. Expected: PASS** (SQL tests PASS with Docker, or SKIP with the fixture reason).
- [ ] **Step 5: Commit** `feat(shop): reserve QR capacity before payment`

### Task 6: Convert on payment completed; release on failure and cancel

**Files:**
- Modify: `backend/TummlyBackend/Services/RevolutOrderCompletedApplier.cs:1097-1140` (`ApplyShopMaterialsOrderAsync`)
- Modify: `backend/TummlyBackend/Helpers/ShopMaterialsOrderPaymentFailure.cs` (release after marking failed)
- Modify: `backend/TummlyBackend/Services/ShopOrderCancelReorderService.cs:27` (`CancelAsync`, release)
- Modify: `IShopQrSlotReservationService` (add the method below)
- Test: `Services/ShopQrSlotConversionTests.cs` (new), `Services/RevolutOrderCompletedApplierTests.cs`, `Integration/RevolutWebhookEndpointsTests.cs`

**Interfaces:**
- Produces: `Task<IReadOnlyList<int>> ConvertForPaidOrderAsync(ShopOrder order, CancellationToken ct)` on `IShopQrSlotReservationService`. It runs inside the caller's transaction after `LockBillingAccountAsync`. It returns the QR code ids. It does not check the limit for held reservations. If a paid order has a `NeedsSlot` type with no held reservation, it checks the limit. If the limit is full, it sets the order review reason `qr_capacity_unavailable_after_payment` and logs a warning.
- Order review: add `ShopOrder.ReviewReason` (`string(64)?`) with a migration generated in this task (`ShopOrderReviewReason`).

- [ ] **Step 1: Write failing tests**
  - `Paid_GuestCardOrder_NoCode_MintsOneActive_PrintJobReady`
  - `Paid_SecondOrder_ReusesSameCode`
  - `Paid_PausedCodeExists_ReactivatesKeepingToken`
  - `Paid_OnlyArchivedCodeExists_MintsNewCode` (Review Focus 4)
  - `Paid_PlanCapLoweredAfterReserve_StillConverts` (Review Focus 5)
  - `SharedKey_FirstOrderFails_SlotStillHeld_CaptureActivateDenied`
  - `SharedKey_FirstConverts_SecondLaterConvertsToSameCode`
  - `OrderCompleted_DeliveredTwice_OneCodeOneConversion`
  - `OrderCancelled_AfterCompleted_ReservationStaysConverted`
  - `OrderCompleted_AfterRelease_CapacityFull_OrderReviewNoRefund` (fake merchant records zero `RefundOrderAsync` calls)
  - `Cancel_UnpaidOrder_ReleasesReservation`
- [ ] **Step 2: Run and confirm they fail.**
- [ ] **Step 3: Implement.** Call `ConvertForPaidOrderAsync` in the same transaction that sets `Paid`.
- [ ] **Step 4: Run. Expected: PASS.** Run the full backend suite.
- [ ] **Step 5: Commit** `feat(shop): convert QR reservations on payment and release on failure`

### Task 7: Revolut cancel call and reconciliation sweeper

**Files:**
- Modify: `backend/TummlyBackend/Interfaces/IRevolutMerchantClient.cs` (add `CancelOrderAsync` as a **default interface method** that returns `new RevolutOrderRetrieveResult(Succeeded: false, ErrorCode: "not_supported")`. The 16 existing test fakes then compile unchanged.)
- Modify: `backend/TummlyBackend/Services/RevolutMerchantClient.cs` (real `POST api/orders/{id}/cancel`; parse `state` like `GetOrderAsync`)
- Create: `backend/TummlyBackend/Services/ShopQrSlotReservationSweeperBackgroundService.cs` (pattern: `CreditReservationSweeperBackgroundService`, 1-minute interval, public `SweepAsync(CancellationToken)`)
- Modify: `Program.cs` (hosted service)
- Test: `Services/ShopQrSlotReservationSweeperTests.cs` (new), `Services/RevolutMerchantClientTests.cs` (cancel parse)

**Interfaces:**
- Consumes: `IRevolutOrderCompletedApplier.ApplyAsync`, `ReleaseForOrderAsync`, `ShopMaterialsOrderPaymentFailure.TryMarkFailedAsync`, `GetOrderAsync`.
- Produces: `Task<RevolutOrderRetrieveResult> CancelOrderAsync(string orderId, CancellationToken ct = default)`.
- Decision table per order (spec 4.4.4):

| Revolut state | Action |
|---|---|
| `completed` | Call `IRevolutOrderCompletedApplier.ApplyAsync(new RevolutOrderCompletedApplyRequest(OrderId, "completed", ...))`, the same path as the webhook. It converts through `ConvertForPaidOrderAsync`. |
| `pending`, `authorised` | Cancel. Release only if the cancel response or a re-read shows `cancelled`. Otherwise back off. |
| `cancelled`, `failed` | Mark failed, then release |
| `processing`, other, or HTTP error | Back off, `ReconcileAttempts++`, set `LastReconcileError` |
| No intent | Release |
| Any non-final result and `now >= CreatedAtUtc + 24h` | Set `review`, log a warning |

- [ ] **Step 1: Write failing tests** (fake merchant with scripted states, `FakeTimeProvider`)
  - `Sweep_Pending_CancelSucceeds_OrderFailed_Released`
  - `Sweep_Pending_CancelFails_StaysOpen_AttemptsIncrement_RetryIn5Min`
  - `Sweep_Processing_StaysOpen_ThenCompleted_Converts`
  - `Sweep_Completed_NoWebhookYet_ConvertsAndMarksPaid`
  - `Sweep_ErrorFor24h_MovesToReview_StillCountsAsHeld`
  - `Sweep_NoIntent_Releases`
  - `Sweep_BackoffSequence_5_15_60_60`
  - `CancelOrderAsync_ParsesCancelledState`
- [ ] **Step 2: Run and confirm they fail.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run. Expected: PASS.** Run the full backend suite.
- [ ] **Step 5: Commit** `feat(shop): reconcile stale QR reservations with Revolut before release`

### Task 8: Capture pause warning for Shop-printed placements

**Files:**
- Modify: `backend/TummlyBackend/Services/CaptureLocationSnapshotService.cs` and its placement row DTO (add `HasShopPrintedMaterials`)
- Modify: `src/types/dashboard.ts:~290` (`CapturePlacementItem.hasShopPrintedMaterials?: boolean`)
- Modify: `src/lib/operatorCapture/buildPauseActivateConfirm.ts` (pause + `hasShopPrintedMaterials` → use the Global Constraints warning; otherwise keep `PRINT_WARNING`)
- Test: `Integration/CaptureLocationSnapshotEndpointsTests.cs` (or the existing snapshot test file), `src/lib/operatorCapture/buildPauseActivateConfirm.test.ts`

**Interfaces:**
- `HasShopPrintedMaterials` is true when a `ShopOrder` at the location has `PaymentStatus == paid` and a line whose catalog QR type equals the code's `QrType`.

- [ ] **Step 1: Write failing tests:** `Snapshot_PlacementWithPaidShopOrderOfType_HasShopPrintedMaterialsTrue`; Vitest `pause with hasShopPrintedMaterials shows shop warning`; `pause without flag keeps print warning`.
- [ ] **Step 2: Run and confirm they fail.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run. Expected: PASS.** Also run `npm run typecheck`.
- [ ] **Step 5: Commit** `feat(capture): warn before pausing a placement used by Shop prints`

### Task 9: Frontend offer dialog bound to Guest Cards, never on load

**Files:**
- Modify: `src/lib/operatorShop/shopOfferCardSetupPresentation.ts:38` (rename to `SHOP_GUEST_CARD_SKU_ID = "qr-guest-cards"`)
- Modify: `src/lib/operatorShop/createShopOfferCardSetupModule.ts` (`loadForLocation` never sets `isOpen: true` and sets `isOpen: false`; new adapter `isGuestCardAvailable: () => boolean`; `open()` returns `"noop"` when false; delete `openIfNeeded` if unused)
- Modify: `src/components/dashboard/operator/Shop/ShopPage.tsx` (rename uses; pass `isGuestCardAvailable` from `catalogProducts`; on a 409 `offer_card_offer_required` from cart, place or pay, call `offerCardSetupModule.open()`)
- Create: `src/lib/operatorShop/readShopApiErrorCode.ts`. `readShopApiErrorCode(error: unknown): string | null` reads `response.data.code` from an Axios error.
- Test: `src/lib/operatorShop/createShopOfferCardSetupModule.test.ts`, `src/components/dashboard/operator/Shop/ShopPage.offerDialog.test.tsx` (new)

**Interfaces:**
- Produces: `readShopApiErrorCode` (Task 10 uses it), `SHOP_GUEST_CARD_SKU_ID`.

- [ ] **Step 1: Write failing tests**
  - `loadForLocation never opens (attached and not attached)`
  - `open returns noop when Guest Cards unavailable`
  - `location change closes an open dialog and reloads attach state` (Review Focus 2)
  - ShopPage with the launch catalog (Counter Display available, Guest Cards Coming soon): no dialog on load, on Counter Display Add to cart, on Order now, or on Checkout
  - `409 offer_card_offer_required opens the dialog`
- [ ] **Step 2: Run and confirm they fail.** `npx vitest run src/lib/operatorShop src/components/dashboard/operator/Shop`
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run. Expected: PASS.** Then `npm run typecheck && npm run lint`.
- [ ] **Step 5: Commit** `fix(shop): open card-offer dialog only for available Guest Cards`

### Task 10: Frontend QR limit dialog and reorder unavailable lines

**Files:**
- Create: `src/components/dashboard/operator/Shop/ShopQrLimitDialog.tsx` (shadcn `Dialog`, copy from Global Constraints)
- Create: `src/lib/operatorShop/shopQrLimitPresentation.ts` (`buildShopQrLimitBody(limit: number): string`)
- Modify: `src/components/dashboard/operator/Shop/ShopCheckoutScreen.tsx:320-355` (on `qr_limit_reached` from place or pay, open the dialog. **Back to order** closes it and keeps state. **Review plan options** goes to the Billing manage-plan route. **Manage QR codes** goes to Capture for `locationId`. Use the existing route helpers in `src/lib/operatorHome/operatorDashboardPaths.ts`.)
- Modify: `src/api/shopOrdersApi.ts:97` (`ShopReorderPrefillWire.unavailableLines?: { skuId: string; title: string }[]`), `ShopPage.tsx` `onReorder` (toast `Some items are no longer available: <titles>`)
- Test: `src/components/dashboard/operator/Shop/ShopQrLimitDialog.test.tsx`, `src/lib/operatorShop/shopQrLimitPresentation.test.ts`

- [ ] **Step 1: Write failing tests:** `body uses limit N verbatim copy`; `dialog shows title and three buttons`; `Back to order keeps checkout state`; `reorder with unavailableLines shows the toast with titles`.
- [ ] **Step 2: Run and confirm they fail.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run. Expected: PASS.** Then `npm run typecheck && npm run lint && npm test`.
- [ ] **Step 5: Commit** `feat(shop): QR code limit dialog at checkout`

### Task 11: Whole-branch verification

- [ ] Run `dotnet test backend/TummlyBackend.Tests` (with Docker, so the SQL lock tests run). Expected: all pass, with no new skips except SQL tests where Docker is missing.
- [ ] Run `npm run typecheck && npm run lint && npm test`. Expected: all pass.
- [ ] Manual check (`run` skill): with the launch catalog, open Shop and order a Counter Display. No card-offer dialog appears.
- [ ] Spec section 7 acceptance list: tick each line against a passing test name.

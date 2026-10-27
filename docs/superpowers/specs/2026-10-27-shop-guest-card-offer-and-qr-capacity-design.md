# Shop: Guest Card offer binding and QR capacity at checkout

**Date:** 2026-10-27
**Status:** Draft for review
**Branch:** `feat/shop-guest-card-offer-qr-capacity`
**Figma (Shop):** https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP?node-id=3963-77369&m=dev

## 1. Why

The client audited the Tummly Shop. The audit found these problems:

1. The "Set up your card offer" dialog opens when the Shop page loads.
2. The dialog is linked to the wrong product. The code links it to `qr-counter-display`. The approved rule (Director-approved Shop and Print reconciliation, 7 October 2026) links it to **QR Guest Cards** only.
3. The A5 Counter Display PDF waits for a card offer, but that PDF does not print an offer.
4. The Shop cannot make a QR code for a non-starter product. If Admin activates Guest Cards today, every paid order fails at print time.

This spec corrects the product binding. It also adds the missing QR-at-fulfilment step and makes the Shop obey the plan's active-QR limit, with the same lock that Capture uses.

## 2. Approved product rules

| Product | Availability now | Card offer setup | Print output |
|---|---|---|---|
| QR Counter Display (`qr-counter-display`) | Available | Never | A5 print-ready PDF, the location's production QR, no offer headline |
| QR Guest Cards (`qr-guest-cards`) | Coming soon | Only when available, only on purchase | 80 × 50.8 mm template, variable QR + `OfferHeadline`, Guest Card CSV |

- A product that is "Coming soon" (`AvailableInShop = false`) never enters a cart, an order, a reorder, a recommendation or the Starter Kit.
- The server enforces every rule. Frontend visibility is only a convenience.
- Counter Display checkout, the complimentary Starter Kit and the A5 PDF do not depend on the card-offer dialog or on an attached offer.
- A QR code that the Shop makes obeys the same plan limit as a QR code that Capture makes. The Shop cannot bypass the subscription entitlement.

## 3. Facts from the code (as of `qa` 8979bb4d)

- The attached offer is stored per location in `RestaurantLocation.OfferCardCatalogOfferId`. `GET/PUT /api/shop/locations/{id}/offer-card-offer` reads and writes it. This spec keeps the column and the routes. It changes only their meaning to "the Guest Card print offer".
- `CatalogOfferStatus.AttachSourceOfferCard = "offer-card"` is a derived filter label. Do not rename it.
- Server offer gates check the literal SKU `"offer-card"` (`ShopCartService.cs:65-80`, `ShopOrderPlaceService.cs:176-187`). After the seed remap, that SKU no longer exists, so these gates never run.
- `PrintReadyQrMaterialsService.EnsureOneWithinLockAsync` (`:825-841`) stops every `QrType.OfferCard` asset until an offer is attached. The Counter Display recipe uses `QrType.OfferCard`.
- The Guest Card CSV already has an `OfferHeadline` column (`PrintReadyPlacementCsvComposer.cs`). It reads the attached offer title. If no offer is attached, it silently uses the pack default.
- QR codes are made only by `QrCodeProvisioningService` (starter types) and `CaptureQrLifecycleService` (Digital Guest Link). `MintOnShopFulfilment` is stored but nothing reads it.
- Capture checks the active-QR limit in `CaptureQrLifecycleService.DenyIfActiveQrWouldExceedAsync`, under an `UPDLOCK` on the `BillingAccounts` row.
- Shop checkout has two steps. `POST /api/shop/orders` makes the order as `awaiting_payment`. `POST /api/shop/orders/{id}/pay` opens a Revolut hosted payment page. An unpaid order never expires.
- `QrCode` has no Guest Form link and no Offer link. The Guest Form (`ThankYouCatalogOfferId`, form settings) and the print offer are location-scoped.

## 4. Design

### 4.1 Rebind the card offer to Guest Cards

**Server**

- Add `ShopCardOfferRules.RequiresCardOffer(snapshotSku)`. It is true when the SKU's `QrType == QrType.GuestCard`. The rule uses the recipe's QR type, not a SKU string, so an Admin SKU rename cannot break it.
- **Cart upsert** (`ShopCartService`). Run the existing availability check first. Then, if `RequiresCardOffer` and the location has no attachable offer, throw `OfferCardOfferRequiredException` (409 `offer_card_offer_required`). Delete the dead `"offer-card"` literal check.
- **Order place** (`ShopOrderPlaceService.PlaceAsync`). Apply the same rule after pricing resolves each SKU. Return 409 `offer_card_offer_required`. Delete the dead literal check.
- **Pay** (`/orders/{id}/pay`). Check availability and the card-offer rule again before you open a Revolut session. Admin can deactivate a product, or an operator can detach an offer, between place and pay.
- **"Attachable offer"** uses the same rule as `ShopOfferCardOfferService.SetAsync`: the offer exists, belongs to the location, and is Draft or Active.
- **Print gate.** Remove the `QrType.OfferCard` attach gate from `EnsureOneWithinLockAsync`. Add the gate for `QrType.GuestCard`: without an attached offer, the asset stays at Preparing. A Guest Card CSV never uses the default headline for a shop order.
- **Keep `MapMaterialType`** (`ShopOrderPlaceService.cs:488-507`) as it is. It is a stored display label.

**Frontend**

- Rename `SHOP_OFFER_CARD_SKU_ID` to `SHOP_GUEST_CARD_SKU_ID = "qr-guest-cards"`. Update every call site in `ShopPage.tsx`.
- `createShopOfferCardSetupModule.loadForLocation()` loads the attach state only. It never sets `isOpen`. Delete `openIfNeeded()` if it has no caller after this change.
- Add the adapter `isGuestCardAvailable(): boolean`, which reads the catalog. The module's `open()` returns `"noop"` when Guest Cards are not available.
- `requireOfferCardAttach()` runs only for Guest Card lines: Add to cart, Order now, Checkout, Reorder, materials pack and recommended kit.
- When the server returns 409 `offer_card_offer_required`, the frontend opens the dialog. It shows no generic error toast.

### 4.2 Availability on every entry path

- **Recommendations** (`ShopLocationRecommendationsService`). Skip SKUs with `AvailableInShop = false`.
- **Reorder prefill** (`ShopOrderCancelReorderService`). Drop unavailable lines and return them in `unavailableLines` with title and SKU. The frontend shows "Some items are no longer available" and lists them. If every line is unavailable, return 409 `reorder_no_available_lines`.
- **Starter Kit** (`StarterQrMaterialTypes`, `ComplimentaryStarterShopOrderService`). Require `AvailableInShop = true` together with `IncludeInComplimentaryStarter`.

### 4.3 Make QR codes at shop fulfilment

- **Placement key.** A shop line needs a QR placement for its location and QR type. Because the Guest Form and the print offer are location-scoped (section 3), the key is **(Location, QrType)**. If a later change binds a Guest Form or an Offer to one QR code, the key must grow to include that binding. Section 4.4 and `QrPlacementResolver` (below) are the only places that use the key.
- **`QrPlacementResolver.Resolve(locationId, qrType)`** returns one of these results:
  - `Reuse(qrCodeId)`: an Active, not archived QR code of that type exists at the location. If more than one exists, choose the oldest by `CreatedAt`, so a reorder gets the placement that earlier prints used.
  - `NeedsSlot(reactivateQrCodeId?)`: no Active code exists. If a Paused, not archived code of that type exists, set `reactivateQrCodeId` to the oldest one. Reactivation keeps its token and attribution. If no such code exists, the server makes a new code.
- **Starter types never use this path.** The starter QR comes from `QrCodeProvisioningService`. Counter Display keeps `MintOnShopFulfilment = false`.
- **Flag data.** An EF data migration sets `MintOnShopFulfilment = true` for `table-tents`, `table-talker`, `window-stickers` and `qr-guest-cards`. The seed defaults for these recipes change to `true`.
- **Admin guard.** `AdminShopCatalogService` rejects `AvailableInShop = true` with 409 `sku_has_no_qr_path` when the SKU is not a starter type and `MintOnShopFulfilment = false`. `AdminShopInterestProductService` promote uses the same guard.

### 4.4 QR capacity at checkout

#### 4.4.1 Data

New table `ShopQrSlotReservations`:

| Column | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `ShopOrderId` | `Guid` | FK `ShopOrders` |
| `RestaurantLocationId` | `int` | FK |
| `QrType` | `QrType` | |
| `ReactivateQrCodeId` | `int?` | From the resolver, if any |
| `Status` | `string(16)` | `open`, `converted`, `released`, `review` |
| `ReconcileAfterUtc` | `DateTime` | Created time + 30 min |
| `ReconcileAttempts` | `int` | |
| `LastReconcileAtUtc` | `DateTime?` | |
| `LastReconcileError` | `string(500)?` | |
| `ConvertedQrCodeId` | `int?` | QR code made or reactivated |
| `CreatedAtUtc`, `UpdatedAtUtc` | `DateTime` | |

- Unique index on `(ShopOrderId, QrType)`. This makes repeated `/pay` calls idempotent.
- Index on `(RestaurantLocationId, QrType, Status)`.

#### 4.4.2 Shared capacity count

Add one service, `IActiveQrCapacity`, used by Shop and by Capture:

```
used(location) = count(Active QR codes at location)
               + count(DISTINCT QrType among held reservations at location
                       where no Active QR code of that QrType exists)
```

A **held** reservation has status `open` or `review`.

- Two open reservations for the same (Location, QrType) use **one** slot, because both orders will use the same placement.
- Releasing one of them does not free the slot while another open reservation for the same key exists. This follows from the `DISTINCT` count, so it needs no reference counter.
- The per-location limit and the account-wide extra pool keep the current rules in `DenyIfActiveQrWouldExceedAsync`. Move that logic into `IActiveQrCapacity` without a change in behaviour. Capture calls the new service.
- Every caller holds the `BillingAccounts` `UPDLOCK` in a transaction during check and write. Shop and Capture therefore serialize on the same row.

#### 4.4.3 Flow

1. **Place** (`POST /orders`). Resolve each QR type in the order. Count the distinct `NeedsSlot` types that have no open reservation already. If `used + needed > allowance`, return 409 `qr_limit_reached` with `{ limit: N }`. This is a check only: it takes no lock and writes no reservation.
2. **Pay** (`POST /orders/{id}/pay`). In one transaction, under the billing-account lock:
   - Run the resolver again for each QR type.
   - For `Reuse`, write nothing.
   - For `NeedsSlot`, upsert the reservation `(ShopOrderId, QrType)`. If it already exists and is `open`, keep it and do not count it again.
   - If the new slots exceed the allowance, roll back and return 409 `qr_limit_reached`. The server does not create or reuse a Revolut order.
   - Commit, then open or reuse the Revolut session as today.
   - If Revolut session creation fails after commit, the reservation stays `open` and the sweeper reconciles it.
3. **Payment completed** (`RevolutOrderCompletedApplier.ApplyShopMaterialsOrderAsync`). In the same transaction that marks the order Paid, under the billing-account lock, and for each `open` reservation of the order:
   - If an Active code of that type now exists, set `ConvertedQrCodeId` to it.
   - Else, if `ReactivateQrCodeId` is still Paused and not archived, set that code Active.
   - Else, make a new Active code (token from `SmartGuestLink.GenerateTokenAsync`).
   - Set the reservation to `converted`. Do not check the limit again here, because the slot is already held.
   - Other `open` reservations for the same key stay open until their own order resolves. They then convert to the same code through the first branch.
4. **Payment failed or order cancelled** (`ShopMaterialsOrderPaymentFailure`, order cancel). Set the order's `open` reservations to `released`.
5. **Fulfilment.** `EnsureShopOrderMaterialsAsync` runs as today. It now finds an Active code of each type.

#### 4.4.4 Reconciliation at 30 minutes

`ShopQrSlotReservationSweeperBackgroundService` follows `CreditReservationSweeperBackgroundService` (1-minute sweep). For each `open` reservation with `ReconcileAfterUtc <= now`, it groups by order and does these steps:

1. Read the order's open Revolut intent and call `GetOrderAsync`. The Revolut state is the source of truth.
2. **`completed`**: run the payment-completed path (4.4.3 step 3). It is idempotent with the webhook.
3. **`pending`** (or `authorised`): call the new `RevolutMerchantClient.CancelOrderAsync` (`POST /api/orders/{id}/cancel`).
   - If the response, or a follow-up `GetOrderAsync`, shows `cancelled`, mark the order `payment_failed`, close the intent and release the reservations.
   - If the cancel fails, keep the reservations and retry.
4. **`cancelled` or `failed`**: release the reservations. Mark the order `payment_failed` if it is still `awaiting_payment`.
5. **`processing`, an unknown state, or a Revolut error**: keep the reservations. Increase `ReconcileAttempts`, set `LastReconcileError`, and push `ReconcileAfterUtc` back with backoff: 5 min, then 15 min, then 60 min, capped at 60 min.
6. **No Revolut intent** (pay never opened a session): release the reservations. No payment can exist.
7. After **24 hours** without a resolution, set the reservation to `review` and log a structured warning for operational review. A `review` reservation still counts as held, so the slot stays reserved. Admin resolves it by hand.

A capacity reservation never disappears while a payment can still complete.

#### 4.4.5 Webhooks: idempotency and order

- `ORDER_COMPLETED` delivered twice: the second call finds the order already Paid and the reservations `converted`. It does nothing.
- `ORDER_COMPLETED` after the sweeper released a reservation. This happens only when Revolut reported `cancelled` or `failed` and then the order completed anyway, which should not occur.
  - Mark the order Paid.
  - Run the resolver again under the lock. If capacity exists, make the code.
  - If capacity does not exist, set the order to `review` with reason `qr_capacity_unavailable_after_payment` and notify Admin.
  - A refund through the existing `RefundOrderAsync` is the exceptional recovery path. It is never automatic.
- `ORDER_CANCELLED` or `ORDER_FAILED` after `ORDER_COMPLETED`: ignore it for the reservation, because a `converted` reservation is final.
- `/pay` called again or at the same time: the unique index and the lock stop duplicate reservations. Revolut intent reuse keeps today's idempotency-key rules.

### 4.5 Frontend: "QR code limit reached"

- The frontend handles `qr_limit_reached` from Place and from Pay with one dialog (shadcn `Dialog`):
  - Title: **QR code limit reached**
  - Body: **Your plan allows N active QR codes at this location. You can use an existing active QR code or upgrade your plan to add another.**
  - Buttons: **Back to order** (closes the dialog and keeps the checkout state), **Review plan options** (Billing > Manage plan), **Manage QR codes** (Capture for this location).
- The frontend does not create a print job or an order line state for this case.
- **Capture pause warning.** Before an operator pauses a QR code, Capture checks whether any paid shop order uses that QR type at that location. If one exists, the confirm dialog adds: **"Printed materials from your Tummly Shop orders use this QR code. Guests who scan them will not reach your form while it is paused."** The server exposes this as `hasShopPrintedMaterials` on the QR detail response.

## 5. Out of scope

These items are separate work in the same client feedback:

- **B.** Shop page layout from Figma: "Available now", then "Explore more QR materials" (node `3963-77369`).
- **C.** Full-colour Guest Card preview: green background and white box. Blocked until the client sends the card artwork.
- **D.** Letter-spacing and line-height on the marketing website and the web app.
- **E.** "Offer title" feedback. Waiting for the rest of the transcript.
- Fixing `ShopOrdersListService` materialType filter allowlist. This is an existing bug and gets its own ticket.
- Guest Card CSV headline staleness after an offer change. This is an existing risk and gets its own ticket.

## 6. Tests

All capacity and concurrency tests run on SQL Server through `Testcontainers.MsSql`, so `UPDLOCK` behaves as in production.

### 6.1 Product binding and availability

1. Counter Display: cart, place and pay succeed with no attached offer.
2. Counter Display: the Starter Kit and paid-order A5 PDFs reach Ready with no attached offer.
3. Guest Cards (available): cart, place and pay without an attached offer return 409 `offer_card_offer_required`.
4. Guest Cards (available): with an offer attached, the order succeeds and the CSV `OfferHeadline` equals the offer title.
5. Guest Cards (Coming soon): cart, place and pay return `sku_not_available`.
6. An offer detached between place and pay makes pay return 409.
7. Recommendations, reorder prefill and the Starter Kit skip unavailable SKUs. Reorder returns `unavailableLines`.
8. Admin cannot set a SKU with no QR path to available (409 `sku_has_no_qr_path`).

### 6.2 QR fulfilment

9. A paid Guest Card order with no GuestCard QR code makes one Active code, and the print job reaches Ready.
10. A second paid order uses the same code again. No new code is made.
11. A Paused, not archived GuestCard code is reactivated, and its token stays the same.

### 6.3 Capacity

12. An order at the limit that needs a new QR code: Place returns 409 `qr_limit_reached`. Pay returns 409, and no Revolut order is created.
13. A reorder at the limit that reuses an Active placement: pay succeeds, and no reservation row is written.
14. Two concurrent `/pay` calls for different orders that both need the last slot for **different** QR types: exactly one succeeds.
15. Two concurrent `/pay` calls for orders that need the **same** (Location, QrType) at the last slot: both succeed and use one slot.
16. Concurrent Capture activation and Shop `/pay` for the last slot: exactly one succeeds.
17. Repeated `/pay` for the same order: one reservation row, one Revolut intent.

### 6.4 Shared reservations

18. Two open reservations for one key. The first order fails, and the slot is still held (Capture activation at the limit is denied). The second order completes and converts.
19. Two open reservations for one key. The first order completes and converts. The second order later completes and links to the same QR code.

### 6.5 Reconciliation at 30 minutes

20. Revolut `pending` and cancel succeeds: the order becomes `payment_failed` and the reservation is released.
21. Revolut `pending` and cancel fails: the reservation stays `open`, attempts increase, and a retry is scheduled.
22. Revolut `processing` at the deadline: the reservation stays `open` and a retry is scheduled. Later `completed` converts it.
23. Revolut `completed` at the deadline with no webhook yet: the sweeper converts the reservation and marks the order Paid.
24. A Revolut error or unknown state for more than 24 hours: the reservation moves to `review`, still counts as held, and a warning is logged.
25. No Revolut intent: the reservation is released.

### 6.6 Webhooks

26. `ORDER_COMPLETED` delivered twice: one QR code, one conversion.
27. `ORDER_COMPLETED` arrives after the sweeper already converted the reservation: nothing changes.
28. `ORDER_CANCELLED` after `ORDER_COMPLETED`: the reservation stays `converted`.
29. `ORDER_COMPLETED` after a release, with capacity full: the order goes to `review` with reason `qr_capacity_unavailable_after_payment`, and no refund is automatic.

### 6.7 Frontend (Vitest)

30. `createShopOfferCardSetupModule`: `loadForLocation` never opens the dialog, with or without an attached offer.
31. `open()` returns `"noop"` when Guest Cards are not available.
32. The Shop page with the current launch catalog never shows the dialog on load, on Counter Display Add to cart, on Order now, or on Checkout.
33. A 409 `offer_card_offer_required` opens the dialog.
34. A 409 `qr_limit_reached` shows the limit dialog with N and the three buttons.
35. The Capture pause confirm shows the printed-materials warning when `hasShopPrintedMaterials` is true.

## 7. Acceptance

- With the current launch configuration, no card-offer dialog appears anywhere in the Counter Display flow.
- When Admin activates Guest Cards, the purchase flow asks for the card offer, and the paid order produces a Guest Card CSV with the correct QR code and `OfferHeadline`.
- An order that needs a new QR code at the limit cannot be charged.
- A valid reorder that reuses an Active placement proceeds and uses no extra allowance.
- Concurrent checkouts, and Capture together with checkout, cannot exceed capacity.
- A reservation is released only after a confirmed cancellation or failure.

# Shop and Admin Feedback Round 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Apply the client's second round of Shop and Admin feedback (videos of 2026-10-27) on branch `feat/shop-guest-card-offer-qr-capacity` (PR #74).

**Architecture:** Three batches run in parallel in separate worktrees with disjoint files: Batch BE (backend), Batch FE-OP (operator Shop frontend), Batch FE-ADMIN (Admin frontend). The API contracts below are fixed first so the batches do not wait for each other. Typography (W9) runs after the merge.

**Tech Stack:** .NET 10 / EF Core / SQL Server, xUnit; React + TypeScript + Vite, Vitest (node only, `.test.ts`), shadcn/ui.

**Spec:** this plan (user-approved decisions in the conversation of 2026-10-27) plus `docs/superpowers/specs/2026-10-27-shop-guest-card-offer-and-qr-capacity-design.md` for the existing reservation rules.

## Global Constraints

- Decisions (user-approved): **remove Express delivery**; **keep one live product per placement type**; hidden offer description default = `Show this offer to a member of staff on your next visit to redeem it.`; **server confirms payment with Revolut on return**.
- UI rules (AGENTS.md): reuse `src/components/ui` primitives (Button, Badge, DropdownMenu, Dialog, Switch, Input…), no raw `<button>`; Operator design tokens; typography per `docs/agents/typography.md` (*functional* = body/buttons/tables, *headline* = titles); dropdowns inside sheets/drawers/dialogs render above the parent.
- Text colour: body text uses `text-op-text-primary` or the dark gray body token (`--op-color-gray-650` `#616161` or darker); do not use `text-op-text-secondary` (`#969696`) or `--op-color-gray-550` for body copy on the touched screens.
- Money shown to users in pounds `£x.xx`; stored values stay in pence.
- Do not break the existing reservation / payment-safety rules of the previous plan.
- Commit trailer (exact, only this one): `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Backend gate: full suite failing set ⊆ baseline (117 pre-existing). Frontend gate: `npm run typecheck` error count ≤ 218, `npm test` only the pre-existing `assistantWaitGerunds` failure.

## API contracts (fixed; BE implements, FE consumes)

1. `POST /api/shop/orders/{orderId}/confirm-payment?locationId={id}` (Scoped). If the order is `awaiting_payment` and has an open Revolut intent, the server calls Revolut `GetOrderAsync`; if `completed` it applies the paid path through `IRevolutOrderCompletedApplier.ApplyAsync` (idempotent with the webhook). Returns 200 with the operator order detail DTO (same shape as `GET /api/shop/orders/{id}`). Never releases a reservation, never refunds.
2. Express removed: `POST /api/shop/orders` with `deliveryMethod: "express"` → 400 `{ success:false, code:"invalid_delivery_method", message:"deliveryMethod must be standard." }`. Reorder prefill always returns `deliveryMethod: "standard"`.
3. Operator order DTOs (list + detail): `paymentStatusLabel` = `"Awaiting payment"` for `awaiting_payment`; `fulfilmentStatusLabel` = `"Awaiting payment"` when payment is not paid; new `isComplimentary: boolean` and `orderKindLabel: "Complimentary Starter Kit" | null`; `orderDate` formatted in Europe/London time.
4. Admin shop order list item DTO: add `restaurantName: string`, `createdAtUtc: string` (ISO UTC). Admin list default status filter (no `status` param) = all statuses.
5. Admin print asset QR type label source: backend adds `productTitle: string` to each print asset row (the ordered SKU title, e.g. "QR Counter Display") so the drawer never maps `OfferCard` to "QR Guest Cards".
6. `GET /api/admin/shop-interest`: `counts` includes every active interest product, with `count: 0` when none.

---

### Batch BE — backend (model: sonnet)

**Files (expected):** `Services/ShopOrderPlaceService.cs`, `Services/ShopOrderCancelReorderService.cs`, `Controllers/ShopOrdersController.cs`, new `Services/ShopOrderPaymentConfirmService.cs` + interface, `Services/RevolutMerchantClient.cs` + `Interfaces/IRevolutMerchantClient.cs`, `Helpers/ShopMaterialsOrderLineCopy.cs`, `Services/ShopMaterialsOrderPaySessionService.cs`, `Helpers/ShopOrderFulfilmentLabels.cs`, `Helpers/ShopOrderPresentation.cs`, `Helpers/ShopOrderDtoMapper.cs`, `Services/ShopOrdersListService.cs`, DTOs, `Services/AdminShopOrderFulfilmentService.cs`, `DTOs/Admin/AdminShopOrderDtos.cs`, `Services/PrintReadyQrMaterialsService.cs` (print asset row title), `Services/ShopInterestService.cs`, `Program.cs` (DI).

- [ ] BE1 Express removal (contract 2). Tests: place with express → 400 invalid_delivery_method; reorder prefill of an old express order → standard.
- [ ] BE2 Revolut delivery line: name by method `Tummly Shop · Standard delivery`; if the Revolut line-item schema supports `type`, send `service` for the delivery line (check `RevolutMerchantClient` and the Revolut create-order doc `https://developer.revolut.com/docs/api/merchant/operations/create-order.md`). Test: payload line name for standard.
- [ ] BE3 Billing contact email: before creating the Revolut order in `/pay`, if `BillingAccount.BillingEmail` is set and differs from the Revolut customer email, update the Revolut customer email (add `UpdateCustomerAsync` as a default interface method + real implementation; verify the endpoint in `https://developer.revolut.com/docs/api/merchant/operations/update-customer.md`). Failure to update must not block payment (log Warning). Test with the fake merchant.
- [ ] BE4 Confirm-payment endpoint (contract 1). Tests: completed → Paid and reservation converted; pending → unchanged 200; not awaiting → unchanged 200; second call idempotent.
- [ ] BE5 Operator labels and dates (contract 3). Tests for each label and a London-time date at 23:30 UTC in BST.
- [ ] BE6 Admin list DTO + default filter (contract 4). Tests.
- [ ] BE7 Print asset `productTitle` (contract 5). Test: a Counter Display order's asset row has `productTitle = "QR Counter Display"`.
- [ ] BE8 Interest counts with zeros (contract 6). Test.

### Batch FE-OP — operator Shop frontend (model: haiku)

**Files (expected):** `src/components/dashboard/operator/Shop/ShopOfferCardSetupDialog.tsx`, `src/lib/operatorShop/createShopOfferCardSetupModule.ts`, `src/lib/operatorShop/shopOfferCardSetupPresentation.ts`, `ShopProductScreen.tsx`, `ShopCheckoutScreen.tsx`, `ShopPage.tsx`, `src/api/shopOrdersApi.ts`, `ShopOrdersScreen.tsx`, `ShopOrderDetailSidebar.tsx`, `ShopCartDrawer.tsx`, related `.test.ts`.

- [ ] OP1 Offer dialog: show only Offer type, the type's value field(s), and Offer title (max 60). Hide description, validity, expiry, staff instructions; the module fills them with defaults (description = the Global Constraints text; validity = existing default; staff instructions = existing default). Auto title in the Shop dialog = `Get {pct}% off your next visit` / `Get £{amount} off your next order` / `Enjoy a free {item}` / `Replacement {item}`, and it follows the value until the user types in the title (then stays). Do not change the shared `offerCatalogPresentation` behaviour for campaigns — wrap it in the Shop module. Preview headline = current title. Tests (.ts): title follows 20→30→40; stops after manual edit; hidden fields get defaults on confirm; description default text exact.
- [ ] OP2 Product page: remove the "Pack of N recommended" marker and the line "Prices come from the live shop catalog for this product."; body text uses the dark token (Global Constraints); the review/summary section gets the shop surface background used elsewhere (`bg-op-surface-secondary` or the Figma 4405:52884 equivalent). Default selected pack = first pack.
- [ ] OP3 Checkout: remove "Edit address" and the add-address form path; the delivery address card is read-only (location address). Remove the Express option and `EXPRESS_DELIVERY_SURCHARGE_PENCE` use; Standard shows its real price (`£x.xx`, not "Free"). Step 1 button `Continue`; step 2 button `Continue to payment`. Increase vertical spacing between rows to the Figma 4450:55084 rhythm (≥ 12px between label/value rows, 24px between sections), use the dark body token, and remove the extra side padding so the content uses the full pane width up to the existing max width. Cart drawer: remove "Standard Delivery (2-3 business days) Free" copy → real delivery price or "Calculated at checkout".
- [ ] OP4 Payment return: in `ShopPage` on `shopPayOutcome=success`, call `confirm-payment` first, then poll `GET` (keep 12×1.5s) only if still not paid; wrap in try/catch so the screen always leaves "Confirming…". Add `confirmShopOrderPayment` to `src/api/shopOrdersApi.ts`.
- [ ] OP5 Orders screens: show `orderKindLabel` ("Complimentary Starter Kit") as a badge on the list row and the sidebar header; show `paymentStatusLabel` / `fulfilmentStatusLabel` from the API (no local mapping of raw codes); remove Express rendering. Fix any React console errors you can reproduce in the Shop screens with `npm run dev` is not required — instead grep for missing `key` props and controlled/uncontrolled input warnings in the touched files and fix them.

### Batch FE-ADMIN — Admin frontend (model: haiku)

**Files (expected):** `src/components/dashboard/admin/AdminShopOrdersPanel.tsx`, `AdminShopOrderDetailDrawer.tsx`, `AdminShopCatalogPanel.tsx`, `AdminShopInterestPanel.tsx`, `src/api/adminShopOrdersApi.ts`, `src/api/adminShopCatalogApi.ts`, `src/api/adminShopInterestApi.ts`, `src/lib/adminShopOrderFulfilment.ts`, `src/lib/admin/*`, related `.test.ts`.

- [ ] AD1 Orders table: columns Order, Restaurant (name, contract 4), Location, Materials, Total (£), Payment (badge: Paid / Unpaid / Refunded / Free), Order date (`createdAtUtc`, `dd MMM yyyy, HH:mm` Europe/London), Fulfilment, Actions. Remove "Paid" date column and "Next step". Actions = `DropdownMenu` per row: "View order", "Download materials" (enabled only when an asset is Ready and approved), "Approve materials" (when Ready and not approved), "Mark as dispatched" (processing), "Mark as delivered" (in_transit); dispatch/deliver ask confirmation (AlertDialog/Dialog). Default status filter "All"; add a "Refresh" button. Table text `text-sm` with dark body token; no 10px text.
- [ ] AD2 Order drawer: print asset rows show `productTitle` (contract 5) instead of the QR type label map; show unit price and line total in £; remove decorative icons on rows and buttons except Download; padding 24px sections, 12px rows; dark body token.
- [ ] AD3 Materials: availability Switch updates the row optimistically (no full reload, no skeleton; revert + toast on error; do not overwrite an open editor). Pack prices and delivery shown and edited in pounds (`£` input with 2 decimals, converted to pence on save). Space below the panel title (`mb-6`). "Add product": when every placement type already has a product, show the message `Each placement type can have one product. Edit the existing product, or delete it before adding a new one.`; the add panel saves the draft and then shows the image upload in the same panel without closing it.
- [ ] AD4 Interest: list every product from the counts (zeros included, contract 6) with its count; show an error state (not the empty text) when a request fails; add a "Refresh" button.

### Task W9 — Typography (after merge; model: haiku)

Run after the three batches merge. Compare the touched Shop and Admin screens to Figma text styles; add letter-spacing and line-height tokens where missing (`docs/agents/typography.md`). Separate brief at that time.

### Waiting for client inputs (not in this plan)

Product visuals; card border change in Figma; the client's CSV template; the client's notes document.

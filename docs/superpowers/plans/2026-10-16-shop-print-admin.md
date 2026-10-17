# Shop + Print Admin Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Detailed acceptance lives in `.scratch/shop-print-admin/issues/`.

**Goal:** Make the five physical Shop products Admin-configurable (availability, packs/prices/delivery, media/copy) and wire print outputs (A5 PDF + patch/Guest Card CSV) so launch ships with only QR Counter Display buyable and the rest Coming soon.

**Architecture:** Extend the existing DB materials catalog + `AdminShopCatalogPanel` — do not add a second pricebook. Keep Draft/Published/Deleted for editorial lifecycle; add an Available-in-Shop control (lock `01`) so Published-but-OFF SKUs stay listed with Coming soon and fail cart. Packs/delivery follow lock `02`. Print engines already named on `PlacementPrintRoutes`; seed recipes to the correct route and finish CSV generation + approve-before-download.

**Tech Stack:** ASP.NET Core + EF; existing Admin audit; React operator Shop + Admin dashboard; Vitest / xUnit.

**Tracker:** [.scratch/shop-print-admin/map.md](../../../.scratch/shop-print-admin/map.md) · [PRD.md](../../../.scratch/shop-print-admin/PRD.md)

## Global Constraints

- Server-authoritative Shop config; no hard-coded Figma pricebook in the frontend.
- Exactly five core products at launch; Counter Available; other four Coming soon.
- OFF products visible, not orderable.
- QR always Location/placement-scoped; never manually substituted.
- No web generation of AnyLabels `.DLB` / `.DLL`.
- Reuse existing Admin audit; no parallel audit subsystem.
- Report to the human in ASD-STE100 Simplified Technical English.
- Commit only when the human asks.
- Subagents: Cursor Auto / `inherit` only.

---

## File map (primary)

| Area | Files |
|---|---|
| Seed / recipes | `backend/TummlyBackend/Shop/MaterialsCatalog/ShopMaterialsCatalogSeed.cs`, `Models/ShopMaterialCatalog.cs` (`PlacementPrintRoutes`) |
| Catalog read | `DbMaterialsCatalog.cs`, `ShopMaterialCatalogMapper.cs`, Shop DTOs |
| Admin catalog | `AdminShopCatalogService.cs`, `AdminShopCatalogController.cs`, `src/components/dashboard/admin/AdminShopCatalogPanel.tsx`, `src/api/adminShopCatalogApi.ts` |
| Operator Shop | `ShopCatalogItemCard.tsx`, `ShopProductScreen.tsx`, cart/place services |
| Print / fulfilment | Admin shop order print assets module + print-ready services |
| Starter kit | `ComplimentaryStarterShopOrderService.cs` (+ recipe `IncludeInComplimentaryStarter`) |
| Capture mint | `QrCodeProvisioningService.cs`, `StarterQrMaterialTypes.cs`, print-ready starter ensure (ticket `11`) |
| Register interest | new entity/API + Admin list (ticket `08`) |

---

## Phase 0 — Locks (before build)

- [ ] **Resolve `01` Available-in-Shop model** — prefer `AvailableInShop` bool on Published SKUs; document Coming soon badge (fixed vs tag).
- [ ] **Resolve `02` Pack/pricing/delivery shape** — prefer JSON pack rows or child table + per-SKU delivery pence.
- [ ] **Resolve `03` Figma / visuals** — Mohamed supplies full Figma URLs with `node-id`; paste onto map Notes + UI tickets.

Do not claim build tickets that list these as blockers until answered under `## Answer` and map **Decisions so far** updated.

---

## Phase 1 — Catalogue + availability (tickets `04`, `05`)

### Task 1: Seed five products + print-route recipes (`04`)

**Files:** `ShopMaterialsCatalogSeed.cs`; placement recipe migration/seed updates; catalog tests.

- [ ] Add recipes: Counter Display (`EmbeddedPdf`), Table Tent / Talker / Window (`PatchCsv60`), QR Guest Cards (`GuestCardCsv`).
- [ ] Seed five Published SKUs with release copy/specs; retire Offer Card as a sixth core tile.
- [ ] Set launch availability per lock `01` (Counter ON; others OFF).
- [ ] Tests: list length = 5; recipe print routes match table.

### Task 2: Available ON/OFF + Coming soon (`05`)

**Files:** `ShopMaterialSku` + migration; Admin service/panel; `DbMaterialsCatalog` / mapper DTOs; `ShopCatalogItemCard`; cart guards; audit.

- [ ] Persist availability per lock `01`.
- [ ] Admin toggle with audit.
- [ ] Operator list includes Published OFF SKUs with Coming soon badge (Essential chrome).
- [ ] Cart/place reject unavailable SKUs.
- [ ] Tests: OFF visible; add-to-cart fails; ON succeeds.

---

## Phase 2 — Commercial config + starter (`06`, `09`)

### Task 3: Packs / prices / delivery (`06`)

**Files:** model per lock `02`; Admin editor; product screen pack picker; cart/place price lock.

- [ ] Admin CRUD for pack tiers + delivery.
- [ ] Operator selects pack; “From £” = lowest pack.
- [ ] Place order stores pack qty + locked pence.
- [ ] Tests: round-trip Admin; invalid pack rejected.

### Task 4: Starter Kit Counter Display (`09`)

**Files:** `ComplimentaryStarterShopOrderService`; recipe `IncludeInComplimentaryStarter` only on Counter at launch.

- [ ] £0 order creates lines for current starter-kit SKU set (launch: 1 × Counter Display).
- [ ] Ensure idempotent; paid conversion does not duplicate.
- [ ] Tests: one complimentary; second Ensure no-op.

### Task 4b: Capture mint from starter set (`11`, lock `10`)

**Files:** `QrCodeProvisioningService`; `StarterQrMaterialTypes`; `PrintReadyQrMaterialsService.EnsureStarterMaterialsAsync`; Capture labels.

- [ ] Stop hard-coded Tent/Window/OfferCard mint list.
- [ ] Mint one Active QR per Published starter-kit SKU placement key.
- [ ] Signup / first Shop Ensure mints missing types only.
- [ ] Tests: 1 starter → 1 Capture type; 2 starters → 2; Coming soon not minted.

---

## Phase 3 — Print + interest (`07`, `08`)

### Task 5: Print PDF + CSV + approval (`07`)

**Files:** print asset generation; Admin order detail / print-assets module (CSV MIME/filename); approval + audit.

- [ ] Counter → A5 PDF with embedded production QR.
- [ ] Tent/Talker/Window → PatchCsv60 columns including `PrtQty`.
- [ ] Guest Cards → GuestCardCsv + `OfferHeadline`.
- [ ] Preview → approve → download.
- [ ] Integration/manual scan checklist before closeout.

### Task 6: Register interest (`08`)

**Files:** new interest store + APIs; operator CTA; Admin list/counts.

- [ ] Record product, business, Location when available, user, timestamp.
- [ ] Thanks confirmation; never cart/checkout.
- [ ] Admin list + count by product.
- [ ] Blocked on Design names/visuals (`03`) for final cards; API can land earlier with slug placeholders if agreed.

---

## Verification (release acceptance)

- [ ] Five tiles; only Counter buyable.
- [ ] Admin ON/OFF + commercial edit without deploy.
- [ ] Three print routes reconciled on one test order each.
- [ ] Register-interest records only.
- [ ] Starter Kit one £0 Counter; no duplicate.

---

## Execution handoff

Plan and tickets are ready. Recommended path:

1. **Resolve locks `01`–`03`** (especially `01`/`02` so build is not blocked).
2. Then either:
   - **Subagent-Driven** — fresh subagent per build ticket, review between tickets.
   - **Inline Execution** — implement in this session with checkpoints.

Which approach, and which lock answers do you want to lock first (`01` Available flag vs status, `02` packs JSON vs table)?

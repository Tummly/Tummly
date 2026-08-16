# 20 — Create offer draft Action and land

**What to build:** An operator can complete an Offer **Draft interview**, click `Create offer draft`, persist a stored **Offers catalog** Draft, and land on the Offers **Drafts** tab with toast — same close / location / spent-row rules as Campaign.

**Blocked by:** 18 — Offers catalog stored-Draft create path; 19 — Draft interview and Create campaign draft

**Status:** resolved

## Parent

[AI Assistant drafts, progress, and chrome](../PRD.md)

## Figma

- Answer + Actions: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3310-30861&m=dev
- Main Offers page: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3498-1587&m=dev

## Acceptance criteria

- [x] Offer draft ask starts or continues a **Draft interview** locked to **Offers catalog** (same conversation / packing / skip rules as Campaign; Offer field table from PRD / ticket **06**).
- [x] Completing turn: markdown field summary + exactly one `draft-offer` row labelled `Create offer draft`; navigate rows hidden.
- [x] Click uses the stored-Draft create path (not Active POST). Badge **Draft**; not attachable until Active.
- [x] Success: close Assistant; set `?location=` to **Analysis scope**; open Offers; reset list chrome; select **Drafts**; scroll table; toast **Offer draft created.** Does not open Create Offer drawer or **Offer Details**.
- [x] Failure: stay in Assistant; error toast; row re-clickable; not clickable while in flight. Spent row after success.
- [x] Module / service tests cover Offer interview complete, persist, land, and failure re-click.

## Done

Merged to `fix/assistant` as `b333348` (`feat/ai-assistant-20-offer-draft-action`).

- Durable Offers catalog Draft interview, operator-facing summary labels, bare ask-back fills after type lock, and server-owned `draft-offer` Action.
- Persist via `POST /api/offers/draft` (`createCatalogOfferDraft`), clear interview, spent/retry click behaviour, and Offers **Drafts** landing intent (reset chrome, scroll, toast **Offer draft created.**).
- Service, Action catalog, module, navigation, and dashboard `offersIntent` tests.

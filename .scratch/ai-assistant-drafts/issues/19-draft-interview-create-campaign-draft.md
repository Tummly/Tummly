# 19 — Draft interview and Create campaign draft

**What to build:** An operator who asks to draft a Campaign starts a **Draft interview** on the same **Assistant conversation**. When the bar is met, the completing **live answer** shows a markdown field summary and one `Create campaign draft` **Draft Action**. Click persists a **Campaign Draft**, closes the Assistant, sets dashboard location to **Analysis scope**, and lands on Campaigns **Drafts** with toast.

**Blocked by:** 16 — Grounded live-answer markdown subset

**Status:** resolved

## Parent

[AI Assistant drafts, progress, and chrome](../PRD.md)

## Figma

- Answer + Actions: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3310-30861&m=dev
- Campaigns overview / Drafts: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3462-61988&m=dev

## Acceptance criteria

- [x] Refuse-mutate is amended so a Campaign draft ask starts or continues a **Draft interview** instead of a full refusal. Send / schedule / issue / status change / reports / Help Centre stay refused.
- [x] Interview locks one target on the current conversation. Location is always **Analysis scope** (never asked). Missing fields are interview ask-backs, not **Clarify**.
- [x] Ask-back packing and Campaign field rules follow the PRD / ticket **06** tables (silent fills, useful optionals, skip leftover optionals only on “Draft it now”).
- [x] Before the bar is met: missing fields are named; no **Draft Action**; nothing persists.
- [x] Completing turn is **Grounded**: markdown field summary in the body above **Actions**; exactly one `draft-campaign` row labelled `Create campaign draft`; navigate **Action**s hidden. Server attaches the type; model does not invent it.
- [x] Click persists via live Campaign POST (stored draft), closes the Assistant, sets `?location=` to **Analysis scope**, opens Campaigns, resets list chrome, selects **Drafts**, scrolls the table, toast **New draft created.** Does not open the wizard or **Campaign Detail**.
- [x] Persist failure: stay in Assistant; error toast; completing answer and row kept; same row re-clickable. Do not replace with **Failure** + turn **Retry**. Row not clickable while create is in flight.
- [x] After success, that row stays visible, same label, not clickable. Interview state clears. **New chat** drops incomplete interview; **Close** keeps it; **Change Scope** Apply is allowed mid-interview.
- [x] Body never shows raw ids (titles/labels only). Names resolve per PRD (unique substring or candidate ask-back).
- [x] Module and live-answer service tests cover interview start, completing Action, land success, and re-click failure.

## Done

Merged to `fix/assistant` as `08d32b3` (`feat/ai-assistant-19-campaign-draft-interview`).

- Durable Campaign Draft interview state, deterministic ask-backs, completion summary, and server-owned `draft-campaign` Action.
- Persist via `POST /api/campaigns`, clear interview, spent/retry click behaviour, and Campaigns **Drafts** landing intent (reset chrome, scroll, toast **New draft created.**).
- Live-answer service, Action catalog, module, navigation, and dashboard intent tests.
- Follow-up: EF migration Designer pairing (ADR-0015); reset spent Action state when opening another conversation.

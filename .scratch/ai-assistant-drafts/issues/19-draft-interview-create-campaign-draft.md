# 19 — Draft interview and Create campaign draft

**What to build:** An operator who asks to draft a Campaign starts a **Draft interview** on the same **Assistant conversation**. When the bar is met, the completing **live answer** shows a markdown field summary and one `Create campaign draft` **Draft Action**. Click persists a **Campaign Draft**, closes the Assistant, sets dashboard location to **Analysis scope**, and lands on Campaigns **Drafts** with toast.

**Blocked by:** 16 — Grounded live-answer markdown subset

**Status:** ready-for-human

## Done

Implemented on branch `feat/ai-assistant-19-campaign-draft-interview` in worktree `/run/media/salman/D/Freelance/Tummly-worktrees/ai-asst-19-campaign-draft`.

- Added durable Campaign Draft interview state, deterministic ask-backs, completion summary, and server-owned `draft-campaign` Action.
- Added Campaign Draft persist, clear, spent/retry click behavior, and Campaigns Drafts landing intent.
- Added live-answer service, Action catalog, module, navigation, and dashboard intent tests.

## Acceptance criteria

- [x] Refuse-mutate is amended so a Campaign draft ask starts or continues a **Draft interview** instead of a full refusal. Send / schedule / issue / status change / reports / Help Centre stay refused.
- [x] Interview locks one target on the current conversation. Location is always **Analysis scope** (never asked). Missing fields are interview ask-backs, not **Clarify**.
- [x] Ask-back packing and Campaign field rules follow ticket **06**.
- [x] Before the bar is met: missing fields are named; no **Draft Action**; nothing persists.
- [x] Completing turn is **Grounded** with a markdown field summary and exactly one `draft-campaign` row.
- [x] Click persists, closes the Assistant, selects Analysis scope, and lands on Campaigns **Drafts** with reset list chrome, scroll, and toast.
- [x] Persist failure keeps the Assistant and Action available for retry. The row is disabled in flight.
- [x] Success leaves the row spent and clears interview state. New chat drops the active interview; Close keeps it.
- [x] Body uses operator labels and titles, not raw ids.
- [x] Module and live-answer service tests cover interview start, completion, success, failure, and re-click.

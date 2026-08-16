# 22 — Mixed read and draft ask, Clarify block, cancel

**What to build:** One user send that mixes a read and an allowed draft ask yields one **live answer**: ground the retrieve and start or continue the **Draft interview**. **Clarify** blocks interview start. Clear cancel language drops an incomplete interview. Navigate and **Draft Action** never share one turn.

**Blocked by:** 19 — Draft interview and Create campaign draft

**Status:** resolved

## Parent

[AI Assistant drafts, progress, and chrome](../PRD.md)

## Acceptance criteria

- [x] Mixed in-scope retrieve + allowed draft target: one **live answer**; ground retrieve; start or continue **Draft interview**. No second Assistant message. No silent persist.
- [x] Body order: grounded retrieve → interview cluster or completing summary → one refuse sentence per refused out-part.
- [x] Send / schedule / issue / status change / reports / Help Centre / other writes stay refused (full or one refuse sentence).
- [x] Two draft targets on one send: ground retrieve if present; ask which one target; refuse sentence that the Assistant drafts one target per interview. No silent pick.
- [x] Mixed start, bar not met: navigate **Action**s for retrieve only (parent max three). Bar already met: exactly one **Draft Action**; hide navigate. Grounded empty + interview start: no navigate **Action**s.
- [x] Mid-interview retrieve: stay on locked target; ground then continue same interview; navigate may appear for that retrieve.
- [x] Send that needs **Clarify** and also names a draft ask: **Clarify** only (no retrieve, no interview start, no **Action**s).
- [x] Clear cancel language drops the incomplete interview; if the send also has a retrieve, ground it as a normal retrieve turn.
- [x] Live-answer service / module tests cover mixed start, two-target ask, Clarify block, cancel, and Action exclusivity.

## Done

Merged to `fix/assistant` as `850e2ef` (`ticket-22-mixed-read-draft`).

- Mixed retrieve and Draft interview turns now compose one ordered **live answer** for Campaign, Offer, and Feedback recovery targets.
- Two-target choice, **Clarify** blocking, cancel, refused out-parts, and navigate / **Draft Action** exclusivity are enforced.
- Live-answer service and Operator AI Assistant module tests cover the mixed-turn and Action rules.

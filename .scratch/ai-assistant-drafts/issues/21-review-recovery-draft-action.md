# 21 — Review recovery Draft Action

**What to build:** An operator can complete a **Feedback recovery** **Draft interview**, click `Review recovery`, skip Start recovery, close the Assistant, and open the locked intent wizard on Review with interview fields filled (offer attach when needed). Send / Record / Save and exit stay in the wizard.

**Blocked by:** 19 — Draft interview and Create campaign draft

**Status:** resolved

## Parent

[AI Assistant drafts, progress, and chrome](../PRD.md)

## Figma

- Answer + Actions: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=3310-30861&m=dev
- Feedback recovery Review: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=4569-32163&m=dev
- Send Response: https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP--01-Jul--2026-?node-id=4569-32444&m=dev

## Acceptance criteria

- [x] Recovery draft ask starts or continues a **Draft interview** (one in-scope **Feedback** + one of four intents; Review-ready bar; field tables from PRD / ticket **06**).
- [x] Completing turn: one `open-recovery` row labelled `Review recovery` with `feedbackId` + `intent`. Navigate rows hidden. Hide row if slots are bad.
- [x] Click: row not clickable while open in flight; gate Resolved / No contact / offers opt-out; advance **New** → **In progress**; offer intent does attach PUT; close Assistant; set `?location=` to **Analysis scope**; open Feedback with one-shot router state; inject into in-memory map; open Review (no Start recovery, no `POST recovery-draft`, no Create Offer, no success toast).
- [x] Hydrate fields per intent match the PRD / ticket **07** table. Refresh does not reopen the wizard.
- [x] Failure: stay in Assistant; keep completing answer and row; matching error toasts; do not roll back a status or attach that already succeeded.
- [x] Draft Action sessions only: **Back** from Review goes to prior compose; **Back** from first compose is Save and exit (not Start recovery). Manual Start recovery unchanged.
- [x] Module tests cover success hydrate, gate failures, and spent-row behaviour.

## Done

Merged to `fix/assistant` as `0a36bf9` (`feat/ai-assistant-21-review-recovery-draft-action`).

- Durable Feedback recovery Draft interview (target `"recovery"`), Review-ready bar, and server-owned `open-recovery` Action with `feedbackId` + `intent`.
- Click gates, New → In progress, offer attach PUT, clear interview, spent/retry row behaviour, and Feedback one-shot router state to open Review hydrated (no Start recovery / no `POST recovery-draft`).
- Draft Action Back skips Start recovery; manual Start recovery Back unchanged.
- Interview, Action catalog, module, navigation, and recovery wizard tests.

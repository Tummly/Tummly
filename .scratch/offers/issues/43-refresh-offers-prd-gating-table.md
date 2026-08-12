# 43 — Refresh Offers PRD gating table

**What to build:** Offers build PRD first-build gating table and related notes match what already shipped: Edit Save and Pause / Resume / Archive / Duplicate are live (**31** / **32**). Active offers KPI wording matches all stored Active (**16**), not the older “issuable now” line from **14**.

**Blocked by:** None — can start immediately.

**Status:** resolved

## Acceptance

- [x] PRD gating table no longer lists Edit Save or Pause/Resume/Archive/Duplicate as gated-until-API.
- [x] PRD / glossary-facing notes state Active offers KPI = all stored Active (MVP).
- [x] Remaining true gates stay listed: Cancel claim, Resend, navigate-only ⋮, Delete draft, AI live, Export, Template Preview, Void/Redeem stubs until **38**/**39** land.
- [x] No code behaviour change required for this ticket.

## Answer

PRD first-build gating table refreshed: Edit Save (**31**) and Pause / Resume / Archive / Duplicate (**32**) are Ship; Cancel claim, Resend, Delete draft, navigate-only ⋮, AI live, Export, and Template Preview stay gated; Redeem/Void noted as stubs until **38**/**39**. **Active offers** KPI locked as all stored Active (**16**). `CONTEXT.md` Offers Performance already matched — no glossary edit.

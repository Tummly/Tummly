# Launch Cookies + accessibility evidence pack

**Status:** Design approved (chat 2026-09-21); awaiting user review of this file before plan/build  
**Authority:** Tummly Launch Reconciliation — Engineering Build Directive  
**Clears P0:** Cookie/storage inventory + keyboard / mobile / tablet / 200% zoom / screen-reader / contrast / reduced-motion QA evidence

## Problem

- Cookie consent UI is shipped (banner, settings dialog, Cookie Policy, GA gated by consent).
- No engineering **cookie/storage inventory** listing every cookie / `localStorage` / `sessionStorage` key with purpose and consent category.
- No **accessibility QA evidence pack** for the directive checks on public launch surfaces.
- Footer **Accessibility** link remains a placeholder (out of scope for this pack unless Legal asks later).

## Goals

1. Ship a complete cookie/storage inventory under product docs.
2. Ship an a11y QA pack (matrix + findings + how to re-run) for public launch surfaces.
3. Add thin automated axe smoke on cookie banner, Guest Form shell, and Sign-in.
4. Fix only launch-blocking fails found while producing the pack.
5. Link the pack from existing marketing/security product docs.

## Non-goals

- Operator dashboard or Admin a11y packs
- Public Accessibility statement page (footer placeholder stays)
- Rewriting Cookie Policy legal copy
- Full WCAG certification claim
- Fixing all Important/Minor findings in this pack
- UK data residency (separate ops P0)

## Locked decisions

| Decision | Choice |
|----------|--------|
| Deliverable shape | Evidence pack only (docs + thin tests); not a UI redesign |
| Surfaces | Public launch only: marketing + legal, cookie banner/settings, Guest Form, Sign-in / activation |
| Evidence method | Hybrid: automated axe smoke + manual checklist results |
| Doc layout | Two files: inventory + a11y QA (not one combined pack) |
| Cookie Policy | Keep legal copy separate; inventory is engineering evidence |
| Fix bar | Blockers only (keyboard/task blocked, bad focus trap, unlabeled control that blocks use, analytics before consent) |
| Accessibility page | Out of scope |
| Axe package | `vitest-axe` (Vitest + Testing Library) |
| Screen-reader cells | VoiceOver or NVDA short pass when available; if unavailable in the build environment, mark those cells **Pending human verification** (do not invent Pass) and still ship axe + keyboard / zoom / motion / contrast evidence |

## Design

### Deliverable A — Cookie/storage inventory

**File:** `docs/product/cookie-storage-inventory.md`

| Column | Meaning |
|--------|---------|
| Key / name | Exact storage key or cookie name |
| Storage | Cookie / localStorage / sessionStorage |
| Purpose | Why it exists |
| Category | Essential / Analytics (optional) |
| Set by | Code path or third party |
| Consent | Always on / requires analytics accept |
| Cleared when | Sign-out / reject / never / browser session end |

**Inventory rules**

1. Seed from known keys (`tummly-cookie-consent`, `tummly-auth`, device token, selected location, signup session, GA cookies when accepted, UI prefs).
2. Repo-scan `localStorage` / `sessionStorage` / cookie writes; list all keys that can appear on public or shared auth/consent paths.
3. Operator-only prefs found during the scan may be listed under an “Operator-only (out of surface)” subsection — present for completeness, not in-scope for a11y QA.
4. Third-party GA cookies are conditional on analytics accept; document that reject/disable path must not leave analytics active.
5. Do not invent keys; only document what code or known third parties set.

### Deliverable B — Accessibility QA pack

**File:** `docs/product/accessibility-qa.md`

**Surfaces (rows)**

- Marketing homepage
- Legal pages (Privacy / Terms / Cookie Policy as representative)
- Cookie consent banner + Cookie settings dialog
- Guest Form (`/scan/:token`)
- Sign-in / activation

**Checks (columns)**

- Keyboard (Tab / Shift+Tab / Enter / Escape; dialog focus)
- Mobile viewport
- Tablet viewport
- 200% zoom
- Screen-reader notes (VoiceOver or NVDA short pass)
- Contrast (spot-check primary text/controls)
- `prefers-reduced-motion`

Each cell: Pass / Fail / N/A + short note + date.

**Findings section:** severity (Blocker / Important / Minor), surface, description, fix status (`fixed in this pack` / `accepted` / `deferred`).

**Automated smoke subsection:** components/routes covered, command to re-run, last result summary.

### Deliverable C — Automated axe smoke

- Add `vitest-axe` (and `axe-core` if required as peer) to the frontend test deps.
- Three focused mounts (mock network / stores as needed):
  1. `CookieConsentBanner` with choice unset (banner visible)
  2. Guest Form page component with mocked scan token / form props so primary landmarks and contact controls render
  3. Sign-in page shell (primary form landmarks)
- Assert with axe; fail the test on **serious** / **critical** violations for those mounts.
- Moderate / minor → log in Findings; do not auto-expand fix scope.

### Launch-blocking fix bar

Fix in this pack only when:

- User cannot complete the core task by keyboard on a scoped surface
- Focus never lands or is trapped badly in banner/settings/dialog
- Unlabeled control blocks use
- Analytics runs before consent

All other fails → Findings as deferred.

### Product doc links

- `docs/product/marketing-site.md` — Cookie consent section links to inventory + a11y QA; mark evidence shipped when pack lands.
- `docs/product/security-and-rbac.md` — Session/storage table links to inventory.
- `docs/product/analytics.md` — Consent section links to inventory (optional one-liner).
- `docs/product/CHANGELOG.md` — short entry when pack ships.
- `docs/product/README.md` — index entry if that file lists product docs.

## Done when

1. Inventory complete per rules above.
2. A11y matrix filled for all scoped surfaces × checks (screen-reader cells may be **Pending human verification** per locked decision).
3. Automated axe smoke green for the three mounts.
4. No open **Blocker** rows in Findings (fixed or none found).
5. Product docs linked; P0 evidence marked shipped in marketing-site status notes.

## Out of scope (explicit)

- Operator / Admin a11y
- Accessibility statement page
- Cookie Policy legal rewrite
- UK residency
- Broad WCAG conformance claim

## Related

- Directive P0: Cookies/accessibility
- Existing: `cookieConsentStore`, `CookieConsentBanner`, `CookieSettingsDialog`, `GoogleAnalytics`, Cookie Policy page

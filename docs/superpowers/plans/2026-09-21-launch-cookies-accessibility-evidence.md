# Launch Cookies + Accessibility Evidence Pack Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship cookie/storage inventory + public-surface a11y QA evidence (hybrid axe smoke + manual matrix), link product docs, and fix only launch-blocking fails.

**Architecture:** Two product docs (`cookie-storage-inventory.md`, `accessibility-qa.md`). Add Vitest jsdom + Testing Library + `vitest-axe` for three component mounts (`CookieConsentBanner`, `GuestFeedbackForm`, `SignInForm`). Manual matrix filled for scoped public surfaces; screen-reader cells may be Pending human verification.

**Tech Stack:** Markdown product docs, Vitest 4, jsdom, `@testing-library/react`, `vitest-axe` / `axe-core`, existing Zustand cookie consent + GA gate.

**Spec:** [docs/superpowers/specs/2026-09-21-launch-cookies-accessibility-evidence-design.md](../specs/2026-09-21-launch-cookies-accessibility-evidence-design.md)

## Global Constraints

- Evidence pack only — no Operator/Admin a11y pack; no Accessibility statement page; no Cookie Policy legal rewrite.
- Surfaces: marketing + legal, cookie banner/settings, Guest Form, Sign-in / activation.
- Fix only Blockers (keyboard/task blocked, bad focus trap, unlabeled control that blocks use, analytics before consent).
- Axe: fail tests on serious/critical only; moderate/minor → Findings deferred.
- Screen-reader: VoiceOver/NVDA when available; else mark **Pending human verification** (do not invent Pass).
- Do not invent storage keys — only document what code or known third parties set.
- Commit only when the human asks (skip Commit steps unless asked).
- Subagents: Cursor Auto / `inherit` only.
- Report to the human in ASD-STE100 Simplified Technical English.

---

## File map

| File | Role |
|------|------|
| `docs/product/cookie-storage-inventory.md` | Cookie / localStorage / sessionStorage inventory |
| `docs/product/accessibility-qa.md` | Surfaces × checks matrix + Findings + axe re-run notes |
| `docs/product/marketing-site.md` | Link inventory + a11y; mark evidence shipped |
| `docs/product/security-and-rbac.md` | Link inventory from session table |
| `docs/product/analytics.md` | One-liner link to inventory |
| `docs/product/CHANGELOG.md` | Short ship entry |
| `docs/product/README.md` | Index rows for the two new docs |
| `package.json` / lockfile | Add jsdom, Testing Library, vitest-axe, axe-core |
| `vitest.config.ts` | jsdom + include `*.a11y.test.tsx` |
| `vitest.setup.ts` | Extend with jest-dom + vitest-axe matchers for a11y files |
| `src/test/a11y/axeSeriousOnly.ts` | Shared axe assert helper (serious/critical only) |
| `src/components/common/CookieConsentBanner.a11y.test.tsx` | Banner smoke |
| `src/components/guest-feedback/GuestFeedbackForm.a11y.test.tsx` | Guest Form smoke |
| `src/components/auth/SignInForm.a11y.test.tsx` | Sign-in smoke |

---

### Task 1: Cookie/storage inventory doc

**Files:**
- Create: `docs/product/cookie-storage-inventory.md`
- Modify (light): none in this task (links in Task 4)

**Interfaces:**
- Consumes: keys from `cookieConsentStore`, `authStore`, `authHelpers`, `signupSession`, `operatorAppearance`, operator home prefs, GA (`src/lib/analytics.ts`)
- Produces: inventory markdown Compliance can cite

- [ ] **Step 1: Repo-scan storage writes**

Run from repo root:

```bash
rg -n "localStorage\.(setItem|getItem|removeItem)|sessionStorage\.(setItem|getItem|removeItem)|createJSONStorage|document\.cookie|name:\s*[\"']tummly" \
  src --glob '!**/node_modules/**' -g '*.{ts,tsx}' | head -120
```

Expected: hits for `tummly-cookie-consent`, `tummly-auth`, `deviceToken`, `selectedLocationId`, `tummly.signupSession`, `tummly-theme`, sidebar/setup keys, activate-dialog dismiss, legacy `token`/`role`.

- [ ] **Step 2: Write the inventory file**

Create `docs/product/cookie-storage-inventory.md` with this structure (fill every row from the scan; do not invent keys). Use date **2026-09-21** in the status line.

```markdown
# Cookie and storage inventory

**Status:** Shipped (engineering evidence)  
**Last reviewed:** 2026-09-21  
**Authority:** Tummly Launch Reconciliation — Cookies/accessibility P0  
**Related:** [Cookie Policy](./marketing-site.md#cookie-consent), [analytics.md](./analytics.md), [security-and-rbac.md](./security-and-rbac.md)

## Scope

First-party keys set by the Tummly web app, plus Google Analytics cookies when analytics consent is granted. Operator-only prefs are listed for completeness; they are out of the public a11y QA surface.

## First-party storage

| Key / name | Storage | Purpose | Category | Set by | Consent | Cleared when |
|------------|---------|---------|----------|--------|---------|--------------|
| `tummly-cookie-consent` | localStorage | Analytics preference (`analytics`, `updatedAt`) | Essential (preference) | `cookieConsentStore` | Always on (stores choice) | Never automatic; user can change via Cookie settings |
| `tummly-auth` | localStorage | JWT session (Zustand persist) | Essential | `authStore` | Always on | Sign-out / clear session |
| `token` / `role` | localStorage | Legacy auth keys (migrated then removed) | Essential (legacy) | `authStore` migrate | Always on | Removed after migrate / sign-out |
| `deviceToken` | localStorage | Trusted device for Sign-in OTP skip | Essential | `authHelpers` | Always on | Not cleared on sign-out (30-day trust) |
| `selectedLocationId` | localStorage | Last chosen workspace location | Essential | `authHelpers` | Always on | Cleared when session routing clears it |
| `tummly.signupSession` | sessionStorage | Self-service Pilot signup token | Essential | `signupSession.ts` | Always on | Browser session end / explicit clear |
| `activationRequired` | localStorage | Client flag used in session routing tests/flows | Essential | session routing paths | Always on | When gate clears |

<!-- Add every other first-party key found in Step 1 with accurate Purpose/Set by. -->

## Operator-only (out of public a11y surface)

| Key / name | Storage | Purpose | Category | Set by | Consent | Cleared when |
|------------|---------|---------|----------|--------|---------|--------------|
| `tummly-theme` | localStorage | Operator appearance (light/dark/system) | Essential (UI pref) | `operatorAppearance.ts` | Always on | User changes theme |
| `tummly-operator-sidebar-collapsed` | localStorage | Sidebar collapsed pin | Essential (UI pref) | `sidebarCollapsed.ts` | Always on | User toggles |
| `tummly-operator-sidebar-settings-expanded` | localStorage | Settings nav expanded | Essential (UI pref) | `sidebarSettingsExpanded.ts` | Always on | User toggles |
| `tummly-operator-setup-checklist-open` | localStorage | Setup checklist open state | Essential (UI pref) | `setupChecklistOpen.ts` | Always on | User toggles |
| `tummly.operator.activate-tummly-dialog.dismissed` | sessionStorage | Activate Pilot dialog dismiss | Essential (UI pref) | `activateTummlyPilotDialogGate.ts` | Always on | Session end / clear |

<!-- Confirm exact key strings from source constants; fix table if scan differs. -->

## Third-party (conditional)

| Key / name | Storage | Purpose | Category | Set by | Consent | Cleared when |
|------------|---------|---------|----------|--------|---------|--------------|
| `_ga`, `_ga_*`, `_gid` (typical GA) | Cookie | Google Analytics measurement | Analytics (optional) | gtag.js after `setAnalyticsConsent(true)` | Requires analytics accept | Reject / disable sets `ga-disable-{MEASUREMENT_ID}`; cookies may remain until browser expiry — analytics must not run while consent is false |

## Consent gate

- Banner until `cookieConsentStore.analytics` is non-null.
- Accept all → `setAnalyticsConsent(true)` → `initGoogleAnalytics`.
- Reject non-essential / analytics off → `disableGoogleAnalytics` (fail closed: no page views / events).
- Evidence: `src/stores/cookieConsentStore.ts`, `src/lib/analytics.ts`, `src/components/common/GoogleAnalytics.tsx`.

## Review notes

- No advertising cookies at launch.
- Inventory is engineering evidence; Legal Cookie Policy copy stays separate.
```

Replace the HTML comments with real rows. If Step 1 finds extra keys, add them. If a listed operator key string differs from source, use the source constant value.

- [ ] **Step 3: Spot-check consent fail-closed**

Confirm in `src/lib/analytics.ts` that `trackPageView` / `trackEvent` / `initGoogleAnalytics` no-op when consent is false. If analytics can run before consent, that is a **Blocker** — fix in this task (minimal) and note in inventory Review notes. Expected: already gated; no code change.

- [ ] **Step 4: Commit** (skip unless human asks)

```bash
git add docs/product/cookie-storage-inventory.md
git commit -m "$(cat <<'EOF'
docs: add launch cookie and storage inventory

EOF
)"
```

---

### Task 2: Vitest a11y harness + three axe smoke tests

**Files:**
- Modify: `package.json` (and lockfile via npm/pnpm install)
- Modify: `vitest.config.ts`
- Modify: `vitest.setup.ts`
- Create: `src/test/a11y/axeSeriousOnly.ts`
- Create: `src/components/common/CookieConsentBanner.a11y.test.tsx`
- Create: `src/components/guest-feedback/GuestFeedbackForm.a11y.test.tsx`
- Create: `src/components/auth/SignInForm.a11y.test.tsx`

**Interfaces:**
- Consumes: `CookieConsentBanner`, `GuestFeedbackForm`, `SignInForm`, `GUEST_FORM_CONSENT_DEMO`, `signInCredentialsDefaultValues`
- Produces: `assertNoSeriousAxeViolations(container: HTMLElement): Promise<void>`

- [ ] **Step 1: Install test deps**

From repo root (npm — match lockfile already in repo):

```bash
npm install -D jsdom @testing-library/react @testing-library/jest-dom @testing-library/dom vitest-axe axe-core
```

Expected: packages appear under `devDependencies`.

- [ ] **Step 2: Update Vitest config**

Replace `vitest.config.ts` with:

```ts
import path from "path"
import { defineConfig } from "vitest/config"

export default defineConfig({
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  test: {
    environment: "node",
    include: ["src/**/*.test.ts", "src/**/*.a11y.test.tsx"],
    setupFiles: ["./vitest.setup.ts"],
    environmentMatchGlobs: [
      ["src/**/*.a11y.test.tsx", "jsdom"],
    ],
  },
})
```

Existing `*.test.ts` stay on `node`. Only a11y files use jsdom.

- [ ] **Step 3: Extend setup for matchers**

Update `vitest.setup.ts` to keep the existing localStorage mock, and add:

```ts
import { afterEach, expect, vi } from "vitest"
import * as matchers from "vitest-axe/matchers"
import "@testing-library/jest-dom/vitest"

expect.extend(matchers)

// ... keep existing localStorageMock / stubGlobal / afterEach clear ...
```

If `vitest-axe/matchers` import path differs for the installed version, use the package’s documented matcher import — do not invent a second assertion style.

- [ ] **Step 4: Shared serious/critical helper**

Create `src/test/a11y/axeSeriousOnly.ts`:

```ts
import { axe } from "vitest-axe"

/** Fail only on serious/critical — moderate/minor are logged in accessibility-qa Findings. */
export async function assertNoSeriousAxeViolations(
  container: HTMLElement,
): Promise<void> {
  const results = await axe(container)
  const blocking = results.violations.filter(
    (v) => v.impact === "serious" || v.impact === "critical",
  )

  if (blocking.length === 0) {
    return
  }

  const summary = blocking
    .map((v) => `${v.id} (${v.impact}): ${v.help}`)
    .join("\n")
  throw new Error(`Serious/critical axe violations:\n${summary}`)
}
```

- [ ] **Step 5: Write CookieConsentBanner a11y test**

Create `src/components/common/CookieConsentBanner.a11y.test.tsx`:

```tsx
import { beforeEach, describe, expect, it } from "vitest"
import { render, screen } from "@testing-library/react"
import { MemoryRouter } from "react-router-dom"

import { CookieConsentBanner } from "@/components/common/CookieConsentBanner"
import { useCookieConsentStore } from "@/stores/cookieConsentStore"
import { useCookieSettingsUiStore } from "@/stores/cookieSettingsUiStore"
import { assertNoSeriousAxeViolations } from "@/test/a11y/axeSeriousOnly"

describe("CookieConsentBanner a11y", () => {
  beforeEach(() => {
    useCookieConsentStore.setState({
      analytics: null,
      updatedAt: null,
      _hasHydrated: true,
    })
    useCookieSettingsUiStore.setState({ isOpen: false })
  })

  it("has no serious/critical axe violations when visible", async () => {
    const { container } = render(
      <MemoryRouter>
        <CookieConsentBanner />
      </MemoryRouter>,
    )

    expect(screen.getByText("We use cookies")).toBeInTheDocument()
    await assertNoSeriousAxeViolations(container)
  })
})
```

- [ ] **Step 6: Write GuestFeedbackForm a11y test**

Create `src/components/guest-feedback/GuestFeedbackForm.a11y.test.tsx`:

```tsx
import { describe, it } from "vitest"
import { render } from "@testing-library/react"
import { MemoryRouter } from "react-router-dom"

import { GuestFeedbackForm } from "@/components/guest-feedback/GuestFeedbackForm"
import { GUEST_FORM_CONSENT_DEMO } from "@/lib/guestFeedback/guestFormConsentPresentation"
import { assertNoSeriousAxeViolations } from "@/test/a11y/axeSeriousOnly"

describe("GuestFeedbackForm a11y", () => {
  it("has no serious/critical axe violations on shell", async () => {
    const { container } = render(
      <MemoryRouter>
        <GuestFeedbackForm
          token="test-token"
          locationName="Test Location"
          restaurantName="Test Restaurant"
          address="1 High Street"
          guestFormConsent={GUEST_FORM_CONSENT_DEMO}
          isSubmitting={false}
          submitError={null}
          onSubmit={async () => {}}
          onRetry={() => {}}
        />
      </MemoryRouter>,
    )

    await assertNoSeriousAxeViolations(container)
  })
})
```

If framer-motion / mic adapters throw in jsdom, mock the minimum modules at the top of the file (e.g. `vi.mock` mic adapters) — keep mocks thin; do not rewrite the form.

- [ ] **Step 7: Write SignInForm a11y test**

Create `src/components/auth/SignInForm.a11y.test.tsx`:

```tsx
import { describe, it } from "vitest"
import { render } from "@testing-library/react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { MemoryRouter } from "react-router-dom"

import { SignInForm } from "@/components/auth/SignInForm"
import {
  signInCredentialsDefaultValues,
  signInCredentialsSchema,
  type SignInCredentialsValues,
} from "@/schemas/signIn"
import { assertNoSeriousAxeViolations } from "@/test/a11y/axeSeriousOnly"

function Harness() {
  const form = useForm<SignInCredentialsValues>({
    resolver: zodResolver(signInCredentialsSchema),
    defaultValues: signInCredentialsDefaultValues,
  })

  return (
    <SignInForm form={form} onSubmit={async () => {}} />
  )
}

describe("SignInForm a11y", () => {
  it("has no serious/critical axe violations on shell", async () => {
    const { container } = render(
      <MemoryRouter>
        <Harness />
      </MemoryRouter>,
    )

    await assertNoSeriousAxeViolations(container)
  })
})
```

- [ ] **Step 8: Run a11y tests — expect pass or fix Blockers only**

```bash
npm test -- src/components/common/CookieConsentBanner.a11y.test.tsx src/components/guest-feedback/GuestFeedbackForm.a11y.test.tsx src/components/auth/SignInForm.a11y.test.tsx
```

Expected: all three PASS. If serious/critical axe fails:

- Fix only if it meets the Blocker bar in Global Constraints.
- Otherwise soften by documenting in Task 3 Findings as Important/Minor **and** keep the test failing only for serious/critical (do not weaken the helper). If a known false positive is serious but not a real Blocker, record Product Accept in Findings and exclude that rule id in the single test via axe `rules: { [id]: { enabled: false } }` with a one-line comment citing the Findings row — last resort only.

Also confirm existing node tests still run:

```bash
npm test -- src/stores/authStore.test.ts
```

Expected: PASS.

- [ ] **Step 9: Commit** (skip unless human asks)

```bash
git add package.json package-lock.json vitest.config.ts vitest.setup.ts \
  src/test/a11y/axeSeriousOnly.ts \
  src/components/common/CookieConsentBanner.a11y.test.tsx \
  src/components/guest-feedback/GuestFeedbackForm.a11y.test.tsx \
  src/components/auth/SignInForm.a11y.test.tsx
git commit -m "$(cat <<'EOF'
test: add vitest-axe smoke for public launch shells

EOF
)"
```

---

### Task 3: Accessibility QA matrix + Findings

**Files:**
- Create: `docs/product/accessibility-qa.md`

**Interfaces:**
- Consumes: Task 2 axe results + manual checklist on scoped surfaces
- Produces: filled matrix Compliance can cite

- [ ] **Step 1: Create the QA pack skeleton and fill automated subsection**

Create `docs/product/accessibility-qa.md`:

```markdown
# Accessibility QA — public launch surfaces

**Status:** Shipped (engineering evidence pack)  
**Last run:** 2026-09-21  
**Authority:** Tummly Launch Reconciliation — Cookies/accessibility P0  
**Scope:** Marketing + legal, cookie banner/settings, Guest Form, Sign-in / activation  
**Out of scope:** Operator dashboard, Admin, Accessibility statement page

## Automated axe smoke

| Mount | File | Command | Last result |
|-------|------|---------|-------------|
| Cookie consent banner | `src/components/common/CookieConsentBanner.a11y.test.tsx` | `npm test -- src/components/common/CookieConsentBanner.a11y.test.tsx` | Pass (serious/critical) — 2026-09-21 |
| Guest Form shell | `src/components/guest-feedback/GuestFeedbackForm.a11y.test.tsx` | `npm test -- src/components/guest-feedback/GuestFeedbackForm.a11y.test.tsx` | Pass (serious/critical) — 2026-09-21 |
| Sign-in form shell | `src/components/auth/SignInForm.a11y.test.tsx` | `npm test -- src/components/auth/SignInForm.a11y.test.tsx` | Pass (serious/critical) — 2026-09-21 |

Re-run all three:

```bash
npm test -- src/**/*.a11y.test.tsx
```

## Manual matrix

Date: 2026-09-21. Cells: Pass / Fail / N/A / Pending human verification.

| Surface | Keyboard | Mobile | Tablet | 200% zoom | Screen-reader | Contrast | prefers-reduced-motion |
|---------|----------|--------|--------|-----------|---------------|----------|------------------------|
| Marketing homepage | | | | | Pending human verification | | |
| Legal pages (Privacy / Terms / Cookie Policy) | | | | | Pending human verification | | |
| Cookie banner + Cookie settings | | | | | Pending human verification | | |
| Guest Form (`/scan/:token`) | | | | | Pending human verification | | |
| Sign-in / activation | | | | | Pending human verification | | |

### How cells were filled

- Keyboard: Tab / Shift+Tab / Enter / Escape on banner, settings dialog, Guest Form, Sign-in (browser or local).
- Mobile / tablet: DevTools device presets (≈375×667 and ≈768×1024).
- 200% zoom: browser zoom 200% on each surface; check no clipped primary CTA / form fields.
- Contrast: spot-check primary text `#141414` / `#525252` on white and primary buttons.
- `prefers-reduced-motion`: DevTools emulate; Guest Form uses `useReducedMotion` — note Pass if no hard dependency on motion for core submit.
- Screen-reader: mark **Pending human verification** unless VoiceOver/NVDA was actually run.

Fill every non-SR cell with Pass/Fail/N/A + short note inline or in Findings. Do not leave blank.

## Findings

| Severity | Surface | Description | Fix status |
|----------|---------|-------------|------------|
| — | — | None open at pack ship (update if fails found) | — |

Rules:

- **Blocker** → must fix in this pack or pack is not Done.
- **Important / Minor** → `deferred` unless fixed opportunistically.
- Moderate/minor axe from Task 2 → list here as deferred if observed.

## Claims

This pack is **evidence**, not a WCAG conformance certificate.
```

- [ ] **Step 2: Run manual checks and fill the matrix**

For each surface in scope:

1. Open the surface (local `npm run dev` or available env).
2. Keyboard through primary controls; Escape closes Cookie settings.
3. Mobile + tablet viewports; 200% zoom.
4. Emulate `prefers-reduced-motion: reduce`.
5. Spot-check contrast on titles, body, primary buttons.

Update each matrix cell. If a Blocker is found, fix it in the smallest code change and set Findings fix status to `fixed in this pack`. If VoiceOver/NVDA unavailable, leave SR column as **Pending human verification**.

- [ ] **Step 3: Commit** (skip unless human asks)

```bash
git add docs/product/accessibility-qa.md
# plus any Blocker UI fixes if made
git commit -m "$(cat <<'EOF'
docs: add public launch accessibility QA evidence pack

EOF
)"
```

---

### Task 4: Product doc links + index + changelog

**Files:**
- Modify: `docs/product/marketing-site.md` — Cookie consent section + status
- Modify: `docs/product/security-and-rbac.md` — session/storage link
- Modify: `docs/product/analytics.md` — consent one-liner
- Modify: `docs/product/README.md` — document index rows
- Modify: `docs/product/CHANGELOG.md` — short entry

**Interfaces:**
- Consumes: Task 1 + Task 3 file paths
- Produces: discoverable links from existing product docs

- [ ] **Step 1: Update marketing-site cookie section**

In `docs/product/marketing-site.md` under **Cookie consent**, after Behaviour bullets, add:

```markdown
### Evidence (launch P0)

- Cookie/storage inventory: [cookie-storage-inventory.md](./cookie-storage-inventory.md)
- Accessibility QA pack: [accessibility-qa.md](./accessibility-qa.md)
```

In status table / Not yet live if any cookie evidence row exists as Planned, mark inventory + a11y evidence as **Shipped**. Do not claim Accessibility statement page shipped.

- [ ] **Step 2: Update security-and-rbac session table**

In the Client session mechanism table (cookie consent row), add a note or Related line:

```markdown
Full cookie/storage inventory: [cookie-storage-inventory.md](./cookie-storage-inventory.md).
```

- [ ] **Step 3: Update analytics.md**

Under Consent, add:

```markdown
Storage keys and GA cookie category: [cookie-storage-inventory.md](./cookie-storage-inventory.md).
```

- [ ] **Step 4: Update README index**

In `docs/product/README.md` Document index table, add rows:

```markdown
| [cookie-storage-inventory.md](./cookie-storage-inventory.md) | First-party + GA storage keys, consent categories | Launch evidence |
| [accessibility-qa.md](./accessibility-qa.md) | Public launch a11y matrix + axe smoke | Launch evidence |
```

- [ ] **Step 5: CHANGELOG entry**

At top of recent entries in `docs/product/CHANGELOG.md`:

```markdown
- **cookie-storage-inventory.md / accessibility-qa.md:** Launch P0 cookie/storage inventory + public a11y QA evidence pack (axe smoke on banner, Guest Form, Sign-in).
```

- [ ] **Step 6: Smoke docs exist**

```bash
test -f docs/product/cookie-storage-inventory.md && test -f docs/product/accessibility-qa.md && echo OK
npm test -- src/**/*.a11y.test.tsx
```

Expected: `OK` and a11y tests PASS.

- [ ] **Step 7: Commit** (skip unless human asks)

```bash
git add docs/product/marketing-site.md docs/product/security-and-rbac.md \
  docs/product/analytics.md docs/product/README.md docs/product/CHANGELOG.md
git commit -m "$(cat <<'EOF'
docs: link cookie inventory and a11y QA from product docs

EOF
)"
```

---

## Self-review (plan vs spec)

| Spec requirement | Task |
|------------------|------|
| Cookie/storage inventory columns + scan rules | Task 1 |
| A11y matrix + Findings + automated subsection | Task 3 |
| vitest-axe three mounts (banner, Guest Form, Sign-in) | Task 2 |
| Serious/critical fail CI; moderate/minor Findings | Task 2 helper + Task 3 |
| Blocker-only fixes | Global + Task 2/3 |
| SR Pending human verification | Task 3 |
| Product doc links + shipped notes | Task 4 |
| No Accessibility page / no Operator pack / no legal rewrite | Global Constraints |

No TBD placeholders. Exact key strings may be corrected from Step 1 scan — plan requires source constants over invented names.

# Pilot Activate Tummly Dialog Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** After Operator dashboard workspace load, show the Figma “Activate Tummly to publish offers” dialog for Pilot accounts every sign-in (session dismiss), with the hero image ready at open and Operator light/dark tokens on all chrome.

**Architecture:** Pure gate + `sessionStorage` helpers decide eligibility. A thin host in `DashboardContent` preloads the bundled WebP hero, then opens `ActivateTummlyPilotDialog`. Both CTAs dismiss the session flag and navigate to Manage Plan via `operatorDashboardBillingCreditsManagePlanPath`.

**Tech Stack:** React, React Router, Vitest, shadcn `Dialog` / Operator `Button` variants, Vite static asset import, Operator design tokens in `src/index.css`.

**Spec:** [docs/superpowers/specs/2026-09-19-pilot-activate-tummly-dialog-design.md](../specs/2026-09-19-pilot-activate-tummly-dialog-design.md)

## Global Constraints

- Open only when workspace `status === "loaded"` and `subscriptionPlan === "Pilot"`.
- Every sign-in while Pilot; dismiss for this tab session via `sessionStorage` only (not `localStorage`).
- Start and View plans both go to Manage Plan (`operatorDashboardBillingCreditsManagePlanPath`).
- No new backend Pilot-start API.
- Hero must be preloaded / decoded before `open=true` (short timeout fallback).
- Operator semantic tokens for light and dark — no hardcoded dialog surface/text hex.
- Prefer existing UI primitives (`Dialog`, `Button` `op-primary` / `op-tertiary` / `op-collapse`).
- Report to the human in ASD-STE100 Simplified Technical English.
- Commit only when the human asks (skip Commit steps unless asked).
- Subagents: Cursor Auto / `inherit` only (no Opus / GPT model overrides).

---

## File map

| File | Role |
|---|---|
| `src/lib/operatorHome/activateTummlyPilotDialogGate.ts` | Pure should-open + session dismiss key helpers (new) |
| `src/lib/operatorHome/activateTummlyPilotDialogGate.test.ts` | Gate / dismiss unit tests (new) |
| `src/lib/operatorHome/activateTummlyPilotDialogPresentation.ts` | Copy + token class constants (new) |
| `src/lib/operatorHome/activateTummlyPilotDialogPresentation.test.ts` | Assert Operator tokens, no hex (new) |
| `src/lib/operatorHome/preloadImage.ts` | `preloadImage(src, timeoutMs)` helper (new) |
| `src/lib/operatorHome/preloadImage.test.ts` | Preload resolve / timeout tests (new) |
| `src/assets/operator-home/activate-tummly-pilot-hero.webp` | Optimized Figma hero (new) |
| `src/components/dashboard/operator/ActivateTummlyPilotDialog.tsx` | Dialog UI (new) |
| `src/components/dashboard/operator/ActivateTummlyPilotDialogHost.tsx` | Gate + preload + navigate host (new) |
| `src/components/dashboard/operator/Dashboard.tsx` | Mount host inside loaded shell |

---

### Task 1: Gate + session dismiss helpers

**Files:**
- Create: `src/lib/operatorHome/activateTummlyPilotDialogGate.ts`
- Create: `src/lib/operatorHome/activateTummlyPilotDialogGate.test.ts`

**Interfaces:**
- Consumes: none
- Produces:
  - `ACTIVATE_TUMMLY_PILOT_DIALOG_DISMISS_KEY = "tummly.operator.activate-tummly-dialog.dismissed"`
  - `shouldOpenActivateTummlyPilotDialog(input: { status: string; subscriptionPlan: string; isDismissed: boolean }): boolean`
  - `readActivateTummlyPilotDialogDismissed(storage?: Pick<Storage, "getItem">): boolean`
  - `markActivateTummlyPilotDialogDismissed(storage?: Pick<Storage, "setItem">): void`

- [ ] **Step 1: Write the failing test**

```typescript
import { describe, expect, it } from "vitest"

import {
  ACTIVATE_TUMMLY_PILOT_DIALOG_DISMISS_KEY,
  markActivateTummlyPilotDialogDismissed,
  readActivateTummlyPilotDialogDismissed,
  shouldOpenActivateTummlyPilotDialog,
} from "./activateTummlyPilotDialogGate"

describe("shouldOpenActivateTummlyPilotDialog", () => {
  it("opens when loaded Pilot and not dismissed", () => {
    expect(
      shouldOpenActivateTummlyPilotDialog({
        status: "loaded",
        subscriptionPlan: "Pilot",
        isDismissed: false,
      })
    ).toBe(true)
  })

  it("does not open while loading", () => {
    expect(
      shouldOpenActivateTummlyPilotDialog({
        status: "loading",
        subscriptionPlan: "Pilot",
        isDismissed: false,
      })
    ).toBe(false)
  })

  it("does not open for non-Pilot plans", () => {
    expect(
      shouldOpenActivateTummlyPilotDialog({
        status: "loaded",
        subscriptionPlan: "Starter",
        isDismissed: false,
      })
    ).toBe(false)
  })

  it("does not open when dismissed", () => {
    expect(
      shouldOpenActivateTummlyPilotDialog({
        status: "loaded",
        subscriptionPlan: "Pilot",
        isDismissed: true,
      })
    ).toBe(false)
  })
})

describe("activate Tummly dialog session dismiss", () => {
  it("reads and writes the session key", () => {
    const store = new Map<string, string>()
    const storage = {
      getItem: (key: string) => store.get(key) ?? null,
      setItem: (key: string, value: string) => {
        store.set(key, value)
      },
    }

    expect(readActivateTummlyPilotDialogDismissed(storage)).toBe(false)
    markActivateTummlyPilotDialogDismissed(storage)
    expect(store.get(ACTIVATE_TUMMLY_PILOT_DIALOG_DISMISS_KEY)).toBe("1")
    expect(readActivateTummlyPilotDialogDismissed(storage)).toBe(true)
  })
})
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
npm test -- src/lib/operatorHome/activateTummlyPilotDialogGate.test.ts
```

Expected: FAIL (module not found).

- [ ] **Step 3: Write minimal implementation**

```typescript
export const ACTIVATE_TUMMLY_PILOT_DIALOG_DISMISS_KEY =
  "tummly.operator.activate-tummly-dialog.dismissed"

export function shouldOpenActivateTummlyPilotDialog(input: {
  status: string
  subscriptionPlan: string
  isDismissed: boolean
}): boolean {
  if (input.status !== "loaded") {
    return false
  }
  if (input.subscriptionPlan !== "Pilot") {
    return false
  }
  if (input.isDismissed) {
    return false
  }
  return true
}

export function readActivateTummlyPilotDialogDismissed(
  storage: Pick<Storage, "getItem"> = sessionStorage
): boolean {
  return storage.getItem(ACTIVATE_TUMMLY_PILOT_DIALOG_DISMISS_KEY) === "1"
}

export function markActivateTummlyPilotDialogDismissed(
  storage: Pick<Storage, "setItem"> = sessionStorage
): void {
  storage.setItem(ACTIVATE_TUMMLY_PILOT_DIALOG_DISMISS_KEY, "1")
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```bash
npm test -- src/lib/operatorHome/activateTummlyPilotDialogGate.test.ts
```

Expected: PASS.

- [ ] **Step 5: Commit** (only if the human asked)

```bash
git add src/lib/operatorHome/activateTummlyPilotDialogGate.ts src/lib/operatorHome/activateTummlyPilotDialogGate.test.ts
git commit -m "$(cat <<'EOF'
Add Pilot activate-dialog open gate and session dismiss helpers.

EOF
)"
```

---

### Task 2: Presentation tokens + copy

**Files:**
- Create: `src/lib/operatorHome/activateTummlyPilotDialogPresentation.ts`
- Create: `src/lib/operatorHome/activateTummlyPilotDialogPresentation.test.ts`

**Interfaces:**
- Consumes: none
- Produces:
  - `ACTIVATE_TUMMLY_PILOT_DIALOG_COPY` with `title`, `body`, `startCta`, `viewPlansCta`
  - `ACTIVATE_TUMMLY_PILOT_DIALOG_CONTENT_CLASS` — surface + layout classes using Operator tokens
  - `ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_CLASS`
  - `ACTIVATE_TUMMLY_PILOT_DIALOG_BODY_CLASS`
  - `ACTIVATE_TUMMLY_PILOT_DIALOG_HERO_CLASS`

- [ ] **Step 1: Write the failing test**

```typescript
import { describe, expect, it } from "vitest"

import {
  ACTIVATE_TUMMLY_PILOT_DIALOG_BODY_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_CONTENT_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_COPY,
  ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_CLASS,
} from "./activateTummlyPilotDialogPresentation"

describe("Activate Tummly Pilot dialog presentation", () => {
  it("uses Figma copy", () => {
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_COPY.title).toBe(
      "Activate Tummly to publish offers"
    )
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_COPY.startCta).toBe("Start 30-day Pilot")
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_COPY.viewPlansCta).toBe("View plans")
  })

  it("uses Operator light/dark surface and text tokens", () => {
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_CONTENT_CLASS).toContain(
      "bg-op-surface-secondary"
    )
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_CONTENT_CLASS).not.toMatch(/bg-\[#/)
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_CLASS).toContain(
      "text-op-text-primary"
    )
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_BODY_CLASS).toContain(
      "text-op-text-secondary"
    )
  })
})
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
npm test -- src/lib/operatorHome/activateTummlyPilotDialogPresentation.test.ts
```

Expected: FAIL (module not found).

- [ ] **Step 3: Write minimal implementation**

```typescript
export const ACTIVATE_TUMMLY_PILOT_DIALOG_COPY = {
  title: "Activate Tummly to publish offers",
  body: "Start your 30-day Pilot or choose a plan to create live offers and make them available to guests.",
  startCta: "Start 30-day Pilot",
  viewPlansCta: "View plans",
} as const

/** Overrides default DialogContent marketing hex with Operator theme tokens. */
export const ACTIVATE_TUMMLY_PILOT_DIALOG_CONTENT_CLASS =
  "gap-0 overflow-hidden border-0 bg-op-surface-secondary p-0 text-op-text-primary shadow-lg sm:max-w-[560px] dark:bg-op-surface-secondary"

export const ACTIVATE_TUMMLY_PILOT_DIALOG_HEADER_CLASS =
  "flex w-full flex-col gap-7 px-8 pt-8"

export const ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_ROW_CLASS =
  "flex w-full items-start gap-[22px]"

export const ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_CLASS =
  "pr-0 text-2xl font-bold tracking-normal text-op-text-primary"

export const ACTIVATE_TUMMLY_PILOT_DIALOG_BODY_CLASS =
  "max-w-none text-sm font-semibold leading-[19px] text-op-text-secondary"

export const ACTIVATE_TUMMLY_PILOT_DIALOG_ACTIONS_CLASS =
  "flex flex-wrap items-center gap-3"

export const ACTIVATE_TUMMLY_PILOT_DIALOG_HERO_CLASS =
  "relative h-[384px] w-full overflow-hidden"
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```bash
npm test -- src/lib/operatorHome/activateTummlyPilotDialogPresentation.test.ts
```

Expected: PASS.

- [ ] **Step 5: Commit** (only if the human asked)

---

### Task 3: Hero asset + preload helper

**Files:**
- Create: `src/assets/operator-home/activate-tummly-pilot-hero.webp`
- Create: `src/lib/operatorHome/preloadImage.ts`
- Create: `src/lib/operatorHome/preloadImage.test.ts`

**Interfaces:**
- Consumes: none
- Produces: `preloadImage(src: string, timeoutMs?: number): Promise<"ready" | "timeout" | "error">`

- [ ] **Step 1: Download and optimize the Figma hero**

From Figma MCP asset for node `5043:10805` (hero frames — prefer the composite lifestyle shot). Download, then convert to WebP:

```bash
# Example if cwebp is available:
cwebp -q 80 /tmp/activate-tummly-hero.png -o src/assets/operator-home/activate-tummly-pilot-hero.webp
```

If `cwebp` is missing, use `npx sharp-cli` or a one-off Node `sharp` script to write the WebP. Target roughly ≤300KB when possible without obvious quality loss.

Figma asset URLs expire in ~7 days — commit the bytes; do not leave a live Figma URL in source.

- [ ] **Step 2: Write the failing preload test**

```typescript
import { afterEach, describe, expect, it, vi } from "vitest"

import { preloadImage } from "./preloadImage"

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe("preloadImage", () => {
  it("resolves ready when the image loads", async () => {
    vi.stubGlobal(
      "Image",
      class {
        onload: (() => void) | null = null
        onerror: (() => void) | null = null
        decoding = "async"
        set src(_value: string) {
          queueMicrotask(() => this.onload?.())
        }
        decode() {
          return Promise.resolve()
        }
      }
    )

    await expect(preloadImage("/hero.webp", 1000)).resolves.toBe("ready")
  })

  it("resolves timeout when load is slow", async () => {
    vi.useFakeTimers()
    vi.stubGlobal(
      "Image",
      class {
        onload: (() => void) | null = null
        onerror: (() => void) | null = null
        set src(_value: string) {
          /* never load */
        }
        decode() {
          return new Promise(() => {})
        }
      }
    )

    const pending = preloadImage("/hero.webp", 50)
    await vi.advanceTimersByTimeAsync(50)
    await expect(pending).resolves.toBe("timeout")
    vi.useRealTimers()
  })
})
```

- [ ] **Step 3: Run test to verify it fails**

Run:

```bash
npm test -- src/lib/operatorHome/preloadImage.test.ts
```

Expected: FAIL (module not found).

- [ ] **Step 4: Implement preloadImage**

```typescript
export type PreloadImageResult = "ready" | "timeout" | "error"

export function preloadImage(
  src: string,
  timeoutMs = 2500
): Promise<PreloadImageResult> {
  return new Promise((resolve) => {
    let settled = false
    const finish = (result: PreloadImageResult) => {
      if (settled) {
        return
      }
      settled = true
      window.clearTimeout(timer)
      resolve(result)
    }

    const timer = window.setTimeout(() => {
      finish("timeout")
    }, timeoutMs)

    const image = new Image()
    image.decoding = "async"
    image.onload = () => {
      void image
        .decode()
        .catch(() => undefined)
        .finally(() => {
          finish("ready")
        })
    }
    image.onerror = () => {
      finish("error")
    }
    image.src = src
  })
}
```

- [ ] **Step 5: Run test to verify it passes**

Run:

```bash
npm test -- src/lib/operatorHome/preloadImage.test.ts
```

Expected: PASS.

- [ ] **Step 6: Commit** (only if the human asked)

---

### Task 4: Dialog component

**Files:**
- Create: `src/components/dashboard/operator/ActivateTummlyPilotDialog.tsx`

**Interfaces:**
- Consumes: presentation constants from Task 2; hero import from Task 3; `Dialog` / `Button` / Lucide `XIcon`
- Produces: `ActivateTummlyPilotDialog(props: { open: boolean; onOpenChange: (open: boolean) => void; onStartPilot: () => void; onViewPlans: () => void })`

- [ ] **Step 1: Implement the dialog**

Mirror `OperatorDestructiveConfirmDialog` chrome (custom close, Operator tokens), Figma layout (header → actions → full-bleed hero):

```tsx
import { XIcon } from "lucide-react"

import activateTummlyPilotHero from "@/assets/operator-home/activate-tummly-pilot-hero.webp"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  ACTIVATE_TUMMLY_PILOT_DIALOG_ACTIONS_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_BODY_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_CONTENT_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_COPY,
  ACTIVATE_TUMMLY_PILOT_DIALOG_HEADER_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_HERO_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_ROW_CLASS,
} from "@/lib/operatorHome/activateTummlyPilotDialogPresentation"

export type ActivateTummlyPilotDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onStartPilot: () => void
  onViewPlans: () => void
}

export function ActivateTummlyPilotDialog({
  open,
  onOpenChange,
  onStartPilot,
  onViewPlans,
}: ActivateTummlyPilotDialogProps) {
  const copy = ACTIVATE_TUMMLY_PILOT_DIALOG_COPY

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        className={ACTIVATE_TUMMLY_PILOT_DIALOG_CONTENT_CLASS}
      >
        <div className={ACTIVATE_TUMMLY_PILOT_DIALOG_HEADER_CLASS}>
          <div className={ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_ROW_CLASS}>
            <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
              <DialogTitle className={ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_CLASS}>
                {copy.title}
              </DialogTitle>
              <DialogDescription
                className={ACTIVATE_TUMMLY_PILOT_DIALOG_BODY_CLASS}
              >
                {copy.body}
              </DialogDescription>
            </DialogHeader>
            <DialogClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                size="icon"
                aria-label="Close"
                className="shrink-0"
              >
                <XIcon aria-hidden />
              </Button>
            </DialogClose>
          </div>

          <div className={ACTIVATE_TUMMLY_PILOT_DIALOG_ACTIONS_CLASS}>
            <Button type="button" variant="op-primary" onClick={onStartPilot}>
              {copy.startCta}
            </Button>
            <Button type="button" variant="op-tertiary" onClick={onViewPlans}>
              {copy.viewPlansCta}
            </Button>
          </div>
        </div>

        <div className={ACTIVATE_TUMMLY_PILOT_DIALOG_HERO_CLASS}>
          <img
            src={activateTummlyPilotHero}
            alt=""
            width={560}
            height={384}
            className="absolute inset-0 size-full object-cover"
            decoding="async"
          />
        </div>
      </DialogContent>
    </Dialog>
  )
}
```

- [ ] **Step 2: Smoke-check TypeScript on the new file**

Run:

```bash
npx tsc --noEmit -p tsconfig.app.json 2>&1 | head -n 40
```

Expected: no errors that mention `ActivateTummlyPilotDialog` (project may have unrelated noise — fix only this file’s issues).

- [ ] **Step 3: Commit** (only if the human asked)

---

### Task 5: Host + Dashboard mount

**Files:**
- Create: `src/components/dashboard/operator/ActivateTummlyPilotDialogHost.tsx`
- Modify: `src/components/dashboard/operator/Dashboard.tsx` (loaded `DashboardShell` return — mount host as sibling inside the shell children or immediately wrapping `Outlet`)

**Interfaces:**
- Consumes: gate helpers (Task 1), `preloadImage` (Task 3), dialog (Task 4), `operatorDashboardBillingCreditsManagePlanPath`, workspace snapshot fields, `useNavigate`
- Produces: `ActivateTummlyPilotDialogHost(props: { mode: "single" | "multi"; status: string; subscriptionPlan: string; selectedLocationId: number | null; billingCreditsAccess: "none" | "view" | "manage" })`

- [ ] **Step 1: Implement the host**

```tsx
import { useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"

import { ActivateTummlyPilotDialog } from "@/components/dashboard/operator/ActivateTummlyPilotDialog"
import activateTummlyPilotHero from "@/assets/operator-home/activate-tummly-pilot-hero.webp"
import { operatorDashboardBillingCreditsManagePlanPath } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  markActivateTummlyPilotDialogDismissed,
  readActivateTummlyPilotDialogDismissed,
  shouldOpenActivateTummlyPilotDialog,
} from "@/lib/operatorHome/activateTummlyPilotDialogGate"
import { preloadImage } from "@/lib/operatorHome/preloadImage"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"
import type { BillingCreditsAccess } from "@/lib/operatorHome/parseOperatorProfile"

type ActivateTummlyPilotDialogHostProps = {
  mode: OperatorDashboardMode
  status: string
  subscriptionPlan: string
  selectedLocationId: number | null
  billingCreditsAccess: BillingCreditsAccess
}

export function ActivateTummlyPilotDialogHost({
  mode,
  status,
  subscriptionPlan,
  selectedLocationId,
  billingCreditsAccess,
}: ActivateTummlyPilotDialogHostProps) {
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [heroReady, setHeroReady] = useState(false)

  useEffect(() => {
    let cancelled = false
    void preloadImage(activateTummlyPilotHero).then(() => {
      if (!cancelled) {
        setHeroReady(true)
      }
    })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!heroReady) {
      return
    }
    const eligible = shouldOpenActivateTummlyPilotDialog({
      status,
      subscriptionPlan,
      isDismissed: readActivateTummlyPilotDialogDismissed(),
    })
    setOpen(eligible)
  }, [heroReady, status, subscriptionPlan])

  const dismiss = () => {
    markActivateTummlyPilotDialogDismissed()
    setOpen(false)
  }

  const goToManagePlan = () => {
    dismiss()
    if (selectedLocationId == null) {
      return
    }
    if (billingCreditsAccess === "none") {
      return
    }
    navigate(
      operatorDashboardBillingCreditsManagePlanPath(mode, selectedLocationId)
    )
  }

  return (
    <ActivateTummlyPilotDialog
      open={open}
      onOpenChange={(next) => {
        if (!next) {
          dismiss()
          return
        }
        setOpen(true)
      }}
      onStartPilot={goToManagePlan}
      onViewPlans={goToManagePlan}
    />
  )
}
```

- [ ] **Step 2: Mount in `DashboardContent` after load succeeds**

Inside the successful render that returns `<DashboardShell>…</DashboardShell>` (after the loading/error early returns), add the host as a child of `DashboardShell` next to `Outlet`:

```tsx
import { ActivateTummlyPilotDialogHost } from "@/components/dashboard/operator/ActivateTummlyPilotDialogHost"

// … inside DashboardShell children:
<>
  <ActivateTummlyPilotDialogHost
    mode={mode}
    status={workspace.snapshot.status}
    subscriptionPlan={workspace.snapshot.subscriptionPlan}
    selectedLocationId={selectedLocationId}
    billingCreditsAccess={workspace.snapshot.billingCreditsAccess}
  />
  <Outlet context={{ /* existing */ }} />
</>
```

Do **not** mount on the loading spinner branch — that satisfies “after loading is done.”

- [ ] **Step 3: Run related unit tests**

Run:

```bash
npm test -- src/lib/operatorHome/activateTummlyPilotDialogGate.test.ts src/lib/operatorHome/activateTummlyPilotDialogPresentation.test.ts src/lib/operatorHome/preloadImage.test.ts
```

Expected: all PASS.

- [ ] **Step 4: Manual check**

1. Sign in as a Pilot Operator (or use a Pilot seed).
2. Confirm the full-page dashboard loader finishes, then the dialog appears with hero already visible (no empty hero flash).
3. Toggle light / dark — surface, title, body, close chip, and buttons follow tokens.
4. Close with X — dialog stays closed on route change in the same tab; new sign-in / new session shows it again while Pilot.
5. Start / View plans — lands on Manage Plan.

- [ ] **Step 5: Commit** (only if the human asked)

---

## Spec coverage checklist

| Spec requirement | Task |
|---|---|
| Open after workspace loaded + Pilot | 1, 5 |
| Every sign-in / session dismiss | 1, 5 |
| Figma copy + layout | 2, 4 |
| Operator light/dark tokens | 2, 4 |
| Preload hero before open | 3, 5 |
| Start / View plans → Manage Plan | 5 |
| Mount in Dashboard shell path | 5 |
| No new backend API | (none) |

## Plan self-review

- Placeholders: none.
- Types: `shouldOpenActivateTummlyPilotDialog`, dismiss helpers, `preloadImage`, dialog props, and host props are consistent across tasks.
- Image path: `src/assets/operator-home/activate-tummly-pilot-hero.webp` is the single import site.
- `billingCreditsAccess === "none"` still dismisses but skips navigate (per spec).

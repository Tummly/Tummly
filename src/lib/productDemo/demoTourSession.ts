/** Client flag: product demo tour chrome + forced light theme on the real dashboard. */

import { PRODUCT_DEMO } from "@/content/marketing/productDemoPage"

export const DEMO_TOUR_SESSION_KEY = "tummly-product-demo-tour"
export const DEMO_GUEST_SUBMISSION_KEY = "tummly-product-demo-guest-submission"
export const DEMO_GUEST_SCAN_PATH_KEY = "tummly-product-demo-guest-scan-path"
export const DEMO_TOUR_CHANGE_EVENT = "tummly-demo-tour-change"

export type DemoTourPhase = "entry" | "welcome" | "tour" | "exit"

export type DemoTourEntryPath = "guest" | "skip"

export type DemoTourSessionState = {
  chapterIndex: number
  stepIndex: number
  completedChapterIds: string[]
  exploreOpen: boolean
  /** When true, hide coachmarks — user is browsing the dashboard freely. */
  freeExplore: boolean
  phase: DemoTourPhase
  entryPath: DemoTourEntryPath
  hasGuestSubmission: boolean
}

export type ProductDemoGuestSubmission = {
  commentPreview: string
  submittedAt: string
}

export function isDemoTourActive(): boolean {
  if (typeof sessionStorage === "undefined") {
    return false
  }
  return sessionStorage.getItem(DEMO_TOUR_SESSION_KEY) != null
}

function notifyDemoTourChange(): void {
  if (typeof window === "undefined") {
    return
  }
  window.dispatchEvent(new Event(DEMO_TOUR_CHANGE_EVENT))
}

export function readDemoTourSession(): DemoTourSessionState | null {
  if (typeof sessionStorage === "undefined") {
    return null
  }
  const raw = sessionStorage.getItem(DEMO_TOUR_SESSION_KEY)
  if (!raw) {
    return null
  }
  try {
    const parsed = JSON.parse(raw) as Partial<DemoTourSessionState> & {
      /** Legacy 4-tip tour shape */
      stepIndex?: number
      phase?: string
    }

    const phase =
      parsed.phase === "entry" ||
      parsed.phase === "welcome" ||
      parsed.phase === "tour" ||
      parsed.phase === "exit"
        ? parsed.phase
        : null
    if (phase == null) {
      return null
    }

    // Migrate legacy { stepIndex, exploreOpen, phase: tour|exit }
    const legacyOnly =
      parsed.chapterIndex == null &&
      typeof parsed.stepIndex === "number" &&
      (parsed as { exploreOpen?: boolean }).exploreOpen != null

    if (legacyOnly) {
      const legacyStep = Math.max(0, parsed.stepIndex ?? 0)
      return {
        chapterIndex: Math.min(
          legacyStep,
          PRODUCT_DEMO.tour.chapters.length - 1
        ),
        stepIndex: 0,
        completedChapterIds: [],
        exploreOpen: Boolean(
          (parsed as { exploreOpen?: boolean }).exploreOpen
        ),
        freeExplore: false,
        phase: phase === "exit" ? "exit" : "tour",
        entryPath: "skip",
        hasGuestSubmission: false,
      }
    }

    return {
      chapterIndex:
        typeof parsed.chapterIndex === "number" && parsed.chapterIndex >= 0
          ? parsed.chapterIndex
          : 0,
      stepIndex:
        typeof parsed.stepIndex === "number" && parsed.stepIndex >= 0
          ? parsed.stepIndex
          : 0,
      completedChapterIds: Array.isArray(parsed.completedChapterIds)
        ? parsed.completedChapterIds.filter(
            (id): id is string => typeof id === "string"
          )
        : [],
      exploreOpen: Boolean(parsed.exploreOpen),
      freeExplore: Boolean(parsed.freeExplore),
      phase,
      entryPath: parsed.entryPath === "guest" ? "guest" : "skip",
      hasGuestSubmission: Boolean(parsed.hasGuestSubmission),
    }
  } catch {
    return null
  }
}

export function writeDemoTourSession(state: DemoTourSessionState): void {
  if (typeof sessionStorage === "undefined") {
    return
  }
  sessionStorage.setItem(DEMO_TOUR_SESSION_KEY, JSON.stringify(state))
  notifyDemoTourChange()
}

export function startDemoTourSession(options?: {
  entryPath?: DemoTourEntryPath
  hasGuestSubmission?: boolean
  phase?: DemoTourPhase
}): void {
  writeDemoTourSession({
    chapterIndex: 0,
    stepIndex: 0,
    completedChapterIds: [],
    exploreOpen: false,
    freeExplore: false,
    phase: options?.phase ?? "welcome",
    entryPath: options?.entryPath ?? "skip",
    hasGuestSubmission: Boolean(options?.hasGuestSubmission),
  })
}

export function clearDemoTourSession(): void {
  if (typeof sessionStorage === "undefined") {
    return
  }
  sessionStorage.removeItem(DEMO_TOUR_SESSION_KEY)
  sessionStorage.removeItem(DEMO_GUEST_SCAN_PATH_KEY)
  notifyDemoTourChange()
}

export function readGuestSubmission(): ProductDemoGuestSubmission | null {
  if (typeof sessionStorage === "undefined") {
    return null
  }
  const raw = sessionStorage.getItem(DEMO_GUEST_SUBMISSION_KEY)
  if (!raw) return null
  try {
    const parsed = JSON.parse(raw) as Partial<ProductDemoGuestSubmission>
    if (
      typeof parsed.commentPreview !== "string" ||
      typeof parsed.submittedAt !== "string"
    ) {
      return null
    }
    return {
      commentPreview: parsed.commentPreview,
      submittedAt: parsed.submittedAt,
    }
  } catch {
    return null
  }
}

export function writeGuestSubmission(submission: ProductDemoGuestSubmission): void {
  if (typeof sessionStorage === "undefined") {
    return
  }
  sessionStorage.setItem(DEMO_GUEST_SUBMISSION_KEY, JSON.stringify(submission))
}

export function clearGuestSubmission(): void {
  if (typeof sessionStorage === "undefined") {
    return
  }
  sessionStorage.removeItem(DEMO_GUEST_SUBMISSION_KEY)
}

export function writeDemoGuestScanPath(scanPath: string): void {
  if (typeof sessionStorage === "undefined") {
    return
  }
  sessionStorage.setItem(DEMO_GUEST_SCAN_PATH_KEY, scanPath)
}

export function readDemoGuestScanPath(): string | null {
  if (typeof sessionStorage === "undefined") {
    return null
  }
  const value = sessionStorage.getItem(DEMO_GUEST_SCAN_PATH_KEY)
  return value && value.trim() ? value : null
}

export function clearDemoGuestScanPath(): void {
  if (typeof sessionStorage === "undefined") {
    return
  }
  sessionStorage.removeItem(DEMO_GUEST_SCAN_PATH_KEY)
}

export function currentTourStep(state: DemoTourSessionState) {
  const chapter = PRODUCT_DEMO.tour.chapters[state.chapterIndex]
  const step = chapter?.steps[state.stepIndex]
  return { chapter, step }
}

/** Chapter 4 example question (§11 step 3). */
export const DEMO_TOUR_AI_PROMPT =
  "Summarise feedback from the last 30 days and tell me what needs attention."

/** Custom event for Operator modules to open drawers/panels for tour actions. */
export const DEMO_TOUR_ACTION_EVENT = "tummly-demo-tour-action"

export type DemoTourActionDetail = {
  action: string
  target?: string
}

export function dispatchDemoTourAction(detail: DemoTourActionDetail): void {
  if (typeof window === "undefined") {
    return
  }
  window.dispatchEvent(
    new CustomEvent(DEMO_TOUR_ACTION_EVENT, { detail })
  )
}

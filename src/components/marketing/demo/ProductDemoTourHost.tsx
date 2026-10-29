import { useEffect, useMemo, useRef, useState } from "react"
import { createPortal } from "react-dom"
import { Link, useNavigate } from "react-router-dom"
import { toast } from "sonner"

import { fetchDemoGuestEntry } from "@/api/demoSessionApi"
import { DemoEntryPanel } from "@/components/marketing/demo/DemoEntryPanel"
import { DemoExitModal } from "@/components/marketing/demo/DemoExitModal"
import { DemoTourChrome } from "@/components/marketing/demo/DemoTourChrome"
import { DemoWelcomeModal } from "@/components/marketing/demo/DemoWelcomeModal"
import { SpotlightCoachmark } from "@/components/marketing/demo/SpotlightCoachmark"
import { PRODUCT_DEMO } from "@/content/marketing/productDemoPage"
import { isProductDemoEnabled } from "@/lib/env"
import {
  clearDemoTourSession,
  clearGuestSubmission,
  currentTourStep,
  DEMO_TOUR_CHANGE_EVENT,
  dispatchDemoTourAction,
  isDemoTourActive,
  readDemoGuestScanPath,
  readDemoTourSession,
  writeDemoGuestScanPath,
  writeDemoTourSession,
  type DemoTourSessionState,
} from "@/lib/productDemo/demoTourSession"
import {
  DEMO_CHROME_ATTRIBUTE,
  installDemoInteractionLock,
} from "@/lib/productDemo/demoInteractionLock"
import {
  productDemoPath,
  productDemoPathForChapter,
} from "@/lib/productDemo/productDemoPaths"
import { clearAuthSession } from "@/pages/utils/authHelpers"
import { cn } from "@/lib/utils"

function leaveDemo(to = "/") {
  clearDemoTourSession()
  clearAuthSession()
  window.location.assign(to)
}

type TourStep = (typeof PRODUCT_DEMO.tour.chapters)[number]["steps"][number]

const REWIND_POLL_MS = 100
const REWIND_TARGET_TIMEOUT_MS = 8000

function nextFrame(): Promise<void> {
  return new Promise((resolve) =>
    window.requestAnimationFrame(() => window.requestAnimationFrame(() => resolve()))
  )
}

/** Resolves true when the tour target is in the DOM, false on timeout. */
async function waitForTourTarget(
  target: string,
  isCurrent: () => boolean
): Promise<boolean> {
  const deadline = Date.now() + REWIND_TARGET_TIMEOUT_MS
  while (isCurrent() && Date.now() < deadline) {
    if (document.querySelector(`[data-demo-tour="${target}"]`) != null) {
      return true
    }
    await new Promise((resolve) => window.setTimeout(resolve, REWIND_POLL_MS))
  }
  return false
}

/**
 * Tour chrome over the real Operator dashboard when a product-demo session is active.
 */
export function ProductDemoTourHost() {
  const navigate = useNavigate()
  // Gated like /product-demo: no tour chrome and no demo API call while off.
  const demoEnabled = isProductDemoEnabled()
  const [state, setState] = useState<DemoTourSessionState | null>(() =>
    readDemoTourSession()
  )
  const [scanPath, setScanPath] = useState<string | null>(() =>
    readDemoGuestScanPath()
  )
  const [loadingEntry, setLoadingEntry] = useState(false)
  const [entryError, setEntryError] = useState<string | null>(null)
  /** True while Back rebuilds the screen of an earlier step. */
  const [rewinding, setRewinding] = useState(false)
  const rewindRunRef = useRef(0)

  useEffect(() => {
    const sync = () => setState(readDemoTourSession())
    window.addEventListener(DEMO_TOUR_CHANGE_EVENT, sync)
    window.addEventListener("storage", sync)
    return () => {
      window.removeEventListener(DEMO_TOUR_CHANGE_EVENT, sync)
      window.removeEventListener("storage", sync)
    }
  }, [])

  useEffect(() => {
    if (!isDemoTourActive()) return
    // Clear any live-site toasts that opened before demo chrome mounted.
    toast.dismiss()
  }, [])

  const showEntry = state?.phase === "entry"
  const demoActive = state != null && demoEnabled

  // The demo is a guided tour: only the tour chrome takes visitor input.
  useEffect(() => {
    if (!demoActive) return
    return installDemoInteractionLock()
  }, [demoActive])

  useEffect(() => {
    if (!showEntry || !demoEnabled) return
    const existing = readDemoGuestScanPath()
    if (existing) {
      setScanPath(existing)
      return
    }
    let cancelled = false
    setLoadingEntry(true)
    void fetchDemoGuestEntry()
      .then((entry) => {
        if (cancelled) return
        writeDemoGuestScanPath(entry.scanPath)
        setScanPath(entry.scanPath)
        setEntryError(null)
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setEntryError(
          err instanceof Error
            ? err.message
            : "Unable to load the product demo guest entry."
        )
      })
      .finally(() => {
        if (!cancelled) setLoadingEntry(false)
      })
    return () => {
      cancelled = true
    }
  }, [showEntry, demoEnabled])

  const scanAbsoluteUrl = useMemo(() => {
    if (!scanPath) return null
    if (typeof window === "undefined") return scanPath
    return new URL(scanPath, window.location.origin).toString()
  }, [scanPath])

  if (!demoEnabled || state == null || !isDemoTourActive()) {
    return null
  }

  const persist = (next: DemoTourSessionState) => {
    writeDemoTourSession(next)
    setState(next)
  }

  const { chapter, step } = currentTourStep(state)
  const showWelcome = state.phase === "welcome"
  const showExit = state.phase === "exit"
  const showCoachmark =
    !rewinding &&
    state.phase === "tour" &&
    !state.exploreOpen &&
    chapter != null &&
    step != null
  const showFab = state.phase === "tour" || state.phase === "exit"

  const goChapterStep = (chapterIndex: number, stepIndex: number) => {
    const nextChapter = PRODUCT_DEMO.tour.chapters[chapterIndex]
    if (nextChapter == null) return
    const clampedStep = Math.max(
      0,
      Math.min(stepIndex, nextChapter.steps.length - 1)
    )
    rewindRunRef.current += 1
    setRewinding(false)
    persist({
      ...state,
      chapterIndex,
      stepIndex: clampedStep,
      exploreOpen: false,
      phase: "tour",
    })
    // Drawers from an earlier step cannot be rebuilt, so a chapter always
    // restarts from its first screen.
    dispatchDemoTourAction({ action: "resetTourUi" })
    navigate(productDemoPathForChapter(nextChapter.id))
  }

  const runStepAction = (tourStep: TourStep) => {
    if (tourStep.action === "navigateFeedback") {
      navigate(productDemoPath("feedback"))
    } else if (tourStep.action === "navigateCampaigns") {
      navigate(productDemoPath("campaigns"))
    } else if (tourStep.action === "openWeeklyBrief") {
      // Opens the brief page whether or not this week's brief exists yet.
      navigate(`${productDemoPath("reports")}/weekly-brief`)
    } else {
      dispatchDemoTourAction({ action: tourStep.action, target: tourStep.target })
    }
  }

  /**
   * Back: the first step of a chapter returns to the Tour guide. Later steps
   * go to the step before. When that step's CTA changed the screen (route,
   * drawer), the chapter is rebuilt from its first screen and the earlier
   * step actions run again, each when its target is on screen.
   */
  const goBack = () => {
    if (chapter == null) return
    const previousIndex = state.stepIndex - 1
    if (previousIndex < 0) {
      persist({ ...state, exploreOpen: true })
      return
    }
    const previousStep = chapter.steps[previousIndex]
    if (previousStep == null) return
    persist({ ...state, stepIndex: previousIndex })
    if (previousStep.action === "none") return

    const run = ++rewindRunRef.current
    const isCurrent = () => rewindRunRef.current === run
    setRewinding(true)
    void (async () => {
      dispatchDemoTourAction({ action: "resetTourUi" })
      // Leave the page first so route-local drawers close.
      navigate(productDemoPath("home"))
      await nextFrame()
      const chapterPath = productDemoPathForChapter(chapter.id)
      if (chapterPath !== productDemoPath("home")) {
        navigate(chapterPath)
      }
      for (const replayStep of chapter.steps.slice(0, previousIndex)) {
        const found = await waitForTourTarget(replayStep.target, isCurrent)
        if (!isCurrent()) return
        if (!found) break
        runStepAction(replayStep)
      }
      if (isCurrent()) setRewinding(false)
    })()
  }

  const advanceFromCta = () => {
    if (chapter == null || step == null) return

    // Each step CTA does its action once, then the tour moves on.
    runStepAction(step)

    if (step.action === "completeTour") {
      // Keep the AI panel open: closing it cancels a running answer, and the
      // conversion modal covers it anyway.
      persist({
        ...state,
        completedChapterIds: PRODUCT_DEMO.tour.chapters.map((c) => c.id),
        exploreOpen: false,
        phase: "exit",
      })
      return
    }

    const nextStepIndex = state.stepIndex + 1
    if (
      step.action !== "completeChapter" &&
      nextStepIndex < chapter.steps.length
    ) {
      persist({ ...state, stepIndex: nextStepIndex })
      return
    }

    // Chapter done: return to the Tour Guide with progress (§8 step 6).
    const completed = new Set(state.completedChapterIds)
    completed.add(chapter.id)
    const nextChapterIndex = PRODUCT_DEMO.tour.chapters.findIndex(
      (c) => !completed.has(c.id)
    )
    dispatchDemoTourAction({ action: "resetTourUi" })
    persist({
      ...state,
      completedChapterIds: [...completed],
      chapterIndex:
        nextChapterIndex >= 0 ? nextChapterIndex : state.chapterIndex,
      stepIndex: 0,
      exploreOpen: true,
      phase: "tour",
    })
    if (nextChapterIndex >= 0) {
      const nextChapter = PRODUCT_DEMO.tour.chapters[nextChapterIndex]
      if (nextChapter) {
        navigate(productDemoPathForChapter(nextChapter.id))
      }
    }
  }

  const overlay = (
    <div
      className="pointer-events-none fixed inset-0 z-[300]"
      {...{ [DEMO_CHROME_ATTRIBUTE]: "" }}
    >
      {/* Full-page glass only for entry / welcome / exit — not for Tour Guide explore. */}
      {(showEntry || showWelcome || showExit) && (
        <div
          className={cn(
            "pointer-events-none absolute inset-0 transition-[backdrop-filter] duration-300",
            showEntry
              ? "bg-[rgba(20,20,20,0.1)] backdrop-blur-[2px]"
              : "bg-[rgba(20,20,20,0.22)] backdrop-blur-[8px]"
          )}
          aria-hidden
        />
      )}

      <div className="pointer-events-none absolute inset-0">
        {showEntry ? (
          <div className="pointer-events-auto flex h-full overflow-y-auto p-4 sm:p-6">
            <div className="m-auto flex w-full max-w-[648px] flex-col items-center">
              <DemoEntryPanel
                scanAbsoluteUrl={scanAbsoluteUrl}
                loading={loadingEntry}
                error={entryError}
                unavailable={false}
                starting={false}
                onOpenGuestForm={() => {
                  if (!scanPath) return
                  clearGuestSubmission()
                  persist({
                    ...state,
                    entryPath: "guest",
                    phase: "entry",
                  })
                  window.location.assign(scanPath)
                }}
                onRestaurantSkip={() => {
                  clearGuestSubmission()
                  persist({
                    ...state,
                    entryPath: "skip",
                    hasGuestSubmission: false,
                    phase: "welcome",
                  })
                  navigate(productDemoPath("feedback"), { replace: true })
                }}
                onRestart={() => {
                  setLoadingEntry(true)
                  setEntryError(null)
                  void fetchDemoGuestEntry()
                    .then((entry) => {
                      writeDemoGuestScanPath(entry.scanPath)
                      setScanPath(entry.scanPath)
                    })
                    .catch((err: unknown) => {
                      setEntryError(
                        err instanceof Error
                          ? err.message
                          : "Unable to load the product demo guest entry."
                      )
                    })
                    .finally(() => setLoadingEntry(false))
                }}
              />
            </div>
          </div>
        ) : null}

        {showWelcome ? (
          <div className="pointer-events-auto flex h-full items-center justify-center p-4 sm:p-6">
            <DemoWelcomeModal
              hasGuestSubmission={state.hasGuestSubmission}
              onStartTour={() => {
                goChapterStep(0, 0)
              }}
            />
          </div>
        ) : null}

        {state.phase === "tour" ? (
          <div className="pointer-events-none relative h-full w-full">
            <DemoTourChrome
              chapterIndex={state.chapterIndex}
              completedChapterIds={state.completedChapterIds}
              exploreOpen={state.exploreOpen}
              // During a coachmark, Back on its first step opens the Tour
              // guide; the floating button would cover drawers (AI composer).
              showFab={showFab && !showExit && !showCoachmark}
              onToggleExplore={() =>
                persist({
                  ...state,
                  exploreOpen: !state.exploreOpen,
                })
              }
              onSelectChapter={(index) => goChapterStep(index, 0)}
              onContinue={() => {
                if (state.stepIndex === 0) {
                  // Start of a chapter: go to its first screen.
                  goChapterStep(state.chapterIndex, 0)
                  return
                }
                // Mid-chapter: the step's screen is still open; resume it.
                persist({ ...state, exploreOpen: false })
              }}
            />
          </div>
        ) : null}

        {showExit ? (
          <div className="pointer-events-auto flex h-full items-center justify-center p-4 sm:p-6">
            <DemoExitModal onLeaveDemo={(to) => leaveDemo(to)} />
          </div>
        ) : null}
      </div>

      {showCoachmark && step && chapter ? (
        <SpotlightCoachmark
          target={step.target}
          stepLabel={`${state.stepIndex + 1} of ${chapter.steps.length}`}
          title={step.title}
          body={step.body}
          ctaLabel={step.ctaLabel}
          onCta={advanceFromCta}
          onBack={goBack}
        />
      ) : null}
    </div>
  )

  // Own layer above the overlay and the coachmark spotlight (z-310), so the
  // visitor can always leave the demo.
  const exitLayer = (
    <div
      className="pointer-events-none fixed inset-0 z-[320]"
      {...{ [DEMO_CHROME_ATTRIBUTE]: "" }}
    >
      <Link
        to="/"
        onClick={(event) => {
          event.preventDefault()
          leaveDemo("/")
        }}
        className={cn(
          "font-sans pointer-events-auto absolute inline-flex min-h-11 items-center rounded-full bg-white px-4 text-sm font-medium text-[#141414] shadow-[0_4px_16px_rgba(0,0,0,0.12)] hover:bg-[#f3f3f3]",
          state.phase === "tour"
            ? // Bottom left, beside the sidebar: the top bar holds the account menu.
              "bottom-4 left-[68px] sm:bottom-8"
            : "top-4 right-4"
        )}
      >
        Exit demo
      </Link>
    </div>
  )

  if (typeof document === "undefined") {
    return null
  }

  return createPortal(
    <>
      {overlay}
      {exitLayer}
    </>,
    document.body
  )
}

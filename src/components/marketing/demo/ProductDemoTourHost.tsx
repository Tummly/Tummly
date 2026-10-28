import { useEffect, useMemo, useState } from "react"
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

  const advanceFromCta = () => {
    if (chapter == null || step == null) return

    // Each step CTA does its action once, then the tour moves on.
    if (step.action === "navigateFeedback") {
      navigate(productDemoPath("feedback"))
    } else if (step.action === "navigateCampaigns") {
      navigate(productDemoPath("campaigns"))
    } else if (step.action === "openWeeklyBrief") {
      // Opens the brief page whether or not this week's brief exists yet.
      navigate(`${productDemoPath("reports")}/weekly-brief`)
    } else {
      dispatchDemoTourAction({ action: step.action, target: step.target })
    }

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
          <div className="pointer-events-auto flex h-full items-center justify-center p-4 sm:p-6">
            <div className="flex w-full max-w-[480px] flex-col items-center">
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
            <Link
              to="/"
              onClick={(event) => {
                event.preventDefault()
                leaveDemo("/")
              }}
              className="font-sans absolute top-4 right-4 z-40 text-sm font-medium text-[#141414]/90 underline-offset-2 hover:text-[#141414] hover:underline"
            >
              Exit demo
            </Link>
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
            <Link
              to="/"
              onClick={(event) => {
                event.preventDefault()
                leaveDemo("/")
              }}
              className="font-sans absolute top-4 right-4 z-40 text-sm font-medium text-white/90 underline-offset-2 hover:text-white hover:underline"
            >
              Exit demo
            </Link>
          </div>
        ) : null}

        {state.phase === "tour" ? (
          <div className="pointer-events-none relative h-full w-full">
            <DemoTourChrome
              chapterIndex={state.chapterIndex}
              stepIndex={state.stepIndex}
              completedChapterIds={state.completedChapterIds}
              exploreOpen={state.exploreOpen}
              // During a coachmark its "Open tour guide" link is the launcher;
              // the floating button would cover drawers (AI composer).
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
            <Link
              to="/"
              onClick={(event) => {
                event.preventDefault()
                leaveDemo("/")
              }}
              // Bottom left, beside the sidebar: the top bar holds the account menu.
              className="font-sans pointer-events-auto absolute bottom-4 left-[68px] z-40 inline-flex min-h-11 items-center rounded-full bg-white px-4 text-sm font-medium text-[#141414] shadow-[0_4px_16px_rgba(0,0,0,0.12)] hover:bg-[#f3f3f3] sm:bottom-8"
            >
              Exit demo
            </Link>
          </div>
        ) : null}

        {showExit ? (
          <div className="pointer-events-auto flex h-full items-center justify-center p-4 sm:p-6">
            <DemoExitModal onLeaveDemo={(to) => leaveDemo(to)} />
            <Link
              to="/"
              onClick={(event) => {
                event.preventDefault()
                leaveDemo("/")
              }}
              className="font-sans absolute top-4 right-4 z-40 text-sm font-medium text-white/90 underline-offset-2 hover:text-white hover:underline"
            >
              Exit demo
            </Link>
          </div>
        ) : null}
      </div>

      {showCoachmark && step && chapter ? (
        <SpotlightCoachmark
          target={step.target}
          chapterLabel={chapter.label}
          stepLabel={`Step ${state.stepIndex + 1} of ${chapter.steps.length}`}
          title={step.title}
          body={step.body}
          ctaLabel={step.ctaLabel}
          onCta={advanceFromCta}
          onSkip={() =>
            persist({
              ...state,
              exploreOpen: true,
            })
          }
        />
      ) : null}
    </div>
  )

  if (typeof document === "undefined") {
    return null
  }

  return createPortal(overlay, document.body)
}

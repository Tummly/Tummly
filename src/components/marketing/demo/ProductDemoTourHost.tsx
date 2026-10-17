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

  useEffect(() => {
    if (!showEntry) return
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
  }, [showEntry])

  const scanAbsoluteUrl = useMemo(() => {
    if (!scanPath) return null
    if (typeof window === "undefined") return scanPath
    return new URL(scanPath, window.location.origin).toString()
  }, [scanPath])

  if (state == null || !isDemoTourActive()) {
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
    !state.freeExplore &&
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
      freeExplore: false,
      phase: "tour",
    })
    navigate(productDemoPathForChapter(nextChapter.id))
    const nextStep = nextChapter.steps[clampedStep]
    if (nextStep) {
      window.setTimeout(() => {
        dispatchDemoTourAction({
          action: nextStep.action,
          target: nextStep.target,
        })
      }, 250)
    }
  }

  const advanceFromCta = () => {
    if (chapter == null || step == null) return

    dispatchDemoTourAction({ action: step.action, target: step.target })

    if (step.action === "completeTour") {
      persist({
        ...state,
        completedChapterIds: PRODUCT_DEMO.tour.chapters.map((c) => c.id),
        exploreOpen: false,
        phase: "exit",
      })
      return
    }

    if (step.action === "completeChapter") {
      const completed = new Set(state.completedChapterIds)
      completed.add(chapter.id)
      const nextChapterIndex = state.chapterIndex + 1
      if (nextChapterIndex >= PRODUCT_DEMO.tour.chapters.length) {
        persist({
          ...state,
          completedChapterIds: [...completed],
          exploreOpen: true,
          phase: "tour",
        })
        return
      }
      persist({
        ...state,
        completedChapterIds: [...completed],
        chapterIndex: nextChapterIndex,
        stepIndex: 0,
        exploreOpen: true,
        phase: "tour",
      })
      const nextChapter = PRODUCT_DEMO.tour.chapters[nextChapterIndex]
      if (nextChapter) {
        navigate(productDemoPathForChapter(nextChapter.id))
      }
      return
    }

    if (step.action === "navigateCampaigns") {
      navigate(productDemoPath("campaigns"))
    }

    const nextStepIndex = state.stepIndex + 1
    if (nextStepIndex < chapter.steps.length) {
      goChapterStep(state.chapterIndex, nextStepIndex)
      return
    }

    // End of chapter without completeChapter action — open explore
    const completed = new Set(state.completedChapterIds)
    completed.add(chapter.id)
    persist({
      ...state,
      completedChapterIds: [...completed],
      exploreOpen: true,
      phase: "tour",
    })
  }

  const overlay = (
    <div className="pointer-events-none fixed inset-0 z-[80]">
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
              onExplore={() => {
                persist({
                  ...state,
                  phase: "tour",
                  exploreOpen: false,
                  freeExplore: true,
                })
                navigate(productDemoPath("feedback"))
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
              showFab={showFab && !showExit}
              onToggleExplore={() =>
                persist({
                  ...state,
                  exploreOpen: !state.exploreOpen,
                })
              }
              onSelectChapter={(index) => goChapterStep(index, 0)}
            />
            <Link
              to="/"
              onClick={(event) => {
                event.preventDefault()
                leaveDemo("/")
              }}
              className="font-sans pointer-events-auto absolute top-4 right-4 z-40 text-sm font-medium text-[#141414]/70 underline-offset-2 hover:text-[#141414] hover:underline"
            >
              Exit demo
            </Link>
          </div>
        ) : null}

        {showExit ? (
          <div className="pointer-events-auto flex h-full items-center justify-center p-4 sm:p-6">
            <DemoExitModal
              onContinueExploring={() =>
                persist({
                  ...state,
                  exploreOpen: false,
                  freeExplore: true,
                  phase: "tour",
                })
              }
              onLeaveDemo={(to) => leaveDemo(to)}
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

import { useEffect, useState } from "react"
import { createPortal } from "react-dom"
import { Link, useNavigate } from "react-router-dom"

import { DemoExitModal } from "@/components/marketing/demo/DemoExitModal"
import { DemoTourChrome } from "@/components/marketing/demo/DemoTourChrome"
import { DemoTourGuideFab } from "@/components/marketing/demo/DemoTourGuideFab"
import { PRODUCT_DEMO } from "@/content/marketing/productDemoPage"
import {
  clearDemoTourSession,
  isDemoTourActive,
  readDemoTourSession,
  writeDemoTourSession,
  type DemoTourSessionState,
} from "@/lib/productDemo/demoTourSession"
import { productDemoPathForTourStep } from "@/lib/productDemo/productDemoPaths"
import { clearAuthSession } from "@/pages/utils/authHelpers"
import { cn } from "@/lib/utils"

/**
 * Leave the real dashboard demo. Must hard-navigate: clearing the JWT while
 * still under ProtectedRoute renders `<Navigate to="/login" />` and beats
 * a React Router navigate to home/pricing.
 */
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

  useEffect(() => {
    const sync = () => setState(readDemoTourSession())
    window.addEventListener("tummly-demo-tour-change", sync)
    window.addEventListener("storage", sync)
    return () => {
      window.removeEventListener("tummly-demo-tour-change", sync)
      window.removeEventListener("storage", sync)
    }
  }, [])

  if (state == null || !isDemoTourActive()) {
    return null
  }

  const { stepIndex, exploreOpen, phase } = state
  const showModalOverlay = phase === "exit"
  const blurShell = phase === "tour" && exploreOpen

  const persist = (next: DemoTourSessionState) => {
    writeDemoTourSession(next)
    setState(next)
  }

  const goTourStep = (index: number) => {
    const step = PRODUCT_DEMO.tour.steps[index]
    if (step == null) return
    persist({
      stepIndex: index,
      exploreOpen: false,
      phase: "tour",
    })
    navigate(productDemoPathForTourStep(step.id))
  }

  const overlay = (
    <div className="pointer-events-none fixed inset-0 z-[80]">
      {(showModalOverlay || blurShell) && (
        <div
          className={cn(
            "pointer-events-none absolute inset-0 bg-[rgba(20,20,20,0.22)] transition-[backdrop-filter] duration-300",
            (showModalOverlay || blurShell) && "backdrop-blur-[8px]"
          )}
          aria-hidden
        />
      )}

      <div className="pointer-events-none absolute inset-0">
        {phase === "tour" ? (
          <div className="pointer-events-none relative h-full w-full">
            <DemoTourChrome
              stepIndex={stepIndex}
              exploreOpen={exploreOpen}
              onToggleExplore={() =>
                persist({
                  ...state,
                  exploreOpen: !exploreOpen,
                })
              }
              onSelectStep={goTourStep}
              onBack={() => goTourStep(Math.max(0, stepIndex - 1))}
              onContinue={() =>
                goTourStep(
                  Math.min(PRODUCT_DEMO.tour.steps.length - 1, stepIndex + 1)
                )
              }
              onFinish={() =>
                persist({
                  ...state,
                  exploreOpen: false,
                  phase: "exit",
                })
              }
            />
            <Link
              to="/"
              onClick={(event) => {
                event.preventDefault()
                leaveDemo("/")
              }}
              className="font-sans pointer-events-auto absolute top-4 right-4 z-40 text-sm font-medium text-[#141414]/70 underline-offset-2 hover:text-[#141414] hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30"
            >
              Exit demo
            </Link>
          </div>
        ) : null}

        {phase === "exit" ? (
          <div className="pointer-events-auto flex h-full items-center justify-center p-4 sm:p-6">
            <DemoExitModal
              onContinueExploring={() =>
                persist({
                  stepIndex,
                  exploreOpen: false,
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
              className="font-sans absolute top-4 right-4 z-40 text-sm font-medium text-white/90 underline-offset-2 hover:text-white hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/40"
            >
              Exit demo
            </Link>
            <DemoTourGuideFab
              label={PRODUCT_DEMO.tour.fabLabel}
              badge={PRODUCT_DEMO.tour.fabBadgeTotal}
              onClick={() =>
                persist({
                  ...state,
                  exploreOpen: true,
                  phase: "tour",
                })
              }
            />
          </div>
        ) : null}
      </div>
    </div>
  )

  if (typeof document === "undefined") {
    return null
  }

  return createPortal(overlay, document.body)
}

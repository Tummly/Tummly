import { useEffect, useState } from "react"
import { Link, useNavigate } from "react-router-dom"

import { createDemoSession } from "@/api/demoSessionApi"
import { DemoTourGuideFab } from "@/components/marketing/demo/DemoTourGuideFab"
import { DemoWelcomeModal } from "@/components/marketing/demo/DemoWelcomeModal"
import { PRODUCT_DEMO } from "@/content/marketing/productDemoPage"
import { startDemoTourSession } from "@/lib/productDemo/demoTourSession"
import { productDemoPathForTourStep } from "@/lib/productDemo/productDemoPaths"
import {
  completeUserSession,
  persistSelectedLocation,
} from "@/pages/utils/authHelpers"

/**
 * Public product walkthrough entry — welcome, then real demo-session → /single-dashboard.
 * Path: `/product-demo`.
 */
export default function ProductDemoPage() {
  const navigate = useNavigate()
  const [starting, setStarting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    document.title = "Try demo · Tummly"
  }, [])

  const startTour = async () => {
    if (starting) return
    setStarting(true)
    setError(null)
    try {
      const session = await createDemoSession()
      completeUserSession(session)
      if (session.selectedLocationId != null) {
        persistSelectedLocation(session.selectedLocationId)
      }
      const firstStep = PRODUCT_DEMO.tour.steps[0]
      startDemoTourSession(0)
      navigate(productDemoPathForTourStep(firstStep.id), {
        replace: true,
      })
    } catch (err) {
      const message =
        err &&
        typeof err === "object" &&
        "response" in err &&
        err.response &&
        typeof err.response === "object" &&
        "data" in err.response &&
        err.response.data &&
        typeof err.response.data === "object" &&
        "message" in err.response.data &&
        typeof (err.response.data as { message: unknown }).message === "string"
          ? (err.response.data as { message: string }).message
          : "Unable to start the product demo. Try again later."
      setError(message)
      setStarting(false)
    }
  }

  return (
    <main className="relative h-dvh w-full overflow-hidden bg-[#f3f3f3]">
      <div className="absolute inset-0 scale-[1.01] bg-[#e8e8e8] blur-[8px]" />
      <div
        className="pointer-events-none absolute inset-0 bg-[rgba(20,20,20,0.22)]"
        aria-hidden
      />

      <div className="absolute inset-0 z-30 flex h-full items-center justify-center p-4 sm:p-6">
        <div className="flex w-full max-w-[662px] flex-col items-center gap-4">
          <DemoWelcomeModal
            onStartTour={() => {
              void startTour()
            }}
          />
          {starting ? (
            <p className="font-sans text-sm text-white/90">Starting demo…</p>
          ) : null}
          {error ? (
            <p
              role="alert"
              className="font-sans max-w-md text-center text-sm text-[#ffb4b4]"
            >
              {error}
            </p>
          ) : null}
          <DemoTourGuideFab
            label={PRODUCT_DEMO.tour.fabLabel}
            badge={PRODUCT_DEMO.tour.fabBadgeTotal}
            onClick={() => {
              void startTour()
            }}
          />
          <Link
            to="/"
            className="font-sans absolute top-4 right-4 z-40 text-sm font-medium text-white/90 underline-offset-2 hover:text-white hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/40"
          >
            Exit demo
          </Link>
        </div>
      </div>
    </main>
  )
}

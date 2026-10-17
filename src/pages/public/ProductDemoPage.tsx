import { useEffect, useState } from "react"
import { Link, useNavigate } from "react-router-dom"

import {
  createDemoSession,
  fetchDemoGuestEntry,
} from "@/api/demoSessionApi"
import { AuthSessionLoading } from "@/components/auth/AuthSessionLoading"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { PRODUCT_DEMO } from "@/content/marketing/productDemoPage"
import {
  startDemoTourSession,
  writeDemoGuestScanPath,
} from "@/lib/productDemo/demoTourSession"
import { productDemoPath } from "@/lib/productDemo/productDemoPaths"
import {
  completeUserSession,
  persistSelectedLocation,
} from "@/pages/utils/authHelpers"

function apiErrorMessage(err: unknown, fallback: string): string {
  if (
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
  ) {
    return (err.response.data as { message: string }).message
  }
  if (err instanceof Error && err.message) {
    return err.message
  }
  return fallback
}

/**
 * Public product walkthrough bootstrap — creates the demo JWT, then opens the
 * real Operator dashboard with the entry panel over it (`phase: entry`).
 * Path: `/product-demo`.
 */
export default function ProductDemoPage() {
  const navigate = useNavigate()
  const [unavailable, setUnavailable] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [bootKey, setBootKey] = useState(0)

  useEffect(() => {
    document.title = "Try demo · Tummly"

    let cancelled = false

    async function boot() {
      setUnavailable(false)
      setError(null)
      try {
        const [entry, session] = await Promise.all([
          fetchDemoGuestEntry(),
          createDemoSession(),
        ])
        if (cancelled) return

        completeUserSession(session)
        if (session.selectedLocationId != null) {
          persistSelectedLocation(session.selectedLocationId)
        }
        writeDemoGuestScanPath(entry.scanPath)
        startDemoTourSession({
          entryPath: "skip",
          hasGuestSubmission: false,
          phase: "entry",
        })
        navigate(productDemoPath("home"), { replace: true })
      } catch (err) {
        if (cancelled) return
        setUnavailable(true)
        setError(
          apiErrorMessage(err, "Unable to start the product demo right now.")
        )
      }
    }

    void boot()
    return () => {
      cancelled = true
    }
  }, [navigate, bootKey])

  if (unavailable) {
    const copy = PRODUCT_DEMO.entry
    return (
      <main className="relative flex h-dvh w-full items-center justify-center overflow-hidden bg-[#f3f3f3] p-4">
        <div
          role="dialog"
          aria-modal="true"
          aria-labelledby="demo-entry-unavailable-title"
          className="relative z-10 flex w-full max-w-[440px] flex-col items-center gap-6 overflow-clip rounded-[10px] bg-white px-6 py-8 shadow-[0_8px_40px_rgba(0,0,0,0.18)] sm:px-10"
        >
          <MarketingLogo onLight width={120} height={30} className="h-[30px] w-auto" />
          <div className="flex flex-col items-center gap-2 text-center">
            <h1
              id="demo-entry-unavailable-title"
              className="font-jakarta text-[22px] font-semibold text-[#141414] sm:text-[26px]"
            >
              {copy.unavailableTitle}
            </h1>
            <p className="font-sans text-sm text-[#4e4e4e]">{copy.unavailableBody}</p>
            {error ? (
              <p role="alert" className="font-sans text-sm text-[#b42318]">
                {error}
              </p>
            ) : null}
          </div>
          <div className="flex w-full flex-col gap-3 sm:flex-row">
            <MarketingButton
              type="button"
              variant="primary"
              showArrow={false}
              className="w-full"
              asChild
            >
              <Link to={PRODUCT_DEMO.exit.primaryTo}>
                {PRODUCT_DEMO.exit.primaryLabel}
              </Link>
            </MarketingButton>
            <MarketingButton
              type="button"
              variant="secondary"
              showArrow={false}
              className="w-full"
              asChild
            >
              <Link to={PRODUCT_DEMO.exit.secondaryTo}>
                {PRODUCT_DEMO.exit.secondaryLabel}
              </Link>
            </MarketingButton>
          </div>
          <button
            type="button"
            onClick={() => setBootKey((key) => key + 1)}
            className="font-sans text-sm text-[#4e4e4e] underline-offset-2 hover:underline"
          >
            {copy.restartLabel}
          </button>
        </div>
        <Link
          to="/"
          className="font-sans absolute top-4 right-4 z-40 text-sm font-medium text-[#141414]/90 underline-offset-2 hover:underline"
        >
          Exit demo
        </Link>
      </main>
    )
  }

  return <AuthSessionLoading />
}

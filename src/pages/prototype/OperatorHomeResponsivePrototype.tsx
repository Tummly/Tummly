import { useEffect, useRef, useState } from "react"
import { useSearchParams } from "react-router-dom"

import { cn } from "@/lib/utils"

import { PrototypeViewportSwitcher } from "./PrototypeViewportSwitcher"
import {
  getOperatorHomeResponsiveViewportWidth,
  OPERATOR_HOME_RESPONSIVE_VIEWPORTS,
  parseOperatorHomeResponsiveViewport,
} from "./operatorHomeResponsiveFixtures"

const FRAME_PATH = "/prototype/operator-home-responsive/frame"

/**
 * PROTOTYPE — throwaway route for wayfinder ticket 02.
 * Question: validate operator shell + hero layout at 320 / 768 / 1024px.
 * Route: /prototype/operator-home-responsive?viewport=320|768|1024|full
 *
 * Uses an iframe so Tailwind breakpoints follow the preview width, not the
 * browser window.
 */
export default function OperatorHomeResponsivePrototype() {
  const [searchParams] = useSearchParams()
  const viewportId = parseOperatorHomeResponsiveViewport(
    searchParams.get("viewport")
  )
  const viewportWidth = getOperatorHomeResponsiveViewportWidth(viewportId)
  const iframeRef = useRef<HTMLIFrameElement>(null)
  const [hasHorizontalOverflow, setHasHorizontalOverflow] = useState(false)

  useEffect(() => {
    const iframe = iframeRef.current
    if (!iframe) {
      return
    }

    function measureOverflow() {
      const doc = iframe.contentDocument
      const shellRoot = doc?.querySelector("[data-prototype-shell-root]")
      if (!(shellRoot instanceof HTMLElement)) {
        setHasHorizontalOverflow(false)
        return
      }

      setHasHorizontalOverflow(shellRoot.scrollWidth > shellRoot.clientWidth + 1)
    }

    function handleLoad() {
      measureOverflow()
      const doc = iframe.contentDocument
      const shellRoot = doc?.querySelector("[data-prototype-shell-root]")
      if (!(shellRoot instanceof HTMLElement) || typeof ResizeObserver === "undefined") {
        return
      }

      const observer = new ResizeObserver(measureOverflow)
      observer.observe(shellRoot)
      iframe.dataset.resizeObserver = "attached"
    }

    iframe.addEventListener("load", handleLoad)
    if (iframe.contentDocument?.readyState === "complete") {
      handleLoad()
    }

    return () => iframe.removeEventListener("load", handleLoad)
  }, [viewportId])

  const frameSrc = `${FRAME_PATH}${searchParams.toString() ? `?${searchParams.toString()}` : ""}`

  return (
    <div className="flex min-h-dvh flex-col bg-[#d4d8dc] text-foreground">
      <header className="border-b border-black/10 bg-white px-4 py-3">
        <p className="text-xs font-semibold uppercase tracking-wide text-primary">
          Prototype
        </p>
        <h1 className="text-base font-semibold">
          Operator Home — shell + hero responsive preview
        </h1>
        <p className="mt-1 max-w-3xl text-sm text-muted-foreground">
          Production <code className="text-xs">OperatorDashboardShell</code> and{" "}
          <code className="text-xs">OperatorHomeHero</code> render inside an
          iframe so Tailwind breakpoints match the preview width. Use the bottom
          bar, quick links, or <code className="text-xs">?viewport=</code>{" "}
          (320, 768, 1024, full).
        </p>
        <div className="mt-3 flex flex-wrap gap-2">
          {OPERATOR_HOME_RESPONSIVE_VIEWPORTS.map((item) => (
            <a
              key={item.id}
              href={`?viewport=${item.id}`}
              className={cn(
                "rounded-full border px-3 py-1 text-xs font-medium",
                viewportId === item.id
                  ? "border-primary bg-primary/10 text-primary"
                  : "border-border bg-background text-foreground"
              )}
            >
              {item.label}
            </a>
          ))}
        </div>
      </header>

      <div className="flex flex-1 flex-col items-center overflow-auto p-4 pb-24">
        <div
          className={cn(
            "mb-3 rounded-md px-3 py-2 text-sm font-medium",
            hasHorizontalOverflow
              ? "bg-destructive/10 text-destructive"
              : "bg-emerald-600/10 text-emerald-800"
          )}
          role="status"
          aria-live="polite"
        >
          {hasHorizontalOverflow
            ? "Horizontal overflow detected inside the shell — content is wider than the iframe viewport."
            : "No horizontal overflow detected in the shell root."}
        </div>

        <iframe
          ref={iframeRef}
          title="Operator home responsive preview frame"
          src={frameSrc}
          className={cn(
            "rounded-xl bg-white shadow-2xl",
            viewportWidth == null
              ? "w-full max-w-360 border-2 border-black/20"
              : "mx-auto block border-0"
          )}
          style={{
            width: viewportWidth == null ? undefined : `${viewportWidth}px`,
            height: "min(90vh, 900px)",
          }}
        />
      </div>

      <PrototypeViewportSwitcher />
    </div>
  )
}

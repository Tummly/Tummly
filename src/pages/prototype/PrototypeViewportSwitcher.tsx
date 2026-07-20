import { ChevronLeftIcon, ChevronRightIcon } from "lucide-react"
import { useEffect } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"

import { cn } from "@/lib/utils"

import {
  OPERATOR_HOME_RESPONSIVE_VIEWPORTS,
  parseOperatorHomeResponsiveViewport,
  type OperatorHomeResponsiveViewportId,
} from "./operatorHomeResponsiveFixtures"

type PrototypeViewportSwitcherProps = {
  paramName?: string
  className?: string
}

/** Fixed bottom bar for throwaway responsive prototypes — dev only. */
export function PrototypeViewportSwitcher({
  paramName = "viewport",
  className,
}: PrototypeViewportSwitcherProps) {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const current = parseOperatorHomeResponsiveViewport(
    searchParams.get(paramName)
  )
  const currentIndex = OPERATOR_HOME_RESPONSIVE_VIEWPORTS.findIndex(
    (item) => item.id === current
  )

  const setViewport = (next: OperatorHomeResponsiveViewportId) => {
    const params = new URLSearchParams(searchParams)
    params.set(paramName, next)
    navigate({ search: params.toString() }, { replace: true })
  }

  const cycle = (direction: -1 | 1) => {
    const nextIndex =
      (currentIndex + direction + OPERATOR_HOME_RESPONSIVE_VIEWPORTS.length) %
      OPERATOR_HOME_RESPONSIVE_VIEWPORTS.length
    setViewport(OPERATOR_HOME_RESPONSIVE_VIEWPORTS[nextIndex].id)
  }

  useEffect(() => {
    if (import.meta.env.PROD) {
      return
    }

    function handleKeyDown(event: KeyboardEvent) {
      const target = event.target
      if (
        target instanceof HTMLElement &&
        (target.closest("input, textarea, select, [contenteditable='true']") !=
          null)
      ) {
        return
      }

      if (event.key === "ArrowLeft" || event.key === "ArrowRight") {
        event.preventDefault()
        const index = OPERATOR_HOME_RESPONSIVE_VIEWPORTS.findIndex(
          (item) => item.id === current
        )
        const direction = event.key === "ArrowLeft" ? -1 : 1
        const nextIndex =
          (index + direction + OPERATOR_HOME_RESPONSIVE_VIEWPORTS.length) %
          OPERATOR_HOME_RESPONSIVE_VIEWPORTS.length
        setViewport(OPERATOR_HOME_RESPONSIVE_VIEWPORTS[nextIndex].id)
      }
    }

    window.addEventListener("keydown", handleKeyDown)
    return () => window.removeEventListener("keydown", handleKeyDown)
  }, [current, searchParams, paramName, navigate])

  if (import.meta.env.PROD) {
    return null
  }

  const currentLabel =
    OPERATOR_HOME_RESPONSIVE_VIEWPORTS[currentIndex]?.label ?? current

  return (
    <div
      className={cn(
        "fixed bottom-4 left-1/2 z-50 flex -translate-x-1/2 items-center gap-2 rounded-full border border-black/10 bg-zinc-950 px-2 py-1.5 text-white shadow-lg",
        className
      )}
      role="toolbar"
      aria-label="Prototype viewport switcher"
    >
      <button
        type="button"
        className="rounded-full p-1.5 hover:bg-white/10"
        aria-label="Previous viewport"
        onClick={() => cycle(-1)}
      >
        <ChevronLeftIcon className="size-4" aria-hidden />
      </button>
      <span className="min-w-24 text-center text-xs font-medium">
        {currentLabel}
      </span>
      <button
        type="button"
        className="rounded-full p-1.5 hover:bg-white/10"
        aria-label="Next viewport"
        onClick={() => cycle(1)}
      >
        <ChevronRightIcon className="size-4" aria-hidden />
      </button>
    </div>
  )
}

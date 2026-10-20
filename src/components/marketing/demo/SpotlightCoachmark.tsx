import { useEffect, useLayoutEffect, useRef, useState } from "react"
import { createPortal } from "react-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { DEMO_CHROME_ATTRIBUTE } from "@/lib/productDemo/demoInteractionLock"
import { cn } from "@/lib/utils"

type SpotlightCoachmarkProps = {
  target: string
  chapterLabel: string
  stepLabel: string
  title: string
  body: string
  ctaLabel: string
  onCta: () => void
  onSkip?: () => void
}

/** Viewport rectangle (the overlay is `position: fixed`). */
type Box = { top: number; left: number; width: number; height: number }

type Placement = { top: number; left: number }

/** Space between the highlighted control and its ring. */
const RING_PAD = 8
/** Space between the ring and the popover. */
const GAP = 14
/** Minimum space to the viewport edge. */
const EDGE = 16
const POLL_MS = 250
/** Operator sidebar width plus edge space. */
const SIDEBAR_CLEARANCE = 68
/** Height of the bottom-left Exit demo button plus its gap. */
const EXIT_BUTTON_CLEARANCE = 72

function findTarget(target: string): HTMLElement | null {
  return document.querySelector<HTMLElement>(`[data-demo-tour="${target}"]`)
}

function readTargetBox(el: HTMLElement | null): Box | null {
  if (!el) return null
  const rect = el.getBoundingClientRect()
  if (rect.width < 2 && rect.height < 2) return null
  // Clip to the viewport and to every scrolling / clipping ancestor, so a
  // wide table row or a tall panel keeps a ring around its visible part.
  let top = Math.max(rect.top, 0)
  let left = Math.max(rect.left, 0)
  let bottom = Math.min(rect.bottom, window.innerHeight)
  let right = Math.min(rect.right, window.innerWidth)
  for (
    let parent = el.parentElement;
    parent != null && parent !== document.body;
    parent = parent.parentElement
  ) {
    const style = window.getComputedStyle(parent)
    if (style.overflowX === "visible" && style.overflowY === "visible") {
      continue
    }
    const clip = parent.getBoundingClientRect()
    if (style.overflowX !== "visible") {
      left = Math.max(left, clip.left)
      right = Math.min(right, clip.right)
    }
    if (style.overflowY !== "visible") {
      top = Math.max(top, clip.top)
      bottom = Math.min(bottom, clip.bottom)
    }
  }
  if (bottom - top < 2 || right - left < 2) return null
  return { top, left, width: right - left, height: bottom - top }
}

/** Ring around the box, kept 2px inside the viewport so it stays visible. */
function ringStyle(box: Box) {
  const top = Math.max(box.top - RING_PAD, 2)
  const left = Math.max(box.left - RING_PAD, 2)
  const bottom = Math.min(box.top + box.height + RING_PAD, window.innerHeight - 2)
  const right = Math.min(box.left + box.width + RING_PAD, window.innerWidth - 2)
  return { top, left, width: right - left, height: bottom - top }
}

function sameBox(a: Box | null, b: Box | null): boolean {
  if (a == null || b == null) return a === b
  return (
    Math.abs(a.top - b.top) < 0.5 &&
    Math.abs(a.left - b.left) < 0.5 &&
    Math.abs(a.width - b.width) < 0.5 &&
    Math.abs(a.height - b.height) < 0.5
  )
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), Math.max(min, max))
}

/**
 * Places the popover next to the ring without covering it: below, above,
 * right, then left. With no room, it docks at the bottom of the viewport.
 */
function placePopover(
  box: Box | null,
  size: { width: number; height: number }
): Placement {
  const vw = window.innerWidth
  const vh = window.innerHeight
  const maxLeft = vw - size.width - EDGE
  const maxTop = vh - size.height - EDGE

  if (box == null) {
    return { top: clamp(vh / 2 - size.height / 2, EDGE, maxTop), left: clamp(vw / 2 - size.width / 2, EDGE, maxLeft) }
  }

  const ring = {
    top: box.top - RING_PAD,
    left: box.left - RING_PAD,
    bottom: box.top + box.height + RING_PAD,
    right: box.left + box.width + RING_PAD,
  }
  const centreX = clamp(box.left + box.width / 2 - size.width / 2, EDGE, maxLeft)
  const centreY = clamp(box.top + box.height / 2 - size.height / 2, EDGE, maxTop)

  if (ring.bottom + GAP + size.height <= vh - EDGE) {
    return { top: ring.bottom + GAP, left: centreX }
  }
  if (ring.top - GAP - size.height >= EDGE) {
    return { top: ring.top - GAP - size.height, left: centreX }
  }
  if (ring.right + GAP + size.width <= vw - EDGE) {
    return { top: centreY, left: ring.right + GAP }
  }
  if (ring.left - GAP - size.width >= EDGE) {
    return { top: centreY, left: ring.left - GAP - size.width }
  }
  // No free side: dock bottom-left, clear of the sidebar, the Exit demo
  // button below it, and the Tour guide launcher at bottom-right.
  return {
    top: clamp(maxTop - EXIT_BUTTON_CLEARANCE, EDGE, maxTop),
    left: clamp(SIDEBAR_CLEARANCE, EDGE, maxLeft),
  }
}

/** DOM-anchored coachmark with spotlight cutout (no tour library). */
export function SpotlightCoachmark({
  target,
  chapterLabel,
  stepLabel,
  title,
  body,
  ctaLabel,
  onCta,
  onSkip,
}: SpotlightCoachmarkProps) {
  const [box, setBox] = useState<Box | null>(null)
  const [placement, setPlacement] = useState<Placement | null>(null)
  const popoverRef = useRef<HTMLDivElement>(null)
  const scrolledRef = useRef<HTMLElement | null>(null)
  const reduceMotion =
    typeof window !== "undefined" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches

  // Track the target: it can mount late (data load, drawer open) and move.
  useLayoutEffect(() => {
    scrolledRef.current = null
    const update = () => {
      const el = findTarget(target)
      if (el != null && scrolledRef.current !== el) {
        // Scroll once per element, in whichever container owns it.
        scrolledRef.current = el
        // Instant scroll: a smooth scroll leaves the ring on stale positions.
        el.scrollIntoView({ block: "center", inline: "nearest" })
      }
      const next = readTargetBox(el)
      setBox((prev) => (sameBox(prev, next) ? prev : next))
    }
    update()
    const id = window.setInterval(update, POLL_MS)
    window.addEventListener("resize", update)
    window.addEventListener("scroll", update, true)
    return () => {
      window.clearInterval(id)
      window.removeEventListener("resize", update)
      window.removeEventListener("scroll", update, true)
    }
  }, [target, reduceMotion])

  useLayoutEffect(() => {
    const popover = popoverRef.current
    if (!popover) return
    const next = placePopover(box, {
      width: popover.offsetWidth,
      height: popover.offsetHeight,
    })
    setPlacement((prev) =>
      prev != null &&
      Math.abs(prev.top - next.top) < 0.5 &&
      Math.abs(prev.left - next.left) < 0.5
        ? prev
        : next
    )
  }, [box, title, body])

  // Keyboard users land on the step CTA (the only way forward).
  useEffect(() => {
    popoverRef.current
      ?.querySelector<HTMLButtonElement>("button")
      ?.focus({ preventScroll: true })
  }, [target, title])

  if (typeof document === "undefined") {
    return null
  }

  return createPortal(
    <div
      className="pointer-events-none fixed inset-0 z-[310]"
      {...{ [DEMO_CHROME_ATTRIBUTE]: "" }}
    >
      {box ? (
        <div
          aria-hidden
          className={cn(
            "absolute rounded-[10px]",
            !reduceMotion && "transition-[top,left,width,height] duration-200"
          )}
          style={{
            ...ringStyle(box),
            boxShadow:
              "0 0 0 2px #14a74a, 0 0 0 6px rgba(20,167,74,0.18), 0 0 0 9999px rgba(20,20,20,0.45)",
          }}
        />
      ) : (
        <div aria-hidden className="absolute inset-0 bg-[rgba(20,20,20,0.45)]" />
      )}

      <div
        ref={popoverRef}
        role="dialog"
        aria-labelledby="demo-coachmark-title"
        aria-describedby="demo-coachmark-body"
        className={cn(
          "pointer-events-auto absolute z-10 w-[min(calc(100vw-2rem),340px)] rounded-[12px] bg-white p-5 shadow-[0_12px_40px_rgba(0,0,0,0.2)]",
          !reduceMotion && "transition-[top,left,opacity] duration-200",
          placement == null && "opacity-0"
        )}
        style={{ top: placement?.top ?? EDGE, left: placement?.left ?? EDGE }}
      >
        <p className="font-sans mb-2 text-[11px] font-medium tracking-[0.06em] text-[#7c7c7c] uppercase">
          {chapterLabel} · {stepLabel}
        </p>
        <h2
          id="demo-coachmark-title"
          className="font-jakarta mb-2 text-lg font-semibold text-[#141414]"
        >
          {title}
        </h2>
        <p
          id="demo-coachmark-body"
          className="font-sans mb-4 text-sm leading-5 text-[#4e4e4e]"
        >
          {body}
        </p>
        <div className="flex flex-col gap-2">
          <MarketingButton
            type="button"
            variant="primary"
            showArrow={false}
            className="w-full min-h-11"
            onClick={onCta}
          >
            {ctaLabel}
          </MarketingButton>
          {onSkip ? (
            <button
              type="button"
              onClick={onSkip}
              className="font-sans min-h-11 text-sm text-[#4e4e4e] underline-offset-2 hover:underline"
            >
              Open tour guide
            </button>
          ) : null}
        </div>
      </div>
    </div>,
    document.body
  )
}

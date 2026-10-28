import { useEffect, useLayoutEffect, useRef, useState } from "react"
import { createPortal } from "react-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { DEMO_CHROME_ATTRIBUTE } from "@/lib/productDemo/demoInteractionLock"
import { cn } from "@/lib/utils"

type SpotlightCoachmarkProps = {
  target: string
  /** Figma Guide footer, e.g. "1 of 4". */
  stepLabel: string
  title: string
  body: string
  ctaLabel: string
  backLabel?: string
  onCta: () => void
  onBack: () => void
}

/** Viewport rectangle (the overlay is `position: fixed`). */
type Box = { top: number; left: number; width: number; height: number }

/** Popover side relative to the target; `null` = centred or docked (no arrow). */
type Side = "bottom" | "top" | "right" | "left"

type Placement = {
  top: number
  left: number
  side: Side | null
  /** Arrow centre along the popover edge that faces the target, in px. */
  arrowOffset: number
}

/** Space between the highlighted control and its ring. */
const RING_PAD = 8
/** Space between the ring and the popover (holds the arrow). */
const GAP = 18
/** Figma Guide pointer: 20px deep, 24px wide, overlaps the card by 4px. */
const ARROW_DEPTH = 20
const ARROW_WIDTH = 24
const ARROW_OVERLAP = 4
/** Arrow keeps this distance from the popover corners. */
const ARROW_INSET = 20
/** Figma Guide drop shadow (`5734:17945`). */
const POPOVER_SHADOW =
  "drop-shadow(-2px 14px 16px rgba(0,0,0,0.1)) drop-shadow(-6px 57px 29px rgba(0,0,0,0.09)) drop-shadow(-14px 129px 39px rgba(0,0,0,0.05))"
/** Minimum space to the viewport edge. */
const EDGE = 16
const POLL_MS = 250
/** Targets this wide or less get the popover beside them. */
const NARROW_TARGET = 120
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

/** Spotlight cutout around the box, kept 2px inside the viewport. */
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
    return {
      top: clamp(vh / 2 - size.height / 2, EDGE, maxTop),
      left: clamp(vw / 2 - size.width / 2, EDGE, maxLeft),
      side: null,
      arrowOffset: 0,
    }
  }

  const ring = {
    top: box.top - RING_PAD,
    left: box.left - RING_PAD,
    bottom: box.top + box.height + RING_PAD,
    right: box.left + box.width + RING_PAD,
  }
  const targetX = box.left + box.width / 2
  const targetY = box.top + box.height / 2
  const centreX = clamp(targetX - size.width / 2, EDGE, maxLeft)
  const centreY = clamp(targetY - size.height / 2, EDGE, maxTop)
  const arrowX = (left: number) =>
    clamp(targetX - left, ARROW_INSET, size.width - ARROW_INSET)
  const arrowY = (top: number) =>
    clamp(targetY - top, ARROW_INSET, size.height - ARROW_INSET)

  const candidates: Record<Side, () => Placement | null> = {
    bottom: () =>
      ring.bottom + GAP + size.height <= vh - EDGE
        ? { top: ring.bottom + GAP, left: centreX, side: "bottom", arrowOffset: arrowX(centreX) }
        : null,
    top: () =>
      ring.top - GAP - size.height >= EDGE
        ? { top: ring.top - GAP - size.height, left: centreX, side: "top", arrowOffset: arrowX(centreX) }
        : null,
    right: () =>
      ring.right + GAP + size.width <= vw - EDGE
        ? { top: centreY, left: ring.right + GAP, side: "right", arrowOffset: arrowY(centreY) }
        : null,
    left: () =>
      ring.left - GAP - size.width >= EDGE
        ? { top: centreY, left: ring.left - GAP - size.width, side: "left", arrowOffset: arrowY(centreY) }
        : null,
  }
  // Narrow targets (sidebar icons) take the popover beside them, as in the
  // Figma Guide; wide targets (rows, panels) take it below or above.
  const order: Side[] =
    box.width <= NARROW_TARGET
      ? ["right", "left", "bottom", "top"]
      : ["bottom", "top", "right", "left"]
  for (const side of order) {
    const placement = candidates[side]()
    if (placement) return placement
  }
  // No free side: dock bottom-left, clear of the sidebar, the Exit demo
  // button below it, and the Tour guide launcher at bottom-right.
  return {
    top: clamp(maxTop - EXIT_BUTTON_CLEARANCE, EDGE, maxTop),
    left: clamp(SIDEBAR_CLEARANCE, EDGE, maxLeft),
    side: null,
    arrowOffset: 0,
  }
}

/** Arrow position: on the popover edge that faces the target, pointing at it. */
function arrowStyle(side: Side, offset: number) {
  const outside = -(ARROW_DEPTH - ARROW_OVERLAP)
  // The SVG points left; rotate it to face the target.
  switch (side) {
    case "right":
      return { left: outside, top: offset - ARROW_WIDTH / 2, rotate: "0deg" }
    case "left":
      return { right: outside, top: offset - ARROW_WIDTH / 2, rotate: "180deg" }
    case "bottom":
      return {
        top: outside - (ARROW_WIDTH - ARROW_DEPTH) / 2,
        left: offset - ARROW_DEPTH / 2,
        rotate: "90deg",
      }
    case "top":
      return {
        bottom: outside - (ARROW_WIDTH - ARROW_DEPTH) / 2,
        left: offset - ARROW_DEPTH / 2,
        rotate: "-90deg",
      }
  }
}

function samePlacement(a: Placement | null, b: Placement): boolean {
  return (
    a != null &&
    a.side === b.side &&
    Math.abs(a.top - b.top) < 0.5 &&
    Math.abs(a.left - b.left) < 0.5 &&
    Math.abs(a.arrowOffset - b.arrowOffset) < 0.5
  )
}

/** DOM-anchored coachmark with spotlight cutout (no tour library). */
export function SpotlightCoachmark({
  target,
  stepLabel,
  title,
  body,
  ctaLabel,
  backLabel = "Back",
  onCta,
  onBack,
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
    setPlacement((prev) => (samePlacement(prev, next) ? prev : next))
  }, [box, title, body])

  // Keyboard users land on the step CTA (the way forward).
  useEffect(() => {
    popoverRef.current
      ?.querySelector<HTMLButtonElement>('button[data-variant="primary"]')
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
            boxShadow: "0 0 0 9999px rgba(20,20,20,0.45)",
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
          "pointer-events-auto absolute z-10 w-[min(calc(100vw-2rem),480px)]",
          !reduceMotion && "transition-[top,left,opacity] duration-200",
          placement == null && "opacity-0"
        )}
        style={{
          top: placement?.top ?? EDGE,
          left: placement?.left ?? EDGE,
          filter: POPOVER_SHADOW,
        }}
      >
        {placement?.side ? (
          <svg
            aria-hidden
            width={ARROW_DEPTH}
            height={ARROW_WIDTH}
            viewBox="0 0 20 24"
            className="absolute"
            style={arrowStyle(placement.side, placement.arrowOffset)}
          >
            <path d="M20 1.5 2 12l18 10.5Z" fill="#fff" />
          </svg>
        ) : null}
        <div className="flex flex-col items-end gap-10 rounded-[8px] bg-white p-5 sm:gap-[73px] sm:p-[26px]">
          <div className="flex w-full flex-col items-start gap-4 text-black">
            <h2
              id="demo-coachmark-title"
              className="font-jakarta text-xl leading-normal font-bold"
            >
              {title}
            </h2>
            <p
              id="demo-coachmark-body"
              className="font-sans max-w-[394px] text-base leading-6"
            >
              {body}
            </p>
          </div>
          <div className="flex w-full items-center justify-between gap-4">
            <p className="font-sans text-sm leading-[19px] font-medium whitespace-nowrap text-black">
              {stepLabel}
            </p>
            <div className="flex items-center gap-[19px]">
              <MarketingButton
                type="button"
                variant="secondary"
                showArrow={false}
                className="h-[42px] w-[97px]"
                onClick={onBack}
              >
                {backLabel}
              </MarketingButton>
              <MarketingButton
                type="button"
                variant="primary"
                showArrow={false}
                className="h-[42px] min-w-[97px]"
                onClick={onCta}
              >
                {ctaLabel}
              </MarketingButton>
            </div>
          </div>
        </div>
      </div>
    </div>,
    document.body
  )
}

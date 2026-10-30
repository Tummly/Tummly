import {
  useEffect,
  useEffectEvent,
  useRef,
  useState,
  type CSSProperties,
  type KeyboardEvent,
  type RefObject,
} from "react"
import { animate, motion, useReducedMotion } from "framer-motion"

import stepFeedbackImage from "@/assets/images/marketing/how-it-works/guest-form-step-01.webp"
import stepDetailsImage from "@/assets/images/marketing/how-it-works/guest-form-step-02.webp"
import stepUpdatesImage from "@/assets/images/marketing/how-it-works/guest-form-step-03.webp"
import stepThankYouImage from "@/assets/images/marketing/how-it-works/guest-form-step-04.webp"
import {
  HOW_IT_WORKS_GUEST_FORM,
  HOW_IT_WORKS_GUEST_FORM_STEPS,
  HOW_IT_WORKS_SECTION_BODY,
  HOW_IT_WORKS_SECTION_EYEBROW,
  HOW_IT_WORKS_SECTION_HEADING,
  HOW_IT_WORKS_SECTION_INSET,
  type HowItWorksGuestFormStep,
  type HowItWorksGuestFormStepId,
} from "@/content/marketing/howItWorksPage"
import { cn } from "@/lib/utils"

const STEP_COUNT = HOW_IT_WORKS_GUEST_FORM_STEPS.length
/** Phone paint wipe on each step change (not scroll-scrubbed). */
const PAINT_MS = 0.45
/** Figma active-step green progress `5887:20608` (200px, design token green-600 = #14a946) over the #c9c9c9 divider. */
const PROGRESS_WIDTH_PX = 200
/** Operator `--op-color-green-600`; op tokens are scoped to `html.op`. */
const PROGRESS_GREEN = "#14a946"
/** Minimum clearance under sticky marketing header chrome. */
const HEADER_CLEARANCE_PX = 86
/** Fallback sticky row height until measured (tabs + phone). */
const FALLBACK_PANEL_HEIGHT_PX = 820

/** Guest Form phone per step — Figma `5887:20625`, `5880:19483`, `5880:19597`, `5880:19826` (392×782 at 2×). */
const STEP_IMAGES: Record<HowItWorksGuestFormStepId, string> = {
  feedback: stepFeedbackImage,
  details: stepDetailsImage,
  updates: stepUpdatesImage,
  "thank-you": stepThankYouImage,
}

const PHONE_IMAGE_WIDTH = 784
const PHONE_IMAGE_HEIGHT = 1564

function clampIndex(index: number) {
  return Math.max(0, Math.min(STEP_COUNT - 1, index))
}

function clamp01(value: number) {
  return Math.max(0, Math.min(1, value))
}

/**
 * Centre the sticky panel when it fits; pin under the header when it does not
 * (common on mobile before compact layout measures in).
 */
function stickyTopForViewport(viewportHeight: number, panelHeight: number) {
  const available = viewportHeight - HEADER_CLEARANCE_PX
  if (panelHeight >= available) {
    return HEADER_CLEARANCE_PX
  }
  return HEADER_CLEARANCE_PX + (available - panelHeight) / 2
}

/** Top→bottom paint: reveal grows downward. */
function clipTopToBottom(progress: number): string {
  const p = clamp01(progress)
  return `inset(0 0 ${(1 - p) * 100}% 0)`
}

/** Bottom→top paint: reveal grows upward. */
function clipBottomToTop(progress: number): string {
  const p = clamp01(progress)
  return `inset(${(1 - p) * 100}% 0 0 0)`
}

function StepRow({
  step,
  active,
  reduceMotion,
  onSelect,
}: {
  step: HowItWorksGuestFormStep
  active: boolean
  reduceMotion: boolean | null
  onSelect: () => void
}) {
  return (
    <div className="relative flex w-full flex-col gap-4 lg:gap-[26px]">
      <div className="flex w-full flex-col">
        <button
          type="button"
          id={`how-it-works-guest-form-tab-${step.id}`}
          role="tab"
          aria-selected={active}
          aria-controls={`how-it-works-guest-form-panel-${step.id}`}
          tabIndex={active ? 0 : -1}
          onClick={onSelect}
          className={cn(
            "m-0 flex w-full cursor-pointer flex-col items-start gap-2.5 border-0 bg-transparent p-0 text-left lg:gap-[18px]",
            "transition-colors duration-200 ease-out motion-reduce:transition-none",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2 focus-visible:ring-offset-[#f0f0f0]",
            active ? "text-black" : "text-[#7c7c7c]",
          )}
        >
          <span className="flex items-center gap-2 font-sans text-base leading-[normal] font-medium whitespace-nowrap lg:text-lg">
            <span>Step</span>
            <span aria-hidden>/</span>
            <span>{step.number}</span>
          </span>
          <span
            className={cn(
              "block w-full",
              active
                ? "font-jakarta text-[26px] leading-[normal] font-medium sm:text-[30px] lg:text-[36px]"
                : "font-sans text-base leading-[normal] font-medium lg:text-lg",
            )}
          >
            {step.title}
          </span>
        </button>

        {/*
          CSS grid-rows expand, not framer `height: auto`: framer re-measures on every
          height animation and restores the scroll position, which cancels the
          smooth scroll that a step click starts.
        */}
        <div
          id={`how-it-works-guest-form-panel-${step.id}`}
          role="tabpanel"
          aria-labelledby={`how-it-works-guest-form-tab-${step.id}`}
          aria-hidden={!active}
          inert={!active}
          className={cn(
            "grid w-full transition-[grid-template-rows,opacity] duration-[280ms] ease-out motion-reduce:transition-none",
            active ? "grid-rows-[1fr] opacity-100" : "grid-rows-[0fr] opacity-0",
          )}
        >
          <div className="min-h-0 overflow-hidden">
            <p
              className={cn(
                "m-0 max-w-[596px] pt-3.5",
                HOW_IT_WORKS_SECTION_BODY,
                "text-base leading-[22px] lg:text-base lg:leading-[22px]",
              )}
            >
              {step.body}
            </p>
          </div>
        </div>
      </div>

      <div className="relative h-px w-full bg-[#c9c9c9]" aria-hidden>
        {active ? (
          <motion.span
            layoutId="how-it-works-guest-form-progress"
            className="absolute inset-y-0 left-0 h-px max-w-full"
            style={{
              width: PROGRESS_WIDTH_PX,
              backgroundColor: PROGRESS_GREEN,
            }}
            transition={{
              type: "spring",
              stiffness: reduceMotion ? 500 : 380,
              damping: reduceMotion ? 40 : 32,
              mass: 0.6,
            }}
          />
        ) : null}
      </div>
    </div>
  )
}

function PhoneImage({
  step,
  clipPath,
  zIndex,
}: {
  step: HowItWorksGuestFormStep
  clipPath?: string
  zIndex: number
}) {
  const style: CSSProperties = {
    zIndex,
    ...(clipPath != null ? { clipPath, WebkitClipPath: clipPath } : null),
  }

  return (
    <img
      src={STEP_IMAGES[step.id]}
      alt={step.imageAlt}
      width={PHONE_IMAGE_WIDTH}
      height={PHONE_IMAGE_HEIGHT}
      className="absolute inset-0 size-full object-contain object-center"
      decoding="async"
      draggable={false}
      style={style}
    />
  )
}

/**
 * Phone stack — no opacity/translate.
 * Paint runs on step change and finishes even if scroll stops:
 * next step → top→bottom; previous step → bottom→top.
 */
function PhoneVisual({
  activeIndex,
  reduceMotion,
}: {
  activeIndex: number
  reduceMotion: boolean | null
}) {
  const paintedIndexRef = useRef(activeIndex)
  const paintProgressRef = useRef(1)
  const [baseIndex, setBaseIndex] = useState(activeIndex)
  const [incomingIndex, setIncomingIndex] = useState<number | null>(null)
  const [direction, setDirection] = useState<"down" | "up">("down")
  const [paintProgress, setPaintProgress] = useState(1)

  useEffect(() => {
    const from = paintedIndexRef.current
    const to = clampIndex(activeIndex)
    if (to === from) {
      return
    }

    if (reduceMotion) {
      // Reduced motion paints the active step directly (see render below).
      paintedIndexRef.current = to
      paintProgressRef.current = 1
      return
    }

    setBaseIndex(from)
    setIncomingIndex(to)
    setDirection(to > from ? "down" : "up")
    paintProgressRef.current = 0
    setPaintProgress(0)

    const controls = animate(0, 1, {
      duration: PAINT_MS,
      ease: "easeOut",
      onUpdate: (value) => {
        paintProgressRef.current = value
        setPaintProgress(value)
      },
      onComplete: () => {
        paintedIndexRef.current = to
        paintProgressRef.current = 1
        setBaseIndex(to)
        setIncomingIndex(null)
        setPaintProgress(1)
      },
    })

    return () => {
      controls.stop()
      const settled = paintProgressRef.current >= 0.5 ? to : from
      paintedIndexRef.current = settled
      paintProgressRef.current = 1
      setBaseIndex(settled)
      setIncomingIndex(null)
      setPaintProgress(1)
    }
  }, [activeIndex, reduceMotion])

  /*
    Figma phone `5887:20625` is 392×782. At lg+ it keeps that size when the viewport
    is tall enough (sticky panel = phone + 70px above + 40px below + 86px header);
    short viewports shrink it down to 520px.
  */
  const frameClass =
    "relative mx-auto aspect-[392/782] h-[min(30svh,360px)] w-auto max-w-[280px] shrink-0 sm:h-[min(48vh,480px)] sm:max-w-[320px] lg:mx-0 lg:h-[clamp(520px,calc(100svh-86px-110px),782px)] lg:max-w-[392px]"

  if (reduceMotion) {
    return (
      <div className={frameClass}>
        <PhoneImage
          step={
            HOW_IT_WORKS_GUEST_FORM_STEPS[clampIndex(activeIndex)] ??
            HOW_IT_WORKS_GUEST_FORM_STEPS[0]
          }
          zIndex={1}
        />
      </div>
    )
  }

  const settledIndex =
    incomingIndex != null && paintProgress >= 0.999
      ? incomingIndex
      : baseIndex
  const baseStep =
    HOW_IT_WORKS_GUEST_FORM_STEPS[baseIndex] ?? HOW_IT_WORKS_GUEST_FORM_STEPS[0]
  const incomingStep =
    incomingIndex != null
      ? (HOW_IT_WORKS_GUEST_FORM_STEPS[incomingIndex] ??
        HOW_IT_WORKS_GUEST_FORM_STEPS[0])
      : null

  if (incomingStep == null || paintProgress >= 0.999) {
    const step =
      HOW_IT_WORKS_GUEST_FORM_STEPS[settledIndex] ??
      HOW_IT_WORKS_GUEST_FORM_STEPS[0]
    return (
      <div className={frameClass}>
        <PhoneImage step={step} zIndex={1} />
      </div>
    )
  }

  return (
    <div className={frameClass}>
      <PhoneImage step={baseStep} zIndex={1} />
      <PhoneImage
        step={incomingStep}
        zIndex={2}
        clipPath={
          direction === "down"
            ? clipTopToBottom(paintProgress)
            : clipBottomToTop(paintProgress)
        }
      />
    </div>
  )
}

function SectionIntro() {
  return (
    <header
      className={cn(
        "flex w-full flex-col items-center",
        HOW_IT_WORKS_SECTION_INSET,
        "pt-12.5 lg:pt-[70px]",
      )}
    >
      {/* Figma intro `5887:20595`: title box 550px, body 456px, centred. */}
      <div className="flex w-full max-w-[550px] flex-col items-center gap-[22px] text-center">
        <div className="flex w-full flex-col items-center gap-3.5">
          <p className={cn(HOW_IT_WORKS_SECTION_EYEBROW, "font-medium")}>
            {HOW_IT_WORKS_GUEST_FORM.eyebrow}
          </p>
          <h2
            id="how-it-works-guest-form-heading"
            className={cn("m-0 w-full", HOW_IT_WORKS_SECTION_HEADING)}
          >
            {HOW_IT_WORKS_GUEST_FORM.title}
          </h2>
        </div>
        <p className={cn("m-0 w-full max-w-[456px]", HOW_IT_WORKS_SECTION_BODY)}>
          {HOW_IT_WORKS_GUEST_FORM.body}
        </p>
      </div>
    </header>
  )
}

function StickyStepsPanel({
  activeIndex,
  onSelect,
  reduceMotion,
  panelRef,
}: {
  activeIndex: number
  onSelect: (index: number) => void
  reduceMotion: boolean | null
  panelRef: RefObject<HTMLDivElement | null>
}) {
  /** WAI-ARIA tabs: arrows / Home / End move focus and select. */
  const onTabKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    let next: number
    switch (event.key) {
      case "ArrowDown":
      case "ArrowRight":
        next = clampIndex(activeIndex + 1)
        break
      case "ArrowUp":
      case "ArrowLeft":
        next = clampIndex(activeIndex - 1)
        break
      case "Home":
        next = 0
        break
      case "End":
        next = STEP_COUNT - 1
        break
      default:
        return
    }
    event.preventDefault()
    const target = HOW_IT_WORKS_GUEST_FORM_STEPS[next]
    if (target == null) {
      return
    }
    onSelect(next)
    document
      .getElementById(`how-it-works-guest-form-tab-${target.id}`)
      ?.focus({ preventScroll: true })
  }

  return (
    <div
      ref={panelRef}
      className={cn(
        "flex w-full flex-col items-center gap-5 bg-[#f0f0f0] sm:gap-8 lg:flex-row lg:items-center lg:justify-center lg:gap-[70px]",
        "px-5 lg:px-[60px]",
        "py-5 lg:pt-[70px] lg:pb-10",
      )}
    >
      {/*
        Mobile: phone first so the paint stays in view while sticky.
        Desktop: steps left, phone right (Figma).
      */}
      <div className="order-1 w-full max-w-[615px] lg:order-2 lg:w-auto lg:max-w-none">
        <PhoneVisual activeIndex={activeIndex} reduceMotion={reduceMotion} />
      </div>

      <div
        role="tablist"
        aria-label="Guest Form steps"
        aria-orientation="vertical"
        onKeyDown={onTabKeyDown}
        className="order-2 flex w-full max-w-[615px] flex-col gap-4 sm:gap-5 lg:order-1 lg:gap-[34px]"
      >
        {HOW_IT_WORKS_GUEST_FORM_STEPS.map((entry, index) => (
          <StepRow
            key={entry.id}
            step={entry}
            active={index === activeIndex}
            reduceMotion={reduceMotion}
            onSelect={() => {
              onSelect(index)
            }}
          />
        ))}
      </div>
    </div>
  )
}

/**
 * Figma How it works Guest Form `5887:20699` (+ variants `5887:20694`–`5887:20697`).
 * Sticky scroll-hijack when the panel is vertically centred (all viewports);
 * phone paints on each 01–04 step change (not scroll-scrubbed).
 */
export function HowItWorksGuestForm() {
  const reduceMotion = useReducedMotion()
  const trackRef = useRef<HTMLDivElement>(null)
  const panelRef = useRef<HTMLDivElement | null>(null)

  const [panelHeightPx, setPanelHeightPx] = useState(FALLBACK_PANEL_HEIGHT_PX)
  const [stickyTopPx, setStickyTopPx] = useState(() =>
    typeof window !== "undefined"
      ? stickyTopForViewport(window.innerHeight, FALLBACK_PANEL_HEIGHT_PX)
      : HEADER_CLEARANCE_PX,
  )
  const [activeIndex, setActiveIndex] = useState(0)

  const trackHeightPx = panelHeightPx * STEP_COUNT

  /** Measured scroll geometry of the sticky track (live DOM, not state). */
  const measureTrack = () => {
    const track = trackRef.current
    if (track == null) {
      return null
    }
    const stickyEl = track.firstElementChild
    const scrollable = track.offsetHeight - panelHeightPx
    const stickyTop =
      stickyEl instanceof HTMLElement
        ? Number.parseFloat(getComputedStyle(stickyEl).top) || stickyTopPx
        : stickyTopPx
    return { track, scrollable, stickyTop }
  }

  const syncFromScroll = useEffectEvent(() => {
    const geometry = measureTrack()
    if (geometry == null || geometry.scrollable <= 0) {
      return
    }
    const rect = geometry.track.getBoundingClientRect()
    const scrolled = Math.max(
      0,
      Math.min(geometry.scrollable, geometry.stickyTop - rect.top),
    )
    const next = clampIndex(
      Math.floor((scrolled / geometry.scrollable) * STEP_COUNT),
    )
    setActiveIndex((prev) => (prev === next ? prev : next))
  })

  /**
   * Panel height drives the track height. The panel grows and shrinks with the
   * active step's copy, so the track uses the tallest height seen at the current
   * width. A track that follows the active step would move the scroll position
   * and re-select a step in a loop. The observer callback is the only setter.
   */
  useEffect(() => {
    const panel = panelRef.current
    if (panel == null || typeof ResizeObserver === "undefined") {
      return
    }
    let widthPx = 0
    let tallestPx = 0
    const ro = new ResizeObserver(() => {
      const rect = panel.getBoundingClientRect()
      const width = Math.round(rect.width)
      const height = Math.ceil(rect.height)
      if (height <= 0) {
        return
      }
      tallestPx = width === widthPx ? Math.max(tallestPx, height) : height
      widthPx = width
      setPanelHeightPx((prev) => (prev === tallestPx ? prev : tallestPx))
      setStickyTopPx((prev) => {
        const next = stickyTopForViewport(window.innerHeight, tallestPx)
        return prev === next ? prev : next
      })
    })
    ro.observe(panel)
    return () => {
      ro.disconnect()
    }
  }, [])

  useEffect(() => {
    const onResize = () => {
      const next = stickyTopForViewport(window.innerHeight, panelHeightPx)
      setStickyTopPx((prev) => (prev === next ? prev : next))
    }
    onResize()
    const frame = window.requestAnimationFrame(syncFromScroll)
    window.addEventListener("scroll", syncFromScroll, { passive: true })
    window.addEventListener("resize", onResize)
    return () => {
      window.cancelAnimationFrame(frame)
      window.removeEventListener("scroll", syncFromScroll)
      window.removeEventListener("resize", onResize)
    }
  }, [panelHeightPx, stickyTopPx, trackHeightPx])

  /** Scroll to the middle of the step's segment so the floor in `syncFromScroll` lands on it. */
  const scrollToIndex = (index: number) => {
    const next = clampIndex(index)
    setActiveIndex(next)
    const geometry = measureTrack()
    if (geometry == null || geometry.scrollable <= 0) {
      return
    }
    const segment = geometry.scrollable / STEP_COUNT
    const trackTop = window.scrollY + geometry.track.getBoundingClientRect().top
    window.scrollTo({
      top: trackTop - geometry.stickyTop + (next + 0.5) * segment,
      behavior: reduceMotion ? "auto" : "smooth",
    })
  }

  return (
    <section
      className="w-full bg-[#f0f0f0]"
      aria-labelledby="how-it-works-guest-form-heading"
    >
      <SectionIntro />
      <div
        ref={trackRef}
        className="relative w-full"
        style={{ height: trackHeightPx }}
      >
        <div className="sticky w-full" style={{ top: stickyTopPx }}>
          <StickyStepsPanel
            panelRef={panelRef}
            activeIndex={activeIndex}
            onSelect={scrollToIndex}
            reduceMotion={reduceMotion}
          />
        </div>
      </div>
    </section>
  )
}

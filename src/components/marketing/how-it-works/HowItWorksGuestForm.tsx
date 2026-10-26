import {
  useEffect,
  useEffectEvent,
  useRef,
  useState,
  type CSSProperties,
} from "react"
import {
  AnimatePresence,
  animate,
  motion,
  useReducedMotion,
} from "framer-motion"

import {
  HOW_IT_WORKS_GUEST_FORM,
  HOW_IT_WORKS_GUEST_FORM_STEPS,
  HOW_IT_WORKS_SECTION_BODY,
  HOW_IT_WORKS_SECTION_HEADING,
  HOW_IT_WORKS_SECTION_INSET,
  type HowItWorksGuestFormStep,
} from "@/content/marketing/howItWorksPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

const STEP_COUNT = HOW_IT_WORKS_GUEST_FORM_STEPS.length
/** Accordion copy expand only — phone has no fade/slide. */
const COPY_MS = 0.28
/** Phone paint wipe on each step change (not scroll-scrubbed). */
const PAINT_MS = 0.45
/** Figma active-step green progress under the divider (~200px). */
const PROGRESS_WIDTH_PX = 200
const PROGRESS_GREEN = "#248a44"
/** Minimum clearance under sticky marketing header chrome. */
const HEADER_CLEARANCE_PX = 86
/** Fallback sticky row height until measured (tabs + phone). */
const FALLBACK_PANEL_HEIGHT_PX = 820

const STEP_IMAGES = Object.fromEntries(
  HOW_IT_WORKS_GUEST_FORM_STEPS.map((step) => [
    step.id,
    remoteSimpleImg(step.mediaId),
  ]),
) as Record<
  (typeof HOW_IT_WORKS_GUEST_FORM_STEPS)[number]["id"],
  ReturnType<typeof remoteSimpleImg>
>

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
    <div className="relative flex w-full flex-col gap-3 lg:gap-[26px]">
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
            "m-0 w-full cursor-pointer border-0 bg-transparent p-0 text-left font-sans text-sm font-medium leading-[normal] sm:text-base lg:text-lg",
            "transition-colors duration-200 ease-out motion-reduce:transition-none",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2 focus-visible:ring-offset-[#f0f0f0]",
            active ? "text-black" : "text-[#7c7c7c]",
          )}
        >
          {step.label}
        </button>

        <AnimatePresence initial={false}>
          {active ? (
            <motion.div
              key={`${step.id}-copy`}
              id={`how-it-works-guest-form-panel-${step.id}`}
              role="tabpanel"
              aria-labelledby={`how-it-works-guest-form-tab-${step.id}`}
              initial={
                reduceMotion ? false : { opacity: 0, height: 0, marginTop: 0 }
              }
              animate={{ opacity: 1, height: "auto", marginTop: 14 }}
              exit={
                reduceMotion
                  ? undefined
                  : { opacity: 0, height: 0, marginTop: 0 }
              }
              transition={{
                duration: reduceMotion ? 0 : COPY_MS,
                ease: "easeOut",
              }}
              className="w-full overflow-hidden"
            >
              <div className="flex w-full flex-col gap-2.5 lg:gap-3.5">
                <h3 className="m-0 max-w-[615px] font-jakarta text-[22px] font-medium leading-[normal] text-black sm:text-[28px] lg:text-[42px]">
                  {step.title}
                </h3>
                <p
                  className={cn(
                    "m-0 max-w-[548px]",
                    HOW_IT_WORKS_SECTION_BODY,
                  )}
                >
                  {step.body}
                </p>
              </div>
            </motion.div>
          ) : null}
        </AnimatePresence>
      </div>

      <div className="relative h-px w-full bg-[#d0d0d0]" aria-hidden>
        {active ? (
          <motion.span
            layoutId="how-it-works-guest-form-progress"
            className="absolute inset-y-0 left-0 h-px"
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
  const image = STEP_IMAGES[step.id]
  const style: CSSProperties = {
    zIndex,
    ...(clipPath != null ? { clipPath, WebkitClipPath: clipPath } : null),
  }

  return (
    <img
      src={image.src}
      srcSet={image.srcSet}
      sizes="(min-width: 1024px) 392px, min(100vw - 40px, 392px)"
      alt={step.imageAlt}
      width={image.width}
      height={image.height}
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
      paintedIndexRef.current = to
      paintProgressRef.current = 1
      setBaseIndex(to)
      setIncomingIndex(null)
      setPaintProgress(1)
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

  const frameClass =
    "relative mx-auto aspect-[392/782] h-[min(38vh,360px)] w-auto max-w-[280px] shrink-0 sm:h-[min(48vh,480px)] sm:max-w-[320px] lg:mx-0 lg:h-[min(70vh,782px)] lg:max-w-[392px]"

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
      {/*
        Figma intro is 615px / body 576px centred.
        Keep inset on the outer shell so padding does not shrink the 615px box.
      */}
      <div className="flex w-full max-w-[615px] flex-col items-center gap-[18px] text-center">
        <h2
          id="how-it-works-guest-form-heading"
          className={cn("m-0 w-full", HOW_IT_WORKS_SECTION_HEADING)}
        >
          {HOW_IT_WORKS_GUEST_FORM.title}
        </h2>
        <p className={cn("m-0 w-full max-w-[576px]", HOW_IT_WORKS_SECTION_BODY)}>
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
  onPanelNode,
}: {
  activeIndex: number
  onSelect: (index: number) => void
  reduceMotion: boolean | null
  onPanelNode?: (node: HTMLDivElement | null) => void
}) {
  return (
    <div
      ref={onPanelNode}
      className={cn(
        "flex w-full flex-col items-center gap-5 bg-[#f0f0f0] sm:gap-8 lg:flex-row lg:items-center lg:justify-center lg:gap-[70px]",
        "px-5 lg:px-[60px]",
        "py-5 lg:py-10",
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
        className="order-2 flex w-full max-w-[615px] flex-col gap-3 sm:gap-5 lg:order-1 lg:gap-[34px]"
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
 * Figma How it works Guest Form —
 * `5887:20697` (+ variants).
 * Sticky scroll-hijack when the panel is vertically centred (all viewports);
 * phone paints on each 01–05 step change (not scroll-scrubbed).
 */
export function HowItWorksGuestForm() {
  const reduceMotion = useReducedMotion()
  const trackRef = useRef<HTMLDivElement>(null)
  const panelNodeRef = useRef<HTMLDivElement | null>(null)
  const resizeObserverRef = useRef<ResizeObserver | null>(null)

  const [panelHeightPx, setPanelHeightPx] = useState(FALLBACK_PANEL_HEIGHT_PX)
  const [stickyTopPx, setStickyTopPx] = useState(() =>
    typeof window !== "undefined"
      ? stickyTopForViewport(window.innerHeight, FALLBACK_PANEL_HEIGHT_PX)
      : HEADER_CLEARANCE_PX,
  )
  const [activeIndex, setActiveIndex] = useState(0)

  const trackHeightPx = panelHeightPx * STEP_COUNT

  const applyPanelHeight = useEffectEvent((height: number) => {
    if (height > 0) {
      setPanelHeightPx((prev) => {
        const next = Math.ceil(height)
        return prev === next ? prev : next
      })
    }
  })

  const onPanelNode = useEffectEvent((node: HTMLDivElement | null) => {
    resizeObserverRef.current?.disconnect()
    resizeObserverRef.current = null
    panelNodeRef.current = node
    if (node == null || typeof ResizeObserver === "undefined") {
      return
    }
    applyPanelHeight(node.getBoundingClientRect().height)
    const ro = new ResizeObserver((entries) => {
      const entry = entries[0]
      if (entry == null) {
        return
      }
      const borderBox = entry.borderBoxSize?.[0]?.blockSize
      const height =
        borderBox != null && borderBox > 0
          ? borderBox
          : entry.target instanceof HTMLElement
            ? entry.target.getBoundingClientRect().height
            : entry.contentRect.height
      applyPanelHeight(height)
    })
    ro.observe(node)
    resizeObserverRef.current = ro
  })

  const syncFromScroll = useEffectEvent(() => {
    const track = trackRef.current
    if (track == null) {
      return
    }
    const stickyEl = track.firstElementChild
    const panelEl = panelNodeRef.current
    const panelH =
      panelEl?.getBoundingClientRect().height ||
      stickyEl?.getBoundingClientRect().height ||
      panelHeightPx
    const trackH = track.offsetHeight || trackHeightPx
    const scrollable = trackH - panelH
    if (scrollable <= 0) {
      return
    }
    const stickyTop =
      stickyEl instanceof HTMLElement
        ? Number.parseFloat(getComputedStyle(stickyEl).top) || stickyTopPx
        : stickyTopPx
    const rect = track.getBoundingClientRect()
    const scrolled = Math.max(
      0,
      Math.min(scrollable, stickyTop - rect.top),
    )
    const next = clampIndex(
      Math.floor((scrolled / scrollable) * STEP_COUNT),
    )
    setActiveIndex((prev) => (prev === next ? prev : next))
  })

  useEffect(() => {
    return () => {
      resizeObserverRef.current?.disconnect()
      resizeObserverRef.current = null
    }
  }, [])

  useEffect(() => {
    const onResize = () => {
      const panelH =
        panelNodeRef.current?.getBoundingClientRect().height || panelHeightPx
      setStickyTopPx(stickyTopForViewport(window.innerHeight, panelH))
    }
    onResize()
    syncFromScroll()
    window.addEventListener("scroll", syncFromScroll, { passive: true })
    window.addEventListener("resize", onResize)
    return () => {
      window.removeEventListener("scroll", syncFromScroll)
      window.removeEventListener("resize", onResize)
    }
  }, [panelHeightPx, stickyTopPx, trackHeightPx])

  const scrollToIndex = (index: number) => {
    const next = clampIndex(index)
    setActiveIndex(next)
    const track = trackRef.current
    if (track == null) {
      return
    }
    const scrollable = trackHeightPx - panelHeightPx
    if (scrollable <= 0) {
      return
    }
    const step = scrollable / STEP_COUNT
    const trackTop = window.scrollY + track.getBoundingClientRect().top
    window.scrollTo({
      top: trackTop - stickyTopPx + next * step,
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
            onPanelNode={onPanelNode}
            activeIndex={activeIndex}
            onSelect={scrollToIndex}
            reduceMotion={reduceMotion}
          />
        </div>
      </div>
    </section>
  )
}

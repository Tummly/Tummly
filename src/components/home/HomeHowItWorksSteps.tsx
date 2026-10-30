import {
  useCallback,
  useEffect,
  useEffectEvent,
  useRef,
  useState,
  type CSSProperties,
  type KeyboardEvent,
} from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"
import { Link } from "react-router-dom"

import howConnectImage from "@/assets/images/marketing/home/how-connect.webp"
import howInviteBackImage from "@/assets/images/marketing/home/how-invite-back.png"
import howKnowGuestsImage from "@/assets/images/marketing/home/how-know-guests.png"
import howUnderstandResponseImage from "@/assets/images/marketing/home/how-understand-response.png"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  HOME_HOW_IT_WORKS_STEPS,
  type HomeHowItWorksStep,
} from "@/content/marketing/homePage"
import { cn } from "@/lib/utils"

const STEP_COUNT = HOME_HOW_IT_WORKS_STEPS.length
/** Fallback until measured: tabs 48 + gap 30 + media 600 + padding 80. */
const FALLBACK_PANEL_HEIGHT_PX = 758
const CROSSFADE_MS = 0.25
/** Figma media row: every state sits in a 600px-high frame; the widest is 766px. */
const MEDIA_BOX_WIDTH_PX = 766
const MEDIA_BOX_HEIGHT_PX = 600
/** Release the tab-click scroll lock if the browser never reports arrival. */
const SCROLL_LOCK_MS = 1500
const STEP_PANEL_ID = "home-how-step-panel"
/** Minimum clearance under sticky marketing header chrome. */
const HEADER_CLEARANCE_PX = 86
/** Left copy + right visual share the same step reveal (fade up from bottom). */
const STEP_REVEAL = {
  initial: { opacity: 0, y: 8 },
  animate: { opacity: 1, y: 0 },
  exit: { opacity: 0, y: -8 },
} as const

/**
 * Media per tab state, exported from Figma at 2×:
 * `5339:30056` (photo), `4161:19167`, `4161:19210`, `4161:19253` (product UI).
 * Widths follow the frames: 750px, except the Guests screen (766px). The
 * visual box is always 766×600 so every tab yields the same panel height.
 */
const STEP_MEDIA = {
  connect: { src: howConnectImage, width: 750 },
  /**
   * Figma `4161:19167` carries a VIDEO fill (hash `a282565a…`) that the
   * API cannot export. The still below is the frame's rendered export
   * (Guests table + guest drawer). Video source is pending: swap this still
   * for a `MarketingLoopVideo` once the file is published to Blob.
   */
  know: { src: howKnowGuestsImage, width: 766 },
  invite: { src: howInviteBackImage, width: 750 },
  understand: { src: howUnderstandResponseImage, width: 750 },
} as const satisfies Record<
  HomeHowItWorksStep["id"],
  { src: string; width: number }
>

function stickyTopForViewport(viewportHeight: number, panelHeight: number) {
  const available = viewportHeight - HEADER_CLEARANCE_PX
  if (panelHeight >= available) {
    return HEADER_CLEARANCE_PX
  }
  return HEADER_CLEARANCE_PX + (available - panelHeight) / 2
}

function clampIndex(index: number) {
  return Math.max(0, Math.min(STEP_COUNT - 1, index))
}

function StepTabs({
  activeIndex,
  onSelect,
}: {
  activeIndex: number
  onSelect: (index: number) => void
}) {
  const tabRefs = useRef<(HTMLButtonElement | null)[]>([])

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const focusedIndex = tabRefs.current.findIndex(
      (node) => node === document.activeElement,
    )
    const current = focusedIndex >= 0 ? focusedIndex : activeIndex
    let next: number
    switch (event.key) {
      case "ArrowRight":
        next = (current + 1) % STEP_COUNT
        break
      case "ArrowLeft":
        next = (current - 1 + STEP_COUNT) % STEP_COUNT
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
    onSelect(next)
    tabRefs.current[next]?.focus()
  }

  return (
    <div
      role="tablist"
      aria-label="How it works steps"
      onKeyDown={onKeyDown}
      className="flex w-full gap-[10px] overflow-x-auto [-ms-overflow-style:none] [scrollbar-width:none] lg:gap-[18px] [&::-webkit-scrollbar]:hidden"
    >
      {HOME_HOW_IT_WORKS_STEPS.map((step, index) => {
        const active = index === activeIndex
        return (
          <button
            key={step.id}
            ref={(node) => {
              tabRefs.current[index] = node
            }}
            type="button"
            role="tab"
            id={`home-how-step-tab-${step.id}`}
            aria-selected={active}
            aria-controls={active ? STEP_PANEL_ID : undefined}
            tabIndex={active ? 0 : -1}
            onClick={() => {
              onSelect(index)
            }}
            className={cn(
              "shrink-0 rounded-[4px] border-0 px-4 py-3 text-left font-sans text-sm leading-5 text-black",
              "cursor-pointer transition-colors duration-200 ease-out motion-reduce:transition-none",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2",
              "lg:flex-1 lg:px-[26px] lg:py-3.5",
              active ? "bg-[#dfebe3]" : "bg-[#f0f0f0]",
            )}
          >
            {step.tabLabel}
          </button>
        )
      })}
    </div>
  )
}

function StepCopy({
  step,
  reduceMotion,
}: {
  step: HomeHowItWorksStep
  reduceMotion: boolean | null
}) {
  return (
    <div className="relative w-full pt-0 lg:min-w-0 lg:flex-1 lg:pt-10">
      <AnimatePresence initial={false} mode="wait">
        <motion.div
          key={step.id}
          initial={reduceMotion ? false : STEP_REVEAL.initial}
          animate={STEP_REVEAL.animate}
          exit={reduceMotion ? undefined : STEP_REVEAL.exit}
          transition={{
            duration: reduceMotion ? 0 : CROSSFADE_MS,
            ease: "easeOut",
          }}
          className="flex w-full flex-col items-start gap-10 text-[#141414] lg:gap-[52px]"
        >
          <div className="flex w-full flex-col gap-[18px]">
            <h3 className="m-0 w-full max-w-[400px] font-jakarta text-[28px] font-medium leading-[normal] text-balance lg:text-[36px]">
              {step.title}
            </h3>
            <p className="m-0 w-full max-w-[440px] font-sans text-base font-normal leading-5">
              {step.body}
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-x-[22px] gap-y-3">
            <MarketingButton variant="primary" asChild>
              <RequestTrialLink planIntent={{ plan: "Pilot" }}>
                {step.primaryLabel}
              </RequestTrialLink>
            </MarketingButton>
            <MarketingButton variant="secondary" asChild>
              <Link to={step.secondaryTo}>{step.secondaryLabel}</Link>
            </MarketingButton>
          </div>
        </motion.div>
      </AnimatePresence>
    </div>
  )
}

function StepVisual({
  step,
  reduceMotion,
}: {
  step: HomeHowItWorksStep
  reduceMotion: boolean | null
}) {
  const media = STEP_MEDIA[step.id]

  /* The outer box keeps one fixed ratio for every tab, so the panel height
   * never depends on the active step (a per-tab height fed the scroll sync
   * back into itself). The frame inside is right-aligned like the Figma row. */
  return (
    <div
      className="relative w-full lg:w-[766px] lg:max-w-[58%] lg:shrink-0"
      style={
        {
          aspectRatio: `${MEDIA_BOX_WIDTH_PX} / ${MEDIA_BOX_HEIGHT_PX}`,
          "--step-media-share": `${(media.width / MEDIA_BOX_WIDTH_PX) * 100}%`,
        } as CSSProperties
      }
    >
      <AnimatePresence initial={false} mode="wait">
        <motion.div
          key={step.id}
          initial={reduceMotion ? false : STEP_REVEAL.initial}
          animate={STEP_REVEAL.animate}
          exit={reduceMotion ? undefined : STEP_REVEAL.exit}
          transition={{
            duration: reduceMotion ? 0 : CROSSFADE_MS,
            ease: "easeOut",
          }}
          className="absolute inset-0 flex justify-end"
        >
          <div className="relative h-full w-full overflow-hidden rounded-[12px] bg-[#e2eae5] lg:w-(--step-media-share)">
            <img
              src={media.src}
              alt={step.imageAlt}
              width={media.width}
              height={MEDIA_BOX_HEIGHT_PX}
              className="absolute inset-0 size-full object-cover object-center"
              draggable={false}
              decoding="async"
            />
          </div>
        </motion.div>
      </AnimatePresence>
    </div>
  )
}

function PanelChrome({
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
  const step =
    HOME_HOW_IT_WORKS_STEPS[activeIndex] ?? HOME_HOW_IT_WORKS_STEPS[0]

  return (
    <div
      ref={onPanelNode}
      className={cn(
        "flex w-full flex-col gap-[30px] bg-white",
        "px-5 lg:px-[60px]",
        "py-8 lg:py-10",
      )}
      role="region"
      aria-label="How it works steps"
    >
      <StepTabs activeIndex={activeIndex} onSelect={onSelect} />

      <div
        id={STEP_PANEL_ID}
        role="tabpanel"
        aria-labelledby={`home-how-step-tab-${step.id}`}
        className="flex w-full flex-col gap-10 lg:flex-row lg:items-start lg:justify-between lg:gap-12"
      >
        <StepCopy step={step} reduceMotion={reduceMotion} />
        <StepVisual step={step} reduceMotion={reduceMotion} />
      </div>
    </div>
  )
}

/**
 * Home How it works steps — Figma `5754:22093`; tab states `5339:30041`,
 * `4161:19285`, `4161:19284`, `4161:19283`.
 * Sticky scroll-driven active step on all viewports (same pattern as
 * How it works page). Tabs also jump the track.
 */
export function HomeHowItWorksSteps() {
  const reduceMotion = useReducedMotion()
  const trackRef = useRef<HTMLDivElement>(null)
  const panelNodeRef = useRef<HTMLDivElement | null>(null)
  const resizeObserverRef = useRef<ResizeObserver | null>(null)
  const measuredWidthRef = useRef<number | null>(null)
  const scrollLockRef = useRef<{ index: number; until: number } | null>(null)

  const [panelHeightPx, setPanelHeightPx] = useState(FALLBACK_PANEL_HEIGHT_PX)
  const [stickyTopPx, setStickyTopPx] = useState(() =>
    typeof window !== "undefined"
      ? stickyTopForViewport(window.innerHeight, FALLBACK_PANEL_HEIGHT_PX)
      : HEADER_CLEARANCE_PX,
  )
  const [activeIndex, setActiveIndex] = useState(0)

  const trackHeightPx = panelHeightPx * STEP_COUNT

  /*
   * The panel height only grows while the panel width stays the same. The
   * track height is derived from it and the active step is derived from the
   * scroll position, so a height that moved with the active step could
   * re-trigger itself. A width change (resize) starts a fresh measurement.
   */
  const applyPanelHeight = useCallback((height: number, width: number) => {
    if (height <= 0) {
      return
    }
    const next = Math.ceil(height)
    const widthChanged =
      measuredWidthRef.current == null ||
      Math.abs(measuredWidthRef.current - width) >= 1
    measuredWidthRef.current = width
    setPanelHeightPx((prev) => {
      if (widthChanged) {
        return prev === next ? prev : next
      }
      return next > prev ? next : prev
    })
  }, [])

  const onPanelNode = useCallback(
    (node: HTMLDivElement | null) => {
      resizeObserverRef.current?.disconnect()
      resizeObserverRef.current = null
      panelNodeRef.current = node
      measuredWidthRef.current = null
      if (node == null || typeof ResizeObserver === "undefined") {
        return
      }
      const initial = node.getBoundingClientRect()
      applyPanelHeight(initial.height, initial.width)
      const ro = new ResizeObserver((entries) => {
        const entry = entries[0]
        if (entry == null || !(entry.target instanceof HTMLElement)) {
          return
        }
        const rect = entry.target.getBoundingClientRect()
        applyPanelHeight(rect.height, rect.width)
      })
      ro.observe(node)
      resizeObserverRef.current = ro
    },
    [applyPanelHeight],
  )

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
    const scrolled = Math.max(0, Math.min(scrollable, stickyTop - rect.top))
    const next = clampIndex(Math.floor((scrolled / scrollable) * STEP_COUNT))
    const lock = scrollLockRef.current
    if (lock != null) {
      // A tab click is scrolling to its step: ignore the steps passed on the way.
      if (next !== lock.index && performance.now() < lock.until) {
        return
      }
      scrollLockRef.current = null
    }
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
    scrollLockRef.current = {
      index: next,
      until: performance.now() + SCROLL_LOCK_MS,
    }
    // Aim at the middle of the step so rounding never lands on a neighbour.
    window.scrollTo({
      top: trackTop - stickyTopPx + (next + 0.5) * step,
      behavior: reduceMotion ? "auto" : "smooth",
    })
  }

  return (
    <section className="w-full bg-white">
      <div
        ref={trackRef}
        className="relative w-full"
        style={{ height: trackHeightPx }}
      >
        <div className="sticky w-full" style={{ top: stickyTopPx }}>
          <PanelChrome
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

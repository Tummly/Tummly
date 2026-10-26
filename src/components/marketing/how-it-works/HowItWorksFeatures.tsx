import {
  useEffect,
  useEffectEvent,
  useRef,
  useState,
} from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"

import {
  HOW_IT_WORKS_FEATURES,
  HOW_IT_WORKS_SECTION_BODY,
  type HowItWorksFeature,
} from "@/content/marketing/howItWorksPage"
import { cn } from "@/lib/utils"

const STEP_COUNT = HOW_IT_WORKS_FEATURES.length
/** Fallback until measured (tabs + visual + padding). */
const FALLBACK_PANEL_HEIGHT_PX = 820
const CROSSFADE_MS = 0.25
/** Minimum clearance under sticky marketing header chrome. */
const HEADER_CLEARANCE_PX = 86

const STEP_REVEAL = {
  initial: { opacity: 0, y: 8 },
  animate: { opacity: 1, y: 0 },
  exit: { opacity: 0, y: -8 },
} as const

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

function FeatureTabs({
  activeIndex,
  onSelect,
}: {
  activeIndex: number
  onSelect: (index: number) => void
}) {
  return (
    <div
      role="tablist"
      aria-label="Product areas"
      className="flex w-full gap-2 overflow-x-auto [-ms-overflow-style:none] [scrollbar-width:none] sm:gap-2.5 lg:gap-[18px] [&::-webkit-scrollbar]:hidden"
    >
      {HOW_IT_WORKS_FEATURES.map((feature, index) => {
        const active = index === activeIndex
        return (
          <button
            key={feature.id}
            type="button"
            role="tab"
            id={`how-it-works-feature-tab-${feature.id}`}
            aria-selected={active}
            aria-controls={`how-it-works-feature-panel-${feature.id}`}
            tabIndex={active ? 0 : -1}
            onClick={() => {
              onSelect(index)
            }}
            className={cn(
              "shrink-0 rounded-[4px] border-0 px-3 py-2.5 text-left font-sans text-xs leading-4 text-black sm:px-4 sm:py-3 sm:text-sm sm:leading-5",
              "cursor-pointer transition-colors duration-200 ease-out motion-reduce:transition-none",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2",
              "lg:flex-1 lg:px-[26px] lg:py-3.5",
              !active && "bg-[#f0f0f0]",
            )}
            style={active ? { backgroundColor: feature.accent } : undefined}
          >
            {feature.tabLabel}
          </button>
        )
      })}
    </div>
  )
}

function FeatureCopy({
  feature,
  reduceMotion,
}: {
  feature: HowItWorksFeature
  reduceMotion: boolean | null
}) {
  return (
    <div className="relative w-full max-w-[606px]">
      <AnimatePresence initial={false} mode="wait">
        <motion.div
          key={feature.id}
          initial={reduceMotion ? false : STEP_REVEAL.initial}
          animate={STEP_REVEAL.animate}
          exit={reduceMotion ? undefined : STEP_REVEAL.exit}
          transition={{
            duration: reduceMotion ? 0 : CROSSFADE_MS,
            ease: "easeOut",
          }}
          className="flex w-full flex-col gap-3 text-[#141414] sm:gap-[18px]"
        >
          <h3 className="m-0 w-full max-w-[593px] font-jakarta text-[22px] font-medium leading-[normal] sm:text-[28px] lg:text-[42px]">
            {feature.title}
          </h3>
          {feature.body.map((paragraph) => (
            <p
              key={paragraph}
              className={cn("m-0 w-full max-w-[532px]", HOW_IT_WORKS_SECTION_BODY)}
            >
              {paragraph}
            </p>
          ))}
        </motion.div>
      </AnimatePresence>
    </div>
  )
}

function FeatureVisual({
  feature,
  reduceMotion,
}: {
  feature: HowItWorksFeature
  reduceMotion: boolean | null
}) {
  return (
    <div className="relative w-full lg:w-[min(100%,758px)] lg:shrink-0">
      <AnimatePresence initial={false} mode="wait">
        <motion.div
          key={feature.id}
          initial={reduceMotion ? false : STEP_REVEAL.initial}
          animate={STEP_REVEAL.animate}
          exit={reduceMotion ? undefined : STEP_REVEAL.exit}
          transition={{
            duration: reduceMotion ? 0 : CROSSFADE_MS,
            ease: "easeOut",
          }}
          className="w-full"
        >
          <div
            className="h-[min(345px,36vh)] max-h-[280px] w-full rounded-[12px] sm:aspect-[758/620] sm:h-auto sm:max-h-[min(42vh,360px)] lg:aspect-auto lg:h-[620px] lg:max-h-none"
            style={{ backgroundColor: feature.mediaBg }}
            aria-hidden
          />
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
  const feature =
    HOW_IT_WORKS_FEATURES[activeIndex] ?? HOW_IT_WORKS_FEATURES[0]

  return (
    <div
      ref={onPanelNode}
      className={cn(
        "flex w-full flex-col gap-5 bg-white sm:gap-[30px] lg:gap-[70px]",
        "px-5 lg:px-[60px]",
        "py-5 sm:pt-8 sm:pb-12.5 lg:pt-10 lg:pb-10",
      )}
      role="region"
      aria-label="Product areas"
    >
      <FeatureTabs activeIndex={activeIndex} onSelect={onSelect} />

      <div
        id={`how-it-works-feature-panel-${feature.id}`}
        role="tabpanel"
        aria-labelledby={`how-it-works-feature-tab-${feature.id}`}
        className="flex w-full flex-col gap-[50px] sm:gap-10 lg:flex-row lg:items-start lg:justify-between lg:gap-12"
      >
        {/* Mobile + desktop: copy then visual (Figma Home mobile). */}
        <div className="lg:min-w-0 lg:flex-1">
          <FeatureCopy feature={feature} reduceMotion={reduceMotion} />
        </div>
        <div className="lg:w-[min(100%,758px)] lg:shrink-0">
          <FeatureVisual feature={feature} reduceMotion={reduceMotion} />
        </div>
      </div>
    </div>
  )
}

/**
 * How it works product areas — Figma `5661:19613` (+ tab variants).
 * Sticky scroll-driven active tab on all viewports; tinted placeholders until media ships.
 */
export function HowItWorksFeatures() {
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
    const next = clampIndex(Math.floor((scrolled / scrollable) * STEP_COUNT))
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

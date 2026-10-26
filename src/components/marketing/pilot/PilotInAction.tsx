import { useEffect, useEffectEvent, useRef, useState } from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"
import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  PILOT_IN_ACTION,
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
  PILOT_SECTION_INSET,
} from "@/content/marketing/pilotPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

const TAB_COUNT = PILOT_IN_ACTION.tabs.length
/** Fallback until measured (tabs + visual + padding). */
const FALLBACK_PANEL_HEIGHT_PX = 834
const CROSSFADE_MS = 0.25
/** Minimum clearance under sticky marketing header chrome. */
const HEADER_CLEARANCE_PX = 86

const TAB_IMAGES = {
  hear: remoteSimpleImg("pilot/in-action-hear"),
  connected: remoteSimpleImg("pilot/in-action-connected"),
  opportunity: remoteSimpleImg("pilot/in-action-opportunity"),
  working: remoteSimpleImg("pilot/in-action-working"),
} as const

function stickyTopForViewport(viewportHeight: number, panelHeight: number) {
  const available = viewportHeight - HEADER_CLEARANCE_PX
  if (panelHeight >= available) {
    return HEADER_CLEARANCE_PX
  }
  return HEADER_CLEARANCE_PX + (available - panelHeight) / 2
}

function clampIndex(index: number) {
  return Math.max(0, Math.min(TAB_COUNT - 1, index))
}

function InActionTabs({
  activeIndex,
  onSelect,
}: {
  activeIndex: number
  onSelect: (index: number) => void
}) {
  const tabs = PILOT_IN_ACTION.tabs

  const moveActive = (delta: number) => {
    onSelect(clampIndex(activeIndex + delta))
  }

  return (
    <div
      role="tablist"
      aria-label="See Tummly in action"
      className="flex w-full gap-2 overflow-x-auto [-ms-overflow-style:none] [scrollbar-width:none] sm:gap-2.5 lg:gap-[18px] [&::-webkit-scrollbar]:hidden"
      onKeyDown={(event) => {
        if (event.key === "ArrowDown" || event.key === "ArrowRight") {
          event.preventDefault()
          moveActive(1)
        } else if (event.key === "ArrowUp" || event.key === "ArrowLeft") {
          event.preventDefault()
          moveActive(-1)
        } else if (event.key === "Home") {
          event.preventDefault()
          onSelect(0)
        } else if (event.key === "End") {
          event.preventDefault()
          onSelect(TAB_COUNT - 1)
        }
      }}
    >
      {tabs.map((tab, index) => {
        const active = index === activeIndex
        return (
          <button
            key={tab.id}
            type="button"
            role="tab"
            id={`pilot-in-action-tab-${tab.id}`}
            aria-selected={active}
            aria-controls="pilot-in-action-panel"
            tabIndex={active ? 0 : -1}
            onClick={() => {
              onSelect(index)
            }}
            className={cn(
              "shrink-0 rounded-[4px] border-0 px-3 py-2.5 text-left font-sans text-xs leading-4 text-black sm:px-4 sm:py-3.5 sm:text-sm sm:leading-5",
              "cursor-pointer transition-colors duration-200 ease-out motion-reduce:transition-none",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2",
              "lg:flex-1 lg:px-[26px]",
              !active && "bg-[#f0f0f0]",
            )}
            style={
              active
                ? { backgroundColor: PILOT_IN_ACTION.activeTabColor }
                : undefined
            }
          >
            {tab.tabLabel}
          </button>
        )
      })}
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
  const activeTab =
    PILOT_IN_ACTION.tabs[activeIndex] ?? PILOT_IN_ACTION.tabs[0]!
  const image = TAB_IMAGES[activeTab.id]

  return (
    <div
      ref={onPanelNode}
      className={cn(
        "flex w-full flex-col items-center gap-[30px] bg-white",
        PILOT_SECTION_INSET,
        "pb-10",
      )}
      role="region"
      aria-label="See Tummly in action"
    >
      <InActionTabs activeIndex={activeIndex} onSelect={onSelect} />

      <div
        id="pilot-in-action-panel"
        role="tabpanel"
        aria-labelledby={`pilot-in-action-tab-${activeTab.id}`}
        className="flex w-full flex-col items-start gap-10 lg:flex-row lg:items-start lg:justify-between"
      >
        <div className="flex w-full max-w-[582px] flex-col items-start gap-[52px] pt-0 lg:pt-10">
          <AnimatePresence initial={false} mode="wait">
            <motion.div
              key={activeTab.id}
              initial={reduceMotion ? false : { opacity: 0, y: 8 }}
              animate={{ opacity: 1, y: 0 }}
              exit={reduceMotion ? undefined : { opacity: 0, y: -8 }}
              transition={{
                duration: reduceMotion ? 0 : CROSSFADE_MS,
                ease: "easeOut",
              }}
              className="flex w-full flex-col items-start gap-[18px] text-[#141414]"
            >
              <h3 className="m-0 max-w-[466px] font-jakarta text-[28px] font-medium leading-[normal] lg:text-[36px]">
                {activeTab.title}
              </h3>
              <p className="m-0 max-w-[541px] font-sans text-base font-normal leading-5">
                {activeTab.body}
              </p>
            </motion.div>
          </AnimatePresence>

          <div className="flex flex-wrap items-center gap-4">
            <MarketingButton asChild variant="primary">
              <RequestTrialLink planIntent={PILOT_IN_ACTION.planIntent}>
                {PILOT_IN_ACTION.primaryLabel}
              </RequestTrialLink>
            </MarketingButton>
            <MarketingButton asChild variant="secondary">
              <Link to={PILOT_IN_ACTION.secondaryTo}>
                {PILOT_IN_ACTION.secondaryLabel}
              </Link>
            </MarketingButton>
          </div>
        </div>

        <div className="relative aspect-[750/600] w-full overflow-hidden rounded-[12px] bg-[#edece8] lg:h-[600px] lg:w-[min(100%,750px)] lg:max-w-[750px] lg:shrink-0 lg:aspect-auto">
          <AnimatePresence initial={false} mode="wait">
            <motion.img
              key={activeTab.id}
              src={image.src}
              srcSet={image.srcSet}
              sizes="(min-width: 1024px) 750px, 100vw"
              alt={activeTab.imageAlt}
              width={750}
              height={600}
              initial={reduceMotion ? false : { opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={reduceMotion ? undefined : { opacity: 0 }}
              transition={{
                duration: reduceMotion ? 0 : CROSSFADE_MS,
                ease: "easeOut",
              }}
              className="absolute inset-0 size-full object-cover object-left-top"
              decoding="async"
            />
          </AnimatePresence>
        </div>
      </div>
    </div>
  )
}

/**
 * Figma Pilot “See Tummly in Action” —
 * `6495:18145` / `6495:18183` / `6495:18223` / `6495:18254`.
 * Sticky scroll-driven active tab on all viewports (same pattern as
 * HomeHowItWorksSteps). Tabs also jump the track.
 */
export function PilotInAction() {
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

  const trackHeightPx = panelHeightPx * TAB_COUNT

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
    const next = clampIndex(Math.floor((scrolled / scrollable) * TAB_COUNT))
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
    const step = scrollable / TAB_COUNT
    const trackTop = window.scrollY + track.getBoundingClientRect().top
    window.scrollTo({
      top: trackTop - stickyTopPx + next * step,
      behavior: reduceMotion ? "auto" : "smooth",
    })
  }

  return (
    <section
      className="w-full bg-white"
      aria-labelledby="pilot-in-action-heading"
    >
      <header
        className={cn(
          "mx-auto flex w-full max-w-[590px] flex-col items-center gap-6 text-center",
          PILOT_SECTION_INSET,
          "pt-[50px] pb-[30px] lg:pt-[70px]",
        )}
      >
        <div className="flex w-full flex-col items-center gap-3.5 text-black">
          <p className="m-0 font-sans text-sm font-medium uppercase leading-[normal] tracking-[1.12px]">
            {PILOT_IN_ACTION.eyebrow}
          </p>
          <h2
            id="pilot-in-action-heading"
            className={cn(
              "m-0 w-full max-w-[584px] text-black",
              PILOT_SECTION_HEADING,
            )}
          >
            {PILOT_IN_ACTION.title}
          </h2>
        </div>
        <p className={cn("m-0 w-full", PILOT_SECTION_BODY)}>
          {PILOT_IN_ACTION.body}
        </p>
      </header>

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

import {
  useEffect,
  useEffectEvent,
  useRef,
  useState,
} from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"
import { Link } from "react-router-dom"

import howBringImage from "@/assets/images/marketing/home/how-bring.png"
import howBuildImage from "@/assets/images/marketing/home/how-build.png"
import howHearImage from "@/assets/images/marketing/home/how-hear.png"
import howPlaceImage from "@/assets/images/marketing/home/how-place.png"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  HOME_HOW_IT_WORKS_STEPS,
  type HomeHowItWorksStep,
} from "@/content/marketing/homePage"
import { marketingChromeContentInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

const STEP_COUNT = HOME_HOW_IT_WORKS_STEPS.length
/** Tabs + gap + 600px visual + vertical padding (Figma `5339:30041`). */
const PANEL_HEIGHT_PX = 758
/** Desktop scroll track — one panel-height step per how-it-works tab. */
const TRACK_HEIGHT_PX = PANEL_HEIGHT_PX * STEP_COUNT
const CROSSFADE_MS = 0.25
/** Minimum clearance under sticky marketing header chrome. */
const HEADER_CLEARANCE_PX = 100
const LG_MQ = "(min-width: 1024px)"

const STEP_IMAGES = {
  place: howPlaceImage,
  hear: howHearImage,
  build: howBuildImage,
  bring: howBringImage,
} as const satisfies Record<HomeHowItWorksStep["id"], string>

function stickyTopForViewport(viewportHeight: number) {
  const centered = (viewportHeight - PANEL_HEIGHT_PX) / 2
  return Math.max(HEADER_CLEARANCE_PX, centered)
}

function clampIndex(index: number) {
  return Math.max(0, Math.min(STEP_COUNT - 1, index))
}

function useIsLgUp() {
  const [isLgUp, setIsLgUp] = useState(() =>
    typeof window !== "undefined" ? window.matchMedia(LG_MQ).matches : false,
  )

  useEffect(() => {
    const mq = window.matchMedia(LG_MQ)
    const onChange = () => {
      setIsLgUp(mq.matches)
    }
    onChange()
    mq.addEventListener("change", onChange)
    return () => {
      mq.removeEventListener("change", onChange)
    }
  }, [])

  return isLgUp
}

function StepTabs({
  activeIndex,
  onSelect,
}: {
  activeIndex: number
  onSelect: (index: number) => void
}) {
  return (
    <div
      role="tablist"
      aria-label="How it works steps"
      className="flex w-full gap-[10px] overflow-x-auto [-ms-overflow-style:none] [scrollbar-width:none] lg:gap-[18px] [&::-webkit-scrollbar]:hidden"
    >
      {HOME_HOW_IT_WORKS_STEPS.map((step, index) => {
        const active = index === activeIndex
        return (
          <button
            key={step.id}
            type="button"
            role="tab"
            id={`home-how-step-tab-${step.id}`}
            aria-selected={active}
            aria-controls={`home-how-step-panel-${step.id}`}
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
    <div className="relative w-full max-w-[542px] pt-0 lg:pt-10">
      <AnimatePresence initial={false} mode="wait">
        <motion.div
          key={step.id}
          initial={reduceMotion ? false : { opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          exit={reduceMotion ? undefined : { opacity: 0, y: -8 }}
          transition={{
            duration: reduceMotion ? 0 : CROSSFADE_MS,
            ease: "easeOut",
          }}
          className="flex w-full flex-col items-start gap-10 text-[#141414] lg:gap-[52px]"
        >
          <div className="flex w-full flex-col gap-[18px]">
            <h3 className="m-0 w-full max-w-[467px] font-jakarta text-[28px] font-medium leading-normal lg:text-[36px]">
              {step.title}
            </h3>
            <p className="m-0 w-full max-w-[542px] font-sans text-base font-normal leading-5">
              {step.body}
            </p>
          </div>
          <MarketingButton variant="secondary" asChild>
            <Link to={step.ctaTo}>{step.ctaLabel}</Link>
          </MarketingButton>
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
  return (
    <div className="relative aspect-[750/600] w-full overflow-hidden rounded-[12px] bg-[#e2eae5] lg:aspect-auto lg:h-[600px] lg:w-[min(100%,750px)] lg:shrink-0">
      <AnimatePresence initial={false} mode="sync">
        <motion.div
          key={step.id}
          initial={reduceMotion ? false : { opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={reduceMotion ? undefined : { opacity: 0 }}
          transition={{
            duration: reduceMotion ? 0 : CROSSFADE_MS,
            ease: "easeInOut",
          }}
          className="absolute inset-0"
        >
          <img
            src={STEP_IMAGES[step.id]}
            alt={step.imageAlt}
            width={750}
            height={600}
            className="absolute inset-0 size-full object-cover"
            draggable={false}
            decoding="async"
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
}: {
  activeIndex: number
  onSelect: (index: number) => void
  reduceMotion: boolean | null
}) {
  const step =
    HOME_HOW_IT_WORKS_STEPS[activeIndex] ?? HOME_HOW_IT_WORKS_STEPS[0]

  return (
    <div
      className={cn(
        "flex w-full flex-col gap-[30px] bg-white",
        marketingChromeContentInset,
        "py-8 lg:py-10",
      )}
      role="region"
      aria-label="How it works steps"
    >
      <StepTabs activeIndex={activeIndex} onSelect={onSelect} />

      <div
        id={`home-how-step-panel-${step.id}`}
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
 * Home How it works scroll-hijack —
 * Figma `5339:30041`, `4161:19285`, `4161:19284`, `4161:19283`.
 * Desktop (lg+): sticky scroll-driven active step. Below lg: tap tabs.
 */
export function HomeHowItWorksSteps() {
  const isLgUp = useIsLgUp()
  const reduceMotion = useReducedMotion()
  const trackRef = useRef<HTMLDivElement>(null)
  const [activeIndex, setActiveIndex] = useState(0)
  const [stickyTopPx, setStickyTopPx] = useState(() =>
    typeof window !== "undefined"
      ? stickyTopForViewport(window.innerHeight)
      : HEADER_CLEARANCE_PX,
  )

  const syncFromScroll = useEffectEvent(() => {
    const track = trackRef.current
    if (track == null) {
      return
    }
    const rect = track.getBoundingClientRect()
    const scrollable = TRACK_HEIGHT_PX - PANEL_HEIGHT_PX
    if (scrollable <= 0) {
      return
    }
    const scrolled = Math.max(
      0,
      Math.min(scrollable, stickyTopPx - rect.top),
    )
    const next = clampIndex(Math.floor((scrolled / scrollable) * STEP_COUNT))
    setActiveIndex((prev) => (prev === next ? prev : next))
  })

  useEffect(() => {
    if (!isLgUp) {
      return
    }
    const onResize = () => {
      setStickyTopPx(stickyTopForViewport(window.innerHeight))
    }
    onResize()
    syncFromScroll()
    window.addEventListener("scroll", syncFromScroll, { passive: true })
    window.addEventListener("resize", onResize)
    return () => {
      window.removeEventListener("scroll", syncFromScroll)
      window.removeEventListener("resize", onResize)
    }
  }, [isLgUp])

  useEffect(() => {
    if (!isLgUp) {
      return
    }
    syncFromScroll()
  }, [isLgUp, stickyTopPx])

  const scrollToIndex = (index: number) => {
    const next = clampIndex(index)
    if (!isLgUp) {
      setActiveIndex(next)
      return
    }
    const track = trackRef.current
    if (track == null) {
      setActiveIndex(next)
      return
    }
    const scrollable = TRACK_HEIGHT_PX - PANEL_HEIGHT_PX
    const step = scrollable / STEP_COUNT
    const trackTop = window.scrollY + track.getBoundingClientRect().top
    window.scrollTo({
      top: trackTop - stickyTopPx + next * step,
      behavior: reduceMotion ? "auto" : "smooth",
    })
  }

  if (!isLgUp) {
    return (
      <section className="w-full bg-white">
        <PanelChrome
          activeIndex={activeIndex}
          onSelect={scrollToIndex}
          reduceMotion={reduceMotion}
        />
      </section>
    )
  }

  return (
    <section className="w-full bg-white">
      <div
        ref={trackRef}
        className="relative w-full"
        style={{ height: TRACK_HEIGHT_PX }}
      >
        <div className="sticky w-full" style={{ top: stickyTopPx }}>
          <PanelChrome
            activeIndex={activeIndex}
            onSelect={scrollToIndex}
            reduceMotion={reduceMotion}
          />
        </div>
      </div>
    </section>
  )
}

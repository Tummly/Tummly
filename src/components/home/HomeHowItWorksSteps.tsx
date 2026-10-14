import {
  useEffect,
  useEffectEvent,
  useRef,
  useState,
  type ReactNode,
} from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"
import { Link } from "react-router-dom"

import howBringImage from "@/assets/images/marketing/home/how-bring.png"
import howBuildImage from "@/assets/images/marketing/home/how-build.png"
import { signupCtaBgPicture } from "@/assets/marketing-images"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  HOME_HOW_IT_WORKS_PLACE_MEDIA,
  HOME_HOW_IT_WORKS_STEPS,
  type HomeHowItWorksPlaceMedia,
  type HomeHowItWorksStep,
} from "@/content/marketing/homePage"
import { signUpCtaVideoUrl } from "@/lib/marketingMedia"
import { remoteSimpleUrl } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

const STEP_COUNT = HOME_HOW_IT_WORKS_STEPS.length
/** Fallback until measured (tabs + visual + padding). */
const FALLBACK_PANEL_HEIGHT_PX = 834
const CROSSFADE_MS = 0.25
/** Minimum clearance under sticky marketing header chrome. */
const HEADER_CLEARANCE_PX = 86
/** Left copy + right visual share the same step reveal (fade up from bottom). */
const STEP_REVEAL = {
  initial: { opacity: 0, y: 8 },
  animate: { opacity: 1, y: 0 },
  exit: { opacity: 0, y: -8 },
} as const
/** Match Pricing QR expand timing — Place panels grow/collapse in place. */
const PLACE_EXPAND_MS = 0.85
const PLACE_EXPAND_EASE = [0.22, 1, 0.36, 1] as const
/** Active panel vs peek share inside the 600px Place media frame. */
const PLACE_EXPANDED_GROW = 6.6
const PLACE_COLLAPSED_GROW = 1

const STEP_IMAGES = {
  build: howBuildImage,
  bring: howBringImage,
} as const satisfies Record<"build" | "bring", string>

const placeMediaImages = Object.fromEntries(
  HOME_HOW_IT_WORKS_PLACE_MEDIA.map((item) => [
    item.id,
    remoteSimpleUrl(item.mediaId),
  ]),
) as Record<HomeHowItWorksPlaceMedia["id"], string>

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

/**
 * Place the Guest Loop — Figma `5339:30041`.
 * Three material panels expand/collapse in place; only the in-image label
 * buttons change the active panel (no hover expand).
 */
function PlaceStepVisual({ reduceMotion }: { reduceMotion: boolean | null }) {
  const [activeId, setActiveId] = useState<HomeHowItWorksPlaceMedia["id"]>(
    HOME_HOW_IT_WORKS_PLACE_MEDIA[0].id,
  )
  const duration = reduceMotion ? 0 : PLACE_EXPAND_MS

  return (
    <div
      className="relative flex aspect-[750/600] w-full flex-col overflow-hidden rounded-[12px] bg-[#e2eae5] lg:aspect-auto lg:h-[600px]"
      role="group"
      aria-label="Guest Loop placement materials"
    >
      {HOME_HOW_IT_WORKS_PLACE_MEDIA.map((item) => {
        const active = item.id === activeId
        return (
          <motion.div
            key={item.id}
            initial={false}
            animate={{ flexGrow: active ? PLACE_EXPANDED_GROW : PLACE_COLLAPSED_GROW }}
            transition={{ duration, ease: PLACE_EXPAND_EASE }}
            className="relative min-h-0 w-full basis-0 overflow-hidden"
            style={{ flexShrink: 1 }}
          >
            <img
              src={placeMediaImages[item.id]}
              alt={active ? item.imageAlt : ""}
              width={750}
              height={600}
              className="pointer-events-none absolute inset-0 size-full object-cover"
              draggable={false}
              decoding="async"
              aria-hidden={!active}
            />
            <div
              aria-hidden
              className={cn(
                "pointer-events-none absolute inset-0",
                active
                  ? "bg-[linear-gradient(180deg,rgba(0,0,0,0)_0%,rgb(0,0,0)_116%),linear-gradient(90deg,rgba(0,0,0,0.2),rgba(0,0,0,0.2))]"
                  : "bg-black/80",
              )}
            />
            <button
              type="button"
              aria-pressed={active}
              onClick={() => {
                setActiveId(item.id)
              }}
              className={cn(
                "absolute left-1/2 z-10 -translate-x-1/2 cursor-pointer rounded-[30px] border border-[#363636] bg-transparent px-[18px] py-2 font-jakarta font-semibold uppercase text-white",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/60 focus-visible:ring-offset-2 focus-visible:ring-offset-transparent",
                active
                  ? "bottom-8 text-sm leading-normal lg:bottom-10"
                  : "top-1/2 -translate-y-1/2 text-xs leading-normal",
              )}
            >
              {item.label}
            </button>
          </motion.div>
        )
      })}
    </div>
  )
}

/**
 * Hear from guests — signup CTA video cropped to the step media box.
 */
function HearStepVisual({ step }: { step: HomeHowItWorksStep }) {
  const mediaRef = useRef<HTMLDivElement>(null)
  const videoRef = useRef<HTMLVideoElement>(null)
  const [videoFailed, setVideoFailed] = useState(false)
  const [videoInView, setVideoInView] = useState(false)

  useEffect(() => {
    const root = mediaRef.current
    if (root == null || typeof IntersectionObserver === "undefined") {
      setVideoInView(true)
      return
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        setVideoInView(entry?.isIntersecting === true)
      },
      {
        root: null,
        rootMargin: "0px",
        threshold: 0.25,
      },
    )

    observer.observe(root)
    return () => observer.disconnect()
  }, [])

  useEffect(() => {
    const el = videoRef.current
    if (el == null || videoFailed) {
      return
    }

    if (videoInView) {
      void el.play().catch(() => {
        /* Autoplay can still be blocked. */
      })
    } else {
      el.pause()
    }
  }, [videoFailed, videoInView])

  return (
    <div
      ref={mediaRef}
      className="relative aspect-[750/600] w-full overflow-hidden rounded-[12px] bg-[#e2eae5] lg:aspect-auto lg:h-[600px]"
    >
      <img
        src={signupCtaBgPicture.img.src}
        alt={step.imageAlt}
        width={750}
        height={600}
        className="absolute inset-0 size-full object-cover object-center"
        draggable={false}
        decoding="async"
      />
      {!videoFailed ? (
        <video
          ref={videoRef}
          className="absolute inset-0 size-full object-cover object-center"
          muted
          loop
          playsInline
          preload="metadata"
          poster={signupCtaBgPicture.img.src}
          aria-label={step.imageAlt}
          onError={() => setVideoFailed(true)}
        >
          <source src={signUpCtaVideoUrl()} type="video/mp4" />
        </video>
      ) : null}
    </div>
  )
}

function StaticStepVisual({
  stepId,
  imageAlt,
}: {
  stepId: keyof typeof STEP_IMAGES
  imageAlt: string
}) {
  return (
    <div className="relative aspect-[750/600] w-full overflow-hidden rounded-[12px] bg-[#e2eae5] lg:aspect-auto lg:h-[600px]">
      <img
        src={STEP_IMAGES[stepId]}
        alt={imageAlt}
        width={750}
        height={600}
        className="absolute inset-0 size-full object-cover"
        draggable={false}
        decoding="async"
      />
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
  let media: ReactNode = null
  if (step.id === "place") {
    media = <PlaceStepVisual reduceMotion={reduceMotion} />
  } else if (step.id === "hear") {
    media = <HearStepVisual step={step} />
  } else if (step.id === "build" || step.id === "bring") {
    media = <StaticStepVisual stepId={step.id} imageAlt={step.imageAlt} />
  }

  return (
    <div className="relative w-full lg:w-[min(100%,750px)] lg:shrink-0">
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
          className="w-full"
        >
          {media}
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
 * Home How it works steps —
 * Figma `5339:30041`, `4161:19285`, `4161:19284`, `4161:19283`.
 * Sticky scroll-driven active step on all viewports (same pattern as
 * How it works page). Tabs also jump the track.
 */
export function HomeHowItWorksSteps() {
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

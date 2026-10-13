import { useState } from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"

import {
  MARKETING_QR_EXPAND_STRIP,
  type MarketingQrExpandSlide,
} from "@/content/marketing/qrMaterialsExpandStrip"
import { cn } from "@/lib/utils"

const DEFAULT_ACTIVE_INDEX = Math.max(
  0,
  MARKETING_QR_EXPAND_STRIP.slides.findIndex((slide) => slide.featured),
)

/**
 * Collapsed : expanded flex share.
 * Softer than Figma 128:886 (~1:7) so hover crop change reads less like zoom.
 */
const COLLAPSED_GROW = 1.35
const EXPANDED_GROW = 4

/** Expand / collapse duration — keep slower than a quick hover snap. */
const EXPAND_MS = 0.85
/** Fade in after expand starts; exit must be near-instant so copy does not reflow. */
const CAPTION_IN_MS = 0.35
const CAPTION_OUT_MS = 0.08

const EXPAND_EASE = [0.22, 1, 0.36, 1] as const

function QrStripPanel({
  slide,
  active,
  reduceMotion,
  listLabel,
  onActivate,
}: {
  slide: MarketingQrExpandSlide
  active: boolean
  reduceMotion: boolean | null
  listLabel: string
  onActivate: () => void
}) {
  const duration = reduceMotion ? 0 : EXPAND_MS

  return (
    <motion.button
      type="button"
      aria-pressed={active}
      aria-label={`${listLabel}. ${slide.imageAlt}`}
      onMouseEnter={onActivate}
      onFocus={onActivate}
      onClick={onActivate}
      initial={false}
      animate={{ flexGrow: active ? EXPANDED_GROW : COLLAPSED_GROW }}
      transition={{ duration, ease: EXPAND_EASE }}
      className={cn(
        "relative h-[min(70vw,360px)] min-w-0 basis-0 cursor-pointer overflow-hidden rounded-[12px] border-0 p-0 sm:h-[420px] lg:h-[578px]",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2",
      )}
      style={{ flexShrink: 1 }}
    >
      <div
        aria-hidden
        className={cn(
          "pointer-events-none absolute inset-0 rounded-[12px]",
          active
            ? "bg-[linear-gradient(180deg,rgb(240,240,240)_0%,rgb(36,138,68)_139%)]"
            : "bg-[#f0f0f0]",
        )}
      />
      {/*
        Fixed-ish cover width reduces extreme crop when the strip is narrow,
        so expand/collapse feels less like a zoom punch.
      */}
      <img
        src={slide.image}
        alt=""
        width={886}
        height={578}
        className="pointer-events-none absolute left-1/2 top-0 h-full w-[min(160%,720px)] max-w-none -translate-x-1/2 object-cover object-center"
        draggable={false}
      />
      <div
        aria-hidden
        className={cn(
          "pointer-events-none absolute inset-0 rounded-[12px]",
          active
            ? "bg-black/20"
            : "bg-[linear-gradient(180.34deg,rgba(0,0,0,0)_0%,rgb(0,0,0)_116%)]",
        )}
      />

      <AnimatePresence initial={false}>
        {active ? (
          <motion.div
            key={`${slide.id}-caption`}
            initial={reduceMotion ? false : { opacity: 0 }}
            animate={{
              opacity: 1,
              transition: {
                duration: reduceMotion ? 0 : CAPTION_IN_MS,
                ease: "easeOut",
              },
            }}
            exit={{
              opacity: 0,
              transition: {
                duration: reduceMotion ? 0 : CAPTION_OUT_MS,
                ease: "easeIn",
              },
            }}
            className="pointer-events-none absolute bottom-8 left-8 z-10 flex w-[265px] shrink-0 flex-col gap-3 text-left text-white sm:bottom-10 sm:left-10"
          >
            <p className="m-0 whitespace-nowrap font-jakarta text-[22px] font-semibold leading-none sm:text-[28px]">
              {MARKETING_QR_EXPAND_STRIP.cardTitle}
            </p>
            <p className="m-0 font-jakarta text-sm font-normal leading-normal">
              {MARKETING_QR_EXPAND_STRIP.cardDescription}
            </p>
          </motion.div>
        ) : null}
      </AnimatePresence>
    </motion.button>
  )
}

type MarketingQrExpandStripProps = {
  /** Accessible name for the strip list. */
  ariaLabel?: string
}

/** Figma QR peek expand strip `5880:19364` — shared by Pricing and Product. */
export function MarketingQrExpandStrip({
  ariaLabel = "Physical QR materials",
}: MarketingQrExpandStripProps) {
  const slides = MARKETING_QR_EXPAND_STRIP.slides
  const reduceMotion = useReducedMotion()
  const [activeIndex, setActiveIndex] = useState(DEFAULT_ACTIVE_INDEX)

  return (
    <div role="list" aria-label={ariaLabel} className="flex w-full gap-1">
      {slides.map((slide, index) => (
        <QrStripPanel
          key={slide.id}
          slide={slide}
          active={index === activeIndex}
          reduceMotion={reduceMotion}
          listLabel={ariaLabel}
          onActivate={() => {
            setActiveIndex(index)
          }}
        />
      ))}
    </div>
  )
}

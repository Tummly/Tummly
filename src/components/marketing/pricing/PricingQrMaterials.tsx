import { useState } from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"
import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  PRICING_QR_MATERIALS,
  PRICING_SECTION_BODY,
  PRICING_SECTION_HEADING,
  PRICING_SECTION_INSET,
  PRICING_SECTION_Y,
} from "@/content/marketing/pricingPage"
import { cn } from "@/lib/utils"

const DEFAULT_ACTIVE_INDEX = Math.max(
  0,
  PRICING_QR_MATERIALS.slides.findIndex((slide) => slide.featured),
)

/** Collapsed : expanded flex share ≈ 128 : 886 from Figma. */
const COLLAPSED_GROW = 1
const EXPANDED_GROW = 7

/** Expand / collapse duration — keep slower than a quick hover snap. */
const EXPAND_MS = 0.85
/** Fade in after expand starts; exit must be near-instant so copy does not reflow. */
const CAPTION_IN_MS = 0.35
const CAPTION_OUT_MS = 0.08

const EXPAND_EASE = [0.22, 1, 0.36, 1] as const

type QrSlide = (typeof PRICING_QR_MATERIALS.slides)[number]

function QrStripPanel({
  slide,
  active,
  reduceMotion,
  onActivate,
}: {
  slide: QrSlide
  active: boolean
  reduceMotion: boolean | null
  onActivate: () => void
}) {
  const duration = reduceMotion ? 0 : EXPAND_MS

  return (
    <motion.button
      type="button"
      aria-pressed={active}
      aria-label={`${PRICING_QR_MATERIALS.cardTitle}. ${slide.imageAlt}`}
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
      <img
        src={slide.image}
        alt=""
        width={active ? 886 : 128}
        height={578}
        className="pointer-events-none absolute inset-0 size-full object-cover"
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
              {PRICING_QR_MATERIALS.cardTitle}
            </p>
            <p className="m-0 font-jakarta text-sm font-normal leading-normal">
              {PRICING_QR_MATERIALS.cardDescription}
            </p>
          </motion.div>
        ) : null}
      </AnimatePresence>
    </motion.button>
  )
}

/** Figma Physical QR materials (`5661:14445`) — hover expand strip. */
export function PricingQrMaterials() {
  const slides = PRICING_QR_MATERIALS.slides
  const reduceMotion = useReducedMotion()
  const [activeIndex, setActiveIndex] = useState(DEFAULT_ACTIVE_INDEX)

  return (
    <section className="w-full bg-white">
      <div
        className={cn(
          "flex w-full flex-col gap-[46px]",
          PRICING_SECTION_INSET,
          PRICING_SECTION_Y,
        )}
      >
        <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
          <div className="flex max-w-[578px] flex-col gap-[18px]">
            <h2 className={cn("m-0", PRICING_SECTION_HEADING)}>
              {PRICING_QR_MATERIALS.title}
            </h2>
            {PRICING_QR_MATERIALS.paragraphs.map((paragraph) => (
              <p key={paragraph} className={cn("m-0", PRICING_SECTION_BODY)}>
                {paragraph}
              </p>
            ))}
          </div>
          <MarketingButton asChild variant="secondary" className="w-fit">
            <Link to={PRICING_QR_MATERIALS.ctaTo}>
              {PRICING_QR_MATERIALS.ctaLabel}
            </Link>
          </MarketingButton>
        </div>

        <div
          role="list"
          aria-label="Physical QR materials"
          className="flex w-full gap-1"
        >
          {slides.map((slide, index) => (
            <QrStripPanel
              key={slide.id}
              slide={slide}
              active={index === activeIndex}
              reduceMotion={reduceMotion}
              onActivate={() => {
                setActiveIndex(index)
              }}
            />
          ))}
        </div>
      </div>
    </section>
  )
}

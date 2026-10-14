import { useState } from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"

import industryCafesImage from "@/assets/images/marketing/home/industry-cafes.png"
import industryCasualImage from "@/assets/images/marketing/home/industry-casual.png"
import industryGroupsImage from "@/assets/images/marketing/home/industry-groups.png"
import industryTakeawayImage from "@/assets/images/marketing/home/industry-takeaway.png"
import {
  HOME_INDUSTRIES,
  type HomeIndustry,
} from "@/content/marketing/homePage"
import { cn } from "@/lib/utils"

const INDUSTRY_COUNT = HOME_INDUSTRIES.items.length
const CROSSFADE_MS = 0.25

const INDUSTRY_IMAGES = {
  takeaway: industryTakeawayImage,
  cafes: industryCafesImage,
  casual: industryCasualImage,
  groups: industryGroupsImage,
} as const satisfies Record<HomeIndustry["id"], string>

function clampIndex(index: number) {
  return Math.max(0, Math.min(INDUSTRY_COUNT - 1, index))
}

function IndustryVisual({
  industry,
  reduceMotion,
  className,
}: {
  industry: HomeIndustry
  reduceMotion: boolean | null
  className?: string
}) {
  return (
    <div
      className={cn(
        "relative w-full overflow-hidden rounded-[12px] bg-[#e2eae5]",
        className,
      )}
    >
      <AnimatePresence initial={false} mode="sync">
        <motion.div
          key={industry.id}
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
            src={INDUSTRY_IMAGES[industry.id]}
            alt={industry.imageAlt}
            width={759}
            height={718}
            className="absolute inset-0 size-full object-cover"
            draggable={false}
            decoding="async"
          />
        </motion.div>
      </AnimatePresence>
    </div>
  )
}

function IndustryList({
  activeIndex,
  onSelect,
  reduceMotion,
}: {
  activeIndex: number
  onSelect: (index: number) => void
  reduceMotion: boolean | null
}) {
  return (
    <div
      role="tablist"
      aria-label="Restaurant types"
      className="flex w-full flex-col gap-[30px]"
    >
      {HOME_INDUSTRIES.items.map((item, index) => {
        const active = index === activeIndex
        return (
          <div key={item.id} className="flex w-full flex-col gap-[30px]">
            <button
              type="button"
              role="tab"
              id={`home-industry-tab-${item.id}`}
              aria-selected={active}
              aria-controls="home-industry-visual home-industry-visual-desktop"
              tabIndex={active ? 0 : -1}
              onClick={() => {
                onSelect(index)
              }}
              className={cn(
                "flex w-full flex-col items-start gap-[18px] border-0 border-b bg-transparent pb-5 text-left",
                "cursor-pointer transition-[opacity,border-color] duration-200 ease-out motion-reduce:transition-none",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2",
                active
                  ? "border-[#14a946] opacity-100"
                  : "border-[#7c7c7c] opacity-40",
              )}
            >
              <span className="font-jakarta text-[22px] font-medium leading-[28px] text-[#141414]">
                {item.title}
              </span>
              <span className="font-sans text-sm font-normal leading-4 text-[#141414] lg:text-base lg:leading-normal">
                {item.body}
              </span>
            </button>

            {/* Mobile Figma `6241:19397`: image sits under the active industry. */}
            {active ? (
              <div
                id="home-industry-visual"
                role="tabpanel"
                aria-labelledby={`home-industry-tab-${item.id}`}
                className="w-full lg:hidden"
              >
                <IndustryVisual
                  industry={item}
                  reduceMotion={reduceMotion}
                  className="aspect-[345/199]"
                />
              </div>
            ) : null}
          </div>
        )
      })}
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
  const industry =
    HOME_INDUSTRIES.items[activeIndex] ?? HOME_INDUSTRIES.items[0]

  return (
    <div
      className={cn(
        "flex w-full flex-col gap-10 overflow-hidden bg-[#f0f0f0]",
        /* Mobile Figma `6241:19397`: full-bleed grey, 24px pad, 60px vertical. */
        "px-6 py-[60px]",
        "lg:h-[838px] lg:flex-row lg:items-stretch lg:gap-[70px] lg:rounded-[12px] lg:p-[60px]",
      )}
      role="region"
      aria-labelledby="home-industries-heading"
    >
      {/* Desktop visual column; mobile image is inlined under the active row. */}
      <div
        id="home-industry-visual-desktop"
        role="tabpanel"
        aria-labelledby={`home-industry-tab-${industry.id}`}
        className="order-2 hidden min-h-0 w-full lg:order-1 lg:flex lg:min-w-0 lg:flex-1"
      >
        <IndustryVisual
          industry={industry}
          reduceMotion={reduceMotion}
          className="aspect-[759/718] h-full min-h-0 flex-1 lg:aspect-auto"
        />
      </div>

      <div className="order-1 flex w-full flex-col gap-10 lg:order-2 lg:min-w-0 lg:flex-1 lg:justify-center lg:gap-[70px]">
        <header className="flex max-w-[615px] flex-col gap-[18px] text-[#141414]">
          <div className="flex flex-col gap-3.5">
            <p className="m-0 font-jakarta text-sm font-medium leading-4">
              {HOME_INDUSTRIES.eyebrow}
            </p>
            <h2
              id="home-industries-heading"
              className="m-0 font-jakarta text-[28px] font-medium leading-[38px] lg:text-[46px] lg:leading-[58px]"
            >
              {HOME_INDUSTRIES.title}
            </h2>
          </div>
          <p className="m-0 max-w-[548px] font-sans text-sm font-normal leading-[22px] lg:text-[18px] lg:leading-6">
            {HOME_INDUSTRIES.body}
          </p>
        </header>

        <IndustryList
          activeIndex={activeIndex}
          onSelect={onSelect}
          reduceMotion={reduceMotion}
        />
      </div>
    </div>
  )
}

/**
 * Home Built for restaurants —
 * Figma `5088:12319` … `5088:12316` / mobile `6241:19397`.
 * Active industry changes only on click of the list items.
 * Mobile: image renders under the active row.
 */
export function HomeIndustries() {
  const reduceMotion = useReducedMotion()
  const [activeIndex, setActiveIndex] = useState(0)

  const selectIndex = (index: number) => {
    setActiveIndex(clampIndex(index))
  }

  return (
    <section className="w-full bg-white lg:bg-white">
      <div
        className={cn(
          "mx-auto w-full",
          /* Mobile: panel is full-bleed. Desktop: 60px content inset. */
          "px-0 lg:px-[60px] lg:py-[70px]",
        )}
      >
        <PanelChrome
          activeIndex={activeIndex}
          onSelect={selectIndex}
          reduceMotion={reduceMotion}
        />
      </div>
    </section>
  )
}

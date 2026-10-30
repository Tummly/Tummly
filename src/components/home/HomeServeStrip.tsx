import { useState } from "react"

import burritoBars from "@/assets/images/marketing/home/serve-strip/burrito-bars.webp"
import bubbleTeaShops from "@/assets/images/marketing/home/serve-strip/bubble-tea-shops.webp"
import casualDiningRestaurants from "@/assets/images/marketing/home/serve-strip/casual-dining-restaurants.webp"
import dessertParlours from "@/assets/images/marketing/home/serve-strip/dessert-parlours.webp"
import fishAndChipShops from "@/assets/images/marketing/home/serve-strip/fish-and-chip-shops.webp"
import juiceBars from "@/assets/images/marketing/home/serve-strip/juice-bars.webp"
import kebabShops from "@/assets/images/marketing/home/serve-strip/kebab-shops.webp"
import noodleBars from "@/assets/images/marketing/home/serve-strip/noodle-bars.webp"
import sushiBars from "@/assets/images/marketing/home/serve-strip/sushi-bars.webp"
import { Button } from "@/components/ui/button"
import { HOME_SERVE_STRIP } from "@/content/marketing/homePage"
import { cn } from "@/lib/utils"

type ServeStripItemId = (typeof HOME_SERVE_STRIP.items)[number]["id"]

/** Figma `6903:*` icon frames, exported from the Sectors component, rasterised at 48×48 (2× the 24px render). */
const SERVE_STRIP_ICONS: Record<ServeStripItemId, string> = {
  "fish-and-chip-shops": fishAndChipShops,
  "kebab-shops": kebabShops,
  "sushi-bars": sushiBars,
  "noodle-bars": noodleBars,
  "burrito-bars": burritoBars,
  "dessert-parlours": dessertParlours,
  "bubble-tea-shops": bubbleTeaShops,
  "juice-bars": juiceBars,
  "casual-dining-restaurants": casualDiningRestaurants,
}

function ServeStripChips({ duplicate = false }: { duplicate?: boolean }) {
  return (
    <ul
      aria-hidden={duplicate ? true : undefined}
      className={cn(
        "m-0 flex shrink-0 list-none items-center gap-8 p-0 pr-8",
        "motion-reduce:flex-wrap motion-reduce:justify-center motion-reduce:pr-0",
        duplicate && "motion-reduce:hidden",
      )}
    >
      {HOME_SERVE_STRIP.items.map((item) => (
        <li
          key={item.id}
          className="flex shrink-0 items-center gap-1.5 rounded-[6px] bg-[#e3eae5] px-3.5 py-2.5"
        >
          <img
            src={SERVE_STRIP_ICONS[item.id]}
            alt=""
            width={24}
            height={24}
            className="size-6 shrink-0"
          />
          <span className="font-sans text-xs leading-[normal] font-medium whitespace-nowrap text-[#3b3834]">
            {item.label}
          </span>
        </li>
      ))}
    </ul>
  )
}

type HomeServeStripProps = {
  /** Strip title. Default is the Home copy; How it works `6863:12786` passes its own. */
  eyebrow?: string
  /** Merged onto the title (How it works uses Medium 500 / #676767). */
  eyebrowClassName?: string
  /** `id` of the title and `aria-labelledby` of the section. Unique per page section. */
  headingId?: string
}

/**
 * Home "Built for the way you serve" strip — Figma `6757:45771`.
 * CSS-only endless marquee: the chip list is rendered twice and shifted by one copy.
 * Under `prefers-reduced-motion` it stops, hides the copy and wraps the chips.
 * WCAG 2.2.2: pauses on hover and keyboard focus, a tap on touch toggles it, and a
 * pause/play button is revealed on focus (hidden visually otherwise).
 */
export function HomeServeStrip({
  eyebrow = HOME_SERVE_STRIP.eyebrow,
  eyebrowClassName,
  headingId = "home-serve-strip-heading",
}: HomeServeStripProps = {}) {
  const [paused, setPaused] = useState(false)

  return (
    <section
      aria-labelledby={headingId}
      className="w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-white px-6 pt-5 pb-[70px] lg:px-[60px]"
    >
      <div className="flex w-full flex-col items-center gap-10">
        <h2
          id={headingId}
          className={cn(
            "m-0 text-center font-sans text-sm leading-4 font-semibold tracking-[2px] text-[#4a4a4a] uppercase",
            eyebrowClassName,
          )}
        >
          {eyebrow}
        </h2>

        <div
          data-paused={paused}
          onPointerUp={(event) => {
            if (event.pointerType === "touch") {
              setPaused((prev) => !prev)
            }
          }}
          className="home-serve-marquee relative w-full overflow-hidden motion-reduce:overflow-visible"
        >
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => {
              setPaused((prev) => !prev)
            }}
            className="font-inter transition-none sr-only h-px min-h-px px-0 focus:not-sr-only focus:absolute focus:top-1/2 focus:right-2 focus:z-10 focus:h-7 focus:min-h-7 focus:-translate-y-1/2 focus:px-2.5 focus:text-sm focus:font-medium focus:shadow-sm motion-reduce:hidden"
          >
            {paused ? "Play animation" : "Pause animation"}
          </Button>
          <div className="home-serve-marquee-track flex w-max motion-reduce:w-full motion-reduce:flex-col motion-reduce:items-center">
            <ServeStripChips />
            <ServeStripChips duplicate />
          </div>
          <div
            aria-hidden
            className="pointer-events-none absolute inset-y-0 left-0 w-[83px] bg-gradient-to-l from-transparent from-[18.3%] to-white to-[75.1%] motion-reduce:hidden"
          />
          <div
            aria-hidden
            className="pointer-events-none absolute inset-y-0 right-0 w-[83px] bg-gradient-to-r from-transparent from-[18.3%] to-white to-[75.1%] motion-reduce:hidden"
          />
        </div>
      </div>
    </section>
  )
}

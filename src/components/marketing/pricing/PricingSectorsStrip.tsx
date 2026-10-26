import bubbleTeaIcon from "@/assets/svg/marketing/pricing/sectors/bubble-tea.svg"
import burritoIcon from "@/assets/svg/marketing/pricing/sectors/burrito.svg"
import casualDiningIcon from "@/assets/svg/marketing/pricing/sectors/casual-dining.svg"
import dessertIcon from "@/assets/svg/marketing/pricing/sectors/dessert.svg"
import fishAndChipIcon from "@/assets/svg/marketing/pricing/sectors/fish-and-chip.svg"
import juiceIcon from "@/assets/svg/marketing/pricing/sectors/juice.svg"
import kebabIcon from "@/assets/svg/marketing/pricing/sectors/kebab.svg"
import noodleIcon from "@/assets/svg/marketing/pricing/sectors/noodle.svg"
import sushiIcon from "@/assets/svg/marketing/pricing/sectors/sushi.svg"
import {
  PRICING_SECTORS,
  PRICING_SECTION_INSET,
  PRICING_SECTION_Y,
  type PricingSectorId,
} from "@/content/marketing/pricingPage"
import { cn } from "@/lib/utils"

const SECTOR_ICONS = {
  "fish-and-chip": fishAndChipIcon,
  kebab: kebabIcon,
  sushi: sushiIcon,
  noodle: noodleIcon,
  burrito: burritoIcon,
  dessert: dessertIcon,
  "bubble-tea": bubbleTeaIcon,
  juice: juiceIcon,
  "casual-dining": casualDiningIcon,
} as const satisfies Record<PricingSectorId, string>

/**
 * Figma UK restaurant sectors `6731:14319` — below Customise your plan.
 */
export function PricingSectorsStrip() {
  return (
    <section
      aria-labelledby="pricing-sectors-heading"
      className="w-full bg-white"
    >
      <div
        className={cn(
          "flex w-full flex-col items-center gap-10 overflow-hidden",
          PRICING_SECTION_INSET,
          PRICING_SECTION_Y,
        )}
      >
        <p
          id="pricing-sectors-heading"
          className="m-0 text-center font-sans text-base font-normal leading-[normal] text-[#141414]"
        >
          {PRICING_SECTORS.title}
        </p>

        <div className="relative w-full">
          <ul className="m-0 flex list-none justify-start gap-[18px] overflow-x-auto px-0.5 py-0.5 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden xl:justify-center">
            {PRICING_SECTORS.items.map((item) => (
              <li key={item.id} className="shrink-0">
                <div className="flex items-center gap-1.5 rounded-md bg-[#f0f0f0] px-3.5 py-2.5">
                  <span className="flex size-6 shrink-0 items-center justify-center overflow-hidden">
                    <img
                      src={SECTOR_ICONS[item.id]}
                      alt=""
                      aria-hidden
                      className={cn(
                        "max-h-full max-w-full object-contain",
                        item.id === "burrito" && "-rotate-45",
                      )}
                    />
                  </span>
                  <span className="whitespace-nowrap font-sans text-xs font-medium leading-[normal] text-[#3b3834]">
                    {item.label}
                  </span>
                </div>
              </li>
            ))}
          </ul>
          <div
            aria-hidden
            className="pointer-events-none absolute inset-y-0 left-0 w-[83px] bg-gradient-to-r from-white to-transparent xl:hidden"
          />
          <div
            aria-hidden
            className="pointer-events-none absolute inset-y-0 right-0 w-[83px] bg-gradient-to-l from-white to-transparent xl:hidden"
          />
        </div>
      </div>
    </section>
  )
}

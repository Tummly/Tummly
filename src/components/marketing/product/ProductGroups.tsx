import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  PRODUCT_GROUPS,
  PRODUCT_SECTION_BODY,
  PRODUCT_SECTION_HEADING,
  PRODUCT_SECTION_INSET,
  PRODUCT_SECTION_Y,
} from "@/content/marketing/productPage"
import { cn } from "@/lib/utils"

/** Figma Product multi-location banner `5661:24169`. */
export function ProductGroups() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="product-groups-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-8 lg:flex-row lg:items-end lg:justify-between lg:gap-10",
          PRODUCT_SECTION_INSET,
          PRODUCT_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[692px] flex-col gap-[18px]">
          <p className="m-0 font-sans text-base font-medium uppercase leading-normal text-black">
            {PRODUCT_GROUPS.eyebrow}
          </p>
          <h2
            id="product-groups-heading"
            className={cn("m-0 max-w-[520px]", PRODUCT_SECTION_HEADING)}
          >
            {PRODUCT_GROUPS.title}
          </h2>
          <p className={cn("m-0 max-w-[692px]", PRODUCT_SECTION_BODY)}>
            {PRODUCT_GROUPS.body}
          </p>
        </header>

        <MarketingButton asChild variant="secondary" className="w-fit shrink-0">
          <Link to={PRODUCT_GROUPS.ctaTo}>{PRODUCT_GROUPS.ctaLabel}</Link>
        </MarketingButton>
      </div>
    </section>
  )
}

import { CheckIcon } from "lucide-react"
import { Link } from "react-router-dom"

import groupsImage from "@/assets/images/marketing/product/groups.webp"
import groupsStripImage from "@/assets/images/marketing/product/groups-strip.webp"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  PRODUCT_DISPLAY_HEADING,
  PRODUCT_EYEBROW,
  PRODUCT_GROUPS,
  PRODUCT_SECTION_INSET,
} from "@/content/marketing/productPage"
import { cn } from "@/lib/utils"

/**
 * Figma Product For Groups `6821:36359`: white 12px card, 20px chrome gap above
 * and below. Copy 640px, media plate 720×650 with a green strip across the top.
 */
export function ProductGroups() {
  return (
    <section className="w-full py-5" aria-labelledby="product-groups-heading">
      <div
        className={cn(
          "flex w-full flex-col items-start gap-10 rounded-[12px] bg-white py-12.5 lg:flex-row lg:items-center lg:justify-between lg:gap-12 lg:py-[70px]",
          PRODUCT_SECTION_INSET,
        )}
      >
        <div className="flex w-full flex-col gap-10 lg:min-h-[650px] lg:min-w-0 lg:max-w-[640px] lg:flex-1 lg:justify-between">
          <header className="flex w-full flex-col gap-[22px]">
            <div className="flex w-full flex-col gap-3.5">
              <p className={PRODUCT_EYEBROW}>{PRODUCT_GROUPS.eyebrow}</p>
              <h2
                id="product-groups-heading"
                className={cn(PRODUCT_DISPLAY_HEADING, "max-w-[545px]")}
              >
                {PRODUCT_GROUPS.title}
              </h2>
            </div>
            <p className="m-0 max-w-[599px] font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6">
              {PRODUCT_GROUPS.body}
            </p>
          </header>

          <div className="flex w-full flex-col gap-[46px]">
            <ul className="m-0 flex w-full list-none flex-col gap-4 p-0">
              {PRODUCT_GROUPS.points.map((point, index) => (
                <li key={point.id} className="flex flex-col gap-4">
                  <div className="flex flex-col gap-2 text-[#141414]">
                    <h3 className="m-0 font-jakarta text-base font-bold leading-[normal]">
                      {point.title}
                    </h3>
                    <p className="m-0 font-sans text-sm font-normal leading-5">
                      {point.body}
                    </p>
                  </div>
                  {index < PRODUCT_GROUPS.points.length - 1 ? (
                    <hr className="m-0 w-full border-[#c6c6c6]" />
                  ) : null}
                </li>
              ))}
            </ul>

            <div className="flex flex-col items-start gap-5">
              <div className="flex flex-wrap items-center gap-5">
                <MarketingButton asChild variant="primary">
                  <RequestTrialLink planIntent={{ plan: "Pilot" }}>
                    {PRODUCT_GROUPS.primaryLabel}
                  </RequestTrialLink>
                </MarketingButton>
                <MarketingButton asChild variant="secondary">
                  <Link to={PRODUCT_GROUPS.ctaTo}>
                    {PRODUCT_GROUPS.ctaLabel}
                  </Link>
                </MarketingButton>
              </div>
              <div className="flex flex-wrap items-center gap-[18px]">
                {PRODUCT_GROUPS.trustItems.map((item) => (
                  <p
                    key={item}
                    className="m-0 flex items-center gap-1.5 font-inter text-xs font-medium leading-[normal] text-[#727272]"
                  >
                    <CheckIcon
                      className="size-3 shrink-0 text-[#727272]"
                      aria-hidden
                      strokeWidth={2}
                    />
                    {item}
                  </p>
                ))}
              </div>
            </div>
          </div>
        </div>

        <div className="relative aspect-[720/650] w-full shrink-0 overflow-hidden rounded-[12px] bg-[#e9e9e9] lg:w-auto lg:max-w-[720px] lg:flex-1">
          <img
            src={groupsImage}
            alt={PRODUCT_GROUPS.imageAlt}
            width={1448}
            height={1086}
            className="absolute inset-0 size-full max-w-none object-cover"
            decoding="async"
            loading="lazy"
          />
          <img
            src={groupsStripImage}
            alt=""
            width={2400}
            height={1000}
            className="pointer-events-none absolute right-[-2.64%] top-0 h-[7.23%] w-[102.64%] max-w-none object-cover"
            decoding="async"
            loading="lazy"
            aria-hidden
          />
        </div>
      </div>
    </section>
  )
}

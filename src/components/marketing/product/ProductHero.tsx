import { CheckIcon } from "lucide-react"
import { Link } from "react-router-dom"

import heroImage from "@/assets/images/marketing/product/hero.webp"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  PRODUCT_EYEBROW,
  PRODUCT_HERO,
  PRODUCT_SECTION_INSET,
  PRODUCT_SECTION_Y,
} from "@/content/marketing/productPage"
import { cn } from "@/lib/utils"

/**
 * Figma Product hero `5661:22625` (1668×749).
 * Left copy + CTAs (616px); right media plate `5838:27265` (766×609).
 */
export function ProductHero() {
  return (
    <section className="w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-[#fafafa]">
      <div
        className={cn(
          "flex w-full flex-col items-start gap-10 lg:flex-row lg:items-center lg:justify-between lg:gap-8",
          PRODUCT_SECTION_INSET,
          PRODUCT_SECTION_Y,
        )}
      >
        <div className="flex w-full flex-col items-start justify-center gap-[46px] lg:min-w-0 lg:max-w-[616px] lg:flex-1">
          <div className="flex w-full flex-col gap-[22px]">
            <div className="flex w-full flex-col gap-3.5">
              <p className={PRODUCT_EYEBROW}>{PRODUCT_HERO.eyebrow}</p>
              <h1 className="m-0 font-jakarta text-[36px] font-semibold leading-10 text-black lg:text-[54px] lg:leading-[60px]">
                {PRODUCT_HERO.title}
              </h1>
            </div>
            <p className="m-0 font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6">
              {PRODUCT_HERO.body}
            </p>
          </div>

          <div className="flex flex-col items-start gap-5">
            <div className="flex flex-wrap items-center gap-5">
              <MarketingButton asChild variant="primary">
                <RequestTrialLink planIntent={{ plan: "Pilot" }}>
                  {PRODUCT_HERO.primaryLabel}
                </RequestTrialLink>
              </MarketingButton>
              <MarketingButton asChild variant="secondary">
                <Link to={PRODUCT_HERO.secondaryTo}>
                  {PRODUCT_HERO.secondaryLabel}
                </Link>
              </MarketingButton>
            </div>

            <div className="flex flex-wrap items-center gap-[18px]">
              {PRODUCT_HERO.trustItems.map((item) => (
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

        <div className="relative aspect-[766/609] w-full shrink-0 overflow-hidden rounded-[12px] bg-[#e9e9e9] lg:w-auto lg:max-w-[766px] lg:flex-1">
          <img
            src={heroImage}
            alt={PRODUCT_HERO.imageAlt}
            width={2160}
            height={2160}
            className="absolute left-[3.55%] top-[-6.85%] h-[116.86%] w-[92.91%] max-w-none"
            decoding="async"
            fetchPriority="high"
          />
        </div>
      </div>
    </section>
  )
}

import { CheckIcon } from "lucide-react"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  PRODUCT_HERO,
  PRODUCT_SECTION_BODY,
  PRODUCT_SECTION_INSET,
  PRODUCT_SECTION_Y,
} from "@/content/marketing/productPage"
import { marketingHeroHeading } from "@/lib/marketing-layout"
import { remoteSimpleUrl } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

const heroCollageImage = remoteSimpleUrl("product/hero-collage")

/**
 * Figma Product hero `5661:22625` (1668×749).
 * Left copy + CTAs; right collage frame `5838:27265`.
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
        <div className="flex w-full max-w-[785px] flex-col items-start justify-center gap-[46px] lg:min-w-0 lg:flex-1">
          <div className="flex w-full flex-col items-start gap-[18px]">
            <h1
              className={cn(
                "m-0 w-full text-black",
                marketingHeroHeading,
                "font-semibold lg:text-[54px] lg:leading-[70px]",
              )}
            >
              {PRODUCT_HERO.titleLine1}
              <br aria-hidden />
              {PRODUCT_HERO.titleLine2}
            </h1>
            <p className={cn("m-0 w-full max-w-[634px]", PRODUCT_SECTION_BODY)}>
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
                <a href={PRODUCT_HERO.secondaryHref}>
                  {PRODUCT_HERO.secondaryLabel}
                </a>
              </MarketingButton>
            </div>

            <div className="flex flex-wrap items-center gap-[18px]">
              {PRODUCT_HERO.trustItems.map((item) => (
                <p
                  key={item}
                  className="m-0 flex items-center gap-1.5 text-xs font-medium leading-normal text-[#727272]"
                >
                  <CheckIcon
                    className="size-3 shrink-0 text-[#14a74a]"
                    aria-hidden
                    strokeWidth={2.5}
                  />
                  {item}
                </p>
              ))}
            </div>
          </div>
        </div>

        <div className="w-full shrink-0 overflow-hidden rounded-[12px] lg:w-[min(100%,766px)] lg:max-w-[766px]">
          <img
            src={heroCollageImage}
            alt={PRODUCT_HERO.dashboardImageAlt}
            width={766}
            height={609}
            className="block h-auto w-full"
            decoding="async"
            fetchPriority="high"
          />
        </div>
      </div>
    </section>
  )
}

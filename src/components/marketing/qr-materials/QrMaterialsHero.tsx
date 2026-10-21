import { Link } from "react-router-dom"

import heroProductImage from "@/assets/images/marketing/qr-materials/hero-product.png"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  QR_MATERIALS_HERO,
  QR_MATERIALS_SECTION_BODY,
  QR_MATERIALS_SECTION_INSET,
  QR_MATERIALS_SECTION_Y,
} from "@/content/marketing/qrMaterialsPage"
import { marketingHeroHeading } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/**
 * Figma QR materials hero `5661:19336`.
 * Left copy + CTAs + note; right product frame `5661:19346`.
 */
export function QrMaterialsHero() {
  return (
    <section className="w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-[#fafafa]">
      <div
        className={cn(
          "flex w-full flex-col items-start gap-10 lg:flex-row lg:items-start lg:justify-center lg:gap-[70px]",
          QR_MATERIALS_SECTION_INSET,
          QR_MATERIALS_SECTION_Y,
        )}
      >
        <div className="flex w-full max-w-[785px] flex-col items-start gap-[46px] lg:min-w-0 lg:flex-1">
          <div className="flex w-full flex-col items-start gap-[18px]">
            <h1
              className={cn(
                "m-0 w-full text-black",
                marketingHeroHeading,
                "font-semibold lg:text-[54px] lg:leading-[70px]",
              )}
            >
              {QR_MATERIALS_HERO.title}
            </h1>
            <p
              className={cn(
                "m-0 w-full max-w-[738px]",
                QR_MATERIALS_SECTION_BODY,
              )}
            >
              {QR_MATERIALS_HERO.body}
            </p>
          </div>

          <div className="flex w-full max-w-[600px] flex-col items-start gap-5">
            <div className="flex flex-wrap items-center gap-5">
              <MarketingButton asChild variant="primary">
                <RequestTrialLink planIntent={{ plan: "Pilot" }}>
                  {QR_MATERIALS_HERO.primaryLabel}
                </RequestTrialLink>
              </MarketingButton>
              <MarketingButton asChild variant="secondary">
                <Link to={QR_MATERIALS_HERO.secondaryTo}>
                  {QR_MATERIALS_HERO.secondaryLabel}
                </Link>
              </MarketingButton>
            </div>

            <p className="m-0 max-w-[560px] text-xs font-inter font-medium leading-normal text-[#727272]">
              {QR_MATERIALS_HERO.note}
            </p>
          </div>
        </div>

        <div
          className="relative aspect-[671/578] w-full shrink-0 overflow-hidden rounded-[12px] lg:w-[min(100%,671px)] lg:max-w-[671px]"
          style={{
            backgroundImage:
              "linear-gradient(180deg, #f0f0f0 0%, #248a44 140%)",
          }}
        >
          <img
            src={heroProductImage}
            alt={QR_MATERIALS_HERO.productImageAlt}
            width={1467}
            height={1665}
            className="absolute inset-0 size-full object-cover object-center"
            decoding="async"
            fetchPriority="high"
          />
        </div>
      </div>
    </section>
  )
}

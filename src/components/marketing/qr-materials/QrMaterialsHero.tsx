import { Check } from "lucide-react"
import { Link } from "react-router-dom"

import heroFrameBgImage from "@/assets/images/marketing/qr-materials/hero-frame-bg.webp"
import heroPhotoImage from "@/assets/images/marketing/qr-materials/hero-photo.webp"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  QR_MATERIALS_HERO,
  QR_MATERIALS_SECTION_BODY,
  QR_MATERIALS_SECTION_INSET,
  QR_MATERIALS_SECTION_Y,
} from "@/content/marketing/qrMaterialsPage"
import { cn } from "@/lib/utils"

/**
 * Figma QR materials hero `5661:19336`.
 * Left copy + CTAs + Pilot note; right green frame `7020:14314` (766×609)
 * with the photo `7020:14315` (821×547) bleeding off its right edge.
 */
export function QrMaterialsHero() {
  return (
    <section className="w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-[#fafafa]">
      <div
        className={cn(
          "flex w-full flex-col items-start gap-10 lg:flex-row lg:items-center lg:gap-[70px]",
          QR_MATERIALS_SECTION_INSET,
          QR_MATERIALS_SECTION_Y,
        )}
      >
        <div className="flex w-full min-w-0 flex-col items-start gap-10 lg:flex-1 lg:gap-[76px]">
          <div className="flex w-full flex-col items-start gap-[22px]">
            <div className="flex w-full flex-col items-start gap-3.5">
              <p className="m-0 font-sans text-sm leading-4 font-semibold tracking-[1.12px] text-[#4a4a4a] uppercase">
                {QR_MATERIALS_HERO.eyebrow}
              </p>
              <h1
                className={cn(
                  "m-0 w-full max-w-[544px] font-jakarta font-semibold text-black",
                  "text-[34px] leading-10 lg:text-[54px] lg:leading-[60px]",
                )}
              >
                {QR_MATERIALS_HERO.title}
              </h1>
            </div>
            <p
              className={cn(
                "m-0 w-full max-w-[643px]",
                QR_MATERIALS_SECTION_BODY,
              )}
            >
              {QR_MATERIALS_HERO.body}
            </p>
          </div>

          <div className="flex w-full max-w-[600px] flex-col items-start gap-5">
            <div className="flex flex-wrap items-center gap-5">
              <MarketingButton
                asChild
                variant="primary"
                className="sm:w-[162px]"
              >
                <RequestTrialLink planIntent={{ plan: "Pilot" }}>
                  {QR_MATERIALS_HERO.primaryLabel}
                </RequestTrialLink>
              </MarketingButton>
              <MarketingButton
                asChild
                variant="secondary"
                className="sm:h-11 sm:w-[164px]"
              >
                <Link to={QR_MATERIALS_HERO.secondaryTo}>
                  {QR_MATERIALS_HERO.secondaryLabel}
                </Link>
              </MarketingButton>
            </div>

            <p className="m-0 flex items-center gap-1.5 text-xs font-inter font-medium leading-[normal] text-[#727272]">
              <Check
                aria-hidden
                className="size-3.5 shrink-0 text-[#727272]"
                strokeWidth={2}
              />
              {QR_MATERIALS_HERO.note}
            </p>
          </div>
        </div>

        <div className="relative aspect-[766/609] w-full shrink-0 overflow-hidden rounded-[12px] lg:w-[min(49.5%,766px)]">
          <img
            src={heroFrameBgImage}
            alt=""
            width={1600}
            height={1600}
            className="pointer-events-none absolute inset-0 size-full object-cover"
            decoding="async"
            aria-hidden
          />
          {/* Figma `7020:14315`: 821×547 at left 33, centred vertically; photo shifted inside. */}
          <div className="absolute top-1/2 left-[4.31%] aspect-[821/547] w-[107.2%] -translate-y-1/2 overflow-hidden rounded-[12px]">
            <img
              src={heroPhotoImage}
              alt={QR_MATERIALS_HERO.imageAlt}
              width={1448}
              height={1086}
              className="absolute top-[-6.28%] left-[-10.42%] h-[112.57%] w-full max-w-none"
              decoding="async"
              fetchPriority="high"
            />
          </div>
        </div>
      </div>
    </section>
  )
}

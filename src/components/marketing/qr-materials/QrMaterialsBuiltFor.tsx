import { Check } from "lucide-react"
import { Link } from "react-router-dom"

import builtFrameBgImage from "@/assets/images/marketing/qr-materials/built-frame-bg.webp"
import builtPhotoImage from "@/assets/images/marketing/qr-materials/built-photo.webp"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  QR_MATERIALS_BUILT_FOR,
  QR_MATERIALS_SECTION_BODY,
  QR_MATERIALS_SECTION_INSET,
  QR_MATERIALS_SECTION_Y,
} from "@/content/marketing/qrMaterialsPage"
import { cn } from "@/lib/utils"

/**
 * Figma QR materials Built for your restaurant `6853:12456`.
 * Grey card, copy left; green photo frame `7018:14310` (765×497) sits at
 * 59.05% / vertically centred on desktop (bleeds off the right edge, the copy
 * column sets the card height) and stacks under the copy on mobile.
 */
export function QrMaterialsBuiltFor() {
  return (
    <section
      className="relative mt-5 w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-[#f0f0f0]"
      aria-labelledby="qr-materials-built-for-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-10 lg:flex-row lg:items-center lg:gap-8",
          QR_MATERIALS_SECTION_INSET,
          QR_MATERIALS_SECTION_Y,
        )}
      >
        <div className="flex w-full min-w-0 flex-col items-start gap-10 lg:w-[41%] lg:max-w-[640px] lg:shrink-0 lg:gap-[62px]">
          <div className="flex w-full flex-col items-start gap-[22px]">
            <div className="flex w-full flex-col items-start gap-3.5">
              <p className="m-0 font-sans text-sm leading-4 font-medium tracking-[1.12px] text-[#4a4a4a] uppercase">
                {QR_MATERIALS_BUILT_FOR.eyebrow}
              </p>
              <h2
                id="qr-materials-built-for-heading"
                className="m-0 w-full max-w-[571px] font-jakarta text-[34px] leading-10 font-medium text-black lg:text-[40px] lg:leading-[normal] xl:text-[46px]"
              >
                {QR_MATERIALS_BUILT_FOR.title}
              </h2>
            </div>
            <p
              className={cn(
                "m-0 w-full max-w-[432px]",
                QR_MATERIALS_SECTION_BODY,
              )}
            >
              {QR_MATERIALS_BUILT_FOR.body}
            </p>
          </div>

          <div className="flex w-full flex-col items-start gap-5">
            <div className="flex flex-wrap items-center gap-5">
              <MarketingButton
                asChild
                variant="primary"
                className="sm:w-[162px]"
              >
                <RequestTrialLink planIntent={QR_MATERIALS_BUILT_FOR.planIntent}>
                  {QR_MATERIALS_BUILT_FOR.primaryLabel}
                </RequestTrialLink>
              </MarketingButton>
              <MarketingButton
                asChild
                variant="secondary"
                className="sm:w-[196px]"
              >
                <Link to={QR_MATERIALS_BUILT_FOR.secondaryTo}>
                  {QR_MATERIALS_BUILT_FOR.secondaryLabel}
                </Link>
              </MarketingButton>
            </div>
            <ul className="m-0 flex list-none flex-wrap items-center gap-[18px] p-0">
              {QR_MATERIALS_BUILT_FOR.trustItems.map((item) => (
                <li
                  key={item}
                  className="m-0 flex items-center gap-1.5 text-xs font-inter font-medium leading-[normal] text-[#727272]"
                >
                  <Check
                    aria-hidden
                    className="size-3.5 shrink-0 text-[#727272]"
                    strokeWidth={2}
                  />
                  {item}
                </li>
              ))}
            </ul>
          </div>
        </div>

        <div className="relative aspect-[765/497] w-full shrink-0 overflow-hidden rounded-[12px] lg:absolute lg:top-1/2 lg:left-[59.05%] lg:w-[45.86%] lg:-translate-y-1/2 xl:w-[max(45.86%,700px)]">
          <img
            src={builtFrameBgImage}
            alt=""
            width={1600}
            height={1600}
            className="pointer-events-none absolute inset-0 size-full object-cover"
            decoding="async"
            aria-hidden
          />
          {/* Figma `7018:14311`: 664×429 photo, 35px from the left, centred vertically. */}
          <div className="absolute top-1/2 left-[4.58%] aspect-[664/429] w-[86.8%] -translate-y-1/2 overflow-hidden rounded-[12px]">
            <img
              src={builtPhotoImage}
              alt={QR_MATERIALS_BUILT_FOR.imageAlt}
              width={1448}
              height={1086}
              className="absolute inset-0 size-full object-cover"
              decoding="async"
              loading="lazy"
            />
          </div>
        </div>
      </div>
    </section>
  )
}

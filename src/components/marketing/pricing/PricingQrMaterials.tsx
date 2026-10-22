import { Link } from "react-router-dom"

import qrMaterialsEditorial from "@/assets/images/marketing/pricing/qr-materials-editorial.jpg"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  PRICING_QR_MATERIALS,
  PRICING_SECTION_BODY,
  PRICING_SECTION_INSET,
  PRICING_SECTION_Y,
} from "@/content/marketing/pricingPage"
import { cn } from "@/lib/utils"

/** Figma QR materials `5661:14445` — replaces Usage stays predictable. */
export function PricingQrMaterials() {
  return (
    <section
      id="qr-materials"
      aria-labelledby="pricing-qr-materials-heading"
      className="w-full bg-white"
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-10 lg:flex-row lg:items-center lg:justify-between lg:gap-[70px]",
          PRICING_SECTION_INSET,
          PRICING_SECTION_Y,
        )}
      >
        <div className="flex w-full max-w-[578px] flex-col items-start justify-between gap-10 lg:min-h-[538px] lg:flex-1 lg:gap-0">
          <div className="flex flex-col gap-[22px]">
            <div className="flex flex-col gap-3.5">
              <p className="m-0 font-sans text-sm font-normal uppercase leading-4 text-[#141414]">
                {PRICING_QR_MATERIALS.eyebrow}
              </p>
              <h2
                id="pricing-qr-materials-heading"
                className="m-0 font-jakarta text-[34px] font-[500] leading-normal text-black lg:text-[46px]"
              >
                {PRICING_QR_MATERIALS.title}
              </h2>
            </div>
            <div className="flex max-w-[540px] flex-col gap-[18px]">
              {PRICING_QR_MATERIALS.paragraphs.map((paragraph) => (
                <p key={paragraph} className={cn("m-0", PRICING_SECTION_BODY)}>
                  {paragraph}
                </p>
              ))}
            </div>
          </div>
          <MarketingButton asChild variant="secondary" className="w-fit">
            <Link to={PRICING_QR_MATERIALS.ctaTo}>
              {PRICING_QR_MATERIALS.ctaLabel}
            </Link>
          </MarketingButton>
        </div>

        <div className="relative aspect-[739/631] w-full shrink-0 overflow-hidden rounded-[12px] lg:w-[min(100%,739px)] lg:max-w-[739px]">
          <img
            src={qrMaterialsEditorial}
            alt={PRICING_QR_MATERIALS.imageAlt}
            width={1478}
            height={1262}
            className="absolute inset-0 size-full object-cover"
            decoding="async"
          />
        </div>
      </div>
    </section>
  )
}

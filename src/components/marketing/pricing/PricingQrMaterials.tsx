import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { MarketingQrExpandStrip } from "@/components/marketing/MarketingQrExpandStrip"
import {
  PRICING_QR_MATERIALS,
  PRICING_SECTION_BODY,
  PRICING_SECTION_HEADING,
  PRICING_SECTION_INSET,
  PRICING_SECTION_Y,
} from "@/content/marketing/pricingPage"
import { cn } from "@/lib/utils"

/** Figma Physical QR materials (`5661:14445`) — hover expand strip. */
export function PricingQrMaterials() {
  return (
    <section className="w-full bg-white">
      <div
        className={cn(
          "flex w-full flex-col gap-[46px]",
          PRICING_SECTION_INSET,
          PRICING_SECTION_Y,
        )}
      >
        <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
          <div className="flex max-w-[578px] flex-col gap-[18px]">
            <h2 className={cn("m-0", PRICING_SECTION_HEADING)}>
              {PRICING_QR_MATERIALS.title}
            </h2>
            {PRICING_QR_MATERIALS.paragraphs.map((paragraph) => (
              <p key={paragraph} className={cn("m-0", PRICING_SECTION_BODY)}>
                {paragraph}
              </p>
            ))}
          </div>
          <MarketingButton asChild variant="secondary" className="w-fit">
            <Link to={PRICING_QR_MATERIALS.ctaTo}>
              {PRICING_QR_MATERIALS.ctaLabel}
            </Link>
          </MarketingButton>
        </div>

        <MarketingQrExpandStrip ariaLabel="Physical QR materials" />
      </div>
    </section>
  )
}

import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  QR_MATERIALS_NO_WEBSITE,
  QR_MATERIALS_SECTION_BODY,
  QR_MATERIALS_SECTION_HEADING,
  QR_MATERIALS_SECTION_INSET,
  QR_MATERIALS_SECTION_Y,
} from "@/content/marketing/qrMaterialsPage"
import { cn } from "@/lib/utils"

/**
 * Figma QR materials no-website reassurance `5661:19452`.
 * Title + body + secondary CTA to How it works.
 */
export function QrMaterialsNoWebsite() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="qr-materials-no-website-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-[46px] lg:gap-[66px]",
          QR_MATERIALS_SECTION_INSET,
          QR_MATERIALS_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[776px] flex-col gap-[18px]">
          <h2
            id="qr-materials-no-website-heading"
            className={cn("m-0 w-full", QR_MATERIALS_SECTION_HEADING)}
          >
            {QR_MATERIALS_NO_WEBSITE.title}
          </h2>
          <p className={cn("m-0 w-full", QR_MATERIALS_SECTION_BODY)}>
            {QR_MATERIALS_NO_WEBSITE.body}
          </p>
        </header>

        <MarketingButton asChild variant="secondary">
          <Link to={QR_MATERIALS_NO_WEBSITE.ctaTo}>
            {QR_MATERIALS_NO_WEBSITE.ctaLabel}
          </Link>
        </MarketingButton>
      </div>
    </section>
  )
}

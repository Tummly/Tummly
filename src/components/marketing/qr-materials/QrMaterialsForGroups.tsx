import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  QR_MATERIALS_FOR_GROUPS,
  QR_MATERIALS_SECTION_BODY,
  QR_MATERIALS_SECTION_HEADING,
  QR_MATERIALS_SECTION_INSET,
  QR_MATERIALS_SECTION_Y,
} from "@/content/marketing/qrMaterialsPage"
import { cn } from "@/lib/utils"

/**
 * Figma QR materials for-groups CTA `5661:19490`.
 * Copy left, secondary CTA bottom-right on desktop; stacks on mobile.
 */
export function QrMaterialsForGroups() {
  return (
    <section
      className="w-full overflow-hidden rounded-bl-[12px] rounded-br-[12px] bg-white"
      aria-labelledby="qr-materials-for-groups-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-10 lg:flex-row lg:items-end lg:justify-between lg:gap-12",
          QR_MATERIALS_SECTION_INSET,
          QR_MATERIALS_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[714px] flex-col gap-[18px]">
          <p className="m-0 font-sans text-base font-medium uppercase leading-normal text-black">
            {QR_MATERIALS_FOR_GROUPS.eyebrow}
          </p>
          <h2
            id="qr-materials-for-groups-heading"
            className={cn("m-0 w-full", QR_MATERIALS_SECTION_HEADING)}
          >
            {QR_MATERIALS_FOR_GROUPS.title}
          </h2>
          <p className={cn("m-0 w-full", QR_MATERIALS_SECTION_BODY)}>
            {QR_MATERIALS_FOR_GROUPS.body}
          </p>
        </header>

        <MarketingButton asChild variant="secondary" className="shrink-0">
          <Link to={QR_MATERIALS_FOR_GROUPS.ctaTo}>
            {QR_MATERIALS_FOR_GROUPS.ctaLabel}
          </Link>
        </MarketingButton>
      </div>
    </section>
  )
}

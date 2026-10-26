import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  QR_MATERIALS_SECTION_BODY,
  QR_MATERIALS_SECTION_HEADING,
  QR_MATERIALS_SECTION_INSET,
  QR_MATERIALS_SECTION_Y,
  QR_MATERIALS_STARTER_KIT,
} from "@/content/marketing/qrMaterialsPage"
import { cn } from "@/lib/utils"

/**
 * Figma QR materials starter kit CTA `5661:19459`.
 * Eyebrow + title + body + Pilot CTA with note.
 */
export function QrMaterialsStarterKit() {
  return (
    <section
      className="w-full bg-[#f0f0f0]"
      aria-labelledby="qr-materials-starter-kit-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-[46px] lg:gap-[66px]",
          QR_MATERIALS_SECTION_INSET,
          QR_MATERIALS_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[748px] flex-col gap-[18px]">
          <p className="m-0 font-sans text-base font-medium uppercase leading-[normal] text-black">
            {QR_MATERIALS_STARTER_KIT.eyebrow}
          </p>
          <h2
            id="qr-materials-starter-kit-heading"
            className={cn("m-0 w-full", QR_MATERIALS_SECTION_HEADING)}
          >
            {QR_MATERIALS_STARTER_KIT.title}
          </h2>
          {QR_MATERIALS_STARTER_KIT.body.map((paragraph) => (
            <p key={paragraph} className={cn("m-0 w-full", QR_MATERIALS_SECTION_BODY)}>
              {paragraph}
            </p>
          ))}
        </header>

        <div className="flex flex-col items-start gap-5">
          <MarketingButton asChild variant="primary" showArrow={false}>
            <RequestTrialLink planIntent={{ plan: "Pilot" }}>
              {QR_MATERIALS_STARTER_KIT.ctaLabel}
            </RequestTrialLink>
          </MarketingButton>
          <p className="m-0 text-xs font-inter font-medium leading-[normal] text-[#727272]">
            {QR_MATERIALS_STARTER_KIT.note}
          </p>
        </div>
      </div>
    </section>
  )
}

import { CircleCheckBig } from "lucide-react"
import { Link } from "react-router-dom"

import counterDisplayBgImage from "@/assets/images/marketing/qr-materials/counter-display-bg.webp"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { PilotGradientPanel } from "@/components/marketing/pilot/PilotGradientPanel"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
} from "@/content/marketing/pilotPage"
import { QR_MATERIALS_COUNTER_DISPLAY } from "@/content/marketing/qrMaterialsPage"
import { cn } from "@/lib/utils"

/**
 * Figma QR materials Counter Display panel `6855:12460`.
 * Pilot gradient panel (`5681:46128`) with a wider copy block and no top
 * padding: it follows the pricing strip directly. Figma rotates the plate
 * 180° (dark corner top-right), so it ships as `counter-display-bg.webp`.
 */
export function QrMaterialsCounterDisplay() {
  const { planIntent } = QR_MATERIALS_COUNTER_DISPLAY

  return (
    <PilotGradientPanel
      headingId="qr-materials-counter-display-heading"
      className="rounded-b-[12px]"
      backgroundSrc={counterDisplayBgImage}
      innerClassName="lg:pt-0"
      copyClassName="max-w-[688px] gap-[22px]"
      footer={
        <div className="flex w-full flex-col items-center gap-8">
          <div className="flex flex-wrap items-center justify-center gap-[18px]">
            <MarketingButton asChild variant="primary" className="w-[186px]">
              <RequestTrialLink planIntent={planIntent}>
                {QR_MATERIALS_COUNTER_DISPLAY.primaryLabel}
              </RequestTrialLink>
            </MarketingButton>
            <MarketingButton asChild variant="secondary" className="w-[184px]">
              <Link to={QR_MATERIALS_COUNTER_DISPLAY.secondaryTo}>
                {QR_MATERIALS_COUNTER_DISPLAY.secondaryLabel}
              </Link>
            </MarketingButton>
          </div>
          <div className="flex flex-wrap items-center justify-center gap-[18px]">
            {QR_MATERIALS_COUNTER_DISPLAY.trustItems.map((item) => (
              <p
                key={item}
                className="m-0 flex items-center gap-2 text-xs font-inter font-medium leading-[normal] text-[#727272]"
              >
                <CircleCheckBig
                  aria-hidden
                  className="size-4 shrink-0 text-[#727272]"
                  strokeWidth={2}
                />
                {item}
              </p>
            ))}
          </div>
        </div>
      }
    >
      <h2
        id="qr-materials-counter-display-heading"
        className={cn("m-0 w-full text-black", PILOT_SECTION_HEADING)}
      >
        {QR_MATERIALS_COUNTER_DISPLAY.title}
      </h2>
      <p className={cn("m-0 w-full max-w-[682px]", PILOT_SECTION_BODY)}>
        {QR_MATERIALS_COUNTER_DISPLAY.body}
      </p>
    </PilotGradientPanel>
  )
}

import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { PilotGradientPanel } from "@/components/marketing/pilot/PilotGradientPanel"
import {
  PILOT_DAY_30,
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
} from "@/content/marketing/pilotPage"
import { cn } from "@/lib/utils"

/**
 * Figma Pilot “What happens on Day 30” `5661:20456`.
 * Gradient panel + secondary CTA to Pricing compare.
 */
export function PilotDay30() {
  return (
    <PilotGradientPanel
      headingId="pilot-day-30-heading"
      footer={
        <MarketingButton asChild variant="secondary" className="w-[175px]">
          <Link to={PILOT_DAY_30.ctaTo}>{PILOT_DAY_30.ctaLabel}</Link>
        </MarketingButton>
      }
    >
      <p className="m-0 font-sans text-sm font-medium uppercase leading-normal text-black lg:text-base">
        {PILOT_DAY_30.eyebrow}
      </p>
      <h2
        id="pilot-day-30-heading"
        className={cn("m-0 w-full text-black", PILOT_SECTION_HEADING)}
      >
        {PILOT_DAY_30.title}
      </h2>
      <p className={cn("m-0 w-full", PILOT_SECTION_BODY)}>{PILOT_DAY_30.body}</p>
    </PilotGradientPanel>
  )
}

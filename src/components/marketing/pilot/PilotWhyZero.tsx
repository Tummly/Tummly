import { CircleCheckBig } from "lucide-react"
import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { PilotGradientPanel } from "@/components/marketing/pilot/PilotGradientPanel"
import {
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
  PILOT_WHY_ZERO,
} from "@/content/marketing/pilotPage"
import {
  buildSignupPath,
  saveSignupPlanIntent,
} from "@/lib/signupPlanIntent"
import { cn } from "@/lib/utils"

/**
 * Figma Pilot mid-page CTA `5681:46128`.
 * Gradient panel + centred copy, dual CTAs and trust row.
 */
export function PilotWhyZero() {
  const signupTo = buildSignupPath(PILOT_WHY_ZERO.planIntent)

  return (
    <PilotGradientPanel
      headingId="pilot-why-zero-heading"
      footer={
        <div className="flex w-full flex-col items-center gap-8">
          <div className="flex flex-wrap items-center justify-center gap-[18px]">
            <MarketingButton asChild variant="primary" className="w-[186px]">
              <Link
                to={signupTo}
                onClick={() => {
                  saveSignupPlanIntent(PILOT_WHY_ZERO.planIntent)
                }}
              >
                {PILOT_WHY_ZERO.primaryLabel}
              </Link>
            </MarketingButton>
            <MarketingButton asChild variant="secondary" className="w-[180px]">
              <Link to={PILOT_WHY_ZERO.secondaryTo}>
                {PILOT_WHY_ZERO.secondaryLabel}
              </Link>
            </MarketingButton>
          </div>
          <div className="flex flex-wrap items-center justify-center gap-[18px]">
            {PILOT_WHY_ZERO.trustItems.map((item) => (
              <p
                key={item}
                className="m-0 flex items-center gap-2 text-xs font-inter font-medium leading-normal text-[#727272]"
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
      <p className="m-0 font-sans text-sm font-medium uppercase leading-normal text-black lg:text-base">
        {PILOT_WHY_ZERO.eyebrow}
      </p>
      <h2
        id="pilot-why-zero-heading"
        className={cn(
          "m-0 w-full max-w-[488px] text-black",
          PILOT_SECTION_HEADING,
        )}
      >
        {PILOT_WHY_ZERO.title}
      </h2>
      <p className={cn("m-0 w-full max-w-[558px]", PILOT_SECTION_BODY)}>
        {PILOT_WHY_ZERO.body}
      </p>
    </PilotGradientPanel>
  )
}

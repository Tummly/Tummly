import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  AI_ASSISTANT_MID_CTA,
  AI_ASSISTANT_SECTION_BODY,
  AI_ASSISTANT_SECTION_HEADING,
  AI_ASSISTANT_SECTION_INSET,
  AI_ASSISTANT_SECTION_Y,
} from "@/content/marketing/aiAssistantPage"
import { cn } from "@/lib/utils"

/** Figma mid CTA `5661:15749`. */
export function AiAssistantMidCta() {
  return (
    <section className="w-full bg-white">
      <div
        className={cn(
          "flex w-full flex-col items-center gap-[46px] lg:gap-[70px]",
          AI_ASSISTANT_SECTION_INSET,
          AI_ASSISTANT_SECTION_Y,
        )}
      >
        <div className="flex w-full max-w-[615px] flex-col items-center gap-[18px] text-center">
          <h2 className={cn("m-0 max-w-[566px]", AI_ASSISTANT_SECTION_HEADING)}>
            {AI_ASSISTANT_MID_CTA.title}
          </h2>
          <p className={cn("m-0 max-w-[582px]", AI_ASSISTANT_SECTION_BODY)}>
            {AI_ASSISTANT_MID_CTA.body}
          </p>
        </div>

        <div className="flex flex-wrap items-center justify-center gap-5">
          <MarketingButton asChild variant="primary">
            <RequestTrialLink planIntent={{ plan: "Pilot" }}>
              {AI_ASSISTANT_MID_CTA.primaryLabel}
            </RequestTrialLink>
          </MarketingButton>
          <MarketingButton asChild variant="secondary">
            <Link to={AI_ASSISTANT_MID_CTA.secondaryTo}>
              {AI_ASSISTANT_MID_CTA.secondaryLabel}
            </Link>
          </MarketingButton>
        </div>
      </div>
    </section>
  )
}

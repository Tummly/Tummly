import { CircleCheckBig } from "lucide-react"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { AiAssistantMutedVideo } from "@/components/marketing/ai-assistant/AiAssistantMutedVideo"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  AI_ASSISTANT_HERO,
  AI_ASSISTANT_SECTION_BODY,
  AI_ASSISTANT_SECTION_INSET,
} from "@/content/marketing/aiAssistantPage"
import { aiAssistantVideoUrl } from "@/lib/marketingMedia"
import { marketingHeroHeading } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/**
 * Figma AI Assistant hero `5661:14925`.
 * Top padding stays on the copy. Video sits in a sized frame and overlaps
 * upward behind the CTAs (no %-margin — margin-% is width-based and ate the top).
 */
export function AiAssistantHero() {
  return (
    <section className="relative w-full overflow-hidden rounded-tl-[8px] rounded-tr-[8px] bg-white">
      <div
        className={cn(
          "relative z-10 mx-auto flex w-full max-w-md flex-col items-center gap-7 pt-12.5 pb-4 text-center md:max-w-3xl lg:pt-[70px] lg:pb-6",
          AI_ASSISTANT_SECTION_INSET,
        )}
      >
        <div className="flex w-full flex-col items-center gap-[18px]">
          <h1
            className={cn(
              "m-0 w-full max-w-md md:max-w-3xl",
              marketingHeroHeading,
              "font-semibold text-black lg:text-[54px] lg:leading-[70px]",
            )}
          >
            {AI_ASSISTANT_HERO.title}
          </h1>
          <p
            className={cn(
              "m-0 w-full max-w-sm md:max-w-2xl",
              AI_ASSISTANT_SECTION_BODY,
            )}
          >
            {AI_ASSISTANT_HERO.body}
          </p>
        </div>

        <div className="flex flex-col items-center gap-7">
          <div className="inline-flex items-center gap-2 rounded-[30px] bg-[#ebebeb] px-2.5 py-2">
            <CircleCheckBig
              aria-hidden
              className="size-4 shrink-0 text-[#727272]"
              strokeWidth={2}
            />
            <p className="m-0 text-xs font-normal leading-normal text-[#727272]">
              {AI_ASSISTANT_HERO.badge}
            </p>
          </div>

          <div className="flex flex-wrap items-center justify-center gap-5">
            <MarketingButton asChild variant="primary">
              <RequestTrialLink planIntent={{ plan: "Pilot" }}>
                {AI_ASSISTANT_HERO.primaryLabel}
              </RequestTrialLink>
            </MarketingButton>
            <MarketingButton asChild variant="secondary">
              <a href={AI_ASSISTANT_HERO.secondaryHref}>
                {AI_ASSISTANT_HERO.secondaryLabel}
              </a>
            </MarketingButton>
          </div>
        </div>
      </div>

      <div className="relative z-0 aspect-[16/9] w-full lg:aspect-[1668/791]">
        <div className="absolute inset-x-0 top-0 bottom-0 md:-top-[50%] lg:-top-[55%]">
          <AiAssistantMutedVideo
            src={aiAssistantVideoUrl("heroMotion")}
            eager
            className="size-full bg-transparent"
          />
        </div>
      </div>
    </section>
  )
}

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
 * Figma AI Assistant hero `5661:14925` (1668×1060).
 * Video is full-bleed 1668×791, pinned to the bottom. A shorter flow band
 * (~470px on desktop) sits under the CTAs so the clip overlaps upward without
 * shrinking or cropping the video frame.
 */
export function AiAssistantHero() {
  return (
    <section className="relative w-full overflow-hidden rounded-tl-[8px] rounded-tr-[8px] bg-white">
      <div
        className={cn(
          "relative z-10 mx-auto flex w-full max-w-md flex-col items-center gap-7 pt-12.5 text-center md:max-w-3xl lg:pt-[70px]",
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
            <p className="m-0 text-xs font-normal leading-[normal] text-[#727272]">
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

      {/*
        Flow band under the CTAs. Shorter than the video (1668×791) so the clip
        overlaps upward. 470 is tighter than Figma’s ~626 visible band.
      */}
      <div
        aria-hidden
        className="relative w-full aspect-[16/8] lg:aspect-[1668/470]"
      />
      <div className="pointer-events-none absolute inset-x-0 bottom-0 z-0 aspect-video w-full lg:aspect-[1668/791]">
        <AiAssistantMutedVideo
          src={aiAssistantVideoUrl("heroMotion")}
          eager
          className="size-full bg-transparent"
        />
      </div>
    </section>
  )
}

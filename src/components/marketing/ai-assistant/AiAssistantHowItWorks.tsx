import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { AiAssistantMutedVideo } from "@/components/marketing/ai-assistant/AiAssistantMutedVideo"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  AI_ASSISTANT_HOW_IT_WORKS,
  AI_ASSISTANT_SECTION_BODY,
  AI_ASSISTANT_SECTION_HEADING,
  AI_ASSISTANT_SECTION_INSET,
  AI_ASSISTANT_SECTION_Y,
  type AiAssistantHowItWorksStep,
} from "@/content/marketing/aiAssistantPage"
import { MARKETING_DEMO_PATH } from "@/constants/marketingNav"
import { aiAssistantVideoUrl } from "@/lib/marketingMedia"
import { cn } from "@/lib/utils"

function StepRow({ step }: { step: AiAssistantHowItWorksStep }) {
  const textBlock = (
    <div className="flex w-full max-w-md shrink-0 flex-col items-start gap-13 md:max-w-3xl">
      <div className="flex w-full flex-col gap-4.5">
        <div className="flex flex-col gap-2">
          <p className="m-0 font-sans text-sm font-normal leading-5 text-black">
            {step.eyebrow}
          </p>
          <h3 className="m-0 font-jakarta text-[28px] font-medium leading-none text-[#141414] sm:text-[36px]">
            {step.title}
          </h3>
        </div>
        <p className={cn("m-0", AI_ASSISTANT_SECTION_BODY)}>{step.body}</p>
      </div>
      <div className="flex flex-wrap items-center gap-5">
        <MarketingButton asChild variant="dark">
          <RequestTrialLink planIntent={{ plan: "Pilot" }}>
            {step.primaryLabel}
          </RequestTrialLink>
        </MarketingButton>
        <MarketingButton asChild variant="secondary">
          <Link to={MARKETING_DEMO_PATH}>{step.secondaryLabel}</Link>
        </MarketingButton>
      </div>
    </div>
  )

  const mediaBlock = (
    <AiAssistantMutedVideo
      src={aiAssistantVideoUrl(step.videoKey)}
      className="aspect-758/580 w-full max-w-189.5 shrink-0 rounded-[12px] bg-[#efefef] md:flex-1 md:max-w-189.5"
    />
  )

  return (
    <div
      className={cn(
        "flex w-full items-center gap-10 md:gap-16 lg:gap-30",
        // Mobile: always Text → Video. Desktop: alternate sides.
        step.textFirst
          ? "flex-col md:flex-row md:justify-end"
          : "flex-col-reverse md:flex-row md:justify-start",
        "md:items-center",
      )}
    >
      {step.textFirst ? (
        <>
          {textBlock}
          {mediaBlock}
        </>
      ) : (
        <>
          {mediaBlock}
          {textBlock}
        </>
      )}
    </div>
  )
}

/** Figma How Tummly AI works `5892:31812`. */
export function AiAssistantHowItWorks() {
  return (
    <section className="w-full bg-white">
      <div
        className={cn(
          "flex w-full flex-col gap-16 lg:gap-17.5",
          AI_ASSISTANT_SECTION_INSET,
          AI_ASSISTANT_SECTION_Y,
        )}
      >
        <header className="mx-auto flex w-full max-w-141 flex-col items-center gap-4.5 text-center">
          <p className="m-0 font-sans text-base font-normal uppercase leading-normal text-black">
            {AI_ASSISTANT_HOW_IT_WORKS.eyebrow}
          </p>
          <h2 className={cn("m-0", AI_ASSISTANT_SECTION_HEADING)}>
            {AI_ASSISTANT_HOW_IT_WORKS.titleLine1}
            <br aria-hidden />
            {AI_ASSISTANT_HOW_IT_WORKS.titleLine2}
          </h2>
          <p className={cn("m-0", AI_ASSISTANT_SECTION_BODY)}>
            {AI_ASSISTANT_HOW_IT_WORKS.body}
          </p>
        </header>

        <div className="flex w-full flex-col gap-16 lg:gap-17.5">
          {AI_ASSISTANT_HOW_IT_WORKS.steps.map((step) => (
            <StepRow key={step.id} step={step} />
          ))}
        </div>
      </div>
    </section>
  )
}

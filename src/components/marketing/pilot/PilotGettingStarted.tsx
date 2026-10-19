import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  PILOT_GETTING_STARTED,
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
  PILOT_SECTION_INSET,
} from "@/content/marketing/pilotPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

const stepsImage = remoteSimpleImg(PILOT_GETTING_STARTED.mediaId)

/**
 * Figma Pilot “Getting Started” `6535:18445`.
 * Left copy + CTAs; right composite (stairs + step labels) as one asset.
 */
export function PilotGettingStarted() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="pilot-getting-started-heading"
    >
      <div className={cn(PILOT_SECTION_INSET, "py-10 lg:py-[60px]")}>
        <div
          className={cn(
            "flex w-full flex-col items-start gap-12 rounded-[12px] border-2 border-dashed border-[#c6c6c6] bg-[#f0f0f0]",
            "px-5 py-10 sm:px-10 lg:flex-row lg:items-center lg:gap-[120px] lg:px-[150px] lg:py-[50px]",
          )}
        >
          <div className="flex w-full max-w-[502px] flex-col items-start gap-[60px] lg:shrink-0">
            <div className="flex w-full max-w-[448px] flex-col items-start gap-[18px]">
              <div className="flex w-full flex-col items-start gap-3.5 text-black">
                <p className="m-0 font-sans text-sm font-medium uppercase leading-normal tracking-[1.12px]">
                  {PILOT_GETTING_STARTED.eyebrow}
                </p>
                <h2
                  id="pilot-getting-started-heading"
                  className={cn(
                    "m-0 w-full max-w-[434px] text-black",
                    PILOT_SECTION_HEADING,
                    "lg:text-[46px] lg:leading-none",
                  )}
                >
                  {PILOT_GETTING_STARTED.title}
                </h2>
              </div>
              <p className={cn("m-0 w-full max-w-[502px]", PILOT_SECTION_BODY)}>
                {PILOT_GETTING_STARTED.body}
              </p>
            </div>

            <div className="flex flex-wrap items-center gap-[22px]">
              <MarketingButton asChild variant="primary">
                <RequestTrialLink planIntent={PILOT_GETTING_STARTED.planIntent}>
                  {PILOT_GETTING_STARTED.primaryLabel}
                </RequestTrialLink>
              </MarketingButton>
              <MarketingButton asChild variant="secondary">
                <Link to={PILOT_GETTING_STARTED.secondaryTo}>
                  {PILOT_GETTING_STARTED.secondaryLabel}
                </Link>
              </MarketingButton>
            </div>
          </div>

          <div className="w-full min-w-0 lg:max-w-[753px] lg:flex-1">
            <img
              src={stepsImage.src}
              srcSet={stepsImage.srcSet}
              sizes="(min-width: 1024px) 753px, 100vw"
              alt={PILOT_GETTING_STARTED.imageAlt}
              width={753}
              height={510}
              className="h-auto w-full max-w-[753px] select-none"
              decoding="async"
              draggable={false}
            />
            <ol className="sr-only">
              {PILOT_GETTING_STARTED.steps.map((step) => (
                <li key={step.id}>
                  Step {step.number}. {step.title}. {step.body}
                </li>
              ))}
            </ol>
          </div>
        </div>
      </div>
    </section>
  )
}

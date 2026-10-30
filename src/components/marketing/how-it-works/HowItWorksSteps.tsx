import { ChevronLeft, ChevronRight } from "lucide-react"
import type { CSSProperties } from "react"
import { Link } from "react-router-dom"

import inviteFeedbackImage from "@/assets/images/marketing/how-it-works/step-invite-feedback.png"
import reviewRespondImage from "@/assets/images/marketing/how-it-works/step-review-respond.png"
import setUpLocationImage from "@/assets/images/marketing/how-it-works/step-set-up-location.png"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  HOW_IT_WORKS_SECTION_BODY,
  HOW_IT_WORKS_SECTION_EYEBROW,
  HOW_IT_WORKS_SECTION_HEADING,
  HOW_IT_WORKS_SECTION_INSET,
  HOW_IT_WORKS_STEPS,
  type HowItWorksStepId,
} from "@/content/marketing/howItWorksPage"
import { buildSignupPath, saveSignupPlanIntent } from "@/lib/signupPlanIntent"
import { cn } from "@/lib/utils"

/** Figma illustrations `6865:35345`, `6865:35370`, `6865:35407`, exported at 2×. */
const STEP_IMAGES: Record<HowItWorksStepId, string> = {
  "set-up": setUpLocationImage,
  invite: inviteFeedbackImage,
  review: reviewRespondImage,
}

/**
 * Figma chevron `6865:35323` on the dashed loop rail.
 * No fill — a grey chip would sit half on the white page and read as a box.
 * Top row points right, bottom row points left (the loop runs clockwise).
 * Same approach as PilotWhyTummly `LoopHalfArrows`.
 */
function LoopChevron({
  direction,
  className,
}: {
  direction: "right" | "left"
  className: string
}) {
  const Icon = direction === "right" ? ChevronRight : ChevronLeft
  return (
    <span
      aria-hidden
      className={cn(
        "absolute hidden h-4 w-6 -translate-x-1/2 items-center justify-center overflow-hidden lg:flex",
        className,
      )}
    >
      <Icon className="size-3.5 text-[#a2a2a2]" strokeWidth={1.87} />
    </span>
  )
}

/**
 * Figma How it works steps `6865:35312`.
 * Three white cards inside a dashed grey loop (two stacked 28px-radius halves
 * with chevron chips). Cards stack on mobile and drop the loop chrome.
 */
export function HowItWorksSteps() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="how-it-works-steps-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-center gap-10 pt-12.5 pb-17.5 lg:gap-14 lg:pt-[70px] lg:pb-[100px]",
          HOW_IT_WORKS_SECTION_INSET,
        )}
      >
        <div className="flex w-full max-w-[1270px] flex-col items-center gap-10 lg:gap-14">
          <header className="flex w-full max-w-[578px] flex-col items-center gap-[22px] text-center">
            <div className="flex w-full flex-col items-center gap-3.5">
              <p className={HOW_IT_WORKS_SECTION_EYEBROW}>
                {HOW_IT_WORKS_STEPS.eyebrow}
              </p>
              <h2
                id="how-it-works-steps-heading"
                className={cn("m-0 w-full", HOW_IT_WORKS_SECTION_HEADING)}
              >
                {HOW_IT_WORKS_STEPS.title}
              </h2>
            </div>
            <p
              className={cn(
                "m-0 w-full max-w-[462px]",
                HOW_IT_WORKS_SECTION_BODY,
              )}
            >
              {HOW_IT_WORKS_STEPS.body}
            </p>
          </header>

          <div className="relative w-full lg:h-[550px]">
            {/* Loop chrome (desktop): two dashed halves that meet in the middle. */}
            <div
              aria-hidden
              className="absolute inset-0 hidden grid-rows-2 lg:grid"
            >
              <div className="relative rounded-[28px] border border-dashed border-[#a2a2a2] bg-[#ededed]" />
              <div className="relative rounded-[28px] border border-dashed border-[#a2a2a2] bg-[#ededed]" />
            </div>
            <LoopChevron
              direction="right"
              className="top-[-8px] left-[6%]"
            />
            <LoopChevron
              direction="right"
              className="top-[-8px] left-1/2"
            />
            <LoopChevron
              direction="right"
              className="top-[-8px] left-[83.7%]"
            />
            <LoopChevron
              direction="left"
              className="bottom-[-8px] left-[14.7%]"
            />
            <LoopChevron
              direction="left"
              className="bottom-[-8px] left-1/2"
            />
            <LoopChevron
              direction="left"
              className="bottom-[-8px] left-[92.5%]"
            />

            <ol className="relative m-0 grid list-none grid-cols-1 gap-4 rounded-[28px] border border-dashed border-[#a2a2a2] bg-[#ededed] p-4 lg:absolute lg:inset-x-[3.46%] lg:inset-y-10 lg:grid-cols-3 lg:gap-0 lg:rounded-none lg:border-0 lg:bg-transparent lg:p-0">
              {HOW_IT_WORKS_STEPS.steps.map((step) => (
                <li
                  key={step.id}
                  className="relative m-0 flex min-w-0 flex-col items-center gap-6 rounded-[22px] border border-[#cecece] bg-white px-5 py-8 text-center text-[#141414] lg:h-[470px] lg:gap-0 lg:overflow-hidden lg:p-0"
                >
                  <h3
                    className={cn(
                      "relative z-10 m-0 font-jakarta text-2xl leading-[normal] font-semibold lg:absolute lg:inset-x-0 lg:top-[50px] lg:-translate-y-1/2",
                      step.titleDrop && "lg:top-[66px]",
                    )}
                  >
                    {step.title}
                  </h3>
                  <img
                    src={STEP_IMAGES[step.id]}
                    alt={step.imageAlt}
                    width={step.artSize}
                    height={step.artSize}
                    className="aspect-square w-[240px] max-w-full select-none lg:absolute lg:top-1/2 lg:left-1/2 lg:mt-[var(--art-offset)] lg:w-[var(--art-size)] lg:-translate-x-1/2 lg:-translate-y-1/2"
                    style={
                      {
                        "--art-size": `${step.artSize}px`,
                        "--art-offset": `${step.artOffset}px`,
                      } as CSSProperties
                    }
                    decoding="async"
                    draggable={false}
                  />
                  {/*
                    Figma body boxes share one top edge (y=373). Their widths are
                    the Figma box widths widened for Helvetica Neue so each line
                    breaks where Figma breaks it; the cap keeps text inside the card.
                  */}
                  <p
                    className="m-0 max-w-[318px] font-sans text-sm leading-[18px] font-normal lg:absolute lg:top-[373px] lg:left-1/2 lg:w-[var(--body-width)] lg:max-w-[calc(100%-24px)] lg:-translate-x-1/2"
                    style={
                      { "--body-width": `${step.bodyWidth}px` } as CSSProperties
                    }
                  >
                    {step.body}
                  </p>
                </li>
              ))}
            </ol>
          </div>
        </div>

        <div className="flex w-full flex-wrap items-center justify-center gap-5">
          <MarketingButton
            asChild
            variant="primary"
            className="w-full sm:h-[46px] sm:w-[162px]"
          >
            <Link
              to={buildSignupPath(HOW_IT_WORKS_STEPS.planIntent)}
              onClick={() => {
                saveSignupPlanIntent(HOW_IT_WORKS_STEPS.planIntent)
              }}
            >
              {HOW_IT_WORKS_STEPS.primaryLabel}
            </Link>
          </MarketingButton>
          <MarketingButton
            asChild
            variant="secondary"
            className="w-full sm:h-[46px] sm:w-[179px]"
          >
            <Link to={HOW_IT_WORKS_STEPS.secondaryTo}>
              {HOW_IT_WORKS_STEPS.secondaryLabel}
            </Link>
          </MarketingButton>
        </div>
      </div>
    </section>
  )
}

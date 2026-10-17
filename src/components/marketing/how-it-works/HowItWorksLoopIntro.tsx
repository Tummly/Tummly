import {
  HOW_IT_WORKS_LOOP_INTRO,
  HOW_IT_WORKS_SECTION_BODY,
  HOW_IT_WORKS_SECTION_HEADING,
  HOW_IT_WORKS_SECTION_INSET,
  HOW_IT_WORKS_SECTION_Y,
} from "@/content/marketing/howItWorksPage"
import { cn } from "@/lib/utils"

/**
 * Figma How it works loop intro `5661:19534`.
 * Left-aligned title + body on `#f0f0f0`.
 */
export function HowItWorksLoopIntro() {
  return (
    <section
      className="w-full bg-[#f0f0f0]"
      aria-labelledby="how-it-works-loop-intro-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-start justify-center",
          HOW_IT_WORKS_SECTION_INSET,
          HOW_IT_WORKS_SECTION_Y,
        )}
      >
        <div className="flex w-full max-w-[776px] flex-col items-start gap-[18px]">
          <h2
            id="how-it-works-loop-intro-heading"
            className={cn("m-0 w-full", HOW_IT_WORKS_SECTION_HEADING)}
          >
            {HOW_IT_WORKS_LOOP_INTRO.title}
          </h2>
          <p
            className={cn(
              "m-0 w-full max-w-[689px]",
              HOW_IT_WORKS_SECTION_BODY,
            )}
          >
            {HOW_IT_WORKS_LOOP_INTRO.body}
          </p>
        </div>
      </div>
    </section>
  )
}

import heroFrameBgImage from "@/assets/images/marketing/how-it-works/hero-frame-bg.webp"
import heroVisualImage from "@/assets/images/marketing/how-it-works/hero-feedback-visual.webp"
import { HeroSignupForm } from "@/components/home/HeroSignupForm"
import {
  HOW_IT_WORKS_HERO,
  HOW_IT_WORKS_SECTION_BODY,
  HOW_IT_WORKS_SECTION_EYEBROW,
  HOW_IT_WORKS_SECTION_INSET,
  HOW_IT_WORKS_SECTION_Y,
} from "@/content/marketing/howItWorksPage"
import { cn } from "@/lib/utils"

/**
 * Figma How it works hero `5661:19524`.
 * Left copy + sign-up form; right green frame `6863:12782` (724×609) with the
 * Feedback dashboard visual `7020:14317` (726×657) bleeding off its right edge.
 */
export function HowItWorksHero() {
  return (
    <section className="w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-[#fafafa]">
      <div
        className={cn(
          "flex w-full flex-col items-start gap-10 lg:flex-row lg:items-center lg:gap-[70px]",
          HOW_IT_WORKS_SECTION_INSET,
          HOW_IT_WORKS_SECTION_Y,
        )}
      >
        <div className="flex w-full min-w-0 flex-col items-start gap-10 lg:flex-1 lg:gap-[46px]">
          <div className="flex w-full flex-col items-start gap-[22px]">
            <div className="flex w-full flex-col items-start gap-3.5">
              <p className={HOW_IT_WORKS_SECTION_EYEBROW}>
                {HOW_IT_WORKS_HERO.eyebrow}
              </p>
              <h1
                className={cn(
                  "m-0 w-full max-w-[564px] font-jakarta font-semibold text-black",
                  "text-[34px] leading-10 lg:text-[54px] lg:leading-[60px]",
                )}
              >
                {HOW_IT_WORKS_HERO.title}
              </h1>
            </div>
            <p
              className={cn(
                "m-0 w-full max-w-[597px]",
                HOW_IT_WORKS_SECTION_BODY,
              )}
            >
              {HOW_IT_WORKS_HERO.body}
            </p>
          </div>

          <HeroSignupForm
            align="start"
            legalNote="read"
            trustBadge={HOW_IT_WORKS_HERO.trustBadge}
            emailPlaceholder={HOW_IT_WORKS_HERO.emailPlaceholder}
            primaryLabel={HOW_IT_WORKS_HERO.primaryLabel}
            planIntent={HOW_IT_WORKS_HERO.planIntent}
            primaryButtonClassName="sm:w-auto"
          />
        </div>

        <div className="relative aspect-[724/609] w-full shrink-0 overflow-hidden rounded-[12px] lg:w-[min(36%,724px)] xl:w-[min(50%,724px)]">
          <img
            src={heroFrameBgImage}
            alt=""
            width={1600}
            height={1600}
            className="pointer-events-none absolute inset-0 size-full object-cover object-[70%_30%]"
            decoding="async"
            aria-hidden
          />
          {/* Figma `7020:14320`: 726×657 at (3, 10); a white strip continues the dashboard to the frame edge. */}
          <div className="absolute top-[1.64%] left-[0.41%] h-[107.88%] w-[100.28%]">
            <img
              src={heroVisualImage}
              alt={HOW_IT_WORKS_HERO.imageAlt}
              width={1400}
              height={1400}
              className="absolute top-0 left-0 h-[100.83%] w-[91.05%] max-w-none"
              decoding="async"
              fetchPriority="high"
            />
          </div>
          <div
            aria-hidden
            className="absolute top-[8.7%] left-[91.3%] h-[95.4%] w-[9.3%] bg-white"
          />
        </div>
      </div>
    </section>
  )
}

import { HeroSignupForm } from "@/components/home/HeroSignupForm"
import {
  HOW_IT_WORKS_HERO,
  HOW_IT_WORKS_SECTION_BODY,
  HOW_IT_WORKS_SECTION_INSET,
  HOW_IT_WORKS_SECTION_Y,
} from "@/content/marketing/howItWorksPage"
import { marketingHeroHeading } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/**
 * Figma How it works hero `5661:19524`.
 * Centered title + body + Pilot signup (no trust badge).
 */
export function HowItWorksHero() {
  return (
    <section className="w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-[#fafafa]">
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-center gap-[46px]",
          HOW_IT_WORKS_SECTION_INSET,
          HOW_IT_WORKS_SECTION_Y,
        )}
      >
        <div className="flex w-full max-w-[880px] flex-col items-center gap-[18px] text-center">
          <h1
            className={cn(
              "m-0 w-full text-black",
              marketingHeroHeading,
              "font-semibold lg:text-[54px] lg:leading-[70px]",
            )}
          >
            {HOW_IT_WORKS_HERO.title}
          </h1>
          <p
            className={cn(
              "m-0 w-full max-w-[803px]",
              HOW_IT_WORKS_SECTION_BODY,
            )}
          >
            {HOW_IT_WORKS_HERO.body}
          </p>
        </div>

        <HeroSignupForm showTrustBadge={false} />
      </div>
    </section>
  )
}

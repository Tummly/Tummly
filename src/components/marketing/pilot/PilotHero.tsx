import { HeroSignupForm } from "@/components/home/HeroSignupForm"
import {
  PILOT_HERO,
  PILOT_SECTION_BODY,
  PILOT_SECTION_INSET,
  PILOT_SECTION_Y,
} from "@/content/marketing/pilotPage"
import { marketingHeroHeading } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/**
 * Figma 30-day Pilot hero `5661:19874`.
 * Eyebrow + title + body + Pilot signup + live-use footnote.
 */
export function PilotHero() {
  return (
    <section className="w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-[#fafafa]">
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-center gap-[46px]",
          PILOT_SECTION_INSET,
          PILOT_SECTION_Y,
        )}
      >
        <div className="flex w-full max-w-[888px] flex-col items-center gap-[18px] text-center">
          <div className="flex w-full flex-col items-center gap-3.5">
            <p className="m-0 w-full font-sans text-sm font-medium uppercase leading-normal text-black lg:text-base">
              {PILOT_HERO.eyebrow}
            </p>
            <h1
              className={cn(
                "m-0 w-full text-black",
                marketingHeroHeading,
                "font-semibold lg:text-[54px] lg:leading-[60px]",
              )}
            >
              {PILOT_HERO.title}
            </h1>
          </div>
          <p
            className={cn(
              "m-0 w-full max-w-[690px]",
              PILOT_SECTION_BODY,
            )}
          >
            {PILOT_HERO.body}
          </p>
        </div>

        <HeroSignupForm
          trustBadge={PILOT_HERO.trustBadge}
          emailPlaceholder={PILOT_HERO.emailPlaceholder}
          primaryLabel={PILOT_HERO.primaryLabel}
          planIntent={PILOT_HERO.planIntent}
        />

        <p className="m-0 text-center font-sans text-xs font-medium leading-normal text-[#727272]">
          {PILOT_HERO.footnote}
        </p>
      </div>
    </section>
  )
}

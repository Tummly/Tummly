import { HeroSignupForm } from "@/components/home/HeroSignupForm"
import {
  FOR_GROUPS_HERO,
  FOR_GROUPS_SECTION_BODY,
  FOR_GROUPS_SECTION_INSET,
  FOR_GROUPS_SECTION_Y,
} from "@/content/marketing/forGroupsPage"
import { marketingHeroHeading } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/**
 * Figma For Groups hero `5661:20525`.
 * Eyebrow + title + body + Pilot signup (trust badge shown).
 */
export function ForGroupsHero() {
  return (
    <section className="w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-[#fafafa]">
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-center gap-[46px]",
          FOR_GROUPS_SECTION_INSET,
          FOR_GROUPS_SECTION_Y,
        )}
      >
        <div className="flex w-full max-w-[739px] flex-col items-center gap-[18px] text-center">
          <div className="flex w-full flex-col items-center gap-3.5">
            <p className="m-0 w-full font-jakarta text-sm font-medium leading-4 text-[#4a4a4a]">
              {FOR_GROUPS_HERO.eyebrow}
            </p>
            <h1
              className={cn(
                "m-0 w-full max-w-[648px] text-black",
                marketingHeroHeading,
                "font-semibold lg:text-[54px] lg:leading-[60px]",
              )}
            >
              {FOR_GROUPS_HERO.title}
            </h1>
          </div>
          <p
            className={cn(
              "m-0 w-full max-w-[648px]",
              FOR_GROUPS_SECTION_BODY,
            )}
          >
            {FOR_GROUPS_HERO.body}
          </p>
        </div>

        <HeroSignupForm
          trustBadge={FOR_GROUPS_HERO.trustBadge}
          emailPlaceholder={FOR_GROUPS_HERO.emailPlaceholder}
          primaryLabel={FOR_GROUPS_HERO.primaryLabel}
          planIntent={FOR_GROUPS_HERO.planIntent}
        />
      </div>
    </section>
  )
}

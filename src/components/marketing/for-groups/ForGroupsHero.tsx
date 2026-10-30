import heroImage from "@/assets/images/marketing/for-groups/hero.webp"
import laurel from "@/assets/svg/marketing/for-groups/laurel.svg"
import { HeroSignupForm } from "@/components/home/HeroSignupForm"
import { MarketingCertificationCarousel } from "@/components/marketing/MarketingCertificationCarousel"
import {
  FOR_GROUPS_HERO,
  FOR_GROUPS_SECTION_INSET,
  FOR_GROUPS_SECTION_Y,
} from "@/content/marketing/forGroupsPage"
import { PRODUCT_EYEBROW, PRODUCT_TRUST } from "@/content/marketing/productPage"
import { marketingHeroHeading } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

const heroBodyClass = "font-sans text-base font-normal leading-6 text-[#141414]"

/**
 * Figma For Groups hero `5661:20525`.
 * Laurel eyebrow + title + body + Pilot signup + hero photo + trust carousel.
 */
export function ForGroupsHero() {
  return (
    <section
      className="w-full overflow-hidden rounded-[12px] bg-white"
      aria-labelledby="for-groups-hero-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-center gap-10 lg:gap-[70px]",
          FOR_GROUPS_SECTION_INSET,
          FOR_GROUPS_SECTION_Y,
        )}
      >
        <div className="flex w-full flex-col items-center gap-[46px]">
          <div className="flex w-full max-w-[716px] flex-col items-center gap-[22px] text-center">
            <div className="flex w-full flex-col items-center gap-3.5">
              <div className="flex items-center">
                <span className="flex size-[38px] shrink-0 items-start justify-start pl-2.5 pt-[2.8px]">
                  <img
                    src={laurel}
                    alt=""
                    width={13.4328}
                    height={30.9522}
                    aria-hidden
                    decoding="async"
                  />
                </span>
                <p className={cn(PRODUCT_EYEBROW, "text-center")}>
                  {FOR_GROUPS_HERO.eyebrow}
                </p>
                <span className="flex size-[38px] shrink-0 items-start justify-end pr-2.5 pt-[2.8px]">
                  <img
                    src={laurel}
                    alt=""
                    width={13.4328}
                    height={30.9522}
                    className="-scale-x-100"
                    aria-hidden
                    decoding="async"
                  />
                </span>
              </div>
              <h1
                id="for-groups-hero-heading"
                className={cn(
                  "m-0 w-full text-black",
                  marketingHeroHeading,
                  "font-semibold lg:text-[54px] lg:leading-[60px]",
                )}
              >
                {FOR_GROUPS_HERO.title}
              </h1>
            </div>
            <p className={cn("m-0 w-full max-w-[598px]", heroBodyClass)}>
              {FOR_GROUPS_HERO.body}
            </p>
          </div>

          <HeroSignupForm
            showTrustBadge={false}
            emailPlaceholder={FOR_GROUPS_HERO.emailPlaceholder}
            primaryLabel={FOR_GROUPS_HERO.primaryLabel}
            planIntent={FOR_GROUPS_HERO.planIntent}
            legalNote="read"
            className="lg:max-w-[573px] [&_p]:max-w-[382px]"
          />
        </div>

        {/* Figma `6392:18184` is 1548×763. */}
        <img
          src={heroImage}
          alt=""
          width={3096}
          height={1526}
          className="aspect-[1548/763] w-full rounded-[12px] bg-[#ebebeb] object-cover"
          fetchPriority="high"
          decoding="async"
          aria-hidden
        />

        <div className="flex w-full flex-col items-center gap-10">
          <p className="m-0 text-center font-sans text-lg font-medium leading-[normal] text-black">
            {FOR_GROUPS_HERO.trustTitle}
          </p>
          <MarketingCertificationCarousel
            certifications={PRODUCT_TRUST.certifications}
          />
        </div>
      </div>
    </section>
  )
}

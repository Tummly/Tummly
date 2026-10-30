import { forGroupsHeroImage } from "@/assets/for-groups-images"
import laurel from "@/assets/svg/marketing/for-groups/laurel.svg"
import { HeroSignupForm } from "@/components/home/HeroSignupForm"
import { MarketingCertificationCarousel } from "@/components/marketing/MarketingCertificationCarousel"
import {
  FOR_GROUPS_HERO,
  FOR_GROUPS_SECTION_INSET,
  FOR_GROUPS_SECTION_Y,
} from "@/content/marketing/forGroupsPage"
import { PRODUCT_EYEBROW } from "@/content/marketing/productPage"
import { publicForGroupsTrustBadges } from "@/content/marketing/publicTrustClaims"
import { marketingHeroHeading } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

const heroBodyClass = "font-sans text-base font-normal leading-6 text-[#141414]"

/**
 * Figma For Groups hero `5661:20525`.
 * Laurel eyebrow + title + body + Pilot signup + hero photo + trust carousel
 * (hidden behind `publicForGroupsTrustBadges()` until the claims are approved).
 */
export function ForGroupsHero() {
  const trustBadges = publicForGroupsTrustBadges()

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
                <span className="flex size-6 shrink-0 items-start justify-start sm:size-[38px] sm:pl-2.5 sm:pt-[2.8px]">
                  <img
                    src={laurel}
                    alt=""
                    width={13.4328}
                    height={30.9522}
                    className="max-sm:h-6 max-sm:w-auto"
                    aria-hidden
                    decoding="async"
                  />
                </span>
                <p className={cn(PRODUCT_EYEBROW, "text-center")}>
                  {FOR_GROUPS_HERO.eyebrow}
                </p>
                <span className="flex size-6 shrink-0 items-start justify-end sm:size-[38px] sm:pr-2.5 sm:pt-[2.8px]">
                  <img
                    src={laurel}
                    alt=""
                    width={13.4328}
                    height={30.9522}
                    className="-scale-x-100 max-sm:h-6 max-sm:w-auto"
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
            // Figma `5661:20525`: 444px field, 10px gap, 119px button.
            primaryButtonClassName="sm:w-[119px]"
            className="lg:max-w-[573px]"
          />
        </div>

        {/* Figma `6392:18184` is 1548×763. */}
        <img
          src={forGroupsHeroImage.src}
          srcSet={forGroupsHeroImage.srcSet}
          sizes="(min-width: 1024px) min(1548px, calc(100vw - 180px)), calc(100vw - 40px)"
          alt=""
          width={forGroupsHeroImage.width}
          height={forGroupsHeroImage.height}
          className="aspect-[1548/763] w-full rounded-[12px] bg-[#ebebeb] object-cover"
          fetchPriority="high"
          decoding="async"
          aria-hidden
        />

        {trustBadges.length > 0 ? (
          <div className="flex w-full flex-col items-center gap-10">
            <p className="m-0 text-center font-sans text-lg font-medium leading-[normal] text-black">
              {FOR_GROUPS_HERO.trustTitle}
            </p>
            <MarketingCertificationCarousel certifications={trustBadges} />
          </div>
        ) : null}
      </div>
    </section>
  )
}

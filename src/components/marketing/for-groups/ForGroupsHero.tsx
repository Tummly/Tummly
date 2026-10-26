import eyebrowSparkle from "@/assets/svg/marketing/for-groups-eyebrow-sparkle.svg"
import { HeroSignupForm } from "@/components/home/HeroSignupForm"
import { AiAssistantMutedVideo } from "@/components/marketing/ai-assistant/AiAssistantMutedVideo"
import {
  FOR_GROUPS_HERO,
  FOR_GROUPS_HERO_TRUST,
  FOR_GROUPS_SECTION_INSET,
  FOR_GROUPS_SECTION_Y,
} from "@/content/marketing/forGroupsPage"
import { publicForGroupsTrustBadges } from "@/content/marketing/publicTrustClaims"
import { marketingHeroHeading } from "@/lib/marketing-layout"
import { forGroupsHeroVideoUrl } from "@/lib/marketingMedia"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

const trustBadgeImages = Object.fromEntries(
  FOR_GROUPS_HERO_TRUST.badges.map((badge) => [
    badge.id,
    remoteSimpleImg(badge.mediaId),
  ]),
) as Record<
  (typeof FOR_GROUPS_HERO_TRUST.badges)[number]["id"],
  ReturnType<typeof remoteSimpleImg>
>

const heroBodyClass =
  "font-sans text-base font-normal leading-6 text-[#141414]"

/**
 * Figma For Groups hero `5661:20525`.
 * Eyebrow + sparkles + title + body + Pilot signup + hero video + trust badges.
 */
export function ForGroupsHero() {
  const trustBadges = publicForGroupsTrustBadges()

  return (
    <section className="w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-[#fafafa]">
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-center gap-10 lg:gap-[70px]",
          FOR_GROUPS_SECTION_INSET,
          FOR_GROUPS_SECTION_Y,
        )}
      >
        <div className="flex w-full flex-col items-center gap-[46px]">
          <div className="flex w-full max-w-[648px] flex-col items-center gap-[22px] text-center">
            <div className="relative flex w-full flex-col items-center gap-3.5">
              <div className="relative inline-flex items-center justify-center">
                <img
                  src={eyebrowSparkle}
                  alt=""
                  width={22}
                  height={30}
                  className="pointer-events-none absolute top-1/2 right-full mr-2 size-auto max-h-[30px] w-[22px] -translate-y-[60%] -scale-x-100"
                  aria-hidden
                  decoding="async"
                />
                <p className="m-0 font-jakarta text-sm font-medium leading-4 tracking-[1.12px] text-[#4a4a4a]">
                  {FOR_GROUPS_HERO.eyebrow}
                </p>
                <img
                  src={eyebrowSparkle}
                  alt=""
                  width={22}
                  height={30}
                  className="pointer-events-none absolute top-1/2 left-full ml-2 size-auto max-h-[30px] w-[22px] -translate-y-[60%]"
                  aria-hidden
                  decoding="async"
                />
              </div>
              <h1
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
          />
        </div>

        {/* Frame matches prior editorial 1548×763 CSS aspect. */}
        <div className="aspect-[1548/763] w-full overflow-hidden rounded-[12px] bg-[#ebebeb]">
          <AiAssistantMutedVideo
            src={forGroupsHeroVideoUrl()}
            eager
            className="size-full bg-[#ebebeb]"
          />
        </div>

        {trustBadges.length > 0 ? (
          <div className="flex w-full flex-col items-center gap-10">
            <p className="m-0 text-center font-sans text-lg font-medium leading-[normal] text-black">
              {FOR_GROUPS_HERO_TRUST.title}
            </p>
            <ul className="m-0 flex w-full list-none flex-wrap items-start justify-center gap-x-4 gap-y-8 p-0 lg:flex-nowrap lg:justify-between lg:gap-0">
              {trustBadges.map((badge) => {
                const image = trustBadgeImages[badge.id]
                return (
                  <li
                    key={badge.id}
                    className="flex w-[172px] shrink-0 flex-col items-center gap-6"
                  >
                    <img
                      src={image.src}
                      srcSet={image.srcSet}
                      sizes="172px"
                      alt=""
                      width={172}
                      height={152}
                      className="block h-[152px] w-[172px] rounded-[12px] object-cover"
                      aria-hidden
                      decoding="async"
                    />
                    <p className="m-0 w-[132px] text-center font-sans text-sm font-normal leading-[17px] text-black">
                      {badge.label}
                    </p>
                  </li>
                )
              })}
            </ul>
          </div>
        ) : null}
      </div>
    </section>
  )
}

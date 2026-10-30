import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import { MARKETING_HOW_IT_WORKS_PATH } from "@/constants/marketingNav"
import {
  ABOUT_HERO,
  ABOUT_HERO_BODY,
  ABOUT_HERO_EYEBROW,
  ABOUT_HERO_HEADING,
  ABOUT_HERO_Y,
  ABOUT_SECTION_INSET,
} from "@/content/marketing/aboutPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

/** Figma About hero buttons `6610:33200` / `6610:33208` are 8px radius (shared button default is 4px). */
const ABOUT_HERO_BUTTON = "rounded-[8px]"

const heroBandImage = remoteSimpleImg("about/hero-band")

/**
 * Figma About Tummly hero `5673:24144`.
 * Centre copy → CTAs → full-width hospitality band (`5673:24165`).
 */
export function AboutHero() {
  return (
    <section className="w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-white">
      <div
        className={cn(
          "flex w-full flex-col items-center gap-10 lg:gap-[50px]",
          ABOUT_SECTION_INSET,
          ABOUT_HERO_Y,
        )}
      >
        <div className="flex w-full max-w-[739px] flex-col items-center overflow-hidden">
          <div className="flex w-full flex-col items-center gap-6 text-center">
            <div className="flex w-full flex-col items-center gap-3.5">
              <p className={cn("m-0 w-full", ABOUT_HERO_EYEBROW)}>
                {ABOUT_HERO.eyebrow}
              </p>
              <h1 className={cn("m-0 w-full max-w-[712px]", ABOUT_HERO_HEADING)}>
                {ABOUT_HERO.title}
              </h1>
            </div>
            <p className={cn("m-0 w-full max-w-[594px]", ABOUT_HERO_BODY)}>
              {ABOUT_HERO.body}
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-center justify-center gap-[22px]">
          <MarketingButton
            asChild
            variant="primary"
            className={ABOUT_HERO_BUTTON}
          >
            <RequestTrialLink planIntent={{ plan: "Pilot" }}>
              {ABOUT_HERO.primaryLabel}
            </RequestTrialLink>
          </MarketingButton>
          <MarketingButton
            asChild
            variant="secondary"
            className={ABOUT_HERO_BUTTON}
          >
            <Link to={MARKETING_HOW_IT_WORKS_PATH}>
              {ABOUT_HERO.secondaryLabel}
            </Link>
          </MarketingButton>
        </div>
      </div>

      <div className="mt-10 w-full overflow-hidden lg:mt-[50px]">
        <img
          src={heroBandImage.src}
          srcSet={heroBandImage.srcSet}
          sizes="(min-width: 1024px) min(1860px, 100vw), 100vw"
          alt={ABOUT_HERO.imageAlt}
          width={heroBandImage.width}
          height={heroBandImage.height}
          className="block h-auto w-full"
          decoding="async"
          fetchPriority="high"
        />
      </div>
    </section>
  )
}

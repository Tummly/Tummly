import { CircleCheckBig } from "lucide-react"

import { HeroSignupForm } from "@/components/home/HeroSignupForm"
import {
  PILOT_HERO,
  PILOT_SECTION_BODY,
  PILOT_SECTION_INSET,
} from "@/content/marketing/pilotPage"
import { marketingHeroHeading } from "@/lib/marketing-layout"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

const heroImage = remoteSimpleImg(PILOT_HERO.mediaId)

/**
 * Figma 30-day Pilot hero `5661:19874`.
 * Left: eyebrow + title + body + signup + trust items.
 * Right: lifestyle frame `6426:17174` (blob `pilot/hero`).
 */
export function PilotHero() {
  return (
    <section className="w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-[#fafafa]">
      <div
        className={cn(
          "flex w-full flex-col items-start gap-10 lg:flex-row lg:items-center lg:gap-[70px]",
          PILOT_SECTION_INSET,
          "pt-[50px] pb-[50px] lg:pt-[70px]",
        )}
      >
        <div className="flex w-full min-w-0 flex-1 flex-col items-start gap-[46px]">
          <div className="flex w-full flex-col items-start gap-8">
            <div className="flex w-full flex-col items-start gap-3.5 text-black">
              <p className="m-0 font-sans text-sm font-medium uppercase leading-normal tracking-[1.12px] lg:text-sm">
                {PILOT_HERO.eyebrow}
              </p>
              <h1
                className={cn(
                  "m-0 w-full max-w-[608px] text-black",
                  marketingHeroHeading,
                  "font-semibold lg:text-[54px] lg:leading-[60px]",
                )}
              >
                {PILOT_HERO.title}
              </h1>
            </div>
            <p
              className={cn(
                "m-0 w-full max-w-[620px]",
                PILOT_SECTION_BODY,
              )}
            >
              {PILOT_HERO.body}
            </p>
          </div>

          <HeroSignupForm
            showTrustBadge={false}
            align="start"
            emailPlaceholder={PILOT_HERO.emailPlaceholder}
            primaryLabel={PILOT_HERO.primaryLabel}
            planIntent={PILOT_HERO.planIntent}
          />

          <div className="flex flex-wrap items-center gap-[18px]">
            {PILOT_HERO.trustItems.map((item) => (
              <p
                key={item}
                className="m-0 flex items-center gap-2 text-xs font-medium leading-normal text-[#727272]"
              >
                <CircleCheckBig
                  aria-hidden
                  className="size-4 shrink-0 text-[#727272]"
                  strokeWidth={2}
                />
                {item}
              </p>
            ))}
          </div>
        </div>

        <div className="aspect-[739/631] w-full shrink-0 overflow-hidden rounded-[12px] lg:h-[631px] lg:w-[min(100%,739px)] lg:max-w-[739px] lg:aspect-auto">
          <img
            src={heroImage.src}
            srcSet={heroImage.srcSet}
            sizes="(min-width: 1024px) 739px, 100vw"
            alt={PILOT_HERO.imageAlt}
            width={heroImage.width}
            height={heroImage.height}
            className="block size-full object-cover object-center"
            decoding="async"
            fetchPriority="high"
          />
        </div>
      </div>
    </section>
  )
}

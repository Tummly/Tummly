import { HeroSignupForm } from "@/components/home/HeroSignupForm"
import {
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
  PILOT_SECTION_INSET,
  PILOT_SIGN_UP_CTA,
} from "@/content/marketing/pilotPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

const frameImage = remoteSimpleImg(PILOT_SIGN_UP_CTA.mediaId)

/**
 * Figma Pilot bottom split CTA `6464:35738`.
 * White surface, left copy + signup, right lifestyle frame (smoke strip baked in).
 * Outer `pt-5` matches MarketingSignUpCta / Footer chrome gap above the card.
 */
export function PilotSignUpCta() {
  return (
    <section className="w-full pt-5">
      <div className="w-full overflow-hidden rounded-[12px] bg-white">
        <div
          className={cn(
            "flex w-full flex-col items-start gap-10 lg:flex-row lg:items-center lg:justify-between lg:gap-[70px]",
            PILOT_SECTION_INSET,
            "py-12.5 lg:py-[70px]",
          )}
        >
          <div className="flex w-full min-w-0 flex-col items-start justify-between gap-10 lg:h-[560px] lg:max-w-[640px] lg:flex-1">
            <div className="flex w-full flex-col items-start gap-[18px]">
              <h2
                className={cn(
                  "m-0 w-full max-w-[545px] text-black",
                  PILOT_SECTION_HEADING,
                  "lg:leading-none",
                )}
              >
                {PILOT_SIGN_UP_CTA.title}
              </h2>
              <p
                className={cn(
                  "m-0 w-full max-w-[587px]",
                  PILOT_SECTION_BODY,
                )}
              >
                {PILOT_SIGN_UP_CTA.body}
              </p>
            </div>

            <HeroSignupForm
              showTrustBadge={false}
              align="start"
              emailPlaceholder={PILOT_SIGN_UP_CTA.emailPlaceholder}
              primaryLabel={PILOT_SIGN_UP_CTA.primaryLabel}
              planIntent={PILOT_SIGN_UP_CTA.planIntent}
            />
          </div>

          <div className="relative aspect-[720/650] w-full shrink-0 overflow-hidden rounded-[12px] lg:h-[650px] lg:w-[min(100%,720px)] lg:max-w-[720px] lg:aspect-auto">
            <img
              src={frameImage.src}
              srcSet={frameImage.srcSet}
              sizes="(min-width: 1024px) 720px, 100vw"
              alt={PILOT_SIGN_UP_CTA.imageAlt}
              width={720}
              height={650}
              className="absolute inset-0 size-full object-cover object-center"
              decoding="async"
            />
          </div>
        </div>
      </div>
    </section>
  )
}

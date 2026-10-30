import heroStillImage from "@/assets/images/marketing/home/hero-still.webp"
import { HeroSignupForm } from "@/components/home/HeroSignupForm"
import { MarketingLoopVideo } from "@/components/marketing/MarketingLoopVideo"
import { HOME_HERO } from "@/content/marketing/homePage"
import { homeHeroVideoUrl } from "@/lib/marketingMedia"
import { cn } from "@/lib/utils"

/**
 * Home hero — desktop Figma `5433:12691`, mobile `6241:17596`.
 * Centered copy + Pilot signup; campaign visual in frame `5985:32077`.
 */
function Hero() {
  return (
    <section className="-mt-5 w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-[#fafafa]">
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-center gap-[50px] pt-[50px] pb-[50px] lg:pt-[90px]",
          // Figma mobile `6241:17596`: 24px. Desktop keep 60px chrome inset.
          "px-6 lg:px-[60px]",
        )}
      >
        <div className="flex w-full max-w-[1428px] flex-col items-center gap-[26px]">
          <div className="flex w-full flex-col items-center gap-[26px] text-center">
            <div className="flex w-full flex-col items-center gap-[14px]">
              <p className="m-0 font-sans text-sm leading-4 font-semibold tracking-[1.12px] text-[#4a4a4a] uppercase">
                {HOME_HERO.eyebrow}
              </p>
              <h1
                className={cn(
                  "m-0 w-full max-w-[760px] font-jakarta font-semibold text-[#141414]",
                  "text-[28px] leading-[34px] lg:text-[54px] lg:leading-[60px]",
                )}
              >
                {HOME_HERO.title}
              </h1>
            </div>
            <p
              className={cn(
                "m-0 w-full max-w-[592px] font-sans font-normal text-[#141414]",
                "text-sm leading-5 lg:text-lg lg:leading-6",
              )}
            >
              {HOME_HERO.body}
            </p>
          </div>

          <HeroSignupForm legalNote="read" />
        </div>

        {/*
          Figma `5985:32077` (1548×796, radius 12): campaign results loop. Its
          first frame is the pizza table, so that still is the poster and the
          reduced-motion image.
        */}
        <MarketingLoopVideo
          src={homeHeroVideoUrl()}
          poster={heroStillImage}
          preload="auto"
          className="aspect-[1548/796] w-full rounded-[12px] bg-[#ebebeb]"
          videoClassName="object-center"
        />
      </div>
    </section>
  )
}

export default Hero

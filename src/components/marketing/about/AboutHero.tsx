import heroEditorialImage from "@/assets/images/marketing/about/hero-editorial.png"
import {
  ABOUT_HERO,
  ABOUT_HERO_BODY,
  ABOUT_HERO_EYEBROW,
  ABOUT_HERO_HEADING,
  ABOUT_HERO_Y,
  ABOUT_SECTION_INSET,
} from "@/content/marketing/aboutPage"
import { cn } from "@/lib/utils"

/**
 * Figma About Tummly hero `5673:24144`.
 * Centre copy (eyebrow → title → body) + full-width editorial image.
 */
export function AboutHero() {
  return (
    <section className="w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-white">
      <div
        className={cn(
          "flex w-full flex-col items-center gap-10 lg:gap-[70px]",
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
            <p className={cn("m-0 w-full max-w-[642px]", ABOUT_HERO_BODY)}>
              {ABOUT_HERO.body}
            </p>
          </div>
        </div>

        <div className="aspect-[1548/712] w-full overflow-hidden rounded-[12px]">
          <img
            src={heroEditorialImage}
            alt={ABOUT_HERO.imageAlt}
            width={3096}
            height={1424}
            className="block size-full object-cover"
            decoding="async"
            fetchPriority="high"
          />
        </div>
      </div>
    </section>
  )
}

import industryCafesImage from "@/assets/images/marketing/about/industry-cafes.jpg"
import industryCasualImage from "@/assets/images/marketing/about/industry-casual.jpg"
import industryGroupsImage from "@/assets/images/marketing/about/industry-groups.jpg"
import industryTakeawayImage from "@/assets/images/marketing/about/industry-takeaway.jpg"
import {
  ABOUT_HERO_EYEBROW,
  ABOUT_INDUSTRIES,
  ABOUT_INDUSTRIES_Y,
  ABOUT_SECTION_BODY,
  ABOUT_SECTION_HEADING,
  ABOUT_SECTION_INSET,
  type AboutIndustry,
} from "@/content/marketing/aboutPage"
import { cn } from "@/lib/utils"

const INDUSTRY_IMAGES = {
  takeaway: industryTakeawayImage,
  cafes: industryCafesImage,
  casual: industryCasualImage,
  groups: industryGroupsImage,
} as const satisfies Record<AboutIndustry["id"], string>

function IndustryCard({ industry }: { industry: AboutIndustry }) {
  return (
    <article
      className={cn(
        "relative flex h-[360px] w-[min(100%,320px)] shrink-0 flex-col items-start justify-end overflow-hidden rounded-[12px]",
        "px-[30px] pb-10 pt-[30px] sm:w-auto sm:min-w-[280px] sm:flex-1 lg:h-[426px] lg:min-w-0",
      )}
    >
      <img
        src={INDUSTRY_IMAGES[industry.id]}
        alt={industry.imageAlt}
        width={748}
        height={852}
        className="pointer-events-none absolute inset-0 size-full object-cover"
        decoding="async"
      />
      <div
        className="pointer-events-none absolute inset-0 rounded-[12px] bg-gradient-to-b from-[rgba(20,20,20,0)] to-[#141414]"
        aria-hidden
      />
      <div className="relative flex w-full flex-col items-start justify-center gap-[18px] text-white">
        <h3 className="m-0 font-jakarta text-2xl font-semibold leading-normal">
          {industry.title}
        </h3>
        <p className="m-0 font-sans text-sm font-normal leading-[18px]">
          {industry.body}
        </p>
      </div>
    </article>
  )
}

/**
 * Figma About Industries `5685:35427`.
 * Intro copy + four industry photo cards + bottom divider.
 */
export function AboutIndustries() {
  return (
    <section className="w-full bg-white">
      <div
        className={cn(
          "flex w-full flex-col items-center gap-10 lg:gap-[70px]",
          ABOUT_SECTION_INSET,
          ABOUT_INDUSTRIES_Y,
        )}
      >
        <div className="flex w-full flex-col gap-3.5">
          <p className={cn("m-0", ABOUT_HERO_EYEBROW)}>
            {ABOUT_INDUSTRIES.eyebrow}
          </p>
          <div className="flex w-full flex-col items-start gap-6 lg:flex-row lg:justify-between lg:gap-8">
            <h2
              className={cn(
                "m-0 w-full max-w-[702px] shrink-0 lg:flex-1",
                ABOUT_SECTION_HEADING,
              )}
            >
              {ABOUT_INDUSTRIES.title}
            </h2>
            <div
              className={cn(
                "flex w-full max-w-[596px] flex-col gap-[18px] lg:shrink-0",
                ABOUT_SECTION_BODY,
              )}
            >
              {ABOUT_INDUSTRIES.paragraphs.map((paragraph) => (
                <p key={paragraph} className="m-0">
                  {paragraph}
                </p>
              ))}
            </div>
          </div>
        </div>

        <div className="-mx-5 w-[calc(100%+2.5rem)] overflow-x-auto px-5 lg:mx-0 lg:w-full lg:overflow-visible lg:px-0">
          <div className="flex w-max min-w-full gap-[18px] lg:w-full">
            {ABOUT_INDUSTRIES.items.map((industry) => (
              <IndustryCard key={industry.id} industry={industry} />
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}

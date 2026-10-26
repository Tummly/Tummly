import {
  ABOUT_HERO_EYEBROW,
  ABOUT_INDUSTRIES,
  ABOUT_INDUSTRIES_Y,
  ABOUT_SECTION_BODY,
  ABOUT_SECTION_HEADING,
  ABOUT_SECTION_INSET,
  type AboutIndustry,
} from "@/content/marketing/aboutPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

const INDUSTRY_IMAGES = {
  cafes: remoteSimpleImg("about/industry-cafes"),
  takeaway: remoteSimpleImg("about/industry-takeaway"),
  casual: remoteSimpleImg("about/industry-casual"),
  groups: remoteSimpleImg("about/industry-groups"),
} as const satisfies Record<
  AboutIndustry["id"],
  ReturnType<typeof remoteSimpleImg>
>

/**
 * Figma absolute frames (`6367:30260`). Height + vertical offset from Figma.
 * Horizontal centre via `left-1/2 -translate-x-1/2` (Figma left% was only the
 * centre of a fixed width% box). `aspect-ratio` + `w-auto` blocks stretch.
 */
const INDUSTRY_IMAGE_FRAME = {
  // `6447:17210` — h 77.23%, top 0
  cafes: "absolute top-0 left-1/2 h-[77.23%] w-auto max-w-none -translate-x-1/2",
  // `6367:30244` — h full, top -35.16%
  takeaway:
    "absolute top-[-35.16%] left-1/2 h-full w-auto max-w-none -translate-x-1/2",
  // `6447:17254` — h 70.19%, top 0
  casual: "absolute top-0 left-1/2 h-[70.19%] w-auto max-w-none -translate-x-1/2",
  // `6447:17258` — h full, top -16.12%
  groups:
    "absolute top-[-16.12%] left-1/2 h-full w-auto max-w-none -translate-x-1/2",
} as const satisfies Record<AboutIndustry["id"], string>

function IndustryCard({ industry }: { industry: AboutIndustry }) {
  const image = INDUSTRY_IMAGES[industry.id]

  return (
    <article
      className={cn(
        // Figma card `373.5 × 426` — fixed width so layout matches design.
        "relative flex shrink-0 flex-col items-start justify-end overflow-hidden rounded-[12px]",
        "h-[360px] w-[min(100%,320px)]",
        "px-[30px] pb-10 pt-[30px]",
        "lg:h-[426px] lg:w-[373.5px]",
      )}
    >
      <div className="pointer-events-none absolute inset-0 overflow-hidden rounded-[12px]">
        <img
          src={image.src}
          srcSet={image.srcSet}
          // Photo frame is ~1.5–1.7× card width (~585–640 CSS px), not 374.
          sizes="(min-width: 1024px) 640px, 90vw"
          alt={industry.imageAlt}
          width={image.width}
          height={image.height}
          className={cn(
            "pointer-events-none",
            INDUSTRY_IMAGE_FRAME[industry.id],
          )}
          style={{ aspectRatio: `${image.width} / ${image.height}` }}
          decoding="async"
        />
      </div>
      <div
        className="pointer-events-none absolute inset-0 rounded-[12px] bg-gradient-to-b from-[rgba(20,20,20,0)] from-[1%] to-[#141414] to-[73%]"
        aria-hidden
      />
      <div className="relative flex w-full flex-col items-start justify-center gap-[18px] text-white">
        <h3 className="m-0 font-jakarta text-2xl font-semibold leading-[normal]">
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
 * Figma About Industries `5685:35427` / row `6367:30260`.
 * Intro copy + four fixed-width industry photo cards + bottom divider.
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

        <div className="-mx-5 w-[calc(100%+2.5rem)] overflow-x-auto px-5 lg:mx-0 lg:w-full lg:overflow-x-auto lg:px-0">
          <div className="mx-auto flex w-max gap-[18px]">
            {ABOUT_INDUSTRIES.items.map((industry) => (
              <IndustryCard key={industry.id} industry={industry} />
            ))}
          </div>
        </div>

        <div
          className="h-px w-full shrink-0 bg-[#e8e8e8]"
          role="presentation"
        />
      </div>
    </section>
  )
}

import {
  ABOUT_HERO_EYEBROW,
  ABOUT_INDUSTRIES,
  ABOUT_INDUSTRIES_Y,
  ABOUT_SECTION_HEADING,
  ABOUT_SECTION_INSET,
  type AboutIndustry,
} from "@/content/marketing/aboutPage";
import { remoteSimpleImg } from "@/lib/marketingMediaPictures";
import { cn } from "@/lib/utils";

/** v2 photos (taller crops from Figma) published to Blob. */
const INDUSTRY_IMAGES = {
  cafes: remoteSimpleImg("about/industry-cafes-v2"),
  takeaway: remoteSimpleImg("about/industry-takeaway-v2"),
  casual: remoteSimpleImg("about/industry-casual-v2"),
  groups: remoteSimpleImg("about/industry-groups-v2"),
} as const satisfies Record<
  AboutIndustry["id"],
  ReturnType<typeof remoteSimpleImg>
>;

/**
 * Figma photo frames (`7183:10664` … `7183:10680`). Height + vertical offset are
 * % of the 476px card; width follows the photo aspect and is centred
 * (`aspect-ratio` + `w-auto` blocks stretch).
 */
const INDUSTRY_IMAGE_FRAME = {
  cafes: "absolute top-0 left-1/2 h-full w-auto max-w-none -translate-x-1/2",
  takeaway:
    "absolute top-[-35.16%] left-1/2 h-[135.08%] w-auto max-w-none -translate-x-1/2",
  casual: "absolute top-0 left-1/2 h-full w-auto max-w-none -translate-x-1/2",
  groups:
    "absolute top-[-16.12%] left-1/2 h-[116.04%] w-auto max-w-none -translate-x-1/2",
} as const satisfies Record<AboutIndustry["id"], string>;

function IndustryCard({ industry }: { industry: AboutIndustry }) {
  const image = INDUSTRY_IMAGES[industry.id];

  return (
    <article
      className={cn(
        // Figma card `374 × 476` — fixed height; width fills the grid or scroll track.
        "relative flex h-[476px] w-[320px] shrink-0 flex-col items-start justify-end overflow-hidden rounded-[12px] sm:w-auto",
        "px-[30px] pb-10 pt-[30px]",
      )}
    >
      <div className="pointer-events-none absolute inset-0 overflow-hidden rounded-[12px]">
        <img
          src={image.src}
          srcSet={image.srcSet}
          // Photo frame is ~1.5–1.9× card width (~585–640 CSS px), not 374.
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
        className="pointer-events-none absolute inset-0 rounded-[12px] bg-[linear-gradient(179.45deg,rgba(20,20,20,0)_0.37%,#141414_99.64%)]"
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
  );
}

/**
 * Figma About Industries `5685:35427` / row `7183:10663`.
 * Centre intro + four industry photo cards + bottom divider.
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
        <div className="flex w-full flex-col items-center gap-3.5 text-center">
          <p className={cn("m-0 w-full", ABOUT_HERO_EYEBROW)}>
            {ABOUT_INDUSTRIES.eyebrow}
          </p>
          <h2 className={cn("m-0 w-full max-w-[544px]", ABOUT_SECTION_HEADING)}>
            {ABOUT_INDUSTRIES.title}
          </h2>
        </div>

        <div className="-mx-5 w-[calc(100%+2.5rem)] overflow-x-auto px-5 sm:mx-0 sm:w-full sm:overflow-visible sm:px-0">
          <div className="flex w-max gap-[18px] sm:grid sm:w-full sm:grid-cols-2 xl:grid-cols-4">
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
  );
}

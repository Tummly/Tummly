import {
  ABOUT_HERO_EYEBROW,
  ABOUT_PRINCIPLES_HEADING,
  ABOUT_PRINCIPLES_INTRO,
  ABOUT_REALITY,
  ABOUT_REALITY_Y,
  ABOUT_SECTION_INSET,
  type AboutRealityItem,
} from "@/content/marketing/aboutPage";
import { remoteSimpleImg } from "@/lib/marketingMediaPictures";
import { cn } from "@/lib/utils";

/**
 * v2 illustrations (Figma `7183:10733` / `10823` / `10884` / `10918`, exported
 * @4×) published to Blob. Display size is the @4× size / 4.
 */
const REALITY_IMAGES = {
  visit: remoteSimpleImg("about/reality-v2-visit"),
  attention: remoteSimpleImg("about/reality-v2-attention"),
  invitation: remoteSimpleImg("about/reality-v2-invitation"),
  review: remoteSimpleImg("about/reality-v2-review"),
} as const satisfies Record<
  AboutRealityItem["id"],
  ReturnType<typeof remoteSimpleImg>
>;

function RealityCard({ item }: { item: AboutRealityItem }) {
  const image = REALITY_IMAGES[item.id];

  return (
    <article
      className={cn(
        // Figma card `5673:24199` — 493px tall, title / illustration / body spread.
        "flex flex-col items-center justify-between gap-6 overflow-hidden rounded-[12px] border border-solid border-[#e5e5e5] bg-white",
        "px-[26px] py-9 xl:h-[493px]",
      )}
    >
      <h3 className="m-0 w-full max-w-[300px] text-center font-jakarta text-2xl font-semibold leading-[normal] text-[#0e0e0e]">
        {item.title}
      </h3>
      <img
        src={image.src}
        srcSet={image.srcSet}
        sizes={`${image.width / 4}px`}
        alt={item.imageAlt}
        width={image.width / 4}
        height={image.height / 4}
        className="h-auto max-w-full shrink-0"
        decoding="async"
        loading="lazy"
      />
      <p className="m-0 w-full max-w-[328px] text-center font-sans text-base font-normal leading-[22px] text-[#141414]">
        {item.body}
      </p>
    </article>
  );
}

/**
 * Figma Restaurant reality as a design philosophy `5673:24194`.
 * Beige panel with centre intro + four bordered illustration cards.
 */
export function AboutReality() {
  return (
    <section
      className={cn("w-full bg-white", ABOUT_SECTION_INSET, ABOUT_REALITY_Y)}
    >
      <div className="flex w-full flex-col items-center gap-10 rounded-[12px] bg-[#eceae6] px-5 py-12.5 sm:px-10 lg:gap-[70px] lg:py-20">
        <div className="flex w-full max-w-[622px] flex-col items-center gap-[22px] text-center">
          <div className="flex w-full flex-col items-center gap-3.5">
            <p className={cn("m-0 w-full", ABOUT_HERO_EYEBROW)}>
              {ABOUT_REALITY.eyebrow}
            </p>
            <h2 className={cn("m-0 w-full", ABOUT_PRINCIPLES_HEADING)}>
              {ABOUT_REALITY.title}
            </h2>
          </div>
          <p className={cn("m-0 w-full max-w-[576px]", ABOUT_PRINCIPLES_INTRO)}>
            {ABOUT_REALITY.intro}
          </p>
        </div>

        <div className="grid w-full grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4 xl:gap-px">
          {ABOUT_REALITY.items.map((item) => (
            <RealityCard key={item.id} item={item} />
          ))}
        </div>
      </div>
    </section>
  );
}

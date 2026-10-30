import {
  ABOUT_HERO_EYEBROW,
  ABOUT_PRINCIPLES,
  ABOUT_PRINCIPLES_HEADING,
  ABOUT_PRINCIPLES_INTRO,
  ABOUT_PRINCIPLES_Y,
  ABOUT_SECTION_INSET,
  type AboutPrinciple,
} from "@/content/marketing/aboutPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

const PRINCIPLE_IMAGES = {
  heard: remoteSimpleImg("about/principle-heard"),
  listening: remoteSimpleImg("about/principle-listening"),
  trust: remoteSimpleImg("about/principle-trust"),
  return: remoteSimpleImg("about/principle-return"),
  location: remoteSimpleImg("about/principle-location"),
} as const satisfies Record<
  AboutPrinciple["id"],
  ReturnType<typeof remoteSimpleImg>
>

function PrincipleCard({ item }: { item: AboutPrinciple }) {
  const image = PRINCIPLE_IMAGES[item.id]

  return (
    <article
      className={cn(
        // Figma card `5673:24216` — 340px tall (grows with copy), icon on top, text pinned to the bottom.
        // Mobile: fixed-width scroll track. sm: 2 cols, lg: 3 cols (last row centred), xl: five in a row.
        "flex min-h-[340px] w-[260px] shrink-0 flex-col items-center justify-between gap-9 self-stretch overflow-hidden rounded-[8px] bg-[#eceae6]",
        "px-[26px] pt-[46px] pb-[26px]",
        "sm:w-[calc((100%-1.25rem)/2)] lg:w-[calc((100%-2.5rem)/3)] xl:w-auto xl:min-w-0 xl:flex-1 xl:basis-0",
      )}
    >
      <img
        src={image.src}
        srcSet={image.srcSet}
        sizes="98px"
        alt={item.imageAlt}
        width={image.width}
        height={image.height}
        className="h-[98px] w-auto max-w-[115px] shrink-0 object-contain"
        decoding="async"
      />
      <div className="flex w-full flex-col items-center gap-2.5 text-center">
        <h3 className="m-0 w-full font-jakarta text-xl font-bold leading-6 text-[#0e0e0e]">
          {item.title}
        </h3>
        <p className="m-0 w-full font-sans text-sm font-medium leading-5 text-[#141414]">
          {item.body}
        </p>
      </div>
    </article>
  )
}

/**
 * Figma The principles behind Tummly `5673:24211`.
 * Centre intro + five beige principle cards with illustration icons.
 */
export function AboutPrinciples() {
  return (
    <section className="w-full bg-white">
      <div
        className={cn(
          "flex w-full flex-col items-center gap-10 lg:gap-14",
          ABOUT_SECTION_INSET,
          ABOUT_PRINCIPLES_Y,
        )}
      >
        <div className="flex w-full max-w-[760px] flex-col items-center gap-[22px] text-center">
          <div className="flex w-full flex-col items-center gap-3.5">
            <p className={cn("m-0 w-full", ABOUT_HERO_EYEBROW)}>
              {ABOUT_PRINCIPLES.eyebrow}
            </p>
            <h2 className={cn("m-0 w-full", ABOUT_PRINCIPLES_HEADING)}>
              {ABOUT_PRINCIPLES.title}
            </h2>
          </div>
          <p
            className={cn(
              "m-0 w-full max-w-[576px]",
              ABOUT_PRINCIPLES_INTRO,
            )}
          >
            {ABOUT_PRINCIPLES.intro}
          </p>
        </div>

        <div className="-mx-5 w-[calc(100%+2.5rem)] overflow-x-auto px-5 sm:mx-0 sm:w-full sm:overflow-visible sm:px-0">
          <div className="flex w-max items-stretch gap-5 sm:w-full sm:flex-wrap sm:justify-center xl:flex-nowrap">
            {ABOUT_PRINCIPLES.items.map((item) => (
              <PrincipleCard key={item.id} item={item} />
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}

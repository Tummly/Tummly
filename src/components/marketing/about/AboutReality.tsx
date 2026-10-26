import { AiAssistantMutedVideo } from "@/components/marketing/ai-assistant/AiAssistantMutedVideo"
import {
  ABOUT_HERO_EYEBROW,
  ABOUT_PRINCIPLES_HEADING,
  ABOUT_PRINCIPLES_INTRO,
  ABOUT_REALITY,
  ABOUT_REALITY_Y,
  ABOUT_SECTION_INSET,
  type AboutRealityItem,
} from "@/content/marketing/aboutPage"
import {
  aboutRealityVideoUrl,
  type AboutRealityVideoKey,
} from "@/lib/marketingMedia"
import { cn } from "@/lib/utils"

const REALITY_VIDEOS = {
  return: aboutRealityVideoUrl("return"),
  relationships: aboutRealityVideoUrl("relationships"),
  together: aboutRealityVideoUrl("together"),
  visibility: aboutRealityVideoUrl("visibility"),
} as const satisfies Record<AboutRealityVideoKey, string>

function RealityCard({ item }: { item: AboutRealityItem }) {
  const videoSrc = REALITY_VIDEOS[item.id]

  return (
    <article
      className={cn(
        "flex min-w-[260px] flex-1 flex-col items-center justify-between gap-3.5 self-stretch overflow-hidden rounded-[12px] border border-solid border-[#e5e5e5] bg-white",
        "px-[26px] py-9",
      )}
    >
      <h3 className="m-0 w-full max-w-[300px] text-center font-jakarta text-2xl font-semibold leading-[normal] text-[#0e0e0e]">
        {item.title}
      </h3>
      <AiAssistantMutedVideo
        src={videoSrc}
        aria-label={item.imageAlt}
        className="relative aspect-square h-auto max-h-[227px] w-full max-w-[250px] shrink-0 bg-transparent"
        videoClassName="object-contain"
      />
      <p className="m-0 w-full max-w-[328px] text-center font-sans text-base font-normal leading-[22px] text-[#141414]">
        {item.body}
      </p>
    </article>
  )
}

/**
 * Figma Restaurant reality as a design philosophy `5673:24194`.
 * Beige panel with centre intro + four bordered motion cards.
 */
export function AboutReality() {
  return (
    <section
      className={cn("w-full bg-white", ABOUT_SECTION_INSET, ABOUT_REALITY_Y)}
    >
      <div className="flex w-full flex-col items-center gap-10 rounded-[12px] bg-[#eceae6] px-5 py-12.5 sm:px-10 lg:gap-[86px] lg:px-10 lg:py-20">
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

        <div className="-mx-2 w-[calc(100%+1rem)] overflow-x-auto px-2 sm:mx-0 sm:w-full sm:overflow-visible sm:px-0">
          <div className="flex w-max min-w-full items-stretch gap-0 lg:w-full">
            {ABOUT_REALITY.items.map((item) => (
              <RealityCard key={item.id} item={item} />
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}

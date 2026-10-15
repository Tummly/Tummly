import realityGrowthImage from "@/assets/images/marketing/about/reality-growth.png"
import realityPhysicalImage from "@/assets/images/marketing/about/reality-physical.png"
import realitySystemsImage from "@/assets/images/marketing/about/reality-systems.png"
import realityTimeImage from "@/assets/images/marketing/about/reality-time.png"
import {
  ABOUT_HERO_EYEBROW,
  ABOUT_REALITY,
  ABOUT_REALITY_Y,
  ABOUT_SECTION_INSET,
  type AboutRealityItem,
} from "@/content/marketing/aboutPage"
import { cn } from "@/lib/utils"

const REALITY_IMAGES = {
  time: realityTimeImage,
  physical: realityPhysicalImage,
  systems: realitySystemsImage,
  growth: realityGrowthImage,
} as const satisfies Record<AboutRealityItem["id"], string>

/** Section title inside beige panel — *headline* Jakarta 46 / 58 medium. */
const realityHeading =
  "font-jakarta text-[34px] font-medium leading-none tracking-normal text-[#0e0e0e] lg:text-[46px] lg:leading-[58px]"

function RealityCard({ item }: { item: AboutRealityItem }) {
  return (
    <article className="flex min-w-0 flex-1 flex-col items-center gap-3.5 self-stretch overflow-hidden rounded-[12px] border border-solid border-[#e5e5e5] bg-white px-[26px] py-9">
      <h3 className="m-0 w-full text-center font-jakarta text-2xl font-semibold leading-normal text-[#0e0e0e]">
        {item.title}
      </h3>
      <img
        src={REALITY_IMAGES[item.id]}
        alt={item.imageAlt}
        width={227}
        height={227}
        className="size-[180px] shrink-0 object-contain lg:size-[227px]"
        decoding="async"
      />
      <p className="m-0 w-full text-center font-sans text-base font-normal leading-[22px] text-[#4a4a4a]">
        {item.body}
      </p>
    </article>
  )
}

/**
 * Figma Restaurant reality as a design philosophy `5673:24194`.
 * Beige panel with centre intro + four bordered illustration cards.
 */
export function AboutReality() {
  return (
    <section className={cn("w-full bg-white", ABOUT_SECTION_INSET, ABOUT_REALITY_Y)}>
      <div className="flex w-full flex-col items-center gap-10 rounded-[12px] bg-[#eceae6] px-5 py-12.5 sm:px-10 lg:gap-[86px] lg:px-10 lg:py-20">
        <div className="flex w-full max-w-[622px] flex-col items-center gap-3.5 text-center">
          <p className={cn("m-0 w-full", ABOUT_HERO_EYEBROW)}>
            {ABOUT_REALITY.eyebrow}
          </p>
          <h2 className={cn("m-0 w-full", realityHeading)}>
            {ABOUT_REALITY.title}
          </h2>
        </div>

        <div className="grid w-full grid-cols-1 gap-4 sm:grid-cols-2 lg:flex lg:items-stretch lg:gap-0">
          {ABOUT_REALITY.items.map((item) => (
            <RealityCard key={item.id} item={item} />
          ))}
        </div>
      </div>
    </section>
  )
}

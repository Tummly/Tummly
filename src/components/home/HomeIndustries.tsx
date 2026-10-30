import industryCafesImage from "@/assets/images/marketing/home/industry-cafes.jpg"
import industryCasualImage from "@/assets/images/marketing/home/industry-casual.jpg"
import industryGroupsImage from "@/assets/images/marketing/home/industry-groups.jpg"
import industryTakeawayImage from "@/assets/images/marketing/home/industry-takeaway.jpg"
import {
  HOME_INDUSTRIES,
  type HomeIndustry,
} from "@/content/marketing/homePage"

/** Pre-cropped 746×952 (2×) so `object-cover` matches the Figma card crops. */
const INDUSTRY_IMAGES = {
  takeaway: industryTakeawayImage,
  cafes: industryCafesImage,
  casual: industryCasualImage,
  groups: industryGroupsImage,
} as const satisfies Record<HomeIndustry["id"], string>

function IndustryCard({ industry }: { industry: HomeIndustry }) {
  return (
    <li className="m-0 flex min-w-0 p-0">
      <article className="relative flex min-h-[420px] w-full flex-col justify-end overflow-hidden rounded-[12px] bg-[#141414] px-[30px] pt-[30px] pb-10 text-white sm:min-h-[476px] xl:h-[476px] xl:min-h-0">
        <img
          src={INDUSTRY_IMAGES[industry.id]}
          alt={industry.imageAlt}
          width={746}
          height={952}
          loading="lazy"
          decoding="async"
          draggable={false}
          className="absolute inset-0 size-full object-cover"
        />
        <div
          aria-hidden
          className="absolute inset-0 bg-[linear-gradient(179.45deg,rgba(20,20,20,0)_0.37%,rgb(20,20,20)_99.64%)]"
        />
        <div className="relative flex flex-col gap-[18px]">
          <h3 className="m-0 font-jakarta text-2xl font-semibold leading-[normal]">
            {industry.title}
          </h3>
          <p className="m-0 font-sans text-sm font-normal leading-[18px]">
            {industry.body}
          </p>
        </div>
      </article>
    </li>
  )
}

/**
 * Home Built for restaurants — Figma `6792:13613`.
 * Centered heading block over four photo cards. Static: no tabs or selection.
 */
export function HomeIndustries() {
  return (
    <section
      className="w-full bg-[#f0f0f0]"
      aria-labelledby="home-industries-heading"
    >
      <div className="mx-auto flex w-full flex-col gap-10 px-6 py-[60px] lg:gap-[70px] lg:px-[60px]">
        <header className="mx-auto flex w-full max-w-[615px] flex-col items-center gap-[22px] text-center text-[#141414]">
          <div className="flex w-full flex-col items-center gap-3.5">
            <p className="m-0 font-sans text-sm font-semibold leading-4 tracking-[1.12px] text-[#4a4a4a]">
              {HOME_INDUSTRIES.eyebrow}
            </p>
            <h2
              id="home-industries-heading"
              className="m-0 max-w-[472px] font-jakarta text-[28px] font-medium leading-[38px] lg:text-[46px] lg:leading-[52px]"
            >
              {HOME_INDUSTRIES.title}
            </h2>
          </div>
          <p className="m-0 max-w-[536px] font-sans text-sm font-normal leading-[22px] lg:text-[18px] lg:leading-6">
            {HOME_INDUSTRIES.body}
          </p>
        </header>

        <ul className="m-0 grid w-full list-none grid-cols-1 gap-[18px] p-0 sm:grid-cols-2 xl:grid-cols-4">
          {HOME_INDUSTRIES.items.map((industry) => (
            <IndustryCard key={industry.id} industry={industry} />
          ))}
        </ul>
      </div>
    </section>
  )
}

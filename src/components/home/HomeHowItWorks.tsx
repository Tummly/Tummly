import { HOME_HOW_IT_WORKS } from "@/content/marketing/homePage"
import { marketingChromeContentInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/**
 * Home How it works intro — Figma `4902:19644`.
 * Eyebrow + title + body (centered).
 */
export function HomeHowItWorks() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="home-how-it-works-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-center pt-[70px] pb-10",
          marketingChromeContentInset,
        )}
      >
        <div className="flex w-full max-w-[626px] flex-col items-center gap-[18px] text-center text-[#141414]">
          <div className="flex w-full flex-col items-center gap-3.5">
            <p className="m-0 font-jakarta text-sm font-medium leading-4 tracking-normal">
              {HOME_HOW_IT_WORKS.eyebrow}
            </p>
            <h2
              id="home-how-it-works-heading"
              className="m-0 w-full font-jakarta text-[34px] font-medium leading-none lg:text-[46px] lg:leading-[58px]"
            >
              {HOME_HOW_IT_WORKS.title}
            </h2>
          </div>
          <p className="m-0 w-full max-w-[542px] font-sans text-base font-normal leading-[22px] lg:text-[18px] lg:leading-6">
            {HOME_HOW_IT_WORKS.body}
          </p>
        </div>
      </div>
    </section>
  )
}

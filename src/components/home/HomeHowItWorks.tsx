import { HOME_HOW_IT_WORKS } from "@/content/marketing/homePage"
import { marketingChromeContentInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/**
 * Home How it works intro — Figma `5088:12372` (text frame `4902:19644`).
 * Eyebrow + title (centered).
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
        <div className="flex w-full max-w-[580px] flex-col items-center gap-3.5 text-center text-[#141414]">
          <p className="m-0 font-sans text-sm font-semibold leading-4 tracking-[1.12px] text-[#4a4a4a]">
            {HOME_HOW_IT_WORKS.eyebrow}
          </p>
          <h2
            id="home-how-it-works-heading"
            className="m-0 w-full font-jakarta text-[34px] font-medium leading-10 text-balance lg:text-[46px] lg:leading-[52px]"
          >
            {HOME_HOW_IT_WORKS.title}
          </h2>
        </div>
      </div>
    </section>
  )
}

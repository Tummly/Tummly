import { SearchIcon } from "lucide-react"

import { FAQS_PAGE_HERO } from "@/content/marketing/faqsPage"
import { Input } from "@/components/ui/input"
import { marketingChromeContentInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

type FaqsPageHeroProps = {
  searchQuery: string
  onSearchQueryChange: (value: string) => void
}

/** Figma FAQ title: Season Mix 66 / 74 on desktop. */
const faqHeroHeading =
  "text-[36px] font-medium leading-10 tracking-normal lg:text-[66px] lg:leading-[74px]"

/** Figma FAQ body: Helvetica Neue 18 / 24. */
const faqHeroBody =
  "text-base font-normal leading-[22px] tracking-normal lg:text-[18px] lg:leading-6"

/**
 * Figma `Frame 269` (`4974:26244`): #fafafa, pad 70/60, title→body 18px,
 * copy→search 46px, search #eaeaea with 26px pad.
 * `-mt-5` cancels MainLayout header bottom chrome so nav and hero sit flush.
 */
export function FaqsPageHero({
  searchQuery,
  onSearchQueryChange,
}: FaqsPageHeroProps) {
  return (
    <section className="-mt-5 w-full bg-[#fafafa]">
      <div
        className={cn(
          "mx-auto flex w-full flex-col gap-11.5",
          marketingChromeContentInset,
          "py-17.5",
        )}
      >
        <div className="flex max-w-214.25 flex-col gap-4.5">
          <h1 className={cn("m-0 text-[#141414]", faqHeroHeading)}>
            {FAQS_PAGE_HERO.title}
          </h1>
          <p className={cn("m-0 max-w-168.5 text-[#141414]", faqHeroBody)}>
            {FAQS_PAGE_HERO.body}
          </p>
        </div>

        <label className="relative block w-full">
          <span className="sr-only">{FAQS_PAGE_HERO.searchPlaceholder}</span>
          <SearchIcon
            aria-hidden
            className="pointer-events-none absolute top-1/2 left-6.5 size-4.5 -translate-y-1/2 text-[#707070]"
          />
          <Input
            type="search"
            value={searchQuery}
            onChange={(event) => onSearchQueryChange(event.target.value)}
            placeholder={FAQS_PAGE_HERO.searchPlaceholder}
            className={cn(
              "h-auto min-h-0 rounded-none border-0 bg-[#eaeaea] py-6.5 pr-6.5 pl-17",
              "text-base font-normal text-[#141414] shadow-none",
              "placeholder:text-[#707070]",
              "focus-visible:border-0 focus-visible:ring-2 focus-visible:ring-[#141414]/20",
              "md:text-[18px] md:leading-normal",
              "[&::-webkit-search-cancel-button]:hidden [&::-webkit-search-decoration]:hidden",
            )}
          />
        </label>
      </div>
    </section>
  )
}

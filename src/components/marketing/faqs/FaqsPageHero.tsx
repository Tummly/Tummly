import { SearchIcon } from "lucide-react"

import { FAQS_PAGE_HERO } from "@/content/marketing/faqsPage"
import { Input } from "@/components/ui/input"
import {
  marketingHeroBody,
  marketingHeroHeading,
  marketingSectionInset,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

type FaqsPageHeroProps = {
  searchQuery: string
  onSearchQueryChange: (value: string) => void
}

/** Figma `Frame 269`: pad 70/60, #fafafa, search field #eaeaea. */
export function FaqsPageHero({
  searchQuery,
  onSearchQueryChange,
}: FaqsPageHeroProps) {
  return (
    <section className="w-full bg-[#fafafa]">
      <div
        className={cn(
          "mx-auto flex w-full max-w-[1568px] flex-col gap-8",
          marketingSectionInset,
          "py-[70px]",
        )}
      >
        <div className="flex max-w-3xl flex-col gap-3">
          <h1 className={cn("m-0 text-[#141414]", marketingHeroHeading)}>
            {FAQS_PAGE_HERO.title}
          </h1>
          <p className={cn("m-0 text-[#141414]", marketingHeroBody)}>
            {FAQS_PAGE_HERO.body}
          </p>
        </div>

        <label className="relative block w-full">
          <span className="sr-only">{FAQS_PAGE_HERO.searchPlaceholder}</span>
          <SearchIcon
            aria-hidden
            className="pointer-events-none absolute top-1/2 left-4 size-5 -translate-y-1/2 text-[#707070]"
          />
          <Input
            type="search"
            value={searchQuery}
            onChange={(event) => onSearchQueryChange(event.target.value)}
            placeholder={FAQS_PAGE_HERO.searchPlaceholder}
            className="h-14 rounded-[6px] border-0 bg-[#eaeaea] px-4 py-3 pl-12 text-base text-[#141414] shadow-none placeholder:text-[#707070] focus-visible:border-0 focus-visible:ring-2 focus-visible:ring-[#141414]/20 md:text-base"
          />
        </label>
      </div>
    </section>
  )
}

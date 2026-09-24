import { Link } from "react-router-dom"
import { ArrowRightIcon } from "lucide-react"

import { FaqsAccordionItem } from "@/components/marketing/faqs/FaqsAccordionItem"
import { Button } from "@/components/ui/button"
import { Accordion } from "@/components/ui/accordion"
import {
  PRICING_FAQS,
  PRICING_SECTION_HEADING,
  PRICING_SECTION_INSET,
} from "@/content/marketing/pricingPage"
import { cn } from "@/lib/utils"

const exploreButtonClass =
  "h-auto min-h-11 gap-1.5 rounded-[4px] border border-[#4e4e4e] bg-transparent px-[19px] py-[13px] text-sm font-medium text-[#141414] shadow-none hover:bg-[#141414]/5"

/**
 * Figma Pricing FAQs (`5437:14172` / `4974:29501`).
 * Column gap 60px; accordion item gap 30px; Q→A 16px (FaqsAccordionItem).
 */
export function PricingFaqs() {
  const defaultOpen = PRICING_FAQS.items[0]?.id

  return (
    <section className="w-full bg-white">
      <div
        className={cn(
          "flex w-full flex-col gap-8 sm:gap-10 lg:flex-row lg:items-start lg:gap-[60px]",
          PRICING_SECTION_INSET,
          "pt-12.5 pb-17.5",
        )}
      >
        <header className="flex w-full shrink-0 flex-col lg:sticky lg:top-44 lg:w-137 lg:max-w-137 lg:pt-5">
          <h2 className={cn("m-0", PRICING_SECTION_HEADING)}>
            {PRICING_FAQS.title}
          </h2>
        </header>

        <div className="flex min-w-0 flex-1 flex-col gap-7.5 lg:pt-5">
          <Accordion
            type="single"
            collapsible
            defaultValue={defaultOpen}
            className="gap-7.5"
          >
            {PRICING_FAQS.items.map((item) => (
              <FaqsAccordionItem
                key={item.id}
                value={item.id}
                question={item.question}
                answer={item.answer}
              />
            ))}
          </Accordion>

          <div className="flex flex-col gap-7 pb-5">
            <div className="flex flex-col gap-3">
              <h3 className="m-0 font-jakarta text-[28px] font-medium leading-normal text-black sm:text-[36px]">
                {PRICING_FAQS.moreTitle}
              </h3>
              <p className="m-0 text-base font-normal leading-[21px] text-[#141414]">
                {PRICING_FAQS.moreBody}
              </p>
            </div>
            <Button asChild className={cn(exploreButtonClass, "w-fit")}>
              <Link to={PRICING_FAQS.moreCtaTo}>
                {PRICING_FAQS.moreCtaLabel}
                <ArrowRightIcon className="size-3.5" aria-hidden />
              </Link>
            </Button>
          </div>
        </div>
      </div>
    </section>
  )
}

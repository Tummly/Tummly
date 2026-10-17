import { Link } from "react-router-dom"

import { FaqsAccordionItem } from "@/components/marketing/faqs/FaqsAccordionItem"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { Accordion } from "@/components/ui/accordion"
import {
  HOW_IT_WORKS_FAQS,
  HOW_IT_WORKS_SECTION_HEADING,
  HOW_IT_WORKS_SECTION_INSET,
} from "@/content/marketing/howItWorksPage"
import { marketingHeaderStickyTopClass } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/** Figma How it works FAQs `5661:19821` — same accordion pattern as Product. */
export function HowItWorksFaqs() {
  const defaultOpen = HOW_IT_WORKS_FAQS.items[0]?.id

  return (
    <section className="w-full rounded-bl-[12px] rounded-br-[12px] bg-white">
      <div
        className={cn(
          "flex w-full flex-col gap-8 sm:gap-10 lg:flex-row lg:items-start lg:justify-between lg:gap-[60px]",
          HOW_IT_WORKS_SECTION_INSET,
          "pt-12.5 pb-17.5",
        )}
      >
        <header
          className={cn(
            "flex w-full shrink-0 flex-col lg:sticky lg:w-137 lg:max-w-137",
            marketingHeaderStickyTopClass,
          )}
        >
          <h2 className={cn("m-0", HOW_IT_WORKS_SECTION_HEADING)}>
            {HOW_IT_WORKS_FAQS.title}
          </h2>
        </header>

        <div className="flex min-w-0 w-full flex-1 flex-col gap-7.5 lg:max-w-[746px]">
          <Accordion
            type="single"
            collapsible
            defaultValue={defaultOpen}
            className="gap-7.5"
          >
            {HOW_IT_WORKS_FAQS.items.map((item) => (
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
              <h3 className="m-0 font-jakarta text-[28px] font-medium leading-none text-black sm:text-[36px]">
                {HOW_IT_WORKS_FAQS.moreTitle}
              </h3>
              <p className="m-0 text-sm font-normal leading-[19px] text-[#141414]">
                {HOW_IT_WORKS_FAQS.moreBody}
              </p>
            </div>
            <MarketingButton asChild variant="secondary" className="w-fit">
              <Link to={HOW_IT_WORKS_FAQS.moreCtaTo}>
                {HOW_IT_WORKS_FAQS.moreCtaLabel}
              </Link>
            </MarketingButton>
          </div>
        </div>
      </div>
    </section>
  )
}

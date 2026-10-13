import { Link } from "react-router-dom"

import { FaqsAccordionItem } from "@/components/marketing/faqs/FaqsAccordionItem"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { Accordion } from "@/components/ui/accordion"
import {
  PRICING_FAQS,
  PRICING_SECTION_HEADING,
  PRICING_SECTION_INSET,
} from "@/content/marketing/pricingPage"
import { marketingHeaderStickyTopClass } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/**
 * Figma Pricing FAQs (`5437:14172` / `4974:29501`).
 * Column gap 60px; accordion item gap 30px; Q→A 16px (FaqsAccordionItem).
 */
export function PricingFaqs() {
  const defaultOpen = PRICING_FAQS.items[0]?.id

  return (
    <section className="w-full rounded-bl-[12px] rounded-br-[12px] bg-[#f0f0f0]">
      <div
        className={cn(
          "flex w-full flex-col gap-8 sm:gap-10 lg:flex-row lg:items-start lg:gap-[60px]",
          PRICING_SECTION_INSET,
          "pt-12.5 pb-17.5",
        )}
      >
        <header
          className={cn(
            "flex w-full shrink-0 flex-col lg:sticky lg:w-137 lg:max-w-137",
            marketingHeaderStickyTopClass,
          )}
        >
          <h2 className={cn("m-0", PRICING_SECTION_HEADING)}>
            {PRICING_FAQS.title}
          </h2>
        </header>

        <div className="flex min-w-0 flex-1 flex-col gap-7.5">
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
              <h3 className="m-0 font-jakarta text-[28px] font-medium leading-none text-black sm:text-[36px]">
                {PRICING_FAQS.moreTitle}
              </h3>
              <p className="m-0 text-base font-normal leading-[21px] text-[#141414]">
                {PRICING_FAQS.moreBody}
              </p>
            </div>
            <MarketingButton asChild variant="secondary" className="w-fit">
              <Link to={PRICING_FAQS.moreCtaTo}>
                {PRICING_FAQS.moreCtaLabel}
              </Link>
            </MarketingButton>
          </div>
        </div>
      </div>
    </section>
  )
}

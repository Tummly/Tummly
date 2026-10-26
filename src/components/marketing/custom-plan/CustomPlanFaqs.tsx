import { Link } from "react-router-dom"

import { FaqsAccordionItem } from "@/components/marketing/faqs/FaqsAccordionItem"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { Accordion } from "@/components/ui/accordion"
import {
  CUSTOM_PLAN_FAQS,
  CUSTOM_PLAN_SECTION_HEADING,
  CUSTOM_PLAN_SECTION_INSET,
} from "@/content/marketing/customPlanPage"
import { marketingHeaderStickyTopClass } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/** Figma Custom Plan FAQs `6661:39170` — after the hero / builder. */
export function CustomPlanFaqs() {
  const defaultOpen = CUSTOM_PLAN_FAQS.items[0]?.id

  return (
    <section className="w-full rounded-bl-[12px] rounded-br-[12px] bg-[#f2f0ec]">
      <div
        className={cn(
          "flex w-full flex-col gap-8 sm:gap-10 lg:flex-row lg:items-start lg:justify-between lg:gap-[60px]",
          CUSTOM_PLAN_SECTION_INSET,
          "pt-12.5 pb-17.5",
        )}
      >
        <header
          className={cn(
            // Figma py-20 on the title: a gap under the nav while it sticks.
            "flex w-full shrink-0 flex-col lg:sticky lg:w-137 lg:max-w-137 lg:py-5",
            marketingHeaderStickyTopClass,
          )}
        >
          <h2
            className={cn("m-0", CUSTOM_PLAN_SECTION_HEADING, "leading-[normal]")}
          >
            {CUSTOM_PLAN_FAQS.title}
          </h2>
        </header>

        <div className="flex min-w-0 w-full flex-1 flex-col gap-7.5 lg:pt-5 lg:max-w-[746px]">
          <Accordion
            type="single"
            collapsible
            defaultValue={defaultOpen}
            className="gap-7.5"
          >
            {CUSTOM_PLAN_FAQS.items.map((item) => (
              <FaqsAccordionItem
                key={item.id}
                value={item.id}
                question={item.question}
                answer={item.answer}
                variant="compact"
              />
            ))}
          </Accordion>

          <div className="flex flex-col gap-7 pb-5">
            <div className="flex flex-col gap-3">
              <h3 className="m-0 font-jakarta text-lg font-semibold leading-[normal] text-black">
                {CUSTOM_PLAN_FAQS.moreTitle}
              </h3>
              <p className="m-0 font-sans text-base font-normal leading-[21px] text-[#141414]">
                {CUSTOM_PLAN_FAQS.moreBody}
              </p>
            </div>
            <MarketingButton asChild variant="secondary" className="w-fit">
              <Link to={CUSTOM_PLAN_FAQS.moreCtaTo}>
                {CUSTOM_PLAN_FAQS.moreCtaLabel}
              </Link>
            </MarketingButton>
          </div>
        </div>
      </div>
    </section>
  )
}

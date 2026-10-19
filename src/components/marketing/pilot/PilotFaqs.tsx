import { Link } from "react-router-dom"

import { FaqsAccordionItem } from "@/components/marketing/faqs/FaqsAccordionItem"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { Accordion } from "@/components/ui/accordion"
import {
  PILOT_FAQS,
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
  PILOT_SECTION_INSET,
} from "@/content/marketing/pilotPage"
import { marketingHeaderStickyTopClass } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/**
 * Figma Pilot FAQs `5661:20468`.
 * Warm surface `#f2f0ec`. Left column flexes; FAQ list is fixed 783px.
 */
export function PilotFaqs() {
  const defaultOpen = PILOT_FAQS.items[0]?.id

  return (
    <section className="w-full overflow-clip rounded-bl-[12px] rounded-br-[12px] bg-[#f2f0ec]">
      <div
        className={cn(
          "flex w-full flex-col items-start gap-8 sm:gap-10",
          "lg:flex-row lg:gap-15",
          PILOT_SECTION_INSET,
          "pt-12.5 pb-17.5",
        )}
      >
        {/* Figma `5661:20469` — flex-1 so title stays on the left inset. */}
        <div className="flex min-w-0 w-full flex-1 flex-col items-start self-stretch">
          <header
            className={cn(
              "flex w-full max-w-[548px] flex-col items-start gap-[18px] py-5",
              "lg:sticky",
              marketingHeaderStickyTopClass,
            )}
          >
            <h2 className={cn("m-0 w-full text-left text-black", PILOT_SECTION_HEADING)}>
              {PILOT_FAQS.title}
            </h2>
            <p className={cn("m-0 w-full text-left", PILOT_SECTION_BODY)}>
              {PILOT_FAQS.body}
            </p>
          </header>
        </div>

        {/* Figma `5661:20473` — fixed 783px FAQ column. */}
        <div className="flex w-full shrink-0 flex-col gap-7.5 pt-5 lg:w-[783px]">
          <Accordion
            type="single"
            collapsible
            defaultValue={defaultOpen}
            className="gap-7.5"
          >
            {PILOT_FAQS.items.map((item) => (
              <FaqsAccordionItem
                key={item.id}
                value={item.id}
                question={item.question}
                answer={item.answer}
              />
            ))}
          </Accordion>

          <div className="flex flex-col gap-7 pb-5">
            <div className="flex flex-col gap-2">
              <h3 className="m-0 font-jakarta text-lg font-semibold leading-normal text-black">
                {PILOT_FAQS.moreTitle}
              </h3>
              <p className="m-0 font-sans text-sm font-normal leading-[19px] text-[#141414]">
                {PILOT_FAQS.moreBody}
              </p>
            </div>
            <MarketingButton asChild variant="secondary" className="w-fit">
              <Link to={PILOT_FAQS.moreCtaTo}>{PILOT_FAQS.moreCtaLabel}</Link>
            </MarketingButton>
          </div>
        </div>
      </div>
    </section>
  )
}

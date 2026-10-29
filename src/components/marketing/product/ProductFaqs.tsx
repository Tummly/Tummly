import { PlusIcon } from "lucide-react"
import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import {
  PRODUCT_DISPLAY_HEADING,
  PRODUCT_FAQS,
  PRODUCT_SECTION_INSET,
} from "@/content/marketing/productPage"
import { marketingHeaderStickyTopClass } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/**
 * Figma Product FAQs `5661:24202`: sticky title left (548px max), 746px list right.
 * Plus glyph turns into a cross when open; Figma shows every answer open.
 */
export function ProductFaqs() {
  return (
    <section
      className="w-full rounded-bl-[12px] rounded-br-[12px] bg-white"
      aria-labelledby="product-faqs-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col gap-8 sm:gap-10 lg:flex-row lg:items-start lg:justify-between lg:gap-[60px]",
          PRODUCT_SECTION_INSET,
          "pt-12.5 pb-17.5",
        )}
      >
        <header
          className={cn(
            "flex w-full flex-col lg:sticky lg:w-[380px] lg:flex-none lg:py-5",
            marketingHeaderStickyTopClass,
          )}
        >
          <h2 id="product-faqs-heading" className={PRODUCT_DISPLAY_HEADING}>
            {PRODUCT_FAQS.title}
          </h2>
        </header>

        <div className="flex min-w-0 w-full flex-col gap-[30px] lg:min-w-0 lg:max-w-[746px] lg:flex-1 lg:pt-5">
          <Accordion
            type="multiple"
            defaultValue={PRODUCT_FAQS.items.map((item) => item.id)}
            className="gap-[30px]"
          >
            {PRODUCT_FAQS.items.map((item) => (
              <AccordionItem
                key={item.id}
                value={item.id}
                className="border-0 border-b border-[#c9c9c9] pb-[30px]"
              >
                <AccordionTrigger className="items-center gap-4 rounded-none border-0 py-0 hover:cursor-pointer hover:no-underline focus-visible:border-0 focus-visible:ring-2 focus-visible:ring-[#141414]/40 **:data-[slot=accordion-trigger-icon]:hidden">
                  <span className="m-0 min-w-0 flex-1 text-left font-jakarta text-lg font-semibold leading-[normal] text-[#141414]">
                    {item.question}
                  </span>
                  <PlusIcon
                    aria-hidden
                    strokeWidth={1.4}
                    className="size-7 shrink-0 text-[#141414] transition-transform duration-200 group-data-[state=open]/accordion-trigger:rotate-45"
                  />
                </AccordionTrigger>
                <AccordionContent className="pt-4 pb-0">
                  <p className="m-0 font-sans text-sm font-normal leading-[18px] text-[#141414]">
                    {item.answer}
                  </p>
                </AccordionContent>
              </AccordionItem>
            ))}
          </Accordion>

          <div className="flex flex-col gap-7 pb-5">
            <div className="flex flex-col gap-3">
              <h3 className="m-0 font-jakarta text-lg font-semibold leading-[normal] text-black">
                {PRODUCT_FAQS.moreTitle}
              </h3>
              <p className="m-0 font-sans text-sm font-normal leading-[19px] text-[#141414]">
                {PRODUCT_FAQS.moreBody}
              </p>
            </div>
            <MarketingButton asChild variant="secondary" className="w-fit">
              <Link to={PRODUCT_FAQS.moreCtaTo}>
                {PRODUCT_FAQS.moreCtaLabel}
              </Link>
            </MarketingButton>
          </div>
        </div>
      </div>
    </section>
  )
}

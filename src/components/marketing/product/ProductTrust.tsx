import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { MarketingCertificationCarousel } from "@/components/marketing/MarketingCertificationCarousel"
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import {
  PRODUCT_DISPLAY_HEADING,
  PRODUCT_EYEBROW,
  PRODUCT_SECTION_INSET,
  PRODUCT_SECTION_Y,
  PRODUCT_TRUST,
} from "@/content/marketing/productPage"
import { cn } from "@/lib/utils"

const accordionTriggerClassName = cn(
  "items-start gap-[30px] rounded-none border-0 py-0 hover:no-underline hover:cursor-pointer",
  "focus-visible:border-0 focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2",
  "**:data-[slot=accordion-trigger-icon]:ml-0 **:data-[slot=accordion-trigger-icon]:size-4",
  "**:data-[slot=accordion-trigger-icon]:shrink-0 **:data-[slot=accordion-trigger-icon]:text-[#141414]",
)

/**
 * Figma Product Trust & privacy `6937:13422`.
 * Intro + CTA, accordion (open by default, Figma shows every answer), and a
 * scrolling row of certification badges with previous / next controls.
 */
export function ProductTrust() {
  return (
    <section
      className="w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-white"
      aria-labelledby="product-trust-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col gap-[60px]",
          PRODUCT_SECTION_INSET,
          PRODUCT_SECTION_Y,
        )}
      >
        <div className="flex w-full flex-col gap-10 lg:flex-row lg:items-start lg:justify-between lg:gap-12">
          <div className="flex flex-col gap-10 lg:min-w-0 lg:flex-1 lg:justify-between lg:self-stretch">
            <header className="flex flex-col gap-[22px]">
              <div className="flex flex-col gap-3.5">
                <p className={PRODUCT_EYEBROW}>{PRODUCT_TRUST.eyebrow}</p>
                <h2
                  id="product-trust-heading"
                  className={cn(PRODUCT_DISPLAY_HEADING, "whitespace-pre-line")}
                >
                  {PRODUCT_TRUST.title}
                </h2>
              </div>
              <p className="m-0 max-w-[692px] font-sans text-base font-normal leading-[22px] text-[#141414]">
                {PRODUCT_TRUST.body}
              </p>
            </header>
            <MarketingButton asChild variant="secondary" className="w-fit">
              <Link to={PRODUCT_TRUST.ctaTo}>{PRODUCT_TRUST.ctaLabel}</Link>
            </MarketingButton>
          </div>

          <Accordion
            type="multiple"
            defaultValue={PRODUCT_TRUST.items.map((item) => item.id)}
            className="w-full gap-[30px] lg:w-[46.5%] lg:max-w-[720px] lg:min-w-0 lg:shrink-0"
          >
            {PRODUCT_TRUST.items.map((item) => (
              <AccordionItem
                key={item.id}
                value={item.id}
                className="border-0 border-b border-[#7c7c7c] pb-5"
              >
                <AccordionTrigger className={accordionTriggerClassName}>
                  <span className="m-0 min-w-0 flex-1 text-left font-jakarta text-xl font-medium leading-[normal] text-[#141414]">
                    {item.title}
                  </span>
                </AccordionTrigger>
                <AccordionContent className="pt-[18px] pb-0">
                  <p className="m-0 font-sans text-base font-normal leading-5 text-[#141414]">
                    {item.body}
                  </p>
                </AccordionContent>
              </AccordionItem>
            ))}
          </Accordion>
        </div>

        <MarketingCertificationCarousel
          certifications={PRODUCT_TRUST.certifications}
        />
      </div>
    </section>
  )
}

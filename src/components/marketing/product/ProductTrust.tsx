import { useRef, useState } from "react"
import { ChevronLeft, ChevronRight } from "lucide-react"
import { Link } from "react-router-dom"

import iso27001Cert from "@/assets/svg/marketing/product/certs/iso-27001.svg"
import cyberEssentialsPlusCert from "@/assets/svg/marketing/product/certs/cyber-essentials-plus.svg"
import icoDpeCert from "@/assets/svg/marketing/product/certs/ico-dpe.svg"
import icoDataCert from "@/assets/svg/marketing/product/certs/ico-data.svg"
import cyberEssentialsCert from "@/assets/svg/marketing/product/certs/cyber-essentials.svg"
import iasmeLevel1Cert from "@/assets/svg/marketing/product/certs/iasme-level-1.svg"
import goodBusinessCharterCert from "@/assets/svg/marketing/product/certs/good-business-charter.svg"
import iso27701Cert from "@/assets/svg/marketing/product/certs/iso-27701.svg"
import ukGdprCert from "@/assets/svg/marketing/product/certs/uk-gdpr.svg"
import privacyChoicesCert from "@/assets/svg/marketing/product/certs/privacy-choices.svg"
import optInMarketingCert from "@/assets/svg/marketing/product/certs/opt-in-marketing.svg"
import dataRightsCert from "@/assets/svg/marketing/product/certs/data-rights.svg"
import { MarketingButton } from "@/components/marketing/MarketingButton"
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

const CERTIFICATION_ICONS: Readonly<Record<string, string>> = {
  "iso-27001": iso27001Cert,
  "cyber-essentials-plus": cyberEssentialsPlusCert,
  "ico-dpe": icoDpeCert,
  "ico-data": icoDataCert,
  "cyber-essentials": cyberEssentialsCert,
  "iasme-level-1": iasmeLevel1Cert,
  "good-business-charter": goodBusinessCharterCert,
  "iso-27701": iso27701Cert,
  "uk-gdpr": ukGdprCert,
  "privacy-choices": privacyChoicesCert,
  "opt-in-marketing": optInMarketingCert,
  "data-rights": dataRightsCert,
}

const accordionTriggerClassName = cn(
  "items-start gap-[30px] rounded-none border-0 py-0 hover:no-underline hover:cursor-pointer",
  "focus-visible:border-0 focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2",
  "**:data-[slot=accordion-trigger-icon]:ml-0 **:data-[slot=accordion-trigger-icon]:size-4",
  "**:data-[slot=accordion-trigger-icon]:shrink-0 **:data-[slot=accordion-trigger-icon]:text-[#141414]",
)

/** Card width 172 + 24 gap — one step of the badge carousel. */
const CAROUSEL_STEP_PX = 196

/**
 * Figma Product Trust & privacy `6937:13422`.
 * Intro + CTA, accordion (open by default, Figma shows every answer), and a
 * scrolling row of certification badges with previous / next controls.
 */
export function ProductTrust() {
  const trackRef = useRef<HTMLUListElement>(null)
  const [edges, setEdges] = useState({ start: true, end: false })

  const updateEdges = () => {
    const track = trackRef.current
    if (track == null) {
      return
    }
    setEdges({
      start: track.scrollLeft <= 1,
      end: track.scrollLeft + track.clientWidth >= track.scrollWidth - 1,
    })
  }

  const scrollTrack = (direction: -1 | 1) => {
    trackRef.current?.scrollBy({
      left: direction * CAROUSEL_STEP_PX * 3,
      behavior: "smooth",
    })
  }

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
              <p className="m-0 max-w-[692px] font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6">
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
            className="w-full shrink-0 gap-[30px] lg:w-[min(100%,720px)]"
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

        <div className="flex w-full flex-col items-end gap-2.5">
          <ul
            ref={trackRef}
            onScroll={updateEdges}
            className="m-0 flex w-full list-none gap-6 overflow-x-auto p-0 pb-1 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
          >
            {PRODUCT_TRUST.certifications.map((cert) => (
              <li
                key={cert.id}
                className="flex w-[172px] shrink-0 flex-col items-center gap-6"
              >
                <div className="flex h-[152px] w-full items-center justify-center rounded-[12px] bg-[#f2f0ec]">
                  <img
                    src={CERTIFICATION_ICONS[cert.id]}
                    alt=""
                    className="block max-h-[82px] max-w-[74px]"
                    aria-hidden
                    decoding="async"
                    loading="lazy"
                  />
                </div>
                <p className="m-0 w-[132px] text-center font-sans text-sm font-normal leading-[17px] text-[#141414]">
                  {cert.label}
                </p>
              </li>
            ))}
          </ul>

          <div className="flex items-start gap-5">
            <button
              type="button"
              aria-label="Previous certifications"
              onClick={() => scrollTrack(-1)}
              disabled={edges.start}
              className="flex size-12 cursor-pointer items-center justify-center rounded-[2px] border border-[#4e4e4e] bg-transparent p-3 text-[#141414] focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:outline-none disabled:cursor-default disabled:opacity-20"
            >
              <ChevronLeft
                className="size-[22px]"
                strokeWidth={1.5}
                aria-hidden
              />
            </button>
            <button
              type="button"
              aria-label="Next certifications"
              onClick={() => scrollTrack(1)}
              disabled={edges.end}
              className="flex size-12 cursor-pointer items-center justify-center rounded-[2px] border border-[#4e4e4e] bg-transparent p-3 text-[#141414] focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:outline-none disabled:cursor-default disabled:opacity-20"
            >
              <ChevronRight
                className="size-[22px]"
                strokeWidth={1.5}
                aria-hidden
              />
            </button>
          </div>
        </div>
      </div>
    </section>
  )
}

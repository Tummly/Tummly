import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import {
  Carousel,
  CarouselContent,
  CarouselItem,
  CarouselNext,
  CarouselPrevious,
} from "@/components/ui/carousel"
import type { FooterTrustBadge } from "@/content/marketing/footerTrustBadges"
import { HOME_TRUST_CONTROL } from "@/content/marketing/homePage"
import { publicHomeTrustBadges } from "@/content/marketing/publicTrustClaims"
import { cn } from "@/lib/utils"

/** Figma carousel badges are drawn 72px tall in the 43px footer artwork scale. */
const BADGE_SCALE = 72 / 43

const accordionTriggerClassName = cn(
  "items-start gap-[30px] rounded-none border-0 py-0 hover:no-underline hover:cursor-pointer",
  "focus-visible:border-0 focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2",
  "**:data-[slot=accordion-trigger-icon]:ml-0 **:data-[slot=accordion-trigger-icon]:size-4",
  "**:data-[slot=accordion-trigger-icon]:shrink-0 **:data-[slot=accordion-trigger-icon]:text-[#141414]",
)

const carouselArrowClassName = cn(
  "static size-12 translate-y-0 rounded-[2px] border-[#4e4e4e] bg-transparent p-0 text-[#141414]",
  "hover:bg-[#f1f1f1] disabled:opacity-20 [&_svg]:size-[22px]",
)

function TrustBadgeSlide({ badge }: { badge: FooterTrustBadge }) {
  return (
    <CarouselItem className="basis-auto pl-6">
      <div className="flex w-[172px] flex-col items-center gap-6">
        <div className="flex h-[152px] w-full items-center justify-center rounded-xl bg-[#f2f0ec]">
          <img
            src={badge.src}
            alt=""
            width={Math.round(badge.width * BADGE_SCALE)}
            height={Math.round(badge.height * BADGE_SCALE)}
            className="block shrink-0"
            decoding="async"
            aria-hidden
          />
        </div>
        <p className="m-0 w-[132px] text-center font-sans text-sm font-normal leading-[17px] text-[#141414]">
          {badge.title}
        </p>
      </div>
    </CarouselItem>
  )
}

/**
 * Home Trust & control — Figma `4942:19219`.
 * Copy + accordion (all items open, as drawn) + "Explore Trust & Privacy".
 * The badge carousel stays hidden behind `publicHomeTrustBadges()`.
 */
export function HomeTrustControl() {
  const allOpen = HOME_TRUST_CONTROL.items.map((item) => item.id)
  const badges = publicHomeTrustBadges()

  return (
    <section
      className="w-full bg-white"
      aria-labelledby="home-trust-control-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col gap-[60px] py-[70px]",
          // Figma mobile `6241:18218`: 24px. Desktop keep 60px chrome inset.
          "px-6 lg:px-[60px]",
        )}
      >
        <div className="flex w-full flex-col gap-[56px] lg:flex-row lg:items-stretch lg:justify-between lg:gap-12">
          <div className="flex max-w-[548px] flex-col justify-between gap-[46px] lg:min-w-0 lg:flex-1">
            <div className="flex flex-col gap-[22px] text-[#141414]">
              <header className="flex flex-col gap-3.5">
                <p className="m-0 font-jakarta text-sm font-medium leading-4 text-[#4a4a4a]">
                  {HOME_TRUST_CONTROL.eyebrow}
                </p>
                <h2
                  id="home-trust-control-heading"
                  className="m-0 max-w-[514px] font-jakarta text-[34px] font-medium leading-[40px] lg:text-[44px] lg:leading-[50px] xl:text-[56px] xl:leading-[62px]"
                >
                  {HOME_TRUST_CONTROL.title}
                </h2>
              </header>
              <p className="m-0 max-w-[502px] font-sans text-base font-normal leading-[22px] lg:text-[18px] lg:leading-6">
                {HOME_TRUST_CONTROL.body}
              </p>
            </div>
            <MarketingButton variant="secondary" asChild className="w-fit">
              <Link to={HOME_TRUST_CONTROL.ctaTo}>
                {HOME_TRUST_CONTROL.ctaLabel}
              </Link>
            </MarketingButton>
          </div>

          <Accordion
            type="multiple"
            defaultValue={allOpen}
            className="w-full gap-[30px] lg:min-w-0 lg:max-w-[754px] lg:flex-1"
          >
            {HOME_TRUST_CONTROL.items.map((item) => (
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

        {badges.length > 0 ? (
          <Carousel
            opts={{ align: "start", dragFree: true, containScroll: "trimSnaps" }}
            className="flex w-full flex-col gap-2.5"
            aria-label="Trust and privacy credentials"
          >
            <CarouselContent className="-ml-6">
              {badges.map((badge) => (
                <TrustBadgeSlide key={badge.itemNumber} badge={badge} />
              ))}
            </CarouselContent>
            <div className="flex items-start justify-end gap-5">
              <CarouselPrevious
                variant="outline"
                size="icon-sm"
                className={carouselArrowClassName}
              />
              <CarouselNext
                variant="outline"
                size="icon-sm"
                className={carouselArrowClassName}
              />
            </div>
          </Carousel>
        ) : null}
      </div>
    </section>
  )
}

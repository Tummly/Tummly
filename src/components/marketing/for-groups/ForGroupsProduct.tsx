import { ChevronLeftIcon, ChevronRightIcon } from "lucide-react"
import { Link } from "react-router-dom"

import posterAiAssistant from "@/assets/images/marketing/for-groups/ai-assistant.png"
import posterCampaigns from "@/assets/images/marketing/for-groups/campaigns.png"
import posterCapture from "@/assets/images/marketing/for-groups/capture.png"
import posterFeedback from "@/assets/images/marketing/for-groups/feedback.png"
import posterGuests from "@/assets/images/marketing/for-groups/guests.png"
import posterOffers from "@/assets/images/marketing/for-groups/offers.png"
import posterReports from "@/assets/images/marketing/for-groups/reports.png"
import posterWeeklyBrief from "@/assets/images/marketing/for-groups/weekly-brief.png"
import { ForGroupsProductCardMedia } from "@/components/marketing/for-groups/ForGroupsProductCardMedia"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  Carousel,
  CarouselContent,
  CarouselItem,
  useCarousel,
} from "@/components/ui/carousel"
import {
  FOR_GROUPS_PRODUCT,
  FOR_GROUPS_SECTION_HEADING,
  FOR_GROUPS_SECTION_INSET,
  FOR_GROUPS_SECTION_Y,
  type ForGroupsProductSlide,
} from "@/content/marketing/forGroupsPage"
import {
  forGroupsProductVideoUrl,
  type ForGroupsProductVideoKey,
} from "@/lib/marketingMedia"
import { cn } from "@/lib/utils"

const SLIDE_POSTERS = {
  capture: posterCapture,
  feedback: posterFeedback,
  "ai-assistant": posterAiAssistant,
  guests: posterGuests,
  offers: posterOffers,
  campaigns: posterCampaigns,
  reports: posterReports,
  "weekly-brief": posterWeeklyBrief,
} as const satisfies Record<ForGroupsProductVideoKey, string>

const controlButtonClass =
  "inline-flex size-11 shrink-0 cursor-pointer items-center justify-center rounded-[2.5px] border-0 p-0 text-[#141414] shadow-none outline-none transition-opacity focus-visible:ring-2 focus-visible:ring-[#141414]/30 disabled:cursor-not-allowed disabled:opacity-40"

function ForGroupsProductSlideCard({ slide }: { slide: ForGroupsProductSlide }) {
  return (
    <article
      className="relative flex h-[496px] w-full flex-col items-center justify-end overflow-hidden rounded-[12px] pb-10"
      style={{ backgroundColor: FOR_GROUPS_PRODUCT.cardBackground }}
    >
      <ForGroupsProductCardMedia
        videoSrc={forGroupsProductVideoUrl(slide.id)}
        posterSrc={SLIDE_POSTERS[slide.id]}
      />
      <div className="relative z-10 flex w-full flex-col items-center gap-[7px] px-2.5 text-center text-white">
        <h3 className="m-0 font-jakarta text-xl font-medium leading-normal">
          {slide.title}
        </h3>
        <p className="m-0 max-w-[273px] font-sans text-sm font-normal leading-5">
          {slide.body}
        </p>
      </div>
    </article>
  )
}

function ForGroupsProductCarouselControls() {
  const { scrollPrev, scrollNext, canScrollPrev, canScrollNext } = useCarousel()

  return (
    <div className="flex items-center gap-[18px]">
      <button
        type="button"
        className={cn(controlButtonClass, "bg-transparent")}
        aria-label="Previous product slide"
        disabled={!canScrollPrev}
        onClick={scrollPrev}
      >
        <ChevronLeftIcon className="size-[18px]" strokeWidth={2} aria-hidden />
      </button>
      <button
        type="button"
        className={cn(controlButtonClass, "bg-[#dbdbdb]")}
        aria-label="Next product slide"
        disabled={!canScrollNext}
        onClick={scrollNext}
      >
        <ChevronRightIcon className="size-[18px]" strokeWidth={2} aria-hidden />
      </button>
    </div>
  )
}

/**
 * Figma For Groups Product carousel `6320:19475`.
 * Frame 5564 cards: background video (poster still fallback) + text overlay.
 */
export function ForGroupsProduct() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="for-groups-product-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-start gap-[46px]",
          FOR_GROUPS_SECTION_INSET,
          FOR_GROUPS_SECTION_Y,
        )}
      >
        <header className="flex max-w-[548px] flex-col gap-3.5 text-[#141414]">
          <p className="m-0 font-jakarta text-sm font-medium leading-4">
            {FOR_GROUPS_PRODUCT.eyebrow}
          </p>
          <h2
            id="for-groups-product-heading"
            className={cn("m-0", FOR_GROUPS_SECTION_HEADING)}
          >
            {FOR_GROUPS_PRODUCT.title}
          </h2>
        </header>

        <Carousel
          opts={{ align: "start", containScroll: "trimSnaps" }}
          className="flex w-full flex-col gap-[46px]"
        >
          <CarouselContent className="-ml-[26px]">
            {FOR_GROUPS_PRODUCT.slides.map((slide) => (
              <CarouselItem
                key={slide.id}
                className="basis-[min(365px,85vw)] pl-[26px] sm:basis-[365px]"
              >
                <ForGroupsProductSlideCard slide={slide} />
              </CarouselItem>
            ))}
          </CarouselContent>

          <div className="flex w-full flex-wrap items-center justify-between gap-4">
            <MarketingButton
              asChild
              variant="secondary"
              className="w-full sm:w-[210px]"
            >
              <Link to={FOR_GROUPS_PRODUCT.ctaTo}>
                {FOR_GROUPS_PRODUCT.ctaLabel}
              </Link>
            </MarketingButton>
            <ForGroupsProductCarouselControls />
          </div>
        </Carousel>
      </div>
    </section>
  )
}

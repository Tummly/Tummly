import { hospitalityPictures } from "@/assets/marketing-images"
import { cn } from "@/lib/utils"
import ImageWithCard from "@/components/home/ImageWithCard"
import {
  Carousel,
  CarouselContent,
  CarouselItem,
  CarouselNext,
  CarouselPrevious,
} from "@/components/ui/carousel"

const hospitalitySlides = [
  {
    picture: hospitalityPictures[0],
    title: "Takeaways and quick-service restaurants",
    description:
      "Invite guests to join from counters, receipts, packaging and delivery inserts, so more orders can become direct guest relationships.",
  },
  {
    picture: hospitalityPictures[1],
    title: "Cafés, coffee shops and bakeries",
    description:
      "Grow your guest list, collect quick feedback and send simple offers for quieter periods, new items and return visits.",
  },
  {
    picture: hospitalityPictures[2],
    title: "Casual dining and hospitality",
    description:
      "Collect private feedback after visits and follow up with guests who choose to hear from you again.",
  },
  {
    picture: hospitalityPictures[3],
    title: "Small groups and multi-site operators",
    description:
      "View guest captures, feedback, offers and campaign activity by location from one shared workspace.",
  },
] as const

// Duplicated so Embla always has enough track width to scroll/loop (4 slides alone can fit on wide viewports).
const hospitalityCarouselSlides = [
  ...hospitalitySlides,
  ...hospitalitySlides,
] as const

const sectionInset =
  "px-4 sm:px-6 md:px-10 lg:px-16 xl:px-45"
const sectionInsetLeft =
  "pl-4 sm:pl-6 md:pl-10 lg:pl-16 xl:pl-45"
const sectionInsetRight =
  "pr-4 sm:pr-6 md:pr-10 lg:pr-16 xl:pr-45"

function Hospitality() {
  return (
    <section className="w-full bg-white">
      <div className="mx-auto flex w-full flex-col gap-12 py-12 sm:gap-14 sm:py-16 lg:gap-15 lg:py-22.5">
        <div className={sectionInset}>
          <header className="flex max-w-2xl flex-col gap-3">
            <h2 className="m-0 text-[clamp(1.75rem,4vw,2.625rem)] font-bold leading-[normal] text-[#232323]">
              Built for restaurants, cafés and hospitality groups
            </h2>
            <p className="m-0 text-base font-medium leading-6.5 text-[#232323] sm:text-[17px] lg:text-lg">
              Tummly helps you collect private feedback, grow your guest list and
              bring guests back with clear consent built into every step.
            </p>
          </header>
        </div>

        <Carousel
          opts={{ align: "start", loop: true }}
          className="flex w-full flex-col gap-15"
        >
          <div className={sectionInsetLeft}>
            <CarouselContent className={cn("-ml-7.5", sectionInsetRight)}>
              {hospitalityCarouselSlides.map((slide, index) => (
                <CarouselItem
                  key={`${slide.title}-${index}`}
                  className="basis-full pl-7.5 sm:basis-109"
                >
                  <ImageWithCard
                    picture={slide.picture}
                    imageAlt={slide.title}
                    title={slide.title}
                    description={slide.description}
                  />
                </CarouselItem>
              ))}
            </CarouselContent>
          </div>

          <div className={cn("flex gap-2", sectionInset)}>
            <CarouselPrevious
              variant="default"
              size="icon-xs"
              className="static top-auto left-auto size-8 translate-x-0 translate-y-0"
            />
            <CarouselNext
              variant="default"
              size="icon-xs"
              className="static top-auto right-auto size-8 translate-x-0 translate-y-0"
            />
          </div>
        </Carousel>
      </div>
    </section>
  )
}

export default Hospitality

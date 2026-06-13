import card4 from "@/assets/images/card4.jpg"
import card5 from "@/assets/images/card5.jpg"
import card6 from "@/assets/images/card6.jpg"
import card7 from "@/assets/images/card7.jpg"
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
    image: card4,
    title: "Takeaways and quick-service restaurants",
    description:
      "Invite guests to join from counters, receipts, packaging and delivery inserts, so more orders can become direct guest relationships.",
  },
  {
    image: card5,
    title: "Cafés, coffee shops and bakeries",
    description:
      "Grow your guest list, collect quick feedback and send simple offers for quieter periods, new items and return visits.",
  },
  {
    image: card6,
    title: "Casual dining and hospitality",
    description:
      "Collect private feedback after visits and follow up with guests who choose to hear from you again.",
  },
  {
    image: card7,
    title: "Small groups and multi-site operators",
    description:
      "View guest captures, feedback, offers and campaign activity by location from one shared workspace.",
  },
] as const

function Hospitality() {
  return (
    <section className="w-full bg-white">
      <div className="mx-auto flex w-full max-w-360 flex-col gap-12 px-4 py-12 sm:gap-14 sm:px-6 sm:py-16 md:px-10 lg:gap-15 lg:px-16 lg:py-22.5 xl:px-45">
        <header className="flex max-w-2xl flex-col gap-3">
          <h2 className="m-0 text-[clamp(1.75rem,4vw,2.625rem)] font-bold leading-[normal] text-[#232323]">
            Built for restaurants, cafés and hospitality groups
          </h2>
          <p className="m-0 text-base font-medium leading-6.5 text-[#232323] sm:text-[17px] lg:text-lg">
            Tummly helps you collect private feedback, grow your guest list and
            bring guests back with clear consent built into every step.
          </p>
        </header>

        <Carousel
          opts={{ align: "start", loop: false }}
          className="flex w-full flex-col gap-15"
        >
          <CarouselContent className="-ml-7.5">
            {hospitalitySlides.map((slide) => (
              <CarouselItem
                key={slide.title}
                className="basis-full pl-7.5 sm:basis-[calc(50%-0.9375rem)] lg:basis-[calc(33.333%-1.25rem)] xl:basis-[calc(30%-1.125rem)]"
              >
                <ImageWithCard
                  image={slide.image}
                  imageAlt={slide.title}
                  title={slide.title}
                  description={slide.description}
                />
              </CarouselItem>
            ))}
          </CarouselContent>

          <div className="flex gap-2">
            <CarouselPrevious
              variant="outline"
              size="icon-xs"
              className="static top-auto left-auto size-8 translate-x-0 translate-y-0 border-0 bg-[#e4e4e4] text-[#232323] hover:bg-[#e4e4e4]/80 hover:text-[#232323]"
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

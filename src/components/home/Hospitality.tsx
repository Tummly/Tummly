import { hospitalityPictures } from "@/assets/marketing-images"
import CarouselNavigation from "@/components/home/CarouselNavigation"
import ImageWithCard from "@/components/home/ImageWithCard"
import {
  Carousel,
  CarouselContent,
  CarouselItem,
} from "@/components/ui/carousel"
import {
  marketingCarouselContentClass,
  marketingCarouselItemClass,
  marketingCarouselOptions,
  marketingSectionBody,
  marketingSectionHeading,
  marketingSectionInset,
  marketingSectionPadding,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

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

function Hospitality() {
  return (
    <section className="w-full bg-white">
      <div
        className={cn(
          "mx-auto flex w-full flex-col gap-12 sm:gap-14 lg:gap-15",
          marketingSectionPadding
        )}
      >
        <div className={marketingSectionInset}>
          <header className="flex max-w-2xl flex-col gap-3">
            <h2 className={cn("m-0", marketingSectionHeading)}>
              Built for restaurants, cafés and hospitality groups
            </h2>
            <p className={cn("m-0", marketingSectionBody)}>
              Tummly helps you collect private feedback, grow your guest list and
              bring guests back with clear consent built into every step.
            </p>
          </header>
        </div>

        <Carousel
          opts={marketingCarouselOptions(hospitalitySlides.length)}
          className="flex w-full flex-col gap-5 lg:gap-15"
        >
          <CarouselContent className={marketingCarouselContentClass}>
            {hospitalitySlides.map((slide, index) => (
              <CarouselItem
                key={slide.title}
                className={marketingCarouselItemClass(index)}
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

          <CarouselNavigation className={marketingSectionInset} />
        </Carousel>
      </div>
    </section>
  )
}

export default Hospitality

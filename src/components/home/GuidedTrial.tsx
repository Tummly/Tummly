import { trialPictures } from "@/assets/marketing-images"
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

const trialSlides = [
  {
    picture: trialPictures[0],
    title: "Trial workspace access",
    description:
      "Use the Tummly workspace during your trial, including guest links, private feedback, guest list, offers, campaigns and weekly brief.",
  },
  {
    picture: trialPictures[1],
    title: "Starter QR materials",
    description:
      "Approved trials include starter materials matched to your setup, so guests can scan from key in-store, takeaway, delivery and digital touchpoints.",
  },
  {
    picture: trialPictures[2],
    title: "Smart Guest Links",
    description:
      "Use trackable links for digital channels, receipts, messages and places where a printed QR prompt is not the best fit.",
  },
  {
    picture: trialPictures[3],
    title: "Feedback and opt-in form",
    description:
      "Let guests share quick private feedback and choose whether to join your restaurant’s guest list.",
  },
  {
    picture: trialPictures[4],
    title: "Offers and campaign sending",
    description:
      "Create your first thank-you, quiet-day or win-back offer and send simple messages to eligible opted-in guests.",
  },
  {
    picture: trialPictures[5],
    title: "Trial launch allowance",
    description:
      "Your trial includes a standard usage allowance for launch activity, including eligible messages and AI-assisted briefs. Any extra usage or paid add-ons are confirmed before use.",
  },
  {
    picture: trialPictures[6],
    title: "AI-assisted weekly brief",
    description:
      "See what changed each week, what guests are saying and which actions are worth reviewing next.",
  },
  {
    picture: trialPictures[7],
    title: "Guided launch support",
    description:
      "Get help setting up your workspace, choosing your first guest prompts and preparing your first feedback or offer campaign.",
  },
] as const

function GuidedTrial() {
  return (
    <section className="w-full bg-white">
      <div
        className={cn(
          "mx-auto flex w-full flex-col gap-12 sm:gap-14 lg:gap-15",
          marketingSectionPadding
        )}
      >
        <div className={marketingSectionInset}>
          <header className="flex max-w-3xl flex-col gap-3">
            <h2 className={cn("m-0", marketingSectionHeading)}>
              What&apos;s included in your guided trial
            </h2>
            <p className={cn("m-0", marketingSectionBody)}>
              Your guided trial includes access to the core workspace, starter QR
              materials, guided setup support and a standard launch allowance to
              help you start your first Guest Loop. We review your setup before
              opening the workspace.
            </p>
          </header>
        </div>

        <Carousel
          opts={marketingCarouselOptions(trialSlides.length)}
          className="flex w-full flex-col gap-5"
        >
          <CarouselContent className={marketingCarouselContentClass}>
            {trialSlides.map((slide, index) => (
              <CarouselItem
                key={slide.title}
                className={marketingCarouselItemClass(index)}
              >
                <ImageWithCard
                  picture={slide.picture}
                  imageAlt={slide.title}
                  title={slide.title}
                  description={slide.description}
                  size="trial"
                />
              </CarouselItem>
            ))}
          </CarouselContent>

          <CarouselNavigation className={marketingSectionInset} />
        </Carousel>

        <div className={marketingSectionInset}>
          <p className="m-0 max-w-2xl text-sm leading-5 text-[#232323]">
            Trial length, starter materials and launch allowance may vary by
            setup. No payment is taken when you request access. Reorders, premium
            branded print packs and extra usage can be added later with approval.
          </p>
        </div>
      </div>
    </section>
  )
}

export default GuidedTrial

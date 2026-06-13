import { useState, useSyncExternalStore } from "react"

import trial1 from "@/assets/images/trial1.jpg"
import trial2 from "@/assets/images/trial2.jpg"
import trial3 from "@/assets/images/trial3.jpg"
import trial4 from "@/assets/images/trial4.jpg"
import trial5 from "@/assets/images/trial5.jpg"
import trial6 from "@/assets/images/trial6.jpg"
import trial7 from "@/assets/images/trial7.jpg"
import trial8 from "@/assets/images/trial8.jpg"
import ImageWithCard from "@/components/home/ImageWithCard"
import {
  Carousel,
  type CarouselApi,
  CarouselContent,
  CarouselItem,
  CarouselNext,
  CarouselPrevious,
} from "@/components/ui/carousel"

const trialSlides = [
  {
    image: trial1,
    title: "Trial workspace access",
    description:
      "Use the Tummly workspace during your trial, including guest links, private feedback, guest list, offers, campaigns and weekly brief.",
  },
  {
    image: trial2,
    title: "Starter QR materials",
    description:
      "Approved trials include starter materials matched to your setup, so guests can scan from key in-store, takeaway, delivery and digital touchpoints.",
  },
  {
    image: trial3,
    title: "Smart Guest Links",
    description:
      "Use trackable links for digital channels, receipts, messages and places where a printed QR prompt is not the best fit.",
  },
  {
    image: trial4,
    title: "Feedback and opt-in form",
    description:
      "Let guests share quick private feedback and choose whether to join your restaurant’s guest list.",
  },
  {
    image: trial5,
    title: "Offers and campaign sending",
    description:
      "Create your first thank-you, quiet-day or win-back offer and send simple messages to eligible opted-in guests.",
  },
  {
    image: trial6,
    title: "Trial launch allowance",
    description:
      "Your trial includes a standard usage allowance for launch activity, including eligible messages and AI-assisted briefs. Any extra usage or paid add-ons are confirmed before use.",
  },
  {
    image: trial7,
    title: "AI-assisted weekly brief",
    description:
      "See what changed each week, what guests are saying and which actions are worth reviewing next.",
  },
  {
    image: trial8,
    title: "Guided launch support",
    description:
      "Get help setting up your workspace, choosing your first guest prompts and preparing your first feedback or offer campaign.",
  },
] as const

function GuidedTrial() {
  const [api, setApi] = useState<CarouselApi>()

  const scrollProgress = useSyncExternalStore(
    (onStoreChange) => {
      if (!api) return () => { }

      const handleUpdate = () => onStoreChange()

      api.on("reInit", handleUpdate)
      api.on("scroll", handleUpdate)

      return () => {
        api.off("reInit", handleUpdate)
        api.off("scroll", handleUpdate)
      }
    },
    () => api?.scrollProgress() ?? 0,
    () => 0
  )

  const progressPercent =
    scrollProgress === 0
      ? (1 / trialSlides.length) * 100
      : Math.max(scrollProgress * 100, (1 / trialSlides.length) * 100)

  return (
    <section className="w-full bg-white">
      <div className="mx-auto flex w-full max-w-360 flex-col gap-12 px-4 py-12 sm:gap-14 sm:px-6 sm:py-16 md:px-10 lg:gap-15 lg:px-16 lg:py-22.5 xl:px-45">
        <header className="flex max-w-3xl flex-col gap-3">
          <h2 className="m-0 text-[clamp(1.75rem,4vw,2.625rem)] font-bold leading-[normal] text-[#232323]">
            What&apos;s included in your guided trial
          </h2>
          <p className="m-0 text-base font-medium leading-6.5 text-[#232323] sm:text-[17px] lg:text-lg">
            Your guided trial includes access to the core workspace, starter QR
            materials, guided setup support and a standard launch allowance to
            help you start your first Guest Loop. We review your setup before
            opening the workspace.
          </p>
        </header>

        <Carousel
          setApi={setApi}
          opts={{ align: "start", loop: false }}
          className="flex w-full flex-col gap-5"
        >
          <CarouselContent className="-ml-7.5">
            {trialSlides.map((slide) => (
              <CarouselItem
                key={slide.title}
                className="basis-full pl-7.5 sm:basis-1/2 lg:basis-1/3"
              >
                <ImageWithCard
                  image={slide.image}
                  imageAlt={slide.title}
                  title={slide.title}
                  description={slide.description}
                  size="trial"
                />
              </CarouselItem>
            ))}
          </CarouselContent>

          <div
            className="h-0.5 w-full overflow-hidden rounded-full bg-[#f3f3f3]"
            role="progressbar"
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={Math.round(progressPercent)}
            aria-label="Carousel scroll progress"
          >
            <div
              className="h-full rounded-full bg-[#14a247]"
              style={{ width: `${progressPercent}%` }}
            />
          </div>

          <div className="flex gap-2">
            <CarouselPrevious
              variant="outline"
              size="icon-xs"
              className="static top-auto left-auto size-8 translate-x-0 translate-y-0 border-0 bg-[#f3f3f3] text-[#232323] hover:bg-[#f3f3f3]/80 hover:text-[#232323]"
            />
            <CarouselNext
              variant="default"
              size="icon-xs"
              className="static top-auto right-auto size-8 translate-x-0 translate-y-0"
            />
          </div>
        </Carousel>

        <p className="m-0 max-w-2xl text-sm leading-5 text-[#232323]">
          Trial length, starter materials and launch allowance may vary by
          setup. No payment is taken when you request access. Reorders, premium
          branded print packs and extra usage can be added later with approval.
        </p>
      </div>
    </section>
  )
}

export default GuidedTrial

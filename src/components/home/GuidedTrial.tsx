import { useState, useSyncExternalStore } from "react"

import { trialPictures } from "@/assets/marketing-images"
import { cn } from "@/lib/utils"
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

const sectionInset =
  "px-4 sm:px-6 md:px-10 lg:px-16 xl:px-45"
const sectionInsetLeft =
  "pl-4 sm:pl-6 md:pl-10 lg:pl-16 xl:pl-45"
const sectionInsetRight =
  "pr-4 sm:pr-6 md:pr-10 lg:pr-16 xl:pr-45"

function GuidedTrial() {
  const [api, setApi] = useState<CarouselApi>()

  const selectedIndex = useSyncExternalStore(
    (onStoreChange) => {
      if (!api) return () => { }

      const handleUpdate = () => onStoreChange()

      api.on("reInit", handleUpdate)
      api.on("select", handleUpdate)

      return () => {
        api.off("reInit", handleUpdate)
        api.off("select", handleUpdate)
      }
    },
    () => api?.selectedScrollSnap() ?? 0,
    () => 0
  )

  const progressPercent =
    ((selectedIndex + 1) / trialSlides.length) * 100

  return (
    <section className="w-full bg-white">
      <div className="mx-auto flex w-full flex-col gap-12 py-12 sm:gap-14 sm:py-16 lg:gap-15 lg:py-22.5">
        <div className={sectionInset}>
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
        </div>

        <Carousel
          setApi={setApi}
          opts={{ align: "start", loop: true }}
          className="flex w-full flex-col gap-5"
        >
          <div className={sectionInsetLeft}>
            <CarouselContent className={cn("-ml-7.5", sectionInsetRight)}>
              {trialSlides.map((slide) => (
                <CarouselItem
                  key={slide.title}
                  className="basis-full pl-7.5 sm:basis-109"
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
          </div>

          <div className={cn("flex flex-col gap-5", sectionInset)}>
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
          </div>
        </Carousel>

        <div className={sectionInset}>
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

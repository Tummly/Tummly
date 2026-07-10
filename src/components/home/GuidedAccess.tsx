import { howItWorksPictures } from "@/assets/marketing-images"
import CarouselNavigation from "@/components/home/CarouselNavigation"
import OptimizedImage, {
  type PictureOutput,
} from "@/components/media/OptimizedImage"
import {
  Carousel,
  CarouselContent,
  CarouselItem,
} from "@/components/ui/carousel"
import { GRID_CARD_IMAGE_SIZES } from "@/lib/imagePresets"
import {
  marketingCarouselContentClassCompact,
  marketingCarouselItemClassCompact,
  marketingCarouselOptions,
  marketingSectionBody,
  marketingSectionHeading,
  marketingSectionInset,
  marketingSectionPadding,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

type AccessStepCardProps = {
  step: string
  title: string
  description: string
  picture: PictureOutput
  imageAlt: string
  isLast?: boolean
}

function StepIndicator({ isLast = false }: { isLast?: boolean }) {
  return (
    <div className="flex h-2.5 w-full items-center gap-1.25" aria-hidden>
      <span className="size-2.5 shrink-0 bg-black" />
      {!isLast && <span className="h-px flex-1 bg-black" />}
    </div>
  )
}

function AccessStepCard({
  step,
  title,
  description,
  picture,
  imageAlt,
  isLast = false,
}: AccessStepCardProps) {
  return (
    <article className="flex flex-col gap-6.5">
      <div className="relative aspect-436/230 w-full overflow-hidden rounded-[6px]">
        <OptimizedImage
          picture={picture}
          sizes={GRID_CARD_IMAGE_SIZES}
          alt={imageAlt}
          className="absolute inset-0 size-full rounded-[6px] object-cover"
        />
      </div>

      <StepIndicator isLast={isLast} />

      <div className="flex flex-col gap-3">
        <p className="m-0 text-sm font-bold uppercase leading-[normal] text-black">
          {step}
        </p>
        <h3 className="m-0 font-serif text-2xl leading-[normal] text-black">
          {title}
        </h3>
        <p className="m-0 text-sm font-normal leading-4.75 text-black">
          {description}
        </p>
      </div>
    </article>
  )
}

const accessSteps = [
  {
    step: "Step 1",
    title: "Request guided access",
    description:
      "Tell us about your restaurant, location count, role and main goal so we can route your setup correctly.",
    picture: howItWorksPictures[0],
    imageAlt: "Request guided access",
  },
  {
    step: "Step 2",
    title: "Verify your email",
    description:
      "We send a short code to confirm your email before reviewing the request.",
    picture: howItWorksPictures[1],
    imageAlt: "Verify your email",
  },
  {
    step: "Step 3",
    title: "Create your workspace",
    description:
      "Once approved, you receive a secure setup link to create your account, add your restaurant details and start the right setup path.",
    picture: howItWorksPictures[2],
    imageAlt: "Create your workspace",
  },
] as const

function GuidedAccessCarousel() {
  return (
    <Carousel
      opts={marketingCarouselOptions()}
      className="flex w-full flex-col gap-5 lg:hidden"
    >
      <CarouselContent className={marketingCarouselContentClassCompact}>
        {accessSteps.map((item, index) => (
          <CarouselItem
            key={item.step}
            className={marketingCarouselItemClassCompact(index, accessSteps.length)}
          >
            <AccessStepCard
              step={item.step}
              title={item.title}
              description={item.description}
              picture={item.picture}
              imageAlt={item.imageAlt}
              isLast={index === accessSteps.length - 1}
            />
          </CarouselItem>
        ))}
      </CarouselContent>

      <CarouselNavigation className={marketingSectionInset} />
    </Carousel>
  )
}

function GuidedAccess() {
  return (
    <section className="w-full bg-[#f8f8f8]">
      <div
        className={cn(
          "mx-auto flex w-full flex-col gap-12 sm:gap-14 lg:gap-15",
          marketingSectionPadding,
        )}
      >
        <header
          className={cn(
            "flex max-w-2xl md:max-w-5xl flex-col gap-3",
            marketingSectionInset,
          )}
        >
          <h2 className={cn("m-0", marketingSectionHeading)}>
            How guided access works
          </h2>
          <p className={cn("m-0", marketingSectionBody)}>
            Request access, verify your email and receive the right setup link
            once your restaurant details are reviewed.
          </p>
        </header>

        <GuidedAccessCarousel />

        <div
          className={cn(
            "hidden gap-7.5 lg:grid lg:grid-cols-3",
            marketingSectionInset,
          )}
        >
          {accessSteps.map((item, index) => (
            <AccessStepCard
              key={item.step}
              step={item.step}
              title={item.title}
              description={item.description}
              picture={item.picture}
              imageAlt={item.imageAlt}
              isLast={index === accessSteps.length - 1}
            />
          ))}
        </div>

        <p
          className={cn(
            "m-0 max-w-2xl md:max-w-4xl text-sm leading-5 text-[#232323]",
            marketingSectionInset,
          )}
        >
          After your workspace is opened, we help you prepare your first Guest
          Loop with starter QR materials, guest form setup, offer guidance and
          launch support.
        </p>
      </div>
    </section>
  )
}

export default GuidedAccess

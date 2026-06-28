import CarouselProgress from "@/components/home/CarouselProgress"
import {
  CarouselNext,
  CarouselPrevious,
  useCarousel,
} from "@/components/ui/carousel"
import { useCarouselScrollProgress } from "@/hooks/use-carousel-scroll-progress"
import { cn } from "@/lib/utils"

type CarouselNavigationProps = {
  className?: string
}

const navButtonClass =
  "static top-auto size-8 min-h-8 min-w-8 translate-x-0 translate-y-0 [&_svg:not([class*='size-'])]:size-4 disabled:bg-[#e4e4e4] disabled:text-[#737373] disabled:opacity-100 disabled:hover:bg-[#e4e4e4]"

function CarouselNavigation({ className }: CarouselNavigationProps) {
  const { api, canScrollPrev, canScrollNext } = useCarousel()
  const { progressPercent } = useCarouselScrollProgress(api)

  if (!canScrollPrev && !canScrollNext) {
    return null
  }

  return (
    <div className={cn("flex w-full flex-col gap-5", className)}>
      <CarouselProgress progressPercent={progressPercent} />

      <div className="flex gap-2">
        <CarouselPrevious
          variant="default"
          size="icon-xs"
          className={navButtonClass}
        />
        <CarouselNext
          variant="default"
          size="icon-xs"
          className={cn(navButtonClass, "right-auto")}
        />
      </div>
    </div>
  )
}

export default CarouselNavigation

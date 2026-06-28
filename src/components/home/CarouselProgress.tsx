import { cn } from "@/lib/utils"

type CarouselProgressProps = {
  progressPercent: number
  className?: string
  "aria-label"?: string
}

function CarouselProgress({
  progressPercent,
  className,
  "aria-label": ariaLabel = "Carousel scroll progress",
}: CarouselProgressProps) {
  return (
    <div
      className={cn(
        "h-0.5 w-full overflow-hidden rounded-full bg-[#f3f3f3]",
        className
      )}
      role="progressbar"
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuenow={Math.round(progressPercent)}
      aria-label={ariaLabel}
    >
      <div
        className="h-full rounded-full bg-[#14a247] transition-[width] duration-300 ease-out"
        style={{ width: `${progressPercent}%` }}
      />
    </div>
  )
}

export default CarouselProgress

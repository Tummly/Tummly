import { Progress } from "@/components/ui/progress"
import { cn } from "@/lib/utils"

type SignupLocationsCapacityProps = {
  current: number
  cap: number
  className?: string
}

export function SignupLocationsCapacity({
  current,
  cap,
  className,
}: SignupLocationsCapacityProps) {
  const safeCap = Math.max(cap, 0)
  const safeCurrent = Math.max(current, 0)
  const percent =
    safeCap > 0 ? Math.min(100, Math.round((safeCurrent / safeCap) * 100)) : 0

  return (
    <div className={cn("flex w-full flex-col gap-3", className)}>
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <p className="m-0 font-sans text-sm font-medium text-[#141414]">
          Location capacity
        </p>
        <p
          className="m-0 font-sans text-sm font-semibold text-[#141414]"
          aria-live="polite"
        >
          {safeCurrent} of {safeCap} added
        </p>
      </div>
      <Progress value={percent} className="h-2 rounded-full bg-[#e8e8e8]" />
    </div>
  )
}

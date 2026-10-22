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
      <div className="flex items-center justify-between gap-2 font-sans text-sm leading-[18px] text-[#141414]">
        <p className="m-0 font-bold">Location capacity</p>
        <p className="m-0 font-medium" aria-live="polite">
          {safeCurrent} of {safeCap} added
        </p>
      </div>
      <Progress
        value={percent}
        aria-label="Location capacity"
        className="h-1.5 rounded-[8px] bg-[#cbcbcb] [&_[data-slot=progress-indicator]]:rounded-[8px] [&_[data-slot=progress-indicator]]:bg-[#14a74a]"
      />
    </div>
  )
}

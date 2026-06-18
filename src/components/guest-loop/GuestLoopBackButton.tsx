import { ChevronLeftIcon } from "lucide-react"

import { cn } from "@/lib/utils"

type GuestLoopBackButtonProps = {
  onClick: () => void
  disabled?: boolean
  className?: string
}

export function GuestLoopBackButton({
  onClick,
  disabled = false,
  className,
}: GuestLoopBackButtonProps) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onClick}
      className={cn(
        "flex items-center gap-3 self-start rounded-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30",
        disabled
          ? "cursor-not-allowed text-[#7d7d7d] opacity-60"
          : "text-[#232323] hover:text-[#141414]",
        className
      )}
    >
      <span
        className={cn(
          "flex size-9 shrink-0 items-center justify-center rounded-full bg-[#f4f4f4] hover:cursor-pointer",
          disabled && "hover:cursor-none"
        )}
      >
        <ChevronLeftIcon className="size-4" aria-hidden />
      </span>
      <span className="text-lg font-medium leading-6 tracking-[-0.36px]">
        Back
      </span>
    </button>
  )
}

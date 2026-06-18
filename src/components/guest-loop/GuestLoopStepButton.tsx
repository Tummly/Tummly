import type { ReactNode } from "react"

import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

type GuestLoopStepButtonProps = {
  children: ReactNode
  enabled: boolean
  disabled?: boolean
  isSubmitting?: boolean
  onClick: () => void | Promise<void>
  type?: "button" | "submit"
}

const guestLoopStepButtonLayoutClassName =
  "h-[50px] min-h-[50px] w-full rounded-[54px] border-0 text-base font-medium leading-normal shadow-none disabled:opacity-100"

const guestLoopStepButtonDisabledClassName =
  "bg-[#e0e0e0] text-[#7d7d7d] hover:bg-[#e0e0e0]"

export function GuestLoopStepButton({
  children,
  enabled,
  disabled,
  isSubmitting = false,
  onClick,
  type = "button",
}: GuestLoopStepButtonProps) {
  const isDisabled = disabled ?? (!enabled || isSubmitting)
  const isActive = enabled && !isSubmitting

  return (
    <Button
      type={type}
      variant={isActive ? "default" : "muted"}
      disabled={isDisabled}
      onClick={() => void onClick()}
      className={cn(
        guestLoopStepButtonLayoutClassName,
        !isActive && guestLoopStepButtonDisabledClassName
      )}
    >
      {isSubmitting ? "Please wait..." : children}
    </Button>
  )
}

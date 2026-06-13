import * as React from "react"
import { OTPInput, OTPInputContext } from "input-otp"

import { cn } from "@/lib/utils"

function InputOTP({
  className,
  containerClassName,
  ...props
}: React.ComponentProps<typeof OTPInput> & {
  containerClassName?: string
}) {
  return (
    <OTPInput
      data-slot="input-otp"
      containerClassName={cn(
        "flex w-full items-center has-disabled:opacity-50",
        containerClassName
      )}
      spellCheck={false}
      className={cn("disabled:cursor-not-allowed", className)}
      {...props}
    />
  )
}

function InputOTPGroup({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="input-otp-group"
      className={cn("flex w-full items-start gap-2", className)}
      {...props}
    />
  )
}

function InputOTPSlot({
  index,
  className,
  ...props
}: React.ComponentProps<"div"> & {
  index: number
}) {
  const inputOTPContext = React.useContext(OTPInputContext)
  const { char, hasFakeCaret, isActive } = inputOTPContext?.slots[index] ?? {}

  return (
    <div
      data-slot="input-otp-slot"
      data-active={isActive}
      className={cn(
        "relative flex h-[74px] min-w-0 flex-1 items-center justify-center rounded-[4px] border border-[rgba(74,74,76,0.4)] bg-white text-xl font-medium text-[#232323] transition-all outline-none",
        "data-[active=true]:z-10 data-[active=true]:border-[#232323] data-[active=true]:ring-2 data-[active=true]:ring-[#232323]/15",
        "aria-invalid:border-destructive aria-invalid:data-[active=true]:border-destructive aria-invalid:data-[active=true]:ring-destructive/20",
        className
      )}
      {...props}
    >
      {char}
      {hasFakeCaret ? (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center">
          <div className="h-6 w-px animate-caret-blink bg-[#232323] duration-1000" />
        </div>
      ) : null}
    </div>
  )
}

export { InputOTP, InputOTPGroup, InputOTPSlot }

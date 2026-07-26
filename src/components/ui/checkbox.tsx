import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { Checkbox as CheckboxPrimitive } from "radix-ui"
import { CheckIcon } from "lucide-react"

import { cn } from "@/lib/utils"

const checkboxVariants = cva(
  "peer relative flex shrink-0 cursor-pointer items-center justify-center border transition-colors outline-none group-has-disabled/field:opacity-50 after:absolute after:-inset-x-3 after:-inset-y-2 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 aria-invalid:aria-checked:border-primary dark:aria-invalid:border-destructive/50 dark:aria-invalid:ring-destructive/40",
  {
    variants: {
      variant: {
        default:
          "size-4 rounded-[4px] border-input dark:bg-input/30 data-checked:border-primary data-checked:bg-primary data-checked:text-primary-foreground dark:data-checked:bg-primary",
        ghost:
          "size-[18px] rounded-[2px] border-[rgba(74,74,76,0.3)] bg-transparent text-white data-checked:border-[rgba(74,74,76,0.3)] data-checked:bg-transparent data-checked:text-white dark:border-[rgba(74,74,76,0.3)] dark:bg-transparent dark:data-checked:border-[rgba(74,74,76,0.3)] dark:data-checked:bg-transparent",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)

function GhostCheckIcon({ className }: { className?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width={12}
      height={10}
      viewBox="0 0 12 10"
      fill="none"
      aria-hidden
      className={className}
    >
      <path
        d="M4 9.4L0 5.4L1.4 4L4 6.6L10.6 0L12 1.4L4 9.4Z"
        fill="currentColor"
        fillOpacity={0.3}
      />
    </svg>
  )
}

function Checkbox({
  className,
  variant = "default",
  ...props
}: React.ComponentProps<typeof CheckboxPrimitive.Root> &
  VariantProps<typeof checkboxVariants>) {
  return (
    <CheckboxPrimitive.Root
      data-slot="checkbox"
      className={cn(checkboxVariants({ variant }), className)}
      {...props}
    >
      <CheckboxPrimitive.Indicator
        data-slot="checkbox-indicator"
        className={cn(
          "grid place-content-center text-current transition-none",
          variant === "ghost" ? "[&>svg]:size-auto" : "[&>svg]:size-3.5"
        )}
      >
        {variant === "ghost" ? <GhostCheckIcon /> : <CheckIcon />}
      </CheckboxPrimitive.Indicator>
    </CheckboxPrimitive.Root>
  )
}

export { Checkbox, checkboxVariants }

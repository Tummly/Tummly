import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { Slot } from "radix-ui"

import { cn } from "@/lib/utils"

const badgeVariants = cva(
  "inline-flex w-fit shrink-0 items-center justify-center whitespace-nowrap",
  {
    variants: {
      variant: {
        default:
          "rounded-full bg-primary px-2 py-0.5 text-xs font-semibold text-primary-foreground",
        ready:
          "rounded-full bg-[#d2efdc] px-2 py-0.5 text-xs font-semibold text-[#14a247]",
        error:
          "rounded-full bg-[#fae0e0] px-2 py-0.5 text-xs font-semibold text-[#f1292d]",
        secondary:
          "rounded-full bg-secondary px-2 py-0.5 text-xs font-semibold text-secondary-foreground",
        outline:
          "rounded-full border border-border px-2 py-0.5 text-xs font-semibold text-foreground",
        /** Operator feedback sentiment — Figma chips */
        positive:
          "rounded-[4px] bg-[#e7f7ec] px-1.5 py-1 text-xs font-medium text-primary",
        neutral:
          "rounded-[4px] bg-[#fff4e6] px-1.5 py-1 text-xs font-medium text-[#f99810]",
        negative:
          "rounded-[4px] bg-[#ffeeec] px-1.5 py-1 text-xs font-medium text-[#da4231]",
        /** Soft gray chip (detected tags) */
        tag: "rounded-[4px] bg-[#f4f4f4] px-1.5 py-1 text-xs font-medium text-foreground dark:bg-white/10",
        /** Muted status chip (e.g. New) */
        soft: "rounded bg-[#e4e4e4] px-1.5 py-1 text-xs font-medium text-foreground dark:bg-white/15",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)

function Badge({
  className,
  variant = "default",
  asChild = false,
  ...props
}: React.ComponentProps<"span"> &
  VariantProps<typeof badgeVariants> & { asChild?: boolean }) {
  const Comp = asChild ? Slot.Root : "span"

  return (
    <Comp
      data-slot="badge"
      data-variant={variant}
      className={cn(badgeVariants({ variant }), className)}
      {...props}
    />
  )
}

export { Badge, badgeVariants }

import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { Slot } from "radix-ui"

import { cn } from "@/lib/utils"

const operatorChipBase =
  "rounded-[2px] px-1.5 py-1 text-xs font-medium"

/** Default operator chip — Figma Tag Default (3360:65888 light, 3360:56178 dark). */
const operatorChipDefault =
  `${operatorChipBase} bg-[rgba(57,57,57,0.2)] text-foreground dark:text-white`

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
        /** Operator feedback sentiment — Figma Tag variants */
        positive:
          "rounded-[4px] bg-[#e7f7ec] px-1.5 py-1 text-xs font-medium text-primary dark:rounded-[2px] dark:text-[#0b973a] dark:[background-image:linear-gradient(rgba(11,151,58,0.16),rgba(11,151,58,0.16)),linear-gradient(rgba(57,57,57,0.2),rgba(57,57,57,0.2))]",
        neutral: `${operatorChipBase} bg-[rgba(249,152,16,0.16)] text-[#f99810]`,
        negative: `${operatorChipBase} bg-[rgba(218,66,49,0.16)] text-[#da4231]`,
        /** Detected tags — Default chip skin */
        tag: operatorChipDefault,
        /** Muted status chip (e.g. New) — Default chip skin */
        soft: operatorChipDefault,
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

import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { Slot } from "radix-ui"

import { cn } from "@/lib/utils"

const buttonVariants = cva(
  "group/button inline-flex shrink-0 cursor-pointer items-center justify-center rounded-full border border-transparent bg-clip-padding font-medium whitespace-nowrap no-underline transition-all outline-none select-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 active:not-aria-[haspopup]:translate-y-px disabled:pointer-events-none disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 dark:aria-invalid:border-destructive/50 dark:aria-invalid:ring-destructive/40 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
  {
    variants: {
      variant: {
        default:
          "bg-primary text-white hover:bg-primary/90",
        secondary:
          "bg-brand-dark text-white hover:bg-brand-dark/90 aria-expanded:bg-brand-dark aria-expanded:text-white",
        outline:
          "border-border bg-background text-foreground hover:bg-muted hover:text-foreground aria-expanded:bg-muted aria-expanded:text-foreground dark:border-input dark:bg-input/30 dark:hover:bg-input/50",
        "outline-inverse":
          "border-white/15 bg-transparent text-[#aaa] hover:border-white/40 hover:bg-white/10 hover:text-white",
        ghost:
          "hover:bg-muted hover:text-foreground aria-expanded:bg-muted aria-expanded:text-foreground dark:hover:bg-muted/50",
        destructive:
          "bg-destructive/10 text-destructive hover:bg-destructive/20 focus-visible:border-destructive/40 focus-visible:ring-destructive/20 dark:bg-destructive/20 dark:hover:bg-destructive/30 dark:focus-visible:ring-destructive/40",
        "destructive-solid":
          "bg-destructive text-white hover:bg-destructive/90",
        "destructive-soft":
          "border border-red-300 bg-red-100 text-red-600 hover:bg-red-100/90",
        link: "h-auto min-h-0 p-0 text-primary underline-offset-4 hover:underline",
        "link-destructive":
          "h-auto min-h-0 p-0 font-semibold text-destructive underline-offset-4 hover:underline",
        cta: "bg-white text-foreground shadow-[0_4px_15px_rgba(0,0,0,0.4)] hover:bg-white/95",
        accordion:
          "h-auto min-h-0 w-full justify-between gap-6 rounded-none px-0 py-3.5 text-left font-semibold hover:bg-transparent [&>span:first-child]:text-lg [&>span:first-child]:leading-snug [&>span:first-child]:text-foreground group-hover/faq:[&>span:first-child]:text-[#4f46e5]",
        "section-toggle":
          "h-auto min-h-0 w-full justify-between rounded-none px-0 font-semibold hover:bg-transparent",
        warning: "bg-amber-500 text-white hover:bg-amber-500/90",
        info: "bg-blue-500 text-white hover:bg-blue-500/90",
        muted:
          "bg-muted text-muted-foreground hover:bg-muted/90",
        "input-toggle":
          "absolute right-[18px] top-1/2 !size-7 -translate-y-1/2 border-0 bg-transparent text-muted-foreground shadow-none hover:bg-transparent hover:text-foreground",
      },
      size: {
        default:
          "h-[38px] min-h-[38px] gap-1.5 px-[17px] text-base leading-5",
        block:
          "h-[38px] min-h-[38px] w-full gap-1.5 px-[17px] text-base leading-5",
        responsive:
          "h-[38px] min-h-[38px] w-full gap-1.5 px-[17px] text-base leading-5 sm:w-auto",
        xs: "h-6 min-h-6 gap-1 px-2 text-xs has-data-[icon=inline-end]:pr-1.5 has-data-[icon=inline-start]:pl-1.5 [&_svg:not([class*='size-'])]:size-3",
        sm: "h-7 min-h-7 gap-1 px-2.5 text-sm has-data-[icon=inline-end]:pr-1.5 has-data-[icon=inline-start]:pl-1.5 [&_svg:not([class*='size-'])]:size-3.5",
        lg: "h-[38px] min-h-[38px] gap-1.5 px-[17px] text-base leading-5",
        icon: "size-[38px]",
        "icon-xs": "size-6 [&_svg:not([class*='size-'])]:size-3",
        "icon-sm": "size-7 [&_svg:not([class*='size-'])]:size-3.5",
        "icon-lg": "size-[38px]",
        "auth-xl":
          "h-[62px] min-h-[62px] w-full text-lg font-medium",
        "auth-lg":
          "h-[58px] min-h-[58px] w-full text-[17px]",
        "auth-md":
          "h-[56px] min-h-[56px] w-full text-base font-semibold",
        "auth-sm":
          "h-[46px] min-h-[46px] w-full text-sm font-medium",
        "form-row": "h-[58px] min-h-[58px] flex-1",
        "form-continue":
          "h-[62px] min-h-[62px] w-full text-base font-semibold sm:h-[64px]",
        toolbar: "h-[54px] px-[26px] text-[15px] font-semibold",
        "toolbar-flex": "h-[54px] flex-1 text-[15px] font-semibold",
        "form-action": "mb-5 h-12 min-h-12 w-full text-[15px] font-semibold",
        "form-action-lg": "mb-9 h-12 min-h-12 w-full text-[15px] font-semibold",
        "link-block": "h-auto min-h-0 w-full text-base font-medium",
        "link-sm": "h-auto min-h-0 p-0 text-sm",
      },
    },
    compoundVariants: [
      {
        variant: ["accordion", "section-toggle", "link", "link-destructive", "input-toggle"],
        class: "!h-auto !min-h-0",
      },
      {
        variant: "default",
        size: "auth-md",
        class: "shadow-[0_10px_25px_rgba(8,181,106,0.20)]",
      },
      {
        variant: "outline",
        size: "auth-md",
        class: "text-[15px] font-medium shadow-[0_2px_12px_rgba(0,0,0,0.04)]",
      },
      {
        variant: "default",
        size: "auth-xl",
        class: "font-semibold shadow-[0_12px_30px_rgba(22,163,74,0.18)]",
      },
      {
        variant: "secondary",
        size: "auth-sm",
        class: "font-semibold",
      },
    ],
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

function Button({
  className,
  variant = "default",
  size = "default",
  asChild = false,
  ...props
}: React.ComponentProps<"button"> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean
  }) {
  const Comp = asChild ? Slot.Root : "button"

  return (
    <Comp
      data-slot="button"
      data-variant={variant}
      data-size={size}
      className={cn(buttonVariants({ variant, size }), className)}
      {...props}
    />
  )
}

export { Button, buttonVariants }

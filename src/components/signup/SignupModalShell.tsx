import type { ReactNode } from "react"
import { Link } from "react-router-dom"

import signupModalBackdrop from "@/assets/images/signup-modal-backdrop.png"
import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { cn } from "@/lib/utils"

type SignupModalShellProps = {
  children: ReactNode
  className?: string
  cardClassName?: string
}

/**
 * Centered signup / verify card over a blurred Guest Loop backdrop.
 * Not AuthShell — marketing modal chrome only.
 */
export function SignupModalShell({
  children,
  className,
  cardClassName,
}: SignupModalShellProps) {
  return (
    <div
      className={cn(
        "relative flex min-h-dvh w-full items-center justify-center overflow-auto p-4 sm:p-6",
        className
      )}
    >
      <div aria-hidden className="pointer-events-none absolute inset-0">
        <img
          src={signupModalBackdrop}
          alt=""
          className="absolute inset-0 size-full object-cover"
        />
        <div className="absolute inset-0 bg-[rgba(20,20,20,0.38)] backdrop-blur-[7px]" />
      </div>

      <div
        className={cn(
          "relative z-10 flex w-full max-w-[582px] flex-col items-center gap-10 overflow-clip rounded-[10px] bg-white px-6 py-10 shadow-[0_8px_40px_rgba(0,0,0,0.18)] sm:px-[50px]",
          cardClassName
        )}
      >
        <Link
          to="/"
          className="inline-flex shrink-0 rounded-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30"
        >
          <MarketingLogo
            onLight
            width={145}
            height={37}
            className="h-[37px] w-auto"
          />
        </Link>

        <div className="flex w-full flex-col items-center">{children}</div>
      </div>
    </div>
  )
}

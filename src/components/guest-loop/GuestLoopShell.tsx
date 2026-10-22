import type { ReactNode } from "react"
import { Link } from "react-router-dom"

import { AuthFooter } from "@/components/auth/AuthFooter"
import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import { cn } from "@/lib/utils"

import { GuestLoopBackButton } from "./GuestLoopBackButton"

interface GuestLoopShellProps {
  children: ReactNode
  className?: string
  /** Vertical alignment of main content within the shell. */
  contentAlign?: "center" | "start"
  /** Override the default 473px content max width (e.g. provisioning). */
  contentMaxWidthClassName?: string
  /** When false, the back control is hidden (e.g. Account Setup step 1). */
  showBackButton?: boolean
  /** When true, back is visible but not actionable (e.g. Guest Loop provisioning in progress). */
  backButtonDisabled?: boolean
  onBack?: () => void
}

/**
 * Full-viewport Guest Loop chrome — grey pad, #fafafa rounded panel,
 * logo + Contact support header, AuthFooter outside the panel.
 */
export function GuestLoopShell({
  children,
  className,
  contentAlign = "center",
  contentMaxWidthClassName = "max-w-[473px]",
  showBackButton = false,
  backButtonDisabled = false,
  onBack,
}: GuestLoopShellProps) {
  return (
    <div
      className={cn(
        "flex h-dvh min-h-0 flex-col overflow-hidden bg-[#cbcbcb] p-5 text-[#232323]",
        className
      )}
    >
      <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-xl bg-[#fafafa]">
        <header className="flex shrink-0 items-center justify-between px-10 pt-10">
          <Link
            to="/"
            className="rounded-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30"
          >
            <MarketingLogo
              onLight
              width={145}
              height={37}
              className="h-9.25 w-auto"
            />
          </Link>
          <p className="m-0 flex items-center gap-2.5 text-base text-[#141414]">
            Having trouble?{" "}
            <Link
              to={HELP_CENTRE_CONTACT_URL}
              className="text-sm font-medium underline"
            >
              Contact support
            </Link>
          </p>
        </header>

        <main
          className={cn(
            "flex min-h-0 flex-1 flex-col items-center overflow-y-auto px-10 pb-10 pt-6",
            contentAlign === "start" ? "justify-start" : "justify-center"
          )}
        >
          <div
            className={cn("flex w-full flex-col", contentMaxWidthClassName)}
          >
            {showBackButton && onBack ? (
              <GuestLoopBackButton
                onClick={onBack}
                disabled={backButtonDisabled}
                className="mb-6"
              />
            ) : null}
            {children}
          </div>
        </main>
      </div>

      <AuthFooter className="px-10 pb-0 pt-3 sm:px-10 lg:px-10 lg:pb-0" />
    </div>
  )
}

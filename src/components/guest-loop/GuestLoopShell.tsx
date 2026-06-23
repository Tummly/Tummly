import type { ReactNode } from "react"

import { AuthFormAccent } from "@/components/auth/AuthFormAccent"
import Navbar from "@/components/layout/Navbar"
import { cn } from "@/lib/utils"

import { GuestLoopBackButton } from "./GuestLoopBackButton"
import { GuestLoopLegalFooter } from "./GuestLoopLegalFooter"
import { GuestLoopSupportFooter } from "./GuestLoopSupportFooter"

interface GuestLoopShellProps {
  children: ReactNode
  className?: string
  hideFooters?: boolean
  /** Vertical alignment of main content within the shell. */
  contentAlign?: "center" | "start"
  /** When false, the back control is hidden (e.g. Account Setup step 1). */
  showBackButton?: boolean
  /** When true, back is visible but not actionable (e.g. Guest Loop provisioning in progress). */
  backButtonDisabled?: boolean
  onBack?: () => void
}

/**
 * Full-viewport shell for Guest Loop onboarding (single- and multi-location).
 * Site navigation header, top-right kitchen line-art, support and legal footers.
 */
export function GuestLoopShell({
  children,
  className,
  hideFooters = false,
  contentAlign = "center",
  showBackButton = false,
  backButtonDisabled = false,
  onBack,
}: GuestLoopShellProps) {
  return (
    <div
      className={cn(
        "flex min-h-dvh flex-col overflow-x-hidden bg-white text-[#232323]",
        className
      )}
    >
      <Navbar />

      <div className="relative flex min-h-0 flex-1 flex-col">
        <AuthFormAccent />

        <div className="relative z-1 flex min-h-0 flex-1 flex-col px-6.25 lg:px-45 pb-10 pt-22.5 gap-10">
          <main
            className={cn(
              "flex flex-1 flex-col items-center",
              contentAlign === "start" ? "justify-start" : "justify-center"
            )}
          >
            {showBackButton && onBack ? (
              <GuestLoopBackButton
                onClick={onBack}
                disabled={backButtonDisabled}
              />
            ) : null}
            <div className="flex w-full max-w-[min(100%,560px)] flex-col">
              {children}
            </div>
          </main>

          {!hideFooters ? (
            <footer className="flex shrink-0 flex-col items-center gap-8">
              <GuestLoopSupportFooter />
              <GuestLoopLegalFooter />
            </footer>
          ) : null}
        </div>
      </div>
    </div>
  )
}

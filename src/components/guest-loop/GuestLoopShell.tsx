import {
  createContext,
  useContext,
  useState,
  type ReactNode,
} from "react"
import { createPortal } from "react-dom"
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
  /**
   * `content` (default): back sits above the content column.
   * `top`: back sits under the header, so it stays at the top when the
   * content is vertically centered (e.g. provisioning error).
   */
  backButtonPlacement?: "content" | "top"
  /**
   * Wide signup chrome (Figma Plans `6775:39425`, Add locations `6777:41139`):
   * full-screen panel with no grey frame, 70px gutters, divider under the
   * header, and a bordered footer bar filled via `GuestLoopShellFooter`.
   */
  wide?: boolean
  /**
   * Full-screen panel with no grey frame, rounded corners or AuthFooter, for
   * post-payment pages. Always on with `wide`.
   */
  fullBleed?: boolean
}

const GuestLoopShellFooterContext = createContext<HTMLDivElement | null>(null)

/** Renders children into the shell's full-width footer bar (wide layout). */
export function GuestLoopShellFooter({ children }: { children: ReactNode }) {
  const target = useContext(GuestLoopShellFooterContext)
  if (target == null) {
    return null
  }
  return createPortal(children, target)
}

/**
 * Full-viewport Guest Loop chrome — grey pad, #fafafa rounded panel,
 * logo + Contact support header, AuthFooter outside the panel.
 * `wide` / `fullBleed` drop the grey pad, the rounded corners and the AuthFooter.
 */
export function GuestLoopShell({
  children,
  className,
  contentAlign = "center",
  contentMaxWidthClassName = "max-w-[473px]",
  showBackButton = false,
  backButtonDisabled = false,
  onBack,
  backButtonPlacement = "content",
  wide = false,
  fullBleed = false,
}: GuestLoopShellProps) {
  const fullScreen = wide || fullBleed
  const backButton =
    showBackButton && onBack ? (
      <GuestLoopBackButton
        onClick={onBack}
        disabled={backButtonDisabled}
        className={backButtonPlacement === "content" ? "mb-6" : undefined}
      />
    ) : null
  const [footerTarget, setFooterTarget] = useState<HTMLDivElement | null>(null)

  return (
    <GuestLoopShellFooterContext.Provider value={footerTarget}>
    <div
      className={cn(
        "marketing-controls flex h-dvh min-h-0 flex-col overflow-hidden text-[#232323]",
        fullScreen ? "bg-[#fafafa]" : "bg-[#cbcbcb] p-5",
        className
      )}
    >
      <div
        className={cn(
          "flex min-h-0 flex-1 flex-col overflow-hidden bg-[#fafafa]",
          !fullScreen && "rounded-xl",
        )}
      >
        <header
          className={cn(
            "flex shrink-0 items-center justify-between px-10 pt-10",
            wide && "border-b border-[#d2d2d2] pb-5 lg:px-[70px]",
          )}
        >
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

        {backButtonPlacement === "top" && backButton != null ? (
          <div
            className={cn(
              "flex shrink-0 px-10 pt-6",
              wide && "lg:px-[70px]",
            )}
          >
            {backButton}
          </div>
        ) : null}

        <main
          className={cn(
            "scrollbar-none flex min-h-0 flex-1 flex-col items-center overflow-y-auto px-10 pb-10 pt-6",
            wide && "lg:px-[70px] lg:pt-[70px]",
            contentAlign === "start" ? "justify-start" : "justify-center"
          )}
        >
          <div
            className={cn("flex w-full flex-col", contentMaxWidthClassName)}
          >
            {backButtonPlacement === "content" ? backButton : null}
            {children}
          </div>
        </main>
        {wide ? (
          <div
            ref={setFooterTarget}
            className="shrink-0 border-t border-[#d2d2d2] px-10 py-6 empty:hidden lg:px-[70px] lg:py-10"
          />
        ) : null}
      </div>

      {fullScreen ? null : (
        <AuthFooter className="px-10 pb-0 pt-3 sm:px-10 lg:px-10 lg:pb-0" />
      )}
    </div>
    </GuestLoopShellFooterContext.Provider>
  )
}

import { CircleHelpIcon, SearchIcon } from "lucide-react"
import type { ReactNode } from "react"

import HelpCentreHubLink from "@/components/navigation/HelpCentreHubLink"
import { Button } from "@/components/ui/button"
import { HELP_CENTRE_URL } from "@/config/support"
import { OPERATOR_SHELL_TOUCH_TARGET_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"

/** Figma header search / AI / location surface (`#ebebeb` light, `#212121` dark). */
export const OPERATOR_UTILITY_SURFACE_CLASS =
  "rounded-[2px] bg-[#ebebeb] dark:bg-[#212121]"

/** Compact utility height for mobile navbar + nav sheet. */
export const OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS = "h-8 min-h-8"

/** Desktop utility height (Figma 40px) at `lg+`. */
export const OPERATOR_UTILITY_CONTROL_HEIGHT_CLASS = "h-10 min-h-10"

export function OperatorShellDisabledChromeButton({
  label,
  children,
  className,
}: {
  label: string
  children: ReactNode
  className?: string
}) {
  return (
    <Button
      type="button"
      variant="ghost"
      disabled
      aria-disabled="true"
      aria-label={`${label} (unavailable)`}
      title={`${label} is unavailable`}
      className={cn("shrink-0 text-foreground opacity-50", className)}
    >
      {children}
    </Button>
  )
}

/** Disabled navbar / sheet search field (deferred product chrome). */
export function OperatorShellDisabledSearchField({
  className,
  compact = false,
}: {
  className?: string
  /** Tighter sizing for mobile navbar sheet header. */
  compact?: boolean
}) {
  return (
    <div
      role="search"
      aria-disabled="true"
      aria-label="Search (unavailable)"
      title="Search is unavailable"
      className={cn(
        "flex min-w-0 items-center gap-2 px-2.5 text-xs text-[#707070] lg:gap-3 lg:px-3.5 lg:text-sm",
        compact
          ? OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS
          : cn(
              OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS,
              "lg:h-10 lg:min-h-10"
            ),
        OPERATOR_UTILITY_SURFACE_CLASS,
        className
      )}
    >
      <SearchIcon className="size-3.5 shrink-0 lg:size-4" aria-hidden />
      <span className="truncate">
        Search guests, feedback, offers and campaigns…
      </span>
    </div>
  )
}

export function OperatorShellHelpLink({
  showLabel = true,
  className,
}: {
  showLabel?: boolean
  className?: string
}) {
  return (
    <HelpCentreHubLink
      to={HELP_CENTRE_URL}
      className={cn(
        "inline-flex shrink-0 items-center justify-center gap-1 rounded-[2px]",
        "text-sm font-medium text-foreground no-underline",
        "hover:bg-black/5 hover:text-foreground",
        "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
        "dark:hover:bg-white/10",
        OPERATOR_SHELL_TOUCH_TARGET_CLASS,
        showLabel && "lg:h-10 lg:min-h-10 lg:w-auto lg:min-w-0 lg:gap-1.5 lg:px-3",
        className
      )}
      aria-label="Help Centre"
    >
      <CircleHelpIcon className="size-4 shrink-0" aria-hidden />
      {showLabel ? <span className="hidden lg:inline">Help</span> : null}
    </HelpCentreHubLink>
  )
}

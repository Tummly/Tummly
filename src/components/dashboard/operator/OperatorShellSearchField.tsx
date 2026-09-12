import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { Kbd } from "@/components/ui/kbd"
import {
  OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS,
  OPERATOR_UTILITY_SURFACE_CLASS,
} from "@/components/dashboard/operator/ShellUtilityChrome"
import {
  GLOBAL_SEARCH_KBD_CLASS,
  GLOBAL_SEARCH_TRIGGER_CLASS,
  GLOBAL_SEARCH_TRIGGER_PLACEHOLDER,
} from "@/lib/operatorGlobalSearch/globalSearchPresentation"
import { cn } from "@/lib/utils"

type OperatorShellSearchFieldProps = {
  className?: string
  /** Tighter sizing for mobile navbar sheet header. */
  compact?: boolean
  shortcutModifierLabel: string
  onOpen: () => void
}

/** Live Global Search trigger — navbar and mobile nav sheet. */
export function OperatorShellSearchField({
  className,
  compact = false,
  shortcutModifierLabel,
  onOpen,
}: OperatorShellSearchFieldProps) {
  return (
    <button
      type="button"
      role="search"
      aria-label="Search"
      title="Open Global Search"
      className={cn(
        GLOBAL_SEARCH_TRIGGER_CLASS,
        compact
          ? OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS
          : cn(
              OPERATOR_UTILITY_CONTROL_HEIGHT_COMPACT_CLASS,
              "lg:h-10 lg:min-h-10"
            ),
        OPERATOR_UTILITY_SURFACE_CLASS,
        className
      )}
      onClick={onOpen}
    >
      <OperatorSearchIcon className="size-3.5 shrink-0 text-op-header-search-text lg:size-4" />
      <span className="min-w-0 flex-1 truncate">
        {GLOBAL_SEARCH_TRIGGER_PLACEHOLDER}
      </span>
      <Kbd
        className={cn(
          GLOBAL_SEARCH_KBD_CLASS,
          "hidden shrink-0 lg:inline-flex"
        )}
      >
        {shortcutModifierLabel}K
      </Kbd>
    </button>
  )
}

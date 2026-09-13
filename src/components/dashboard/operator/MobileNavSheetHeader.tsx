import { XIcon } from "lucide-react"

import { OperatorShellSearchField } from "@/components/dashboard/operator/OperatorShellSearchField"
import {
  OperatorShellDisabledSearchField,
  OperatorShellHelpLink,
} from "@/components/dashboard/operator/ShellUtilityChrome"
import { Button } from "@/components/ui/button"
import { SheetClose } from "@/components/ui/sheet"
import { OPERATOR_SHELL_TOUCH_TARGET_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"

type MobileNavSheetHeaderProps = {
  className?: string
  onOpenGlobalSearch?: () => void
  globalSearchShortcutModifierLabel?: string
}

/** Mobile nav sheet chrome — search + help live here below `lg`, not on the navbar. */
export function MobileNavSheetHeader({
  className,
  onOpenGlobalSearch,
  globalSearchShortcutModifierLabel = "Ctrl",
}: MobileNavSheetHeaderProps) {
  return (
    <div
      className={cn(
        "flex shrink-0 items-center gap-1.5 border-b border-black/10 px-2 py-2 dark:border-white/10",
        className
      )}
    >
      {onOpenGlobalSearch != null ? (
        <OperatorShellSearchField
          compact
          className="min-w-0 flex-1"
          shortcutModifierLabel={globalSearchShortcutModifierLabel}
          onOpen={onOpenGlobalSearch}
        />
      ) : (
        <OperatorShellDisabledSearchField compact className="min-w-0 flex-1" />
      )}
      <OperatorShellHelpLink showLabel={false} />
      <SheetClose asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className={cn(
            "shrink-0 text-foreground",
            OPERATOR_SHELL_TOUCH_TARGET_CLASS
          )}
          aria-label="Close navigation"
        >
          <XIcon aria-hidden />
        </Button>
      </SheetClose>
    </div>
  )
}

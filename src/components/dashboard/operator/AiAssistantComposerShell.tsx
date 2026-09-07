import type { ReactNode } from "react"

import { AiAssistantLoadingBorder } from "@/components/dashboard/operator/AiAssistantLoadingBorder"
import { assistantComposerBorderClass } from "@/lib/operatorAiAssistant/assistantCreditsPresentation"
import { cn } from "@/lib/utils"

type AiAssistantComposerShellProps = {
  focused: boolean
  /** True while an assistant turn is in flight. */
  loading: boolean
  className?: string
  /** Credits strip — behind the input (Figma 5686:21778). */
  credits: ReactNode
  /** Composer field only. */
  children: ReactNode
}

/**
 * Credits strip sits behind the input (Figma 5686:21778).
 * Strip has top radius only; field overlaps it by 5px with full radius.
 * Loading paints the moving blue → green ring on the input only.
 */
export function AiAssistantComposerShell({
  focused,
  loading,
  className,
  credits,
  children,
}: AiAssistantComposerShellProps) {
  return (
    <div className={cn("flex flex-col", className)}>
      <div className="relative z-0 shrink-0">{credits}</div>
      <AiAssistantLoadingBorder
        loading={loading}
        className={cn(
          "relative z-10 -mt-[5px] rounded-[8px]",
          !loading
            && cn(
              "border overflow-hidden transition-colors",
              assistantComposerBorderClass(focused)
            )
        )}
        contentClassName="rounded-[8px] bg-op-assistant-composer-background"
      >
        {children}
      </AiAssistantLoadingBorder>
    </div>
  )
}

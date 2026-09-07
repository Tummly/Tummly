import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

type AiAssistantCreditsBarProps = {
  remainingLine: string
  viewUsageLabel: string
  addCreditsLabel: string
  showViewUsage: boolean
  showAddCredits: boolean
  onViewUsage: () => void
  onAddCredits: () => void
  className?: string
}

/** Composer credits strip — top radius only; sits behind the input (Figma 5686:21778). */
export function AiAssistantCreditsBar({
  remainingLine,
  viewUsageLabel,
  addCreditsLabel,
  showViewUsage,
  showAddCredits,
  onViewUsage,
  onAddCredits,
  className,
}: AiAssistantCreditsBarProps) {
  return (
    <div
      className={cn(
        "flex items-center justify-between gap-3 rounded-t-[8px] rounded-b-none bg-op-assistant-credits-background px-5 py-4",
        className
      )}
    >
      <p className="min-w-0 truncate text-xs font-medium text-op-assistant-list-subtitle">
        {remainingLine}
      </p>
      <div className="flex shrink-0 items-center gap-4">
        {showViewUsage ? (
          <Button
            type="button"
            variant="op-ghost"
            className="h-auto min-h-0 p-0 text-xs font-medium text-op-assistant-list-subtitle hover:bg-transparent hover:text-op-assistant-list-title transition-colors cursor-pointer"
            onClick={onViewUsage}
          >
            {viewUsageLabel}
          </Button>
        ) : null}
        {showAddCredits ? (
          <Button
            type="button"
            variant="op-ghost"
            className="h-auto min-h-0 p-0 text-xs font-medium text-op-assistant-list-subtitle hover:bg-transparent hover:text-op-assistant-list-title transition-colors cursor-pointer"
            onClick={onAddCredits}
          >
            {addCreditsLabel}
          </Button>
        ) : null}
      </div>
    </div>
  )
}

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

/** Composer credits strip — Figma 3454:56050. Live Billing AI balances. */
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
        "flex items-center justify-between gap-3 border-b border-op-border-default bg-[#141414] px-5 py-3.5",
        className
      )}
    >
      <p className="min-w-0 truncate text-xs font-medium text-neutral-400">
        {remainingLine}
      </p>
      <div className="flex shrink-0 items-center gap-4">
        {showViewUsage ? (
          <Button
            type="button"
            variant="op-ghost"
            className="h-auto min-h-0 p-0 text-xs font-medium text-neutral-400 hover:bg-transparent hover:text-white transition-colors cursor-pointer"
            onClick={onViewUsage}
          >
            {viewUsageLabel}
          </Button>
        ) : null}
        {showAddCredits ? (
          <Button
            type="button"
            variant="op-ghost"
            className="h-auto min-h-0 p-0 text-xs font-medium text-neutral-400 hover:bg-transparent hover:text-white transition-colors cursor-pointer"
            onClick={onAddCredits}
          >
            {addCreditsLabel}
          </Button>
        ) : null}
      </div>
    </div>
  )
}

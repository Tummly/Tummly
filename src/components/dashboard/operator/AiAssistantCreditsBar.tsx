import { Button } from "@/components/ui/button"

type AiAssistantCreditsBarProps = {
  remainingLine: string
  viewUsageLabel: string
  addCreditsLabel: string
  showViewUsage: boolean
  showAddCredits: boolean
  onViewUsage: () => void
  onAddCredits: () => void
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
}: AiAssistantCreditsBarProps) {
  return (
    <div className="flex items-center justify-between gap-3 px-1 pb-2">
      <p className="min-w-0 truncate text-xs font-normal text-neutral-500">
        {remainingLine}
      </p>
      <div className="flex shrink-0 items-center gap-4">
        {showViewUsage ? (
          <Button
            type="button"
            variant="op-ghost"
            className="h-auto min-h-0 p-0 text-xs font-normal text-neutral-400 hover:bg-transparent hover:text-white transition-colors"
            onClick={onViewUsage}
          >
            {viewUsageLabel}
          </Button>
        ) : null}
        {showAddCredits ? (
          <Button
            type="button"
            variant="op-ghost"
            className="h-auto min-h-0 p-0 text-xs font-normal text-neutral-400 hover:bg-transparent hover:text-white transition-colors"
            onClick={onAddCredits}
          >
            {addCreditsLabel}
          </Button>
        ) : null}
      </div>
    </div>
  )
}

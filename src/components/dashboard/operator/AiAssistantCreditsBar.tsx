import { Button } from "@/components/ui/button"

type AiAssistantCreditsBarProps = {
  remainingLine: string
  viewUsageLabel: string
  addCreditsLabel: string
  onViewUsage: () => void
  onAddCredits: () => void
}

/** Composer credits strip — Figma 3454:56050. Stub counts; no metering. */
export function AiAssistantCreditsBar({
  remainingLine,
  viewUsageLabel,
  addCreditsLabel,
  onViewUsage,
  onAddCredits,
}: AiAssistantCreditsBarProps) {
  return (
    <div className="-mb-[5px] flex items-center justify-between gap-3 bg-op-assistant-credits-background px-5 py-4">
      <p className="min-w-0 truncate text-xs font-medium text-[var(--op-color-gray-550)]">
        {remainingLine}
      </p>
      <div className="flex shrink-0 items-center gap-3">
        <Button
          type="button"
          variant="op-ghost"
          className="h-auto min-h-0 px-0 py-0 text-xs font-medium text-op-text-primary hover:bg-transparent hover:text-op-text-primary"
          onClick={onViewUsage}
        >
          {viewUsageLabel}
        </Button>
        <Button
          type="button"
          variant="op-ghost"
          className="h-auto min-h-0 px-0 py-0 text-xs font-medium text-op-text-primary hover:bg-transparent hover:text-op-text-primary"
          onClick={onAddCredits}
        >
          {addCreditsLabel}
        </Button>
      </div>
    </div>
  )
}

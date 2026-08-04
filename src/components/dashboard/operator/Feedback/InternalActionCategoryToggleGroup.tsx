import { Button } from "@/components/ui/button"
import {
  INTERNAL_ACTION_CATEGORY_OPTIONS,
  type InternalActionCategoryId,
} from "@/lib/operatorFeedback/internalActionPresentation"
import { cn } from "@/lib/utils"

type InternalActionCategoryToggleGroupProps = {
  value: InternalActionCategoryId | null
  onValueChange: (category: InternalActionCategoryId) => void
  disabled?: boolean
  className?: string
}

/** Single-select category picker for internal-action recorder steps. */
export function InternalActionCategoryToggleGroup({
  value,
  onValueChange,
  disabled = false,
  className,
}: InternalActionCategoryToggleGroupProps) {
  return (
    <div
      className={cn("flex w-full flex-col gap-[18px]", className)}
      role="radiogroup"
      aria-label="Category"
    >
      {INTERNAL_ACTION_CATEGORY_OPTIONS.map((option) => {
        const selected = value === option.id
        return (
          <Button
            key={option.id}
            type="button"
            variant="ghost"
            role="radio"
            aria-checked={selected}
            disabled={disabled}
            className={cn(
              "h-auto w-full flex-col items-start justify-start gap-1 rounded-[4px] border px-[18px] py-4 text-left whitespace-normal hover:bg-transparent",
              selected
                ? "border-[var(--op-color-gray-550)] bg-[var(--op-color-gray-990)]"
                : "border-op-card-border bg-[var(--op-color-gray-990)] hover:border-[var(--op-color-gray-550)]"
            )}
            onClick={() => {
              if (selected) {
                return
              }
              onValueChange(option.id)
            }}
          >
            <span className="text-sm font-medium text-op-text-primary">
              {option.label}
            </span>
            <span className="text-xs font-medium text-[var(--op-color-gray-550)]">
              {option.description}
            </span>
          </Button>
        )
      })}
    </div>
  )
}

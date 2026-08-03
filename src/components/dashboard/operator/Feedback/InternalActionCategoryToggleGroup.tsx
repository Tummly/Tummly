import {
  ToggleGroup,
  ToggleGroupItem,
} from "@/components/ui/toggle-group"
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
    <ToggleGroup
      type="single"
      value={value ?? undefined}
      onValueChange={(next) => {
        if (
          next === ""
          || !INTERNAL_ACTION_CATEGORY_OPTIONS.some((option) => option.id === next)
        ) {
          return
        }
        onValueChange(next as InternalActionCategoryId)
      }}
      disabled={disabled}
      spacing={0}
      aria-label="Category"
      className={cn(
        "grid w-full grid-cols-1 gap-2 rounded-none sm:grid-cols-2",
        className
      )}
    >
      {INTERNAL_ACTION_CATEGORY_OPTIONS.map((option) => (
        <ToggleGroupItem
          key={option.id}
          value={option.id}
          variant="outline"
          className={cn(
            "h-auto min-w-0 justify-start rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)] px-4 py-3 text-left text-sm font-medium whitespace-normal text-op-text-primary shadow-none",
            "hover:bg-[var(--op-color-gray-990)] hover:text-op-text-primary",
            "data-[state=on]:border-[var(--op-color-gray-500)] data-[state=on]:bg-[var(--op-color-gray-990)] data-[state=on]:text-op-text-primary data-[state=on]:ring-1 data-[state=on]:ring-[var(--op-color-gray-500)]"
          )}
        >
          {option.label}
        </ToggleGroupItem>
      ))}
    </ToggleGroup>
  )
}

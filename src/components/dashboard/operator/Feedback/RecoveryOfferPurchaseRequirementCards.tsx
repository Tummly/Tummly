import {
  ToggleGroup,
  ToggleGroupItem,
} from "@/components/ui/toggle-group"
import {
  RECOVERY_OFFER_PURCHASE_REQUIREMENT_OPTIONS,
  type RecoveryOfferPurchaseRequirementId,
} from "@/lib/operatorFeedback/recoveryOfferPresentation"
import { cn } from "@/lib/utils"

type RecoveryOfferPurchaseRequirementCardsProps = {
  value: RecoveryOfferPurchaseRequirementId | null
  onValueChange: (value: RecoveryOfferPurchaseRequirementId) => void
  disabled?: boolean
  className?: string
}

/** Free-item purchase requirement as selectable cards (U-08). */
export function RecoveryOfferPurchaseRequirementCards({
  value,
  onValueChange,
  disabled = false,
  className,
}: RecoveryOfferPurchaseRequirementCardsProps) {
  return (
    <div className={cn("flex flex-col gap-2", className)}>
      <p className="text-sm font-semibold leading-5 text-op-text-primary">
        Purchase requirement
      </p>
      <ToggleGroup
        type="single"
        value={value ?? undefined}
        onValueChange={(next) => {
          if (
            next === ""
            || !RECOVERY_OFFER_PURCHASE_REQUIREMENT_OPTIONS.some(
              (option) => option.id === next
            )
          ) {
            return
          }
          onValueChange(next as RecoveryOfferPurchaseRequirementId)
        }}
        disabled={disabled}
        spacing={0}
        aria-label="Purchase requirement"
        className="grid w-full grid-cols-1 gap-[18px] rounded-none"
      >
        {RECOVERY_OFFER_PURCHASE_REQUIREMENT_OPTIONS.map((option) => (
          <ToggleGroupItem
            key={option.id}
            value={option.id}
            variant="outline"
            className={cn(
              "h-auto min-w-0 items-start justify-start rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)] px-[18px] py-4 text-left whitespace-normal shadow-none",
              "hover:bg-[var(--op-color-gray-985)] hover:text-op-text-primary",
              "data-[state=on]:border-[var(--op-color-gray-550)] data-[state=on]:bg-[var(--op-color-gray-990)] data-[state=on]:text-op-text-primary data-[state=on]:ring-1 data-[state=on]:ring-[var(--op-color-gray-550)]"
            )}
          >
            <span className="text-sm font-medium text-op-text-primary">
              {option.label}
            </span>
          </ToggleGroupItem>
        ))}
      </ToggleGroup>
    </div>
  )
}

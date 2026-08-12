import type { LucideIcon } from "lucide-react"
import {
  BanknoteIcon,
  RefreshCwIcon,
  TagIcon,
  TicketPercentIcon,
} from "lucide-react"
import type { ReactNode } from "react"

import { Button } from "@/components/ui/button"
import {
  RECOVERY_OFFER_TYPE_OPTIONS,
  type RecoveryOfferTypeId,
} from "@/lib/operatorFeedback/recoveryOfferPresentation"
import { cn } from "@/lib/utils"

const OFFER_TYPE_ICONS: Record<RecoveryOfferTypeId, LucideIcon> = {
  percentage_discount: TicketPercentIcon,
  fixed_discount: BanknoteIcon,
  free_item: TagIcon,
  replacement_item: RefreshCwIcon,
}

type RecoveryOfferTypeCardsProps = {
  value: RecoveryOfferTypeId | null
  onValueChange: (offerType: RecoveryOfferTypeId) => void
  disabled?: boolean
  /** Type-specific fields rendered under the selected card (Figma intercalation). */
  renderSelectedFields?: (offerType: RecoveryOfferTypeId) => ReactNode
}

/** Figma Offer details — selectable offer-type cards (U-08). */
export function RecoveryOfferTypeCards({
  value,
  onValueChange,
  disabled = false,
  renderSelectedFields,
}: RecoveryOfferTypeCardsProps) {
  return (
    <div
      className="flex w-full flex-col gap-[18px]"
      role="radiogroup"
      aria-label="Offer type"
    >
      {RECOVERY_OFFER_TYPE_OPTIONS.map((option) => {
        const selected = value === option.id
        const Icon = OFFER_TYPE_ICONS[option.id]
        return (
          <div key={option.id} className="flex flex-col gap-[18px]">
            <Button
              type="button"
              variant="ghost"
              role="radio"
              aria-checked={selected}
              disabled={disabled}
              className={cn(
                /* Transparent fill — Figma cards match drawer (#171717 / #202020), not a darker surface. */
                "h-auto w-full items-center justify-start gap-2.5 rounded-[4px] border bg-transparent px-[18px] py-4 text-left whitespace-normal hover:bg-transparent",
                selected
                  ? "border-op-text-muted"
                  : "border-op-card-border hover:border-op-text-muted"
              )}
              onClick={() => {
                onValueChange(option.id)
              }}
            >
              <span className="flex size-9 shrink-0 items-center justify-center rounded-[2px] bg-op-background-secondary p-2.5">
                <Icon className="size-4 text-op-text-primary" aria-hidden />
              </span>
              <span className="flex min-w-0 flex-col gap-1">
                <span className="text-sm font-medium text-op-text-primary">
                  {option.label}
                </span>
                <span className="text-xs font-medium text-op-text-muted">
                  {option.description}
                </span>
              </span>
            </Button>
            {selected && renderSelectedFields != null
              ? renderSelectedFields(option.id)
              : null}
          </div>
        )
      })}
    </div>
  )
}

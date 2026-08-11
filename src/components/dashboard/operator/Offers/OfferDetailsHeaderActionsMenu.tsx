import { MoreVerticalIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { GUESTS_ROW_ACTIONS_MENU_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import type {
  OfferDetailsHeaderActionId,
  OfferDetailsHeaderMenuItem,
} from "@/lib/operatorOffers/offerDetailsPresentation"

type OfferDetailsHeaderActionsMenuProps = {
  ariaLabel: string
  items: readonly OfferDetailsHeaderMenuItem[]
  onAction: (actionId: OfferDetailsHeaderActionId) => void
}

export function OfferDetailsHeaderActionsMenu({
  ariaLabel,
  items,
  onAction,
}: OfferDetailsHeaderActionsMenuProps) {
  if (items.length === 0) {
    return null
  }

  return (
    <DropdownMenu modal={false}>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="op-collapse"
          size="icon"
          aria-label={ariaLabel}
        >
          <MoreVerticalIcon className="size-4" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className={GUESTS_ROW_ACTIONS_MENU_CLASS}>
        {items.map((item) => (
          <DropdownMenuItem
            key={`${item.id}-${item.label}`}
            onSelect={() => {
              onAction(item.id)
            }}
          >
            {item.label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

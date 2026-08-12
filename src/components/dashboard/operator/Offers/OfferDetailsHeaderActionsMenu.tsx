import { Fragment } from "react"
import { MoreVerticalIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_SEPARATOR_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type {
  OfferDetailsHeaderActionId,
  OfferDetailsHeaderMenuItem,
} from "@/lib/operatorOffers/offerDetailsPresentation"

type OfferDetailsHeaderActionsMenuProps = {
  ariaLabel: string
  items: readonly OfferDetailsHeaderMenuItem[]
  onAction: (actionId: OfferDetailsHeaderActionId) => void
}

/** Offer Details header ⋮ — same Actions chrome as Guest Profile / Campaigns. */
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
          variant="ghost"
          size="icon"
          aria-label={ariaLabel}
          className={GUESTS_ROW_ACTIONS_TRIGGER_CLASS}
        >
          <MoreVerticalIcon className="size-6" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className={GUESTS_ROW_ACTIONS_MENU_CLASS}>
        {items.map((item, index) => (
          <Fragment key={`${item.id}-${item.label}`}>
            {index > 0 ? (
              <DropdownMenuSeparator
                className={GUESTS_ROW_ACTIONS_SEPARATOR_CLASS}
              />
            ) : null}
            <DropdownMenuItem
              className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
              onSelect={() => {
                onAction(item.id)
              }}
            >
              {item.label}
            </DropdownMenuItem>
          </Fragment>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

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
  isVisibleOfferDetailsRowAction,
  type OfferDetailsLifecycleRowAction,
} from "@/lib/operatorOffers/offerDetailsPresentation"
import {
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_SEPARATOR_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type OfferDetailsRowActionsMenuProps<TId extends string> = {
  ariaLabel: string
  actions: readonly OfferDetailsLifecycleRowAction<TId>[]
  onAction: (actionId: TId) => void
}

/** Lifecycle table row ⋮ — gated flags live on the action view-model. */
export function OfferDetailsRowActionsMenu<TId extends string>({
  ariaLabel,
  actions,
  onAction,
}: OfferDetailsRowActionsMenuProps<TId>) {
  const visibleActions = actions.filter(isVisibleOfferDetailsRowAction)

  if (visibleActions.length === 0) {
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
          <MoreVerticalIcon className="size-4" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className={GUESTS_ROW_ACTIONS_MENU_CLASS}>
        {visibleActions.map((action, index) => (
          <Fragment key={action.id}>
            {index > 0 ? (
              <DropdownMenuSeparator
                className={GUESTS_ROW_ACTIONS_SEPARATOR_CLASS}
              />
            ) : null}
            <DropdownMenuItem
              className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
              onClick={() => {
                onAction(action.id)
              }}
            >
              {action.label}
            </DropdownMenuItem>
          </Fragment>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

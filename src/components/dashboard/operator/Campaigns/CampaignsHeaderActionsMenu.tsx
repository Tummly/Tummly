import { Fragment } from "react"
import { Link } from "react-router-dom"
import { MoreVerticalIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { CAMPAIGNS_HEADER_OVERFLOW_ACTIONS } from "@/lib/operatorCampaigns/campaignsPresentation"
import {
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_SEPARATOR_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type CampaignsHeaderActionsMenuProps = {
  locationName: string
  campaignHelpUrl: string
  onViewMessagingUsage: () => void
}

/** Campaigns page header ⋮ — Messaging usage scroll + Campaign help only. */
export function CampaignsHeaderActionsMenu({
  locationName,
  campaignHelpUrl,
  onViewMessagingUsage,
}: CampaignsHeaderActionsMenuProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label={`Actions for Campaigns at ${locationName}`}
          className={GUESTS_ROW_ACTIONS_TRIGGER_CLASS}
        >
          <MoreVerticalIcon className="size-6" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className={GUESTS_ROW_ACTIONS_MENU_CLASS}>
        {CAMPAIGNS_HEADER_OVERFLOW_ACTIONS.map((action, index) => (
          <Fragment key={action.id}>
            {index > 0 ? (
              <DropdownMenuSeparator
                className={GUESTS_ROW_ACTIONS_SEPARATOR_CLASS}
              />
            ) : null}
            {action.id === "campaign-help" ? (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                asChild
              >
                <Link to={campaignHelpUrl}>{action.label}</Link>
              </DropdownMenuItem>
            ) : (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onSelect={() => {
                  onViewMessagingUsage()
                }}
              >
                {action.label}
              </DropdownMenuItem>
            )}
          </Fragment>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

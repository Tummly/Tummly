import { Fragment } from "react"
import { MoreVertical } from "lucide-react"

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
  OPERATOR_GUEST_ROW_ACTIONS,
} from "@/lib/operatorGuests/guestsPresentation"
import { resolveGuestPrimaryCta } from "@/lib/operatorGuests/guestPrimaryCta"

type GuestsRowActionsMenuProps = {
  guestId: string
  guestName: string
  marketingEligible: boolean
  needsRecovery: boolean
  recoveryFeedbackId: number | null
  onManageTags: (guestId: string) => void
  onViewGuest: (guestId: string) => void
  onManageMarketingPermissions: (guestId: string) => void
  onEditGuest: (guestId: string) => void
  onExportGuest: (guestId: string) => void
  onDeleteGuest: (guestId: string) => void
  onCreateCampaignWithGuest: (guestId: string) => void
  onStartRecovery: (feedbackId: number) => void
}

/** Figma Guests table Actions menu — node `4213:61228`. */
export function GuestsRowActionsMenu({
  guestId,
  guestName,
  marketingEligible,
  needsRecovery,
  recoveryFeedbackId,
  onManageTags,
  onViewGuest,
  onManageMarketingPermissions,
  onEditGuest,
  onExportGuest,
  onDeleteGuest,
  onCreateCampaignWithGuest,
  onStartRecovery,
}: GuestsRowActionsMenuProps) {
  const primaryCta = resolveGuestPrimaryCta(
    { marketingEligible, needsRecovery, recoveryFeedbackId },
    { createCampaignLabel: "Create campaign with guest" }
  )

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label={`Actions for ${guestName}`}
          className={GUESTS_ROW_ACTIONS_TRIGGER_CLASS}
        >
          <MoreVertical className="size-4" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className={GUESTS_ROW_ACTIONS_MENU_CLASS}>
        {OPERATOR_GUEST_ROW_ACTIONS.map((action, index) => (
          <Fragment key={action.id}>
            {index > 0 ? (
              <DropdownMenuSeparator
                className={GUESTS_ROW_ACTIONS_SEPARATOR_CLASS}
              />
            ) : null}
            {action.id === "manage-tags" ? (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onClick={() => {
                  onManageTags(guestId)
                }}
              >
                {action.label}
              </DropdownMenuItem>
            ) : action.id === "view-guest" ? (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onClick={() => {
                  onViewGuest(guestId)
                }}
              >
                {action.label}
              </DropdownMenuItem>
            ) : action.id === "manage-marketing-permissions" ? (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onClick={() => {
                  onManageMarketingPermissions(guestId)
                }}
              >
                {action.label}
              </DropdownMenuItem>
            ) : action.id === "edit-guest-details" ? (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onClick={() => {
                  onEditGuest(guestId)
                }}
              >
                {action.label}
              </DropdownMenuItem>
            ) : action.id === "export-guest-record" ? (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onClick={() => {
                  onExportGuest(guestId)
                }}
              >
                {action.label}
              </DropdownMenuItem>
            ) : action.id === "delete-guest-data" ? (
              <DropdownMenuItem
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                onClick={() => {
                  onDeleteGuest(guestId)
                }}
              >
                {action.label}
              </DropdownMenuItem>
            ) : action.id === "create-campaign-with-guest" ? (
              primaryCta.kind === "start-recovery" ? (
                <DropdownMenuItem
                  className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                  onClick={() => {
                    onStartRecovery(primaryCta.feedbackId)
                  }}
                >
                  {primaryCta.label}
                </DropdownMenuItem>
              ) : (
                <DropdownMenuItem
                  disabled={!primaryCta.enabled}
                  className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                  onClick={() => {
                    if (!primaryCta.enabled) {
                      return
                    }
                    onCreateCampaignWithGuest(guestId)
                  }}
                >
                  {primaryCta.label}
                </DropdownMenuItem>
              )
            ) : (
              <DropdownMenuItem
                disabled
                className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
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

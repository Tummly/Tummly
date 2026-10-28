import { Fragment } from "react"
import { MoreVertical } from "lucide-react"

import type { AdminShopOrderListItem } from "@/api/adminShopOrdersApi"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  ADMIN_SHOP_CORRECT_STATUS_LABEL,
  ADMIN_SHOP_RECORD_DISPATCH_EVIDENCE_LABEL,
  type AdminShopFulfilmentAction,
  isAdminShopDispatchEvidenceMissing,
} from "@/lib/adminShopOrderFulfilment"
import { adminShopOrderRowActions } from "@/lib/admin/adminShopOrdersTablePresentation"
import {
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_SEPARATOR_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type AdminShopOrderRowActionsMenuProps = {
  order: AdminShopOrderListItem
  busy: boolean
  onViewOrder: () => void
  onDownloadMaterials: () => void
  onApproveMaterials: () => void
  onRequestFulfilment: (action: AdminShopFulfilmentAction) => void
  onRecordDispatchEvidence: () => void
  onCorrectStatus: () => void
}

/** Admin Shop orders row ⋮ menu. Parent cell must stop click propagation. */
export function AdminShopOrderRowActionsMenu({
  order,
  busy,
  onViewOrder,
  onDownloadMaterials,
  onApproveMaterials,
  onRequestFulfilment,
  onRecordDispatchEvidence,
  onCorrectStatus,
}: AdminShopOrderRowActionsMenuProps) {
  const actions = adminShopOrderRowActions(order)
  const canDownload = actions.downloadQrTypes.length > 0
  const canApprove = actions.approveQrTypes.length > 0
  const next = actions.nextFulfilmentAction
  const evidenceMissing = isAdminShopDispatchEvidenceMissing(order)

  return (
    <DropdownMenu modal={false}>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          disabled={busy}
          aria-label={`Actions for ${order.orderNumber}`}
          className={GUESTS_ROW_ACTIONS_TRIGGER_CLASS}
        >
          <MoreVertical className="size-4" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align="end"
        className={GUESTS_ROW_ACTIONS_MENU_CLASS}
      >
        <DropdownMenuItem
          className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
          onSelect={onViewOrder}
        >
          View order
        </DropdownMenuItem>
        <DropdownMenuSeparator className={GUESTS_ROW_ACTIONS_SEPARATOR_CLASS} />
        <DropdownMenuItem
          className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
          disabled={!canDownload || busy}
          onSelect={onDownloadMaterials}
        >
          Download materials
        </DropdownMenuItem>
        {canApprove ? (
          <DropdownMenuItem
            className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
            disabled={busy}
            onSelect={onApproveMaterials}
          >
            Approve materials
          </DropdownMenuItem>
        ) : null}
        {next ? (
          <Fragment>
            <DropdownMenuSeparator
              className={GUESTS_ROW_ACTIONS_SEPARATOR_CLASS}
            />
            <DropdownMenuItem
              className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
              disabled={busy}
              onSelect={() => onRequestFulfilment(next)}
            >
              {next.label}
            </DropdownMenuItem>
          </Fragment>
        ) : null}
        {evidenceMissing ? (
          <Fragment>
            <DropdownMenuSeparator
              className={GUESTS_ROW_ACTIONS_SEPARATOR_CLASS}
            />
            <DropdownMenuItem
              className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
              disabled={busy}
              onSelect={onRecordDispatchEvidence}
            >
              {ADMIN_SHOP_RECORD_DISPATCH_EVIDENCE_LABEL}
            </DropdownMenuItem>
            <DropdownMenuItem
              className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
              disabled={busy}
              onSelect={onCorrectStatus}
            >
              {ADMIN_SHOP_CORRECT_STATUS_LABEL}
            </DropdownMenuItem>
          </Fragment>
        ) : null}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

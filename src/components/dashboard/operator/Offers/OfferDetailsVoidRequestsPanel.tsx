import { OfferDetailsLifecycleEmptyState } from "@/components/dashboard/operator/Offers/OfferDetailsLifecycleEmptyState"
import { OfferDetailsRowActionsMenu } from "@/components/dashboard/operator/Offers/OfferDetailsRowActionsMenu"
import { Badge } from "@/components/ui/badge"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import type { OfferDetailsVoidRequestsTabViewModel } from "@/lib/operatorOffers/createOfferDetailsPageModule"
import type { OfferDetailsVoidRequestsRowActionId } from "@/lib/operatorOffers/offerDetailsPresentation"
import {
  CAPTURE_PLACEMENTS_ACTIONS_CELL_CLASS,
  CAPTURE_PLACEMENTS_BODY_CELL_CLASS,
  CAPTURE_PLACEMENTS_BODY_ROW_CLASS,
  CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_ROW_CLASS,
  CAPTURE_PLACEMENTS_NAME_CELL_CLASS,
  CAPTURE_PLACEMENTS_TABLE_CLASS,
  CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import { GUESTS_MARKETING_STATUS_BADGE_CLASS } from "@/lib/operatorGuests/guestsPresentation"

type OfferDetailsVoidRequestsPanelProps = {
  voidRequests: OfferDetailsVoidRequestsTabViewModel
  onRowAction: (
    rowId: string,
    actionId: OfferDetailsVoidRequestsRowActionId
  ) => void
}

export function OfferDetailsVoidRequestsPanel({
  voidRequests,
  onRowAction,
}: OfferDetailsVoidRequestsPanelProps) {
  if (voidRequests.empty != null) {
    return <OfferDetailsLifecycleEmptyState empty={voidRequests.empty} />
  }

  return (
    <div className={CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS}>
      <Table className={CAPTURE_PLACEMENTS_TABLE_CLASS}>
        <TableHeader>
          <TableRow className={CAPTURE_PLACEMENTS_HEAD_ROW_CLASS}>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {voidRequests.columns.dateTime}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {voidRequests.columns.requestedBy}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {voidRequests.columns.guest}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {voidRequests.columns.offerPass}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {voidRequests.columns.reason}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {voidRequests.columns.location}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {voidRequests.columns.currentState}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {voidRequests.columns.requestedCorrection}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {voidRequests.columns.status}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS}>
              {voidRequests.columns.actions}
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {voidRequests.rows.map((row) => (
            <TableRow key={row.id} className={CAPTURE_PLACEMENTS_BODY_ROW_CLASS}>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.dateTimeText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.requestedByText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_NAME_CELL_CLASS}>
                {row.guestName}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.offerPassText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.reasonText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.locationName}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.currentStateText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.requestedCorrectionText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                <Badge
                  variant="soft"
                  className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
                >
                  {row.statusText}
                </Badge>
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_ACTIONS_CELL_CLASS}>
                <OfferDetailsRowActionsMenu
                  ariaLabel={`Actions for void request ${row.offerPassText}`}
                  actions={row.actions}
                  onAction={(actionId) => {
                    onRowAction(row.id, actionId)
                  }}
                />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}

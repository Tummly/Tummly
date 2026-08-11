import { OfferDetailsLifecycleEmptyState } from "@/components/dashboard/operator/Offers/OfferDetailsLifecycleEmptyState"
import { OfferDetailsRowActionsMenu } from "@/components/dashboard/operator/Offers/OfferDetailsRowActionsMenu"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import type { OfferDetailsRedemptionsTabViewModel } from "@/lib/operatorOffers/createOfferDetailsPageModule"
import type { OfferDetailsRedemptionsRowActionId } from "@/lib/operatorOffers/offerDetailsPresentation"
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

type OfferDetailsRedemptionsPanelProps = {
  redemptions: OfferDetailsRedemptionsTabViewModel
  onRowAction: (
    rowId: string,
    actionId: OfferDetailsRedemptionsRowActionId
  ) => void
}

/** Details Redemptions table — Override column omitted (ticket 10 / 24). */
export function OfferDetailsRedemptionsPanel({
  redemptions,
  onRowAction,
}: OfferDetailsRedemptionsPanelProps) {
  if (redemptions.empty != null) {
    return <OfferDetailsLifecycleEmptyState empty={redemptions.empty} />
  }

  return (
    <div className={CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS}>
      <Table className={CAPTURE_PLACEMENTS_TABLE_CLASS}>
        <TableHeader>
          <TableRow className={CAPTURE_PLACEMENTS_HEAD_ROW_CLASS}>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {redemptions.columns.dateTime}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {redemptions.columns.guest}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {redemptions.columns.passReference}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {redemptions.columns.location}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {redemptions.columns.staffMember}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {redemptions.columns.outcome}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {redemptions.columns.reason}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {redemptions.columns.offerVersion}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS}>
              {redemptions.columns.actions}
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {redemptions.rows.map((row) => (
            <TableRow key={row.id} className={CAPTURE_PLACEMENTS_BODY_ROW_CLASS}>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.dateTimeText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_NAME_CELL_CLASS}>
                {row.guestName}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.passReferenceText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.locationName}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.staffMemberText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.outcomeText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.reasonText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.offerVersionText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_ACTIONS_CELL_CLASS}>
                <OfferDetailsRowActionsMenu
                  ariaLabel={`Actions for redemption ${row.passReferenceText}`}
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

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
import type { OfferDetailsClaimsTabViewModel } from "@/lib/operatorOffers/createOfferDetailsPageModule"
import type { OfferDetailsClaimsRowActionId } from "@/lib/operatorOffers/offerDetailsPresentation"
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

type OfferDetailsClaimsPanelProps = {
  claims: OfferDetailsClaimsTabViewModel
  onShareOfferInCampaign?: () => void
  onRowAction: (rowId: string, actionId: OfferDetailsClaimsRowActionId) => void
}

export function OfferDetailsClaimsPanel({
  claims,
  onShareOfferInCampaign,
  onRowAction,
}: OfferDetailsClaimsPanelProps) {
  if (claims.empty != null) {
    return (
      <OfferDetailsLifecycleEmptyState
        empty={claims.empty}
        onPrimaryCta={onShareOfferInCampaign}
      />
    )
  }

  return (
    <div className={CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS}>
      <Table className={CAPTURE_PLACEMENTS_TABLE_CLASS}>
        <TableHeader>
          <TableRow className={CAPTURE_PLACEMENTS_HEAD_ROW_CLASS}>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {claims.columns.guest}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {claims.columns.claimCode}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {claims.columns.claimed}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {claims.columns.source}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {claims.columns.location}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {claims.columns.expiry}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {claims.columns.status}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS}>
              {claims.columns.actions}
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {claims.rows.map((row) => (
            <TableRow key={row.id} className={CAPTURE_PLACEMENTS_BODY_ROW_CLASS}>
              <TableCell className={CAPTURE_PLACEMENTS_NAME_CELL_CLASS}>
                {row.guestName}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.claimCode}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.claimedText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.sourceText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.locationName}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.expiryText}
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
                  ariaLabel={`Actions for claim ${row.claimCode}`}
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

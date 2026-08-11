import { OffersRowActionsMenu } from "@/components/dashboard/operator/Offers/OffersRowActionsMenu"
import { Badge } from "@/components/ui/badge"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  OFFERS_LIST_TABLE_COPY,
  type OfferRowActionId,
  type OperatorOffersListTableRow,
} from "@/lib/operatorOffers/offerListPresentation"
import {
  GUESTS_TABLE_ACTIONS_CELL_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_GUEST_NAME_CLASS,
  GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
  GUESTS_TABLE_INTERACTION_TIME_CLASS,
  GUESTS_TABLE_LOCATION_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type OffersListTableProps = {
  rows: readonly OperatorOffersListTableRow[]
  onRowAction: (offerId: number, actionId: OfferRowActionId) => void
}

/** Offers table — Figma Main Offers columns (3498:1587 / ticket 20). */
export function OffersListTable({ rows, onRowAction }: OffersListTableProps) {
  const copy = OFFERS_LIST_TABLE_COPY

  return (
    <div className={GUESTS_TABLE_FRAME_CLASS}>
      <Table className={GUESTS_TABLE_CLASS}>
        <TableHeader className="[&_tr]:border-0">
          <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.offerColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.statusColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.validityColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.claimsColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.redeemedColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.redemptionRateColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.controlsColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS}>
              <span className="sr-only">{copy.actionsColumn}</span>
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.map((row) => (
            <TableRow key={row.id} className={GUESTS_TABLE_BODY_ROW_CLASS}>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <div className="flex flex-col gap-1.5">
                  <p className={GUESTS_TABLE_GUEST_NAME_CLASS}>{row.title}</p>
                  {row.attachSubline != null ? (
                    <p className={GUESTS_TABLE_INTERACTION_TIME_CLASS}>
                      {row.attachSubline}
                    </p>
                  ) : null}
                </div>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <Badge variant="soft">{row.statusLabel}</Badge>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_LOCATION_CLASS}>
                  {row.validityLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className="text-sm text-foreground">{row.claimsLabel}</span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className="text-sm text-foreground">
                  {row.redeemedLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className="text-sm text-foreground">
                  {row.redemptionRateLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_LOCATION_CLASS}>
                  {row.controlsLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_ACTIONS_CELL_CLASS}>
                <OffersRowActionsMenu
                  offerTitle={row.title}
                  status={row.status}
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

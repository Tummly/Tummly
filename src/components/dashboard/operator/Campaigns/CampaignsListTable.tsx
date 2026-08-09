import { CampaignsRowActionsMenu } from "@/components/dashboard/operator/Campaigns/CampaignsRowActionsMenu"
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
  CAMPAIGNS_LIST_TABLE_COPY,
  type CampaignRowActionId,
  type OperatorCampaignsListTableRow,
} from "@/lib/operatorCampaigns/campaignListPresentation"
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

type CampaignsListTableProps = {
  rows: readonly OperatorCampaignsListTableRow[]
  onRowAction: (
    campaignId: number,
    rowVersion: string,
    actionId: CampaignRowActionId
  ) => void
}

/** Campaigns table — Figma Draft projection (3462:61988 / ticket 30). */
export function CampaignsListTable({
  rows,
  onRowAction,
}: CampaignsListTableProps) {
  const copy = CAMPAIGNS_LIST_TABLE_COPY

  return (
    <div className={GUESTS_TABLE_FRAME_CLASS}>
      <Table className={GUESTS_TABLE_CLASS}>
        <TableHeader className="[&_tr]:border-0">
          <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.campaignColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.statusColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.locationColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.channelColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.offerColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.sendDateColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.deliveryColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.engagementColumn}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.redemptionsColumn}
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
                  <p className={GUESTS_TABLE_GUEST_NAME_CLASS}>{row.name}</p>
                  {row.metaLine.length > 0 ? (
                    <p className={GUESTS_TABLE_INTERACTION_TIME_CLASS}>
                      {row.metaLine}
                    </p>
                  ) : null}
                </div>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <Badge variant="soft">{row.statusLabel}</Badge>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_LOCATION_CLASS}>
                  {row.locationName}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <div className="flex flex-col gap-1.5">
                  {row.channelLabel != null ? (
                    <Badge variant="soft">{row.channelLabel}</Badge>
                  ) : (
                    <span className={GUESTS_TABLE_LOCATION_CLASS}>
                      {copy.metricDash}
                    </span>
                  )}
                  {row.channelDetail != null ? (
                    <p className={GUESTS_TABLE_INTERACTION_TIME_CLASS}>
                      {row.channelDetail}
                    </p>
                  ) : null}
                </div>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <div className="flex flex-col gap-1.5">
                  <p className={GUESTS_TABLE_GUEST_NAME_CLASS}>{row.offerTitle}</p>
                  {row.offerDetail != null ? (
                    <p className={GUESTS_TABLE_INTERACTION_TIME_CLASS}>
                      {row.offerDetail}
                    </p>
                  ) : null}
                </div>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_LOCATION_CLASS}>
                  {row.sendDateLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className="text-sm text-foreground">
                  {row.deliveryLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className="text-sm text-foreground">
                  {row.engagementLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className="text-sm text-foreground">
                  {row.redemptionsLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_ACTIONS_CELL_CLASS}>
                <CampaignsRowActionsMenu
                  campaignName={row.name}
                  status={row.status}
                  onAction={(actionId) => {
                    onRowAction(row.id, row.rowVersion, actionId)
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

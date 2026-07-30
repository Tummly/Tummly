import { Badge } from "@/components/ui/badge"
import { CapturePlacementRowActionsMenu } from "@/components/dashboard/operator/Capture/CapturePlacementRowActionsMenu"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import type { OperatorCapturePlacementRow } from "@/lib/operatorCapture/buildCapturePlacements"
import {
  CAPTURE_PLACEMENTS_ACTIONS_CELL_CLASS,
  CAPTURE_PLACEMENTS_BODY_CELL_CLASS,
  CAPTURE_PLACEMENTS_BODY_ROW_CLASS,
  CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_ROW_CLASS,
  CAPTURE_PLACEMENTS_LAST_SCAN_CELL_CLASS,
  CAPTURE_PLACEMENTS_NAME_CELL_CLASS,
  CAPTURE_PLACEMENTS_TABLE_CLASS,
  CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS,
  OPERATOR_CAPTURE_PLACEMENTS_COLUMNS,
} from "@/lib/operatorCapture/capturePresentation"

type CapturePlacementsTableProps = {
  rows: OperatorCapturePlacementRow[]
  onViewDetails: (qrCodeId: number) => void
  onPausePlacement: (qrCodeId: number) => void
  onResumePlacement: (qrCodeId: number) => void
  onCopyPlacementLink: (qrCodeId: number) => void
}

/** QR placements table with per-row actions for Active/Paused links. */
export function CapturePlacementsTable({
  rows,
  onViewDetails,
  onPausePlacement,
  onResumePlacement,
  onCopyPlacementLink,
}: CapturePlacementsTableProps) {
  const columns = OPERATOR_CAPTURE_PLACEMENTS_COLUMNS

  return (
    <div className={CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS}>
      <Table className={CAPTURE_PLACEMENTS_TABLE_CLASS}>
        <TableHeader>
          <TableRow className={CAPTURE_PLACEMENTS_HEAD_ROW_CLASS}>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {columns.placement}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {columns.status}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {columns.qrScans}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {columns.feedbackSubmitted}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {columns.marketingOptIns}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {columns.offerClaims}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {columns.lastScan}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS}>
              {columns.actions}
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.map((row) => (
            <TableRow
              key={row.qrCodeId}
              className={CAPTURE_PLACEMENTS_BODY_ROW_CLASS}
            >
              <TableCell className={CAPTURE_PLACEMENTS_NAME_CELL_CLASS}>
                {row.placementLabel}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                <Badge variant="soft">{row.status}</Badge>
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.qrScansText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.feedbackSubmittedText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.marketingOptInsText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.offerClaimsText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_LAST_SCAN_CELL_CLASS}>
                {row.lastScanText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_ACTIONS_CELL_CLASS}>
                <CapturePlacementRowActionsMenu
                  placementLabel={row.placementLabel}
                  status={row.status}
                  onViewDetails={() => {
                    onViewDetails(row.qrCodeId)
                  }}
                  onPause={() => {
                    onPausePlacement(row.qrCodeId)
                  }}
                  onResume={() => {
                    onResumePlacement(row.qrCodeId)
                  }}
                  onCopyLink={() => {
                    onCopyPlacementLink(row.qrCodeId)
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

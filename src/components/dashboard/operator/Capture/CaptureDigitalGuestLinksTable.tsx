import { Badge } from "@/components/ui/badge"
import { CaptureDigitalGuestLinkRowActionsMenu } from "@/components/dashboard/operator/Capture/CaptureDigitalGuestLinkRowActionsMenu"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import type { OperatorCaptureDigitalGuestLinkRow } from "@/lib/operatorCapture/buildCaptureDigitalGuestLinks"
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
  OPERATOR_CAPTURE_DIGITAL_GUEST_LINKS_COLUMNS,
} from "@/lib/operatorCapture/capturePresentation"

type CaptureDigitalGuestLinksTableProps = {
  rows: OperatorCaptureDigitalGuestLinkRow[]
  pauseActivateEnabled?: boolean
  onViewDetails: (qrCodeId: number) => void
  onPreview: (qrCodeId: number) => void
  onPause: (qrCodeId: number) => void
  onActivate: (qrCodeId: number) => void
  onCopyLink: (qrCodeId: number) => void
  onArchive: (qrCodeId: number) => void
}

/** Digital guest links table — Figma `4674:39426` (Guest form opens cells). */
export function CaptureDigitalGuestLinksTable({
  rows,
  pauseActivateEnabled = true,
  onViewDetails,
  onPreview,
  onPause,
  onActivate,
  onCopyLink,
  onArchive,
}: CaptureDigitalGuestLinksTableProps) {
  const columns = OPERATOR_CAPTURE_DIGITAL_GUEST_LINKS_COLUMNS

  return (
    <div className={CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS}>
      <Table className={CAPTURE_PLACEMENTS_TABLE_CLASS}>
        <TableHeader>
          <TableRow className={CAPTURE_PLACEMENTS_HEAD_ROW_CLASS}>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {columns.guestLink}
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
                {row.guestLinkLabel}
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
                <CaptureDigitalGuestLinkRowActionsMenu
                  guestLinkLabel={row.guestLinkLabel}
                  status={row.status}
                  pauseActivateEnabled={pauseActivateEnabled}
                  onViewDetails={() => {
                    onViewDetails(row.qrCodeId)
                  }}
                  onPreview={() => {
                    onPreview(row.qrCodeId)
                  }}
                  onPause={() => {
                    onPause(row.qrCodeId)
                  }}
                  onActivate={() => {
                    onActivate(row.qrCodeId)
                  }}
                  onCopyLink={() => {
                    onCopyLink(row.qrCodeId)
                  }}
                  onArchive={() => {
                    onArchive(row.qrCodeId)
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

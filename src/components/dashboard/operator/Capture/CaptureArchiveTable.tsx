import { MoreVertical } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import type { OperatorCaptureArchiveRow } from "@/lib/operatorCapture/createOperatorCapturePageModule"
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
  CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_TRIGGER_CLASS,
  OPERATOR_CAPTURE_ARCHIVE_COPY,
} from "@/lib/operatorCapture/capturePresentation"

type CaptureArchiveTableProps = {
  rows: readonly OperatorCaptureArchiveRow[]
  onViewDetails: (qrCodeId: number) => void
  onRestore: (qrCodeId: number) => void
  onDuplicateAsNew: (qrCodeId: number) => void
}

/** Archived placements table — View details / Restore / Duplicate as new (digital). */
export function CaptureArchiveTable({
  rows,
  onViewDetails,
  onRestore,
  onDuplicateAsNew,
}: CaptureArchiveTableProps) {
  const copy = OPERATOR_CAPTURE_ARCHIVE_COPY

  return (
    <div className={CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS}>
      <Table className={CAPTURE_PLACEMENTS_TABLE_CLASS}>
        <TableHeader>
          <TableRow className={CAPTURE_PLACEMENTS_HEAD_ROW_CLASS}>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {copy.columns.placement}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {copy.columns.location}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {copy.columns.archivedOn}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {copy.columns.archivedBy}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {copy.columns.qrScans}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {copy.columns.feedbackSubmitted}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {copy.columns.lastScan}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS}>
              {copy.columns.actions}
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
                {row.locationName}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.archivedOnText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.archivedByText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.qrScansText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.feedbackSubmittedText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_LAST_SCAN_CELL_CLASS}>
                {row.lastScanText}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_ACTIONS_CELL_CLASS}>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      type="button"
                      variant="op-ghost"
                      size="icon"
                      aria-label={`Actions for ${row.placementLabel}`}
                      className={CAPTURE_PLACEMENT_ROW_ACTIONS_TRIGGER_CLASS}
                    >
                      <MoreVertical className="size-4" aria-hidden />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent
                    align="end"
                    className={CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS}
                  >
                    <DropdownMenuItem
                      className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
                      onClick={() => {
                        onViewDetails(row.qrCodeId)
                      }}
                    >
                      {copy.rowActions.viewDetails}
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
                      disabled={!row.canRestore}
                      title={
                        row.canRestore
                          ? undefined
                          : copy.rowActions.restoreDisabled
                      }
                      onClick={() => {
                        if (!row.canRestore) {
                          return
                        }
                        onRestore(row.qrCodeId)
                      }}
                    >
                      {copy.rowActions.restore}
                    </DropdownMenuItem>
                    {row.canDuplicateAsNew ? (
                      <DropdownMenuItem
                        className={CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS}
                        onClick={() => {
                          onDuplicateAsNew(row.qrCodeId)
                        }}
                      >
                        {copy.rowActions.duplicateAsNew}
                      </DropdownMenuItem>
                    ) : null}
                  </DropdownMenuContent>
                </DropdownMenu>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}

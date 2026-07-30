import { MoreVerticalIcon } from "lucide-react"

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
  CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS,
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
    <div className="overflow-hidden rounded-op-md border border-op-border-default bg-[var(--op-color-gray-1000)]">
      <Table>
        <TableHeader>
          <TableRow className="border-op-border-default hover:bg-transparent">
            <TableHead className="text-op-text-muted">
              {copy.columns.placement}
            </TableHead>
            <TableHead className="text-op-text-muted">
              {copy.columns.location}
            </TableHead>
            <TableHead className="text-op-text-muted">
              {copy.columns.archivedOn}
            </TableHead>
            <TableHead className="text-op-text-muted">
              {copy.columns.archivedBy}
            </TableHead>
            <TableHead className="text-op-text-muted">
              {copy.columns.qrScans}
            </TableHead>
            <TableHead className="text-op-text-muted">
              {copy.columns.feedbackSubmitted}
            </TableHead>
            <TableHead className="text-op-text-muted">
              {copy.columns.lastScan}
            </TableHead>
            <TableHead className="w-12 text-op-text-muted">
              <span className="sr-only">{copy.columns.actions}</span>
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.map((row) => (
            <TableRow
              key={row.qrCodeId}
              className="border-op-border-default"
            >
              <TableCell className="font-medium text-op-text-primary">
                {row.placementLabel}
              </TableCell>
              <TableCell className="text-op-text-muted">
                {row.locationName}
              </TableCell>
              <TableCell className="text-op-text-muted">
                {row.archivedOnText}
              </TableCell>
              <TableCell className="text-op-text-muted">
                {row.archivedByText}
              </TableCell>
              <TableCell className="text-op-text-muted">
                {row.qrScansText}
              </TableCell>
              <TableCell className="text-op-text-muted">
                {row.feedbackSubmittedText}
              </TableCell>
              <TableCell className="text-op-text-muted">
                {row.lastScanText}
              </TableCell>
              <TableCell>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      type="button"
                      variant="op-ghost"
                      size="icon"
                      aria-label="Archived placement actions"
                      className="size-8"
                    >
                      <MoreVerticalIcon className="size-4" aria-hidden />
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

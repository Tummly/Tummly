import { Link } from "react-router-dom"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  LOCATION_DETAIL_ACTION_BUTTON_CLASS,
  LOCATION_DETAIL_CARD_CLASS,
  LOCATION_DETAIL_PAGE_COPY,
  LOCATION_DETAIL_SECTION_SUBTITLE_CLASS,
  LOCATION_DETAIL_SECTION_TITLE_CLASS,
  LOCATION_DETAIL_TABLE_BODY_CELL_CLASS,
  LOCATION_DETAIL_TABLE_BODY_ROW_CLASS,
  LOCATION_DETAIL_TABLE_FRAME_CLASS,
  LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS,
  LOCATION_DETAIL_TABLE_HEAD_ROW_CLASS,
  LOCATION_DETAIL_TABLE_MUTED_CELL_CLASS,
  LOCATION_DETAIL_TABLE_NAME_CELL_CLASS,
  LOCATION_DETAIL_TABLE_CLASS,
  type LocationDetailQrRow,
} from "@/lib/operatorLocations/locationDetailPresentation"
import { GUESTS_MARKETING_STATUS_BADGE_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"

type LocationDetailAssignedQrSectionProps = {
  qrRows: LocationDetailQrRow[]
  createQrPath: string
}

export function LocationDetailAssignedQrSection({
  qrRows,
  createQrPath,
}: LocationDetailAssignedQrSectionProps) {
  const copy = LOCATION_DETAIL_PAGE_COPY

  return (
    <section
      className={cn(LOCATION_DETAIL_CARD_CLASS, "gap-[22px]")}
      aria-label={copy.assignedQrTitle}
    >
      <div className="flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-start">
        <div className="flex flex-col gap-2">
          <h2 className={LOCATION_DETAIL_SECTION_TITLE_CLASS}>
            {copy.assignedQrTitle}
          </h2>
          <p className={LOCATION_DETAIL_SECTION_SUBTITLE_CLASS}>
            {copy.assignedQrSubtitle}
          </p>
        </div>
        <Button
          type="button"
          variant="op-primary"
          className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
          asChild
        >
          <Link to={createQrPath}>{copy.createQrCode}</Link>
        </Button>
      </div>

      {qrRows.length === 0 ? (
        <div className="flex min-h-[180px] flex-col items-center justify-center gap-2 text-center">
          <p className="m-0 text-base font-medium text-op-text-primary">
            {copy.qrEmptyTitle}
          </p>
          <p className="m-0 max-w-[420px] text-sm font-medium text-op-text-muted">
            {copy.qrEmptyHelper}
          </p>
        </div>
      ) : (
        <div className={LOCATION_DETAIL_TABLE_FRAME_CLASS}>
          <Table className={LOCATION_DETAIL_TABLE_CLASS}>
            <TableHeader className="[&_tr]:border-0">
              <TableRow className={LOCATION_DETAIL_TABLE_HEAD_ROW_CLASS}>
                <TableHead
                  className={cn(
                    LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS,
                    "min-w-[210px]"
                  )}
                >
                  QR name
                </TableHead>
                <TableHead className={LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS}>
                  Placement
                </TableHead>
                <TableHead className={LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS}>
                  Status
                </TableHead>
                <TableHead className={LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS}>
                  Scans
                </TableHead>
                <TableHead className={LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS}>
                  Starts
                </TableHead>
                <TableHead className={LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS}>
                  Submissions
                </TableHead>
                <TableHead className={LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS}>
                  Opt-ins
                </TableHead>
                <TableHead className={LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS}>
                  Claims
                </TableHead>
                <TableHead className={LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS}>
                  Last scanned
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {qrRows.map((row) => (
                <TableRow
                  key={row.id}
                  className={LOCATION_DETAIL_TABLE_BODY_ROW_CLASS}
                >
                  <TableCell className={LOCATION_DETAIL_TABLE_NAME_CELL_CLASS}>
                    {row.name}
                  </TableCell>
                  <TableCell className={LOCATION_DETAIL_TABLE_BODY_CELL_CLASS}>
                    {row.placement}
                  </TableCell>
                  <TableCell className={LOCATION_DETAIL_TABLE_BODY_CELL_CLASS}>
                    <Badge
                      variant="soft"
                      className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
                    >
                      {row.statusLabel}
                    </Badge>
                  </TableCell>
                  <TableCell className={LOCATION_DETAIL_TABLE_BODY_CELL_CLASS}>
                    {row.scans}
                  </TableCell>
                  <TableCell className={LOCATION_DETAIL_TABLE_BODY_CELL_CLASS}>
                    {row.starts}
                  </TableCell>
                  <TableCell className={LOCATION_DETAIL_TABLE_BODY_CELL_CLASS}>
                    {row.submissions}
                  </TableCell>
                  <TableCell className={LOCATION_DETAIL_TABLE_BODY_CELL_CLASS}>
                    {row.optIns}
                  </TableCell>
                  <TableCell className={LOCATION_DETAIL_TABLE_BODY_CELL_CLASS}>
                    {row.claims}
                  </TableCell>
                  <TableCell className={LOCATION_DETAIL_TABLE_MUTED_CELL_CLASS}>
                    {row.lastScannedLabel}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </section>
  )
}

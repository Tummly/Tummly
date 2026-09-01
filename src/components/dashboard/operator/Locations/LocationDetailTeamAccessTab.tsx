import { Link } from "react-router-dom"

import { Button } from "@/components/ui/button"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import type { LocationDetailSnapshot } from "@/lib/operatorLocations/createOperatorLocationDetailPageModule"
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
} from "@/lib/operatorLocations/locationDetailPresentation"
import { cn } from "@/lib/utils"

type LocationDetailTeamAccessTabProps = {
  snap: LocationDetailSnapshot
  teamPermissionsPath: string
}

export function LocationDetailTeamAccessTab({
  snap,
  teamPermissionsPath,
}: LocationDetailTeamAccessTabProps) {
  const copy = LOCATION_DETAIL_PAGE_COPY

  return (
    <section
      className={cn(LOCATION_DETAIL_CARD_CLASS, "gap-[22px]")}
      aria-label={copy.teamAccessTitle}
    >
      <div className="flex flex-col gap-2">
        <h2 className={LOCATION_DETAIL_SECTION_TITLE_CLASS}>
          {copy.teamAccessTitle}
        </h2>
        <p className={LOCATION_DETAIL_SECTION_SUBTITLE_CLASS}>
          {copy.teamAccessSubtitle}
        </p>
      </div>

      {snap.teamAccessRows.length === 0 ? (
        <div className="flex min-h-[180px] flex-col items-start justify-center gap-4">
          <div className="flex flex-col gap-2">
            <p className="m-0 text-base font-medium text-op-text-primary">
              {copy.teamAccessEmptyTitle}
            </p>
            <p className="m-0 max-w-[480px] text-sm font-medium text-op-text-muted">
              {copy.teamAccessEmptyHelper}
            </p>
          </div>
          <Button
            type="button"
            variant="op-secondary"
            className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
            asChild
          >
            <Link to={teamPermissionsPath}>{copy.assignTeamMember}</Link>
          </Button>
        </div>
      ) : (
        <>
          <div className={LOCATION_DETAIL_TABLE_FRAME_CLASS}>
            <Table className={LOCATION_DETAIL_TABLE_CLASS}>
              <TableHeader className="[&_tr]:border-0">
                <TableRow className={LOCATION_DETAIL_TABLE_HEAD_ROW_CLASS}>
                  <TableHead className={LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS}>
                    Name
                  </TableHead>
                  <TableHead className={LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS}>
                    Role
                  </TableHead>
                  <TableHead className={LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS}>
                    Access
                  </TableHead>
                  <TableHead className={LOCATION_DETAIL_TABLE_HEAD_CELL_CLASS}>
                    Last active
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {snap.teamAccessRows.map((row) => (
                  <TableRow
                    key={row.id}
                    className={LOCATION_DETAIL_TABLE_BODY_ROW_CLASS}
                  >
                    <TableCell className={LOCATION_DETAIL_TABLE_NAME_CELL_CLASS}>
                      {row.name}
                    </TableCell>
                    <TableCell className={LOCATION_DETAIL_TABLE_MUTED_CELL_CLASS}>
                      {row.role}
                    </TableCell>
                    <TableCell className={LOCATION_DETAIL_TABLE_MUTED_CELL_CLASS}>
                      {row.accessLabel}
                    </TableCell>
                    <TableCell className={LOCATION_DETAIL_TABLE_MUTED_CELL_CLASS}>
                      {row.lastActiveLabel}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          <Button
            type="button"
            variant="op-secondary"
            className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
            asChild
          >
            <Link to={teamPermissionsPath}>{copy.assignTeamMember}</Link>
          </Button>
        </>
      )}
    </section>
  )
}

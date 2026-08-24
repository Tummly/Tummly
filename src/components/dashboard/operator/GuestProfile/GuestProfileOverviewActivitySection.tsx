import { GuestProfileSectionEmptyCard } from "@/components/dashboard/operator/GuestProfile/GuestProfileSectionEmptyCard"
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
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_HEADER_ROW_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_GUEST_NAME_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
  GUESTS_TABLE_LOCATION_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type OverviewActivityTableSectionProps = {
  sectionTitle: string
  emptyTitle: string
  emptyHelper: string
  viewAllLabel: string
  onViewAll?: () => void
  columns: readonly string[]
  rows: ReadonlyArray<{
    id: number | string
    cells: readonly string[]
  }>
}

/** Overview preview table for Latest offer / Latest campaign activity. */
export function GuestProfileOverviewActivitySection({
  sectionTitle,
  emptyTitle,
  emptyHelper,
  viewAllLabel,
  onViewAll,
  columns,
  rows,
}: OverviewActivityTableSectionProps) {
  if (rows.length === 0) {
    return (
      <GuestProfileSectionEmptyCard
        sectionTitle={sectionTitle}
        emptyTitle={emptyTitle}
        emptyHelper={emptyHelper}
      />
    )
  }

  return (
    <section className={GUESTS_SECTION_CLASS} aria-label={sectionTitle}>
      <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{sectionTitle}</h2>
      </div>
      <div className={GUESTS_TABLE_FRAME_CLASS}>
        <Table className={GUESTS_TABLE_CLASS}>
          <TableHeader className="[&_tr]:border-0">
            <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
              {columns.map((column) => (
                <TableHead key={column} className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  {column}
                </TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {rows.map((row) => (
              <TableRow key={row.id} className={GUESTS_TABLE_BODY_ROW_CLASS}>
                {row.cells.map((cell, index) => (
                  <TableCell
                    key={`${row.id}-${columns[index] ?? index}`}
                    className={GUESTS_TABLE_BODY_CELL_CLASS}
                  >
                    <span
                      className={
                        index === 0
                          ? GUESTS_TABLE_GUEST_NAME_CLASS
                          : GUESTS_TABLE_LOCATION_CLASS
                      }
                    >
                      {cell}
                    </span>
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
      <div>
        <Button
          variant="op-secondary"
          type="button"
          className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
          aria-label={viewAllLabel}
          onClick={() => {
            onViewAll?.()
          }}
        >
          {viewAllLabel}
        </Button>
      </div>
    </section>
  )
}

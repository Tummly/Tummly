import { Fragment } from "react"

import { Separator } from "@/components/ui/separator"
import {
  GUESTS_DETAIL_DIVIDER_CLASS,
  GUESTS_DETAIL_FIELD_CLASS,
  GUESTS_DETAIL_FIELD_LABEL_CLASS,
  GUESTS_DETAIL_FIELD_VALUE_CLASS,
  GUESTS_DETAIL_ROW_PAIR_CLASS,
  GUESTS_DETAIL_ROWS_STACK_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

export type GuestProfileDetailRow = {
  label: string
  value: string | number
}

type GuestProfileDetailRowsProps = {
  rows: ReadonlyArray<GuestProfileDetailRow>
  /**
   * `pairs` — Guest Profile / Offer definition two-column grid.
   * `stack` — one field per row (Offer Details meta: Source, Locations, …).
   */
  layout?: "pairs" | "stack"
}

function pairDetailRows(
  rows: ReadonlyArray<GuestProfileDetailRow>
): GuestProfileDetailRow[][] {
  const pairs: GuestProfileDetailRow[][] = []
  for (let index = 0; index < rows.length; index += 2) {
    pairs.push(rows.slice(index, index + 2))
  }
  return pairs
}

function DetailField({ row }: { row: GuestProfileDetailRow }) {
  return (
    <div className={GUESTS_DETAIL_FIELD_CLASS}>
      <dt className={GUESTS_DETAIL_FIELD_LABEL_CLASS}>{row.label}</dt>
      <dd className={GUESTS_DETAIL_FIELD_VALUE_CLASS}>{row.value}</dd>
    </div>
  )
}

export function GuestProfileDetailRows({
  rows,
  layout = "pairs",
}: GuestProfileDetailRowsProps) {
  if (layout === "stack") {
    return (
      <div className={GUESTS_DETAIL_ROWS_STACK_CLASS}>
        {rows.map((row, rowIndex) => (
          <Fragment key={row.label}>
            {rowIndex > 0 ? (
              <Separator className={GUESTS_DETAIL_DIVIDER_CLASS} />
            ) : null}
            <dl className="m-0">
              <DetailField row={row} />
            </dl>
          </Fragment>
        ))}
      </div>
    )
  }

  const pairs = pairDetailRows(rows)

  return (
    <div className={GUESTS_DETAIL_ROWS_STACK_CLASS}>
      {pairs.map((pair, pairIndex) => (
        <Fragment key={pair.map((row) => row.label).join("|")}>
          {pairIndex > 0 ? (
            <Separator className={GUESTS_DETAIL_DIVIDER_CLASS} />
          ) : null}
          <dl className={GUESTS_DETAIL_ROW_PAIR_CLASS}>
            {pair.map((row) => (
              <DetailField key={row.label} row={row} />
            ))}
          </dl>
        </Fragment>
      ))}
    </div>
  )
}

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
import type { InvoiceRowSnapshot } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  CAPTURE_PLACEMENTS_ACTIONS_CELL_CLASS,
  CAPTURE_PLACEMENTS_BODY_CELL_CLASS,
  CAPTURE_PLACEMENTS_BODY_ROW_CLASS,
  CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_ROW_CLASS,
  CAPTURE_PLACEMENTS_NAME_CELL_CLASS,
  CAPTURE_PLACEMENTS_TABLE_CLASS,
  CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import { BILLING_CREDITS_PAGE_COPY as copy } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import { GUESTS_PAGE_SECONDARY_BUTTON_CLASS } from "@/lib/operatorGuests/guestsPresentation"

type PaymentInvoicesTableProps = {
  rows: readonly InvoiceRowSnapshot[]
  onView: (invoiceNo: string) => void
  onDownload: (invoiceNo: string) => void
}

export function PaymentInvoicesTable({
  rows,
  onView,
  onDownload,
}: PaymentInvoicesTableProps) {
  return (
    <div className={CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS}>
      <Table className={CAPTURE_PLACEMENTS_TABLE_CLASS}>
        <TableHeader>
          <TableRow className={CAPTURE_PLACEMENTS_HEAD_ROW_CLASS}>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {copy.invoiceDate}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {copy.invoiceNo}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {copy.description}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {copy.amount}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
              {copy.status}
            </TableHead>
            <TableHead className={CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS}>
              {copy.actions}
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.map((row) => (
            <TableRow
              key={row.invoiceNo}
              className={CAPTURE_PLACEMENTS_BODY_ROW_CLASS}
            >
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.invoiceDateLabel}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_NAME_CELL_CLASS}>
                {row.invoiceNo}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.description}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                {row.amountLabel}
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                <Badge variant="secondary">{row.status}</Badge>
              </TableCell>
              <TableCell className={CAPTURE_PLACEMENTS_ACTIONS_CELL_CLASS}>
                {row.showActions ? (
                  <div className="flex flex-wrap gap-2">
                    <Button
                      type="button"
                      variant="op-secondary"
                      className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                      onClick={() => {
                        onView(row.invoiceNo)
                      }}
                    >
                      {copy.view}
                    </Button>
                    <Button
                      type="button"
                      variant="op-secondary"
                      className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                      onClick={() => {
                        onDownload(row.invoiceNo)
                      }}
                    >
                      {copy.download}
                    </Button>
                  </div>
                ) : null}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}

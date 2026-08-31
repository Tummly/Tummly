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
  BILLING_CREDITS_CTA_BUTTON_CLASS,
  BILLING_CREDITS_PAGE_COPY as copy,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

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
    <div className={GUESTS_TABLE_FRAME_CLASS}>
      <Table className={GUESTS_TABLE_CLASS}>
        <TableHeader className="[&_tr]:border-0">
          <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.invoiceDate}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.invoiceNo}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.description}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.amount}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.status}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.actions}
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.map((row) => (
            <TableRow
              key={row.invoiceNo}
              className={GUESTS_TABLE_BODY_ROW_CLASS}
            >
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className="text-sm font-semibold leading-[19px] text-foreground">
                  {row.invoiceDateLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className="text-sm font-normal leading-[19px] text-foreground">
                  {row.invoiceNo}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className="text-sm font-normal leading-[19px] text-foreground">
                  {row.description}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className="text-sm font-normal leading-[19px] text-foreground">
                  {row.amountLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <Badge variant="soft" className="px-2 py-1.5">
                  {row.status}
                </Badge>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                {row.showActions ? (
                  <div className="flex flex-wrap items-center gap-2.5">
                    <Button
                      type="button"
                      variant="op-tertiary"
                      className={BILLING_CREDITS_CTA_BUTTON_CLASS}
                      onClick={() => {
                        onView(row.invoiceNo)
                      }}
                    >
                      {copy.view}
                    </Button>
                    <Button
                      type="button"
                      variant="op-tertiary"
                      className={BILLING_CREDITS_CTA_BUTTON_CLASS}
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

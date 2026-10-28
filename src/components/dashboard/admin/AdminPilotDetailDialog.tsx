import type { ReactNode } from "react"

import { Badge } from "@/components/ui/badge"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  buildPilotRowPresentation,
  formatPilotDate,
  pilotAllowanceRows,
  pilotExtensionHistoryRows,
} from "@/lib/admin/adminPilotExtensionPresentation"
import { GUESTS_SECTION_TITLE_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import type { AdminPilotAccount } from "@/types/admin"

type AdminPilotDetailDialogProps = {
  account: AdminPilotAccount | null
  onClose: () => void
}

function PilotFact({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <dt className="text-muted-foreground">{label}</dt>
      <dd>{children}</dd>
    </div>
  )
}

/** Read-only Pilot record. Balances and history come from the server with the row. */
export function AdminPilotDetailDialog({
  account,
  onClose,
}: AdminPilotDetailDialogProps) {
  const allowanceRows = account ? pilotAllowanceRows(account) : null
  const historyRows = account ? pilotExtensionHistoryRows(account) : []
  const presentation = account
    ? buildPilotRowPresentation(account, new Date())
    : null

  return (
    <Dialog
      open={account !== null}
      onOpenChange={(open) => {
        if (!open) onClose()
      }}
    >
      <DialogContent className="sm:max-w-2xl">
        {account && presentation ? (
          <>
            <DialogHeader>
              <DialogTitle>{account.restaurantName}</DialogTitle>
              <DialogDescription>
                Pilot record for {account.ownerEmail}
              </DialogDescription>
            </DialogHeader>

            <dl className="grid grid-cols-2 gap-x-6 gap-y-4 text-sm">
              <PilotFact label="Status">
                <Badge variant="outline">{account.billingStatus}</Badge>
              </PilotFact>
              <PilotFact label="Converted to a paid plan">
                {account.hasPaidHistory ? "Yes" : "No"}
              </PilotFact>
              <PilotFact label="Original start">
                {formatPilotDate(account.pilotStartedAtUtc)}
              </PilotFact>
              <PilotFact label="Original end">
                {presentation.originalEndLabel}
              </PilotFact>
              <PilotFact label="Effective end">{presentation.endLabel}</PilotFact>
              <PilotFact label="Days used">{`${account.daysUsed} days`}</PilotFact>
              <PilotFact label="Extensions">{presentation.extensionsLabel}</PilotFact>
            </dl>

            <section className="flex flex-col gap-2">
              <h3 className={GUESTS_SECTION_TITLE_CLASS}>Extension history</h3>
              {historyRows.length === 0 ? (
                <p className="text-sm text-muted-foreground">No extensions yet.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Date</TableHead>
                      <TableHead>Days</TableHead>
                      <TableHead>By</TableHead>
                      <TableHead>Reason</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {historyRows.map((row, index) => (
                      <TableRow key={`${row.dateLabel}-${index}`}>
                        <TableCell>{row.dateLabel}</TableCell>
                        <TableCell>{row.days}</TableCell>
                        <TableCell>{row.actor}</TableCell>
                        <TableCell className="whitespace-normal">{row.reason}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </section>

            <section className="flex flex-col gap-2">
              <h3 className={GUESTS_SECTION_TITLE_CLASS}>Pilot allowances</h3>
              {allowanceRows === null ? (
                <p className="text-sm text-muted-foreground">
                  Balance unavailable right now.
                </p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Allowance</TableHead>
                      <TableHead>Granted once</TableHead>
                      <TableHead>Used</TableHead>
                      <TableHead>Remaining</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {allowanceRows.map((row) => (
                      <TableRow key={row.channel}>
                        <TableCell>{row.label}</TableCell>
                        <TableCell>{row.granted}</TableCell>
                        <TableCell>{row.used}</TableCell>
                        <TableCell>{row.remaining}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </section>
          </>
        ) : null}
      </DialogContent>
    </Dialog>
  )
}

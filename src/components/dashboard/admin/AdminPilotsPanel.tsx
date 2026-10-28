import { useEffect, useState } from "react"
import axios from "axios"
import { toast } from "sonner"

import { extendPilot, getPilotAccounts } from "@/api/adminApi"
import { AdminPilotDetailDialog } from "@/components/dashboard/admin/AdminPilotDetailDialog"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Textarea } from "@/components/ui/textarea"
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip"
import {
  buildPilotRowPresentation,
  PILOT_EXTENSION_DAY_OPTIONS,
  PILOT_EXTENSION_MAX_COUNT,
  PILOT_EXTENSION_MAX_DAYS_TOTAL,
  pilotExtendedToastMessage,
  pilotExtensionReasonError,
  previewPilotExtension,
  formatPilotDate,
  replacePilotAccountRow,
} from "@/lib/admin/adminPilotExtensionPresentation"
import {
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_GUEST_NAME_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { AdminPilotAccount } from "@/types/admin"

// Dialog overlay and content use z-[120]. The Select menu must sit above them.
const DIALOG_SELECT_CONTENT_CLASS = "z-[130]"

type ExtendDialogState = {
  account: AdminPilotAccount
  days: (typeof PILOT_EXTENSION_DAY_OPTIONS)[number]
  reason: string
  idempotencyKey: string
  /** Once true, the length and reason are locked so a retry matches the first request. */
  attempted: boolean
  /** Validation or server message shown in the dialog. */
  error: string | null
}

type ExtendFailure = {
  message: string
  stale: boolean
}

function extendFailure(error: unknown): ExtendFailure {
  if (axios.isAxiosError(error)) {
    if (error.response?.status === 403) {
      return {
        message: "You do not have permission to extend this Pilot.",
        stale: false,
      }
    }
    const data = error.response?.data as
      | { message?: string; code?: string }
      | undefined
    if (data?.message) {
      return {
        message: data.message,
        stale: data.code === "stale_pilot_version",
      }
    }
  }
  return { message: "Could not extend the Pilot. Try again.", stale: false }
}

export function AdminPilotsPanel() {
  const [accounts, setAccounts] = useState<AdminPilotAccount[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState(false)
  const [dialog, setDialog] = useState<ExtendDialogState | null>(null)
  const [detail, setDetail] = useState<AdminPilotAccount | null>(null)
  const [extending, setExtending] = useState(false)

  useEffect(() => {
    let cancelled = false
    getPilotAccounts()
      .then((rows) => {
        if (!cancelled) setAccounts(rows)
      })
      .catch(() => {
        if (!cancelled) {
          setLoadError(true)
          toast.error("Could not load Pilot accounts")
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const openDialog = (account: AdminPilotAccount) => {
    setDialog({
      account,
      days: 14,
      reason: "",
      // One key per dialog open. A retry reuses it, so the server extends once.
      idempotencyKey: crypto.randomUUID(),
      attempted: false,
      error: null,
    })
  }

  const handleExtend = async () => {
    if (!dialog || extending) return
    const reasonError = pilotExtensionReasonError(dialog.reason)
    if (reasonError) {
      setDialog((current) =>
        current ? { ...current, error: reasonError } : current
      )
      return
    }

    setExtending(true)
    setDialog((current) =>
      current ? { ...current, attempted: true, error: null } : current
    )
    try {
      const updated = await extendPilot(dialog.account.ownerUserId, {
        days: dialog.days,
        idempotencyKey: dialog.idempotencyKey,
        restaurantId: dialog.account.restaurantId,
        reason: dialog.reason.trim(),
        expectedExtensionCount: dialog.account.extensionCount,
      })
      setAccounts((rows) => replacePilotAccountRow(rows, updated))
      toast.success(pilotExtendedToastMessage(updated))
      setDialog(null)
    } catch (error) {
      const failure = extendFailure(error)
      setDialog((current) =>
        current ? { ...current, error: failure.message } : current
      )
      // A stale version means the row is out of date: reload it into the
      // table and the open dialog so the operator can retry.
      if (failure.stale) {
        const restaurantId = dialog.account.restaurantId
        const rows = await getPilotAccounts().catch(() => null)
        if (rows) {
          setAccounts(rows)
          const fresh = rows.find((row) => row.restaurantId === restaurantId)
          setDialog((current) =>
            current && fresh
              ? { ...current, account: fresh, attempted: false }
              : current
          )
        } else {
          toast.error("Could not refresh Pilot accounts")
        }
      }
    } finally {
      setExtending(false)
    }
  }

  const now = new Date()
  const reasonError = dialog ? pilotExtensionReasonError(dialog.reason) : ""
  const preview =
    dialog && dialog.account
      ? previewPilotExtension(dialog.account, dialog.days, now)
      : null
  const dialogPresentation =
    dialog && dialog.account
      ? buildPilotRowPresentation(dialog.account, now)
      : null

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <header className="flex flex-col gap-3.5 leading-[0]">
        <h1 className={GUESTS_PAGE_TITLE_CLASS}>Pilots</h1>
        <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
          Review Pilot accounts and their end dates. Extend a Pilot by 14 or 30
          days, up to {PILOT_EXTENSION_MAX_COUNT} extensions and{" "}
          {PILOT_EXTENSION_MAX_DAYS_TOTAL} days in total.
        </p>
      </header>

      <section className={GUESTS_SECTION_CLASS}>
        <div className="flex flex-col gap-1">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>Pilot accounts</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            {loading
              ? "Loading Pilot accounts…"
              : loadError
                ? "Could not load Pilot accounts"
                : `${accounts.length} account${accounts.length === 1 ? "" : "s"}`}
          </p>
        </div>

        <div className={GUESTS_TABLE_FRAME_CLASS}>
          <TooltipProvider delayDuration={200}>
            <Table className={GUESTS_TABLE_CLASS}>
              <TableHeader className="[&_tr]:border-0">
                <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Restaurant
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Owner
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Status
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Started
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Original end
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Effective end
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Extensions
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    <span className="sr-only">Actions</span>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {!loading && loadError ? (
                  <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                    <TableCell
                      colSpan={8}
                      className={GUESTS_TABLE_BODY_CELL_CLASS}
                    >
                      Could not load Pilot accounts. Reload the page to try again.
                    </TableCell>
                  </TableRow>
                ) : null}
                {!loading && !loadError && accounts.length === 0 ? (
                  <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                    <TableCell
                      colSpan={8}
                      className={GUESTS_TABLE_BODY_CELL_CLASS}
                    >
                      No Pilot accounts yet.
                    </TableCell>
                  </TableRow>
                ) : null}
                {accounts.map((account) => {
                  const row = buildPilotRowPresentation(account, now)
                  return (
                    <TableRow
                      key={account.restaurantId}
                      className={GUESTS_TABLE_BODY_ROW_CLASS}
                    >
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                          {account.restaurantName}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {account.ownerEmail}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <Badge variant="outline">{account.billingStatus}</Badge>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {row.startLabel}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {row.originalEndLabel}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {row.endLabel}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {row.extensionsLabel}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <div className="flex items-center gap-2">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => setDetail(account)}
                          >
                            View
                          </Button>
                          {row.canExtend ? (
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => openDialog(account)}
                            >
                              Extend Pilot
                            </Button>
                          ) : (
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <span className="inline-flex">
                                  <Button variant="outline" size="sm" disabled>
                                    Extend Pilot
                                  </Button>
                                </span>
                              </TooltipTrigger>
                              <TooltipContent>{row.disabledReason}</TooltipContent>
                            </Tooltip>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          </TooltipProvider>
        </div>
      </section>

      <AdminPilotDetailDialog
        account={detail}
        onClose={() => setDetail(null)}
      />

      <Dialog
        open={dialog !== null}
        onOpenChange={(open) => {
          if (!open && !extending) setDialog(null)
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Extend Pilot</DialogTitle>
            <DialogDescription>
              {dialog
                ? `Add days to the Pilot for ${dialog.account.restaurantName}. Each extension is final.`
                : ""}
            </DialogDescription>
          </DialogHeader>
          {dialog && preview && dialogPresentation ? (
            <div className="flex flex-col gap-4">
              <dl className="grid grid-cols-2 gap-x-4 gap-y-2 text-sm">
                <dt className="text-muted-foreground">Current range</dt>
                <dd>
                  {formatPilotDate(dialog.account.pilotStartedAtUtc)} to{" "}
                  {dialogPresentation.endLabel}
                </dd>
                <dt className="text-muted-foreground">Days used</dt>
                <dd>{`${dialog.account.daysUsed} days`}</dd>
                <dt className="text-muted-foreground">Prior extensions</dt>
                <dd>{dialogPresentation.extensionsLabel}</dd>
                <dt className="text-muted-foreground">New end</dt>
                <dd>
                  {preview.newEndLabel}
                  <span className="block text-muted-foreground">
                    {preview.countsFrom === "current-end"
                      ? "Adds to the current end."
                      : "Counts from today, because the current end has passed."}
                  </span>
                </dd>
              </dl>

              <div className="flex flex-col gap-2">
                <Label htmlFor="pilot-extend-days">Extension length</Label>
                <Select
                  value={String(dialog.days)}
                  disabled={dialog.attempted}
                  onValueChange={(value) => {
                    const days = PILOT_EXTENSION_DAY_OPTIONS.find(
                      (option) => String(option) === value
                    )
                    if (days !== undefined) {
                      setDialog((current) =>
                        current ? { ...current, days } : current
                      )
                    }
                  }}
                >
                  <SelectTrigger id="pilot-extend-days">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent
                    position="popper"
                    align="start"
                    className={DIALOG_SELECT_CONTENT_CLASS}
                  >
                    {PILOT_EXTENSION_DAY_OPTIONS.map((days) => (
                      <SelectItem key={days} value={String(days)}>
                        {days} days
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="pilot-extend-reason">Reason</Label>
                <Textarea
                  id="pilot-extend-reason"
                  value={dialog.reason}
                  disabled={dialog.attempted}
                  aria-invalid={dialog.reason.length > 0 && reasonError !== ""}
                  placeholder="Why this Pilot needs more time"
                  onChange={(event) => {
                    const reason = event.target.value
                    setDialog((current) =>
                      current ? { ...current, reason } : current
                    )
                  }}
                />
                {dialog.reason.length > 0 && reasonError ? (
                  <p className="text-xs text-destructive">{reasonError}</p>
                ) : null}
              </div>

              {dialog.error ? (
                <p role="alert" className="text-sm text-destructive">
                  {dialog.error}
                </p>
              ) : null}
            </div>
          ) : null}
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setDialog(null)}
              disabled={extending}
            >
              Cancel
            </Button>
            <Button
              onClick={handleExtend}
              disabled={extending || !dialog || reasonError !== ""}
            >
              {extending ? "Extending…" : "Extend"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

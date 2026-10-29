import { useEffect, useState } from "react"
import axios from "axios"
import { toast } from "sonner"

import { getPremisesReviews, resolvePremisesReview } from "@/api/adminApi"
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
import { Textarea } from "@/components/ui/textarea"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  canSubmitPremisesReviewReason,
  describePremisesMatch,
  formatMatchAddress,
  PREMISES_REVIEW_ACTION_DONE,
  PREMISES_REVIEW_ACTION_LABELS,
  PREMISES_REVIEW_REASON_MAX_LENGTH,
} from "@/lib/admin/adminPremisesReviewPresentation"
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
import type {
  AdminPremisesReviewAction,
  AdminPremisesReviewItem,
} from "@/types/adminPremisesReview"

type ResolveDialogState = {
  item: AdminPremisesReviewItem
  action: AdminPremisesReviewAction
  reason: string
}

function apiErrorMessage(error: unknown, fallback: string) {
  if (axios.isAxiosError(error)) {
    const message = (error.response?.data as { message?: string } | undefined)
      ?.message
    if (message) return message
  }
  return fallback
}

/**
 * Locations saved as Draft for a possible duplicate premises. Admin is
 * cross-tenant staff, so the matched Locations show here and nowhere else.
 */
export function AdminPremisesReviewPanel() {
  const [items, setItems] = useState<AdminPremisesReviewItem[]>([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState(false)
  const [dialog, setDialog] = useState<ResolveDialogState | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    let cancelled = false
    getPremisesReviews()
      .then((rows) => {
        if (!cancelled) setItems(rows)
      })
      .catch(() => {
        if (!cancelled) {
          setLoadError(true)
          toast.error("Could not load premises reviews")
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const handleResolve = async () => {
    if (!dialog || saving || !canSubmitPremisesReviewReason(dialog.reason)) {
      return
    }
    setSaving(true)
    try {
      await resolvePremisesReview(
        dialog.item.locationId,
        dialog.action,
        dialog.reason.trim()
      )
      setItems((rows) =>
        rows.filter((row) => row.locationId !== dialog.item.locationId)
      )
      toast.success(PREMISES_REVIEW_ACTION_DONE[dialog.action])
      setDialog(null)
    } catch (error) {
      toast.error(apiErrorMessage(error, "Could not save the review"))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <header className="flex flex-col gap-3.5 leading-[0]">
        <h1 className={GUESTS_PAGE_TITLE_CLASS}>Premises review</h1>
        <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
          Locations saved as Draft because their premises may already be
          registered. Clear a Location to let the operator activate it, or
          reject it to keep it blocked. A reason is required.
        </p>
      </header>

      <section className={GUESTS_SECTION_CLASS}>
        <div className="flex flex-col gap-1">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>Waiting for review</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            {loading
              ? "Loading premises reviews…"
              : loadError
                ? "Could not load premises reviews"
                : `${items.length} location${items.length === 1 ? "" : "s"}`}
          </p>
        </div>

        <div className={GUESTS_TABLE_FRAME_CLASS}>
          <Table className={GUESTS_TABLE_CLASS}>
            <TableHeader className="[&_tr]:border-0">
              <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Location
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Workspace
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Matched locations
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
                    colSpan={4}
                    className={GUESTS_TABLE_BODY_CELL_CLASS}
                  >
                    Could not load premises reviews. Reload the page to try
                    again.
                  </TableCell>
                </TableRow>
              ) : null}
              {!loading && !loadError && items.length === 0 ? (
                <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                  <TableCell
                    colSpan={4}
                    className={GUESTS_TABLE_BODY_CELL_CLASS}
                  >
                    No locations are waiting for review.
                  </TableCell>
                </TableRow>
              ) : null}
              {items.map((item) => (
                <TableRow
                  key={item.locationId}
                  className={GUESTS_TABLE_BODY_ROW_CLASS}
                >
                  <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    <div className="flex flex-col gap-1">
                      <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                        {item.locationName}
                      </span>
                      <span>
                        {[item.address, item.city, item.postcode]
                          .filter(Boolean)
                          .join(", ")}
                      </span>
                      {item.fsaEstablishmentId ? (
                        <span>FSA id {item.fsaEstablishmentId}</span>
                      ) : null}
                    </div>
                  </TableCell>
                  <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    {item.workspaceName}
                  </TableCell>
                  <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    {item.matches.length === 0 ? (
                      <span>No matched location on file</span>
                    ) : (
                      <ul className="m-0 flex list-none flex-col gap-2 p-0">
                        {item.matches.map((match) => (
                          <li
                            key={match.locationId}
                            className="flex flex-col gap-1"
                          >
                            <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                              {match.locationName ?? "Location removed"} ·{" "}
                              {match.workspaceName}
                            </span>
                            <span>{formatMatchAddress(match)}</span>
                            <span className="flex flex-wrap gap-1">
                              {describePremisesMatch(match).map((tag) => (
                                <Badge key={tag} variant="outline">
                                  {tag}
                                </Badge>
                              ))}
                            </span>
                          </li>
                        ))}
                      </ul>
                    )}
                  </TableCell>
                  <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    <div className="flex gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() =>
                          setDialog({ item, action: "clear", reason: "" })
                        }
                      >
                        {PREMISES_REVIEW_ACTION_LABELS.clear}
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() =>
                          setDialog({ item, action: "reject", reason: "" })
                        }
                      >
                        {PREMISES_REVIEW_ACTION_LABELS.reject}
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </section>

      <Dialog
        open={dialog !== null}
        onOpenChange={(open) => {
          if (!open && !saving) setDialog(null)
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {dialog ? PREMISES_REVIEW_ACTION_LABELS[dialog.action] : ""}{" "}
              premises
            </DialogTitle>
            <DialogDescription>
              {dialog
                ? `${dialog.item.locationName} · ${dialog.item.workspaceName}`
                : ""}
            </DialogDescription>
          </DialogHeader>
          {dialog ? (
            <div className="flex flex-col gap-2">
              <Label htmlFor="premises-review-reason">Reason</Label>
              <Textarea
                id="premises-review-reason"
                value={dialog.reason}
                maxLength={PREMISES_REVIEW_REASON_MAX_LENGTH}
                disabled={saving}
                onChange={(event) =>
                  setDialog((current) =>
                    current
                      ? { ...current, reason: event.target.value }
                      : current
                  )
                }
              />
            </div>
          ) : null}
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setDialog(null)}
              disabled={saving}
            >
              Cancel
            </Button>
            <Button
              onClick={() => void handleResolve()}
              disabled={
                saving ||
                dialog === null ||
                !canSubmitPremisesReviewReason(dialog.reason)
              }
            >
              {dialog ? PREMISES_REVIEW_ACTION_LABELS[dialog.action] : ""}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

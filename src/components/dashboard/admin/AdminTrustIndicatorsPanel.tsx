import { useCallback, useEffect, useState } from "react"
import { toast } from "sonner"

import {
  fetchAdminTrustIndicators,
  publishTrustIndicators,
  saveTrustIndicatorItem,
  submitTrustIndicatorDraft,
  TrustIndicatorApiError,
  type AdminTrustIndicatorItem,
  type AdminTrustIndicatorsView,
  type TrustCtaType,
  type TrustIndicatorStatus,
  type TrustIndicatorValues,
} from "@/api/adminTrustIndicatorsApi"
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
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import { FloatingLabelTextarea } from "@/components/ui/floating-label-textarea"
import { Label } from "@/components/ui/label"
import { Skeleton } from "@/components/ui/skeleton"
import { Switch } from "@/components/ui/switch"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  allowedTrustCtaTypes,
  allowedTrustStatuses,
  nextCtaTargetOnTypeChange,
  TRUST_CTA_TYPE_LABELS,
  TRUST_EDITOR_RELOAD_FAILED_MESSAGE,
  TRUST_EVIDENCE_CLASS_LABELS,
  TRUST_STATUS_LABELS,
  trustDraftChanges,
  trustItemPublishState,
  trustPublishErrorMessage,
  trustSaveErrorMessage,
  toTrustSaveValues,
  type TrustItemPublishState,
} from "@/lib/admin/adminTrustIndicators"
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
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { OPERATOR_RIGHT_DRAWER_CONTENT_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"

const DRAWER_SELECT_CONTENT_CLASS = "z-[150]"

const PUBLISH_STATE_LABELS: Record<TrustItemPublishState, string> = {
  not_published: "Not published",
  unchanged: "Matches live",
  changed: "Unpublished changes",
}

const ONLY_APPROVER_MESSAGE = "Only a Director or compliance approver can publish."
const SUBMIT_FIRST_MESSAGE = "Submit the draft for review first."

type PublishError = {
  message: string
  details: string[]
}

function formatDateTime(value: string | null): string {
  if (!value) return "—"
  return new Date(value).toLocaleString("en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "Europe/London",
  })
}

function messageFor(error: unknown, fallback: string): string {
  if (error instanceof TrustIndicatorApiError) {
    if (error.status === 403) {
      return "You do not have permission to manage trust indicators."
    }
    return error.message || fallback
  }
  return fallback
}

/** The reload message when the server reports a stale draft; null for other errors. */
function draftConflictMessage(error: unknown): string | null {
  if (!(error instanceof TrustIndicatorApiError)) return null
  const mapped = trustPublishErrorMessage(error.code ?? "", error.message)
  return mapped.reload ? mapped.message : null
}

export function AdminTrustIndicatorsPanel() {
  const [view, setView] = useState<AdminTrustIndicatorsView | null>(null)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const [editing, setEditing] = useState<AdminTrustIndicatorItem | null>(null)
  const [form, setForm] = useState<TrustIndicatorValues | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [formDetails, setFormDetails] = useState<string[]>([])
  const [saving, setSaving] = useState(false)

  const [publishOpen, setPublishOpen] = useState(false)
  const [reason, setReason] = useState("")
  const [idempotencyKey, setIdempotencyKey] = useState("")
  const [publishing, setPublishing] = useState(false)
  const [publishError, setPublishError] = useState<PublishError | null>(null)

  const refreshView = useCallback(async () => {
    const next = await fetchAdminTrustIndicators()
    setView(next)
    return next
  }, [])

  const reloadAfterConflict = useCallback(async () => {
    try {
      await refreshView()
    } catch {
      // The shown message still applies. The server refuses a stale publish, so a later reload is safe.
    }
  }, [refreshView])

  const loadView = useCallback(async () => {
    setLoading(true)
    setLoadError(null)
    try {
      await refreshView()
    } catch (error) {
      setLoadError(messageFor(error, "Could not load trust indicators."))
    } finally {
      setLoading(false)
    }
  }, [refreshView])

  useEffect(() => {
    let cancelled = false
    fetchAdminTrustIndicators()
      .then((next) => {
        if (!cancelled) setView(next)
      })
      .catch((error: unknown) => {
        if (!cancelled) {
          setLoadError(messageFor(error, "Could not load trust indicators."))
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  function openEditor(item: AdminTrustIndicatorItem) {
    setEditing(item)
    setForm({ ...item.values })
    setFormError(null)
    setFormDetails([])
  }

  function closeEditor() {
    setEditing(null)
    setForm(null)
  }

  function setField<K extends keyof TrustIndicatorValues>(
    key: K,
    value: TrustIndicatorValues[K]
  ) {
    setForm((current) => (current ? { ...current, [key]: value } : current))
  }

  /** The target rule lives in nextCtaTargetOnTypeChange; None also drops the label. */
  function changeCtaType(ctaType: TrustCtaType) {
    setForm((current) =>
      current
        ? {
            ...current,
            ctaType,
            ctaTarget: nextCtaTargetOnTypeChange(
              current.ctaType,
              ctaType,
              current.ctaTarget
            ),
            ctaLabel: ctaType === "None" ? null : current.ctaLabel,
          }
        : current
    )
  }

  /** A status that does not allow the current CTA type resets the CTA to None. */
  function changeStatus(status: TrustIndicatorStatus) {
    if (!editing) return
    const allowed = allowedTrustCtaTypes(editing.evidenceClass, status)
    setForm((current) => {
      if (!current) return current
      if (allowed.includes(current.ctaType)) return { ...current, status }
      return { ...current, status, ctaType: "None", ctaLabel: null, ctaTarget: null }
    })
  }

  /** Reloads the view and resets the open editor to the item's latest values. */
  async function resetEditorToLatest(itemNumber: number, message: string) {
    try {
      const latest = await refreshView()
      const row = latest.draft.find((item) => item.itemNumber === itemNumber)
      if (row !== undefined) {
        setEditing(row)
        setForm({ ...row.values })
        setFormError(message)
        setFormDetails([])
        return
      }
    } catch {
      // Fall through: the user's values stay, with the failure message.
    }
    setFormError(TRUST_EDITOR_RELOAD_FAILED_MESSAGE)
    setFormDetails([])
  }

  async function saveDraftItem() {
    if (!editing || !form) return
    const { itemNumber } = editing
    setSaving(true)
    setFormError(null)
    setFormDetails([])
    try {
      await saveTrustIndicatorItem(itemNumber, toTrustSaveValues(form))
      await refreshView()
      toast.success("Draft saved. The live website is unchanged.")
      closeEditor()
    } catch (error) {
      const code = error instanceof TrustIndicatorApiError ? (error.code ?? "") : ""
      const mapped = trustSaveErrorMessage(
        code,
        messageFor(error, "Could not save the draft item.")
      )
      if (mapped.reload) {
        await resetEditorToLatest(itemNumber, mapped.message)
      } else {
        setFormError(mapped.message)
        if (error instanceof TrustIndicatorApiError) {
          setFormDetails(error.details)
        }
      }
    } finally {
      setSaving(false)
    }
  }

  async function submitDraft() {
    setSubmitting(true)
    try {
      await submitTrustIndicatorDraft()
      await refreshView()
      toast.success("Draft submitted for review.")
    } catch (error) {
      const conflict = draftConflictMessage(error)
      if (conflict !== null) {
        toast.error(conflict)
        await reloadAfterConflict()
      } else {
        toast.error(messageFor(error, "Could not submit the draft for review."))
      }
    } finally {
      setSubmitting(false)
    }
  }

  function openPublish() {
    setReason("")
    setPublishError(null)
    setIdempotencyKey(crypto.randomUUID())
    setPublishOpen(true)
  }

  /** Maps a refused publish. Reloads the view on a stale draft; a reused key gets a new one. */
  async function publishFailure(error: unknown): Promise<PublishError> {
    if (!(error instanceof TrustIndicatorApiError)) {
      return { message: "Could not publish the trust indicators.", details: [] }
    }
    const mapped = trustPublishErrorMessage(error.code ?? "", error.message)
    if (error.code === "idempotency_key_reused") {
      setIdempotencyKey(crypto.randomUUID())
    }
    if (mapped.reload) {
      await reloadAfterConflict()
    }
    return { message: mapped.message, details: error.details }
  }

  async function publishDraft() {
    const trimmed = reason.trim()
    if (trimmed === "") {
      setPublishError({ message: "Enter a reason for publishing.", details: [] })
      return
    }
    const revision = view?.review.draftRevision ?? null
    if (revision === null) {
      setPublishError({ message: SUBMIT_FIRST_MESSAGE, details: [] })
      return
    }
    setPublishing(true)
    setPublishError(null)
    try {
      const published = await publishTrustIndicators({
        reason: trimmed,
        draftRevision: revision,
        idempotencyKey,
      })
      await refreshView()
      toast.success(`Published trust indicators version ${published.version}.`)
      setPublishOpen(false)
    } catch (error) {
      setPublishError(await publishFailure(error))
    } finally {
      setPublishing(false)
    }
  }

  if (loading && view === null) {
    return (
      <div className={GUESTS_PAGE_STACK_CLASS} aria-busy="true">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-96 w-full" />
      </div>
    )
  }

  if (loadError !== null || view === null) {
    return (
      <div className={GUESTS_PAGE_STACK_CLASS} role="alert">
        <p className={GUESTS_PAGE_SUBTITLE_CLASS}>{loadError}</p>
        <div>
          <Button type="button" variant="outline" onClick={() => void loadView()}>
            Try again
          </Button>
        </div>
      </div>
    )
  }

  const published = view.published
  const publishedItems = published?.items ?? null
  const changedCount = trustDraftChanges(view.draft, publishedItems).length
  const submitted = view.review.submittedByAdminUserId !== null
  const draftRevision = view.review.draftRevision
  const enabledDraftCount = view.draft.filter((row) => row.values.enabled).length
  const editorStatusOptions =
    editing === null
      ? []
      : allowedTrustStatuses(editing.evidenceClass).map((value) => ({
          value,
          label: TRUST_STATUS_LABELS[value],
        }))
  const editorCtaOptions =
    editing === null || form === null
      ? []
      : allowedTrustCtaTypes(editing.evidenceClass, form.status).map((value) => ({
          value,
          label: TRUST_CTA_TYPE_LABELS[value],
        }))

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div>
        <h1 className={GUESTS_PAGE_TITLE_CLASS}>Trust indicators</h1>
        <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
          Edit the 15 indicators as a draft, submit it for review, then have a
          second admin publish. Saving a draft never changes the live website.
        </p>
      </div>

      <section className={GUESTS_SECTION_CLASS}>
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="flex flex-col gap-1">
            <h2 className={GUESTS_SECTION_TITLE_CLASS}>
              {published
                ? `Live version ${published.version}`
                : "Nothing published yet"}
            </h2>
            <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
              {published
                ? `Published ${formatDateTime(published.publishedAtUtc)} · Reason: ${published.reason}`
                : "The public website shows no trust indicators until a version is published."}
            </p>
            <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
              {submitted
                ? `Submitted for review by admin ${view.review.submittedByAdminUserId} at ${formatDateTime(view.review.submittedAtUtc)}.`
                : "Not submitted for review."}
              {changedCount > 0
                ? ` ${changedCount} draft ${changedCount === 1 ? "item differs" : "items differ"} from the live version.`
                : ""}
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={submitting}
              onClick={() => void submitDraft()}
            >
              {submitting ? "Submitting…" : "Submit for review"}
            </Button>
            {view.viewer.canApprove ? (
              <div className="flex flex-col items-end gap-1">
                <Button
                  type="button"
                  disabled={draftRevision === null}
                  onClick={openPublish}
                >
                  Publish…
                </Button>
                {draftRevision === null ? (
                  <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{SUBMIT_FIRST_MESSAGE}</p>
                ) : null}
              </div>
            ) : (
              <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{ONLY_APPROVER_MESSAGE}</p>
            )}
          </div>
        </div>
      </section>

      <section className={GUESTS_SECTION_CLASS}>
        <div className={GUESTS_TABLE_FRAME_CLASS}>
          <Table className={GUESTS_TABLE_CLASS}>
            <TableHeader>
              <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>#</TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Indicator</TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Evidence class</TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Status</TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Website</TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Versus live</TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  <span className="sr-only">Actions</span>
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {view.draft.length === 0 ? (
                <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                  <TableCell colSpan={7} className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    No trust indicators are set up yet.
                  </TableCell>
                </TableRow>
              ) : (
                view.draft.map((row) => {
                  const state = trustItemPublishState(
                    row,
                    publishedItems?.find((live) => live.itemNumber === row.itemNumber)
                  )
                  return (
                    <TableRow
                      key={row.itemNumber}
                      className={GUESTS_TABLE_BODY_ROW_CLASS}
                      data-testid={`trust-row-${row.itemNumber}`}
                    >
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {String(row.itemNumber).padStart(2, "0")}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {row.title}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {TRUST_EVIDENCE_CLASS_LABELS[row.evidenceClass]}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {TRUST_STATUS_LABELS[row.values.status]}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <Badge variant={row.values.enabled ? "secondary" : "outline"}>
                          {row.values.enabled ? "On" : "Off"}
                        </Badge>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <Badge variant={state === "changed" ? "secondary" : "outline"}>
                          {PUBLISH_STATE_LABELS[state]}
                        </Badge>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => openEditor(row)}
                        >
                          Edit draft
                        </Button>
                      </TableCell>
                    </TableRow>
                  )
                })
              )}
            </TableBody>
          </Table>
        </div>
      </section>

      <Drawer
        open={editing != null}
        onOpenChange={(open) => {
          if (!open) closeEditor()
        }}
        direction="right"
      >
        <DrawerContent className={OPERATOR_RIGHT_DRAWER_CONTENT_CLASS}>
          {editing && form ? (
            <div className="flex min-h-0 flex-1 flex-col overflow-y-auto px-[22px] py-8">
              <div className="mb-6 flex items-start justify-between gap-4">
                <div>
                  <DrawerTitle className="text-2xl font-bold">
                    {String(editing.itemNumber).padStart(2, "0")} {editing.title}
                  </DrawerTitle>
                  <DrawerDescription>
                    {TRUST_EVIDENCE_CLASS_LABELS[editing.evidenceClass]}
                  </DrawerDescription>
                </div>
                <DrawerClose asChild>
                  <Button type="button" variant="ghost" size="sm">
                    Close
                  </Button>
                </DrawerClose>
              </div>

              <div className="flex flex-col gap-4">
                <div className="flex items-center gap-3">
                  <Switch
                    id="trust-enabled"
                    checked={form.enabled}
                    onCheckedChange={(checked) => setField("enabled", checked)}
                  />
                  <Label htmlFor="trust-enabled">Show on website</Label>
                </div>
                <FloatingLabelSelect
                  label="Status"
                  value={form.status}
                  options={editorStatusOptions}
                  contentClassName={DRAWER_SELECT_CONTENT_CLASS}
                  onValueChange={(value) => changeStatus(value as TrustIndicatorStatus)}
                />
                <FloatingLabelInput
                  label="Status label (max 60)"
                  maxLength={60}
                  value={form.statusLabel}
                  onChange={(event) => setField("statusLabel", event.target.value)}
                />
                <FloatingLabelSelect
                  label="CTA type"
                  value={form.ctaType}
                  options={editorCtaOptions}
                  contentClassName={DRAWER_SELECT_CONTENT_CLASS}
                  onValueChange={(value) => changeCtaType(value as TrustCtaType)}
                />
                {form.ctaType !== "None" ? (
                  <FloatingLabelInput
                    label="CTA label (max 60)"
                    maxLength={60}
                    value={form.ctaLabel ?? ""}
                    onChange={(event) => setField("ctaLabel", event.target.value)}
                  />
                ) : null}
                {form.ctaType === "ExternalProof" ? (
                  <FloatingLabelInput
                    label="CTA target (the evidence URL, https)"
                    value={form.ctaTarget ?? ""}
                    onChange={(event) => setField("ctaTarget", event.target.value)}
                  />
                ) : null}
                {form.ctaType === "InternalPage" ? (
                  <FloatingLabelInput
                    label="CTA target (a path starting with /)"
                    value={form.ctaTarget ?? ""}
                    onChange={(event) => setField("ctaTarget", event.target.value)}
                  />
                ) : null}
                <FloatingLabelInput
                  label="Evidence URL (https)"
                  value={form.evidenceUrl ?? ""}
                  onChange={(event) => setField("evidenceUrl", event.target.value)}
                />
                <FloatingLabelInput
                  label="Image URL (https, or a path starting with /)"
                  value={form.imageUrl ?? ""}
                  onChange={(event) => setField("imageUrl", event.target.value)}
                />
                <FloatingLabelInput
                  label="Icon key"
                  value={form.iconKey ?? ""}
                  onChange={(event) => setField("iconKey", event.target.value)}
                />
                <FloatingLabelTextarea
                  label="Image usage rights (required with an image, max 300)"
                  maxLength={300}
                  value={form.imageRightsNote ?? ""}
                  onChange={(event) => setField("imageRightsNote", event.target.value)}
                />
                <FloatingLabelInput
                  label="Alt text (required with an image)"
                  value={form.altText}
                  onChange={(event) => setField("altText", event.target.value)}
                />
                <FloatingLabelTextarea
                  label="Tooltip"
                  value={form.tooltip}
                  onChange={(event) => setField("tooltip", event.target.value)}
                />
                <FloatingLabelInput
                  label="Issuer"
                  value={form.issuer ?? ""}
                  onChange={(event) => setField("issuer", event.target.value)}
                />
                <FloatingLabelInput
                  label="Scope"
                  value={form.scope ?? ""}
                  onChange={(event) => setField("scope", event.target.value)}
                />
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                  <FloatingLabelInput
                    label="Issued on"
                    type="date"
                    value={form.issuedOn ?? ""}
                    onChange={(event) => setField("issuedOn", event.target.value)}
                  />
                  <FloatingLabelInput
                    label="Expires on"
                    type="date"
                    value={form.expiresOn ?? ""}
                    onChange={(event) => setField("expiresOn", event.target.value)}
                  />
                  <FloatingLabelInput
                    label="Review on"
                    type="date"
                    value={form.reviewOn ?? ""}
                    onChange={(event) => setField("reviewOn", event.target.value)}
                  />
                </div>

                {formError !== null ? (
                  <div role="alert" className="flex flex-col gap-1 text-sm text-destructive">
                    <p>{formError}</p>
                    {formDetails.map((line) => (
                      <p key={line}>{line}</p>
                    ))}
                  </div>
                ) : null}

                <div className="flex justify-end gap-2">
                  <DrawerClose asChild>
                    <Button type="button" variant="ghost">
                      Cancel
                    </Button>
                  </DrawerClose>
                  <Button
                    type="button"
                    disabled={saving}
                    onClick={() => void saveDraftItem()}
                  >
                    {saving ? "Saving…" : "Save draft"}
                  </Button>
                </div>
              </div>
            </div>
          ) : null}
        </DrawerContent>
      </Drawer>

      <Dialog open={publishOpen} onOpenChange={setPublishOpen}>
        <DialogContent className="gap-6 bg-white sm:max-w-[560px]">
          <DialogHeader className="gap-2">
            <DialogTitle>Publish trust indicators</DialogTitle>
            <DialogDescription>
              {published
                ? `This makes version ${published.version + 1} live. Review the changes against version ${published.version} first.`
                : "This makes version 1 live. Only enabled items that pass the evidence checks appear on the website."}
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-2">
            <h3 className={GUESTS_SECTION_TITLE_CLASS}>
              {published ? "Changes from the live version" : "Items that will show"}
            </h3>
            {published === null ? (
              enabledDraftCount === 0 ? (
                <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
                  No items are enabled. Publishing will show an empty list.
                </p>
              ) : (
                <ul className="list-disc pl-5 text-sm">
                  {view.draft
                    .filter((row) => row.values.enabled)
                    .map((row) => (
                      <li key={row.itemNumber}>{row.title}</li>
                    ))}
                </ul>
              )
            ) : changedCount === 0 ? (
              <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
                The draft matches the live version. Publishing creates a new version with the same content.
              </p>
            ) : (
              <ul className="flex flex-col gap-3 text-sm">
                {trustDraftChanges(view.draft, publishedItems).map(({ item, differences }) => (
                  <li key={item.itemNumber}>
                    <p className="font-semibold">
                      {String(item.itemNumber).padStart(2, "0")} {item.title}
                    </p>
                    <ul className="pl-4">
                      {differences.map((difference) => (
                        <li key={difference.field}>
                          {difference.label}: {difference.published} → {difference.draft}
                        </li>
                      ))}
                    </ul>
                  </li>
                ))}
              </ul>
            )}
          </div>

          <FloatingLabelTextarea
            label="Reason for publishing (required)"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            maxLength={500}
          />

          {publishError !== null ? (
            <div role="alert" className="flex flex-col gap-1 text-sm text-destructive">
              <p>{publishError.message}</p>
              {publishError.details.map((line) => (
                <p key={line}>{line}</p>
              ))}
            </div>
          ) : null}

          {draftRevision === null ? (
            <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{SUBMIT_FIRST_MESSAGE}</p>
          ) : null}

          <DialogFooter>
            <Button
              type="button"
              variant="ghost"
              onClick={() => setPublishOpen(false)}
            >
              Cancel
            </Button>
            <Button
              type="button"
              disabled={publishing || draftRevision === null}
              onClick={() => void publishDraft()}
            >
              {publishing ? "Publishing…" : "Publish"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

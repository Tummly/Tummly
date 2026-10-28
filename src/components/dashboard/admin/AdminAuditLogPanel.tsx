import {
  Fragment,
  useEffect,
  useMemo,
  useState,
  type ChangeEvent,
  type FormEvent,
} from "react"
import axios from "axios"

import { getAdminAuditEvents } from "@/api/adminApi"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  auditActorLabel,
  auditResultLabel,
  auditTargetLabel,
  buildAdminAuditQuery,
  EMPTY_AUDIT_FILTERS,
  formatAuditDetail,
  type AdminAuditFilterInput,
  type AdminAuditQueryParams,
  auditPageRangeText,
} from "@/lib/admin/adminAuditPresentation"
import { formatLondonDateTime } from "@/lib/admin/londonTime"
import {
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { AdminAuditEvent } from "@/types/adminOps"

const PAGE_SIZE = 50
const COLUMN_COUNT = 7

type AuditOutcome =
  | { status: "ready"; rows: AdminAuditEvent[]; totalCount: number }
  | { status: "forbidden" }
  | { status: "error" }

function auditOutcomeFromError(error: unknown): AuditOutcome {
  if (axios.isAxiosError(error) && error.response?.status === 403) {
    return { status: "forbidden" }
  }
  return { status: "error" }
}

function requestKeyFor(params: AdminAuditQueryParams, reloadCount: number) {
  return `${JSON.stringify(params)}#${reloadCount}`
}

export function AdminAuditLogPanel() {
  const [draft, setDraft] = useState<AdminAuditFilterInput>(EMPTY_AUDIT_FILTERS)
  const [applied, setApplied] = useState<AdminAuditFilterInput>(EMPTY_AUDIT_FILTERS)
  const [skip, setSkip] = useState(0)
  const [reloadCount, setReloadCount] = useState(0)
  const [formError, setFormError] = useState<string | null>(null)
  const [expandedIds, setExpandedIds] = useState<ReadonlySet<string>>(() => new Set())
  const [loaded, setLoaded] = useState<{ key: string; outcome: AuditOutcome } | null>(null)

  const query = useMemo(
    () => buildAdminAuditQuery(applied, { skip, take: PAGE_SIZE }),
    [applied, skip]
  )

  useEffect(() => {
    if (!query.ok) {
      return
    }

    const key = requestKeyFor(query.params, reloadCount)
    let active = true
    getAdminAuditEvents(query.params).then(
      (response) => {
        if (active) {
          setLoaded({
            key,
            outcome: {
              status: "ready",
              rows: response.data,
              totalCount: response.totalCount,
            },
          })
        }
      },
      (error: unknown) => {
        if (active) {
          setLoaded({ key, outcome: auditOutcomeFromError(error) })
        }
      }
    )

    return () => {
      active = false
    }
  }, [query, reloadCount])

  // Results from an older request are ignored, so the table shows loading until its own request returns.
  const currentKey = query.ok ? requestKeyFor(query.params, reloadCount) : null
  const outcome = loaded && loaded.key === currentKey ? loaded.outcome : null

  const applyFilters = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const check = buildAdminAuditQuery(draft, { skip: 0, take: PAGE_SIZE })
    if (!check.ok) {
      setFormError(check.error)
      return
    }
    setFormError(null)
    setApplied(draft)
    setSkip(0)
    setExpandedIds(new Set())
  }

  const clearFilters = () => {
    setDraft(EMPTY_AUDIT_FILTERS)
    setApplied(EMPTY_AUDIT_FILTERS)
    setSkip(0)
    setFormError(null)
    setExpandedIds(new Set())
  }

  const toggleExpanded = (id: string) => {
    setExpandedIds((current) => {
      const next = new Set(current)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }

  const updateDraft = (field: keyof AdminAuditFilterInput) => (
    event: ChangeEvent<HTMLInputElement>
  ) => {
    setDraft((current) => ({ ...current, [field]: event.target.value }))
  }

  return (
    <section className={GUESTS_PAGE_STACK_CLASS} aria-labelledby="admin-audit-title">
      <div className="flex flex-col gap-1">
        <h2 id="admin-audit-title" className={GUESTS_PAGE_TITLE_CLASS}>
          Audit log
        </h2>
        <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
          Admin actions with time, actor, target, result and request correlation ID.
        </p>
      </div>

      <form
        className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5"
        onSubmit={applyFilters}
        aria-label="Audit log filters"
      >
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="audit-filter-action">Action</Label>
          <Input
            id="audit-filter-action"
            value={draft.action}
            onChange={updateDraft("action")}
            placeholder="e.g. payment.refund"
            autoComplete="off"
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="audit-filter-restaurant">Restaurant ID</Label>
          <Input
            id="audit-filter-restaurant"
            inputMode="numeric"
            value={draft.restaurantId}
            onChange={updateDraft("restaurantId")}
            autoComplete="off"
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="audit-filter-target">Target ID</Label>
          <Input
            id="audit-filter-target"
            value={draft.targetId}
            onChange={updateDraft("targetId")}
            autoComplete="off"
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="audit-filter-from">From (London date)</Label>
          <Input
            id="audit-filter-from"
            type="date"
            value={draft.from}
            onChange={updateDraft("from")}
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="audit-filter-to">To (London date)</Label>
          <Input
            id="audit-filter-to"
            type="date"
            value={draft.to}
            onChange={updateDraft("to")}
          />
        </div>
        <div className="flex flex-wrap items-center gap-2 sm:col-span-2 xl:col-span-5">
          <Button type="submit" size="sm">
            Apply filters
          </Button>
          <Button type="button" variant="outline" size="sm" onClick={clearFilters}>
            Clear filters
          </Button>
          {formError && (
            <p role="alert" className="text-sm text-destructive">
              {formError}
            </p>
          )}
        </div>
      </form>

      <AuditResults
        outcome={outcome}
        skip={skip}
        expandedIds={expandedIds}
        onToggle={toggleExpanded}
        onRetry={() => setReloadCount((count) => count + 1)}
        onPrevious={() => setSkip((current) => Math.max(0, current - PAGE_SIZE))}
        onNext={() => setSkip((current) => current + PAGE_SIZE)}
      />
    </section>
  )
}

function AuditResults({
  outcome,
  skip,
  expandedIds,
  onToggle,
  onRetry,
  onPrevious,
  onNext,
}: {
  outcome: AuditOutcome | null
  skip: number
  expandedIds: ReadonlySet<string>
  onToggle: (id: string) => void
  onRetry: () => void
  onPrevious: () => void
  onNext: () => void
}) {
  if (outcome === null) {
    return (
      <div className="flex flex-col gap-2" aria-busy="true">
        {Array.from({ length: 4 }, (_, index) => (
          <Skeleton key={index} className="h-10 w-full" />
        ))}
      </div>
    )
  }

  if (outcome.status === "forbidden") {
    return (
      <Alert variant="destructive">
        <AlertTitle>Audit log not available</AlertTitle>
        <AlertDescription>You do not have permission to view the audit log.</AlertDescription>
      </Alert>
    )
  }

  if (outcome.status === "error") {
    return (
      <Alert variant="destructive">
        <AlertTitle>Could not load the audit log</AlertTitle>
        <AlertDescription className="flex flex-col gap-2">
          <span>Try again. Nothing has been changed.</span>
          <Button variant="outline" size="sm" className="w-fit" onClick={onRetry}>
            Retry
          </Button>
        </AlertDescription>
      </Alert>
    )
  }

  if (outcome.totalCount === 0) {
    return <p className="text-sm text-muted-foreground">No audit events match these filters.</p>
  }

  return (
    <div className="flex flex-col gap-3">
      <div className={GUESTS_TABLE_FRAME_CLASS}>
        <Table className={GUESTS_TABLE_CLASS}>
          <TableHeader>
            <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Time</TableHead>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Action</TableHead>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Actor</TableHead>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Target</TableHead>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Restaurant</TableHead>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Result</TableHead>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                <span className="sr-only">Detail</span>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {outcome.rows.map((event) => {
              const expanded = expandedIds.has(event.id)
              const detailId = `audit-detail-${event.id}`
              return (
                <Fragment key={event.id}>
                  <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {formatLondonDateTime(event.occurredAtUtc)}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>{event.action}</TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {auditActorLabel(event)}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {auditTargetLabel(event)}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {event.restaurantId ?? "—"}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <Badge variant={event.succeeded ? "ready" : "error"}>
                        {auditResultLabel(event)}
                      </Badge>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <Button
                        variant="ghost"
                        size="sm"
                        aria-expanded={expanded}
                        aria-controls={detailId}
                        onClick={() => onToggle(event.id)}
                      >
                        {expanded ? "Hide detail" : "Show detail"}
                      </Button>
                    </TableCell>
                  </TableRow>
                  {expanded && (
                    <TableRow id={detailId} className="border-0 hover:bg-transparent">
                      <TableCell colSpan={COLUMN_COUNT} className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <AuditDetail event={event} />
                      </TableCell>
                    </TableRow>
                  )}
                </Fragment>
              )
            })}
          </TableBody>
        </Table>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-2 text-sm text-muted-foreground">
        <span>
          {auditPageRangeText(skip, outcome.rows.length, outcome.totalCount)}
        </span>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={onPrevious} disabled={skip === 0}>
            Previous
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={onNext}
            disabled={skip + PAGE_SIZE >= outcome.totalCount}
          >
            Next
          </Button>
        </div>
      </div>
    </div>
  )
}

function AuditDetail({ event }: { event: AdminAuditEvent }) {
  const detail = formatAuditDetail(event.detailJson)

  return (
    <div className="flex flex-col gap-2">
      <p className="text-sm">
        <span className="font-semibold">Correlation ID: </span>
        {event.correlationId ?? "Not recorded (background job)"}
      </p>
      <pre
        className={`max-h-72 overflow-auto rounded-md bg-muted p-3 text-xs text-foreground ${
          detail.isJson ? "" : "whitespace-pre-wrap"
        }`}
      >
        {detail.text}
      </pre>
    </div>
  )
}

import { useEffect, useState, type ReactNode } from "react"
import { Link, useParams, useSearchParams } from "react-router-dom"
import { isAxiosError } from "axios"
import { ArrowLeft } from "lucide-react"

import {
  getSupportQuery,
  patchSupportQueryStatus,
  postSupportReply,
} from "@/api/supportApi"
import { SupportQueryAttachments } from "@/components/dashboard/support/SupportQueryAttachments"
import { HelpCentreStatusBadge } from "@/components/help-centre/HelpCentreStatusBadge"
import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Skeleton } from "@/components/ui/skeleton"
import { Textarea } from "@/components/ui/textarea"
import {
  supportDashboardInboxUrl,
  type SupportInboxParams,
} from "@/config/support"
import { getFetchErrorMessage } from "@/lib/apiEnvelope"
import { warnIfEmailDispatchFailed } from "@/lib/emailDispatch"
import {
  OPERATOR_OUTLINE_TOOLBAR_BUTTON_CLASS,
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import { querySubmitterTypeLabel } from "@/lib/querySubmitterType"
import { cn } from "@/lib/utils"
import type { HelpCentreQueryStatus } from "@/types/helpCentre"
import type { SupportQueryDetail } from "@/types/support"

const STATUS_OPTIONS: Array<{
  value: HelpCentreQueryStatus
  label: string
}> = [
  { value: "NEW", label: "New" },
  { value: "IN_PROGRESS", label: "In progress" },
  { value: "WAITING_ON_CUSTOMER", label: "Waiting on customer" },
  { value: "ESCALATED_TO_ADMIN", label: "Escalated to Admin" },
  { value: "RESOLVED", label: "Resolved" },
  { value: "CLOSED", label: "Closed" },
]

function formatTimestamp(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return date.toLocaleString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  })
}

function inboxParamsFromSearch(
  searchParams: URLSearchParams
): SupportInboxParams {
  const page = Number(searchParams.get("page"))
  const pageSize = Number(searchParams.get("pageSize"))
  const type = searchParams.get("type")

  return {
    q: searchParams.get("q") ?? undefined,
    status: searchParams.get("status") ?? undefined,
    topic: searchParams.get("topic") ?? undefined,
    type: type === "operator" || type === "contact" ? type : undefined,
    page: Number.isInteger(page) && page > 1 ? page : undefined,
    pageSize:
      pageSize === 50 || pageSize === 100 ? pageSize : undefined,
  }
}

const pageInsetClass = "px-4 py-4 md:py-6 lg:px-8"

function BackLink({ to }: { to: string }) {
  return (
    <Link
      to={to}
      className="inline-flex w-fit items-center gap-2 text-base font-semibold text-primary underline-offset-4 hover:underline"
    >
      <ArrowLeft className="size-5 shrink-0" aria-hidden />
      Go back
    </Link>
  )
}

function DetailSkeleton() {
  return (
    <div className={cn("flex h-full min-h-0 flex-col gap-4", pageInsetClass)}>
      <Skeleton className="h-6 w-28 shrink-0" />
      <Skeleton className="h-8 w-2/3 max-w-md shrink-0" />
      <div className="grid min-h-0 flex-1 gap-4 lg:grid-cols-[300px_minmax(0,1fr)]">
        <div className="flex min-h-0 flex-col gap-3 overflow-y-auto">
          <Skeleton className="h-20 w-full shrink-0" />
          <Skeleton className="h-20 w-full shrink-0" />
          <Skeleton className="h-32 w-full shrink-0" />
        </div>
        <div className="flex min-h-0 flex-col gap-3 overflow-hidden rounded-lg border border-op-border-default bg-op-surface-primary p-4">
          <Skeleton className="h-16 w-3/4 shrink-0" />
          <Skeleton className="ml-auto h-16 w-2/3 shrink-0" />
          <Skeleton className="h-16 w-3/4 shrink-0" />
          <Skeleton className="mt-auto h-24 w-full shrink-0" />
        </div>
      </div>
    </div>
  )
}

function MetaField({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <div className="flex flex-col gap-1">
      <dt className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
        {label}
      </dt>
      <dd className="text-sm text-foreground">{children}</dd>
    </div>
  )
}

export default function SupportQueryDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [searchParams] = useSearchParams()
  const queryId = Number(id)

  const [query, setQuery] = useState<SupportQueryDetail | null>(null)
  const [state, setState] = useState<"loading" | "loaded" | "error">("loading")
  const [replyBody, setReplyBody] = useState("")
  const [escalationNote, setEscalationNote] = useState("")
  const [pendingStatus, setPendingStatus] = useState<HelpCentreQueryStatus | "">(
    ""
  )
  const [isReplying, setIsReplying] = useState(false)
  const [isUpdatingStatus, setIsUpdatingStatus] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  const backHref = supportDashboardInboxUrl(
    inboxParamsFromSearch(searchParams)
  )

  const loadQuery = async (options?: { soft?: boolean }) => {
    if (!Number.isInteger(queryId) || queryId < 1) {
      setState("error")
      return
    }

    if (!options?.soft) {
      setState("loading")
    }
    setActionError(null)

    try {
      const detail = await getSupportQuery(queryId)
      setQuery(detail)
      setPendingStatus(detail.status)
      setEscalationNote(detail.escalationNote ?? "")
      if (!options?.soft) {
        setReplyBody("")
      }
      setState("loaded")
    } catch {
      if (!options?.soft) {
        setState("error")
      } else {
        setActionError("Unable to refresh query.")
      }
    }
  }

  useEffect(() => {
    void loadQuery()
  }, [queryId])

  const handleSendReply = async () => {
    if (!query || !replyBody.trim()) {
      return
    }

    setActionError(null)
    setIsReplying(true)

    try {
      await postSupportReply(query.id, replyBody.trim())
      setReplyBody("")
      await loadQuery({ soft: true })
    } catch (error) {
      setActionError(
        isAxiosError(error)
          ? getFetchErrorMessage(error.response?.data, "Unable to send reply.")
          : "Unable to send reply."
      )
    } finally {
      setIsReplying(false)
    }
  }

  const handleUpdateStatus = async () => {
    if (!query) {
      return
    }

    const nextStatus = pendingStatus || query.status
    setActionError(null)
    setIsUpdatingStatus(true)

    try {
      const result = await patchSupportQueryStatus(
        query.id,
        nextStatus,
        nextStatus === "ESCALATED_TO_ADMIN" ? escalationNote : undefined
      )
      if (nextStatus === "RESOLVED") {
        warnIfEmailDispatchFailed(result)
      }
      await loadQuery({ soft: true })
    } catch (error) {
      setActionError(
        isAxiosError(error)
          ? getFetchErrorMessage(
              error.response?.data,
              "Unable to update status."
            )
          : "Unable to update status."
      )
    } finally {
      setIsUpdatingStatus(false)
    }
  }

  return (
    <div className="relative flex h-[calc(100dvh-3.5rem)] min-h-0 flex-col">
      <div className="min-h-0 flex-1 overflow-hidden">
        {state === "loading" && <DetailSkeleton />}

        {state === "error" && (
          <div
            className={cn(
              "flex h-full flex-col items-start gap-4",
              pageInsetClass
            )}
          >
            <BackLink to={backHref} />
            <p className="text-sm text-destructive">Unable to load this query.</p>
            <Button type="button" variant="outline" onClick={() => void loadQuery()}>
              Try again
            </Button>
          </div>
        )}

        {state === "loaded" && query && (
          <div
            className={cn(
              "flex h-full min-h-0 flex-col gap-4",
              pageInsetClass
            )}
          >
            <div className="flex shrink-0 flex-col gap-3">
              <BackLink to={backHref} />
              <div className="flex flex-wrap items-center gap-3">
                <h1 className="text-xl font-semibold">{query.topicLabel}</h1>
                <HelpCentreStatusBadge
                  status={query.status}
                  statusLabel={query.statusLabel}
                />
                <span className="text-sm text-muted-foreground">
                  Query #{query.id} · Updated {formatTimestamp(query.updatedAt)}
                </span>
              </div>
            </div>

            <div className="grid min-h-0 flex-1 gap-4 lg:grid-cols-[300px_minmax(0,1fr)]">
              <aside className="flex max-h-48 min-h-0 flex-col gap-6 overflow-y-auto rounded-lg border border-op-border-default bg-op-surface-primary p-4 lg:max-h-none">
                <dl className="grid grid-cols-1 gap-4">
                  <MetaField label="From">{query.submitterName}</MetaField>
                  <MetaField label="Email">{query.submitterEmail}</MetaField>
                  <MetaField label="Business">{query.businessName}</MetaField>
                  <MetaField label="Phone">
                    {query.phone?.trim() || "—"}
                  </MetaField>
                  <MetaField label="Location">
                    {query.queryLocation?.label ?? "—"}
                  </MetaField>
                  <MetaField label="Type">
                    {querySubmitterTypeLabel(Boolean(query.linkedOperator))}
                  </MetaField>
                </dl>

                {query.attachments && (
                  <SupportQueryAttachments
                    queryId={query.id}
                    attachments={query.attachments}
                  />
                )}

                <section className="flex flex-col gap-3">
                  <h3 className="text-sm font-semibold">Update status</h3>
                  <div className="flex flex-col gap-2">
                    <Label htmlFor="query-status">Status</Label>
                    <Select
                      value={pendingStatus || query.status}
                      onValueChange={(value) =>
                        setPendingStatus(value as HelpCentreQueryStatus)
                      }
                    >
                      <SelectTrigger
                        id="query-status"
                        className={cn(
                          OPERATOR_OUTLINE_TOOLBAR_BUTTON_CLASS,
                          "w-full data-[size=default]:h-auto"
                        )}
                      >
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent
                        position="popper"
                        className={OPERATOR_SHELL_MENU_PANEL_CLASS}
                      >
                        {STATUS_OPTIONS.map((option) => (
                          <SelectItem
                            key={option.value}
                            value={option.value}
                            className={OPERATOR_SHELL_MENU_ITEM_CLASS}
                          >
                            {option.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  {(pendingStatus || query.status) === "ESCALATED_TO_ADMIN" && (
                    <div className="flex flex-col gap-2">
                      <Label htmlFor="escalation-note">
                        Escalation note (optional)
                      </Label>
                      <Textarea
                        id="escalation-note"
                        value={escalationNote}
                        onChange={(event) =>
                          setEscalationNote(event.target.value)
                        }
                        rows={3}
                        className="rounded-[2px]"
                      />
                    </div>
                  )}

                  <Button
                    type="button"
                    variant="op-secondary"
                    onClick={() => void handleUpdateStatus()}
                    disabled={isUpdatingStatus}
                    className="w-fit rounded-[2px]"
                  >
                    {isUpdatingStatus ? "Saving..." : "Save status"}
                  </Button>
                </section>

                {actionError && (
                  <p className="text-sm text-destructive" role="alert">
                    {actionError}
                  </p>
                )}
              </aside>

              <section className="flex min-h-0 flex-col overflow-hidden rounded-lg border border-op-border-default bg-op-surface-primary">
                <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto bg-op-surface-primary p-4">
                  {query.messages.map((message) => {
                    const isSupport = message.authorKind === "SUPPORT"
                    return (
                      <article
                        key={message.id}
                        className={cn(
                          "max-w-[85%] rounded-2xl px-4 py-3 text-sm leading-6",
                          isSupport
                            ? "ml-auto bg-primary text-primary-foreground"
                            : "mr-auto border border-op-border-default bg-op-background-secondary text-foreground"
                        )}
                      >
                        <div
                          className={cn(
                            "mb-1 flex items-center justify-between gap-3 text-[11px] font-semibold tracking-wide uppercase",
                            isSupport
                              ? "text-primary-foreground/80"
                              : "text-muted-foreground"
                          )}
                        >
                          <span>
                            {isSupport
                              ? "Support"
                              : message.authorKind === "OPERATOR"
                                ? "Operator"
                                : "From"}
                          </span>
                          <span className="font-normal normal-case">
                            {formatTimestamp(message.createdAt)}
                          </span>
                        </div>
                        <p className="whitespace-pre-wrap">{message.body}</p>
                      </article>
                    )
                  })}
                </div>

                <div className="flex shrink-0 flex-col gap-2 border-t border-op-border-default bg-op-surface-primary p-4">
                  <Textarea
                    value={replyBody}
                    onChange={(event) => setReplyBody(event.target.value)}
                    rows={3}
                    placeholder="Write a support reply"
                    className="rounded-[2px] bg-op-surface-primary"
                  />
                  <Button
                    type="button"
                    variant="op-primary"
                    className="w-fit rounded-[2px]"
                    disabled={isReplying || !replyBody.trim()}
                    onClick={() => void handleSendReply()}
                  >
                    {isReplying ? "Sending..." : "Send reply"}
                  </Button>
                </div>
              </section>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

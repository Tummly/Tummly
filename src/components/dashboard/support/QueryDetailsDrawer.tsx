import type { ReactNode } from "react"

import { HelpCentreStatusBadge } from "@/components/help-centre/HelpCentreStatusBadge"
import { Button } from "@/components/ui/button"
import {
  Drawer,
  DrawerContent,
  DrawerDescription,
  DrawerHeader,
  DrawerTitle,
} from "@/components/ui/drawer"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
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

type QueryDetailsDrawerProps = {
  query: SupportQueryDetail | null
  open: boolean
  onOpenChange: (open: boolean) => void
  replyBody: string
  onReplyBodyChange: (value: string) => void
  escalationNote: string
  onEscalationNoteChange: (value: string) => void
  pendingStatus: HelpCentreQueryStatus | ""
  onPendingStatusChange: (value: HelpCentreQueryStatus) => void
  onSendReply: () => void
  onUpdateStatus: () => void
  isReplying: boolean
  isUpdatingStatus: boolean
  actionError: string | null
}

function DetailField({
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

export function QueryDetailsDrawer({
  query,
  open,
  onOpenChange,
  replyBody,
  onReplyBodyChange,
  escalationNote,
  onEscalationNoteChange,
  pendingStatus,
  onPendingStatusChange,
  onSendReply,
  onUpdateStatus,
  isReplying,
  isUpdatingStatus,
  actionError,
}: QueryDetailsDrawerProps) {
  return (
    <Drawer open={open} onOpenChange={onOpenChange} direction="right">
      <DrawerContent className="data-[vaul-drawer-direction=right]:w-full data-[vaul-drawer-direction=right]:sm:max-w-xl">
        {query && (
          <>
            <DrawerHeader className="border-b border-border">
              <div className="flex flex-wrap items-center gap-3">
                <DrawerTitle>{query.topicLabel}</DrawerTitle>
                <HelpCentreStatusBadge
                  status={query.status}
                  statusLabel={query.statusLabel}
                />
              </div>
              <DrawerDescription>
                Query #{query.id} · Updated {formatTimestamp(query.updatedAt)}
              </DrawerDescription>
            </DrawerHeader>

            <div className="flex flex-1 flex-col gap-6 overflow-y-auto p-4">
              <section className="grid grid-cols-2 gap-4">
                <DetailField label="Submitter">{query.submitterName}</DetailField>
                <DetailField label="Email">{query.submitterEmail}</DetailField>
                <DetailField label="Business">{query.businessName}</DetailField>
                <DetailField label="Phone">
                  {query.phone?.trim() || "—"}
                </DetailField>
                <DetailField label="Location">
                  {query.queryLocation?.label ?? "—"}
                </DetailField>
                <DetailField label="Linked operator">
                  {query.linkedOperator
                    ? (query.linkedOperatorEmail ?? "Yes")
                    : "Guest submission"}
                </DetailField>
              </section>

              <section className="flex flex-col gap-3">
                <h3 className="text-sm font-semibold">Thread</h3>
                <div className="flex flex-col gap-3">
                  {query.messages.map((message) => (
                    <article
                      key={message.id}
                      className="rounded-lg border border-border bg-muted/20 px-4 py-3"
                    >
                      <div className="mb-2 flex items-center justify-between gap-3">
                        <span className="text-xs font-semibold uppercase">
                          {message.authorKind}
                        </span>
                        <span className="text-xs text-muted-foreground">
                          {formatTimestamp(message.createdAt)}
                        </span>
                      </div>
                      <p className="text-sm leading-6 whitespace-pre-wrap">
                        {message.body}
                      </p>
                    </article>
                  ))}
                </div>
              </section>

              <section className="flex flex-col gap-3 border-t border-border pt-4">
                <h3 className="text-sm font-semibold">Reply to submitter</h3>
                <Textarea
                  value={replyBody}
                  onChange={(event) => onReplyBodyChange(event.target.value)}
                  rows={4}
                  placeholder="Write a support reply"
                />
                <Button
                  onClick={onSendReply}
                  disabled={isReplying || !replyBody.trim()}
                  className="w-fit"
                >
                  {isReplying ? "Sending..." : "Send reply"}
                </Button>
              </section>

              <section className="flex flex-col gap-3 border-t border-border pt-4">
                <h3 className="text-sm font-semibold">Update status</h3>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="query-status">Status</Label>
                  <Select
                    value={pendingStatus || query.status}
                    onValueChange={(value) =>
                      onPendingStatusChange(value as HelpCentreQueryStatus)
                    }
                  >
                    <SelectTrigger id="query-status">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {STATUS_OPTIONS.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
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
                        onEscalationNoteChange(event.target.value)
                      }
                      rows={3}
                    />
                  </div>
                )}

                <Button
                  variant="outline"
                  onClick={onUpdateStatus}
                  disabled={isUpdatingStatus}
                  className="w-fit"
                >
                  {isUpdatingStatus ? "Saving..." : "Save status"}
                </Button>
              </section>

              {actionError && (
                <p className="text-sm text-destructive" role="alert">
                  {actionError}
                </p>
              )}
            </div>
          </>
        )}
      </DrawerContent>
    </Drawer>
  )
}

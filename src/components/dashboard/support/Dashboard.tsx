import { useEffect, useMemo, useState } from "react"
import { isAxiosError } from "axios"
import { SearchIcon } from "lucide-react"

import {
  getSupportQueries,
  getSupportQuery,
  patchSupportQueryStatus,
  postSupportReply,
} from "@/api/supportApi"
import { HelpCentreStatusBadge } from "@/components/help-centre/HelpCentreStatusBadge"
import { QueryDetailsDrawer } from "@/components/dashboard/support/QueryDetailsDrawer"
import { SupportInboxStats } from "@/components/dashboard/support/SupportInboxStats"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Skeleton } from "@/components/ui/skeleton"
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
import { HELP_CENTRE_QUERY_TOPICS } from "@/content/helpCentre/queryTopics"
import { getFetchErrorMessage } from "@/lib/apiEnvelope"
import type { HelpCentreQueryStatus } from "@/types/helpCentre"
import type { SupportQueryDetail, SupportQueryListItem } from "@/types/support"

const STATUS_FILTER_OPTIONS = [
  { value: "ALL", label: "All statuses" },
  { value: "NEW", label: "New" },
  { value: "IN_PROGRESS", label: "In progress" },
  { value: "WAITING_ON_CUSTOMER", label: "Waiting on customer" },
  { value: "ESCALATED_TO_ADMIN", label: "Escalated to Admin" },
  { value: "RESOLVED", label: "Resolved" },
  { value: "CLOSED", label: "Closed" },
] as const

function formatUpdatedAt(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return date.toLocaleString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  })
}

export default function SupportDashboard() {
  const [queries, setQueries] = useState<SupportQueryListItem[]>([])
  const [state, setState] = useState<"loading" | "loaded" | "error">("loading")
  const [search, setSearch] = useState("")
  const [statusFilter, setStatusFilter] = useState("ALL")
  const [topicFilter, setTopicFilter] = useState("ALL")
  const [selectedQuery, setSelectedQuery] = useState<SupportQueryDetail | null>(
    null
  )
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [replyBody, setReplyBody] = useState("")
  const [escalationNote, setEscalationNote] = useState("")
  const [pendingStatus, setPendingStatus] = useState<HelpCentreQueryStatus | "">(
    ""
  )
  const [isReplying, setIsReplying] = useState(false)
  const [isUpdatingStatus, setIsUpdatingStatus] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  const loadQueries = async () => {
    setState("loading")
    try {
      const result = await getSupportQueries({
        status: statusFilter === "ALL" ? undefined : statusFilter,
        topic: topicFilter === "ALL" ? undefined : topicFilter,
      })
      setQueries(result)
      setState("loaded")
    } catch {
      setState("error")
    }
  }

  useEffect(() => {
    void loadQueries()
  }, [statusFilter, topicFilter])

  const filteredQueries = useMemo(() => {
    const query = search.trim().toLowerCase()
    if (!query) {
      return queries
    }

    return queries.filter(
      (item) =>
        item.topicLabel.toLowerCase().includes(query)
        || item.submitterName.toLowerCase().includes(query)
        || item.submitterEmail.toLowerCase().includes(query)
        || item.businessName.toLowerCase().includes(query)
        || (item.preview ?? "").toLowerCase().includes(query)
    )
  }, [queries, search])

  const openQuery = async (id: number) => {
    setActionError(null)
    try {
      const detail = await getSupportQuery(id)
      setSelectedQuery(detail)
      setPendingStatus(detail.status)
      setEscalationNote(detail.escalationNote ?? "")
      setReplyBody("")
      setDrawerOpen(true)
    } catch {
      setActionError("Unable to open query.")
    }
  }

  const refreshSelectedQuery = async (id: number) => {
    const detail = await getSupportQuery(id)
    setSelectedQuery(detail)
    setPendingStatus(detail.status)
    setEscalationNote(detail.escalationNote ?? "")
    await loadQueries()
  }

  const handleSendReply = async () => {
    if (!selectedQuery || !replyBody.trim()) {
      return
    }

    setActionError(null)
    setIsReplying(true)

    try {
      await postSupportReply(selectedQuery.id, replyBody.trim())
      setReplyBody("")
      await refreshSelectedQuery(selectedQuery.id)
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
    if (!selectedQuery) {
      return
    }

    const nextStatus = pendingStatus || selectedQuery.status
    setActionError(null)
    setIsUpdatingStatus(true)

    try {
      await patchSupportQueryStatus(
        selectedQuery.id,
        nextStatus,
        nextStatus === "ESCALATED_TO_ADMIN" ? escalationNote : undefined
      )
      await refreshSelectedQuery(selectedQuery.id)
    } catch (error) {
      setActionError(
        isAxiosError(error)
          ? getFetchErrorMessage(error.response?.data, "Unable to update status.")
          : "Unable to update status."
      )
    } finally {
      setIsUpdatingStatus(false)
    }
  }

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-6 px-4 py-8 lg:px-8">
      <div className="flex flex-col gap-2">
        <h1 className="text-3xl font-bold">Support dashboard</h1>
        <p className="text-muted-foreground">
          Manage Help Centre queries from operators and guests.
        </p>
      </div>

      <Card>
        <CardHeader className="gap-4">
          <CardTitle>Help Centre inbox</CardTitle>
          {state === "loaded" && queries.length > 0 && (
            <SupportInboxStats
              queries={queries}
              activeStatus={statusFilter}
              onStatusClick={(status) =>
                setStatusFilter(status === "ALL" ? "ALL" : status)
              }
            />
          )}
          <div className="grid gap-3 lg:grid-cols-[1fr_180px_220px]">
            <div className="relative">
              <SearchIcon className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search queries"
                className="pl-9"
              />
            </div>
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {STATUS_FILTER_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Select value={topicFilter} onValueChange={setTopicFilter}>
              <SelectTrigger>
                <SelectValue placeholder="All topics" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="ALL">All topics</SelectItem>
                {HELP_CENTRE_QUERY_TOPICS.map((topic) => (
                  <SelectItem key={topic.slug} value={topic.slug}>
                    {topic.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </CardHeader>
        <CardContent>
          {state === "loading" && (
            <div className="flex flex-col gap-3">
              {Array.from({ length: 5 }).map((_, index) => (
                <Skeleton key={index} className="h-12 w-full" />
              ))}
            </div>
          )}
          {state === "error" && (
            <p className="text-sm text-destructive">Unable to load queries.</p>
          )}
          {state === "loaded" && (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Topic</TableHead>
                  <TableHead>Submitter</TableHead>
                  <TableHead>Business</TableHead>
                  <TableHead>Preview</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Updated</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredQueries.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="text-muted-foreground">
                      No queries match your filters.
                    </TableCell>
                  </TableRow>
                ) : (
                  filteredQueries.map((query) => (
                    <TableRow
                      key={query.id}
                      className="cursor-pointer"
                      onClick={() => void openQuery(query.id)}
                    >
                      <TableCell className="font-medium">
                        {query.topicLabel}
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-col">
                          <span>{query.submitterName}</span>
                          <span className="text-xs text-muted-foreground">
                            {query.submitterEmail}
                          </span>
                        </div>
                      </TableCell>
                      <TableCell>{query.businessName}</TableCell>
                      <TableCell className="max-w-[240px]">
                        <span className="line-clamp-2 text-sm text-muted-foreground">
                          {query.preview ?? "—"}
                        </span>
                      </TableCell>
                      <TableCell>
                        <HelpCentreStatusBadge
                          status={query.status}
                          statusLabel={query.statusLabel}
                        />
                      </TableCell>
                      <TableCell>{formatUpdatedAt(query.updatedAt)}</TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <QueryDetailsDrawer
        query={selectedQuery}
        open={drawerOpen}
        onOpenChange={setDrawerOpen}
        replyBody={replyBody}
        onReplyBodyChange={setReplyBody}
        escalationNote={escalationNote}
        onEscalationNoteChange={setEscalationNote}
        pendingStatus={pendingStatus}
        onPendingStatusChange={setPendingStatus}
        onSendReply={() => void handleSendReply()}
        onUpdateStatus={() => void handleUpdateStatus()}
        isReplying={isReplying}
        isUpdatingStatus={isUpdatingStatus}
        actionError={actionError}
      />
    </div>
  )
}

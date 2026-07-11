import { useEffect, useMemo, useState } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"
import { SearchIcon } from "lucide-react"

import { getSupportQueries } from "@/api/supportApi"
import { HelpCentreStatusBadge } from "@/components/help-centre/HelpCentreStatusBadge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { supportDashboardQueryUrl } from "@/config/support"
import { HELP_CENTRE_QUERY_TOPICS } from "@/content/helpCentre/queryTopics"
import { useSupportInboxParams } from "@/hooks/useSupportInboxParams"
import { querySubmitterTypeLabel } from "@/lib/querySubmitterType"
import type { SupportQueryListItem } from "@/types/support"

const STATUS_FILTER_OPTIONS = [
  { value: "ALL", label: "All statuses" },
  { value: "NEW", label: "New" },
  { value: "IN_PROGRESS", label: "In progress" },
  { value: "WAITING_ON_CUSTOMER", label: "Waiting on customer" },
  { value: "ESCALATED_TO_ADMIN", label: "Escalated to Admin" },
  { value: "RESOLVED", label: "Resolved" },
  { value: "CLOSED", label: "Closed" },
] as const

const TYPE_FILTER_OPTIONS = [
  { value: "ALL", label: "All types" },
  { value: "operator", label: "Operator" },
  { value: "contact", label: "Contact" },
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

function buildPageNumbers(current: number, total: number) {
  if (total <= 7) {
    return Array.from({ length: total }, (_, index) => index + 1)
  }

  const pages = new Set<number>([1, total, current, current - 1, current + 1])
  return [...pages]
    .filter((page) => page >= 1 && page <= total)
    .sort((a, b) => a - b)
}

export default function SupportDashboard() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const {
    state,
    searchDraft,
    setSearchDraft,
    setStatus,
    setTopic,
    setType,
    setPage,
    setPageSize,
    pageSizeOptions,
  } = useSupportInboxParams()

  const [queries, setQueries] = useState<SupportQueryListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loadState, setLoadState] = useState<"loading" | "loaded" | "error">(
    "loading"
  )

  useEffect(() => {
    let cancelled = false

    const loadQueries = async () => {
      setLoadState("loading")
      try {
        const result = await getSupportQueries({
          status: state.status === "ALL" ? undefined : state.status,
          topic: state.topic === "ALL" ? undefined : state.topic,
          type: state.type === "ALL" ? undefined : state.type,
          q: state.q.trim() || undefined,
          page: state.page,
          pageSize: state.pageSize,
        })
        if (cancelled) {
          return
        }
        setQueries(result.queries)
        setTotalCount(result.totalCount)
        setLoadState("loaded")
      } catch {
        if (!cancelled) {
          setLoadState("error")
        }
      }
    }

    void loadQueries()
    return () => {
      cancelled = true
    }
  }, [
    state.page,
    state.pageSize,
    state.q,
    state.status,
    state.topic,
    state.type,
  ])

  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const currentPage = Math.min(state.page, totalPages)
  const pageNumbers = useMemo(
    () => buildPageNumbers(currentPage, totalPages),
    [currentPage, totalPages]
  )

  useEffect(() => {
    if (loadState === "loaded" && state.page > totalPages) {
      setPage(totalPages)
    }
  }, [loadState, setPage, state.page, totalPages])

  const openQuery = (id: number) => {
    const search = searchParams.toString()
    navigate({
      pathname: supportDashboardQueryUrl(id),
      search,
    })
  }

  const showingFrom =
    totalCount === 0 ? 0 : (currentPage - 1) * state.pageSize + 1
  const showingTo = Math.min(currentPage * state.pageSize, totalCount)

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-6 px-4 py-8 lg:px-8">
      <div className="flex flex-col gap-2">
        <h1 className="text-3xl font-bold">Support dashboard</h1>
        <p className="text-muted-foreground">
          Manage Help Centre queries from operators and contacts.
        </p>
      </div>

      <Card>
        <CardHeader className="gap-4">
          <CardTitle>Help Centre inbox</CardTitle>
          <div className="grid gap-3 lg:grid-cols-[1fr_160px_200px_140px]">
            <div className="relative">
              <SearchIcon className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={searchDraft}
                onChange={(event) => setSearchDraft(event.target.value)}
                placeholder="Search queries"
                className="pl-9"
              />
            </div>
            <Select value={state.status} onValueChange={setStatus}>
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
            <Select value={state.topic} onValueChange={setTopic}>
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
            <Select
              value={state.type}
              onValueChange={(value) =>
                setType(value as typeof state.type)
              }
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {TYPE_FILTER_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {loadState === "error" && (
            <p className="text-sm text-destructive">Unable to load queries.</p>
          )}

          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Issue</TableHead>
                <TableHead>From</TableHead>
                <TableHead>Type</TableHead>
                <TableHead>Business</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Updated</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loadState === "loading" &&
                Array.from({ length: 5 }).map((_, index) => (
                  <TableRow key={`skeleton-${index}`}>
                    {Array.from({ length: 6 }).map((__, cellIndex) => (
                      <TableCell key={cellIndex}>
                        <Skeleton className="h-5 w-full max-w-28" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))}

              {loadState === "loaded" && queries.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} className="text-muted-foreground">
                    No queries match your filters.
                  </TableCell>
                </TableRow>
              )}

              {loadState === "loaded" &&
                queries.map((query) => (
                  <TableRow
                    key={query.id}
                    className="cursor-pointer"
                    onClick={() => openQuery(query.id)}
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
                    <TableCell>
                      {querySubmitterTypeLabel(query.linkedOperator)}
                    </TableCell>
                    <TableCell>{query.businessName}</TableCell>
                    <TableCell>
                      <HelpCentreStatusBadge
                        status={query.status}
                        statusLabel={query.statusLabel}
                      />
                    </TableCell>
                    <TableCell>{formatUpdatedAt(query.updatedAt)}</TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>

          {loadState === "loaded" && totalCount > 0 && (
            <div className="flex flex-col gap-3 border-t pt-4 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex flex-wrap items-center gap-3">
                <p className="text-sm text-muted-foreground">
                  Showing {showingFrom}–{showingTo} of {totalCount}
                </p>
                <Select
                  value={String(state.pageSize)}
                  onValueChange={(value) => setPageSize(Number(value))}
                >
                  <SelectTrigger className="w-28" aria-label="Page size">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {pageSizeOptions.map((size) => (
                      <SelectItem key={size} value={String(size)}>
                        {size} / page
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <Pagination className="mx-0 w-auto justify-end">
                <PaginationContent>
                  <PaginationItem>
                    <PaginationPrevious
                      onClick={() => setPage(Math.max(1, currentPage - 1))}
                      disabled={currentPage === 1}
                    />
                  </PaginationItem>

                  {pageNumbers.flatMap((pageNumber, index) => {
                    const previous = pageNumbers[index - 1]
                    const items = []

                    if (previous !== undefined && pageNumber - previous > 1) {
                      items.push(
                        <PaginationItem key={`ellipsis-${pageNumber}`}>
                          <PaginationEllipsis />
                        </PaginationItem>
                      )
                    }

                    items.push(
                      <PaginationItem key={pageNumber}>
                        <PaginationLink
                          isActive={pageNumber === currentPage}
                          onClick={() => setPage(pageNumber)}
                        >
                          {pageNumber}
                        </PaginationLink>
                      </PaginationItem>
                    )

                    return items
                  })}

                  <PaginationItem>
                    <PaginationNext
                      onClick={() =>
                        setPage(Math.min(totalPages, currentPage + 1))
                      }
                      disabled={currentPage === totalPages}
                    />
                  </PaginationItem>
                </PaginationContent>
              </Pagination>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

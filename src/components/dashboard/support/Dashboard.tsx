import { useEffect, useMemo, useState } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"

import { getSupportQueries } from "@/api/supportApi"
import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { HelpCentreStatusBadge } from "@/components/help-centre/HelpCentreStatusBadge"
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
import {
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_SEARCH_FIELD_CLASS,
  GUESTS_SEARCH_WRAP_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_GUEST_NAME_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
  GUESTS_TABLE_INTERACTION_TIME_CLASS,
  GUESTS_TABLE_LOCATION_CLASS,
  GUESTS_TOOLBAR_ACTIONS_CLASS,
  GUESTS_TOOLBAR_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  OPERATOR_OUTLINE_TOOLBAR_BUTTON_CLASS,
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import { querySubmitterTypeLabel } from "@/lib/querySubmitterType"
import { cn } from "@/lib/utils"
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

  const selectTriggerClass = cn(
    OPERATOR_OUTLINE_TOOLBAR_BUTTON_CLASS,
    "w-full min-w-36 data-[size=default]:h-auto sm:w-auto"
  )

  return (
    <div className="flex w-full flex-col px-4 py-8 lg:px-8">
      <div className={GUESTS_PAGE_STACK_CLASS}>
        <header className="flex flex-col gap-3.5 leading-[0]">
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>Support</h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
            Manage Help Centre queries from operators and contacts.
          </p>
        </header>

        <section className={GUESTS_SECTION_CLASS}>
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>Help Centre inbox</h2>

          <div className={GUESTS_TOOLBAR_ROW_CLASS}>
            <div className={GUESTS_SEARCH_WRAP_CLASS}>
              <OperatorSearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-op-icon-default" />
              <Input
                value={searchDraft}
                onChange={(event) => setSearchDraft(event.target.value)}
                placeholder="Search queries"
                className={GUESTS_SEARCH_FIELD_CLASS}
                aria-label="Search queries"
              />
            </div>

            <div className={GUESTS_TOOLBAR_ACTIONS_CLASS}>
              <Select value={state.status} onValueChange={setStatus}>
                <SelectTrigger className={selectTriggerClass}>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent
                  position="popper"
                  className={OPERATOR_SHELL_MENU_PANEL_CLASS}
                >
                  {STATUS_FILTER_OPTIONS.map((option) => (
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
              <Select value={state.topic} onValueChange={setTopic}>
                <SelectTrigger className={selectTriggerClass}>
                  <SelectValue placeholder="All topics" />
                </SelectTrigger>
                <SelectContent
                  position="popper"
                  className={OPERATOR_SHELL_MENU_PANEL_CLASS}
                >
                  <SelectItem
                    value="ALL"
                    className={OPERATOR_SHELL_MENU_ITEM_CLASS}
                  >
                    All topics
                  </SelectItem>
                  {HELP_CENTRE_QUERY_TOPICS.map((topic) => (
                    <SelectItem
                      key={topic.slug}
                      value={topic.slug}
                      className={OPERATOR_SHELL_MENU_ITEM_CLASS}
                    >
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
                <SelectTrigger className={selectTriggerClass}>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent
                  position="popper"
                  className={OPERATOR_SHELL_MENU_PANEL_CLASS}
                >
                  {TYPE_FILTER_OPTIONS.map((option) => (
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
          </div>

          {loadState === "error" && (
            <p className="text-sm text-destructive">Unable to load queries.</p>
          )}

          <div className={GUESTS_TABLE_FRAME_CLASS}>
            <Table className={GUESTS_TABLE_CLASS}>
              <TableHeader className="[&_tr]:border-0">
                <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Issue
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    From
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Type
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Business
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Status
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Updated
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loadState === "loading" &&
                  Array.from({ length: 5 }).map((_, index) => (
                    <TableRow
                      key={`skeleton-${index}`}
                      className={GUESTS_TABLE_BODY_ROW_CLASS}
                    >
                      {Array.from({ length: 6 }).map((__, cellIndex) => (
                        <TableCell
                          key={cellIndex}
                          className={GUESTS_TABLE_BODY_CELL_CLASS}
                        >
                          <Skeleton className="h-5 w-full max-w-28" />
                        </TableCell>
                      ))}
                    </TableRow>
                  ))}

                {loadState === "loaded" && queries.length === 0 && (
                  <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                    <TableCell
                      colSpan={6}
                      className={cn(
                        GUESTS_TABLE_BODY_CELL_CLASS,
                        "text-muted-foreground"
                      )}
                    >
                      No queries match your filters.
                    </TableCell>
                  </TableRow>
                )}

                {loadState === "loaded" &&
                  queries.map((query) => (
                    <TableRow
                      key={query.id}
                      className={cn(GUESTS_TABLE_BODY_ROW_CLASS, "cursor-pointer")}
                      onClick={() => openQuery(query.id)}
                    >
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                          {query.topicLabel}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <div className="flex flex-col gap-1">
                          <span className={GUESTS_TABLE_LOCATION_CLASS}>
                            {query.submitterName}
                          </span>
                          <span className={GUESTS_TABLE_INTERACTION_TIME_CLASS}>
                            {query.submitterEmail}
                          </span>
                        </div>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_LOCATION_CLASS}>
                          {querySubmitterTypeLabel(query.linkedOperator)}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_LOCATION_CLASS}>
                          {query.businessName}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <HelpCentreStatusBadge
                          status={query.status}
                          statusLabel={query.statusLabel}
                        />
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_INTERACTION_TIME_CLASS}>
                          {formatUpdatedAt(query.updatedAt)}
                        </span>
                      </TableCell>
                    </TableRow>
                  ))}
              </TableBody>
            </Table>
          </div>

          {loadState === "loaded" && totalCount > 0 && (
            <div className="flex flex-col gap-3 border-t border-op-border-default pt-4 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex flex-wrap items-center gap-3">
                <p className="text-sm text-muted-foreground">
                  Showing {showingFrom}–{showingTo} of {totalCount}
                </p>
                <Select
                  value={String(state.pageSize)}
                  onValueChange={(value) => setPageSize(Number(value))}
                >
                  <SelectTrigger
                    className={cn(selectTriggerClass, "w-28")}
                    aria-label="Page size"
                  >
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent
                    position="popper"
                    className={OPERATOR_SHELL_MENU_PANEL_CLASS}
                  >
                    {pageSizeOptions.map((size) => (
                      <SelectItem
                        key={size}
                        value={String(size)}
                        className={OPERATOR_SHELL_MENU_ITEM_CLASS}
                      >
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
        </section>
      </div>
    </div>
  )
}

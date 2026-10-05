import { useCallback, useEffect, useMemo, useState } from "react"
import { DownloadIcon } from "lucide-react"
import { toast } from "sonner"

import {
  downloadAdminShopOrdersCsv,
  fetchAdminShopOrders,
  type AdminShopFulfilmentStatus,
  type AdminShopOrderListItem,
} from "@/api/adminShopOrdersApi"
import { AdminShopOrderDetailDrawer } from "@/components/dashboard/admin/AdminShopOrderDetailDrawer"
import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
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
import {
  ADMIN_SHOP_FULFILMENT_FILTER_OPTIONS,
  ALL_ADMIN_SHOP_FULFILMENT_STATUSES,
  adminShopFulfilmentLabel,
  formatAdminShopGbpFromPence,
  nextAdminShopFulfilmentAction,
} from "@/lib/adminShopOrderFulfilment"
import {
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_SEARCH_FIELD_CLASS,
  GUESTS_SEARCH_WRAP_CLASS,
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
import { cn } from "@/lib/utils"

const PAGE_SIZE = 25

function buildPageNumbers(current: number, total: number) {
  if (total <= 7) {
    return Array.from({ length: total }, (_, index) => index + 1)
  }

  const pages = new Set<number>([1, total, current, current - 1, current + 1])
  return [...pages]
    .filter((page) => page >= 1 && page <= total)
    .sort((a, b) => a - b)
}

function fulfilmentStatusesForFilter(
  filter: AdminShopFulfilmentStatus | "all"
): AdminShopFulfilmentStatus[] | undefined {
  if (filter === "all") {
    return ALL_ADMIN_SHOP_FULFILMENT_STATUSES
  }
  if (filter === "processing") {
    // API defaults to processing when omitted; send explicitly for clarity.
    return ["processing"]
  }
  return [filter]
}

export function AdminShopOrdersPanel() {
  const [orders, setOrders] = useState<AdminShopOrderListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState("")
  const [debouncedSearch, setDebouncedSearch] = useState("")
  const [statusFilter, setStatusFilter] = useState<
    AdminShopFulfilmentStatus | "all"
  >("processing")
  const [loading, setLoading] = useState(true)
  const [exporting, setExporting] = useState(false)
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null)

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedSearch(search.trim())
      setPage(1)
    }, 300)
    return () => window.clearTimeout(timer)
  }, [search])

  const listParams = useMemo(
    () => ({
      page,
      pageSize: PAGE_SIZE,
      q: debouncedSearch || undefined,
      fulfilmentStatus: fulfilmentStatusesForFilter(statusFilter),
    }),
    [debouncedSearch, page, statusFilter]
  )

  const loadOrders = useCallback(async () => {
    setLoading(true)
    try {
      const response = await fetchAdminShopOrders(listParams)
      setOrders(response.items)
      setTotalCount(response.totalCount)
    } catch {
      setOrders([])
      setTotalCount(0)
      toast.error("Could not load shop orders")
    } finally {
      setLoading(false)
    }
  }, [listParams])

  useEffect(() => {
    void loadOrders()
  }, [loadOrders])

  const selectedOrder =
    orders.find((order) => order.id === selectedOrderId) ?? null

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))
  const currentPage = Math.min(page, totalPages)
  const pageNumbers = buildPageNumbers(currentPage, totalPages)
  const pageStart = totalCount === 0 ? 0 : (currentPage - 1) * PAGE_SIZE + 1
  const pageEnd = Math.min(currentPage * PAGE_SIZE, totalCount)

  const handleExport = async () => {
    setExporting(true)
    try {
      const { blob, fileName } = await downloadAdminShopOrdersCsv({
        q: debouncedSearch || undefined,
        fulfilmentStatus: fulfilmentStatusesForFilter(statusFilter),
      })
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement("a")
      anchor.href = url
      anchor.download = fileName
      anchor.click()
      URL.revokeObjectURL(url)
      toast.success("Shop orders CSV downloaded")
    } catch {
      toast.error("Could not export shop orders")
    } finally {
      setExporting(false)
    }
  }

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <header className="flex flex-col gap-3.5 leading-[0]">
        <h1 className={GUESTS_PAGE_TITLE_CLASS}>Shop orders</h1>
        <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
          Review paid materials orders, advance fulfilment, set tracking URLs,
          and export the warehouse CSV.
        </p>
      </header>

      <section className={GUESTS_SECTION_CLASS}>
        <div className="flex flex-col gap-1">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>Fulfilment queue</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            {loading
              ? "Loading orders…"
              : `${totalCount} result${totalCount === 1 ? "" : "s"}`}
          </p>
        </div>

        <div className={GUESTS_TOOLBAR_ROW_CLASS}>
          <div className={GUESTS_SEARCH_WRAP_CLASS}>
            <OperatorSearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-op-icon-default" />
            <Input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search order, location, or material"
              className={GUESTS_SEARCH_FIELD_CLASS}
              aria-label="Search shop orders"
            />
          </div>

          <div className={GUESTS_TOOLBAR_ACTIONS_CLASS}>
            <Select
              value={statusFilter}
              onValueChange={(value) => {
                setStatusFilter(value as AdminShopFulfilmentStatus | "all")
                setPage(1)
              }}
            >
              <SelectTrigger
                className={cn(
                  OPERATOR_OUTLINE_TOOLBAR_BUTTON_CLASS,
                  "w-full min-w-40 data-[size=default]:h-auto sm:w-44"
                )}
              >
                <SelectValue placeholder="Status" />
              </SelectTrigger>
              <SelectContent className={OPERATOR_SHELL_MENU_PANEL_CLASS}>
                {ADMIN_SHOP_FULFILMENT_FILTER_OPTIONS.map((option) => (
                  <SelectItem
                    key={option.id}
                    value={option.id}
                    className={OPERATOR_SHELL_MENU_ITEM_CLASS}
                  >
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Button
              type="button"
              variant="op-secondary"
              className="rounded-[2px]"
              disabled={exporting}
              onClick={() => void handleExport()}
            >
              <DownloadIcon />
              Export CSV
            </Button>
          </div>
        </div>

        <div className={GUESTS_TABLE_FRAME_CLASS}>
          <Table className={GUESTS_TABLE_CLASS}>
            <TableHeader className="[&_tr]:border-0">
              <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Order
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Location
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Restaurant
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Materials
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Total
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Payment
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Fulfilment
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Paid
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Next step
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading &&
                Array.from({ length: 5 }).map((_, index) => (
                  <TableRow
                    key={`skeleton-${index}`}
                    className={GUESTS_TABLE_BODY_ROW_CLASS}
                  >
                    {Array.from({ length: 9 }).map((__, cellIndex) => (
                      <TableCell
                        key={cellIndex}
                        className={GUESTS_TABLE_BODY_CELL_CLASS}
                      >
                        <Skeleton className="h-5 w-full max-w-28" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))}

              {!loading &&
                orders.map((order) => {
                  const nextAction = nextAdminShopFulfilmentAction(
                    order.fulfilmentStatus
                  )
                  const materials = order.lines
                    .map((line) => `${line.titleSnapshot} ×${line.quantity}`)
                    .join(", ")

                  return (
                    <TableRow
                      key={order.id}
                      className={cn(
                        GUESTS_TABLE_BODY_ROW_CLASS,
                        "cursor-pointer",
                        selectedOrderId === order.id && "bg-op-background-secondary"
                      )}
                      onClick={() => setSelectedOrderId(order.id)}
                    >
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                          {order.orderNumber}
                        </span>
                      </TableCell>
                      <TableCell
                        className={cn(
                          GUESTS_TABLE_BODY_CELL_CLASS,
                          "max-w-44 truncate"
                        )}
                      >
                        <span className={GUESTS_TABLE_LOCATION_CLASS}>
                          {order.locationNameSnapshot}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_INTERACTION_TIME_CLASS}>
                          #{order.restaurantId}
                        </span>
                      </TableCell>
                      <TableCell
                        className={cn(
                          GUESTS_TABLE_BODY_CELL_CLASS,
                          "max-w-56 truncate"
                        )}
                      >
                        <span className={GUESTS_TABLE_INTERACTION_TIME_CLASS}>
                          {materials || "—"}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className="text-sm text-foreground">
                          {formatAdminShopGbpFromPence(order.grossPence)}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <Badge variant="soft">
                          {order.isComplimentary
                            ? "Free"
                            : order.paymentStatus}
                        </Badge>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <Badge variant="soft">
                          {adminShopFulfilmentLabel(order.fulfilmentStatus)}
                        </Badge>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_INTERACTION_TIME_CLASS}>
                          {order.paidAtUtc
                            ? new Date(order.paidAtUtc).toLocaleDateString()
                            : "—"}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_INTERACTION_TIME_CLASS}>
                          {nextAction?.label ?? "—"}
                        </span>
                      </TableCell>
                    </TableRow>
                  )
                })}

              {!loading && orders.length === 0 && (
                <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                  <TableCell
                    colSpan={9}
                    className={cn(
                      GUESTS_TABLE_BODY_CELL_CLASS,
                      "h-32 text-center text-muted-foreground"
                    )}
                  >
                    No shop orders match this filter.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>

        {!loading && totalCount > 0 && (
          <div className="flex flex-col gap-3 border-t border-op-border-default pt-4 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm text-muted-foreground">
              Showing {pageStart}–{pageEnd} of {totalCount}
            </p>
            <Pagination className="mx-0 w-auto justify-end">
              <PaginationContent>
                <PaginationItem>
                  <PaginationPrevious
                    onClick={() => setPage((value) => Math.max(1, value - 1))}
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
                      setPage((value) => Math.min(totalPages, value + 1))
                    }
                    disabled={currentPage === totalPages}
                  />
                </PaginationItem>
              </PaginationContent>
            </Pagination>
          </div>
        )}
      </section>

      <AdminShopOrderDetailDrawer
        order={selectedOrder}
        open={selectedOrder !== null}
        onOpenChange={(open) => {
          if (!open) {
            setSelectedOrderId(null)
          }
        }}
        onOrderUpdated={(updated) => {
          setOrders((current) => {
            const stillMatchesFilter =
              statusFilter === "all" ||
              updated.fulfilmentStatus === statusFilter

            if (!stillMatchesFilter) {
              return current.filter((item) => item.id !== updated.id)
            }

            return current.map((item) =>
              item.id === updated.id ? updated : item
            )
          })
          if (
            statusFilter !== "all" &&
            updated.fulfilmentStatus !== statusFilter
          ) {
            setTotalCount((count) => Math.max(0, count - 1))
            setSelectedOrderId(null)
            void loadOrders()
          }
        }}
      />
    </div>
  )
}

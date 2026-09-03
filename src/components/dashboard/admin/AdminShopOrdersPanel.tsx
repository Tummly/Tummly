import { useCallback, useEffect, useMemo, useState } from "react"
import { DownloadIcon, SearchIcon } from "lucide-react"
import { toast } from "sonner"

import {
  downloadAdminShopOrdersCsv,
  fetchAdminShopOrders,
  type AdminShopFulfilmentStatus,
  type AdminShopOrderListItem,
} from "@/api/adminShopOrdersApi"
import { AdminShopOrderDetailDrawer } from "@/components/dashboard/admin/AdminShopOrderDetailDrawer"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
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
import {
  ADMIN_SHOP_FULFILMENT_FILTER_OPTIONS,
  ALL_ADMIN_SHOP_FULFILMENT_STATUSES,
  adminShopFulfilmentLabel,
  formatAdminShopGbpFromPence,
  nextAdminShopFulfilmentAction,
} from "@/lib/adminShopOrderFulfilment"
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
    <div className="flex flex-col gap-6">
      <header className="flex flex-col gap-2">
        <h2 className="font-heading text-3xl font-semibold tracking-tight text-foreground">
          Shop orders
        </h2>
        <p className="max-w-2xl text-sm text-muted-foreground">
          Review paid materials orders, advance fulfilment, set tracking URLs,
          and export the warehouse CSV.
        </p>
      </header>

      <Card className="overflow-hidden rounded-2xl">
        <CardHeader className="border-b">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <CardTitle>Fulfilment queue</CardTitle>
              <p className="text-sm text-muted-foreground">
                {loading
                  ? "Loading orders…"
                  : `${totalCount} result${totalCount === 1 ? "" : "s"}`}
              </p>
            </div>

            <div className="flex w-full flex-col gap-3 sm:flex-row sm:items-center lg:max-w-3xl">
              <div className="relative min-w-0 flex-1">
                <SearchIcon className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Search order, location, or material"
                  className="h-10 rounded-xl pl-9"
                  aria-label="Search shop orders"
                />
              </div>

              <Select
                value={statusFilter}
                onValueChange={(value) => {
                  setStatusFilter(value as AdminShopFulfilmentStatus | "all")
                  setPage(1)
                }}
              >
                <SelectTrigger className="h-10 w-full rounded-xl sm:w-44">
                  <SelectValue placeholder="Status" />
                </SelectTrigger>
                <SelectContent>
                  {ADMIN_SHOP_FULFILMENT_FILTER_OPTIONS.map((option) => (
                    <SelectItem key={option.id} value={option.id}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>

              <Button
                type="button"
                variant="outline"
                className="rounded-xl"
                disabled={exporting}
                onClick={() => void handleExport()}
              >
                <DownloadIcon />
                Export CSV
              </Button>
            </div>
          </div>
        </CardHeader>

        <CardContent className="px-0">
          <Table>
            <TableHeader>
              <TableRow className="bg-brand-dark hover:bg-brand-dark">
                <TableHead className="text-brand-dark-foreground">
                  Order
                </TableHead>
                <TableHead className="text-brand-dark-foreground">
                  Location
                </TableHead>
                <TableHead className="text-brand-dark-foreground">
                  Restaurant
                </TableHead>
                <TableHead className="text-brand-dark-foreground">
                  Materials
                </TableHead>
                <TableHead className="text-brand-dark-foreground">
                  Total
                </TableHead>
                <TableHead className="text-brand-dark-foreground">
                  Payment
                </TableHead>
                <TableHead className="text-brand-dark-foreground">
                  Fulfilment
                </TableHead>
                <TableHead className="text-brand-dark-foreground">
                  Paid
                </TableHead>
                <TableHead className="text-right text-brand-dark-foreground">
                  Next step
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading &&
                Array.from({ length: 5 }).map((_, index) => (
                  <TableRow key={`skeleton-${index}`}>
                    {Array.from({ length: 9 }).map((__, cellIndex) => (
                      <TableCell key={cellIndex}>
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
                        "cursor-pointer transition-colors hover:bg-muted/50",
                        selectedOrderId === order.id && "bg-muted/50"
                      )}
                      onClick={() => setSelectedOrderId(order.id)}
                    >
                      <TableCell className="font-medium">
                        {order.orderNumber}
                      </TableCell>
                      <TableCell className="max-w-44 truncate">
                        {order.locationNameSnapshot}
                      </TableCell>
                      <TableCell className="text-muted-foreground">
                        #{order.restaurantId}
                      </TableCell>
                      <TableCell className="max-w-56 truncate text-muted-foreground">
                        {materials || "—"}
                      </TableCell>
                      <TableCell>
                        {formatAdminShopGbpFromPence(order.grossPence)}
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline">{order.paymentStatus}</Badge>
                      </TableCell>
                      <TableCell>
                        <Badge variant="secondary">
                          {adminShopFulfilmentLabel(order.fulfilmentStatus)}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-muted-foreground">
                        {order.paidAtUtc
                          ? new Date(order.paidAtUtc).toLocaleDateString()
                          : "—"}
                      </TableCell>
                      <TableCell className="text-right text-sm text-muted-foreground">
                        {nextAction?.label ?? "—"}
                      </TableCell>
                    </TableRow>
                  )
                })}

              {!loading && orders.length === 0 && (
                <TableRow>
                  <TableCell
                    colSpan={9}
                    className="h-32 text-center text-muted-foreground"
                  >
                    No shop orders match this filter.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>

        {!loading && totalCount > 0 && (
          <div className="flex flex-col gap-3 rounded-b-2xl border-t px-4 py-4 sm:flex-row sm:items-center sm:justify-between">
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
      </Card>

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

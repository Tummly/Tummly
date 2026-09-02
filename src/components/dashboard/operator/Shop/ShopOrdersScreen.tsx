import { useState, useMemo, useEffect, useCallback } from "react"
import { isAxiosError } from "axios"
import {
  ChevronRight,
  MapPin,
  ChevronDown,
  Search,
  X,
  Inbox,
  MoreVertical,
} from "lucide-react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import { ShopOrderDetailSidebar } from "@/components/dashboard/operator/Shop/ShopOrderDetailSidebar"
import {
  shopOrdersFilterSheetSchema,
  type DetailedShopOrder,
} from "@/lib/operatorShop/shopOrdersFilterSheetSchema"
import { buildShopOrdersListQueryParams } from "@/lib/operatorShop/shopOrdersListQueryParams"
import { mapShopOrdersListResponse, mapShopOrderDetailToRow } from "@/lib/operatorShop/mapShopOrdersApiResponse"
import { fetchShopOrdersList, fetchShopOrder, cancelShopOrder, reorderShopOrder, type ShopReorderPrefillWire } from "@/api/shopOrdersApi"
import {
  emptySelection,
  openSession,
  projectChips,
  removeAppliedChip,
  type FilterChip,
  type FilterSheetSession,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import { cn } from "@/lib/utils"

export type { DetailedShopOrder }

const PAGE_SIZE = 25

type ShopOrdersScreenProps = {
  selectedLocationId: number
  selectedLocationName: string
  locations: Array<{ id: number; locationName: string; address: string }>
  onSelectLocation?: (locationId: number) => void
  onBackToShop: () => void
  onReorder?: (input: {
    order: DetailedShopOrder
    prefill: ShopReorderPrefillWire
  }) => void
}

export function ShopOrdersScreen({
  selectedLocationId,
  selectedLocationName,
  locations,
  onSelectLocation,
  onBackToShop,
  onReorder,
}: ShopOrdersScreenProps) {
  const [searchQuery, setSearchQuery] = useState("")
  const [debouncedSearch, setDebouncedSearch] = useState("")
  const [isFilterSheetOpen, setIsFilterSheetOpen] = useState(false)
  const [orders, setOrders] = useState<DetailedShopOrder[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [listLoading, setListLoading] = useState(true)
  const [listError, setListError] = useState<string | null>(null)
  const [aggregates, setAggregates] = useState({
    inProgress: 0,
    dispatched: 0,
    deliveredLast90Days: 0,
  })
  const [viewingOrderId, setViewingOrderId] = useState<string | null>(null)
  const [selectedOrder, setSelectedOrder] = useState<DetailedShopOrder | null>(
    null
  )

  const schema = useMemo(
    () =>
      shopOrdersFilterSheetSchema({
        locations: locations.map((loc) => ({
          id: String(loc.id),
          label: loc.locationName,
        })),
      }),
    [locations]
  )

  const [filterSelection, setFilterSelection] = useState<OperatorFilterSelection>(
    () => emptySelection(schema)
  )
  const [filterSession, setFilterSession] = useState<FilterSheetSession | null>(
    null
  )

  const activeFilterChips = useMemo(
    () => projectChips(schema, filterSelection),
    [schema, filterSelection]
  )

  const handleOpenFilters = () => {
    setFilterSession(openSession(filterSelection))
    setIsFilterSheetOpen(true)
  }

  const handleApplyFilters = (nextSelection: OperatorFilterSelection) => {
    setFilterSelection(nextSelection)
    setPage(1)
  }

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedSearch(searchQuery.trim())
      setPage(1)
    }, 300)
    return () => window.clearTimeout(timer)
  }, [searchQuery])

  const loadOrders = useCallback(async () => {
    setListLoading(true)
    setListError(null)
    try {
      const params = buildShopOrdersListQueryParams({
        locationId: selectedLocationId,
        q: debouncedSearch,
        filters: filterSelection,
        page,
        pageSize: PAGE_SIZE,
      })
      const response = await fetchShopOrdersList(params)
      const mapped = mapShopOrdersListResponse(response)
      setOrders(mapped.orders)
      setTotalCount(mapped.totalCount)
      setAggregates(mapped.aggregates)
    } catch {
      setListError("Could not load shop orders.")
      setOrders([])
      setTotalCount(0)
    } finally {
      setListLoading(false)
    }
  }, [debouncedSearch, filterSelection, page, selectedLocationId])

  useEffect(() => {
    void loadOrders()
  }, [loadOrders])

  const handleViewOrder = async (order: DetailedShopOrder) => {
    setViewingOrderId(order.id)
    try {
      const detail = await fetchShopOrder(order.id, selectedLocationId)
      setSelectedOrder(mapShopOrderDetailToRow(detail))
    } catch {
      toast.error("Could not load order details.")
      setSelectedOrder(null)
    } finally {
      setViewingOrderId(null)
    }
  }

  const pageStart = totalCount === 0 ? 0 : (page - 1) * PAGE_SIZE + 1
  const pageEnd = Math.min(page * PAGE_SIZE, totalCount)
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  const handleRemoveFilterChip = (chip: FilterChip) => {
    setFilterSelection((prev) => removeAppliedChip(schema, prev, chip))
    setPage(1)
  }

  const handleClearAllFilters = () => {
    setFilterSelection(emptySelection(schema))
    setSearchQuery("")
    setPage(1)
  }

  const handleCancelOrder = async (
    order: DetailedShopOrder,
    reasonSlug: string
  ) => {
    const locationId =
      typeof order.locationId === "number"
        ? order.locationId
        : selectedLocationId

    try {
      const updated = await cancelShopOrder({
        orderId: order.id,
        locationId,
        reason: reasonSlug,
      })
      const mapped = mapShopOrderDetailToRow(updated)
      setOrders((prev) =>
        prev.map((row) => (row.id === mapped.id ? mapped : row))
      )
      setSelectedOrder((prev) =>
        prev && prev.id === mapped.id ? mapped : prev
      )
      toast.success(`Order ${mapped.orderNumber} cancelled`)
    } catch (error) {
      if (isAxiosError(error) && error.response?.status === 409) {
        toast.error("This order can no longer be cancelled.")
        return
      }
      toast.error("Could not cancel order.")
    }
  }

  const handleReorder = async (order: DetailedShopOrder) => {
    const locationId =
      typeof order.locationId === "number"
        ? order.locationId
        : selectedLocationId

    try {
      const prefill = await reorderShopOrder({
        orderId: order.id,
        locationId,
      })
      if (onReorder) {
        onReorder({ order, prefill })
      } else {
        toast.success(`Reviewing reorder for ${order.orderNumber}`)
      }
    } catch (error) {
      if (isAxiosError(error) && error.response?.status === 409) {
        const code = error.response.data?.code
        if (code === "catalog_sku_unavailable") {
          toast.error("One or more materials are no longer available.")
          return
        }
        toast.error("This order cannot be reordered.")
        return
      }
      toast.error("Could not start reorder.")
    }
  }

  return (
    <div className="flex flex-col gap-6 pb-20">
      {/* Top Breadcrumb */}
      <div className="flex items-center gap-2 text-sm font-medium">
        <button
          type="button"
          onClick={onBackToShop}
          className="text-op-text-primary transition-colors hover:underline"
        >
          Tummly Shop
        </button>
        <ChevronRight className="size-3.5 text-op-text-muted" />
        <span className="text-op-text-muted">Orders</span>
      </div>

      {/* Header & Location Switcher */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex flex-col gap-1">
          <h1 className="text-2xl font-bold tracking-tight text-op-text-primary sm:text-3xl">
            Orders
          </h1>
          <p className="text-sm font-normal text-op-text-muted">
            Track material orders, deliveries, payments and invoices for your permitted locations.
          </p>
        </div>

        <div className="flex items-center self-start sm:self-center">
          {locations.length > 1 && onSelectLocation ? (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  size="sm"
                  className="h-9 gap-1.5 rounded-md border border-op-border-default bg-op-surface-secondary px-3.5 text-xs font-medium text-op-text-primary hover:bg-op-card-background"
                >
                  <MapPin className="size-3.5 text-op-text-muted" />
                  <span className="max-w-[120px] truncate">
                    {selectedLocationName}
                  </span>
                  <ChevronDown className="size-3 text-op-text-muted" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent
                align="end"
                className="w-48 border-op-border-default bg-op-surface-secondary text-op-text-primary"
              >
                {locations.map((loc) => (
                  <DropdownMenuItem
                    key={loc.id}
                    onClick={() => onSelectLocation(loc.id)}
                    className={cn(
                      "cursor-pointer text-xs",
                      loc.locationName === selectedLocationName &&
                      "font-semibold text-op-action-primary"
                    )}
                  >
                    {loc.locationName}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          ) : (
            <div className="inline-flex h-9 items-center gap-1.5 rounded-md border border-op-border-default bg-op-surface-secondary px-3.5 text-xs font-medium text-op-text-primary">
              <MapPin className="size-3.5 text-op-text-muted" />
              <span className="max-w-[140px] truncate">
                {selectedLocationName}
              </span>
            </div>
          )}
        </div>
      </div>

      {/* KPI Cards Row */}
      <div className="rounded-md border border-op-border-default bg-op-card-background p-6">
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-3">
          <div className="flex flex-col gap-1">
            <span className="text-sm font-medium text-op-text-secondary">
              Orders in progress
            </span>
            <span className="text-3xl font-extrabold leading-none text-op-text-primary">
              {aggregates.inProgress}
            </span>
            <span className="text-xs text-op-text-muted">
              Orders being processed or produced
            </span>
          </div>

          <div className="flex flex-col gap-1 sm:border-l sm:border-op-border-default/60 sm:pl-6">
            <span className="text-sm font-medium text-op-text-secondary">
              Dispatched
            </span>
            <span className="text-3xl font-extrabold leading-none text-op-text-primary">
              {aggregates.dispatched}
            </span>
            <span className="text-xs text-op-text-muted">
              Orders currently on their way
            </span>
          </div>

          <div className="flex flex-col gap-1 sm:border-l sm:border-op-border-default/60 sm:pl-6">
            <span className="text-sm font-medium text-op-text-secondary">
              Delivered
            </span>
            <span className="text-3xl font-extrabold leading-none text-op-text-primary">
              {aggregates.deliveredLast90Days}
            </span>
            <span className="text-xs text-op-text-muted">
              Orders delivered in the last 90 days
            </span>
          </div>
        </div>
      </div>

      {/* Main Table Card Container */}
      <div className="flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6">
        {/* Search and Filters Toolbar */}
        <div className="flex flex-col gap-3">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <div className="relative flex-1">
              <Search className="absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-op-text-muted" />
              <Input
                type="search"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search by order number, material or location"
                className="h-10 w-full rounded-[4px] border-op-border-default bg-op-background-primary pl-9.5 pr-4 text-xs text-op-text-primary placeholder:text-op-text-muted focus-visible:ring-1 focus-visible:ring-op-action-primary"
              />
            </div>

            <Button
              type="button"
              variant="op-secondary"
              onClick={handleOpenFilters}
              className="h-10 shrink-0 rounded-[4px] px-4 text-xs font-medium"
            >
              Filters{activeFilterChips.length > 0 ? ` (${activeFilterChips.length})` : ""}
            </Button>
          </div>

          {/* Active Filter Pills */}
          {activeFilterChips.length > 0 && (
            <div className="flex flex-wrap items-center gap-2 pt-1">
              {activeFilterChips.map((chip) => (
                <div
                  key={chip.id}
                  className="flex items-center gap-1.5 rounded-[4px] border border-op-border-default bg-op-surface-secondary px-3 py-1.5 text-xs text-op-text-primary transition-colors hover:bg-op-background-primary"
                >
                  <span>{chip.label}</span>
                  <button
                    type="button"
                    onClick={() => handleRemoveFilterChip(chip)}
                    className="text-op-text-muted hover:text-op-text-primary"
                    aria-label={`Remove filter ${chip.label}`}
                  >
                    <X className="size-3" />
                  </button>
                </div>
              ))}

              <button
                type="button"
                onClick={handleClearAllFilters}
                className="text-xs font-medium text-op-text-muted underline-offset-4 hover:text-op-text-primary hover:underline ml-1"
              >
                Clear all
              </button>
            </div>
          )}
        </div>

        {/* Orders Table */}
        <>
            {listLoading ? (
              <div className="py-16 text-center text-sm text-op-text-muted">
                Loading orders…
              </div>
            ) : listError != null ? (
              <div className="flex flex-col items-center justify-center gap-3 py-16 text-center">
                <p className="text-sm text-op-text-muted">{listError}</p>
                <Button
                  type="button"
                  variant="op-secondary"
                  size="sm"
                  onClick={() => void loadOrders()}
                >
                  Retry
                </Button>
              </div>
            ) : orders.length === 0 ? (
              <div className="flex flex-col items-center justify-center gap-3 rounded-[4px] border border-dashed border-op-border-default/80 py-16 text-center">
                <div className="flex size-12 items-center justify-center rounded-full bg-op-surface-secondary text-op-text-muted">
                  <Inbox className="size-6" />
                </div>
                <div className="flex flex-col gap-1">
                  <h3 className="text-sm font-semibold text-op-text-primary">
                    No orders match your criteria
                  </h3>
                  <p className="text-xs text-op-text-muted">
                    Try adjusting your search terms or clearing active filters.
                  </p>
                </div>
                <Button
                  type="button"
                  variant="op-secondary"
                  size="sm"
                  className="mt-2 rounded-[4px] text-xs"
                  onClick={handleClearAllFilters}
                >
                  Clear filters
                </Button>
              </div>
            ) : (
              <div className="overflow-x-auto rounded-[4px] border border-op-border-default">
                <table className="w-full min-w-[900px] border-collapse text-left text-xs">
                  <thead>
                    <tr className="border-b border-op-border-default bg-op-background-primary">
                      <th className="px-4 py-3 font-semibold text-op-text-primary">
                        Order
                      </th>
                      <th className="px-4 py-3 font-semibold text-op-text-primary">
                        Location
                      </th>
                      <th className="px-4 py-3 font-semibold text-op-text-primary">
                        Materials
                      </th>
                      <th className="px-4 py-3 font-semibold text-op-text-primary">
                        Placed by
                      </th>
                      <th className="px-4 py-3 font-semibold text-op-text-primary">
                        Total
                      </th>
                      <th className="px-4 py-3 font-semibold text-op-text-primary">
                        Payment
                      </th>
                      <th className="px-4 py-3 font-semibold text-op-text-primary">
                        Fulfilment
                      </th>
                      <th className="px-4 py-3 font-semibold text-op-text-primary">
                        Updated
                      </th>
                      <th className="px-4 py-3 text-center font-semibold text-op-text-primary">
                        Action
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {orders.map((order) => (
                      <tr
                        key={order.id}
                        className="border-b border-op-border-default/60 transition-colors hover:bg-op-surface-secondary/40 last:border-0"
                      >
                        <td className="px-4 py-3.5 font-normal text-op-text-primary">
                          {order.orderNumber} · {order.orderDate}
                        </td>
                        <td className="px-4 py-3.5 text-op-text-secondary">
                          {order.locationName}
                        </td>
                        <td className="px-4 py-3.5 text-op-text-primary">
                          {order.materials}
                        </td>
                        <td className="px-4 py-3.5 text-op-text-secondary">
                          {order.placedBy}
                        </td>
                        <td className="px-4 py-3.5 font-medium text-op-text-primary">
                          {order.total}
                        </td>
                        <td className="px-4 py-3.5 text-op-text-primary">
                          {order.paymentStatus}
                        </td>
                        <td className="px-4 py-3.5">
                          <span className="inline-flex items-center rounded-xs border border-op-border-default bg-op-surface-secondary px-2.5 py-1 text-[11px] font-medium text-op-text-primary">
                            {order.fulfilmentStatus}
                          </span>
                        </td>
                        <td className="px-4 py-3.5 text-op-text-secondary">
                          {order.updatedDate}
                        </td>
                        <td className="px-4 py-3.5 text-center">
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            className="h-8.5 rounded-[4px] border-op-border-default bg-transparent px-3 text-xs text-op-text-primary hover:bg-op-surface-secondary"
                            disabled={viewingOrderId === order.id}
                            onClick={() => void handleViewOrder(order)}
                          >
                            {viewingOrderId === order.id
                              ? "Loading…"
                              : "View order"}
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {/* Bottom Pagination for Orders */}
            <div className="flex items-center justify-between pt-2 text-xs text-op-text-muted">
              <span>
                Showing {pageStart}–{pageEnd} of {totalCount} orders
              </span>
              <div className="flex items-center gap-3">
                <Button
                  type="button"
                  variant="op-secondary"
                  size="sm"
                  className="h-8.5 rounded-[4px] px-3.5 text-xs"
                  disabled={page <= 1 || listLoading}
                  onClick={() => setPage((current) => Math.max(1, current - 1))}
                >
                  Previous
                </Button>
                <Button
                  type="button"
                  variant="op-secondary"
                  size="sm"
                  className="h-8.5 rounded-[4px] px-3.5 text-xs"
                  disabled={page >= totalPages || listLoading}
                  onClick={() =>
                    setPage((current) => Math.min(totalPages, current + 1))
                  }
                >
                  Next
                </Button>
              </div>
            </div>
          </>
      </div>

      {/* Order Filter Sheet Dialog */}
      <OperatorFilterSheetDialog
        open={isFilterSheetOpen}
        title="Filters"
        schema={schema}
        session={filterSession}
        onSessionChange={setFilterSession}
        onOpenChange={setIsFilterSheetOpen}
        onApply={handleApplyFilters}
      />

      {/* Order Detail Sidebar */}
      <ShopOrderDetailSidebar
        order={selectedOrder}
        open={selectedOrder !== null}
        onOpenChange={(open) => {
          if (!open) setSelectedOrder(null)
        }}
        onReorder={handleReorder}
        onCancelOrder={handleCancelOrder}
      />
    </div>
  )
}

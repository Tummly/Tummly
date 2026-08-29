import { useState } from "react"
import {
  ChevronRight,
  MapPin,
  ChevronDown,
  Search,
  X,
  Package,
  Clock,
  FileText,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  Drawer,
  DrawerContent,
  DrawerHeader,
  DrawerTitle,
  DrawerDescription,
  DrawerClose,
} from "@/components/ui/drawer"
import { cn } from "@/lib/utils"

export type DetailedShopOrder = {
  id: string
  orderNumber: string
  orderDate: string
  locationName: string
  materials: string
  placedBy: string
  total: string
  paymentStatus: "Paid" | "Pending" | "Refunded"
  fulfilmentStatus: "In production" | "Dispatched" | "Delivered" | "Processing"
  updatedDate: string
  items?: string[]
}

const DEFAULT_ORDERS: DetailedShopOrder[] = [
  {
    id: "ord-10428",
    orderNumber: "#TM-10428",
    orderDate: "29 Jul 2026",
    locationName: "Padella · Borough Market",
    materials: "Table tents · Pack of 20",
    placedBy: "Mohamed Mahmoud",
    total: "£82.80",
    paymentStatus: "Paid",
    fulfilmentStatus: "In production",
    updatedDate: "30 Jul 2026",
    items: [
      "20x Table tents (Matte Frosted Acrylic)",
      "4x Window stickers (Static Cling)",
    ],
  },
  {
    id: "ord-10429",
    orderNumber: "#TM-10428",
    orderDate: "29 Jul 2026",
    locationName: "Padella · Borough Market",
    materials: "Table tents · Pack of 20",
    placedBy: "Mohamed Mahmoud",
    total: "£82.80",
    paymentStatus: "Paid",
    fulfilmentStatus: "In production",
    updatedDate: "30 Jul 2026",
    items: [
      "20x Table tents (Matte Frosted Acrylic)",
      "2x Counter cards (Soft-touch Recycled Card)",
    ],
  },
  {
    id: "ord-10430",
    orderNumber: "#TM-10428",
    orderDate: "29 Jul 2026",
    locationName: "Padella · Borough Market",
    materials: "Table tents · Pack of 20",
    placedBy: "Mohamed Mahmoud",
    total: "£82.80",
    paymentStatus: "Paid",
    fulfilmentStatus: "In production",
    updatedDate: "30 Jul 2026",
    items: [
      "20x Table tents (Matte Frosted Acrylic)",
      "50x Bill presenter cards",
    ],
  },
]

type ShopOrdersScreenProps = {
  selectedLocationName: string
  locations: Array<{ id: number; locationName: string; address: string }>
  onSelectLocation?: (locationId: number) => void
  onBackToShop: () => void
}

export function ShopOrdersScreen({
  selectedLocationName,
  locations,
  onSelectLocation,
  onBackToShop,
}: ShopOrdersScreenProps) {
  const [activeTab, setActiveTab] = useState<"orders" | "drafts">("orders")
  const [searchQuery, setSearchQuery] = useState("")
  const [activeFilters, setActiveFilters] = useState<string[]>([
    "Eligible to contact",
    "Negative",
    "Camden",
  ])
  const [selectedOrder, setSelectedOrder] = useState<DetailedShopOrder | null>(
    null
  )

  const handleRemoveFilter = (filter: string) => {
    setActiveFilters((prev) => prev.filter((f) => f !== filter))
  }

  const filteredOrders = DEFAULT_ORDERS.filter((order) => {
    if (!searchQuery.trim()) return true
    const q = searchQuery.toLowerCase()
    return (
      order.orderNumber.toLowerCase().includes(q) ||
      order.materials.toLowerCase().includes(q) ||
      order.locationName.toLowerCase().includes(q) ||
      order.placedBy.toLowerCase().includes(q)
    )
  })

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
              0
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
              0
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
              0
            </span>
            <span className="text-xs text-op-text-muted">
              Orders delivered in the last 90 days
            </span>
          </div>
        </div>
      </div>

      {/* Tabs Navigation */}
      <div className="flex border-b border-op-border-default">
        <button
          type="button"
          onClick={() => setActiveTab("orders")}
          className={cn(
            "border-b-2 px-4 py-3 text-sm font-medium transition-colors",
            activeTab === "orders"
              ? "border-op-action-primary bg-op-card-background text-op-text-primary"
              : "border-transparent text-op-text-muted hover:text-op-text-primary"
          )}
        >
          Orders
        </button>
        <button
          type="button"
          onClick={() => setActiveTab("drafts")}
          className={cn(
            "border-b-2 px-4 py-3 text-sm font-medium transition-colors",
            activeTab === "drafts"
              ? "border-op-action-primary bg-op-card-background text-op-text-primary"
              : "border-transparent text-op-text-muted hover:text-op-text-primary"
          )}
        >
          Drafts
        </button>
      </div>

      {/* Main Table Card Container */}
      <div className="flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6">
        {/* Search and Filters Toolbar */}
        <div className="flex flex-col gap-3">
          <div className="flex items-center gap-3">
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
              className="h-10 shrink-0 rounded-[4px] px-4 text-xs font-medium"
            >
              Filters (3)
            </Button>
          </div>

          {/* Active Filter Pills */}
          {activeFilters.length > 0 && (
            <div className="flex flex-wrap items-center gap-2">
              {activeFilters.map((filter) => (
                <div
                  key={filter}
                  className="flex items-center gap-1.5 rounded-[4px] border border-op-border-default bg-op-surface-secondary px-3 py-1.5 text-xs text-op-text-primary transition-colors hover:bg-op-background-primary"
                >
                  <span>{filter}</span>
                  <button
                    type="button"
                    onClick={() => handleRemoveFilter(filter)}
                    className="text-op-text-muted hover:text-op-text-primary"
                    aria-label={`Remove filter ${filter}`}
                  >
                    <X className="size-3" />
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Orders Table */}
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
              {filteredOrders.map((order, idx) => (
                <tr
                  key={`${order.id}-${idx}`}
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
                      onClick={() => setSelectedOrder(order)}
                    >
                      View order
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Bottom Pagination */}
        <div className="flex items-center justify-between pt-2 text-xs text-op-text-muted">
          <span>
            Showing 1–{filteredOrders.length} of 1,248 orders
          </span>
          <div className="flex items-center gap-3">
            <Button
              type="button"
              variant="op-secondary"
              size="sm"
              className="h-8.5 rounded-[4px] px-3.5 text-xs opacity-50"
              disabled
            >
              Previous
            </Button>
            <Button
              type="button"
              variant="op-secondary"
              size="sm"
              className="h-8.5 rounded-[4px] px-3.5 text-xs"
            >
              Next
            </Button>
          </div>
        </div>
      </div>

      {/* Order Detail Drawer */}
      <Drawer
        open={selectedOrder !== null}
        onOpenChange={(open) => {
          if (!open) setSelectedOrder(null)
        }}
      >
        <DrawerContent className="z-[120] border-op-border-default bg-op-card-background text-op-text-primary sm:max-w-lg">
          {selectedOrder && (
            <div className="mx-auto flex w-full max-w-md flex-col gap-6 p-6">
              <DrawerHeader className="p-0 text-left">
                <div className="flex items-center justify-between">
                  <DrawerTitle className="text-lg font-bold text-op-text-primary">
                    Order {selectedOrder.orderNumber}
                  </DrawerTitle>
                  <DrawerClose asChild>
                    <button
                      type="button"
                      className="flex size-7 items-center justify-center rounded-[4px] bg-op-surface-secondary text-op-text-muted hover:text-op-text-primary"
                    >
                      ✕
                    </button>
                  </DrawerClose>
                </div>
                <DrawerDescription className="text-xs text-op-text-muted">
                  Placed on {selectedOrder.orderDate} for {selectedOrder.locationName}
                </DrawerDescription>
              </DrawerHeader>

              {/* Status progression card */}
              <div className="flex flex-col gap-3 rounded-md border border-op-border-default bg-op-background-primary p-4 text-xs">
                <div className="flex items-center justify-between">
                  <span className="font-semibold text-op-text-primary">
                    Production Status
                  </span>
                  <span className="rounded-xs border border-op-border-default bg-op-surface-secondary px-2 py-0.5 text-[11px] font-medium">
                    {selectedOrder.fulfilmentStatus}
                  </span>
                </div>
                <div className="flex items-center gap-2 text-op-text-secondary">
                  <Clock className="size-4 text-op-action-primary" />
                  <span>Estimated dispatch in 2-3 business days</span>
                </div>
              </div>

              {/* Items in order */}
              <div className="flex flex-col gap-2.5">
                <h4 className="text-xs font-semibold uppercase tracking-wider text-op-text-muted">
                  Ordered Items
                </h4>
                <div className="flex flex-col gap-2 rounded-md border border-op-border-default bg-op-background-primary p-3.5 text-xs">
                  {selectedOrder.items?.map((item, i) => (
                    <div
                      key={i}
                      className="flex items-center gap-2 text-op-text-primary"
                    >
                      <Package className="size-3.5 text-op-text-muted" />
                      <span>{item}</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Pricing & Invoice */}
              <div className="flex flex-col gap-2 border-t border-op-border-default/60 pt-4 text-xs text-op-text-secondary">
                <div className="flex justify-between">
                  <span>Payment status:</span>
                  <span className="font-medium text-op-text-primary">
                    {selectedOrder.paymentStatus}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span>Total paid (inc VAT):</span>
                  <span className="font-bold text-op-text-primary">
                    {selectedOrder.total}
                  </span>
                </div>
              </div>

              <div className="flex items-center gap-3 pt-2">
                <Button
                  type="button"
                  variant="outline"
                  className="h-9 flex-1 gap-1.5 rounded-md border-op-border-default bg-transparent text-xs text-op-text-primary hover:bg-op-surface-secondary"
                  onClick={() => setSelectedOrder(null)}
                >
                  <FileText className="size-3.5" />
                  Download invoice
                </Button>
                <Button
                  type="button"
                  variant="op-primary"
                  className="h-9 flex-1 rounded-md text-xs font-medium"
                  onClick={() => setSelectedOrder(null)}
                >
                  Close
                </Button>
              </div>
            </div>
          )}
        </DrawerContent>
      </Drawer>
    </div>
  )
}

import { useState } from "react"
import { X, MoreVertical } from "lucide-react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetClose,
} from "@/components/ui/sheet"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { ShopReorderDialog } from "@/components/dashboard/operator/Shop/ShopReorderDialog"
import { ShopCancelOrderDialog } from "@/components/dashboard/operator/Shop/ShopCancelOrderDialog"
import { downloadOrderInvoice } from "@/lib/operatorShop/downloadOrderInvoice"
import type { DetailedShopOrder } from "@/lib/operatorShop/shopOrdersFilterSheetSchema"

type ShopOrderDetailSidebarProps = {
  order: DetailedShopOrder | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onReorder?: (order: DetailedShopOrder) => void
  onCancelOrder?: (order: DetailedShopOrder, reason: string) => void
  onViewMaterial?: (order: DetailedShopOrder) => void
}

export function ShopOrderDetailSidebar({
  order,
  open,
  onOpenChange,
  onReorder,
  onCancelOrder,
  onViewMaterial,
}: ShopOrderDetailSidebarProps) {
  const [isReorderOpen, setIsReorderOpen] = useState(false)
  const [isCancelOrderOpen, setIsCancelOrderOpen] = useState(false)

  if (!order) return null

  const isCompletedStep = (step: "received" | "processing" | "dispatched" | "delivered") => {
    const status = order.fulfilmentStatus.toLowerCase()
    switch (step) {
      case "received":
        return true
      case "processing":
        return (
          status === "processing" ||
          status === "in production" ||
          status === "dispatched" ||
          status === "delivered"
        )
      case "dispatched":
        return status === "dispatched" || status === "delivered"
      case "delivered":
        return status === "delivered"
      default:
        return false
    }
  }

  const invoiceNumber = `INV-${order.orderNumber.replace("#", "")}`

  const handleDownloadInvoice = () => {
    downloadOrderInvoice(order)
    toast.success(`Downloaded invoice ${invoiceNumber}`)
  }

  const handleConfirmReorder = (ord: DetailedShopOrder) => {
    if (onReorder) {
      onReorder(ord)
    } else {
      toast.success(`Items from ${ord.orderNumber} added to cart for reorder`)
    }
  }

  const handleConfirmCancel = (ord: DetailedShopOrder, reason: string) => {
    if (onCancelOrder) {
      onCancelOrder(ord, reason)
    } else {
      toast.success(`Order ${ord.orderNumber} cancelled`)
    }
  }

  return (
    <>
      <Sheet open={open} onOpenChange={onOpenChange}>
        <SheetContent
          side="right"
          showCloseButton={false}
          className="z-[140] w-full max-w-full overflow-y-auto border-l border-op-border-default bg-op-card-background p-0 text-op-text-primary shadow-2xl sm:w-[535px] sm:max-w-[535px]"
        >
          <div className="flex flex-col justify-start pb-16">
            {/* Top Header Section */}
            <div className="flex items-start justify-between gap-4 px-6 pt-8 pb-6">
              <div className="flex flex-1 flex-col gap-4">
                <SheetHeader className="p-0 text-left">
                  <div className="flex flex-col gap-1.5">
                    <SheetTitle className="text-2xl font-bold tracking-tight text-op-text-primary">
                      Order {order.orderNumber}
                    </SheetTitle>
                    <p className="text-sm font-medium text-op-text-primary">
                      Placed on {order.orderDate} by {order.placedBy}
                    </p>
                    <p className="text-xs font-medium text-op-text-muted">
                      {order.locationName}
                    </p>
                  </div>
                </SheetHeader>

                <div>
                  <span className="inline-flex items-center rounded-xs bg-op-surface-secondary px-2.5 py-1 text-xs font-medium text-op-text-primary">
                    {order.fulfilmentStatus}
                  </span>
                </div>

                {/* Actions Row: Reorder & Menu */}
                <div className="flex items-center gap-3 pt-1">
                  <Button
                    type="button"
                    variant="op-secondary"
                    className="h-9.5 rounded-xs px-4 text-sm font-medium"
                    onClick={() => setIsReorderOpen(true)}
                  >
                    Reorder
                  </Button>

                  <DropdownMenu modal={false}>
                    <DropdownMenuTrigger asChild>
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        className="size-9.5 rounded-xs text-op-text-muted hover:bg-op-surface-secondary hover:text-op-text-primary"
                        aria-label="More order actions"
                      >
                        <MoreVertical className="size-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent
                      align="start"
                      className="z-[150] w-48 border-op-border-default bg-op-background-primary text-op-text-primary"
                    >
                      <DropdownMenuItem
                        onClick={handleDownloadInvoice}
                        className="cursor-pointer text-xs"
                      >
                        Download invoice
                      </DropdownMenuItem>
                      <DropdownMenuItem
                        onClick={() =>
                          toast.success(`Invoice emailed to ${order.placedBy}`)
                        }
                        className="cursor-pointer text-xs"
                      >
                        Email invoice
                      </DropdownMenuItem>
                      <DropdownMenuItem
                        onClick={() => toast.info("Opening support chat...")}
                        className="cursor-pointer text-xs"
                      >
                        Contact support
                      </DropdownMenuItem>
                      <DropdownMenuItem
                        onClick={() => setIsCancelOrderOpen(true)}
                        className="cursor-pointer text-xs text-red-500 hover:text-red-500 focus:bg-red-500/10 focus:text-red-500"
                      >
                        Cancel order
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
              </div>

              {/* Close Button */}
              <SheetClose asChild>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="size-9 shrink-0 rounded-xs bg-op-surface-secondary text-op-text-muted hover:bg-op-surface-secondary/80 hover:text-op-text-primary"
                  aria-label="Close panel"
                >
                  <X className="size-4" />
                </Button>
              </SheetClose>
            </div>

            {/* Section 1: Order Progress */}
            <div className="flex flex-col gap-4 border-t border-op-border-default p-6">
              <h3 className="text-lg font-bold text-op-text-primary">
                Order Progress
              </h3>

              {/* Step 1: Order received */}
              <div className="flex flex-col gap-1.5">
                <div className="flex flex-col gap-0.5">
                  <span className="text-sm font-medium text-op-text-primary">
                    Order received
                  </span>
                  <span className="text-xs text-op-text-primary">
                    Your order was successfully confirmed.
                  </span>
                </div>
                <span className="text-xs text-op-text-muted">
                  {order.orderDate} at 10:24
                </span>
              </div>

              {/* Line 1 */}
              <div className="h-10 pl-2">
                <div
                  className={`h-full w-0.5 ${
                    isCompletedStep("processing")
                      ? "bg-op-action-primary"
                      : "bg-op-border-default"
                  }`}
                />
              </div>

              {/* Step 2: Processing */}
              <div className="flex flex-col gap-1.5">
                <div className="flex flex-col gap-0.5">
                  <span className="text-sm font-medium text-op-text-primary">
                    Processing
                  </span>
                  <span className="text-xs text-op-text-primary">
                    Your physical materials are being produced.
                  </span>
                </div>
                <span className="text-xs text-op-text-muted">
                  {order.orderDate} at 11:05
                </span>
              </div>

              {/* Line 2 */}
              <div className="h-10 pl-2">
                <div
                  className={`h-full w-0.5 ${
                    isCompletedStep("dispatched")
                      ? "bg-op-action-primary"
                      : "bg-op-border-default"
                  }`}
                />
              </div>

              {/* Step 3: Dispatched */}
              <div className="flex flex-col gap-1.5">
                <div className="flex flex-col gap-0.5">
                  <span
                    className={`text-sm font-medium ${
                      isCompletedStep("dispatched")
                        ? "text-op-text-primary"
                        : "text-op-text-muted"
                    }`}
                  >
                    Dispatched
                  </span>
                  <span className="text-xs text-op-text-muted">
                    Tracking information will appear here after dispatch.
                  </span>
                </div>
                <span className="text-xs text-op-text-muted">
                  {order.fulfilmentStatus === "Dispatched" ||
                  order.fulfilmentStatus === "Delivered"
                    ? `${order.updatedDate}`
                    : "Expected [date]"}
                </span>
              </div>

              {/* Line 3 */}
              <div className="h-10 pl-2">
                <div
                  className={`h-full w-0.5 ${
                    isCompletedStep("delivered")
                      ? "bg-op-action-primary"
                      : "bg-op-border-default"
                  }`}
                />
              </div>

              {/* Step 4: Delivered */}
              <div className="flex flex-col gap-1.5">
                <div className="flex flex-col gap-0.5">
                  <span
                    className={`text-sm font-medium ${
                      isCompletedStep("delivered")
                        ? "text-op-text-primary"
                        : "text-op-text-muted"
                    }`}
                  >
                    Delivered
                  </span>
                </div>
                <span className="text-xs text-op-text-muted">
                  {order.fulfilmentStatus === "Delivered"
                    ? `${order.updatedDate}`
                    : "Expected [date range]"}
                </span>
              </div>
            </div>

            {/* Section 2: Materials */}
            <div className="flex flex-col gap-5 border-t border-op-border-default p-6">
              <h3 className="text-lg font-semibold text-op-text-primary">
                Materials
              </h3>

              <div className="flex flex-col gap-5 rounded-sm border border-op-border-default/50 bg-op-background-primary p-6">
                <div className="flex flex-col gap-1">
                  <h4 className="text-base font-semibold text-op-text-primary">
                    {order.materials.split("·")[0]?.trim() || "Table tents"}
                  </h4>
                  <p className="text-sm font-medium text-op-text-muted">
                    A5 folded card · Double-sided · Matte finish
                  </p>
                </div>

                <div className="flex flex-col gap-3.5 text-sm">
                  <div className="flex justify-between items-center">
                    <span className="text-op-text-muted">Quantity</span>
                    <span className="text-op-text-primary font-medium">
                      {order.materials.split("·")[1]?.trim() || "Pack of 20"}
                    </span>
                  </div>

                  <div className="flex justify-between items-center">
                    <span className="text-op-text-muted">Location</span>
                    <span className="text-op-text-primary font-medium">
                      {order.locationName}
                    </span>
                  </div>

                  <div className="flex justify-between items-center">
                    <span className="text-op-text-muted">Price</span>
                    <span className="text-op-text-primary font-medium">
                      {order.total}
                    </span>
                  </div>

                  <div className="flex justify-between items-center">
                    <span className="text-op-text-muted">Status</span>
                    <span className="text-op-text-primary font-medium">
                      {order.fulfilmentStatus}
                    </span>
                  </div>
                </div>

                <div>
                  <Button
                    type="button"
                    variant="op-secondary"
                    className="h-9.5 rounded-xs px-4 text-sm font-medium"
                    onClick={() => {
                      if (onViewMaterial) {
                        onViewMaterial(order)
                      } else {
                        toast.info(
                          `Viewing material specifications for ${order.materials}`
                        )
                      }
                    }}
                  >
                    View material
                  </Button>
                </div>
              </div>
            </div>

            {/* Section 3: Delivery */}
            <div className="flex flex-col gap-5 border-t border-op-border-default p-6">
              <h3 className="text-lg font-semibold text-op-text-primary">
                Delivery
              </h3>

              <div className="flex flex-col gap-4">
                <div className="flex flex-col gap-1">
                  <span className="text-sm font-semibold text-op-text-primary">
                    Delivery address
                  </span>
                  <p className="text-sm text-op-text-muted leading-relaxed">
                    {order.placedBy} · {order.locationName} · 6 Southwark Street, London SE1 1TQ, United Kingdom · +44 20 7407 1234
                  </p>
                </div>

                <div className="flex flex-col gap-1">
                  <span className="text-sm font-semibold text-op-text-primary">
                    Delivery method
                  </span>
                  <p className="text-sm text-op-text-muted">Standard delivery</p>
                </div>

                <div className="flex flex-col gap-1">
                  <span className="text-sm font-semibold text-op-text-primary">
                    Estimated delivery
                  </span>
                  <p className="text-sm text-op-text-muted">
                    [Date or date range]
                  </p>
                </div>

                <div className="flex flex-col gap-1">
                  <span className="text-sm font-semibold text-op-text-primary">
                    Delivery instructions
                  </span>
                  <p className="text-sm text-op-text-muted leading-relaxed">
                    Please deliver through the restaurant’s side entrance and ask for the duty manager.
                  </p>
                </div>
              </div>
            </div>

            {/* Section 4: Payment summary */}
            <div className="flex flex-col gap-5 border-t border-op-border-default p-6">
              <h3 className="text-lg font-semibold text-op-text-primary">
                Payment summary
              </h3>

              <div className="flex flex-col gap-3 text-sm">
                <div className="flex justify-between items-center">
                  <span className="text-op-text-muted">Materials subtotal:</span>
                  <span className="text-op-text-primary font-medium">{order.total}</span>
                </div>

                <div className="flex justify-between items-center">
                  <span className="text-op-text-muted">Starter Kit allowance:</span>
                  <span className="text-op-text-muted">−£[allowance]</span>
                </div>

                <div className="flex justify-between items-center">
                  <span className="text-op-text-muted">Delivery:</span>
                  <span className="text-op-text-muted">£[delivery]</span>
                </div>

                <div className="flex justify-between items-center">
                  <span className="text-op-text-muted">VAT:</span>
                  <span className="text-op-text-muted">£[VAT]</span>
                </div>

                <div className="flex justify-between items-center pt-1 font-bold">
                  <span className="text-op-text-primary">Order total:</span>
                  <span className="text-op-text-primary text-base font-bold">{order.total}</span>
                </div>
              </div>

              <div className="flex flex-col gap-2 rounded-sm border border-op-border-default/50 bg-op-background-primary p-6">
                <span className="text-base font-semibold text-op-text-primary">
                  Payment method
                </span>
                <span className="text-sm text-op-text-muted">
                  Visa ending in 4242 · Paid on {order.orderDate}
                </span>
              </div>
            </div>

            {/* Section 5: Invoice */}
            <div className="flex flex-col gap-5 border-t border-op-border-default p-6">
              <h3 className="text-lg font-semibold text-op-text-primary">
                Invoice
              </h3>

              <div className="flex flex-col gap-3">
                <div className="flex items-center gap-2.5">
                  <span className="text-lg font-medium text-op-text-primary">
                    {invoiceNumber}
                  </span>
                  <span className="rounded-xs bg-green-600/20 px-2 py-0.5 text-xs font-medium text-green-500">
                    Paid
                  </span>
                </div>

                <div className="flex items-center gap-1.5 text-sm">
                  <span className="font-medium text-op-text-primary">Invoice email:</span>
                  <span className="text-op-text-muted">
                    Sent to {order.placedBy.toLowerCase().replace(/\s+/g, ".")}@padella.co.uk
                  </span>
                </div>
              </div>

              <div className="flex items-center gap-3 pt-1">
                <Button
                  type="button"
                  variant="op-secondary"
                  className="h-10 rounded-xs px-4 text-sm font-medium"
                  onClick={handleDownloadInvoice}
                >
                  Download invoice
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="h-10 rounded-xs border-op-border-default bg-transparent px-4 text-sm font-medium text-op-text-primary hover:bg-op-surface-secondary"
                  onClick={() => toast.success(`Invoice emailed again to ${order.placedBy}`)}
                >
                  Email invoice again
                </Button>
              </div>
            </div>
          </div>
        </SheetContent>
      </Sheet>

      {/* Reorder Dialog Modal */}
      <ShopReorderDialog
        order={order}
        open={isReorderOpen}
        onOpenChange={setIsReorderOpen}
        onConfirmReorder={handleConfirmReorder}
      />

      {/* Cancel Order Dialog Modal */}
      <ShopCancelOrderDialog
        order={order}
        open={isCancelOrderOpen}
        onOpenChange={setIsCancelOrderOpen}
        onConfirmCancel={handleConfirmCancel}
      />
    </>
  )
}

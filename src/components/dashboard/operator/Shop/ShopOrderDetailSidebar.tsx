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

function penceToPounds(pence: number): string {
  return `£${(pence / 100).toFixed(2)}`
}

function formatProgressTimestamp(iso: string | null | undefined): string | null {
  if (iso == null) return null
  return new Date(iso).toLocaleString("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  })
}

function formatShipToAddress(input: {
  contactName: string
  contactPhone?: string | null
  addressLine1: string
  addressLine2?: string | null
  postcode: string
  country: string
}): string {
  return [
    input.contactName,
    input.addressLine1,
    input.addressLine2,
    input.postcode,
    input.country,
    input.contactPhone,
  ]
    .filter((part) => part != null && String(part).trim().length > 0)
    .join(" · ")
}

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
    const status = order.fulfilmentStatus
    switch (step) {
      case "received":
        return true
      case "processing":
        return (
          status === "Processing" ||
          status === "Dispatched" ||
          status === "Delivered"
        )
      case "dispatched":
        return status === "Dispatched" || status === "Delivered"
      case "delivered":
        return status === "Delivered"
      default:
        return false
    }
  }

  const detail = order.detail
  const progress = detail?.progress
  const receivedAt =
    formatProgressTimestamp(progress?.orderReceivedAtUtc) ?? order.orderDate
  const processingAt = formatProgressTimestamp(progress?.processingStartedAtUtc)
  const dispatchedAt = formatProgressTimestamp(progress?.dispatchedAtUtc)
  const deliveredAt = formatProgressTimestamp(progress?.deliveredAtUtc)
  const trackingUrl = progress?.trackingUrl ?? null

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
                <span className="text-xs text-op-text-muted">{receivedAt}</span>
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
                  {processingAt ?? "Pending"}
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
                    {trackingUrl != null
                      ? "Track your shipment using the link below."
                      : "Tracking information will appear here after dispatch."}
                  </span>
                </div>
                <span className="text-xs text-op-text-muted">
                  {dispatchedAt ?? "Pending"}
                </span>
                {trackingUrl != null ? (
                  <a
                    href={trackingUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="text-xs font-medium text-op-action-primary underline-offset-2 hover:underline"
                  >
                    Open tracking link
                  </a>
                ) : null}
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
                  {deliveredAt ?? "Pending"}
                </span>
              </div>
            </div>

            {/* Section 2: Materials */}
            <div className="flex flex-col gap-5 border-t border-op-border-default p-6">
              <h3 className="text-lg font-semibold text-op-text-primary">
                Materials
              </h3>

              {(detail?.lines ?? []).length > 0 ? (
                detail!.lines.map((line) => (
                  <div
                    key={line.skuId}
                    className="flex flex-col gap-5 rounded-sm border border-op-border-default/50 bg-op-background-primary p-6"
                  >
                    <div className="flex flex-col gap-1">
                      <h4 className="text-base font-semibold text-op-text-primary">
                        {line.title}
                      </h4>
                      <p className="text-sm font-medium text-op-text-muted">
                        {line.materialType}
                      </p>
                    </div>

                    <div className="flex flex-col gap-3.5 text-sm">
                      <div className="flex items-center justify-between">
                        <span className="text-op-text-muted">Quantity</span>
                        <span className="font-medium text-op-text-primary">
                          {line.quantity}
                        </span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-op-text-muted">Line total</span>
                        <span className="font-medium text-op-text-primary">
                          {penceToPounds(line.lineNetPence)} excluding VAT
                        </span>
                      </div>
                    </div>
                  </div>
                ))
              ) : (
                <div className="flex flex-col gap-5 rounded-sm border border-op-border-default/50 bg-op-background-primary p-6">
                  <div className="flex flex-col gap-1">
                    <h4 className="text-base font-semibold text-op-text-primary">
                      {order.materials.split("·")[0]?.trim() || "Materials"}
                    </h4>
                  </div>
                </div>
              )}
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
                  <p className="text-sm leading-relaxed text-op-text-muted">
                    {detail?.shipTo
                      ? formatShipToAddress(detail.shipTo)
                      : order.locationName}
                  </p>
                </div>

                <div className="flex flex-col gap-1">
                  <span className="text-sm font-semibold text-op-text-primary">
                    Delivery method
                  </span>
                  <p className="text-sm text-op-text-muted">
                    {detail?.deliveryMethod === "express"
                      ? "Express delivery"
                      : "Standard delivery"}
                  </p>
                </div>

                <div className="flex flex-col gap-1">
                  <span className="text-sm font-semibold text-op-text-primary">
                    Delivery instructions
                  </span>
                  <p className="text-sm leading-relaxed text-op-text-muted">
                    {detail?.shipTo.deliveryInstructions?.trim() ||
                      "No special instructions provided."}
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
                <div className="flex items-center justify-between">
                  <span className="text-op-text-muted">Materials subtotal:</span>
                  <span className="font-medium text-op-text-primary">
                    {detail
                      ? penceToPounds(detail.materialsNetPence)
                      : order.total}
                  </span>
                </div>

                <div className="flex items-center justify-between">
                  <span className="text-op-text-muted">Delivery:</span>
                  <span className="text-op-text-muted">
                    {detail
                      ? penceToPounds(detail.deliveryNetPence)
                      : "£0.00"}
                  </span>
                </div>

                <div className="flex items-center justify-between">
                  <span className="text-op-text-muted">VAT:</span>
                  <span className="text-op-text-muted">
                    {detail ? penceToPounds(detail.vatPence) : "—"}
                  </span>
                </div>

                <div className="flex items-center justify-between pt-1 font-bold">
                  <span className="text-op-text-primary">Order total:</span>
                  <span className="text-base font-bold text-op-text-primary">
                    {detail ? penceToPounds(detail.grossPence) : order.total}
                  </span>
                </div>
              </div>

              <div className="flex flex-col gap-2 rounded-sm border border-op-border-default/50 bg-op-background-primary p-6">
                <span className="text-base font-semibold text-op-text-primary">
                  Payment method
                </span>
                <span className="text-sm text-op-text-muted">
                  {detail?.paymentSummary.revolutOrderId
                    ? `Revolut reference ${detail.paymentSummary.revolutOrderId}`
                    : "Paid via Revolut checkout"}
                  {detail?.paymentSummary.paidAtUtc
                    ? ` · Paid on ${formatProgressTimestamp(detail.paymentSummary.paidAtUtc)}`
                    : ""}
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

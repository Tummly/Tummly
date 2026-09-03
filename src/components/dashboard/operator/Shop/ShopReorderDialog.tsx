import { useState, useEffect } from "react"
import { X } from "lucide-react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"
import type { DetailedShopOrder } from "@/lib/operatorShop/shopOrdersFilterSheetSchema"

type ShopReorderDialogProps = {
  order: DetailedShopOrder | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirmReorder: (order: DetailedShopOrder) => void
}

export function ShopReorderDialog({
  order,
  open,
  onOpenChange,
  onConfirmReorder,
}: ShopReorderDialogProps) {
  const [acknowledged, setAcknowledged] = useState(false)

  useEffect(() => {
    if (open) {
      setAcknowledged(false)
    }
  }, [open])

  if (!order) return null

  const handleReviewReorder = () => {
    if (!acknowledged) {
      toast.error("Please confirm that you understand the QR code notice")
      return
    }
    onConfirmReorder(order)
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        className="z-[200] w-full max-w-[642px] gap-6 rounded-sm border border-op-border-default bg-op-card-background p-8 text-op-text-primary shadow-2xl sm:max-w-[642px]"
        overlayClassName="z-[190] bg-black/60 backdrop-blur-xs"
      >
        {/* Header Section */}
        <div className="flex items-start justify-between gap-5">
          <DialogHeader className="gap-2 text-left">
            <DialogTitle className="text-2xl font-bold tracking-tight text-op-text-primary">
              Reorder these materials?
            </DialogTitle>
            <DialogDescription className="text-sm font-medium text-op-text-muted leading-relaxed">
              Create a new order using the same location, materials and quantities.
            </DialogDescription>
          </DialogHeader>

          <Button
            type="button"
            variant="op-collapse"
            onClick={() => onOpenChange(false)}
            aria-label="Close dialog"
          >
            <X className="size-4" />
          </Button>
        </div>

        {/* Divider */}
        <div className="border-t border-op-border-default" />

        {/* Order Details List */}
        <div className="flex flex-col gap-4 text-sm">
          <div className="flex items-center justify-between">
            <span className="text-base font-medium text-op-text-primary">
              Original order
            </span>
            <span className="text-sm font-medium text-op-text-muted">
              Order {order.orderNumber}
            </span>
          </div>

          <div className="flex items-center justify-between">
            <span className="text-base font-medium text-op-text-primary">
              Location
            </span>
            <span className="text-sm font-medium text-op-text-muted">
              {order.locationName}
            </span>
          </div>

          <div className="flex items-center justify-between">
            <span className="text-base font-medium text-op-text-primary">
              Materials
            </span>
            <span className="text-sm font-medium text-op-text-muted">
              {order.materials}
            </span>
          </div>
        </div>

        {/* Divider */}
        <div className="border-t border-op-border-default" />

        {/* Notice & Checkbox */}
        <div className="flex flex-col gap-4">
          <p className="text-sm font-medium text-op-text-muted leading-relaxed">
            Tummly will prepare and connect the QR materials automatically. You can review and change the quantities before placing the new order.
          </p>

          <label
            htmlFor="reorder-acknowledgement"
            className="flex cursor-pointer items-start gap-3 select-none"
          >
            <Checkbox
              id="reorder-acknowledgement"
              checked={acknowledged}
              onCheckedChange={(checked) => setAcknowledged(Boolean(checked))}
              className="mt-0.5 border-op-border-default bg-op-surface-secondary data-checked:bg-op-action-primary data-checked:border-op-action-primary data-checked:text-white"
            />
            <span className="text-sm font-medium text-op-text-muted leading-snug">
              I understand that existing printed materials using this QR code will stop working.
            </span>
          </label>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center gap-3 pt-2">
          <Button
            type="button"
            variant="op-primary"
            onClick={handleReviewReorder}
          >
            Review reorder
          </Button>

          <Button
            type="button"
            variant="op-tertiary"
            onClick={() => onOpenChange(false)}
          >
            Cancel
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}

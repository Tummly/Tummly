import { useState, useEffect } from "react"
import { X } from "lucide-react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  FILTER_SELECT_CONTENT_CLASS,
  FILTER_SELECT_ITEM_CLASS,
  FILTER_SELECT_PLACEHOLDER_CLASS,
  FILTER_SELECT_TRIGGER_CLASS,
} from "@/lib/operatorFilterSheet/filterSelectPresentation"
import { cn } from "@/lib/utils"
import type { DetailedShopOrder } from "@/lib/operatorShop/shopOrdersFilterSheetSchema"

export const SHOP_CANCEL_REASONS = [
  { slug: "ordered_by_mistake", label: "Ordered by mistake" },
  { slug: "incorrect_quantity", label: "Incorrect quantity" },
  { slug: "incorrect_location", label: "Incorrect location" },
  { slug: "delivery_details_changed", label: "Delivery details changed" },
  { slug: "no_longer_required", label: "No longer required" },
  { slug: "other", label: "Other" },
] as const

type ShopCancelOrderDialogProps = {
  order: DetailedShopOrder | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirmCancel: (order: DetailedShopOrder, reasonSlug: string) => void
}

export function ShopCancelOrderDialog({
  order,
  open,
  onOpenChange,
  onConfirmCancel,
}: ShopCancelOrderDialogProps) {
  const [reasonSlug, setReasonSlug] = useState<string>("")

  useEffect(() => {
    if (open) {
      setReasonSlug("")
    }
  }, [open])

  if (!order) return null

  const handleConfirm = () => {
    if (!reasonSlug) {
      toast.error("Please select a cancellation reason")
      return
    }
    onConfirmCancel(order, reasonSlug)
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
              Cancel order {order.orderNumber}?
            </DialogTitle>
            <DialogDescription className="text-sm font-medium text-op-text-muted leading-relaxed">
              This order will be cancelled. Our team will process your refund separately.
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

        {/* Reason Select Section */}
        <div className="flex flex-col gap-2.5">
          <label className="text-sm font-semibold text-op-text-primary">
            Why are you cancelling this order?
          </label>
          <Select value={reasonSlug} onValueChange={setReasonSlug}>
            <SelectTrigger
              className={cn(
                FILTER_SELECT_TRIGGER_CLASS,
                !reasonSlug && FILTER_SELECT_PLACEHOLDER_CLASS
              )}
            >
              <SelectValue placeholder="Select" />
            </SelectTrigger>
            <SelectContent
              position="popper"
              align="start"
              className={cn(FILTER_SELECT_CONTENT_CLASS, "z-[250]")}
            >
              {SHOP_CANCEL_REASONS.map((reason) => (
                <SelectItem
                  key={reason.slug}
                  value={reason.slug}
                  className={cn(FILTER_SELECT_ITEM_CLASS, "cursor-pointer")}
                >
                  {reason.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Divider */}
        <div className="border-t border-op-border-default" />

        {/* Action Buttons */}
        <div className="flex items-center gap-3 pt-2">
          <Button
            type="button"
            variant="op-primary"
            onClick={handleConfirm}
          >
            Cancel order
          </Button>

          <Button
            type="button"
            variant="op-tertiary"
            onClick={() => onOpenChange(false)}
          >
            Keep order
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}

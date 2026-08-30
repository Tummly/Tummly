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
import type { DetailedShopOrder } from "@/lib/operatorShop/shopOrdersFilterSheetSchema"

const CANCEL_REASONS = [
  "Ordered by mistake",
  "Incorrect quantity",
  "Incorrect location",
  "Delivery details changed",
  "No longer required",
  "Other",
]

type ShopCancelOrderDialogProps = {
  order: DetailedShopOrder | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirmCancel: (order: DetailedShopOrder, reason: string) => void
}

export function ShopCancelOrderDialog({
  order,
  open,
  onOpenChange,
  onConfirmCancel,
}: ShopCancelOrderDialogProps) {
  const [reason, setReason] = useState<string>("")

  useEffect(() => {
    if (open) {
      setReason("")
    }
  }, [open])

  if (!order) return null

  const handleConfirm = () => {
    if (!reason) {
      toast.error("Please select a cancellation reason")
      return
    }
    onConfirmCancel(order, reason)
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
              This order has not entered production and can still be cancelled. Any completed payment will be reviewed for refund.
            </DialogDescription>
          </DialogHeader>

          <Button
            type="button"
            variant="ghost"
            size="icon"
            onClick={() => onOpenChange(false)}
            className="size-9 shrink-0 rounded-xs bg-op-surface-secondary text-op-text-muted hover:bg-op-surface-secondary/80 hover:text-op-text-primary"
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
          <Select value={reason} onValueChange={setReason}>
            <SelectTrigger className="h-11 w-full rounded-sm border-op-border-default bg-op-background-primary px-3.5 text-sm text-op-text-primary">
              <SelectValue placeholder="Select" />
            </SelectTrigger>
            <SelectContent
              position="popper"
              className="z-[250] border-op-border-default bg-op-card-background text-op-text-primary"
            >
              {CANCEL_REASONS.map((r) => (
                <SelectItem
                  key={r}
                  value={r}
                  className="cursor-pointer text-sm focus:bg-op-surface-secondary focus:text-op-text-primary"
                >
                  {r}
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
            className="h-10 rounded-xs px-5 text-sm font-medium"
            onClick={handleConfirm}
          >
            Cancel order
          </Button>

          <Button
            type="button"
            variant="outline"
            className="h-10 rounded-xs border-op-border-default bg-transparent px-5 text-sm font-medium text-op-text-primary hover:bg-op-surface-secondary"
            onClick={() => onOpenChange(false)}
          >
            Keep order
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}

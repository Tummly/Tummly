import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  SHOP_QR_LIMIT_COPY,
  buildShopQrLimitBody,
} from "@/lib/operatorShop/shopQrLimitPresentation"

type ShopQrLimitDialogProps = {
  open: boolean
  limit: number
  onOpenChange: (open: boolean) => void
  onReviewPlanOptions: () => void
  onManageQrCodes: () => void
}

export function ShopQrLimitDialog({
  open,
  limit,
  onOpenChange,
  onReviewPlanOptions,
  onManageQrCodes,
}: ShopQrLimitDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        className="w-full max-w-[642px] gap-6 rounded-sm border border-op-border-default bg-op-card-background p-8 text-op-text-primary shadow-2xl sm:max-w-[642px]"
      >
        <DialogHeader className="gap-2 text-left">
          <DialogTitle className="text-2xl font-bold tracking-tight text-op-text-primary">
            {SHOP_QR_LIMIT_COPY.title}
          </DialogTitle>
          <DialogDescription className="text-sm font-medium leading-relaxed text-op-text-muted">
            {buildShopQrLimitBody(limit)}
          </DialogDescription>
        </DialogHeader>

        <DialogFooter className="flex-row flex-wrap items-center justify-end gap-3">
          <Button
            type="button"
            variant="op-tertiary"
            onClick={() => onOpenChange(false)}
          >
            {SHOP_QR_LIMIT_COPY.backToOrder}
          </Button>
          <Button
            type="button"
            variant="op-secondary"
            onClick={onManageQrCodes}
          >
            {SHOP_QR_LIMIT_COPY.manageQrCodes}
          </Button>
          <Button
            type="button"
            variant="op-primary"
            onClick={onReviewPlanOptions}
          >
            {SHOP_QR_LIMIT_COPY.reviewPlanOptions}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

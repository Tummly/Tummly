import { Package, Truck, CheckCircle2, Clock } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Badge } from "@/components/ui/badge"

export type ShopOrder = {
  id: string
  orderNumber: string
  date: string
  locationName: string
  items: string[]
  status: "delivered" | "in-transit" | "processing"
  trackingUrl?: string
}

type ShopOrdersDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  orders: ShopOrder[]
}

export function ShopOrdersDialog({
  open,
  onOpenChange,
  orders,
}: ShopOrdersDialogProps) {
  const getStatusBadge = (status: ShopOrder["status"]) => {
    switch (status) {
      case "delivered":
        return (
          <Badge variant="outline" className="border-op-action-primary/40 bg-op-action-primary/10 text-op-text-success text-[11px] font-semibold gap-1">
            <CheckCircle2 className="size-3" />
            Delivered
          </Badge>
        )
      case "in-transit":
        return (
          <Badge variant="outline" className="border-blue-500/40 bg-blue-500/10 text-blue-400 text-[11px] font-semibold gap-1">
            <Truck className="size-3" />
            In Transit
          </Badge>
        )
      case "processing":
      default:
        return (
          <Badge variant="outline" className="border-amber-500/40 bg-amber-500/10 text-amber-400 text-[11px] font-semibold gap-1">
            <Clock className="size-3" />
            Processing
          </Badge>
        )
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="z-50 border-op-border-default bg-op-card-background sm:max-w-xl">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <div className="rounded-md bg-op-surface-secondary p-2 text-foreground">
              <Package className="size-5" />
            </div>
            <div>
              <DialogTitle className="text-lg font-bold text-foreground">
                Shop Order History
              </DialogTitle>
              <DialogDescription className="text-xs text-muted-foreground">
                Track production and shipment of branded QR feedback materials.
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        <div className="flex flex-col gap-3 py-2 max-h-[400px] overflow-y-auto pr-1">
          {orders.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-10 text-center">
              <Package className="size-10 text-muted-foreground/30" />
              <p className="mt-2 text-xs text-muted-foreground">No orders placed yet.</p>
            </div>
          ) : (
            orders.map((order) => (
              <div
                key={order.id}
                className="flex flex-col gap-2.5 rounded-lg border border-op-border-default bg-op-surface-secondary/40 p-4 text-xs"
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="font-bold text-foreground">{order.orderNumber}</span>
                    <span className="text-muted-foreground">• {order.date}</span>
                  </div>
                  {getStatusBadge(order.status)}
                </div>

                <div className="text-muted-foreground">
                  <span className="font-medium text-foreground">Location: </span>
                  {order.locationName}
                </div>

                <div className="flex flex-wrap gap-1 mt-1">
                  {order.items.map((item, i) => (
                    <span
                      key={i}
                      className="rounded bg-op-surface-secondary px-2 py-0.5 text-[11px] text-muted-foreground border border-op-border-default/50"
                    >
                      {item}
                    </span>
                  ))}
                </div>
              </div>
            ))
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}

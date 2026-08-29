import { Trash2, Plus, Minus, ShoppingBag, MapPin, CheckCircle2 } from "lucide-react"
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"
import { Button } from "@/components/ui/button"
import type { ShopProduct } from "@/components/dashboard/operator/Shop/ShopCatalogSection"

export type CartItem = {
  product: ShopProduct
  quantity: number
}

type ShopCartDrawerProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  items: CartItem[]
  onUpdateQuantity: (productId: string, quantity: number) => void
  onRemoveItem: (productId: string) => void
  onClearCart: () => void
  onCheckout: () => void
  selectedLocationName: string
  selectedLocationAddress?: string
  isSubmitting?: boolean
}

export function ShopCartDrawer({
  open,
  onOpenChange,
  items,
  onUpdateQuantity,
  onRemoveItem,
  onCheckout,
  selectedLocationName,
  selectedLocationAddress,
  isSubmitting = false,
}: ShopCartDrawerProps) {
  const subtotal = items.reduce(
    (total, item) => total + item.product.price * item.quantity,
    0
  )
  const totalItems = items.reduce((total, item) => total + item.quantity, 0)

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent
        side="right"
        className="z-50 flex w-full flex-col justify-between border-l border-op-border-default bg-op-card-background p-6 sm:max-w-md"
      >
        <div className="flex flex-col gap-6 overflow-y-auto pr-1">
          <SheetHeader className="text-left">
            <div className="flex items-center gap-2">
              <ShoppingBag className="size-5 text-op-action-primary" />
              <SheetTitle className="text-lg font-bold text-foreground">
                Your Material Cart
              </SheetTitle>
            </div>
            <SheetDescription className="text-xs text-muted-foreground">
              Branded QR feedback materials for {selectedLocationName}
            </SheetDescription>
          </SheetHeader>

          {/* Delivery Location Preview */}
          <div className="flex items-start gap-3 rounded-lg border border-op-border-default bg-op-surface-secondary/70 p-3 text-xs">
            <MapPin className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
            <div className="flex flex-col">
              <span className="font-semibold text-foreground">Delivery Destination:</span>
              <span className="text-muted-foreground">{selectedLocationName}</span>
              {selectedLocationAddress && (
                <span className="text-muted-foreground">{selectedLocationAddress}</span>
              )}
            </div>
          </div>

          {items.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <ShoppingBag className="size-12 text-muted-foreground/40" />
              <p className="mt-3 text-sm font-medium text-foreground">Your cart is empty</p>
              <p className="mt-1 text-xs text-muted-foreground">
                Add starter kits or custom QR materials from the shop catalog.
              </p>
            </div>
          ) : (
            <div className="flex flex-col gap-3">
              {items.map((item) => (
                <div
                  key={item.product.id}
                  className="flex items-center justify-between gap-3 rounded-lg border border-op-border-default bg-op-surface-secondary/50 p-3"
                >
                  <div className="flex flex-col min-w-0">
                    <span className="truncate text-xs font-semibold text-foreground">
                      {item.product.title}
                    </span>
                    <span className="text-[11px] text-muted-foreground">
                      {item.product.price === 0
                        ? "Plan Included (Free)"
                        : `£${item.product.price.toFixed(2)} each`}
                    </span>
                  </div>

                  <div className="flex items-center gap-2 shrink-0">
                    <div className="flex items-center rounded-md border border-op-border-default bg-op-card-background">
                      <button
                        type="button"
                        onClick={() =>
                          item.quantity > 1
                            ? onUpdateQuantity(item.product.id, item.quantity - 1)
                            : onRemoveItem(item.product.id)
                        }
                        className="flex size-7 items-center justify-center text-muted-foreground hover:text-foreground"
                      >
                        <Minus className="size-3" />
                      </button>
                      <span className="w-6 text-center text-xs font-medium text-foreground">
                        {item.quantity}
                      </span>
                      <button
                        type="button"
                        onClick={() =>
                          onUpdateQuantity(item.product.id, item.quantity + 1)
                        }
                        className="flex size-7 items-center justify-center text-muted-foreground hover:text-foreground"
                      >
                        <Plus className="size-3" />
                      </button>
                    </div>

                    <button
                      type="button"
                      onClick={() => onRemoveItem(item.product.id)}
                      className="text-muted-foreground hover:text-destructive p-1"
                    >
                      <Trash2 className="size-3.5" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {items.length > 0 && (
          <div className="flex flex-col gap-4 border-t border-op-border-default pt-4">
            <div className="flex flex-col gap-1.5 text-xs">
              <div className="flex justify-between text-muted-foreground">
                <span>Items total ({totalItems})</span>
                <span>£{subtotal.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-muted-foreground">
                <span>Custom QR print & branding</span>
                <span className="text-op-text-success font-medium">Free</span>
              </div>
              <div className="flex justify-between text-muted-foreground">
                <span>Standard Delivery (2-3 business days)</span>
                <span className="text-op-text-success font-medium">Free</span>
              </div>
              <div className="mt-2 flex justify-between border-t border-op-border-default/60 pt-2 text-sm font-bold text-foreground">
                <span>Total to pay</span>
                <span>£{subtotal.toFixed(2)}</span>
              </div>
            </div>

            <Button
              type="button"
              variant="op-primary"
              disabled={isSubmitting}
              className="h-11 w-full rounded-md text-sm font-medium"
              onClick={onCheckout}
            >
              {isSubmitting ? (
                "Processing order..."
              ) : (
                <>
                  <CheckCircle2 className="mr-2 size-4" />
                  Confirm & Place Order
                </>
              )}
            </Button>
          </div>
        )}
      </SheetContent>
    </Sheet>
  )
}

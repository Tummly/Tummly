import { Check, Package } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"

/** Named paid materials-pack SKU quantities (not lifetime Starter kit). */
export const SHOP_MATERIALS_PACK_LINES = [
  { skuId: "table-tents", quantity: 10 },
  { skuId: "window-stickers", quantity: 4 },
  { skuId: "counter-cards", quantity: 50 },
] as const

type ShopMaterialsPackDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onAddPackToCart: () => void
  selectedLocationName: string
  purchaseDisabled?: boolean
}

export function ShopMaterialsPackDialog({
  open,
  onOpenChange,
  onAddPackToCart,
  selectedLocationName,
  purchaseDisabled = false,
}: ShopMaterialsPackDialogProps) {
  const kitItems = [
    {
      title: "10× Table tents",
      desc: "Double-sided matte acrylic tents with your custom location QR code.",
    },
    {
      title: "4× Window stickers",
      desc: "UV-resistant vinyl clings for entrances and front windows.",
    },
    {
      title: "50× Counter cards",
      desc: "Compact cards for ordering, payment, or collection counters.",
    },
  ]

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="z-50 border-op-border-default bg-op-card-background sm:max-w-lg">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <div className="rounded-md bg-op-action-primary/10 p-2 text-op-action-primary">
              <Package className="size-5" />
            </div>
            <div>
              <DialogTitle className="text-lg font-bold text-foreground">
                Materials pack overview
              </DialogTitle>
              <DialogDescription className="text-xs text-muted-foreground">
                Named materials pack for {selectedLocationName}
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        <div className="flex flex-col gap-4 py-2">
          <p className="text-xs text-muted-foreground">
            Review the named materials pack, then add it to your cart. Shop
            reorder is always paid at checkout.
          </p>

          <div className="flex flex-col gap-2.5">
            <span className="text-xs font-semibold text-foreground">
              What&apos;s inside this pack:
            </span>
            {kitItems.map((item) => (
              <div
                key={item.title}
                className="flex items-start gap-3 rounded-lg border border-op-border-default bg-op-surface-secondary/40 p-3 text-xs"
              >
                <div className="mt-0.5 rounded-full bg-op-action-primary/15 p-1 text-op-action-primary">
                  <Check className="size-3" />
                </div>
                <div className="flex flex-col">
                  <span className="font-semibold text-foreground">
                    {item.title}
                  </span>
                  <span className="text-muted-foreground">{item.desc}</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        <DialogFooter className="gap-2 sm:gap-0">
          <Button
            type="button"
            variant="op-tertiary"
            onClick={() => onOpenChange(false)}
          >
            Close
          </Button>
          <Button
            type="button"
            variant="op-primary"
            disabled={purchaseDisabled}
            onClick={() => {
              onAddPackToCart()
              onOpenChange(false)
            }}
          >
            Add materials pack to cart
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

/** @deprecated Use ShopMaterialsPackDialog */
export const ShopStarterKitDialog = ShopMaterialsPackDialog

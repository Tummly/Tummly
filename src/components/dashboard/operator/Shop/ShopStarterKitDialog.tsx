import { Check, Package, Sparkles } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"

type ShopStarterKitDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onAddKitToCart: () => void
  selectedLocationName: string
}

export function ShopStarterKitDialog({
  open,
  onOpenChange,
  onAddKitToCart,
  selectedLocationName,
}: ShopStarterKitDialogProps) {
  const kitItems = [
    {
      title: "10x Tabletop Acrylic Stands",
      desc: "Double-sided matte acrylic tents with your custom location QR code and brand colors.",
    },
    {
      title: "4x Window & Entrance Cling Decals",
      desc: "UV-resistant static vinyl decals encouraging guests to share thoughts upon leaving.",
    },
    {
      title: "50x Bill Presenter Mini Cards",
      desc: "Heavyweight tactile cards designed to tuck inside guest payment folders.",
    },
    {
      title: "1x Quick-Start Placement Guide",
      desc: "Best practice guide on where and how to display materials for highest guest engagement.",
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
                QR Starter Kit Overview
              </DialogTitle>
              <DialogDescription className="text-xs text-muted-foreground">
                Essential feedback collection pack for {selectedLocationName}
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        <div className="flex flex-col gap-4 py-2">
          <div className="rounded-lg border border-op-action-primary/30 bg-op-action-primary/5 p-3.5">
            <div className="flex items-center gap-2 text-xs font-semibold text-op-text-success">
              <Sparkles className="size-4" />
              <span>Complimentary with your Tummly plan</span>
            </div>
            <p className="mt-1 text-xs text-muted-foreground">
              Every location receives one free complete QR starter pack with custom high-resolution branding.
            </p>
          </div>

          <div className="flex flex-col gap-2.5">
            <span className="text-xs font-semibold text-foreground">What&apos;s inside this kit:</span>
            {kitItems.map((item, index) => (
              <div
                key={index}
                className="flex items-start gap-3 rounded-lg border border-op-border-default bg-op-surface-secondary/40 p-3 text-xs"
              >
                <div className="mt-0.5 rounded-full bg-op-action-primary/15 p-1 text-op-action-primary">
                  <Check className="size-3" />
                </div>
                <div className="flex flex-col">
                  <span className="font-semibold text-foreground">{item.title}</span>
                  <span className="text-muted-foreground">{item.desc}</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        <DialogFooter className="gap-2 sm:gap-0">
          <Button
            type="button"
            variant="outline"
            className="border-op-border-default text-xs"
            onClick={() => onOpenChange(false)}
          >
            Close
          </Button>
          <Button
            type="button"
            variant="op-primary"
            className="text-xs font-medium"
            onClick={() => {
              onAddKitToCart()
              onOpenChange(false)
            }}
          >
            Order Free Starter Kit
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

import { useState } from "react"
import { QrCode, Download, Printer } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { toast } from "sonner"

type ShopCreateQrAssetDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  locationName: string
}

export function ShopCreateQrAssetDialog({
  open,
  onOpenChange,
  locationName,
}: ShopCreateQrAssetDialogProps) {
  const [headline, setHeadline] = useState<string>("How was your visit?")
  const [callToAction, setCallToAction] = useState<string>("Scan to share private feedback & unlock offers")
  const [tableNumber, setTableNumber] = useState<string>("")

  const handleDownload = (format: "pdf" | "png" | "svg") => {
    toast.success(`Generated ${format.toUpperCase()} asset for ${locationName}`)
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="z-50 border-op-border-default bg-op-card-background sm:max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <div className="rounded-md bg-op-action-primary/10 p-2 text-op-action-primary">
              <QrCode className="size-5" />
            </div>
            <div>
              <DialogTitle className="text-lg font-bold text-foreground">
                Create Printable QR Asset
              </DialogTitle>
              <DialogDescription className="text-xs text-muted-foreground">
                Generate high-resolution printable QR sheets formatted for {locationName}.
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        <div className="flex flex-col gap-4 py-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="headline" className="text-xs font-semibold text-foreground">
              Headline Prompt
            </Label>
            <Input
              id="headline"
              value={headline}
              onChange={(e) => setHeadline(e.target.value)}
              className="h-9 border-op-border-default bg-op-surface-secondary text-sm"
              placeholder="e.g. How was your visit?"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="cta" className="text-xs font-semibold text-foreground">
              Secondary Description
            </Label>
            <Input
              id="cta"
              value={callToAction}
              onChange={(e) => setCallToAction(e.target.value)}
              className="h-9 border-op-border-default bg-op-surface-secondary text-sm"
              placeholder="e.g. Scan to share feedback"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="tableNumber" className="text-xs font-semibold text-foreground">
              Table / Zone Identifier (Optional)
            </Label>
            <Input
              id="tableNumber"
              value={tableNumber}
              onChange={(e) => setTableNumber(e.target.value)}
              className="h-9 border-op-border-default bg-op-surface-secondary text-sm"
              placeholder="e.g. Table 12 or Patio"
            />
          </div>

          {/* Quick Mockup Preview */}
          <div className="flex flex-col items-center justify-center rounded-lg border border-op-border-default bg-op-surface-secondary/60 p-4 text-center">
            <span className="text-xs font-bold text-foreground">{headline || "How was your visit?"}</span>
            <div className="my-2 flex size-20 items-center justify-center rounded-md bg-white p-2 shadow-inner">
              <QrCode className="size-16 text-neutral-900" />
            </div>
            <span className="text-[11px] text-muted-foreground">{callToAction}</span>
            {tableNumber && (
              <span className="mt-1 rounded bg-op-card-background px-2 py-0.5 text-[10px] font-semibold text-op-action-primary">
                {tableNumber}
              </span>
            )}
          </div>
        </div>

        <DialogFooter className="flex-col sm:flex-row gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="border-op-border-default text-xs"
            onClick={() => handleDownload("png")}
          >
            <Download className="mr-1.5 size-3.5" />
            PNG
          </Button>
          <Button
            type="button"
            variant="op-primary"
            size="sm"
            className="text-xs font-medium"
            onClick={() => handleDownload("pdf")}
          >
            <Printer className="mr-1.5 size-3.5" />
            Download Print-Ready PDF
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

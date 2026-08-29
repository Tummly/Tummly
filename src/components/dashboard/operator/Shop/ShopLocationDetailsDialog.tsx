import { useState } from "react"
import { Building2, Utensils, Users } from "lucide-react"
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
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"

type LocationDetails = {
  tableCount: number
  serviceType: string
  seatingArea: string
}

type ShopLocationDetailsDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSaveDetails: (details: LocationDetails) => void
  initialDetails?: LocationDetails | null
  locationName: string
}

export function ShopLocationDetailsDialog({
  open,
  onOpenChange,
  onSaveDetails,
  initialDetails,
  locationName,
}: ShopLocationDetailsDialogProps) {
  const [tableCount, setTableCount] = useState<string>(
    initialDetails?.tableCount ? String(initialDetails.tableCount) : "20"
  )
  const [serviceType, setServiceType] = useState<string>(
    initialDetails?.serviceType ?? "table-service"
  )
  const [seatingArea, setSeatingArea] = useState<string>(
    initialDetails?.seatingArea ?? "indoor-outdoor"
  )

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    onSaveDetails({
      tableCount: parseInt(tableCount, 10) || 10,
      serviceType:
        serviceType === "table-service"
          ? "Full Table Service"
          : serviceType === "counter"
            ? "Counter / Quick Service"
            : "Bar & Lounge",
      seatingArea:
        seatingArea === "indoor-outdoor"
          ? "Indoor & Outdoor"
          : seatingArea === "indoor-only"
            ? "Indoor Only"
            : "Outdoor Patio",
    })
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="z-50 border-op-border-default bg-op-card-background sm:max-w-md">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle className="text-lg font-bold text-foreground">
              Location Details & Recommendations
            </DialogTitle>
            <DialogDescription className="text-xs text-muted-foreground">
              Configure operational parameters for {locationName} to tailor your QR material amounts.
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-4 py-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="tableCount" className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                <Users className="size-3.5 text-muted-foreground" />
                Estimated number of tables / seating stations
              </Label>
              <Input
                id="tableCount"
                type="number"
                min="1"
                max="500"
                value={tableCount}
                onChange={(e) => setTableCount(e.target.value)}
                className="h-9 border-op-border-default bg-op-surface-secondary text-sm"
                required
              />
              <span className="text-[11px] text-muted-foreground">
                We recommend 1 table stand per table plus 10% backup spares.
              </span>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="serviceType" className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                <Utensils className="size-3.5 text-muted-foreground" />
                Service Style
              </Label>
              <Select value={serviceType} onValueChange={setServiceType}>
                <SelectTrigger id="serviceType" className="h-9 border-op-border-default bg-op-surface-secondary text-xs">
                  <SelectValue placeholder="Select service type" />
                </SelectTrigger>
                <SelectContent className="bg-op-card-background border-op-border-default z-50">
                  <SelectItem value="table-service">Full Table Service (Bill Presenters + Table Stands)</SelectItem>
                  <SelectItem value="counter">Counter Order / Fast Casual (Decals + Counter Stands)</SelectItem>
                  <SelectItem value="bar">Bar & Lounge (Drink Coasters + Table Stands)</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="seatingArea" className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                <Building2 className="size-3.5 text-muted-foreground" />
                Seating Layout
              </Label>
              <Select value={seatingArea} onValueChange={setSeatingArea}>
                <SelectTrigger id="seatingArea" className="h-9 border-op-border-default bg-op-surface-secondary text-xs">
                  <SelectValue placeholder="Select layout" />
                </SelectTrigger>
                <SelectContent className="bg-op-card-background border-op-border-default z-50">
                  <SelectItem value="indoor-outdoor">Indoor & Outdoor Patio</SelectItem>
                  <SelectItem value="indoor-only">Indoor Only</SelectItem>
                  <SelectItem value="outdoor-only">Outdoor Only</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <DialogFooter className="gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              className="border-op-border-default text-xs"
              onClick={() => onOpenChange(false)}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              variant="op-primary"
              className="text-xs font-medium"
            >
              Calculate Suggested Kit
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

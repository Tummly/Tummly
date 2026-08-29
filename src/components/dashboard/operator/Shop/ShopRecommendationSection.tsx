import { CheckCircle2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { LocationDetails } from "@/components/dashboard/operator/Shop/ShopLocationDetailsDialog"

type ShopRecommendationSectionProps = {
  locationName: string
  locationDetails: LocationDetails | null
  onAddLocationDetails: () => void
  onAddRecommendedToCart: () => void
}

export function ShopRecommendationSection({
  locationName,
  locationDetails,
  onAddLocationDetails,
  onAddRecommendedToCart,
}: ShopRecommendationSectionProps) {
  const hasDetails = locationDetails && locationDetails.tableCount

  return (
    <div className="flex flex-col rounded-md border border-op-border-default bg-op-card-background p-6">
      <div className="flex items-start gap-2.5">
        <div className="flex flex-col gap-0.5">
          <h3 className="text-sm font-semibold text-foreground">
            Recommended for {locationName}
          </h3>
          <p className="text-xs font-normal text-muted-foreground">
            Based on how this location operates and its recent QR activity, Tummly has suggested the materials and quantities most likely to give guests the right opportunities to respond.
          </p>
        </div>
      </div>

      <div className="mt-8 flex min-h-[140px] flex-col items-center justify-center p-6 text-center">
        {!hasDetails ? (
          <div className="flex max-w-md flex-col items-center gap-1.5">
            <h4 className="text-xs font-medium text-foreground">
              Get a recommendation for this location
            </h4>
            <p className="text-xs text-muted-foreground">
              Tell us how this location operates and Tummly will suggest suitable QR materials and quantities.
            </p>
            <Button
              type="button"
              variant="outline"
              className="mt-3 h-8 rounded-md border-op-border-default bg-transparent px-3 text-xs font-medium text-foreground hover:bg-op-surface-secondary"
              onClick={onAddLocationDetails}
            >
              Add location details
            </Button>
          </div>
        ) : (
          <div className="flex w-full max-w-lg flex-col items-center gap-4">
            <div className="flex items-center gap-2 text-xs font-medium text-op-text-success">
              <CheckCircle2 className="size-3.5" />
              <span>
                Location setup active ({locationDetails.tableCount} tables
                {locationDetails.counterCount ? `, ${locationDetails.counterCount} counters` : ""}
                {locationDetails.entranceCount ? `, ${locationDetails.entranceCount} entrances` : ""})
              </span>
            </div>
            <div className="grid w-full grid-cols-3 gap-3 text-left">
              <div className="rounded-md border border-op-border-default bg-op-surface-secondary p-3">
                <span className="text-xs text-muted-foreground">Table Stands</span>
                <p className="text-sm font-bold text-foreground">{locationDetails.tableCount} units</p>
              </div>
              <div className="rounded-md border border-op-border-default bg-op-surface-secondary p-3">
                <span className="text-xs text-muted-foreground">Window Decals</span>
                <p className="text-sm font-bold text-foreground">
                  {locationDetails.entranceCount ? locationDetails.entranceCount * 2 : 4} units
                </p>
              </div>
              <div className="rounded-md border border-op-border-default bg-op-surface-secondary p-3">
                <span className="text-xs text-muted-foreground">Bill Presenters</span>
                <p className="text-sm font-bold text-foreground">
                  {Math.ceil(locationDetails.tableCount * 1.5) +
                    (locationDetails.counterCount ? locationDetails.counterCount * 2 : 0)}{" "}
                  units
                </p>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <Button
                type="button"
                variant="op-primary"
                size="sm"
                className="h-8 rounded-md px-3 text-xs"
                onClick={onAddRecommendedToCart}
              >
                Add recommended kit to cart
              </Button>
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="h-8 rounded-md border-op-border-default px-3 text-xs text-muted-foreground hover:text-foreground"
                onClick={onAddLocationDetails}
              >
                Edit details
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

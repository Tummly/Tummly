import { useState } from "react"
import { SlidersHorizontal } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { LocationDetails } from "@/components/dashboard/operator/Shop/ShopLocationDetailsDialog"
import { ShopRecommendationCard } from "@/components/dashboard/operator/Shop/ShopRecommendationCard"
import type { ShopProduct } from "@/lib/operatorShop/shopCatalogTypes"
import tummlyStickerImg from "@/assets/images/shop/tummly-sticker.png"

function findCatalogProduct(
  catalogProducts: ShopProduct[],
  skuId: string
): ShopProduct | undefined {
  return catalogProducts.find((product) => product.id === skuId)
}

type ShopRecommendationSectionProps = {
  locationName: string
  locationDetails: LocationDetails | null
  catalogProducts: ShopProduct[]
  onAddLocationDetails: () => void
  onAddRecommendedToCart: () => void
  onSelectProduct?: (product: ShopProduct) => void
}

export function ShopRecommendationSection({
  locationName,
  locationDetails,
  catalogProducts,
  onAddLocationDetails,
  onAddRecommendedToCart,
  onSelectProduct,
}: ShopRecommendationSectionProps) {
  const [hasDismissed, setHasDismissed] = useState(false)

  // Default demo state if none entered yet
  const details = locationDetails || {
    tableCount: 18,
    counterCount: 2,
    entranceCount: 2,
    takeawayVolume: "100-249",
    promptLocations: "tables,counters,bills,windows",
    existingMaterials: "yes",
    serviceType: "Full Table Service",
  }

  const hasDetails = locationDetails !== null || !hasDismissed

  return (
    <div className="flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6">
      <div className="flex items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <div className="flex items-center gap-2">
            <h3 className="text-base font-semibold text-foreground sm:text-lg">
              Recommended for {locationName}
            </h3>
          </div>
          <p className="text-xs font-normal text-muted-foreground sm:text-sm">
            Based on how this location operates and its recent QR activity, Tummly has suggested the materials and quantities most likely to give guests the right opportunities to respond.
          </p>
        </div>
      </div>

      {!hasDetails ? (
        <div className="flex min-h-[140px] flex-col items-center justify-center p-6 text-center">
          <h4 className="text-xs font-medium text-foreground">
            Get a recommendation for this location
          </h4>
          <p className="mt-1 text-xs text-muted-foreground">
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
        <div className="flex flex-col gap-6">
          {/* Based-on tag pills row */}
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex flex-wrap items-center gap-2">
              <span className="text-xs font-medium text-muted-foreground">
                Based on
              </span>
              <span className="rounded-md border border-op-border-default bg-op-surface-secondary px-2.5 py-1 text-[11px] font-medium text-foreground">
                {details.tableCount || 18} guest tables
              </span>
              <span className="rounded-md border border-op-border-default bg-op-surface-secondary px-2.5 py-1 text-[11px] font-medium text-foreground">
                {details.counterCount || 1} table service
              </span>
              <span className="rounded-md border border-op-border-default bg-op-surface-secondary px-2.5 py-1 text-[11px] font-medium text-foreground">
                {details.entranceCount || 2} entrances
              </span>
              <span className="rounded-md border border-op-border-default bg-op-surface-secondary px-2.5 py-1 text-[11px] font-medium text-foreground">
                All touchpoints (Tables, Counters, Bills, Windows)
              </span>
              <span className="rounded-md border border-op-border-default bg-op-surface-secondary px-2.5 py-1 text-[11px] font-medium text-foreground">
                Yes, materials are already in use
              </span>
            </div>

            <Button
              type="button"
              variant="outline"
              size="sm"
              className="h-8 shrink-0 gap-1.5 rounded-md border-op-border-default bg-transparent px-3 text-xs text-foreground hover:bg-op-surface-secondary"
              onClick={onAddLocationDetails}
            >
              <SlidersHorizontal className="size-3 text-muted-foreground" />
              Edit location details
            </Button>
          </div>

          {/* Horizontal scroll cards */}
          <div className="flex gap-4 overflow-x-auto pb-2 [scrollbar-width:none] [-ms-overflow-style:none] [&::-webkit-scrollbar]:hidden">
            <ShopRecommendationCard
              title="Table tents"
              description="Place one on each guest table to collect feedback after dine-in visits."
              allocationText={`${details.tableCount || 18} for guest tables + 2 spare`}
              price={69.0}
              initialQuantity={20}
              imageSrc={tummlyStickerImg}
              onOrderNow={onAddRecommendedToCart}
              onSelectCard={() => {
                const prod = findCatalogProduct(catalogProducts, "table-tents")
                if (prod) {
                  onSelectProduct?.(prod)
                }
              }}
            />
            <ShopRecommendationCard
              title="Counter cards"
              description="Place one on each guest table to collect feedback after dine-in visits."
              allocationText={`${details.tableCount || 18} for guest tables + 2 spare`}
              price={68.0}
              initialQuantity={20}
              imageSrc={tummlyStickerImg}
              onOrderNow={onAddRecommendedToCart}
              onSelectCard={() => {
                const prod = findCatalogProduct(catalogProducts, "counter-cards")
                if (prod) {
                  onSelectProduct?.(prod)
                }
              }}
            />
            <ShopRecommendationCard
              title="Window stickers"
              description="Place one on each guest table to collect feedback after dine-in visits."
              allocationText={`${details.tableCount || 18} for guest tables + 2 spare`}
              price={68.0}
              initialQuantity={20}
              imageSrc={tummlyStickerImg}
              onOrderNow={onAddRecommendedToCart}
              onSelectCard={() => {
                const prod = findCatalogProduct(catalogProducts, "window-stickers")
                if (prod) {
                  onSelectProduct?.(prod)
                }
              }}
            />
          </div>

          {/* Recommendation summary footer */}
          <div className="flex flex-col gap-4 border-t border-op-border-default/60 pt-6">
            <div className="flex flex-col gap-1">
              <h4 className="text-base font-semibold text-foreground sm:text-lg">
                Recommendation summary
              </h4>
              <span className="text-xs text-muted-foreground sm:text-sm">
                4 materials types · 214 total pieces
              </span>
            </div>

            <div className="flex w-full max-w-md flex-col gap-2 rounded-md border border-op-border-default/60 bg-op-card-background p-4 text-xs sm:text-sm">
              <div className="flex items-center justify-between text-muted-foreground">
                <span>Materials sub-total:</span>
                <span className="font-semibold text-foreground">£145.00</span>
              </div>
              <div className="flex items-center justify-between text-muted-foreground">
                <span>Starter-kit allowance:</span>
                <span className="font-semibold text-op-text-success">-£69.00</span>
              </div>
              <div className="flex items-center justify-between border-t border-op-border-default/60 pt-2.5 font-medium text-foreground">
                <span className="font-semibold">Estimated total:</span>
                <div className="flex items-baseline gap-1.5">
                  <span className="text-base font-bold text-foreground sm:text-lg">
                    £76.00
                  </span>
                  <span className="text-[11px] font-normal text-muted-foreground sm:text-xs">
                    excluding VAT and delivery
                  </span>
                </div>
              </div>
            </div>

            <div className="flex items-center gap-3 pt-1">
              <Button
                type="button"
                variant="op-primary"
                className="h-10 rounded-md px-6 text-xs font-semibold sm:text-sm"
                onClick={onAddRecommendedToCart}
              >
                Add recommended items
              </Button>
              <Button
                type="button"
                variant="op-secondary"
                className="h-10 rounded-md px-6 text-xs font-medium sm:text-sm"
                onClick={onAddLocationDetails}
              >
                Review recommendation
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}


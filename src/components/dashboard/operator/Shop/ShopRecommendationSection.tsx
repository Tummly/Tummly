import { SlidersHorizontal } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { ShopRecommendationCard } from "@/components/dashboard/operator/Shop/ShopRecommendationCard"
import {
  SHOP_PROMPT_OPTIONS,
} from "@/components/dashboard/operator/Shop/ShopLocationDetailsDialog"
import { findShopProductById, type ShopProduct } from "@/lib/operatorShop/shopCatalogTypes"
import type { ShopLocationRecommendations } from "@/api/shopRecommendationsApi"
import { formatShopPence } from "@/lib/operatorShop/mapShopRecommendations"
import type { ShopPaidWriteChrome } from "@/lib/operatorShop/shopPaidWriteChrome"

type ShopRecommendationSectionProps = {
  locationName: string
  recommendations: ShopLocationRecommendations | null
  recommendationsLoading: boolean
  catalogProducts: ShopProduct[]
  paidWriteChrome: ShopPaidWriteChrome
  onAddLocationDetails: () => void
  onAddRecommendedToCart: () => void
  onOrderRecommendedLine: (skuId: string, quantity: number) => void
  onSelectProduct?: (product: ShopProduct) => void
}

function formatPromptLabel(id: string): string {
  return (
    SHOP_PROMPT_OPTIONS.find((option) => option.id === id)?.label ?? id
  )
}

function formatExistingMaterials(value: string): string {
  if (value === "yes") {
    return "Yes, materials are already in use"
  }
  if (value === "not-sure") {
    return "Not sure about existing materials"
  }
  return "No existing materials yet"
}

const basedOnChipClassName =
  "rounded-md border border-op-border-default bg-op-surface-secondary px-2.5 py-1 text-[11px] font-medium text-foreground"

export function ShopRecommendationSection({
  locationName,
  recommendations,
  recommendationsLoading,
  catalogProducts,
  paidWriteChrome,
  onAddLocationDetails,
  onAddRecommendedToCart,
  onOrderRecommendedLine,
  onSelectProduct,
}: ShopRecommendationSectionProps) {
  const purchaseBlocked = paidWriteChrome.purchaseDisabled
  const needsDetails = recommendations?.needsLocationDetails ?? true
  const basedOn = recommendations?.basedOn
  const lines = recommendations?.lines ?? []
  const summary = recommendations?.summary

  if (recommendationsLoading) {
    return (
      <div className="flex flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6">
        <p className="text-sm text-muted-foreground">Loading recommendations…</p>
      </div>
    )
  }

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

      {needsDetails ? (
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
          {basedOn && (
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex flex-wrap items-center gap-2">
                <span className="text-xs font-medium text-muted-foreground">
                  Based on
                </span>
                <Badge variant="outline" className={basedOnChipClassName}>
                  {basedOn.tableCount} guest tables
                </Badge>
                <Badge variant="outline" className={basedOnChipClassName}>
                  {basedOn.counterCount} service counters
                </Badge>
                <Badge variant="outline" className={basedOnChipClassName}>
                  {basedOn.entranceCount + basedOn.secondaryEntranceCount} entrances
                </Badge>
                {basedOn.promptLocations.length > 0 && (
                  <Badge variant="outline" className={basedOnChipClassName}>
                    {basedOn.promptLocations.map(formatPromptLabel).join(", ")}
                  </Badge>
                )}
                <Badge variant="outline" className={basedOnChipClassName}>
                  {formatExistingMaterials(basedOn.existingMaterials)}
                </Badge>
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
          )}

          <div className="flex gap-4 overflow-x-auto pb-2 [scrollbar-width:none] [-ms-overflow-style:none] [&::-webkit-scrollbar]:hidden">
            {lines.map((line) => (
              <ShopRecommendationCard
                key={line.skuId}
                title={line.title}
                description={line.reason}
                allocationText={line.allocationText}
                unitPriceGbp={line.unitPriceGbp}
                initialQuantity={line.quantity}
                imageSrc={line.imageSrc}
                purchaseDisabled={purchaseBlocked}
                onOrderNow={(quantity) => {
                  onOrderRecommendedLine(line.skuId, quantity)
                }}
                onSelectCard={() => {
                  const prod = findShopProductById(catalogProducts, line.skuId)
                  if (prod) {
                    onSelectProduct?.(prod)
                  }
                }}
              />
            ))}
          </div>

          {summary && lines.length > 0 && (
            <div className="flex flex-col gap-4 border-t border-op-border-default/60 pt-6">
              <div className="flex flex-col gap-1">
                <h4 className="text-base font-semibold text-foreground sm:text-lg">
                  Recommendation summary
                </h4>
                <span className="text-xs text-muted-foreground sm:text-sm">
                  {summary.materialTypeCount} materials types · {summary.totalPieces} total pieces
                </span>
              </div>

              <div className="flex w-full max-w-md flex-col gap-2 rounded-md border border-op-border-default/60 bg-op-card-background p-4 text-xs sm:text-sm">
                <div className="flex items-center justify-between text-muted-foreground">
                  <span>Materials sub-total:</span>
                  <span className="font-semibold text-foreground">
                    {formatShopPence(summary.materialsNetPence)}
                  </span>
                </div>
                <div className="flex items-center justify-between border-t border-op-border-default/60 pt-2.5 font-medium text-foreground">
                  <span className="font-semibold">Estimated total:</span>
                  <div className="flex items-baseline gap-1.5">
                    <span className="text-base font-bold text-foreground sm:text-lg">
                      {formatShopPence(summary.materialsNetPence)}
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
                  disabled={purchaseBlocked}
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
          )}
        </div>
      )}
    </div>
  )
}

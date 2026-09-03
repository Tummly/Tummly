import { Button } from "@/components/ui/button"
import { AiIcon } from "@/components/ui/ai-icon"
import { ShopRecommendationCard } from "@/components/dashboard/operator/Shop/ShopRecommendationCard"
import {
  EXISTING_MATERIALS_OPTIONS,
  SHOP_PROMPT_OPTIONS,
} from "@/components/dashboard/operator/Shop/ShopLocationDetailsDialog"
import { findShopProductById, type ShopProduct } from "@/lib/operatorShop/shopCatalogTypes"
import type { ShopLocationRecommendations } from "@/api/shopRecommendationsApi"
import { formatShopPence } from "@/lib/operatorShop/mapShopRecommendations"
import type { ShopPaidWriteChrome } from "@/lib/operatorShop/shopPaidWriteChrome"
import {
  SHOP_PRODUCT_SPEC_DIVIDER_CLASS,
  SHOP_RECOMMENDATION_BASED_ON_CLASS,
  SHOP_RECOMMENDATION_HEADER_CLASS,
  SHOP_RECOMMENDATION_INNER_CLASS,
  SHOP_RECOMMENDATION_PARENT_CLASS,
  SHOP_RECOMMENDATION_TAG_CLASS,
} from "@/lib/operatorShop/shopSurfacePresentation"

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

const TAKEAWAY_VOLUME_LABELS: Record<string, string> = {
  "fewer-than-100": "Fewer than 100 takeaway orders per week",
  "100-249": "Approximately 100–249 takeaway orders per week",
  "250-499": "Approximately 250 takeaway orders per week",
  "500-999": "Approximately 500–999 takeaway orders per week",
  "1000-plus": "1,000 or more takeaway orders per week",
  "not-sure": "Takeaway volume not sure",
}

function formatPromptLabel(id: string): string {
  return (
    SHOP_PROMPT_OPTIONS.find((option) => option.id === id)?.label ?? id
  )
}

function formatExistingMaterials(value: string): string {
  return (
    EXISTING_MATERIALS_OPTIONS.find((option) => option.value === value)
      ?.label ?? value
  )
}

function formatTakeawayVolume(value: string): string {
  return TAKEAWAY_VOLUME_LABELS[value] ?? value
}

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
      <div className={SHOP_RECOMMENDATION_PARENT_CLASS}>
        <div className={SHOP_RECOMMENDATION_HEADER_CLASS}>
          <p className="text-sm text-[var(--op-color-gray-550)]">
            Loading recommendations…
          </p>
        </div>
      </div>
    )
  }

  return (
    <div className={SHOP_RECOMMENDATION_PARENT_CLASS}>
      <div className={SHOP_RECOMMENDATION_HEADER_CLASS}>
        <div className="flex items-start gap-3">
          <AiIcon size={22} className="mt-0.5" />
          <div className="flex flex-col gap-2">
            <h3 className="text-xl font-semibold leading-normal text-op-text-primary">
              Recommended for {locationName}
            </h3>
            <p className="max-w-[610px] text-sm font-medium leading-[19px] text-[var(--op-color-gray-550)]">
              Based on how this location operates and its recent QR activity,
              Tummly has suggested the materials and quantities most likely to
              give guests the right opportunities to respond.
            </p>
          </div>
        </div>
      </div>

      {needsDetails ? (
        <div className="mx-6 flex min-h-[140px] flex-col items-center justify-center rounded-[4px] bg-op-color-gray-60 p-6 text-center dark:bg-[var(--op-color-gray-990)]">
          <h4 className="text-sm font-medium text-op-text-primary">
            Get a recommendation for this location
          </h4>
          <p className="mt-1 text-xs text-[var(--op-color-gray-550)]">
            Tell us how this location operates and Tummly will suggest suitable
            QR materials and quantities.
          </p>
          <Button
            type="button"
            variant="op-tertiary"
            className="mt-3"
            onClick={onAddLocationDetails}
          >
            Add location details
          </Button>
        </div>
      ) : (
        <div className={SHOP_RECOMMENDATION_INNER_CLASS}>
          {basedOn ? (
            <div className={SHOP_RECOMMENDATION_BASED_ON_CLASS}>
              <div className="flex min-w-0 flex-col gap-[22px]">
                <h4 className="text-lg font-medium text-op-text-primary">
                  Based on
                </h4>
                <div className="flex flex-wrap items-start gap-3">
                  <span className={SHOP_RECOMMENDATION_TAG_CLASS}>
                    {basedOn.tableCount} guest tables
                  </span>
                  <span className={SHOP_RECOMMENDATION_TAG_CLASS}>
                    {basedOn.counterCount} service{" "}
                    {basedOn.counterCount === 1 ? "counter" : "counters"}
                  </span>
                  <span className={SHOP_RECOMMENDATION_TAG_CLASS}>
                    {basedOn.entranceCount + basedOn.secondaryEntranceCount}{" "}
                    {basedOn.entranceCount + basedOn.secondaryEntranceCount === 1
                      ? "entrance"
                      : "entrances"}
                  </span>
                  {basedOn.takeawayVolume ? (
                    <span className={SHOP_RECOMMENDATION_TAG_CLASS}>
                      {formatTakeawayVolume(basedOn.takeawayVolume)}
                    </span>
                  ) : null}
                  {basedOn.promptLocations.length > 0 ? (
                    <span className={SHOP_RECOMMENDATION_TAG_CLASS}>
                      {basedOn.promptLocations.map(formatPromptLabel).join(", ")}
                    </span>
                  ) : null}
                  <span className={SHOP_RECOMMENDATION_TAG_CLASS}>
                    {formatExistingMaterials(basedOn.existingMaterials)}
                  </span>
                  <span className={SHOP_RECOMMENDATION_TAG_CLASS}>
                    QR activity from the last 30 days
                  </span>
                </div>
              </div>

              <Button
                type="button"
                variant="op-tertiary"
                className="h-[42px] shrink-0 self-start"
                onClick={onAddLocationDetails}
              >
                Update location details
              </Button>
            </div>
          ) : null}

          <div className="flex flex-col gap-[42px] p-5">
            <div className="flex gap-[22px] overflow-x-auto pb-1 [scrollbar-width:none] [-ms-overflow-style:none] [&::-webkit-scrollbar]:hidden">
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

            {summary && lines.length > 0 ? (
              <div className="flex flex-col gap-8">
                <div className="flex flex-col gap-7">
                  <div className="flex flex-col gap-2">
                    <h4 className="text-lg font-medium text-op-text-primary">
                      Recommendation summary
                    </h4>
                    <span className="text-sm font-medium text-[var(--op-color-gray-550)]">
                      {summary.materialTypeCount} material types ·{" "}
                      {summary.totalPieces} total pieces
                    </span>
                  </div>

                  <div className="flex w-full max-w-[473px] flex-col gap-2.5 text-base">
                    <div className="flex items-center justify-between gap-4">
                      <span className="font-semibold text-[var(--op-color-gray-550)]">
                        Materials subtotal:
                      </span>
                      <span className="font-medium text-op-text-primary">
                        {formatShopPence(summary.materialsNetPence)}
                      </span>
                    </div>
                    <div className={SHOP_PRODUCT_SPEC_DIVIDER_CLASS} />
                    <div className="flex items-center justify-between gap-4">
                      <span className="font-semibold text-[var(--op-color-gray-550)]">
                        Estimated total:
                      </span>
                      <span className="font-medium text-op-text-primary text-right">
                        {formatShopPence(summary.materialsNetPence)} excluding
                        VAT and delivery
                      </span>
                    </div>
                  </div>
                </div>

                <div className={SHOP_PRODUCT_SPEC_DIVIDER_CLASS} />

                <div className="flex flex-wrap items-center gap-[18px]">
                  <Button
                    type="button"
                    variant="op-primary"
                    disabled={purchaseBlocked}
                    onClick={onAddRecommendedToCart}
                  >
                    Add recommended items
                  </Button>
                  <Button
                    type="button"
                    variant="op-tertiary"
                    className="h-[42px]"
                    onClick={onAddLocationDetails}
                  >
                    Review recommendation
                  </Button>
                </div>
              </div>
            ) : null}
          </div>
        </div>
      )}
    </div>
  )
}

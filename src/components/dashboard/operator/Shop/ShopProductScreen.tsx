import { useState, useEffect, useMemo } from "react"
import {
  ChevronRight,
  ShoppingBag,
  MapPin,
  ChevronDown,
  X,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  ShopCatalogItemCard,
} from "@/components/dashboard/operator/Shop/ShopCatalogItemCard"
import {
  ShopLocationPicker,
  type ShopLocationOption,
} from "@/components/dashboard/operator/Shop/ShopLocationPicker"
import type { ShopProduct } from "@/lib/operatorShop/shopCatalogTypes"
import type { ShopPaidWriteChrome } from "@/lib/operatorShop/shopPaidWriteChrome"
import { ShopPaidWriteHelperNote } from "@/components/dashboard/operator/Shop/ShopPaidWriteHelperNote"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"
import tummlyStickerImg from "@/assets/images/shop/tummly-sticker.png"
import tummlyBagImg from "@/assets/images/shop/tummly-bag.png"
import {
  SHOP_BODY_TEXT_CLASS,
  SHOP_PRODUCT_DETAIL_CARD_CLASS,
  SHOP_PRODUCT_SPEC_DIVIDER_CLASS,
} from "@/lib/operatorShop/shopSurfacePresentation"
import { cn } from "@/lib/utils"

export function scrollShopPaneToTop() {
  window.scrollTo({ top: 0, behavior: "smooth" })
  document.documentElement.scrollTo({ top: 0, behavior: "smooth" })
  document.body.scrollTo({ top: 0, behavior: "smooth" })
  const scrollContainers = document.querySelectorAll<HTMLElement>(
    "main .overflow-y-auto, main [class*='overflow-y'], .overflow-y-auto"
  )
  scrollContainers.forEach((el) => {
    el.scrollTo({ top: 0, behavior: "smooth" })
  })
}

type PackageOption = {
  id: string
  label: string
  quantity: number
  pricePounds: number
}

type ShopProductScreenProps = {
  product: ShopProduct
  catalogProducts: ShopProduct[]
  selectedLocationId: number
  selectedLocationName: string
  locations: ShopLocationOption[]
  brandLogoPublicUrl: string | null
  mode: OperatorDashboardMode
  /** Catalog seller rate; 0 = launch VAT OFF. */
  vatRateBps?: number
  onSelectLocation?: (locationId: number) => void
  onBackToShop: () => void
  onAddToCart: (product: ShopProduct, quantity: number) => void
  onOrderNow: (product: ShopProduct, quantity: number) => void
  onSelectRelatedProduct: (product: ShopProduct) => void
  paidWriteChrome: ShopPaidWriteChrome
}

export function ShopProductScreen({
  product,
  catalogProducts,
  selectedLocationId,
  selectedLocationName,
  locations,
  brandLogoPublicUrl,
  mode,
  vatRateBps = 0,
  onSelectLocation,
  onBackToShop,
  onAddToCart,
  onOrderNow,
  onSelectRelatedProduct,
  paidWriteChrome,
}: ShopProductScreenProps) {
  const purchaseBlocked =
    paidWriteChrome.purchaseDisabled || !product.availableInShop
  const showVatChrome = vatRateBps > 0
  const comingSoon = !product.availableInShop
  const packageOptions = useMemo<PackageOption[]>(() => {
    const packs =
      product.packs.length > 0
        ? product.packs
        : [{ qty: product.minOrderQty || 1, netPence: product.unitNetPence }]
    return packs.map((pack) => ({
      id: `pack-${pack.qty}`,
      label: `Pack of ${pack.qty.toLocaleString("en-GB")}`,
      quantity: pack.qty,
      pricePounds: pack.netPence / 100,
    }))
  }, [product.packs, product.minOrderQty, product.unitNetPence])

  const defaultPackId = packageOptions[0]?.id ?? ""
  const [selectedPackId, setSelectedPackId] = useState<string>(defaultPackId)
  const [activeImageIdx, setActiveImageIdx] = useState<number>(0)

  // Scroll pane to top whenever a product is opened or changed
  useEffect(() => {
    scrollShopPaneToTop()
    setSelectedPackId(defaultPackId)
    setActiveImageIdx(0)
  }, [product.id, defaultPackId])

  const selectedPack =
    packageOptions.find((pack) => pack.id === selectedPackId) ??
    packageOptions[0]

  const thumbnails =
    product.imageSrcs.length > 0
      ? product.imageSrcs
      : [product.imageSrc, tummlyStickerImg, tummlyBagImg].filter(Boolean)

  const handleSelectPack = (pack: PackageOption) => {
    setSelectedPackId(pack.id)
  }

  const selectedQuantity = selectedPack?.quantity ?? product.minOrderQty
  const calculatedPrice = (selectedPack?.pricePounds ?? product.price).toFixed(
    2
  )

  const relatedProducts = catalogProducts
    .filter((p) => p.id !== product.id)
    .slice(0, 4)

  const handleSelectRelated = (relProduct: ShopProduct) => {
    onSelectRelatedProduct(relProduct)
    scrollShopPaneToTop()
  }

  return (
    <div className="flex flex-col gap-8 pb-24">
      {/* Top Header / Breadcrumb Bar */}
      <div className="flex items-center justify-between border-b border-op-border-default pb-4">
        <div className="flex flex-wrap items-center gap-2 text-sm font-medium">
          <button
            type="button"
            onClick={onBackToShop}
            className="text-op-text-primary transition-colors hover:underline"
          >
            Tummly Shop
          </button>
          <ChevronRight className="size-3.5 text-op-text-muted" />
          <span className="text-op-text-body capitalize">
            {product.category === "tabletop" ? "Table and dine-in" : product.category}
          </span>
          <ChevronRight className="size-3.5 text-op-text-muted" />
          <span className="text-op-text-muted">{product.title}</span>
        </div>

        <button
          type="button"
          onClick={onBackToShop}
          className="flex size-8 items-center justify-center rounded-[4px] bg-op-surface-secondary text-op-text-muted transition-colors hover:bg-op-action-secondary-hover hover:text-op-text-primary focus-visible:outline-none"
          aria-label="Back to Tummly Shop"
        >
          <X className="size-4" />
        </button>
      </div>

      {/* Main 2-Column Product Layout */}
      <div className="grid grid-cols-1 gap-10 lg:grid-cols-12">
        {/* Left Column: Image Gallery & About Text */}
        <div className="flex flex-col gap-8 lg:col-span-6">
          {/* Main Showcase Image */}
          <div className="flex flex-col gap-3">
            <div className="flex h-[420px] w-full items-center justify-center overflow-hidden rounded-op-lg border border-op-border-default bg-op-color-gray-60 p-6 dark:bg-[var(--op-color-gray-990)] sm:h-[520px]">
              <img
                src={thumbnails[activeImageIdx]}
                alt={product.title}
                className="size-full scale-[1.12] object-contain transition-all duration-300"
              />
            </div>

            {/* Thumbnails Row */}
            <div className="grid grid-cols-4 gap-3">
              {thumbnails.map((thumb, idx) => (
                <button
                  key={idx}
                  type="button"
                  onClick={() => setActiveImageIdx(idx)}
                  className={cn(
                    "flex h-24 items-center justify-center overflow-hidden rounded-op-lg border bg-op-color-gray-60 p-2 transition-colors dark:bg-[var(--op-color-gray-990)] sm:h-28",
                    activeImageIdx === idx
                      ? "border-op-action-primary ring-1 ring-op-action-primary"
                      : "border-op-border-default hover:border-op-action-tertiary"
                  )}
                >
                  <img
                    src={thumb}
                    alt={`${product.title} view ${idx + 1}`}
                    className="size-full scale-[1.15] object-contain"
                  />
                </button>
              ))}
            </div>
          </div>

          {/* About Section — flat on page (not a card) */}
          {product.aboutBody ? (
            <div className="flex flex-col gap-3">
              <h3 className="text-lg font-medium text-op-text-primary">
                About the {product.title.toLowerCase()}
              </h3>
              <p className={cn("text-sm font-medium leading-[19px]", SHOP_BODY_TEXT_CLASS)}>
                {product.aboutBody}
              </p>
            </div>
          ) : null}
        </div>

        {/* Right Column: Configuration & Order Summary */}
        <div className="flex flex-col gap-6 lg:col-span-6">
          {/* Title & Description */}
          <div className="flex flex-col gap-2">
            <h1 className="font-jakarta text-2xl font-bold text-op-text-primary sm:text-3xl">
              {product.title}
            </h1>
            <p className={cn("text-sm leading-relaxed", SHOP_BODY_TEXT_CLASS)}>
              {product.description}
            </p>
          </div>

          {/* Box 1: Prepared for */}
          <div className={cn(SHOP_PRODUCT_DETAIL_CARD_CLASS, "flex flex-col gap-4 p-5")}>
            <div className="flex flex-col gap-1">
              <h3 className="text-base font-semibold text-op-text-primary">
                Prepared for
              </h3>
              <p className={cn("text-xs", SHOP_BODY_TEXT_CLASS)}>
                The printed QR code will connect guests to the selected guest form and track activity for this location.
              </p>
            </div>

            <div className="flex items-center justify-between rounded-md border border-op-border-default bg-op-background-primary px-3.5 py-3 text-xs text-op-text-primary">
              <div className="flex items-center gap-2">
                <MapPin className="size-4 text-op-text-muted" />
                <span className="font-medium">{selectedLocationName}</span>
              </div>
              <ChevronDown className="size-4 text-op-text-muted" />
            </div>

            {locations.length > 1 && onSelectLocation ? (
              <ShopLocationPicker
                variant="change"
                selectedLocationId={selectedLocationId}
                selectedLocationName={selectedLocationName}
                locations={locations}
                brandLogoPublicUrl={brandLogoPublicUrl}
                mode={mode}
                onSelectLocation={onSelectLocation}
                changeClassName="self-start"
              />
            ) : null}
          </div>

          {/* Box 2: Choose a quantity */}
          <div className={cn(SHOP_PRODUCT_DETAIL_CARD_CLASS, "flex flex-col gap-4 p-5")}>
            <h3 className="text-base font-semibold text-op-text-primary">
              Choose a pack
            </h3>

            <div className="flex flex-col gap-2.5">
              {packageOptions.map((pack) => {
                const isSelected = selectedPackId === pack.id
                return (
                  <button
                    key={pack.id}
                    type="button"
                    onClick={() => handleSelectPack(pack)}
                    className={cn(
                      "flex items-center justify-between rounded-md border p-3.5 text-xs transition-colors",
                      isSelected
                        ? "border-op-action-primary bg-op-surface-secondary/80 font-medium text-op-text-primary"
                        : cn("border-op-border-default bg-op-background-primary hover:border-op-action-tertiary", SHOP_BODY_TEXT_CLASS)
                    )}
                  >
                    <div className="flex items-center gap-2.5">
                      <span className="text-sm font-medium text-op-text-primary">
                        {pack.label}
                      </span>
                    </div>
                    <span className="text-sm font-semibold text-op-text-primary">
                      £{pack.pricePounds.toFixed(2)}
                    </span>
                  </button>
                )
              })}
            </div>
          </div>

          {/* Box 3: Order summary */}
          <div className="flex flex-col gap-4 rounded-op-lg border border-op-border-default bg-op-surface-secondary p-5">
            <h3 className="text-base font-semibold text-op-text-primary">
              Order summary
            </h3>

            <div className={cn("flex flex-col gap-2.5 text-xs", SHOP_BODY_TEXT_CLASS)}>
              <div className="flex justify-between">
                <span>Material</span>
                <span className="font-medium text-op-text-primary">{product.title}</span>
              </div>
              <div className="h-px bg-op-border-default/60" />
              <div className="flex justify-between">
                <span>Location</span>
                <span className="font-medium text-op-text-primary">{selectedLocationName}</span>
              </div>
              <div className="h-px bg-op-border-default/60" />
              <div className="flex justify-between">
                <span>Quantity</span>
                <span className="font-medium text-op-text-primary">
                  {selectedQuantity.toLocaleString("en-GB")} pieces
                </span>
              </div>
              <div className="h-px bg-op-border-default/60" />
              <div className="flex justify-between">
                <span>Material subtotal</span>
                <span className="font-semibold text-op-text-primary">
                  £{calculatedPrice}
                </span>
              </div>
              <div className="h-px bg-op-border-default/60" />
              <div className="flex justify-between">
                <span>{showVatChrome ? "VAT and delivery" : "Delivery"}</span>
                <span className="font-medium text-op-text-primary">
                  Calculated at checkout
                </span>
              </div>
            </div>

            {/* Total and Actions */}
            <div className="flex flex-col gap-4 border-t border-op-border-default/60 pt-4 sm:flex-row sm:items-end sm:justify-between">
              <div className="flex flex-col gap-0.5">
                <span className="font-jakarta text-2xl font-bold text-op-text-primary sm:text-3xl">
                  £{calculatedPrice}
                </span>
                <span className={cn("text-xs", SHOP_BODY_TEXT_CLASS)}>
                  {showVatChrome
                    ? "Excluding VAT and delivery"
                    : "Excluding delivery"}
                </span>
              </div>

              {comingSoon ? (
                <p className={cn("text-sm font-medium", SHOP_BODY_TEXT_CLASS)}>
                  Coming soon — this material is not available to order yet.
                </p>
              ) : (
                <div className="flex items-center gap-2.5">
                  <Button
                    type="button"
                    variant="op-primary"
                    disabled={purchaseBlocked}
                    onClick={() => onOrderNow(product, selectedQuantity)}
                  >
                    <ShoppingBag className="size-3.5" />
                    Order now
                  </Button>
                  <Button
                    type="button"
                    variant="op-secondary"
                    disabled={purchaseBlocked}
                    onClick={() => onAddToCart(product, selectedQuantity)}
                  >
                    Add to cart
                  </Button>
                </div>
              )}
              {!comingSoon ? (
                <ShopPaidWriteHelperNote
                  chrome={paidWriteChrome}
                  className="text-xs"
                />
              ) : null}
            </div>
          </div>
        </div>
      </div>

      {/* Specifications & QR Tracking — flat on page (Figma 4405:52884) */}
      <div className="border-t border-op-border-default/80 pt-8">
        <div className="grid grid-cols-1 gap-[34px] lg:grid-cols-12 lg:gap-[34px]">
          <div className="flex flex-col gap-8 lg:col-span-6">
            <h3 className="text-lg font-medium text-op-text-primary">
              Product specifications
            </h3>
            <div className="flex flex-col gap-3.5 text-base">
              {(product.specifications.length > 0
                ? product.specifications
                : [
                    { label: "Material", value: product.material },
                    { label: "Dimensions", value: product.dimensions },
                  ].filter((row) => row.value)
              ).map((row, index, rows) => (
                <div key={`${row.label}-${index}`} className="flex flex-col gap-3.5">
                  <div className="flex items-center justify-between gap-4">
                    <span className={cn("font-semibold", SHOP_BODY_TEXT_CLASS)}>
                      {row.label}
                    </span>
                    <span className="font-medium text-op-text-primary text-right">
                      {row.value}
                    </span>
                  </div>
                  {index < rows.length - 1 ? (
                    <div className={SHOP_PRODUCT_SPEC_DIVIDER_CLASS} />
                  ) : null}
                </div>
              ))}
            </div>
          </div>

          <div className="flex flex-col gap-[34px] lg:col-span-6 lg:border-l lg:border-op-border-default lg:pl-[34px]">
            <div className="flex flex-col gap-3">
              <h3 className="text-lg font-medium text-op-text-primary">
                Connected QR tracking
              </h3>
              <p className={cn("text-sm font-medium leading-[19px]", SHOP_BODY_TEXT_CLASS)}>
                The QR code printed on this material is connected to one location and one QR placement. Tummly records scans, form starts and completed submissions against that placement.
              </p>
            </div>

            <div className={SHOP_PRODUCT_SPEC_DIVIDER_CLASS} />

            <div className="flex flex-col gap-3">
              <h3 className="text-lg font-medium text-op-text-primary">
                Production and delivery
              </h3>
              <p className={cn("text-sm font-medium leading-[19px]", SHOP_BODY_TEXT_CLASS)}>
                Materials enter production after the order, artwork and location connection have been confirmed. Delivery estimates are shown at checkout and may vary by quantity and delivery address.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* You May Also Need Section */}
      <div className="flex flex-col gap-6 border-t border-op-border-default/80 pt-8">
        <div className="flex flex-col gap-1">
          <h3 className="text-lg font-semibold text-op-text-primary">
            You may also need
          </h3>
          <p className={cn("text-xs", SHOP_BODY_TEXT_CLASS)}>
            Complementary materials commonly paired with table prompts.
          </p>
        </div>

        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {relatedProducts.map((relProduct) => (
            <ShopCatalogItemCard
              key={relProduct.id}
              variant="secondary"
              title={relProduct.title}
              description={relProduct.description}
              price={relProduct.price}
              imageSrc={relProduct.imageSrc}
              onViewMaterial={() => handleSelectRelated(relProduct)}
            />
          ))}
        </div>
      </div>
    </div>
  )
}

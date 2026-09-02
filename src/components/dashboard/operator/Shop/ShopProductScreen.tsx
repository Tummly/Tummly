import { useState, useEffect } from "react"
import {
  ChevronRight,
  ShoppingBag,
  Minus,
  Plus,
  MapPin,
  ChevronDown,
  X,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  ShopCatalogItemCard,
} from "@/components/dashboard/operator/Shop/ShopCatalogItemCard"
import type { ShopProduct } from "@/lib/operatorShop/shopCatalogTypes"
import tummlyStickerImg from "@/assets/images/shop/tummly-sticker.png"
import tummlyBagImg from "@/assets/images/shop/tummly-bag.png"
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
  price: number
  isRecommended?: boolean
}

const PACKAGE_OPTIONS: PackageOption[] = [
  { id: "pack-5", label: "Pack of 5", quantity: 5, price: 24.0 },
  { id: "pack-10", label: "Pack of 10", quantity: 10, price: 39.0 },
  {
    id: "pack-20",
    label: "Pack of 20",
    quantity: 20,
    price: 69.0,
    isRecommended: true,
  },
]

type ShopProductScreenProps = {
  product: ShopProduct
  catalogProducts: ShopProduct[]
  selectedLocationName: string
  locations: Array<{ id: number; locationName: string; address: string }>
  onSelectLocation?: (locationId: number) => void
  onBackToShop: () => void
  onAddToCart: (product: ShopProduct, quantity: number) => void
  onOrderNow: (product: ShopProduct, quantity: number) => void
  onSelectRelatedProduct: (product: ShopProduct) => void
}

export function ShopProductScreen({
  product,
  catalogProducts,
  selectedLocationName,
  locations,
  onSelectLocation,
  onBackToShop,
  onAddToCart,
  onOrderNow,
  onSelectRelatedProduct,
}: ShopProductScreenProps) {
  const [selectedPackId, setSelectedPackId] = useState<string>("pack-20")
  const [customQuantity, setCustomQuantity] = useState<number>(20)
  const [activeImageIdx, setActiveImageIdx] = useState<number>(0)

  // Scroll pane to top whenever a product is opened or changed
  useEffect(() => {
    scrollShopPaneToTop()
    setSelectedPackId("pack-20")
    setCustomQuantity(20)
    setActiveImageIdx(0)
  }, [product.id])

  const selectedPack =
    PACKAGE_OPTIONS.find((p) => p.id === selectedPackId) ?? PACKAGE_OPTIONS[2]

  const thumbnails = [
    product.imageSrc,
    tummlyStickerImg,
    tummlyBagImg,
    product.imageSrc,
  ]

  const handleSelectPack = (pack: PackageOption) => {
    setSelectedPackId(pack.id)
    setCustomQuantity(pack.quantity)
  }

  const handleIncrease = () => {
    setCustomQuantity((prev) => prev + 1)
  }

  const handleDecrease = () => {
    setCustomQuantity((prev) => (prev > 1 ? prev - 1 : 1))
  }

  // Calculate price dynamically based on quantity
  const unitPrice = selectedPack.price / selectedPack.quantity
  const calculatedPrice = (unitPrice * customQuantity).toFixed(2)

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
          <span className="text-op-text-secondary capitalize">
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
            <div className="flex h-[420px] w-full items-center justify-center overflow-hidden rounded-md border border-op-border-default bg-op-background-primary/80 p-6 sm:h-[520px]">
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
                    "flex h-24 items-center justify-center overflow-hidden rounded-md border bg-op-background-primary/80 p-2 transition-colors sm:h-28",
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

          {/* About Section */}
          <div className="flex flex-col gap-3 rounded-md border border-op-border-default bg-op-card-background p-6">
            <h3 className="text-lg font-semibold text-op-text-primary">
              About the {product.title.toLowerCase()}
            </h3>
            <p className="text-sm font-normal leading-relaxed text-op-text-secondary">
              The Tummly {product.title.toLowerCase()} gives dine-in guests a clear opportunity to share private feedback while their experience is still recent. Each order uses a location-specific QR placement, allowing Tummly to measure scans, form starts and completed feedback submissions from table-based prompts.
            </p>
          </div>
        </div>

        {/* Right Column: Configuration & Order Summary */}
        <div className="flex flex-col gap-6 lg:col-span-6">
          {/* Title & Description */}
          <div className="flex flex-col gap-2">
            <h1 className="text-2xl font-bold tracking-tight text-op-text-primary sm:text-3xl">
              {product.title}
            </h1>
            <p className="text-sm leading-relaxed text-op-text-secondary">
              {product.description}
            </p>
          </div>

          {/* Box 1: Prepared for */}
          <div className="flex flex-col gap-4 rounded-md border border-op-border-default bg-op-card-background p-5">
            <div className="flex flex-col gap-1">
              <h3 className="text-base font-semibold text-op-text-primary">
                Prepared for
              </h3>
              <p className="text-xs text-op-text-secondary">
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

            {locations.length > 1 && onSelectLocation && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="self-start rounded-[4px] border-op-border-default bg-transparent text-xs text-op-text-primary hover:bg-op-surface-secondary"
                  >
                    Change location
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent
                  align="start"
                  className="w-56 border-op-border-default bg-op-surface-secondary text-op-text-primary"
                >
                  {locations.map((loc) => (
                    <DropdownMenuItem
                      key={loc.id}
                      onClick={() => onSelectLocation(loc.id)}
                      className={cn(
                        "cursor-pointer text-xs",
                        loc.locationName === selectedLocationName &&
                          "font-semibold text-op-action-primary"
                      )}
                    >
                      {loc.locationName}
                    </DropdownMenuItem>
                  ))}
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </div>

          {/* Box 2: Choose a quantity */}
          <div className="flex flex-col gap-4 rounded-md border border-op-border-default bg-op-card-background p-5">
            <div className="flex flex-col gap-1">
              <h3 className="text-base font-semibold text-op-text-primary">
                Choose a quantity
              </h3>
              <p className="text-xs text-op-text-secondary">
                <strong className="text-op-text-primary">20 recommended</strong>{" "}
                — This location has 18 guest tables. We recommend one table tent per table and two spare.
              </p>
            </div>

            <div className="flex flex-col gap-2.5">
              {PACKAGE_OPTIONS.map((pack) => {
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
                        : "border-op-border-default bg-op-background-primary text-op-text-secondary hover:border-op-action-tertiary"
                    )}
                  >
                    <div className="flex items-center gap-2.5">
                      <span className="text-sm font-medium text-op-text-primary">
                        {pack.label}
                      </span>
                      {pack.isRecommended && (
                        <span className="rounded-xs border border-op-action-primary/40 bg-op-action-primary/10 px-2 py-0.5 text-[11px] font-semibold text-op-text-success">
                          Recommended
                        </span>
                      )}
                    </div>
                    <span className="text-sm font-semibold text-op-text-primary">
                      £{pack.price.toFixed(2)}
                    </span>
                  </button>
                )
              })}
            </div>

            {/* Stepper for custom adjustments */}
            <div className="flex items-center gap-3 pt-1">
              <button
                type="button"
                onClick={handleDecrease}
                className="flex size-7 items-center justify-center rounded-[3px] border border-op-border-default bg-op-background-primary text-op-text-primary transition-colors hover:bg-op-surface-secondary"
                aria-label="Decrease quantity"
              >
                <Minus className="size-3" />
              </button>
              <span className="min-w-6 text-center text-base font-semibold text-op-text-primary">
                {customQuantity}
              </span>
              <button
                type="button"
                onClick={handleIncrease}
                className="flex size-7 items-center justify-center rounded-[3px] border border-op-border-default bg-op-background-primary text-op-text-primary transition-colors hover:bg-op-surface-secondary"
                aria-label="Increase quantity"
              >
                <Plus className="size-3" />
              </button>
            </div>
          </div>

          {/* Box 3: Order summary */}
          <div className="flex flex-col gap-4 rounded-md border border-op-border-default bg-op-card-background p-5">
            <h3 className="text-base font-semibold text-op-text-primary">
              Order summary
            </h3>

            <div className="flex flex-col gap-2.5 text-xs text-op-text-secondary">
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
                  {customQuantity} pieces
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
                <span>VAT and delivery</span>
                <span className="font-medium text-op-text-primary">
                  Calculated at checkout
                </span>
              </div>
            </div>

            {/* Total and Actions */}
            <div className="flex flex-col gap-4 border-t border-op-border-default/60 pt-4 sm:flex-row sm:items-end sm:justify-between">
              <div className="flex flex-col gap-0.5">
                <span className="text-2xl font-bold text-op-text-primary sm:text-3xl">
                  £{calculatedPrice}
                </span>
                <span className="text-xs text-op-text-muted">
                  Excluding VAT and delivery
                </span>
              </div>

              <div className="flex items-center gap-2.5">
                <Button
                  type="button"
                  variant="op-primary"
                  className="h-10 gap-1.5 rounded-md px-5 text-xs font-semibold"
                  onClick={() => onOrderNow(product, customQuantity)}
                >
                  <ShoppingBag className="size-3.5" />
                  Order now
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="h-10 rounded-md border-op-border-default bg-transparent px-4 text-xs font-medium text-op-text-primary hover:bg-op-surface-secondary"
                  onClick={() => onAddToCart(product, customQuantity)}
                >
                  Add to cart
                </Button>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Specifications & QR Tracking Section */}
      <div className="border-t border-op-border-default/80 pt-8">
        <div className="grid grid-cols-1 gap-10 lg:grid-cols-12">
          {/* Specifications */}
          <div className="flex flex-col gap-4 lg:col-span-6">
            <h3 className="text-lg font-semibold text-op-text-primary">
              Product specifications
            </h3>
            <div className="flex flex-col gap-2.5 rounded-md border border-op-border-default bg-op-card-background p-5 text-xs text-op-text-secondary">
              <div className="flex justify-between">
                <span>Finished size</span>
                <span className="font-medium text-op-text-primary">A5</span>
              </div>
              <div className="h-px bg-op-border-default/60" />
              <div className="flex justify-between">
                <span>Construction</span>
                <span className="font-medium text-op-text-primary">Folded A-frame</span>
              </div>
              <div className="h-px bg-op-border-default/60" />
              <div className="flex justify-between">
                <span>Print</span>
                <span className="font-medium text-op-text-primary">Double-sided</span>
              </div>
              <div className="h-px bg-op-border-default/60" />
              <div className="flex justify-between">
                <span>Finish</span>
                <span className="font-medium text-op-text-primary">Matte</span>
              </div>
              <div className="h-px bg-op-border-default/60" />
              <div className="flex justify-between">
                <span>Material</span>
                <span className="font-medium text-op-text-primary">{product.material}</span>
              </div>
              <div className="h-px bg-op-border-default/60" />
              <div className="flex justify-between">
                <span>QR connection</span>
                <span className="font-medium text-op-text-primary">Location and placement-specific</span>
              </div>
              <div className="h-px bg-op-border-default/60" />
              <div className="flex justify-between">
                <span>Recommended use</span>
                <span className="font-medium text-op-text-primary">Dine-in tables</span>
              </div>
              <div className="h-px bg-op-border-default/60" />
              <div className="flex justify-between">
                <span>Minimum quantity</span>
                <span className="font-medium text-op-text-primary">Pack of 5</span>
              </div>
              <div className="h-px bg-op-border-default/60" />
              <div className="flex justify-between">
                <span>Production</span>
                <span className="font-medium text-op-text-primary">Made to order</span>
              </div>
            </div>
          </div>

          {/* QR Tracking & Delivery info */}
          <div className="flex flex-col gap-6 lg:col-span-6">
            <div className="flex flex-col gap-3 rounded-md border border-op-border-default bg-op-card-background p-5">
              <h3 className="text-lg font-semibold text-op-text-primary">
                Connected QR tracking
              </h3>
              <p className="text-xs leading-relaxed text-op-text-secondary">
                The QR code printed on this material is connected to one location and one QR placement. Tummly records scans, form starts and completed submissions against that placement.
              </p>
            </div>

            <div className="flex flex-col gap-3 rounded-md border border-op-border-default bg-op-card-background p-5">
              <h3 className="text-lg font-semibold text-op-text-primary">
                Production and delivery
              </h3>
              <p className="text-xs leading-relaxed text-op-text-secondary">
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
          <p className="text-xs text-op-text-muted">
            Complementary materials commonly paired with table prompts.
          </p>
        </div>

        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {relatedProducts.map((relProduct) => (
            <ShopCatalogItemCard
              key={relProduct.id}
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

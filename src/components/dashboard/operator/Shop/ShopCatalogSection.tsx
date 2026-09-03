
import { ChevronLeft, ChevronRight } from "lucide-react"
import { Button } from "@/components/ui/button"
import { ShopCatalogItemCard } from "@/components/dashboard/operator/Shop/ShopCatalogItemCard"
import { GUESTS_PAGINATION_BUTTON_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import type { ShopProduct } from "@/lib/operatorShop/shopCatalogTypes"

type ShopCatalogSectionProps = {
  products: ShopProduct[]
  searchQuery: string
  onAddToCart: (product: ShopProduct, quantity?: number) => void
  onSelectProduct?: (product: ShopProduct) => void
}

export function ShopCatalogSection({
  products,
  searchQuery,
  onAddToCart,
  onSelectProduct,
}: ShopCatalogSectionProps) {
  const filteredProducts = products.filter((product) => {
    return (
      searchQuery.trim() === "" ||
      product.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      product.description.toLowerCase().includes(searchQuery.toLowerCase())
    )
  })

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h3 className="text-xl font-bold tracking-tight text-foreground sm:text-2xl">
          A suggested QR materials kit
        </h3>
        <p className="text-xs font-normal text-muted-foreground sm:text-sm">
          Based on your location setup and recent QR activity, Tummly recommends the following materials.
        </p>
      </div>

      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4">
        {filteredProducts.map((product) => (
          <ShopCatalogItemCard
            key={product.id}
            title={product.title}
            description={product.description}
            price={product.price}
            imageSrc={product.imageSrc}
            isPlanIncluded={product.isPlanIncluded}
            popularBadge={product.popularBadge}
            onViewMaterial={() =>
              onSelectProduct ? onSelectProduct(product) : onAddToCart(product, 1)
            }
          />
        ))}
      </div>

      <div className="flex items-center justify-between border-t border-op-border-default/60 pt-4 text-xs text-muted-foreground">
        <div className="flex items-center gap-3">
          <Button
            type="button"
            variant="op-secondary"
            disabled
            aria-disabled
            aria-label="Previous page"
            className={GUESTS_PAGINATION_BUTTON_CLASS}
          >
            <ChevronLeft className="size-3.5" />
            Previous
          </Button>
          <Button
            type="button"
            variant="op-secondary"
            aria-label="Next page"
            className={GUESTS_PAGINATION_BUTTON_CLASS}
          >
            Next
            <ChevronRight className="size-3.5" />
          </Button>
        </div>

        <span>
          Showing 1–{filteredProducts.length} of {products.length} items
        </span>
      </div>
    </div>
  )
}

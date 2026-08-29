
import { ChevronLeft, ChevronRight } from "lucide-react"
import { Button } from "@/components/ui/button"
import { ShopCatalogItemCard } from "@/components/dashboard/operator/Shop/ShopCatalogItemCard"
import tummlyStickerImg from "@/assets/images/shop/tummly-sticker.png"
import tummlyBagImg from "@/assets/images/shop/tummly-bag.png"

export type ShopProduct = {
  id: string
  title: string
  category: "tabletop" | "window" | "payment" | "staff" | "takeaway" | "delivery"
  description: string
  material: string
  dimensions: string
  price: number
  isPlanIncluded?: boolean
  popularBadge?: string
  imageSrc: string
}

export const SHOP_CATALOG_PRODUCTS: ShopProduct[] = [
  {
    id: "table-tents",
    title: "Table tents",
    category: "tabletop",
    description:
      "Place a branded QR prompt on guest tables to collect private feedback after a dine-in visit.",
    material: "3mm Matte Frosted Acrylic",
    dimensions: "105mm x 148mm (A6)",
    price: 24.0,
    isPlanIncluded: true,
    popularBadge: "Essential",
    imageSrc: tummlyStickerImg,
  },
  {
    id: "counter-cards-1",
    title: "Counter cards",
    category: "payment",
    description:
      "Place a compact QR prompt on ordering, payment or collection counter.",
    material: "350gsm Soft-touch Recycled Card",
    dimensions: "85mm x 55mm (Card size)",
    price: 18.0,
    imageSrc: tummlyStickerImg,
  },
  {
    id: "counter-cards-2",
    title: "Counter cards",
    category: "payment",
    description:
      "Place a compact QR prompt on ordering, payment or collection counter.",
    material: "350gsm Soft-touch Recycled Card",
    dimensions: "85mm x 55mm (Card size)",
    price: 18.0,
    imageSrc: tummlyStickerImg,
  },
  {
    id: "window-stickers",
    title: "Window stickers",
    category: "window",
    description:
      "Invite guests to share feedback using a branded QR prompt at an entrance or front windows.",
    material: "UV-resistant Clear Vinyl Cling",
    dimensions: "150mm x 150mm",
    price: 14.0,
    imageSrc: tummlyStickerImg,
  },
  {
    id: "packaging-stickers",
    title: "Packaging stickers",
    category: "takeaway",
    description:
      "Add a compact feedback prompt to takeaway packaging, food boxes or paper bags.",
    material: "Matte Sticker Paper with Strong Adhesive",
    dimensions: "50mm x 50mm",
    price: 22.0,
    imageSrc: tummlyStickerImg,
  },
  {
    id: "receipt-stickers-1",
    title: "Receipt stickers",
    category: "payment",
    description:
      "Add a small QR prompt to printed receipts, collection bags or order slips.",
    material: "Direct Thermal Matte Sticker Roll",
    dimensions: "40mm x 40mm",
    price: 16.0,
    imageSrc: tummlyStickerImg,
  },
  {
    id: "receipt-stickers-bag",
    title: "Receipt stickers",
    category: "takeaway",
    description:
      "Add a small QR prompt to printed receipts, collection bags or order slips.",
    material: "Heavy Kraft Paper Bag with QR Brand Print",
    dimensions: "320mm x 260mm x 120mm",
    price: 16.0,
    imageSrc: tummlyBagImg,
  },
  {
    id: "delivery-inserts",
    title: "Delivery inserts",
    category: "delivery",
    description:
      "Include a branded feedback card inside delivery and collection orders.",
    material: "250gsm Silk Finish Card",
    dimensions: "A6 Postcard Size",
    price: 32.0,
    imageSrc: tummlyStickerImg,
  },
]

type ShopCatalogSectionProps = {
  searchQuery: string
  onAddToCart: (product: ShopProduct, quantity?: number) => void
  onSelectProduct?: (product: ShopProduct) => void
}

export function ShopCatalogSection({
  searchQuery,
  onAddToCart,
  onSelectProduct,
}: ShopCatalogSectionProps) {
  const filteredProducts = SHOP_CATALOG_PRODUCTS.filter((product) => {
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

      {/* 4-column responsive grid */}
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4">
        {filteredProducts.map((product) => (
          <ShopCatalogItemCard
            key={product.id}
            title={product.title}
            description={product.description}
            price={product.price}
            imageSrc={product.imageSrc}
            onViewMaterial={() =>
              onSelectProduct ? onSelectProduct(product) : onAddToCart(product, 1)
            }
          />
        ))}
      </div>

      {/* Bottom pagination */}
      <div className="flex items-center justify-between border-t border-op-border-default/60 pt-4 text-xs text-muted-foreground">
        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="h-8 rounded-md border-op-border-default bg-op-card-background px-3 text-xs text-foreground hover:bg-op-surface-secondary"
            disabled
          >
            <ChevronLeft className="size-3.5" />
            Previous
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="h-8 rounded-md border-op-border-default bg-op-card-background px-3 text-xs text-foreground hover:bg-op-surface-secondary"
          >
            Next
            <ChevronRight className="size-3.5" />
          </Button>
        </div>

        <span>
          Showing 1–{filteredProducts.length} of {SHOP_CATALOG_PRODUCTS.length} items
        </span>
      </div>
    </div>
  )
}

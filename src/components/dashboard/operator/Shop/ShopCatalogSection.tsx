import { useState } from "react"
import { Plus, Check, QrCode, Shield, Layers, Sparkles, CreditCard, UserCheck } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"

export type ShopProduct = {
  id: string
  title: string
  category: "tabletop" | "window" | "payment" | "staff"
  description: string
  material: string
  dimensions: string
  price: number
  isPlanIncluded?: boolean
  popularBadge?: string
  icon: "qr" | "layers" | "shield" | "credit-card" | "user" | "sparkles"
}

export const SHOP_CATALOG_PRODUCTS: ShopProduct[] = [
  {
    id: "table-stands",
    title: "Tabletop Acrylic Stands (Pack of 10)",
    category: "tabletop",
    description: "Premium double-sided matte acrylic table tents with scannable feedback QR code.",
    material: "3mm Matte Frosted Acrylic",
    dimensions: "105mm x 148mm (A6)",
    price: 0,
    isPlanIncluded: true,
    popularBadge: "Essential",
    icon: "layers",
  },
  {
    id: "bill-presenters",
    title: "Bill Presenter Card Inserts (Pack of 50)",
    category: "payment",
    description: "Heavyweight tactile cards designed to slot into guest bill presenters upon payment.",
    material: "350gsm Soft-touch Recycled Card",
    dimensions: "85mm x 55mm (Card size)",
    price: 12.0,
    popularBadge: "High Response Rate",
    icon: "credit-card",
  },
  {
    id: "window-decals",
    title: "Storefront Window Decals (Pack of 4)",
    category: "window",
    description: "Weatherproof static-cling window stickers for front entrance, door, and windows.",
    material: "UV-resistant Clear Vinyl Cling",
    dimensions: "150mm x 150mm",
    price: 8.5,
    icon: "shield",
  },
  {
    id: "staff-badges",
    title: "Staff Lapel QR Badges (Pack of 5)",
    category: "staff",
    description: "Magnetic wearable server pins allowing guests to scan and leave direct feedback.",
    material: "Enamel Coated Metal with Magnet",
    dimensions: "40mm Diameter",
    price: 18.0,
    icon: "user",
  },
  {
    id: "coaster-packs",
    title: "Feedback Beer & Drink Coasters (Pack of 100)",
    category: "tabletop",
    description: "Absorbent branded drink coasters featuring your high-contrast QR prompt.",
    material: "1.5mm Absorbent Pulpboard",
    dimensions: "95mm Square with Rounded Corners",
    price: 22.0,
    icon: "qr",
  },
  {
    id: "outdoor-plaque",
    title: "Outdoor Waterproof Metal Plaque (Single)",
    category: "tabletop",
    description: "Laser-engraved aluminium weatherproof plate for patio tables and outdoor benches.",
    material: "Brushed Anodised Aluminium",
    dimensions: "120mm x 80mm",
    price: 15.0,
    icon: "sparkles",
  },
]

type ShopCatalogSectionProps = {
  searchQuery: string
  onAddToCart: (product: ShopProduct, quantity: number) => void
}

export function ShopCatalogSection({
  searchQuery,
  onAddToCart,
}: ShopCatalogSectionProps) {
  const [selectedCategory, setSelectedCategory] = useState<string>("all")
  const [addedProductIds, setAddedProductIds] = useState<Record<string, boolean>>({})

  const filteredProducts = SHOP_CATALOG_PRODUCTS.filter((product) => {
    const matchesSearch =
      searchQuery.trim() === "" ||
      product.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      product.description.toLowerCase().includes(searchQuery.toLowerCase()) ||
      product.material.toLowerCase().includes(searchQuery.toLowerCase())

    const matchesCategory =
      selectedCategory === "all" || product.category === selectedCategory

    return matchesSearch && matchesCategory
  })

  const handleAdd = (product: ShopProduct) => {
    onAddToCart(product, 1)
    setAddedProductIds((prev) => ({ ...prev, [product.id]: true }))
    setTimeout(() => {
      setAddedProductIds((prev) => ({ ...prev, [product.id]: false }))
    }, 1500)
  }

  const renderIcon = (icon: ShopProduct["icon"]) => {
    switch (icon) {
      case "layers":
        return <Layers className="size-5 text-op-action-primary" />
      case "credit-card":
        return <CreditCard className="size-5 text-op-action-primary" />
      case "shield":
        return <Shield className="size-5 text-op-action-primary" />
      case "user":
        return <UserCheck className="size-5 text-op-action-primary" />
      case "sparkles":
        return <Sparkles className="size-5 text-op-action-primary" />
      case "qr":
      default:
        return <QrCode className="size-5 text-op-action-primary" />
    }
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <h3 className="text-xl font-bold tracking-tight text-foreground">
          A suggested QR materials kit
        </h3>

        <div className="flex flex-wrap items-center gap-1.5">
          {[
            { id: "all", label: "All materials" },
            { id: "tabletop", label: "Tabletop" },
            { id: "payment", label: "Bill & Payment" },
            { id: "window", label: "Window & Doors" },
            { id: "staff", label: "Staff" },
          ].map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => setSelectedCategory(tab.id)}
              className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
                selectedCategory === tab.id
                  ? "bg-op-action-primary text-white"
                  : "bg-op-surface-secondary text-muted-foreground hover:text-foreground"
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {filteredProducts.map((product) => {
          const isAdded = addedProductIds[product.id]

          return (
            <div
              key={product.id}
              className="flex flex-col justify-between rounded-md border border-op-border-default bg-op-card-background p-5 transition-shadow hover:shadow-md"
            >
              <div>
                <div className="flex items-start justify-between gap-2">
                  <div className="rounded-lg border border-op-border-default bg-op-surface-secondary p-2.5">
                    {renderIcon(product.icon)}
                  </div>
                  {product.popularBadge && (
                    <Badge variant="secondary" className="bg-op-surface-secondary text-[11px] font-medium text-foreground">
                      {product.popularBadge}
                    </Badge>
                  )}
                  {product.isPlanIncluded && (
                    <Badge variant="outline" className="border-op-action-primary/40 bg-op-action-primary/10 text-[11px] font-semibold text-op-text-success">
                      Plan Included
                    </Badge>
                  )}
                </div>

                <h4 className="mt-4 text-base font-semibold text-foreground">
                  {product.title}
                </h4>
                <p className="mt-1.5 text-xs leading-relaxed text-muted-foreground">
                  {product.description}
                </p>

                <div className="mt-4 flex flex-col gap-1 rounded-md bg-op-surface-secondary/70 p-2.5 text-[11px] text-muted-foreground">
                  <div className="flex justify-between">
                    <span>Specs:</span>
                    <span className="font-medium text-foreground">{product.dimensions}</span>
                  </div>
                  <div className="flex justify-between">
                    <span>Material:</span>
                    <span className="font-medium text-foreground">{product.material}</span>
                  </div>
                </div>
              </div>

              <div className="mt-5 flex items-center justify-between border-t border-op-border-default/60 pt-4">
                <div>
                  <span className="text-xs text-muted-foreground">Price: </span>
                  <span className="text-sm font-bold text-foreground">
                    {product.price === 0 ? "Free (Included)" : `£${product.price.toFixed(2)}`}
                  </span>
                </div>

                <Button
                  type="button"
                  size="sm"
                  variant={isAdded ? "outline" : "op-primary"}
                  className={`h-8 gap-1.5 rounded-md px-3 text-xs font-medium transition-all ${
                    isAdded ? "border-op-action-primary text-op-text-success" : ""
                  }`}
                  onClick={() => handleAdd(product)}
                >
                  {isAdded ? (
                    <>
                      <Check className="size-3.5" />
                      Added
                    </>
                  ) : (
                    <>
                      <Plus className="size-3.5" />
                      Add to cart
                    </>
                  )}
                </Button>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}

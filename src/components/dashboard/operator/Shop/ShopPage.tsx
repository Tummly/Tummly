import { useEffect, useMemo, useState } from "react"
import { useSearchParams } from "react-router-dom"
import { toast } from "sonner"

import { fetchShopCatalog, fetchShopCatalogItem } from "@/api/shopCatalogApi"
import { ShopHeader } from "@/components/dashboard/operator/Shop/ShopHeader"
import { ShopToolbar } from "@/components/dashboard/operator/Shop/ShopToolbar"
import { ShopBanner } from "@/components/dashboard/operator/Shop/ShopBanner"
import { ShopRecommendationSection } from "@/components/dashboard/operator/Shop/ShopRecommendationSection"
import { ShopCatalogSection } from "@/components/dashboard/operator/Shop/ShopCatalogSection"
import { ShopCartFloatingButton } from "@/components/dashboard/operator/Shop/ShopCartFloatingButton"
import {
  ShopCartDrawer,
  type CartItem,
} from "@/components/dashboard/operator/Shop/ShopCartDrawer"
import { ShopStarterKitDialog } from "@/components/dashboard/operator/Shop/ShopStarterKitDialog"
import {
  ShopLocationDetailsDialog,
  type LocationDetails,
} from "@/components/dashboard/operator/Shop/ShopLocationDetailsDialog"
import {
  ShopOrdersDialog,
  type ShopOrder,
} from "@/components/dashboard/operator/Shop/ShopOrdersDialog"
import { ShopOrdersScreen } from "@/components/dashboard/operator/Shop/ShopOrdersScreen"
import {
  ShopProductScreen,
  scrollShopPaneToTop,
} from "@/components/dashboard/operator/Shop/ShopProductScreen"
import { ShopCheckoutScreen } from "@/components/dashboard/operator/Shop/ShopCheckoutScreen"
import { ShopCreateQrAssetDialog } from "@/components/dashboard/operator/Shop/ShopCreateQrAssetDialog"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"
import type { ShopProduct } from "@/lib/operatorShop/shopCatalogTypes"

type ShopPageProps = {
  selectedLocationId: number
  locations: Array<{ id: number; locationName: string; address: string }>
  mode: DashboardProps["mode"]
  onSelectLocation?: (locationId: number) => void
}

function findProductById(
  products: ShopProduct[],
  skuId: string | null
): ShopProduct | null {
  if (skuId == null) {
    return null
  }

  return products.find((product) => product.id === skuId) ?? null
}

export function ShopPage({
  selectedLocationId,
  locations,
  onSelectLocation,
}: ShopPageProps) {
  const [searchParams, setSearchParams] = useSearchParams()
  const productParam = searchParams.get("product")
  const viewParam = searchParams.get("view")

  const [catalogProducts, setCatalogProducts] = useState<ShopProduct[]>([])
  const [catalogLoading, setCatalogLoading] = useState(true)
  const [productDetail, setProductDetail] = useState<ShopProduct | null>(null)

  const currentView: "shop" | "orders" | "product" | "checkout" =
    viewParam === "checkout"
      ? "checkout"
      : productParam
        ? "product"
        : viewParam === "orders"
          ? "orders"
          : "shop"

  const selectedProduct = useMemo(() => {
    if (productDetail && productDetail.id === productParam) {
      return productDetail
    }

    return findProductById(catalogProducts, productParam) ?? catalogProducts[0] ?? null
  }, [catalogProducts, productDetail, productParam])

  const [searchQuery, setSearchQuery] = useState<string>("")
  const [cartItems, setCartItems] = useState<CartItem[]>([])
  const [isCartOpen, setIsCartOpen] = useState<boolean>(false)
  const [isStarterKitOpen, setIsStarterKitOpen] = useState<boolean>(false)
  const [isLocationDetailsOpen, setIsLocationDetailsOpen] = useState<boolean>(false)
  const [isOrdersOpen, setIsOrdersOpen] = useState<boolean>(false)
  const [isCreateQrAssetOpen, setIsCreateQrAssetOpen] = useState<boolean>(false)
  const [isSubmittingOrder, setIsSubmittingOrder] = useState<boolean>(false)

  const [locationDetails, setLocationDetails] = useState<LocationDetails | null>(null)

  const [orders, setOrders] = useState<ShopOrder[]>([
    {
      id: "ord-1",
      orderNumber: "ORD-9421",
      date: "12 Oct 2026",
      locationName:
        locations.find((l) => l.id === selectedLocationId)?.locationName ?? "Location",
      items: ["10x Tabletop Acrylic Stands", "4x Storefront Window Decals"],
      status: "delivered",
    },
  ])

  useEffect(() => {
    let cancelled = false

    async function loadCatalog() {
      setCatalogLoading(true)
      try {
        const { products } = await fetchShopCatalog(selectedLocationId)
        if (!cancelled) {
          setCatalogProducts(products)
        }
      } catch {
        if (!cancelled) {
          setCatalogProducts([])
          toast.error("Could not load shop catalog.")
        }
      } finally {
        if (!cancelled) {
          setCatalogLoading(false)
        }
      }
    }

    void loadCatalog()

    return () => {
      cancelled = true
    }
  }, [selectedLocationId])

  useEffect(() => {
    if (productParam == null) {
      setProductDetail(null)
      return
    }

    let cancelled = false

    async function loadProductDetail() {
      if (productParam == null) {
        return
      }

      try {
        const detail = await fetchShopCatalogItem(
          selectedLocationId,
          productParam
        )
        if (!cancelled) {
          setProductDetail(detail)
        }
      } catch {
        if (!cancelled) {
          setProductDetail(null)
          toast.error("Could not load product details.")
        }
      }
    }

    void loadProductDetail()

    return () => {
      cancelled = true
    }
  }, [productParam, selectedLocationId])

  const currentLocation =
    locations.find((l) => l.id === selectedLocationId) ?? locations[0]
  const locationName = currentLocation?.locationName ?? "Location"
  const locationAddress = currentLocation?.address ?? ""

  const totalCartCount = cartItems.reduce((acc, item) => acc + item.quantity, 0)

  const handleAddToCart = (product: ShopProduct, quantity: number = 1) => {
    setCartItems((prev) => {
      const existing = prev.find((item) => item.product.id === product.id)
      if (existing) {
        return prev.map((item) =>
          item.product.id === product.id
            ? { ...item, quantity: item.quantity + quantity }
            : item
        )
      }
      return [...prev, { product, quantity }]
    })
    toast.success(`Added ${product.title} to cart`)
  }

  const handleUpdateQuantity = (productId: string, quantity: number) => {
    setCartItems((prev) =>
      prev.map((item) =>
        item.product.id === productId ? { ...item, quantity } : item
      )
    )
  }

  const handleRemoveFromCart = (productId: string) => {
    setCartItems((prev) => prev.filter((item) => item.product.id !== productId))
    toast.info("Item removed from cart")
  }

  const handleAddStarterKitToCart = () => {
    const starterProduct = findProductById(catalogProducts, "table-tents")
    if (starterProduct) {
      handleAddToCart(starterProduct, 1)
      setIsCartOpen(true)
    }
  }

  const handleAddRecommendedKitToCart = () => {
    const tableStands = findProductById(catalogProducts, "table-tents")
    const decals = findProductById(catalogProducts, "window-stickers")
    const billCards = findProductById(catalogProducts, "counter-cards")

    if (tableStands) {
      handleAddToCart(
        tableStands,
        locationDetails?.tableCount ? Math.ceil(locationDetails.tableCount / 10) : 2
      )
    }
    if (decals) {
      handleAddToCart(
        decals,
        locationDetails?.entranceCount ? Math.ceil(locationDetails.entranceCount / 2) : 1
      )
    }
    if (billCards) {
      handleAddToCart(billCards, 1)
    }

    toast.success(`Recommended materials kit added to cart for ${locationName}`)
    setIsCartOpen(true)
  }

  const handleCheckout = () => {
    setIsCartOpen(false)
    const firstProduct = cartItems[0]?.product || selectedProduct
    if (firstProduct) {
      setSearchParams({ view: "checkout", product: firstProduct.id })
    } else {
      setSearchParams({ view: "checkout" })
    }
    scrollShopPaneToTop()
  }

  const handleSelectProduct = (product: ShopProduct) => {
    setSearchParams({ product: product.id })
    scrollShopPaneToTop()
  }

  const handleViewOrders = () => {
    setSearchParams({ view: "orders" })
    scrollShopPaneToTop()
  }

  const handleOrderNow = (product: ShopProduct, quantity: number) => {
    handleAddToCart(product, quantity)
    setSearchParams({ view: "checkout", product: product.id })
    scrollShopPaneToTop()
  }

  const handleBackToShop = () => {
    setSearchParams({})
    scrollShopPaneToTop()
  }

  return (
    <div className="flex flex-col gap-6 pb-20">
      {currentView === "checkout" ? (
        <ShopCheckoutScreen
          product={selectedProduct}
          selectedLocationName={locationName}
          selectedLocationAddress={locationAddress}
          locations={locations}
          onSelectLocation={onSelectLocation}
          onBackToShop={handleBackToShop}
          onBackToProduct={() => {
            if (selectedProduct) {
              setSearchParams({ product: selectedProduct.id })
            } else {
              setSearchParams({})
            }
            scrollShopPaneToTop()
          }}
          onOrderPlaced={(_newOrder) => {
            setCartItems([])
            setSearchParams({ view: "orders" })
            scrollShopPaneToTop()
          }}
          onSaveDraft={(_draft) => {
            toast.success("Draft saved successfully")
            setSearchParams({ view: "orders" })
            scrollShopPaneToTop()
          }}
        />
      ) : currentView === "orders" ? (
        <ShopOrdersScreen
          selectedLocationName={locationName}
          locations={locations}
          onSelectLocation={onSelectLocation}
          onBackToShop={handleBackToShop}
          onContinueCheckoutDraft={(draft) => {
            setSearchParams({ view: "checkout" })
            scrollShopPaneToTop()
            toast.info(`Resuming checkout for ${draft.draftNumber}`)
          }}
          onReorder={(order) => {
            setSearchParams({ view: "checkout" })
            scrollShopPaneToTop()
            toast.success(`Reviewing reorder for ${order.orderNumber}`)
          }}
        />
      ) : currentView === "product" && selectedProduct ? (
        <ShopProductScreen
          product={selectedProduct}
          catalogProducts={catalogProducts}
          selectedLocationName={locationName}
          locations={locations}
          onSelectLocation={onSelectLocation}
          onBackToShop={handleBackToShop}
          onAddToCart={handleAddToCart}
          onOrderNow={handleOrderNow}
          onSelectRelatedProduct={handleSelectProduct}
        />
      ) : (
        <>
          <ShopHeader
            selectedLocationName={locationName}
            locations={locations}
            onSelectLocation={onSelectLocation}
          />

          <ShopToolbar
            searchQuery={searchQuery}
            onSearchQueryChange={setSearchQuery}
            onCreateQrAsset={() => setIsCreateQrAssetOpen(true)}
            onViewOrders={handleViewOrders}
          />

          <ShopBanner
            onReviewStarterKit={() => setIsStarterKitOpen(true)}
            onSeeWhatsIncluded={() => setIsStarterKitOpen(true)}
          />

          <ShopRecommendationSection
            locationName={locationName}
            locationDetails={locationDetails}
            catalogProducts={catalogProducts}
            onAddLocationDetails={() => setIsLocationDetailsOpen(true)}
            onAddRecommendedToCart={handleAddRecommendedKitToCart}
            onSelectProduct={handleSelectProduct}
          />

          {catalogLoading ? (
            <p className="text-sm text-muted-foreground">Loading catalog…</p>
          ) : (
            <ShopCatalogSection
              products={catalogProducts}
              searchQuery={searchQuery}
              onAddToCart={handleAddToCart}
              onSelectProduct={handleSelectProduct}
            />
          )}
        </>
      )}

      <ShopCartFloatingButton
        itemCount={totalCartCount}
        onClick={() => setIsCartOpen(true)}
      />

      <ShopCartDrawer
        open={isCartOpen}
        onOpenChange={setIsCartOpen}
        items={cartItems}
        onUpdateQuantity={handleUpdateQuantity}
        onRemoveItem={handleRemoveFromCart}
        onClearCart={() => setCartItems([])}
        onCheckout={handleCheckout}
        selectedLocationName={locationName}
        selectedLocationAddress={locationAddress}
        isSubmitting={isSubmittingOrder}
      />

      <ShopStarterKitDialog
        open={isStarterKitOpen}
        onOpenChange={setIsStarterKitOpen}
        onAddKitToCart={handleAddStarterKitToCart}
        selectedLocationName={locationName}
      />

      <ShopLocationDetailsDialog
        open={isLocationDetailsOpen}
        onOpenChange={setIsLocationDetailsOpen}
        onSaveDetails={setLocationDetails}
        initialDetails={locationDetails}
        locationName={locationName}
      />

      <ShopOrdersDialog
        open={isOrdersOpen}
        onOpenChange={setIsOrdersOpen}
        orders={orders}
      />

      <ShopCreateQrAssetDialog
        open={isCreateQrAssetOpen}
        onOpenChange={setIsCreateQrAssetOpen}
        locationName={locationName}
      />
    </div>
  )
}

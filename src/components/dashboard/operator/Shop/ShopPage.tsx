import { useEffect, useMemo, useState } from "react"
import { useSearchParams } from "react-router-dom"
import { toast } from "sonner"

import { fetchShopCatalog, fetchShopCatalogItem } from "@/api/shopCatalogApi"
import {
  deleteShopCartLine,
  fetchShopCart,
  mapShopCartToItems,
  upsertShopCartLine,
} from "@/api/shopCartApi"
import type { CheckoutLine } from "@/api/shopOrdersApi"
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
import {
  findShopProductById,
  type ShopProduct,
} from "@/lib/operatorShop/shopCatalogTypes"

type ShopPageProps = {
  selectedLocationId: number
  locations: Array<{ id: number; locationName: string; address: string }>
  mode: DashboardProps["mode"]
  onSelectLocation?: (locationId: number) => void
}

type ExpressCheckoutState = {
  lines: CheckoutLine[]
}

function toCheckoutLine(product: ShopProduct, quantity: number): CheckoutLine {
  const unitNetPence =
    product.unitNetPence ?? Math.round(product.price * 100)
  return {
    skuId: product.id,
    title: product.title,
    quantity,
    unitNetPence,
    lineNetPence: unitNetPence * quantity,
    specification: `${product.material} · ${product.dimensions}`,
  }
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

    if (productParam != null) {
      const fromCatalog = findShopProductById(catalogProducts, productParam)
      if (fromCatalog) {
        return fromCatalog
      }
    }

    return catalogProducts[0] ?? null
  }, [catalogProducts, productDetail, productParam])

  const [searchQuery, setSearchQuery] = useState<string>("")
  const [cartItems, setCartItems] = useState<CartItem[]>([])
  const [isCartOpen, setIsCartOpen] = useState<boolean>(false)
  const [isStarterKitOpen, setIsStarterKitOpen] = useState<boolean>(false)
  const [isLocationDetailsOpen, setIsLocationDetailsOpen] = useState<boolean>(false)
  const [isOrdersOpen, setIsOrdersOpen] = useState<boolean>(false)
  const [isCreateQrAssetOpen, setIsCreateQrAssetOpen] = useState<boolean>(false)
  const [isSubmittingOrder] = useState<boolean>(false)
  const [checkoutFromCart, setCheckoutFromCart] = useState(false)
  const [expressCheckout, setExpressCheckout] =
    useState<ExpressCheckoutState | null>(null)

  const [locationDetails, setLocationDetails] = useState<LocationDetails | null>(null)

  const [orders] = useState<ShopOrder[]>([
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
    let cancelled = false

    async function loadCart() {
      try {
        const cart = await fetchShopCart(selectedLocationId)
        if (!cancelled) {
          setCartItems(mapShopCartToItems(cart, catalogProducts))
        }
      } catch {
        if (!cancelled) {
          setCartItems([])
          toast.error("Could not load shop cart.")
        }
      }
    }

    void loadCart()

    return () => {
      cancelled = true
    }
  }, [selectedLocationId, catalogProducts])

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

  const applyCartResponse = (cart: Awaited<ReturnType<typeof fetchShopCart>>) => {
    setCartItems(mapShopCartToItems(cart, catalogProducts))
  }

  const putAbsoluteQuantity = async (
    skuId: string,
    quantity: number
  ): Promise<Awaited<ReturnType<typeof fetchShopCart>>> => {
    const cart = await upsertShopCartLine(
      selectedLocationId,
      skuId,
      quantity
    )
    applyCartResponse(cart)
    return cart
  }

  const handleAddToCart = async (
    product: ShopProduct,
    quantity: number = 1
  ) => {
    const existing = cartItems.find((item) => item.product.id === product.id)
    const nextQuantity = (existing?.quantity ?? 0) + quantity
    try {
      await putAbsoluteQuantity(product.id, nextQuantity)
      toast.success(`Added ${product.title} to cart`)
    } catch {
      toast.error("Could not update cart.")
    }
  }

  const handleUpdateQuantity = async (productId: string, quantity: number) => {
    try {
      await putAbsoluteQuantity(productId, quantity)
    } catch {
      toast.error("Could not update cart.")
    }
  }

  const handleRemoveFromCart = async (productId: string) => {
    try {
      const cart = await deleteShopCartLine(selectedLocationId, productId)
      applyCartResponse(cart)
      toast.info("Item removed from cart")
    } catch {
      toast.error("Could not update cart.")
    }
  }

  const handleClearCart = async () => {
    try {
      let cart = await fetchShopCart(selectedLocationId)
      for (const line of cart.lines) {
        cart = await deleteShopCartLine(selectedLocationId, line.skuId)
      }
      applyCartResponse(cart)
    } catch {
      toast.error("Could not clear cart.")
    }
  }

  const handleAddStarterKitToCart = () => {
    const starterProduct = findShopProductById(catalogProducts, "table-tents")
    if (starterProduct) {
      void handleAddToCart(starterProduct, 1).then(() => {
        setIsCartOpen(true)
      })
    }
  }

  const handleAddRecommendedKitToCart = async () => {
    const additions: Array<{ product: ShopProduct; quantity: number }> = []
    const tableStands = findShopProductById(catalogProducts, "table-tents")
    const decals = findShopProductById(catalogProducts, "window-stickers")
    const billCards = findShopProductById(catalogProducts, "counter-cards")

    if (tableStands) {
      additions.push({
        product: tableStands,
        quantity: locationDetails?.tableCount
          ? Math.ceil(locationDetails.tableCount / 10)
          : 2,
      })
    }
    if (decals) {
      additions.push({
        product: decals,
        quantity: locationDetails?.entranceCount
          ? Math.ceil(locationDetails.entranceCount / 2)
          : 1,
      })
    }
    if (billCards) {
      additions.push({ product: billCards, quantity: 1 })
    }

    try {
      let working = cartItems
      for (const addition of additions) {
        const existing = working.find(
          (item) => item.product.id === addition.product.id
        )
        const nextQuantity = (existing?.quantity ?? 0) + addition.quantity
        const cart = await putAbsoluteQuantity(
          addition.product.id,
          nextQuantity
        )
        working = mapShopCartToItems(cart, catalogProducts)
      }

      toast.success(`Recommended materials kit added to cart for ${locationName}`)
      setIsCartOpen(true)
    } catch {
      toast.error("Could not add recommended kit.")
    }
  }

  const checkoutLines: CheckoutLine[] = useMemo(
    () =>
      checkoutFromCart
        ? cartItems.map((item) =>
            toCheckoutLine(item.product, item.quantity)
          )
        : (expressCheckout?.lines ?? []),
    [cartItems, checkoutFromCart, expressCheckout]
  )

  const handleCheckout = () => {
    if (cartItems.length === 0) {
      toast.error("Your cart is empty.")
      return
    }
    setIsCartOpen(false)
    setCheckoutFromCart(true)
    setExpressCheckout(null)
    setSearchParams({ view: "checkout" })
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
    // Express path (lock 05): do not merge into the server cart.
    setCheckoutFromCart(false)
    setExpressCheckout({
      lines: [toCheckoutLine(product, quantity)],
    })
    setSearchParams({ view: "checkout" })
    scrollShopPaneToTop()
  }

  const handleBackToShop = () => {
    setCheckoutFromCart(false)
    setExpressCheckout(null)
    setSearchParams({})
    scrollShopPaneToTop()
  }

  const clearCheckoutSession = () => {
    setCheckoutFromCart(false)
    setExpressCheckout(null)
  }

  return (
    <div className="flex flex-col gap-6 pb-20">
      {currentView === "checkout" && checkoutLines.length > 0 ? (
        <ShopCheckoutScreen
          locationId={selectedLocationId}
          lines={checkoutLines}
          fromCart={checkoutFromCart}
          selectedLocationName={locationName}
          selectedLocationAddress={locationAddress}
          locations={locations}
          onSelectLocation={onSelectLocation}
          onBackToShop={handleBackToShop}
          onBackToProduct={() => {
            const wasFromCart = checkoutFromCart
            const expressSkuId = expressCheckout?.lines[0]?.skuId
            clearCheckoutSession()
            if (!wasFromCart && expressSkuId) {
              setSearchParams({ product: expressSkuId })
            } else {
              setSearchParams({})
            }
            scrollShopPaneToTop()
          }}
          onOrderPlaced={(_orderNumber) => {
            void (async () => {
              if (checkoutFromCart) {
                try {
                  const cart = await fetchShopCart(selectedLocationId)
                  applyCartResponse(cart)
                } catch {
                  toast.error("Could not refresh cart after order.")
                }
              }
              clearCheckoutSession()
              setSearchParams({ view: "orders" })
              scrollShopPaneToTop()
            })()
          }}
          onSaveDraft={(_draft) => {
            toast.success("Draft saved successfully")
            clearCheckoutSession()
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
            onAddRecommendedToCart={() => {
              void handleAddRecommendedKitToCart()
            }}
            onSelectProduct={handleSelectProduct}
          />

          {catalogLoading ? (
            <p className="text-sm text-muted-foreground">Loading catalog…</p>
          ) : (
            <ShopCatalogSection
              products={catalogProducts}
              searchQuery={searchQuery}
              onAddToCart={(product) => {
                void handleAddToCart(product)
              }}
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
        onUpdateQuantity={(productId, quantity) => {
          void handleUpdateQuantity(productId, quantity)
        }}
        onRemoveItem={(productId) => {
          void handleRemoveFromCart(productId)
        }}
        onClearCart={() => {
          void handleClearCart()
        }}
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

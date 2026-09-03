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
import {
  fetchShopLocationRecommendations,
  saveShopLocationDetails,
  type ShopLocationRecommendations,
} from "@/api/shopRecommendationsApi"
import type { CheckoutLine, ShopShipToPayload } from "@/api/shopOrdersApi"
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
import {
  ShopMaterialsPackDialog,
  SHOP_MATERIALS_PACK_LINES,
} from "@/components/dashboard/operator/Shop/ShopStarterKitDialog"
import {
  ShopLocationDetailsDialog,
  type LocationDetails,
} from "@/components/dashboard/operator/Shop/ShopLocationDetailsDialog"
import { ShopOrdersScreen } from "@/components/dashboard/operator/Shop/ShopOrdersScreen"
import {
  ShopProductScreen,
  scrollShopPaneToTop,
} from "@/components/dashboard/operator/Shop/ShopProductScreen"
import { ShopCheckoutScreen } from "@/components/dashboard/operator/Shop/ShopCheckoutScreen"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"
import {
  findShopProductById,
  type ShopProduct,
} from "@/lib/operatorShop/shopCatalogTypes"
import {
  pollShopOrderUntilPaid,
} from "@/api/shopOrdersApi"
import type { ShopPaidWriteChrome } from "@/lib/operatorShop/shopPaidWriteChrome"
import type { ShopLocationOption } from "@/components/dashboard/operator/Shop/ShopLocationPicker"

type ShopPageProps = {
  selectedLocationId: number
  locations: ShopLocationOption[]
  brandLogoPublicUrl: string | null
  mode: DashboardProps["mode"]
  onSelectLocation?: (locationId: number) => void
  paidWriteChrome: ShopPaidWriteChrome
}

type ExpressCheckoutState = {
  lines: CheckoutLine[]
  shipTo?: ShopShipToPayload
  deliveryMethod?: "standard" | "express"
  sourceOrderNumber?: string
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
  brandLogoPublicUrl,
  mode,
  onSelectLocation,
  paidWriteChrome,
}: ShopPageProps) {
  const [searchParams, setSearchParams] = useSearchParams()
  const productParam = searchParams.get("product")
  const viewParam = searchParams.get("view")
  const shopPayOutcome = searchParams.get("shopPayOutcome")
  const shopOrderId = searchParams.get("shopOrderId")

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
  const [isMaterialsPackOpen, setIsMaterialsPackOpen] = useState<boolean>(false)
  const [isLocationDetailsOpen, setIsLocationDetailsOpen] = useState<boolean>(false)
  const [checkoutFromCart, setCheckoutFromCart] = useState(false)
  const [expressCheckout, setExpressCheckout] =
    useState<ExpressCheckoutState | null>(null)

  const [locationDetails, setLocationDetails] = useState<LocationDetails | null>(null)
  const [recommendations, setRecommendations] =
    useState<ShopLocationRecommendations | null>(null)
  const [recommendationsLoading, setRecommendationsLoading] = useState(true)

  const mapBasedOnToLocationDetails = (
    basedOn: NonNullable<ShopLocationRecommendations["basedOn"]>
  ): LocationDetails => ({
    tableCount: basedOn.tableCount,
    counterCount: basedOn.counterCount,
    entranceCount: basedOn.entranceCount,
    secondaryEntranceCount: basedOn.secondaryEntranceCount,
    takeawayVolume: basedOn.takeawayVolume,
    promptLocations: basedOn.promptLocations.join(","),
    existingMaterials: basedOn.existingMaterials,
  })

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
    let cancelled = false

    async function loadRecommendations() {
      setRecommendationsLoading(true)
      try {
        const payload = await fetchShopLocationRecommendations(
          selectedLocationId
        )
        if (!cancelled) {
          applyRecommendationsPayload(payload)
        }
      } catch {
        if (!cancelled) {
          setRecommendations(null)
          setLocationDetails(null)
          toast.error("Could not load recommendations.")
        }
      } finally {
        if (!cancelled) {
          setRecommendationsLoading(false)
        }
      }
    }

    void loadRecommendations()

    return () => {
      cancelled = true
    }
  }, [selectedLocationId])

  function applyRecommendationsPayload(payload: ShopLocationRecommendations) {
    setRecommendations(payload)
    if (payload.basedOn) {
      setLocationDetails(mapBasedOnToLocationDetails(payload.basedOn))
    } else {
      setLocationDetails(null)
    }
  }

  const refetchRecommendations = async () => {
    setRecommendationsLoading(true)
    try {
      const payload = await fetchShopLocationRecommendations(
        selectedLocationId
      )
      applyRecommendationsPayload(payload)
    } catch {
      toast.error("Could not load recommendations.")
    } finally {
      setRecommendationsLoading(false)
    }
  }

  const handleSaveLocationDetails = async (details: LocationDetails) => {
    await saveShopLocationDetails(selectedLocationId, {
      tableCount: details.tableCount,
      counterCount: details.counterCount ?? 0,
      entranceCount: details.entranceCount ?? 0,
      secondaryEntranceCount: details.secondaryEntranceCount ?? 0,
      takeawayVolume: details.takeawayVolume ?? "not-sure",
      promptLocations: details.promptLocations ?? "",
      existingMaterials: details.existingMaterials ?? "no",
    })
    setLocationDetails(details)
    await refetchRecommendations()
    toast.success("Location details saved.")
  }

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

  useEffect(() => {
    if (shopPayOutcome !== "success" || shopOrderId == null) {
      return
    }

    let cancelled = false

    void (async () => {
      toast.message("Confirming your Revolut payment…")
      const paidOrder = await pollShopOrderUntilPaid({
        orderId: shopOrderId,
        locationId: selectedLocationId,
      })
      if (cancelled) {
        return
      }

      const nextParams = new URLSearchParams()
      nextParams.set("view", "orders")
      setSearchParams(nextParams)

      if (paidOrder) {
        toast.success(`Order ${paidOrder.orderNumber} is paid.`)
      } else {
        toast.info(
          "Payment is still processing. Check Orders for the latest status."
        )
      }
      scrollShopPaneToTop()
    })()

    return () => {
      cancelled = true
    }
  }, [shopOrderId, shopPayOutcome, selectedLocationId, setSearchParams])

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
    if (paidWriteChrome.purchaseDisabled) {
      return
    }

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
    if (paidWriteChrome.purchaseDisabled) {
      return
    }

    try {
      await putAbsoluteQuantity(productId, quantity)
    } catch {
      toast.error("Could not update cart.")
    }
  }

  const handleRemoveFromCart = async (productId: string) => {
    if (paidWriteChrome.purchaseDisabled) {
      return
    }

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

  const handleAddMaterialsPackToCart = async () => {
    if (paidWriteChrome.purchaseDisabled) {
      return
    }

    try {
      for (const line of SHOP_MATERIALS_PACK_LINES) {
        if (findShopProductById(catalogProducts, line.skuId) == null) {
          continue
        }
        await putAbsoluteQuantity(line.skuId, line.quantity)
      }
      toast.success(`Materials pack added to cart for ${locationName}`)
      setIsCartOpen(true)
    } catch {
      toast.error("Could not add materials pack.")
    }
  }

  const handleAddRecommendedKitToCart = async () => {
    if (paidWriteChrome.purchaseDisabled) {
      return
    }

    const lines = recommendations?.lines ?? []
    if (lines.length === 0) {
      toast.error("No recommended items to add.")
      return
    }

    try {
      for (const line of lines) {
        await putAbsoluteQuantity(line.skuId, line.quantity)
      }

      toast.success(`Recommended materials kit added to cart for ${locationName}`)
      setIsCartOpen(true)
    } catch {
      toast.error("Could not add recommended kit.")
    }
  }

  const handleOrderRecommendedLine = async (
    skuId: string,
    quantity: number
  ) => {
    if (paidWriteChrome.purchaseDisabled) {
      return
    }

    try {
      await putAbsoluteQuantity(skuId, quantity)
      toast.success("Item added to cart")
      setIsCartOpen(true)
    } catch {
      toast.error("Could not add item to cart.")
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
    if (paidWriteChrome.purchaseDisabled) {
      return
    }

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
    if (paidWriteChrome.purchaseDisabled) {
      return
    }

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
    <div className="flex flex-col gap-6">
      {currentView === "checkout" && checkoutLines.length > 0 ? (
        <ShopCheckoutScreen
          locationId={selectedLocationId}
          lines={checkoutLines}
          fromCart={checkoutFromCart}
          initialShipTo={expressCheckout?.shipTo}
          initialDeliveryMethod={expressCheckout?.deliveryMethod}
          selectedLocationName={locationName}
          selectedLocationAddress={locationAddress}
          locations={locations}
          brandLogoPublicUrl={brandLogoPublicUrl}
          mode={mode}
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
          paidWriteChrome={paidWriteChrome}
        />
      ) : currentView === "orders" ? (
        <ShopOrdersScreen
          selectedLocationId={selectedLocationId}
          selectedLocationName={locationName}
          locations={locations}
          brandLogoPublicUrl={brandLogoPublicUrl}
          mode={mode}
          onSelectLocation={onSelectLocation}
          onBackToShop={handleBackToShop}
          onReorder={({ prefill }) => {
            setCheckoutFromCart(false)
            setExpressCheckout({
              lines: prefill.lines.map((line) => ({
                skuId: line.skuId,
                title: line.title,
                quantity: line.quantity,
                unitNetPence: line.unitNetPence,
                lineNetPence: line.lineNetPence,
              })),
              shipTo: prefill.shipTo,
              deliveryMethod: prefill.deliveryMethod,
              sourceOrderNumber: prefill.sourceOrderNumber,
            })
            setSearchParams({ view: "checkout" })
            scrollShopPaneToTop()
          }}
        />
      ) : currentView === "product" && selectedProduct ? (
        <ShopProductScreen
          product={selectedProduct}
          catalogProducts={catalogProducts}
          selectedLocationId={selectedLocationId}
          selectedLocationName={locationName}
          locations={locations}
          brandLogoPublicUrl={brandLogoPublicUrl}
          mode={mode}
          onSelectLocation={onSelectLocation}
          onBackToShop={handleBackToShop}
          onAddToCart={handleAddToCart}
          onOrderNow={handleOrderNow}
          onSelectRelatedProduct={handleSelectProduct}
          paidWriteChrome={paidWriteChrome}
        />
      ) : (
        <>
          <ShopHeader
            selectedLocationId={selectedLocationId}
            selectedLocationName={locationName}
            locations={locations}
            brandLogoPublicUrl={brandLogoPublicUrl}
            mode={mode}
            onSelectLocation={onSelectLocation}
          />

          <ShopToolbar
            searchQuery={searchQuery}
            onSearchQueryChange={setSearchQuery}
            onViewOrders={handleViewOrders}
          />

          <ShopBanner
            onReviewMaterialsPack={() => setIsMaterialsPackOpen(true)}
            onSeeWhatsIncluded={() => setIsMaterialsPackOpen(true)}
          />

          <ShopRecommendationSection
            locationName={locationName}
            recommendations={recommendations}
            recommendationsLoading={recommendationsLoading}
            catalogProducts={catalogProducts}
            paidWriteChrome={paidWriteChrome}
            onAddLocationDetails={() => setIsLocationDetailsOpen(true)}
            onAddRecommendedToCart={() => {
              void handleAddRecommendedKitToCart()
            }}
            onOrderRecommendedLine={(skuId, quantity) => {
              void handleOrderRecommendedLine(skuId, quantity)
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
        paidWriteChrome={paidWriteChrome}
      />

      <ShopMaterialsPackDialog
        open={isMaterialsPackOpen}
        onOpenChange={setIsMaterialsPackOpen}
        onAddPackToCart={() => {
          void handleAddMaterialsPackToCart()
        }}
        selectedLocationName={locationName}
        purchaseDisabled={paidWriteChrome.purchaseDisabled}
      />

      <ShopLocationDetailsDialog
        open={isLocationDetailsOpen}
        onOpenChange={setIsLocationDetailsOpen}
        onSaveDetails={handleSaveLocationDetails}
        initialDetails={locationDetails}
        locationName={locationName}
      />
    </div>
  )
}

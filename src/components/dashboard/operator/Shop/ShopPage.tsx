import { useState } from "react"
import { toast } from "sonner"

import { ShopHeader } from "@/components/dashboard/operator/Shop/ShopHeader"
import { ShopToolbar } from "@/components/dashboard/operator/Shop/ShopToolbar"
import { ShopBanner } from "@/components/dashboard/operator/Shop/ShopBanner"
import { ShopRecommendationSection } from "@/components/dashboard/operator/Shop/ShopRecommendationSection"
import {
  ShopCatalogSection,
  type ShopProduct,
  SHOP_CATALOG_PRODUCTS,
} from "@/components/dashboard/operator/Shop/ShopCatalogSection"
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
import { ShopCreateQrAssetDialog } from "@/components/dashboard/operator/Shop/ShopCreateQrAssetDialog"
import type { DashboardProps } from "@/components/dashboard/operator/Dashboard"

type ShopPageProps = {
  selectedLocationId: number
  locations: Array<{ id: number; locationName: string; address: string }>
  mode: DashboardProps["mode"]
  onSelectLocation?: (locationId: number) => void
}

export function ShopPage({
  selectedLocationId,
  locations,
  onSelectLocation,
}: ShopPageProps) {
  const [currentView, setCurrentView] = useState<"shop" | "orders">("shop")
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
    const starterProduct =
      SHOP_CATALOG_PRODUCTS.find((p) => p.id === "table-tents") ??
      SHOP_CATALOG_PRODUCTS[0]
    if (starterProduct) {
      handleAddToCart(starterProduct, 1)
      setIsCartOpen(true)
    }
  }

  const handleAddRecommendedKitToCart = () => {
    const tableStands =
      SHOP_CATALOG_PRODUCTS.find((p) => p.id === "table-tents") ??
      SHOP_CATALOG_PRODUCTS[0]
    const decals =
      SHOP_CATALOG_PRODUCTS.find((p) => p.id === "window-stickers") ??
      SHOP_CATALOG_PRODUCTS[3]
    const billCards =
      SHOP_CATALOG_PRODUCTS.find((p) => p.id === "counter-cards-1") ??
      SHOP_CATALOG_PRODUCTS[1]

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
    setIsSubmittingOrder(true)
    setTimeout(() => {
      const newOrder: ShopOrder = {
        id: `ord-${Date.now()}`,
        orderNumber: `ORD-${Math.floor(1000 + Math.random() * 9000)}`,
        date: "Just now",
        locationName,
        items: cartItems.map((item) => `${item.quantity}x ${item.product.title}`),
        status: "processing",
      }

      setOrders((prev) => [newOrder, ...prev])
      setCartItems([])
      setIsSubmittingOrder(false)
      setIsCartOpen(false)
      toast.success("Order placed successfully! Tracking updates sent to your email.")
    }, 1200)
  }

  return (
    <div className="flex flex-col gap-6 pb-20">
      {currentView === "orders" ? (
        <ShopOrdersScreen
          selectedLocationName={locationName}
          locations={locations}
          onSelectLocation={onSelectLocation}
          onBackToShop={() => setCurrentView("shop")}
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
            onViewOrders={() => setCurrentView("orders")}
          />

          <ShopBanner
            onReviewStarterKit={() => setIsStarterKitOpen(true)}
            onSeeWhatsIncluded={() => setIsStarterKitOpen(true)}
          />

          <ShopRecommendationSection
            locationName={locationName}
            locationDetails={locationDetails}
            onAddLocationDetails={() => setIsLocationDetailsOpen(true)}
            onAddRecommendedToCart={handleAddRecommendedKitToCart}
          />

          <ShopCatalogSection
            searchQuery={searchQuery}
            onAddToCart={handleAddToCart}
          />
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

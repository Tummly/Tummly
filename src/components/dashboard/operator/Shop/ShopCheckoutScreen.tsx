import { useState } from "react"
import {
  ChevronRight,
  Package,
  CreditCard,
  X,
  Check,
  ChevronDown,
} from "lucide-react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Checkbox } from "@/components/ui/checkbox"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"
import type { ShopProduct } from "@/lib/operatorShop/shopCatalogTypes"
import type { DetailedShopOrder, DetailedShopDraft } from "@/components/dashboard/operator/Shop/ShopOrdersScreen"
import { scrollShopPaneToTop } from "@/components/dashboard/operator/Shop/ShopProductScreen"
import { cn } from "@/lib/utils"

type DeliveryMethod = "standard" | "express"
type CheckoutStep = "delivery" | "payment" | "add-address"

type ShopCheckoutScreenProps = {
  product?: ShopProduct | null
  selectedLocationName: string
  selectedLocationAddress?: string
  locations: Array<{ id: number; locationName: string; address: string }>
  onSelectLocation?: (locationId: number) => void
  onBackToShop: () => void
  onBackToProduct?: () => void
  onOrderPlaced?: (newOrder: DetailedShopOrder) => void
  onSaveDraft?: (draft: DetailedShopDraft) => void
}

export function ShopCheckoutScreen({
  product,
  selectedLocationName,
  selectedLocationAddress = "6 Southwark Street, London SE1 1TQ, United Kingdom",
  locations,
  onSelectLocation,
  onBackToShop,
  onBackToProduct,
  onOrderPlaced,
  onSaveDraft,
}: ShopCheckoutScreenProps) {
  const [checkoutStep, setCheckoutStep] = useState<CheckoutStep>("delivery")
  const [quantity, setQuantity] = useState<number>(20)
  const [deliveryMethod, setDeliveryMethod] = useState<DeliveryMethod>("standard")
  const [contactName, setContactName] = useState("Mohamed Mahmoud")
  const [contactPhone, setContactPhone] = useState("+44 20 7407 1234")
  const [deliveryAddress, setDeliveryAddress] = useState(
    selectedLocationAddress || "6 Southwark Street, London SE1 1TQ, United Kingdom"
  )
  const [deliveryInstructions, setDeliveryInstructions] = useState(
    "Please deliver through the restaurants side entrance and ask for the duty manager."
  )

  // Add delivery address form states
  const [addressContactName, setAddressContactName] = useState("Mohamed Mahmoud")
  const [addressContactPhone, setAddressContactPhone] = useState("+44 20 7407 1234")
  const [addressBusinessName, setAddressBusinessName] = useState("Padella Borough")
  const [addressLocationName, setAddressLocationName] = useState("Borough Market")
  const [addressPostcode, setAddressPostcode] = useState("SE1 1TQ")
  const [addressCountry, setAddressCountry] = useState("United Kingdom")
  const [addressLine1, setAddressLine1] = useState("6 Southwark Street")
  const [addressLine2, setAddressLine2] = useState("London")
  const [saveAddressForFuture, setSaveAddressForFuture] = useState(true)

  // Payment details
  const [cardType, setCardType] = useState("Visa")
  const [cardLast4, setCardLast4] = useState("4242")
  const [cardExpiry, setCardExpiry] = useState("08/29")

  // Checkboxes for payment step
  const [checkedReviewDetails, setCheckedReviewDetails] = useState(false)
  const [checkedAgreeTerms, setCheckedAgreeTerms] = useState(false)

  // Dialog states
  const [isEditQuantityOpen, setIsEditQuantityOpen] = useState(false)
  const [isEditPaymentOpen, setIsEditPaymentOpen] = useState(false)
  const [isPaymentProcessing, setIsPaymentProcessing] = useState(false)

  const productTitle = product?.title || "Table tents"
  const productSpecification =
    product?.category === "stickers"
      ? "Glossy Vinyl · Weatherproof · Permanent adhesive"
      : "A5 folded card · Double-sided · Matte finish"

  const basePrice = product?.price || 69.0
  const deliveryPrice = deliveryMethod === "express" ? 20.0 : 0.0
  const subtotal = basePrice * (quantity / 20)
  const vat = subtotal * 0.2
  const total = subtotal + vat + deliveryPrice

  const [returnStepAfterAddress, setReturnStepAfterAddress] = useState<"delivery" | "payment">("payment")

  const handleContinueToPayment = () => {
    setCheckoutStep("payment")
    scrollShopPaneToTop()
  }

  const handleBackToDelivery = () => {
    setCheckoutStep("delivery")
    scrollShopPaneToTop()
  }

  const handleOpenAddAddress = (returnTo: "delivery" | "payment" = "payment") => {
    setReturnStepAfterAddress(returnTo)
    setCheckoutStep("add-address")
    scrollShopPaneToTop()
  }

  const handleSaveAndUseAddress = () => {
    setContactName(addressContactName)
    setContactPhone(addressContactPhone)
    const formattedAddress = [
      addressLine1,
      addressLine2,
      addressPostcode,
      addressCountry,
    ]
      .filter(Boolean)
      .join(", ")
    setDeliveryAddress(formattedAddress)
    toast.success("Delivery address addded")
    setCheckoutStep(returnStepAfterAddress)
    scrollShopPaneToTop()
  }

  const handleCancelAddAddress = () => {
    setCheckoutStep(returnStepAfterAddress)
    scrollShopPaneToTop()
  }

  const handlePlaceOrder = () => {
    if (!checkedReviewDetails || !checkedAgreeTerms) {
      toast.error("Please confirm the checkout checkboxes to proceed")
      return
    }

    setIsPaymentProcessing(true)

    setTimeout(() => {
      setIsPaymentProcessing(false)
      const orderNum = `#TM-${Math.floor(10400 + Math.random() * 90)}`
      const newOrder: DetailedShopOrder = {
        id: `ord-${Date.now()}`,
        orderNumber: orderNum,
        orderDate: new Date().toLocaleDateString("en-GB", {
          day: "2-digit",
          month: "short",
          year: "numeric",
        }),
        isoDate: new Date().toISOString().split("T")[0],
        locationName: selectedLocationName,
        materials: `${productTitle} · Pack of ${quantity}`,
        materialTypes: ["table-tents"],
        placedBy: contactName,
        total: `£${total.toFixed(2)}`,
        totalNumeric: total,
        paymentStatus: "Paid",
        fulfilmentStatus: "In production",
        updatedDate: new Date().toLocaleDateString("en-GB", {
          day: "2-digit",
          month: "short",
          year: "numeric",
        }),
        items: [`${quantity}x ${productTitle} (${productSpecification})`],
      }

      toast.success(`Order ${orderNum} placed successfully!`)
      if (onOrderPlaced) {
        onOrderPlaced(newOrder)
      } else {
        onBackToShop()
      }
    }, 1000)
  }

  const handleSaveDraft = () => {
    const draftNum = `#TM-${Math.floor(10400 + Math.random() * 90)}`
    const newDraft: DetailedShopDraft = {
      id: `draft-${Date.now()}`,
      draftNumber: draftNum,
      draftDate: new Date().toLocaleDateString("en-GB", {
        day: "2-digit",
        month: "short",
        year: "numeric",
      }),
      isoDate: new Date().toISOString().split("T")[0],
      locationName: selectedLocationName,
      materials: `${productTitle} · Pack of ${quantity}`,
      materialTypes: ["table-tents"],
      lastCompletedStep:
        checkoutStep === "payment" ? "Payment details" : "Delivery details",
      lastUpdatedDate: `Updated ${new Date().toLocaleDateString("en-GB", {
        day: "2-digit",
        month: "short",
        year: "numeric",
      })}`,
      items: [`${quantity}x ${productTitle} (${productSpecification})`],
    }

    if (onSaveDraft) {
      onSaveDraft(newDraft)
    } else {
      toast.success("Draft saved successfully")
      onBackToShop()
    }
  }

  return (
    <div className="flex flex-col gap-6 pb-20">
      {/* ========================================================================= */}
      {/* ADD DELIVERY ADDRESS SCREEN                                               */}
      {/* ========================================================================= */}
      {checkoutStep === "add-address" ? (
        <div className="flex flex-col gap-6">
          {/* Top Bar with Logo & Close Button */}
          <div className="flex items-center justify-between border-b border-op-border-default pb-4">
            <div className="flex items-center">
              <span className="text-2xl font-extrabold tracking-tight text-white">
                tummly<span className="text-op-action-primary">.</span>
              </span>
            </div>

            <Button
              type="button"
              variant="ghost"
              size="icon"
              onClick={handleCancelAddAddress}
              className="size-9 rounded-xs bg-op-surface-secondary text-op-text-muted hover:bg-op-surface-secondary/80 hover:text-op-text-primary"
              aria-label="Close address form"
            >
              <X className="size-4" />
            </Button>
          </div>

          {/* Main Container */}
          <div className="mx-auto flex w-full max-w-[824px] flex-col items-start gap-10 pt-8 pb-24 text-op-text-primary">
            {/* Title & Subtitle */}
            <div className="flex flex-col gap-2">
              <h1 className="text-3xl font-bold tracking-tight text-op-text-primary">
                Add delivery address
              </h1>
              <p className="text-sm font-medium text-op-text-muted leading-5">
                Add the address and contact details Tummly should use for this delivery.
              </p>
            </div>

            <div className="flex w-full flex-col gap-5">
              {/* Card 1: Contact details */}
              <div className="flex w-full flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-5">
                <h2 className="text-lg font-semibold text-op-text-primary">
                  Contact details
                </h2>

                <div className="flex flex-col gap-3">
                  <div className="flex flex-col gap-2">
                    <label className="text-sm font-semibold text-op-text-primary leading-5">
                      Contact name
                    </label>
                    <Input
                      value={addressContactName}
                      onChange={(e) => setAddressContactName(e.target.value)}
                      placeholder="Enter full name"
                      className="h-11 rounded-sm border-op-border-default bg-op-background-primary px-3.5 text-sm text-op-text-primary placeholder:text-op-text-muted"
                    />
                  </div>
                  <p className="text-sm font-normal text-op-text-muted">
                    This person may be contacted if there is an issue with the delivery.
                  </p>
                </div>

                <div className="flex flex-col gap-3">
                  <div className="flex flex-col gap-2">
                    <label className="text-sm font-semibold text-op-text-primary leading-5">
                      Contact phone number
                    </label>
                    <Input
                      value={addressContactPhone}
                      onChange={(e) => setAddressContactPhone(e.target.value)}
                      placeholder="Enter phone number"
                      className="h-11 rounded-sm border-op-border-default bg-op-background-primary px-3.5 text-sm text-op-text-primary placeholder:text-op-text-muted"
                    />
                  </div>
                  <p className="text-sm font-normal text-op-text-muted">
                    Used only for delivery updates or access questions.
                  </p>
                </div>
              </div>

              {/* Card 2: Business details */}
              <div className="flex w-full flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-5">
                <h2 className="text-lg font-semibold text-op-text-primary">
                  Business details
                </h2>

                <div className="flex flex-col gap-2">
                  <label className="text-sm font-semibold text-op-text-primary leading-5">
                    Business name
                  </label>
                  <Input
                    value={addressBusinessName}
                    onChange={(e) => setAddressBusinessName(e.target.value)}
                    className="h-11 rounded-sm border-op-border-default bg-op-background-primary px-3.5 text-sm text-op-text-primary"
                  />
                </div>

                <div className="flex flex-col gap-2">
                  <label className="text-sm font-semibold text-op-text-primary leading-5">
                    Location name
                  </label>
                  <Input
                    value={addressLocationName}
                    onChange={(e) => setAddressLocationName(e.target.value)}
                    className="h-11 rounded-sm border-op-border-default bg-op-background-primary px-3.5 text-sm text-op-text-primary"
                  />
                </div>
              </div>

              {/* Card 3: Address */}
              <div className="flex w-full flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-5">
                <h2 className="text-lg font-semibold text-op-text-primary">
                  Address
                </h2>

                <div className="flex flex-col gap-2">
                  <label className="text-sm font-semibold text-op-text-primary leading-5">
                    Postcode
                  </label>
                  <Input
                    value={addressPostcode}
                    onChange={(e) => setAddressPostcode(e.target.value)}
                    placeholder="Enter postcode"
                    className="h-11 rounded-sm border-op-border-default bg-op-background-primary px-3.5 text-sm text-op-text-primary placeholder:text-op-text-muted"
                  />
                </div>

                <div className="flex flex-col gap-2">
                  <label className="text-sm font-semibold text-op-text-primary leading-5">
                    Country
                  </label>
                  <div className="flex h-11 items-center justify-between rounded-sm border border-op-border-default bg-op-background-primary px-3.5 text-sm text-op-text-primary opacity-80">
                    <span>{addressCountry}</span>
                    <ChevronDown className="size-4 text-op-text-muted" />
                  </div>
                </div>

                <div className="flex flex-col gap-2">
                  <label className="text-sm font-semibold text-op-text-primary leading-5">
                    Address line 1
                  </label>
                  <Input
                    value={addressLine1}
                    onChange={(e) => setAddressLine1(e.target.value)}
                    placeholder="Enter address line 1"
                    className="h-11 rounded-sm border-op-border-default bg-op-background-primary px-3.5 text-sm text-op-text-primary placeholder:text-op-text-muted"
                  />
                </div>

                <div className="border-t border-op-border-default" />

                <div className="flex flex-col gap-2">
                  <label className="text-sm font-semibold text-op-text-primary leading-5">
                    Address line 2
                  </label>
                  <Input
                    value={addressLine2}
                    onChange={(e) => setAddressLine2(e.target.value)}
                    placeholder="Enter address line 2"
                    className="h-11 rounded-sm border-op-border-default bg-op-background-primary px-3.5 text-sm text-op-text-primary placeholder:text-op-text-muted"
                  />
                </div>
              </div>

              {/* Checkbox: Save this address */}
              <label
                htmlFor="check-save-address"
                className="flex cursor-pointer items-center gap-2 select-none pt-1"
              >
                <Checkbox
                  id="check-save-address"
                  checked={saveAddressForFuture}
                  onCheckedChange={(c) => setSaveAddressForFuture(Boolean(c))}
                  className="border-op-border-default bg-op-surface-secondary data-checked:bg-op-action-primary data-checked:border-op-action-primary data-checked:text-white"
                />
                <span className="text-sm font-medium text-op-text-primary">
                  Save this address for future {selectedLocationName} orders.
                </span>
              </label>

              {/* Actions: Save and use address, Cancel */}
              <div className="flex items-center gap-3 pt-2">
                <Button
                  type="button"
                  variant="op-secondary"
                  onClick={handleSaveAndUseAddress}
                  className="h-10 rounded-xs px-4 text-sm font-medium"
                >
                  Save and use address
                </Button>

                <Button
                  type="button"
                  variant="outline"
                  onClick={handleCancelAddAddress}
                  className="h-10 rounded-xs border-op-border-default bg-transparent px-4 text-sm font-medium text-op-text-primary hover:bg-op-surface-secondary"
                >
                  Cancel
                </Button>
              </div>
            </div>
          </div>
        </div>
      ) : (
        <>
          {/* Top Breadcrumb & Close Bar */}
          <div className="flex items-center justify-between border-b border-op-border-default pb-4">
            <div className="flex items-center gap-2 text-sm font-medium">
              <button
                type="button"
                onClick={onBackToShop}
                className="text-op-text-primary transition-colors hover:underline"
              >
                Shop
              </button>
              <ChevronRight className="size-3.5 text-op-text-muted" />
              <button
                type="button"
                onClick={onBackToProduct || onBackToShop}
                className="text-op-text-muted transition-colors hover:text-op-text-primary hover:underline"
              >
                {productTitle}
              </button>
              <ChevronRight className="size-3.5 text-op-text-muted" />
              <span className="text-op-text-muted">Checkout</span>
            </div>

            <Button
              type="button"
              variant="ghost"
              size="icon"
              onClick={onBackToShop}
              className="size-9 rounded-xs bg-op-surface-secondary text-op-text-muted hover:bg-op-surface-secondary/80 hover:text-op-text-primary"
              aria-label="Close checkout"
            >
              <X className="size-4" />
            </Button>
          </div>

          {/* Main Form Center Container */}
          <div className="mx-auto flex w-full max-w-[824px] flex-col items-start gap-12 pt-6 pb-24 text-op-text-primary">
            {/* Brand Wordmark */}
            <div className="flex items-center">
              <span className="text-3xl font-extrabold tracking-tight text-white">
                tummly<span className="text-op-action-primary">.</span>
              </span>
            </div>

            {/* Page Title & Subtitle */}
            <div className="flex flex-col gap-2">
              <h1 className="text-3xl font-bold tracking-tight text-op-text-primary">
                Order {productTitle}
              </h1>
              <p className="max-w-xl text-sm font-medium text-op-text-muted leading-relaxed">
                Review the material, QR connection, delivery details and payment before placing your order.
              </p>
            </div>

            {/* ========================================================================= */}
            {/* STEP 1: DELIVERY & REVIEW MATERIAL                                        */}
            {/* ========================================================================= */}
            {checkoutStep === "delivery" && (
              <div className="flex w-full flex-col gap-6">
                {/* Card 1: Review your material */}
                <div className="flex w-full flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6">
                  <div className="flex flex-col gap-1">
                    <h2 className="text-lg font-semibold text-op-text-primary">
                      Review your material
                    </h2>
                    <p className="text-sm font-normal text-op-text-muted">
                      Confirm the materials, quantities and selected location before continuing.
                    </p>
                  </div>

                  {/* Key-Value Review List */}
                  <div className="flex flex-col gap-3.5 text-sm">
                    <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
                      <span className="text-base font-semibold text-op-text-muted">Product</span>
                      <span className="text-base font-medium text-op-text-primary">{productTitle}</span>
                    </div>

                    <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
                      <span className="text-base font-semibold text-op-text-muted">Specification</span>
                      <span className="text-base font-medium text-op-text-primary">{productSpecification}</span>
                    </div>

                    <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
                      <span className="text-base font-semibold text-op-text-muted">Location</span>
                      <span className="text-base font-medium text-op-text-primary">{selectedLocationName}</span>
                    </div>

                    <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
                      <span className="text-base font-semibold text-op-text-muted">Quantity</span>
                      <span className="text-base font-medium text-op-text-primary">Pack of {quantity}</span>
                    </div>

                    <div className="flex items-center justify-between pb-1">
                      <span className="text-base font-semibold text-op-text-muted">Price</span>
                      <span className="text-base font-medium text-op-text-primary">£{subtotal.toFixed(2)} excluding VAT</span>
                    </div>
                  </div>

                  {/* Actions: Edit quantity & Change location */}
                  <div className="flex items-center gap-3 pt-1">
                    <Button
                      type="button"
                      variant="op-secondary"
                      className="h-10 rounded-xs px-4 text-sm font-medium"
                      onClick={() => setIsEditQuantityOpen(true)}
                    >
                      Edit quantity
                    </Button>

                    {locations.length > 1 && onSelectLocation ? (
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button
                            type="button"
                            variant="outline"
                            className="h-10 rounded-xs border-op-border-default bg-transparent px-4 text-sm font-medium text-op-text-primary hover:bg-op-surface-secondary"
                          >
                            Change location
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent
                          align="start"
                          className="w-56 border-op-border-default bg-op-background-primary text-op-text-primary"
                        >
                          {locations.map((loc) => (
                            <DropdownMenuItem
                              key={loc.id}
                              onClick={() => {
                                onSelectLocation(loc.id)
                                setDeliveryAddress(loc.address || deliveryAddress)
                                toast.success(`Location switched to ${loc.locationName}`)
                              }}
                              className={cn(
                                "cursor-pointer text-xs",
                                loc.locationName === selectedLocationName && "font-semibold text-op-action-primary"
                              )}
                            >
                              {loc.locationName}
                            </DropdownMenuItem>
                          ))}
                        </DropdownMenuContent>
                      </DropdownMenu>
                    ) : (
                      <Button
                        type="button"
                        variant="outline"
                        className="h-10 rounded-xs border-op-border-default bg-transparent px-4 text-sm font-medium text-op-text-primary hover:bg-op-surface-secondary"
                        onClick={() => toast.info(`Current location: ${selectedLocationName}`)}
                      >
                        Change location
                      </Button>
                    )}
                  </div>
                </div>

                {/* Card 2: Delivery details (Expanded) */}
                <div className="flex w-full flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6">
                  <div className="flex items-center gap-4">
                    <div className="flex size-11 items-center justify-center rounded-full border border-op-border-default bg-op-surface-secondary text-op-text-primary">
                      <Package className="size-5" />
                    </div>
                    <div className="flex flex-col gap-0.5">
                      <h2 className="text-lg font-semibold text-op-text-primary">
                        Delivery details
                      </h2>
                      <p className="text-sm font-normal text-op-text-muted">
                        Confirm the delivery address, contact information and preferred delivery method.
                      </p>
                    </div>
                  </div>

                  <div className="border-t border-op-border-default" />

                  {/* Saved Address Subsection */}
                  <div className="flex flex-col gap-3.5">
                    <h3 className="text-base font-semibold text-op-text-primary">
                      Saved address
                    </h3>

                    <div className="flex w-full items-center justify-between rounded-sm border border-op-action-primary/60 bg-op-background-primary p-4">
                      <div className="flex flex-col gap-1">
                        <span className="text-sm font-medium text-op-text-primary">
                          {contactName} · {contactPhone}
                        </span>
                        <span className="text-sm font-medium text-op-text-muted">
                          {selectedLocationName} · {deliveryAddress}
                        </span>
                      </div>

                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-8.5 rounded-xs border-op-border-default bg-transparent px-3 text-xs font-medium text-op-text-primary hover:bg-op-surface-secondary"
                        onClick={() => handleOpenAddAddress("delivery")}
                      >
                        Edit address
                      </Button>
                    </div>
                  </div>

                  <div className="border-t border-op-border-default" />

                  {/* Delivery Method Subsection */}
                  <div className="flex flex-col gap-3.5">
                    <h3 className="text-base font-semibold text-op-text-primary">
                      Delivery method
                    </h3>

                    {/* Standard Delivery Option */}
                    <div
                      onClick={() => setDeliveryMethod("standard")}
                      className={cn(
                        "flex w-full cursor-pointer items-center justify-between rounded-sm border p-4 transition-all",
                        deliveryMethod === "standard"
                          ? "border-op-action-primary bg-op-surface-secondary/40"
                          : "border-op-border-default bg-op-background-primary hover:border-op-border-default/80"
                      )}
                    >
                      <div className="flex flex-col gap-1">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-medium text-op-text-primary">
                            Standard delivery
                          </span>
                          {deliveryMethod === "standard" && (
                            <Check className="size-4 text-op-action-primary" />
                          )}
                        </div>
                        <span className="text-sm font-medium text-op-text-muted">
                          Delivered within 3–5 business days after dispatch.
                        </span>
                      </div>
                      <span className="text-sm font-medium text-op-text-muted">
                        Free
                      </span>
                    </div>

                    {/* Express Delivery Option */}
                    <div
                      onClick={() => setDeliveryMethod("express")}
                      className={cn(
                        "flex w-full cursor-pointer items-center justify-between rounded-sm border p-4 transition-all",
                        deliveryMethod === "express"
                          ? "border-op-action-primary bg-op-surface-secondary/40"
                          : "border-op-border-default bg-op-background-primary hover:border-op-border-default/80"
                      )}
                    >
                      <div className="flex flex-col gap-1">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-medium text-op-text-primary">
                            Express delivery
                          </span>
                          {deliveryMethod === "express" && (
                            <Check className="size-4 text-op-action-primary" />
                          )}
                        </div>
                        <span className="text-sm font-medium text-op-text-muted">
                          Delivered within 1–2 business days after dispatch.
                        </span>
                      </div>
                      <span className="text-sm font-medium text-op-text-muted">
                        £20.00
                      </span>
                    </div>

                    <p className="text-sm text-op-text-muted leading-relaxed">
                      Production time is separate from delivery time. The estimated dispatch date will be confirmed after your order has been processed.
                    </p>
                  </div>

                  <div className="border-t border-op-border-default" />

                  {/* Contact Subsection */}
                  <div className="flex flex-col gap-4">
                    <h3 className="text-base font-semibold text-op-text-primary">
                      Contact
                    </h3>

                    <div className="flex flex-col gap-1.5">
                      <label className="text-sm font-semibold text-op-text-primary">
                        Contact name
                      </label>
                      <Input
                        value={contactName}
                        onChange={(e) => setContactName(e.target.value)}
                        className="h-11 rounded-sm border-op-border-default bg-op-background-primary px-3.5 text-sm text-op-text-primary"
                      />
                    </div>

                    <div className="flex flex-col gap-1.5">
                      <label className="text-sm font-semibold text-op-text-primary">
                        Phone number
                      </label>
                      <Input
                        value={contactPhone}
                        onChange={(e) => setContactPhone(e.target.value)}
                        className="h-11 rounded-sm border-op-border-default bg-op-background-primary px-3.5 text-sm text-op-text-primary"
                      />
                    </div>

                    <p className="text-sm text-op-text-muted">
                      The delivery provider may contact this person if there is an access or delivery issue.
                    </p>
                  </div>

                  <div className="border-t border-op-border-default" />

                  {/* Delivery Instructions Subsection */}
                  <div className="flex flex-col gap-2">
                    <label className="text-sm font-semibold text-op-text-primary">
                      Delivery instructions — optional
                    </label>
                    <Textarea
                      value={deliveryInstructions}
                      onChange={(e) => setDeliveryInstructions(e.target.value)}
                      placeholder="Add delivery instructions"
                      rows={3}
                      className="rounded-sm border-op-border-default bg-op-background-primary p-3 text-sm text-op-text-primary placeholder:text-op-text-muted"
                    />
                  </div>

                  <div className="border-t border-op-border-default" />

                  {/* Step 1 Button: Continue to payment */}
                  <div className="pt-2">
                    <Button
                      type="button"
                      variant="op-secondary"
                      onClick={handleContinueToPayment}
                      className="h-10 rounded-xs px-5 text-sm font-medium"
                    >
                      Continue to payment
                    </Button>
                  </div>
                </div>
              </div>
            )}

            {/* ========================================================================= */}
            {/* STEP 2: PAYMENT & ORDER SUMMARY                                           */}
            {/* ========================================================================= */}
            {checkoutStep === "payment" && (
              <div className="flex w-full flex-col gap-6">
                {/* Card 1: Delivery details (Collapsed summary view with Edit button) */}
                <div className="flex w-full flex-col gap-5 rounded-md border border-op-border-default bg-op-card-background p-6">
                  <div className="flex items-center gap-4">
                    <div className="flex size-11 items-center justify-center rounded-full border border-op-border-default bg-op-surface-secondary text-op-text-primary">
                      <Package className="size-5" />
                    </div>
                    <div className="flex flex-col gap-0.5">
                      <h2 className="text-lg font-semibold text-op-text-primary">
                        Delivery details
                      </h2>
                      <p className="text-sm font-normal text-op-text-muted">
                        Confirm the delivery address, contact information and preferred delivery method.
                      </p>
                    </div>
                  </div>

                  <div className="text-sm font-normal text-op-text-primary">
                    {selectedLocationName}, {deliveryAddress.split(",")[1]?.trim() || "SE1 1TQ"} /{" "}
                    {deliveryMethod === "express" ? "Express delivery" : "Standard delivery"} /{" "}
                    {deliveryPrice === 0 ? "Free" : `£${deliveryPrice.toFixed(2)}`}
                  </div>

                  <div>
                    <Button
                      type="button"
                      variant="op-secondary"
                      className="h-9.5 rounded-xs px-4 text-sm font-medium"
                      onClick={() => handleOpenAddAddress("payment")}
                    >
                      Edit
                    </Button>
                  </div>
                </div>

                {/* Card 2: Payment details */}
                <div className="flex w-full flex-col gap-5 rounded-md border border-op-border-default bg-op-card-background p-6">
                  <div className="flex items-center gap-4">
                    <div className="flex size-11 items-center justify-center rounded-full border border-op-border-default bg-op-surface-secondary text-op-text-primary">
                      <CreditCard className="size-5" />
                    </div>
                    <div className="flex flex-col gap-0.5">
                      <h2 className="text-lg font-semibold text-op-text-primary">
                        Payment details
                      </h2>
                      <p className="text-sm font-normal text-op-text-muted">
                        Choose a payment method, confirm the invoice details and place your order.
                      </p>
                    </div>
                  </div>

                  <div className="border-t border-op-border-default" />

                  {/* Payment Method Subsection */}
                  <div className="flex flex-col gap-3.5">
                    <h3 className="text-base font-semibold text-op-text-primary">
                      Payment method
                    </h3>

                    <div className="flex w-full items-center justify-between rounded-sm border border-op-action-primary/60 bg-op-background-primary p-4">
                      <div className="flex flex-col gap-1">
                        <span className="text-sm font-medium text-op-text-primary">
                          {cardType} ending in {cardLast4}
                        </span>
                        <span className="text-sm font-medium text-op-text-muted">
                          Expires {cardExpiry}
                        </span>
                      </div>

                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-8.5 rounded-xs border-op-border-default bg-transparent px-3 text-xs font-medium text-op-text-primary hover:bg-op-surface-secondary"
                        onClick={() => setIsEditPaymentOpen(true)}
                      >
                        Edit payment
                      </Button>
                    </div>
                  </div>

                  <div className="border-t border-op-border-default" />

                  <div>
                    <Button
                      type="button"
                      variant="op-secondary"
                      className="h-9.5 rounded-xs px-4 text-sm font-medium"
                      onClick={() => setIsEditPaymentOpen(true)}
                    >
                      Add payment method
                    </Button>
                  </div>
                </div>

                {/* Card 3: Order summary */}
                <div className="flex w-full flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6">
                  <div className="flex flex-col gap-1">
                    <h2 className="text-lg font-semibold text-op-text-primary">
                      Order summary
                    </h2>
                    <p className="text-sm font-normal text-op-text-muted">
                      Confirm the materials, quantities and selected location before continuing.
                    </p>
                  </div>

                  <div className="flex flex-col gap-3.5 text-sm">
                    <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
                      <span className="text-base font-semibold text-op-text-muted">Product</span>
                      <span className="text-base font-medium text-op-text-primary">{productTitle}</span>
                    </div>

                    <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
                      <span className="text-base font-semibold text-op-text-muted">Specification</span>
                      <span className="text-base font-medium text-op-text-primary">{productSpecification}</span>
                    </div>

                    <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
                      <span className="text-base font-semibold text-op-text-muted">Location</span>
                      <span className="text-base font-medium text-op-text-primary">{selectedLocationName}</span>
                    </div>

                    <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
                      <span className="text-base font-semibold text-op-text-muted">Quantity</span>
                      <span className="text-base font-medium text-op-text-primary">Pack of {quantity}</span>
                    </div>

                    <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
                      <span className="text-base font-semibold text-op-text-muted">Delivery</span>
                      <span className="text-base font-medium text-op-text-primary">
                        {deliveryPrice === 0 ? "Free" : `£${deliveryPrice.toFixed(2)}`}
                      </span>
                    </div>

                    <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
                      <span className="text-base font-semibold text-op-text-muted">Subtotal before VAT</span>
                      <span className="text-base font-medium text-op-text-primary">£{subtotal.toFixed(2)}</span>
                    </div>

                    <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
                      <span className="text-base font-semibold text-op-text-muted">VAT</span>
                      <span className="text-base font-medium text-op-text-primary">£{vat.toFixed(2)}</span>
                    </div>

                    <div className="flex items-center justify-between pt-1">
                      <span className="text-base font-semibold text-op-text-muted">Total</span>
                      <span className="text-base font-bold text-op-text-primary">£{total.toFixed(2)}</span>
                    </div>
                  </div>
                </div>

                {/* Checkbox 1 */}
                <label
                  htmlFor="check-review-details"
                  className="flex cursor-pointer items-start gap-3 select-none"
                >
                  <Checkbox
                    id="check-review-details"
                    checked={checkedReviewDetails}
                    onCheckedChange={(c) => setCheckedReviewDetails(Boolean(c))}
                    className="mt-0.5 border-op-border-default bg-op-surface-secondary data-checked:bg-op-action-primary data-checked:border-op-action-primary data-checked:text-white"
                  />
                  <span className="text-sm font-medium text-op-text-primary leading-snug">
                    I have checked the materials, quantity, location, delivery and invoice details.
                  </span>
                </label>

                {/* Checkbox 2 */}
                <label
                  htmlFor="check-agree-terms"
                  className="flex max-w-xl cursor-pointer items-start gap-3 select-none"
                >
                  <Checkbox
                    id="check-agree-terms"
                    checked={checkedAgreeTerms}
                    onCheckedChange={(c) => setCheckedAgreeTerms(Boolean(c))}
                    className="mt-0.5 border-op-border-default bg-op-surface-secondary data-checked:bg-op-action-primary data-checked:border-op-action-primary data-checked:text-white"
                  />
                  <span className="text-sm font-medium text-op-text-primary leading-snug">
                    I agree to the Tummly Shop terms and understand that made-to-order materials cannot normally be changed after production begins.
                  </span>
                </label>

                {/* Bottom Actions Row: Place order, Back, Save at Drafts */}
                <div className="flex items-center gap-4 pt-2">
                  <Button
                    type="button"
                    variant="op-secondary"
                    disabled={isPaymentProcessing}
                    onClick={handlePlaceOrder}
                    className="h-10 rounded-xs px-5 text-sm font-medium"
                  >
                    {isPaymentProcessing ? "Processing..." : "Place order"}
                  </Button>

                  <Button
                    type="button"
                    variant="outline"
                    onClick={handleBackToDelivery}
                    className="h-10 rounded-xs border-op-border-default bg-transparent px-5 text-sm font-medium text-op-text-primary hover:bg-op-surface-secondary"
                  >
                    Back
                  </Button>

                  <button
                    type="button"
                    onClick={handleSaveDraft}
                    className="text-sm font-medium text-op-text-muted transition-colors hover:text-op-text-primary hover:underline ml-2"
                  >
                    Save at Drafts
                  </button>
                </div>
              </div>
            )}
          </div>
        </>
      )}

      {/* Edit Quantity Modal */}
      <Dialog open={isEditQuantityOpen} onOpenChange={setIsEditQuantityOpen}>
        <DialogContent
          className="z-[200] w-full max-w-sm gap-6 rounded-sm border border-op-border-default bg-op-card-background p-6 text-op-text-primary shadow-2xl"
          overlayClassName="z-[190] bg-black/60 backdrop-blur-xs"
        >
          <DialogHeader className="text-left">
            <DialogTitle className="text-lg font-bold text-op-text-primary">
              Edit Quantity
            </DialogTitle>
            <DialogDescription className="text-xs text-op-text-muted">
              Select package quantity for {productTitle}
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-2">
            {[5, 10, 20, 50].map((qty) => (
              <button
                key={qty}
                type="button"
                onClick={() => {
                  setQuantity(qty)
                  setIsEditQuantityOpen(false)
                  toast.success(`Quantity updated to Pack of ${qty}`)
                }}
                className={cn(
                  "flex items-center justify-between rounded-sm border p-3 text-sm transition-colors",
                  quantity === qty
                    ? "border-op-action-primary bg-op-surface-secondary font-semibold text-op-text-primary"
                    : "border-op-border-default bg-op-background-primary text-op-text-secondary hover:bg-op-surface-secondary/50"
                )}
              >
                <span>Pack of {qty}</span>
                <span>£{((basePrice * qty) / 20).toFixed(2)}</span>
              </button>
            ))}
          </div>
        </DialogContent>
      </Dialog>

      {/* Edit / Add Payment Method Modal */}
      <Dialog open={isEditPaymentOpen} onOpenChange={setIsEditPaymentOpen}>
        <DialogContent
          className="z-[200] w-full max-w-md gap-6 rounded-sm border border-op-border-default bg-op-card-background p-6 text-op-text-primary shadow-2xl"
          overlayClassName="z-[190] bg-black/60 backdrop-blur-xs"
        >
          <DialogHeader className="text-left">
            <DialogTitle className="text-lg font-bold text-op-text-primary">
              Payment Method
            </DialogTitle>
            <DialogDescription className="text-xs text-op-text-muted">
              Add or update card details for this order
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-semibold text-op-text-muted">Cardholder name</label>
              <Input
                defaultValue="Mohamed Mahmoud"
                className="h-10 rounded-sm border-op-border-default bg-op-background-primary text-xs text-op-text-primary"
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-semibold text-op-text-muted">Card number</label>
              <Input
                defaultValue="•••• •••• •••• 4242"
                className="h-10 rounded-sm border-op-border-default bg-op-background-primary text-xs text-op-text-primary"
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="flex flex-col gap-1.5">
                <label className="text-xs font-semibold text-op-text-muted">Expiry date</label>
                <Input
                  value={cardExpiry}
                  onChange={(e) => setCardExpiry(e.target.value)}
                  placeholder="MM/YY"
                  className="h-10 rounded-sm border-op-border-default bg-op-background-primary text-xs text-op-text-primary"
                />
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-xs font-semibold text-op-text-muted">CVC</label>
                <Input
                  defaultValue="123"
                  type="password"
                  maxLength={4}
                  className="h-10 rounded-sm border-op-border-default bg-op-background-primary text-xs text-op-text-primary"
                />
              </div>
            </div>

            <Button
              type="button"
              variant="op-primary"
              onClick={() => {
                setIsEditPaymentOpen(false)
                toast.success("Payment method updated")
              }}
              className="h-9 text-xs font-medium"
            >
              Save payment method
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}

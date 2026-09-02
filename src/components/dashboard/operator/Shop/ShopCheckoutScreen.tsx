import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
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
import type { DetailedShopDraft } from "@/components/dashboard/operator/Shop/ShopOrdersScreen"
import { scrollShopPaneToTop } from "@/components/dashboard/operator/Shop/ShopProductScreen"
import {
  computeShopCheckoutTotalsPence,
  fetchShopDeliveryDefaults,
  payShopOrder,
  placeShopOrder,
  type CheckoutLine,
  type ShopShipToPayload,
} from "@/api/shopOrdersApi"
import type { ShopPaidWriteChrome } from "@/lib/operatorShop/shopPaidWriteChrome"
import { ukPostcodeRegex } from "@/lib/locationUpload/locationUploadValidation"
import { tryNormalizePhoneToE164 } from "@/lib/phoneNumber"
import { cn } from "@/lib/utils"

type DeliveryMethod = "standard" | "express"
type CheckoutStep = "delivery" | "payment" | "add-address"

type ShopCheckoutScreenProps = {
  locationId: number
  lines: CheckoutLine[]
  fromCart: boolean
  selectedLocationName: string
  selectedLocationAddress?: string
  locations: Array<{ id: number; locationName: string; address: string }>
  onSelectLocation?: (locationId: number) => void
  onBackToShop: () => void
  onBackToProduct?: () => void
  onOrderPlaced?: (orderNumber: string) => void
  onSaveDraft?: (draft: DetailedShopDraft) => void
  paidWriteChrome: ShopPaidWriteChrome
}

function penceToPounds(pence: number): string {
  return `£${(pence / 100).toFixed(2)}`
}

function formatShipToAddress(input: {
  addressLine1: string
  addressLine2: string
  postcode: string
  country: string
}): string {
  return [input.addressLine1, input.addressLine2, input.postcode, input.country]
    .map((part) => part.trim())
    .filter(Boolean)
    .join(", ")
}

export function ShopCheckoutScreen({
  locationId,
  lines,
  fromCart,
  selectedLocationName,
  locations,
  onSelectLocation,
  onBackToShop,
  onBackToProduct,
  onOrderPlaced,
  onSaveDraft,
  paidWriteChrome,
}: ShopCheckoutScreenProps) {
  const [checkoutStep, setCheckoutStep] = useState<CheckoutStep>("delivery")
  const [checkoutLines, setCheckoutLines] = useState<CheckoutLine[]>(lines)
  const [deliveryMethod, setDeliveryMethod] = useState<DeliveryMethod>("standard")

  const [contactName, setContactName] = useState("")
  const [contactPhone, setContactPhone] = useState("")
  const [addressLine1, setAddressLine1] = useState("")
  const [addressLine2, setAddressLine2] = useState("")
  const [postcode, setPostcode] = useState("")
  const [country, setCountry] = useState("United Kingdom")
  const [deliveryInstructions, setDeliveryInstructions] = useState("")

  const [addressContactName, setAddressContactName] = useState("")
  const [addressContactPhone, setAddressContactPhone] = useState("")
  const [addressPostcode, setAddressPostcode] = useState("")
  const [addressCountry, setAddressCountry] = useState("United Kingdom")
  const [addressFormLine1, setAddressFormLine1] = useState("")
  const [addressFormLine2, setAddressFormLine2] = useState("")

  const [checkedReviewDetails, setCheckedReviewDetails] = useState(false)
  const [checkedAgreeTerms, setCheckedAgreeTerms] = useState(false)

  const [isEditQuantityOpen, setIsEditQuantityOpen] = useState(false)
  const [isPaymentProcessing, setIsPaymentProcessing] = useState(false)
  const [returnStepAfterAddress, setReturnStepAfterAddress] = useState<
    "delivery" | "payment"
  >("payment")

  const purchaseBlocked = paidWriteChrome.purchaseDisabled

  useEffect(() => {
    setCheckoutLines(lines)
  }, [lines])

  useEffect(() => {
    let cancelled = false

    async function loadDeliveryDefaults() {
      try {
        const defaults = await fetchShopDeliveryDefaults(locationId)
        if (cancelled) {
          return
        }
        setContactName(defaults.contactName)
        setContactPhone(defaults.contactPhone ?? "")
        setAddressLine1(defaults.addressLine1)
        setAddressLine2(defaults.addressLine2 ?? "")
        setPostcode(defaults.postcode)
        setCountry(defaults.country || "United Kingdom")
      } catch {
        if (!cancelled) {
          toast.error("Could not load delivery defaults.")
        }
      }
    }

    void loadDeliveryDefaults()

    return () => {
      cancelled = true
    }
  }, [locationId])

  const canEditQuantity = !fromCart && checkoutLines.length === 1
  const singleLine = checkoutLines.length === 1 ? checkoutLines[0] : null
  const materialsLabel =
    checkoutLines.length === 1
      ? (singleLine?.title ?? "Checkout")
      : "Your materials"
  const pageTitle =
    checkoutLines.length === 1
      ? `Order ${singleLine?.title ?? "materials"}`
      : "Checkout"

  const materialsNetPence = checkoutLines.reduce(
    (sum, line) => sum + line.lineNetPence,
    0
  )
  const totals = computeShopCheckoutTotalsPence({
    materialsNetPence,
    deliveryMethod,
  })
  const deliveryDisplay =
    totals.deliveryNetPence === 0
      ? "Free"
      : penceToPounds(totals.deliveryNetPence)
  const shipToDisplay = formatShipToAddress({
    addressLine1,
    addressLine2,
    postcode,
    country,
  })

  const handleContinueToPayment = () => {
    setCheckoutStep("payment")
    scrollShopPaneToTop()
  }

  const handleBackToDelivery = () => {
    setCheckoutStep("delivery")
    scrollShopPaneToTop()
  }

  const handleOpenAddAddress = (returnTo: "delivery" | "payment" = "payment") => {
    setAddressContactName(contactName)
    setAddressContactPhone(contactPhone)
    setAddressFormLine1(addressLine1)
    setAddressFormLine2(addressLine2)
    setAddressPostcode(postcode)
    setAddressCountry(country || "United Kingdom")
    setReturnStepAfterAddress(returnTo)
    setCheckoutStep("add-address")
    scrollShopPaneToTop()
  }

  const handleSaveAndUseAddress = () => {
    setContactName(addressContactName.trim())
    setContactPhone(addressContactPhone.trim())
    setAddressLine1(addressFormLine1.trim())
    setAddressLine2(addressFormLine2.trim())
    setPostcode(addressPostcode.trim())
    setCountry(addressCountry.trim() || "United Kingdom")
    toast.success("Delivery address updated")
    setCheckoutStep(returnStepAfterAddress)
    scrollShopPaneToTop()
  }

  const handleCancelAddAddress = () => {
    setCheckoutStep(returnStepAfterAddress)
    scrollShopPaneToTop()
  }

  const validateShipTo = (): string | null => {
    if (!contactName.trim()) {
      return "Contact name is required."
    }
    if (!addressLine1.trim()) {
      return "Address line 1 is required."
    }
    if (!postcode.trim()) {
      return "Postcode is required."
    }
    if (!ukPostcodeRegex.test(postcode.trim())) {
      return "Enter a valid UK postcode."
    }
    if (country.trim() !== "United Kingdom") {
      return "Country must be United Kingdom."
    }
    if (contactPhone.trim() && !tryNormalizePhoneToE164(contactPhone)) {
      return "Enter a valid UK phone number."
    }
    return null
  }

  const handlePlaceOrder = async () => {
    if (purchaseBlocked) {
      return
    }

    if (!checkedReviewDetails || !checkedAgreeTerms) {
      toast.error("Please confirm the checkout checkboxes to proceed")
      return
    }

    const shipToError = validateShipTo()
    if (shipToError) {
      toast.error(shipToError)
      return
    }

    if (checkoutLines.length === 0) {
      toast.error("Add at least one material before placing an order.")
      return
    }

    const normalizedPhone = contactPhone.trim()
      ? tryNormalizePhoneToE164(contactPhone)
      : null
    const shipTo: ShopShipToPayload = {
      contactName: contactName.trim(),
      contactPhone: normalizedPhone,
      addressLine1: addressLine1.trim(),
      addressLine2: addressLine2.trim() || null,
      postcode: postcode.trim(),
      country: country.trim(),
      deliveryInstructions: deliveryInstructions.trim() || null,
    }

    setIsPaymentProcessing(true)
    try {
      const order = fromCart
        ? await placeShopOrder({
            locationId,
            fromCart: true,
            deliveryMethod,
            expectedGrossPence: totals.grossPence,
            shipTo,
          })
        : await placeShopOrder({
            locationId,
            lines: checkoutLines.map((line) => ({
              skuId: line.skuId,
              quantity: line.quantity,
            })),
            deliveryMethod,
            expectedGrossPence: totals.grossPence,
            shipTo,
          })

      const paySession = await payShopOrder({
        orderId: order.id,
        locationId,
        idempotencyKey: crypto.randomUUID(),
      })

      if (paySession.outcome === "pay" && paySession.redirectUrl) {
        window.location.assign(paySession.redirectUrl)
        return
      }

      toast.error("Could not start Revolut payment.")
    } catch {
      toast.error("Could not place order.")
    } finally {
      setIsPaymentProcessing(false)
    }
  }

  const handleSaveDraft = () => {
    const draftNum = `#TM-${Math.floor(10400 + Math.random() * 90)}`
    const materialsSummary =
      checkoutLines.length === 1
        ? `${checkoutLines[0].title} · Pack of ${checkoutLines[0].quantity}`
        : `${checkoutLines.length} materials`
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
      materials: materialsSummary,
      materialTypes: ["table-tents"],
      lastCompletedStep:
        checkoutStep === "payment" ? "Payment details" : "Delivery details",
      lastUpdatedDate: `Updated ${new Date().toLocaleDateString("en-GB", {
        day: "2-digit",
        month: "short",
        year: "numeric",
      })}`,
      items: checkoutLines.map(
        (line) =>
          `${line.quantity}x ${line.title}${
            line.specification ? ` (${line.specification})` : ""
          }`
      ),
    }

    if (onSaveDraft) {
      onSaveDraft(newDraft)
    } else {
      toast.success("Draft saved successfully")
      onBackToShop()
    }
  }

  const renderLinesSummary = (includeTotals: boolean) => (
    <div className="flex flex-col gap-3.5 text-sm">
      {checkoutLines.map((line) => (
        <div
          key={line.skuId}
          className="flex flex-col gap-3 border-b border-op-border-default/60 pb-3"
        >
          <div className="flex items-center justify-between">
            <span className="text-base font-semibold text-op-text-muted">
              Product
            </span>
            <span className="text-base font-medium text-op-text-primary">
              {line.title}
            </span>
          </div>
          {line.specification ? (
            <div className="flex items-center justify-between">
              <span className="text-base font-semibold text-op-text-muted">
                Specification
              </span>
              <span className="text-right text-base font-medium text-op-text-primary">
                {line.specification}
              </span>
            </div>
          ) : null}
          <div className="flex items-center justify-between">
            <span className="text-base font-semibold text-op-text-muted">
              Quantity
            </span>
            <span className="text-base font-medium text-op-text-primary">
              Pack of {line.quantity}
            </span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-base font-semibold text-op-text-muted">
              Price
            </span>
            <span className="text-base font-medium text-op-text-primary">
              {penceToPounds(line.lineNetPence)} excluding VAT
            </span>
          </div>
        </div>
      ))}

      <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
        <span className="text-base font-semibold text-op-text-muted">
          Location
        </span>
        <span className="text-base font-medium text-op-text-primary">
          {selectedLocationName}
        </span>
      </div>

      {includeTotals ? (
        <>
          <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
            <span className="text-base font-semibold text-op-text-muted">
              Delivery
            </span>
            <span className="text-base font-medium text-op-text-primary">
              {deliveryDisplay}
            </span>
          </div>
          <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
            <span className="text-base font-semibold text-op-text-muted">
              Subtotal before VAT
            </span>
            <span className="text-base font-medium text-op-text-primary">
              {penceToPounds(totals.materialsNetPence)}
            </span>
          </div>
          <div className="flex items-center justify-between border-b border-op-border-default/60 pb-3">
            <span className="text-base font-semibold text-op-text-muted">VAT</span>
            <span className="text-base font-medium text-op-text-primary">
              {penceToPounds(totals.vatPence)}
            </span>
          </div>
          <div className="flex items-center justify-between pt-1">
            <span className="text-base font-semibold text-op-text-muted">
              Total
            </span>
            <span className="text-base font-bold text-op-text-primary">
              {penceToPounds(totals.grossPence)}
            </span>
          </div>
        </>
      ) : (
        <div className="flex items-center justify-between pb-1">
          <span className="text-base font-semibold text-op-text-muted">
            Materials net
          </span>
          <span className="text-base font-medium text-op-text-primary">
            {penceToPounds(totals.materialsNetPence)} excluding VAT
          </span>
        </div>
      )}
    </div>
  )

  return (
    <div className="flex flex-col gap-6 pb-20">
      {checkoutStep === "add-address" ? (
        <div className="flex flex-col gap-6">
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

          <div className="mx-auto flex w-full max-w-[824px] flex-col items-start gap-10 pt-8 pb-24 text-op-text-primary">
            <div className="flex flex-col gap-2">
              <h1 className="text-3xl font-bold tracking-tight text-op-text-primary">
                Add delivery address
              </h1>
              <p className="text-sm font-medium text-op-text-muted leading-5">
                Add the address and contact details Tummly should use for this
                delivery.
              </p>
            </div>

            <div className="flex w-full flex-col gap-5">
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
                    This person may be contacted if there is an issue with the
                    delivery.
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
                    value={addressFormLine1}
                    onChange={(e) => setAddressFormLine1(e.target.value)}
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
                    value={addressFormLine2}
                    onChange={(e) => setAddressFormLine2(e.target.value)}
                    placeholder="Enter address line 2"
                    className="h-11 rounded-sm border-op-border-default bg-op-background-primary px-3.5 text-sm text-op-text-primary placeholder:text-op-text-muted"
                  />
                </div>
              </div>

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
                {materialsLabel}
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

          <div className="mx-auto flex w-full max-w-[824px] flex-col items-start gap-12 pt-6 pb-24 text-op-text-primary">
            <div className="flex items-center">
              <span className="text-3xl font-extrabold tracking-tight text-white">
                tummly<span className="text-op-action-primary">.</span>
              </span>
            </div>

            <div className="flex flex-col gap-2">
              <h1 className="text-3xl font-bold tracking-tight text-op-text-primary">
                {pageTitle}
              </h1>
              <p className="max-w-xl text-sm font-medium text-op-text-muted leading-relaxed">
                Review the material, QR connection, delivery details and payment
                before placing your order.
              </p>
            </div>

            {checkoutStep === "delivery" && (
              <div className="flex w-full flex-col gap-6">
                <div className="flex w-full flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6">
                  <div className="flex flex-col gap-1">
                    <h2 className="text-lg font-semibold text-op-text-primary">
                      Review your material
                    </h2>
                    <p className="text-sm font-normal text-op-text-muted">
                      Confirm the materials, quantities and selected location
                      before continuing.
                    </p>
                  </div>

                  {renderLinesSummary(false)}

                  <div className="flex items-center gap-3 pt-1">
                    {canEditQuantity ? (
                      <Button
                        type="button"
                        variant="op-secondary"
                        className="h-10 rounded-xs px-4 text-sm font-medium"
                        onClick={() => setIsEditQuantityOpen(true)}
                      >
                        Edit quantity
                      </Button>
                    ) : null}

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
                                toast.success(
                                  `Location switched to ${loc.locationName}`
                                )
                              }}
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
                    ) : (
                      <Button
                        type="button"
                        variant="outline"
                        className="h-10 rounded-xs border-op-border-default bg-transparent px-4 text-sm font-medium text-op-text-primary hover:bg-op-surface-secondary"
                        onClick={() =>
                          toast.info(`Current location: ${selectedLocationName}`)
                        }
                      >
                        Change location
                      </Button>
                    )}
                  </div>
                </div>

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
                        Confirm the delivery address, contact information and
                        preferred delivery method.
                      </p>
                    </div>
                  </div>

                  <div className="border-t border-op-border-default" />

                  <div className="flex flex-col gap-3.5">
                    <h3 className="text-base font-semibold text-op-text-primary">
                      Saved address
                    </h3>

                    <div className="flex w-full items-center justify-between rounded-sm border border-op-action-primary/60 bg-op-background-primary p-4">
                      <div className="flex flex-col gap-1">
                        <span className="text-sm font-medium text-op-text-primary">
                          {contactName || "Contact pending"}
                          {contactPhone ? ` · ${contactPhone}` : ""}
                        </span>
                        <span className="text-sm font-medium text-op-text-muted">
                          {selectedLocationName}
                          {shipToDisplay ? ` · ${shipToDisplay}` : ""}
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

                  <div className="flex flex-col gap-3.5">
                    <h3 className="text-base font-semibold text-op-text-primary">
                      Delivery method
                    </h3>

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
                      Production time is separate from delivery time. The
                      estimated dispatch date will be confirmed after your order
                      has been processed.
                    </p>
                  </div>

                  <div className="border-t border-op-border-default" />

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
                      The delivery provider may contact this person if there is
                      an access or delivery issue.
                    </p>
                  </div>

                  <div className="border-t border-op-border-default" />

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

            {checkoutStep === "payment" && (
              <div className="flex w-full flex-col gap-6">
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
                        Confirm the delivery address, contact information and
                        preferred delivery method.
                      </p>
                    </div>
                  </div>

                  <div className="text-sm font-normal text-op-text-primary">
                    {selectedLocationName}
                    {postcode ? `, ${postcode}` : ""} /{" "}
                    {deliveryMethod === "express"
                      ? "Express delivery"
                      : "Standard delivery"}{" "}
                    / {deliveryDisplay}
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
                        Choose a payment method, confirm the invoice details and
                        place your order.
                      </p>
                    </div>
                  </div>

                  <div className="border-t border-op-border-default" />

                  <div className="flex flex-col gap-3.5">
                    <h3 className="text-base font-semibold text-op-text-primary">
                      Payment
                    </h3>

                    <div className="rounded-sm border border-op-border-default bg-op-background-primary p-4">
                      <p className="text-sm font-medium text-op-text-primary">
                        Pay securely with Revolut
                      </p>
                      <p className="mt-1 text-sm text-op-text-muted">
                        After you place the order, you are redirected to Revolut
                        to complete payment. Your order is marked paid only
                        after Revolut confirms payment.
                      </p>
                    </div>
                  </div>
                </div>

                <div className="flex w-full flex-col gap-6 rounded-md border border-op-border-default bg-op-card-background p-6">
                  <div className="flex flex-col gap-1">
                    <h2 className="text-lg font-semibold text-op-text-primary">
                      Order summary
                    </h2>
                    <p className="text-sm font-normal text-op-text-muted">
                      Confirm the materials, quantities and selected location
                      before continuing.
                    </p>
                  </div>

                  {renderLinesSummary(true)}
                </div>

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
                    I have checked the materials, quantity, location, delivery
                    and invoice details.
                  </span>
                </label>

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
                    I agree to the Tummly Shop terms and understand that
                    made-to-order materials cannot normally be changed after
                    production begins.
                  </span>
                </label>

                <div className="flex flex-col gap-3 pt-2">
                  <div className="flex flex-wrap items-center gap-4">
                    <Button
                      type="button"
                      variant="op-secondary"
                      disabled={isPaymentProcessing || purchaseBlocked}
                      onClick={() => {
                        void handlePlaceOrder()
                      }}
                      className="h-10 rounded-xs px-5 text-sm font-medium"
                    >
                      {isPaymentProcessing
                        ? "Processing..."
                        : "Place order and pay with Revolut"}
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
                      className="text-sm font-medium text-op-text-muted transition-colors hover:text-op-text-primary hover:underline"
                    >
                      Save at Drafts
                    </button>
                  </div>

                  {purchaseBlocked && paidWriteChrome.helperCta ? (
                    <p className="text-sm text-op-text-muted">
                      Purchases are paused.{" "}
                      <Link
                        to={paidWriteChrome.helperCta.href}
                        className="font-medium text-op-action-primary underline-offset-2 hover:underline"
                      >
                        {paidWriteChrome.helperCta.label}
                      </Link>
                    </p>
                  ) : null}
                </div>
              </div>
            )}
          </div>
        </>
      )}

      {canEditQuantity && singleLine ? (
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
                Select package quantity for {singleLine.title}
              </DialogDescription>
            </DialogHeader>

            <div className="flex flex-col gap-2">
              {[5, 10, 20, 50].map((qty) => (
                <button
                  key={qty}
                  type="button"
                  onClick={() => {
                    setCheckoutLines([
                      {
                        ...singleLine,
                        quantity: qty,
                        lineNetPence: singleLine.unitNetPence * qty,
                      },
                    ])
                    setIsEditQuantityOpen(false)
                    toast.success(`Quantity updated to Pack of ${qty}`)
                  }}
                  className={cn(
                    "flex items-center justify-between rounded-sm border p-3 text-sm transition-colors",
                    singleLine.quantity === qty
                      ? "border-op-action-primary bg-op-surface-secondary font-semibold text-op-text-primary"
                      : "border-op-border-default bg-op-background-primary text-op-text-secondary hover:bg-op-surface-secondary/50"
                  )}
                >
                  <span>Pack of {qty}</span>
                  <span>
                    {penceToPounds(singleLine.unitNetPence * qty)}
                  </span>
                </button>
              ))}
            </div>
          </DialogContent>
        </Dialog>
      ) : null}
    </div>
  )
}

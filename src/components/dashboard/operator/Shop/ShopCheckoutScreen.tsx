import { useEffect, useState } from "react"
import {
  ChevronRight,
  Package,
  CreditCard,
  X,
  ChevronDown,
} from "lucide-react"
import { toast } from "sonner"
import logo from "@/assets/svg/logo.svg"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Checkbox } from "@/components/ui/checkbox"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"
import { scrollShopPaneToTop } from "@/components/dashboard/operator/Shop/ShopProductScreen"
import {
  ShopLocationPicker,
  type ShopLocationOption,
} from "@/components/dashboard/operator/Shop/ShopLocationPicker"
import {
  computeShopCheckoutTotalsPence,
  fetchShopDeliveryDefaults,
  payShopOrder,
  placeShopOrder,
  type CheckoutLine,
  type ShopShipToPayload,
} from "@/api/shopOrdersApi"
import type { ShopPaidWriteChrome } from "@/lib/operatorShop/shopPaidWriteChrome"
import { ShopPaidWriteHelperNote } from "@/components/dashboard/operator/Shop/ShopPaidWriteHelperNote"
import {
  SHOP_CHECKOUT_ICON_WELL_CLASS,
  SHOP_CHECKOUT_INPUT_CLASS,
  SHOP_CHECKOUT_TEXTAREA_CLASS,
  SHOP_CHECKOUT_OPTION_DEFAULT_CLASS,
  SHOP_CHECKOUT_OPTION_SELECTED_CLASS,
  SHOP_CHECKOUT_REVIEW_CARD_CLASS,
  SHOP_CHECKOUT_SECTION_CARD_CLASS,
  SHOP_PRODUCT_SPEC_DIVIDER_CLASS,
} from "@/lib/operatorShop/shopSurfacePresentation"
import { ukPostcodeRegex } from "@/lib/locationUpload/locationUploadValidation"
import { tryNormalizePhoneToE164 } from "@/lib/phoneNumber"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"
import { cn } from "@/lib/utils"

type DeliveryMethod = "standard" | "express"
type CheckoutStep = "delivery" | "payment" | "add-address"

type ShopCheckoutScreenProps = {
  locationId: number
  lines: CheckoutLine[]
  fromCart: boolean
  initialShipTo?: ShopShipToPayload
  initialDeliveryMethod?: DeliveryMethod
  selectedLocationName: string
  selectedLocationAddress?: string
  locations: ShopLocationOption[]
  brandLogoPublicUrl: string | null
  mode: OperatorDashboardMode
  onSelectLocation?: (locationId: number) => void
  onBackToShop: () => void
  onBackToProduct?: () => void
  onOrderPlaced?: (orderNumber: string) => void
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
  initialShipTo,
  initialDeliveryMethod,
  selectedLocationName,
  locations,
  brandLogoPublicUrl,
  mode,
  onSelectLocation,
  onBackToShop,
  onBackToProduct,
  onOrderPlaced,
  paidWriteChrome,
}: ShopCheckoutScreenProps) {
  const [checkoutStep, setCheckoutStep] = useState<CheckoutStep>("delivery")
  const [checkoutLines, setCheckoutLines] = useState<CheckoutLine[]>(lines)
  const [deliveryMethod, setDeliveryMethod] = useState<DeliveryMethod>(
    initialDeliveryMethod ?? "standard"
  )

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
      if (initialShipTo) {
        setContactName(initialShipTo.contactName)
        setContactPhone(initialShipTo.contactPhone ?? "")
        setAddressLine1(initialShipTo.addressLine1)
        setAddressLine2(initialShipTo.addressLine2 ?? "")
        setPostcode(initialShipTo.postcode)
        setCountry(initialShipTo.country || "United Kingdom")
        setDeliveryInstructions(initialShipTo.deliveryInstructions ?? "")
        return
      }

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
  }, [locationId, initialShipTo])

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

  const renderSummaryRow = (label: string, value: string) => (
    <div className="flex w-full flex-col gap-3.5">
      <div className="flex items-center justify-between gap-4">
        <span className="shrink-0 text-base font-semibold text-[var(--op-color-gray-550)]">
          {label}
        </span>
        <span className="text-right text-base font-medium text-op-text-primary">
          {value}
        </span>
      </div>
      <div className={SHOP_PRODUCT_SPEC_DIVIDER_CLASS} />
    </div>
  )

  const renderLinesSummary = (includeTotals: boolean) => {
    const singleProductReview =
      !includeTotals && checkoutLines.length === 1 && singleLine != null

    if (singleProductReview) {
      return (
        <div className="flex w-full flex-col">
          {renderSummaryRow("Product", singleLine.title)}
          {singleLine.specification
            ? renderSummaryRow("Specification", singleLine.specification)
            : null}
          {renderSummaryRow("Location", selectedLocationName)}
          {renderSummaryRow("Quantity", `Pack of ${singleLine.quantity}`)}
          {renderSummaryRow(
            "Price",
            `${penceToPounds(singleLine.lineNetPence)} excluding VAT`
          )}
        </div>
      )
    }

    return (
      <div className="flex w-full flex-col">
        {checkoutLines.map((line) => (
          <div key={line.skuId} className="flex flex-col">
            {renderSummaryRow("Product", line.title)}
            {line.specification
              ? renderSummaryRow("Specification", line.specification)
              : null}
            {renderSummaryRow("Quantity", `Pack of ${line.quantity}`)}
            {renderSummaryRow(
              "Price",
              `${penceToPounds(line.lineNetPence)} excluding VAT`
            )}
          </div>
        ))}

        {renderSummaryRow("Location", selectedLocationName)}

        {includeTotals ? (
          <>
            {renderSummaryRow("Delivery", deliveryDisplay)}
            {renderSummaryRow(
              "Subtotal before VAT",
              penceToPounds(totals.materialsNetPence)
            )}
            {renderSummaryRow("VAT", penceToPounds(totals.vatPence))}
            <div className="flex items-center justify-between gap-4 pt-1">
              <span className="shrink-0 text-base font-semibold text-[var(--op-color-gray-550)]">
                Total
              </span>
              <span className="text-right text-base font-bold text-op-text-primary">
                {penceToPounds(totals.grossPence)}
              </span>
            </div>
          </>
        ) : (
          renderSummaryRow(
            "Materials net",
            `${penceToPounds(totals.materialsNetPence)} excluding VAT`
          )
        )}
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      {checkoutStep === "add-address" ? (
        <div className="flex flex-col gap-6">
          <div className="flex items-center justify-between border-b border-op-border-default pb-4">
            <img
              src={logo}
              alt="tummly"
              width={157}
              height={38}
              className="h-[38px] w-auto max-w-[157px] object-contain brightness-0 dark:brightness-100"
            />

            <Button
              type="button"
              variant="op-collapse"
              onClick={handleCancelAddAddress}
              aria-label="Close address form"
            >
              <X className="size-4" />
            </Button>
          </div>

          <div className="mx-auto flex w-full max-w-[824px] flex-col items-start gap-10 pt-8 pb-24 text-op-text-primary">
            <div className="flex flex-col gap-2">
              <h1 className="text-[29px] font-bold tracking-tight text-op-text-primary">
                Add delivery address
              </h1>
              <p className="text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
                Add the address and contact details Tummly should use for this
                delivery.
              </p>
            </div>

            <div className="flex w-full flex-col gap-5">
              <div className={SHOP_CHECKOUT_SECTION_CARD_CLASS}>
                <h2 className="text-lg font-semibold text-op-text-primary">
                  Contact details
                </h2>

                <div className="flex flex-col gap-3">
                  <div className="flex flex-col gap-2">
                    <label className="text-sm font-semibold leading-5 text-op-text-primary">
                      Contact name
                    </label>
                    <Input
                      value={addressContactName}
                      onChange={(e) => setAddressContactName(e.target.value)}
                      placeholder="Enter full name"
                      className={SHOP_CHECKOUT_INPUT_CLASS}
                    />
                  </div>
                  <p className="text-sm font-normal text-[var(--op-color-gray-550)]">
                    This person may be contacted if there is an issue with the
                    delivery.
                  </p>
                </div>

                <div className="flex flex-col gap-3">
                  <div className="flex flex-col gap-2">
                    <label className="text-sm font-semibold leading-5 text-op-text-primary">
                      Contact phone number
                    </label>
                    <Input
                      value={addressContactPhone}
                      onChange={(e) => setAddressContactPhone(e.target.value)}
                      placeholder="Enter phone number"
                      className={SHOP_CHECKOUT_INPUT_CLASS}
                    />
                  </div>
                  <p className="text-sm font-normal text-[var(--op-color-gray-550)]">
                    Used only for delivery updates or access questions.
                  </p>
                </div>
              </div>

              <div className={SHOP_CHECKOUT_SECTION_CARD_CLASS}>
                <h2 className="text-lg font-semibold text-op-text-primary">
                  Address
                </h2>

                <div className="flex flex-col gap-2">
                  <label className="text-sm font-semibold leading-5 text-op-text-primary">
                    Postcode
                  </label>
                  <Input
                    value={addressPostcode}
                    onChange={(e) => setAddressPostcode(e.target.value)}
                    placeholder="Enter postcode"
                    className={SHOP_CHECKOUT_INPUT_CLASS}
                  />
                </div>

                <div className="flex flex-col gap-2">
                  <label className="text-sm font-semibold leading-5 text-op-text-primary">
                    Country
                  </label>
                  <div
                    className={cn(
                      SHOP_CHECKOUT_INPUT_CLASS,
                      "flex items-center justify-between opacity-80"
                    )}
                  >
                    <span>{addressCountry}</span>
                    <ChevronDown className="size-4 text-[var(--op-color-gray-550)]" />
                  </div>
                </div>

                <div className="flex flex-col gap-2">
                  <label className="text-sm font-semibold leading-5 text-op-text-primary">
                    Address line 1
                  </label>
                  <Input
                    value={addressFormLine1}
                    onChange={(e) => setAddressFormLine1(e.target.value)}
                    placeholder="Enter address line 1"
                    className={SHOP_CHECKOUT_INPUT_CLASS}
                  />
                </div>

                <div className={SHOP_PRODUCT_SPEC_DIVIDER_CLASS} />

                <div className="flex flex-col gap-2">
                  <label className="text-sm font-semibold leading-5 text-op-text-primary">
                    Address line 2
                  </label>
                  <Input
                    value={addressFormLine2}
                    onChange={(e) => setAddressFormLine2(e.target.value)}
                    placeholder="Enter address line 2"
                    className={SHOP_CHECKOUT_INPUT_CLASS}
                  />
                </div>
              </div>

              <div className="flex items-center gap-3 pt-2">
                <Button
                  type="button"
                  variant="op-secondary"
                  onClick={handleSaveAndUseAddress}
                >
                  Save and use address
                </Button>

                <Button
                  type="button"
                  variant="op-tertiary"
                  onClick={handleCancelAddAddress}
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
              variant="op-collapse"
              onClick={onBackToShop}
              aria-label="Close checkout"
            >
              <X className="size-4" />
            </Button>
          </div>

          <div className="mx-auto flex w-full max-w-[824px] flex-col items-start gap-[52px] pt-[60px] text-op-text-primary">
            <img
              src={logo}
              alt="tummly"
              width={157}
              height={38}
              className="h-[38px] w-auto max-w-[157px] object-contain brightness-0 dark:brightness-100"
            />

            <div className="flex w-full flex-col gap-8">
              <div className="flex flex-col gap-2">
                <h1 className="text-[29px] font-bold tracking-tight text-op-text-primary">
                  {pageTitle}
                </h1>
                <p className="max-w-[425px] text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
                  Review the material, QR connection, delivery details and
                  payment before placing your order.
                </p>
              </div>

            {checkoutStep === "delivery" && (
              <div className="flex w-full flex-col gap-[22px]">
                <div className={SHOP_CHECKOUT_REVIEW_CARD_CLASS}>
                  <div className="flex flex-col gap-3">
                    <h2 className="text-lg font-semibold text-op-text-primary">
                      Review your material
                    </h2>
                    <p className="text-sm font-normal text-[var(--op-color-gray-550)]">
                      Confirm the materials, quantities and selected location
                      before continuing.
                    </p>
                  </div>

                  {renderLinesSummary(false)}

                  <div className="flex items-center gap-3">
                    {canEditQuantity ? (
                      <Button
                        type="button"
                        variant="op-secondary"
                        onClick={() => setIsEditQuantityOpen(true)}
                      >
                        Edit quantity
                      </Button>
                    ) : null}

                    {locations.length > 1 && onSelectLocation ? (
                      <ShopLocationPicker
                        variant="change"
                        selectedLocationId={locationId}
                        selectedLocationName={selectedLocationName}
                        locations={locations}
                        brandLogoPublicUrl={brandLogoPublicUrl}
                        mode={mode}
                        onSelectLocation={onSelectLocation}
                        onAfterSelectLocation={(loc) => {
                          toast.success(
                            `Location switched to ${loc.locationName}`
                          )
                        }}
                      />
                    ) : (
                      <Button
                        type="button"
                        variant="op-tertiary"
                        onClick={() =>
                          toast.info(`Current location: ${selectedLocationName}`)
                        }
                      >
                        Change location
                      </Button>
                    )}
                  </div>
                </div>

                <div className={SHOP_CHECKOUT_SECTION_CARD_CLASS}>
                  <div className="flex items-center gap-5">
                    <div className={SHOP_CHECKOUT_ICON_WELL_CLASS}>
                      <Package className="size-7" />
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <h2 className="text-lg font-semibold text-op-text-primary">
                        Delivery details
                      </h2>
                      <p className="text-sm font-normal text-[var(--op-color-gray-550)]">
                        Confirm the delivery address, contact information and
                        preferred delivery method.
                      </p>
                    </div>
                  </div>

                  <div className={SHOP_PRODUCT_SPEC_DIVIDER_CLASS} />

                  <div className="flex w-full flex-col gap-3.5">
                    <h3 className="text-lg font-semibold text-op-text-primary">
                      Saved address
                    </h3>

                    <div className={SHOP_CHECKOUT_OPTION_SELECTED_CLASS}>
                      <div className="flex min-w-0 flex-1 flex-col gap-1">
                        <span className="text-sm font-medium text-op-text-primary">
                          {contactName || "Contact pending"}
                          {contactPhone ? ` · ${contactPhone}` : ""}
                        </span>
                        <span className="text-sm font-medium text-[var(--op-color-gray-550)]">
                          {selectedLocationName}
                          {shipToDisplay ? ` · ${shipToDisplay}` : ""}
                        </span>
                      </div>

                      <Button
                        type="button"
                        variant="op-tertiary"
                        className="shrink-0"
                        onClick={() => handleOpenAddAddress("delivery")}
                      >
                        Edit address
                      </Button>
                    </div>
                  </div>

                  <div className={SHOP_PRODUCT_SPEC_DIVIDER_CLASS} />

                  <div className="flex w-full flex-col gap-3.5">
                    <h3 className="text-lg font-semibold text-op-text-primary">
                      Delivery method
                    </h3>

                    <button
                      type="button"
                      onClick={() => setDeliveryMethod("standard")}
                      className={cn(
                        "cursor-pointer text-left hover:border-[var(--op-color-gray-550)]",
                        deliveryMethod === "standard"
                          ? SHOP_CHECKOUT_OPTION_SELECTED_CLASS
                          : SHOP_CHECKOUT_OPTION_DEFAULT_CLASS
                      )}
                    >
                      <div className="flex min-w-0 flex-1 flex-col gap-1 text-left">
                        <span className="text-sm font-medium text-op-text-primary">
                          Standard delivery
                        </span>
                        <span className="text-sm font-medium text-[var(--op-color-gray-550)]">
                          Delivered within 3–5 business days after dispatch.
                        </span>
                      </div>
                      <span className="shrink-0 text-sm font-medium text-[var(--op-color-gray-550)]">
                        Free
                      </span>
                    </button>

                    <button
                      type="button"
                      onClick={() => setDeliveryMethod("express")}
                      className={cn(
                        "cursor-pointer text-left hover:border-[var(--op-color-gray-550)]",
                        deliveryMethod === "express"
                          ? SHOP_CHECKOUT_OPTION_SELECTED_CLASS
                          : SHOP_CHECKOUT_OPTION_DEFAULT_CLASS
                      )}
                    >
                      <div className="flex min-w-0 flex-1 flex-col gap-1 text-left">
                        <span className="text-sm font-medium text-op-text-primary">
                          Express delivery
                        </span>
                        <span className="text-sm font-medium text-[var(--op-color-gray-550)]">
                          Delivered within 1–2 business days after dispatch.
                        </span>
                      </div>
                      <span className="shrink-0 text-sm font-medium text-[var(--op-color-gray-550)]">
                        £20.00
                      </span>
                    </button>

                    <p className="max-w-[547px] text-sm font-normal leading-normal text-[var(--op-color-gray-550)]">
                      Production time is separate from delivery time. The
                      estimated dispatch date will be confirmed after your order
                      has been processed.
                    </p>
                  </div>

                  <div className={SHOP_PRODUCT_SPEC_DIVIDER_CLASS} />

                  <div className="flex w-full flex-col gap-3.5">
                    <h3 className="text-lg font-semibold text-op-text-primary">
                      Contact
                    </h3>

                    <div className="flex flex-col gap-2">
                      <label className="text-sm font-semibold leading-5 text-op-text-primary">
                        Contact name
                      </label>
                      <Input
                        value={contactName}
                        onChange={(e) => setContactName(e.target.value)}
                        className={SHOP_CHECKOUT_INPUT_CLASS}
                      />
                    </div>

                    <div className="flex flex-col gap-2">
                      <label className="text-sm font-semibold leading-5 text-op-text-primary">
                        Phone number
                      </label>
                      <Input
                        value={contactPhone}
                        onChange={(e) => setContactPhone(e.target.value)}
                        className={SHOP_CHECKOUT_INPUT_CLASS}
                      />
                    </div>

                    <p className="text-sm font-normal text-[var(--op-color-gray-550)]">
                      The delivery provider may contact this person if there is
                      an access or delivery issue.
                    </p>
                  </div>

                  <div className={SHOP_PRODUCT_SPEC_DIVIDER_CLASS} />

                  <div className="flex w-full flex-col gap-2">
                    <label className="text-sm font-semibold leading-5 text-op-text-primary">
                      Delivery instructions — optional
                    </label>
                    <Textarea
                      value={deliveryInstructions}
                      onChange={(e) => setDeliveryInstructions(e.target.value)}
                      placeholder="Add delivery instructions"
                      rows={3}
                      className={SHOP_CHECKOUT_TEXTAREA_CLASS}
                    />
                  </div>

                  <div className={SHOP_PRODUCT_SPEC_DIVIDER_CLASS} />

                  <div>
                    <Button
                      type="button"
                      variant="op-secondary"
                      onClick={handleContinueToPayment}
                    >
                      Continue to payment
                    </Button>
                  </div>
                </div>
              </div>
            )}

            {checkoutStep === "payment" && (
              <div className="flex w-full flex-col gap-[22px]">
                <div className={SHOP_CHECKOUT_SECTION_CARD_CLASS}>
                  <div className="flex items-center gap-5">
                    <div className={SHOP_CHECKOUT_ICON_WELL_CLASS}>
                      <Package className="size-7" />
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <h2 className="text-lg font-semibold text-op-text-primary">
                        Delivery details
                      </h2>
                      <p className="text-sm font-normal text-[var(--op-color-gray-550)]">
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
                      variant="op-tertiary"
                      onClick={() => handleOpenAddAddress("payment")}
                    >
                      Edit
                    </Button>
                  </div>
                </div>

                <div className={SHOP_CHECKOUT_SECTION_CARD_CLASS}>
                  <div className="flex items-center gap-5">
                    <div className={SHOP_CHECKOUT_ICON_WELL_CLASS}>
                      <CreditCard className="size-7" />
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <h2 className="text-lg font-semibold text-op-text-primary">
                        Payment details
                      </h2>
                      <p className="text-sm font-normal text-[var(--op-color-gray-550)]">
                        Choose a payment method, confirm the invoice details and
                        place your order.
                      </p>
                    </div>
                  </div>

                  <div className={SHOP_PRODUCT_SPEC_DIVIDER_CLASS} />

                  <div className="flex flex-col gap-3.5">
                    <h3 className="text-lg font-semibold text-op-text-primary">
                      Payment
                    </h3>

                    <div className={SHOP_CHECKOUT_OPTION_DEFAULT_CLASS}>
                      <div className="flex flex-col gap-1 text-left">
                        <p className="text-sm font-medium text-op-text-primary">
                          Pay securely with Revolut
                        </p>
                        <p className="text-sm text-[var(--op-color-gray-550)]">
                          After you place the order, you are redirected to
                          Revolut to complete payment. Your order is marked paid
                          only after Revolut confirms payment.
                        </p>
                      </div>
                    </div>
                  </div>
                </div>

                <div className={SHOP_CHECKOUT_REVIEW_CARD_CLASS}>
                  <div className="flex flex-col gap-3">
                    <h2 className="text-lg font-semibold text-op-text-primary">
                      Order summary
                    </h2>
                    <p className="text-sm font-normal text-[var(--op-color-gray-550)]">
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
                  <span className="text-sm font-medium leading-snug text-op-text-primary">
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
                  <span className="text-sm font-medium leading-snug text-op-text-primary">
                    I agree to the Tummly Shop terms and understand that
                    made-to-order materials cannot normally be changed after
                    production begins.
                  </span>
                </label>

                <div className="flex flex-col gap-3 pt-2">
                  <div className="flex flex-wrap items-center gap-4">
                    <Button
                      type="button"
                      variant="op-primary"
                      disabled={isPaymentProcessing || purchaseBlocked}
                      onClick={() => {
                        void handlePlaceOrder()
                      }}
                    >
                      {isPaymentProcessing
                        ? "Processing..."
                        : "Continue to payment"}
                    </Button>

                    <Button
                      type="button"
                      variant="op-tertiary"
                      onClick={handleBackToDelivery}
                    >
                      Back
                    </Button>
                  </div>

                  {purchaseBlocked ? (
                    <ShopPaidWriteHelperNote chrome={paidWriteChrome} />
                  ) : null}
                </div>
              </div>
            )}
            </div>
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

import { describe, expect, it } from "vitest"

import {
  shopDeliveryMethodLabel,
  shopPaymentBadgeVariant,
} from "@/lib/operatorShop/shopOrderDetailPresentation"

describe("shopPaymentBadgeVariant", () => {
  it("shows paid as positive", () => {
    expect(shopPaymentBadgeVariant({ paymentStatus: "paid" })).toBe("positive")
  })

  it("shows payment failed as negative", () => {
    expect(shopPaymentBadgeVariant({ paymentStatus: "payment_failed" })).toBe(
      "negative"
    )
  })

  it("shows awaiting payment as a warning", () => {
    expect(shopPaymentBadgeVariant({ paymentStatus: "awaiting_payment" })).toBe(
      "neutral"
    )
  })

  it("shows refunded as the muted tag", () => {
    expect(shopPaymentBadgeVariant({ paymentStatus: "refunded" })).toBe("tag")
  })

  it("shows a complimentary paid order as the muted tag, not green", () => {
    expect(
      shopPaymentBadgeVariant({ paymentStatus: "paid", isComplimentary: true })
    ).toBe("tag")
  })

  it("falls back to the display label when the raw status is missing", () => {
    expect(shopPaymentBadgeVariant({ paymentLabel: "Paid" })).toBe("positive")
    expect(shopPaymentBadgeVariant({ paymentLabel: "Payment failed" })).toBe(
      "negative"
    )
    expect(shopPaymentBadgeVariant({ paymentLabel: "Free" })).toBe("tag")
  })

  it("uses the muted tag for an unknown status", () => {
    expect(shopPaymentBadgeVariant({ paymentStatus: "something_new" })).toBe(
      "tag"
    )
  })
})

describe("shopDeliveryMethodLabel", () => {
  it("names old express orders", () => {
    expect(shopDeliveryMethodLabel("express")).toBe("Express delivery")
    expect(shopDeliveryMethodLabel("Express")).toBe("Express delivery")
  })

  it("names standard orders", () => {
    expect(shopDeliveryMethodLabel("standard")).toBe("Standard delivery")
  })

  it("defaults to standard when the method is missing or unknown", () => {
    expect(shopDeliveryMethodLabel(undefined)).toBe("Standard delivery")
    expect(shopDeliveryMethodLabel("")).toBe("Standard delivery")
    expect(shopDeliveryMethodLabel("pigeon")).toBe("Standard delivery")
  })
})

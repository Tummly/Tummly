import { describe, expect, it, vi } from "vitest"

import type { ShopOrderWire } from "@/api/shopOrdersApi"
import { resolveShopOrderOnPaymentReturn } from "@/lib/operatorShop/resolveShopOrderOnPaymentReturn"

function order(paymentStatus: string): ShopOrderWire {
  return {
    id: "order-1",
    orderNumber: "TM-1001",
    locationId: 9,
    locationName: "Main",
    paymentStatus,
    fulfilmentStatus: null,
    deliveryMethod: "standard",
    materialsNetPence: 2400,
    vatPence: 0,
    deliveryNetPence: 0,
    grossPence: 2400,
    currency: "GBP",
    lines: [],
    shipTo: {
      contactName: "Ada",
      addressLine1: "1 Road",
      postcode: "SW1A 1AA",
      country: "United Kingdom",
    },
  }
}

describe("resolveShopOrderOnPaymentReturn", () => {
  it("returns the confirmed order without polling when confirm reports paid", async () => {
    const poll = vi.fn(async () => order("paid"))

    const result = await resolveShopOrderOnPaymentReturn({
      confirmPayment: async () => order("paid"),
      pollUntilPaid: poll,
    })

    expect(result?.paymentStatus).toBe("paid")
    expect(poll).not.toHaveBeenCalled()
  })

  it("polls when confirm reports the order is still not paid", async () => {
    const poll = vi.fn(async () => order("paid"))

    const result = await resolveShopOrderOnPaymentReturn({
      confirmPayment: async () => order("awaiting_payment"),
      pollUntilPaid: poll,
    })

    expect(poll).toHaveBeenCalledOnce()
    expect(result?.paymentStatus).toBe("paid")
  })

  it("still polls when confirm fails", async () => {
    const poll = vi.fn(async () => order("paid"))

    const result = await resolveShopOrderOnPaymentReturn({
      confirmPayment: async () => {
        throw new Error("network")
      },
      pollUntilPaid: poll,
    })

    expect(poll).toHaveBeenCalledOnce()
    expect(result?.paymentStatus).toBe("paid")
  })

  it("resolves to null instead of throwing when polling fails", async () => {
    const result = await resolveShopOrderOnPaymentReturn({
      confirmPayment: async () => order("awaiting_payment"),
      pollUntilPaid: async () => {
        throw new Error("network")
      },
    })

    expect(result).toBeNull()
  })

  it("resolves to null when the order never becomes paid", async () => {
    const result = await resolveShopOrderOnPaymentReturn({
      confirmPayment: async () => order("awaiting_payment"),
      pollUntilPaid: async () => null,
    })

    expect(result).toBeNull()
  })
})

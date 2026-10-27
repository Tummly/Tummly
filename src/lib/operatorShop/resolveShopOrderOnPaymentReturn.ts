import type { ShopOrderWire } from "@/api/shopOrdersApi"

async function settle<T>(run: () => Promise<T>): Promise<T | null> {
  try {
    return await run()
  } catch {
    return null
  }
}

/**
 * Resolves the paid order after Revolut returns the operator to the Shop.
 * Confirms with the server first. Polls the order only when confirm did not
 * return a paid order. Never throws, so the caller always leaves the
 * confirming state.
 */
export async function resolveShopOrderOnPaymentReturn(input: {
  confirmPayment: () => Promise<ShopOrderWire>
  pollUntilPaid: () => Promise<ShopOrderWire | null>
}): Promise<ShopOrderWire | null> {
  const confirmed = await settle(input.confirmPayment)
  if (confirmed?.paymentStatus === "paid") {
    return confirmed
  }
  return settle(input.pollUntilPaid)
}

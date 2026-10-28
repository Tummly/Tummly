import axios from "axios"

import type { AdminShopCatalogSku } from "@/api/adminShopCatalogApi"

export const APPROVAL_REFERENCE_MAX_LENGTH = 200

/**
 * Immutable availability update for the catalog list. Used for optimistic
 * toggles and for rolling a toggle back when the request fails.
 */
export function withSkuAvailableInShop(
  skus: AdminShopCatalogSku[],
  skuId: string,
  availableInShop: boolean
): AdminShopCatalogSku[] {
  return skus.map((row) =>
    row.skuId === skuId ? { ...row, availableInShop } : row
  )
}

/** Copy for the confirmation dialog that precedes an availability change. */
export function availabilityConfirmCopy(availableInShop: boolean): {
  title: string
  description: string
  actionLabel: string
} {
  if (availableInShop) {
    return {
      title: "Turn on in Shop?",
      description:
        "Customers will be able to buy this product. The server checks the readiness rules before it turns on.",
      actionLabel: "Turn on",
    }
  }

  return {
    title: "Set to Coming soon?",
    description:
      "Customers will not be able to buy this product until it is turned on again.",
    actionLabel: "Set to Coming soon",
  }
}

/** Blank approval references are sent as no reference. */
export function toApprovalReference(input: string): string | undefined {
  const trimmed = input.trim()
  return trimmed.length === 0 ? undefined : trimmed
}

/** Readiness blockers the server returns when an availability change is refused. */
export function availabilityBlockersFromError(error: unknown): string[] {
  if (!axios.isAxiosError(error)) return []

  const blockers = (error.response?.data as { blockers?: unknown } | undefined)
    ?.blockers
  if (!Array.isArray(blockers)) return []

  return blockers.filter((item): item is string => typeof item === "string")
}

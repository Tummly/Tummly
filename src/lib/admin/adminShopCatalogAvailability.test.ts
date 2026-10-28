import axios, { AxiosError, AxiosHeaders } from "axios"
import { describe, expect, it } from "vitest"

import type { AdminShopCatalogSku } from "@/api/adminShopCatalogApi"
import {
  APPROVAL_REFERENCE_MAX_LENGTH,
  availabilityBlockersFromError,
  availabilityConfirmCopy,
  toApprovalReference,
  withSkuAvailableInShop,
} from "./adminShopCatalogAvailability"

function sku(skuId: string, availableInShop: boolean): AdminShopCatalogSku {
  return { skuId, availableInShop } as AdminShopCatalogSku
}

function axiosErrorWithData(data: unknown): AxiosError {
  return new AxiosError(
    "Request failed",
    "ERR_BAD_REQUEST",
    undefined,
    undefined,
    {
      status: 400,
      statusText: "Bad Request",
      data,
      headers: {},
      config: { headers: new AxiosHeaders() },
    }
  )
}

describe("withSkuAvailableInShop", () => {
  it("returns a new list with only the matching SKU changed", () => {
    const before = [sku("a", false), sku("b", false)]

    const after = withSkuAvailableInShop(before, "b", true)

    expect(after).not.toBe(before)
    expect(after[0]).toBe(before[0])
    expect(after[1]).toEqual(expect.objectContaining({ skuId: "b", availableInShop: true }))
    expect(before[1]!.availableInShop).toBe(false)
  })

  it("can revert a SKU to its previous value", () => {
    const toggled = withSkuAvailableInShop([sku("a", false)], "a", true)

    const reverted = withSkuAvailableInShop(toggled, "a", false)

    expect(reverted[0]!.availableInShop).toBe(false)
  })

  it("leaves the list unchanged when no SKU matches", () => {
    const before = [sku("a", false)]

    expect(withSkuAvailableInShop(before, "missing", true)).toEqual(before)
  })
})

describe("availabilityConfirmCopy", () => {
  it("describes buying going live when switching on", () => {
    const copy = availabilityConfirmCopy(true)

    expect(copy.title).toBe("Turn on in Shop?")
    expect(copy.actionLabel).toBe("Turn on")
  })

  it("describes the product staying unbuyable when switching off", () => {
    const copy = availabilityConfirmCopy(false)

    expect(copy.title).toBe("Set to Coming soon?")
    expect(copy.description).toContain("will not be able to buy")
  })
})

describe("toApprovalReference", () => {
  it("trims the reference and drops a blank one", () => {
    expect(toApprovalReference("  CHG-1042  ")).toBe("CHG-1042")
    expect(toApprovalReference("   ")).toBeUndefined()
    expect(toApprovalReference("")).toBeUndefined()
  })

  it("matches the server limit", () => {
    expect(APPROVAL_REFERENCE_MAX_LENGTH).toBe(200)
  })
})

describe("availabilityBlockersFromError", () => {
  it("returns the readiness blockers from a refused switch-on", () => {
    const error = axiosErrorWithData({
      success: false,
      code: "sku_not_ready",
      blockers: ["Add a supplier name.", "Set a delivery price above £0.00."],
    })

    expect(axios.isAxiosError(error)).toBe(true)
    expect(availabilityBlockersFromError(error)).toEqual([
      "Add a supplier name.",
      "Set a delivery price above £0.00.",
    ])
  })

  it("returns no blockers for other failures", () => {
    expect(availabilityBlockersFromError(axiosErrorWithData({ message: "Forbidden" }))).toEqual([])
    expect(availabilityBlockersFromError(new Error("network"))).toEqual([])
  })

  it("ignores non-text entries in the blocker list", () => {
    const error = axiosErrorWithData({ blockers: ["Add a supplier name.", 7, null] })

    expect(availabilityBlockersFromError(error)).toEqual(["Add a supplier name."])
  })
})

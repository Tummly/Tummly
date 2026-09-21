import { describe, expect, it } from "vitest"

import { shopOrderCancelBlockMessage } from "./shopOrderCancelBlockMessage"

describe("shopOrderCancelBlockMessage", () => {
  it("maps known cancel block reasons to operator copy", () => {
    expect(shopOrderCancelBlockMessage("in_transit")).toContain("dispatched")
    expect(shopOrderCancelBlockMessage("delivered")).toContain("delivered")
    expect(shopOrderCancelBlockMessage("production_started")).toBe(
      "Production has started for this order, so it cannot be cancelled here. Contact Tummly support if you need help."
    )
  })

  it("returns null for unknown or empty reasons", () => {
    expect(shopOrderCancelBlockMessage(null)).toBeNull()
    expect(shopOrderCancelBlockMessage(undefined)).toBeNull()
    expect(shopOrderCancelBlockMessage("")).toBeNull()
    expect(shopOrderCancelBlockMessage("other")).toBeNull()
  })
})

import { describe, expect, it } from "vitest"

import { withRowBusy } from "./adminShopOrderRowBusy"

describe("withRowBusy", () => {
  it("adds an order id without dropping ids already busy", () => {
    const first = withRowBusy(new Set(), "order-a", true)
    const second = withRowBusy(first, "order-b", true)

    expect([...second].sort()).toEqual(["order-a", "order-b"])
  })

  it("removes only the finished order id", () => {
    const busy = new Set(["order-a", "order-b"])

    const after = withRowBusy(busy, "order-a", false)

    expect([...after]).toEqual(["order-b"])
  })

  it("returns the same set when nothing changes", () => {
    const busy = new Set(["order-a"])

    expect(withRowBusy(busy, "order-a", true)).toBe(busy)
    expect(withRowBusy(busy, "order-z", false)).toBe(busy)
  })

  it("does not mutate the input set", () => {
    const busy = new Set(["order-a"])

    withRowBusy(busy, "order-b", true)

    expect([...busy]).toEqual(["order-a"])
  })
})

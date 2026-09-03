import { describe, expect, it } from "vitest"

import {
  adminShopFulfilmentLabel,
  canEditAdminShopOpsNotes,
  canEditAdminShopTrackingUrl,
  nextAdminShopFulfilmentAction,
} from "@/lib/adminShopOrderFulfilment"

describe("adminShopOrderFulfilment", () => {
  it("maps API statuses to operator-facing labels", () => {
    expect(adminShopFulfilmentLabel("processing")).toBe("Processing")
    expect(adminShopFulfilmentLabel("in_transit")).toBe("Dispatched")
    expect(adminShopFulfilmentLabel("delivered")).toBe("Delivered")
  })

  it("exposes the next legal fulfilment action", () => {
    expect(nextAdminShopFulfilmentAction("processing")).toEqual({
      status: "in_transit",
      label: "Mark as Dispatched",
    })
    expect(nextAdminShopFulfilmentAction("in_transit")).toEqual({
      status: "delivered",
      label: "Mark as Delivered",
    })
    expect(nextAdminShopFulfilmentAction("delivered")).toBeNull()
    expect(nextAdminShopFulfilmentAction("cancelled")).toBeNull()
  })

  it("limits tracking and notes edits by status", () => {
    expect(canEditAdminShopTrackingUrl("processing")).toBe(true)
    expect(canEditAdminShopTrackingUrl("in_transit")).toBe(true)
    expect(canEditAdminShopTrackingUrl("delivered")).toBe(false)
    expect(canEditAdminShopOpsNotes("cancelled")).toBe(false)
    expect(canEditAdminShopOpsNotes("delivered")).toBe(true)
  })
})

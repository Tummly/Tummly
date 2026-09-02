import { describe, expect, it } from "vitest"
import { resolveShopPaidWriteChrome } from "./shopPaidWriteChrome"

const baseContext = {
  billingStatus: "Active",
  subscriptionPlan: "Starter",
  accessLevel: "manage" as const,
  permissionRole: "Owner",
  mode: "multi" as const,
  locationId: 42,
}

describe("resolveShopPaidWriteChrome", () => {
  it("allows purchases when billing is active", () => {
    expect(resolveShopPaidWriteChrome(baseContext)).toEqual({
      purchaseDisabled: false,
      helperCta: null,
    })
  })

  it("blocks purchases under Soft lock with Choose a plan for Pilot Owner", () => {
    const chrome = resolveShopPaidWriteChrome({
      ...baseContext,
      billingStatus: "Soft lock",
      subscriptionPlan: "Pilot",
    })
    expect(chrome.purchaseDisabled).toBe(true)
    expect(chrome.helperCta?.label).toBe("Choose a plan")
  })

  it("blocks purchases under Dormant with Update payment method for Billing Admin", () => {
    const chrome = resolveShopPaidWriteChrome({
      ...baseContext,
      billingStatus: "Dormant",
      subscriptionPlan: "Starter",
      permissionRole: "Billing Admin",
    })
    expect(chrome.purchaseDisabled).toBe(true)
    expect(chrome.helperCta?.label).toBe("Update payment method")
  })

  it("blocks chargeback without helper", () => {
    expect(
      resolveShopPaidWriteChrome({
        ...baseContext,
        chargebackRestricted: true,
      })
    ).toEqual({
      purchaseDisabled: true,
      helperCta: null,
    })
  })
})

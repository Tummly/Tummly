import { describe, expect, it } from "vitest"

import {
  isAccountLockedBillingStatus,
  resolveLockAlertPresentation,
  resolveLockRestorationCause,
} from "./lockAlertPresentation"

describe("lockAlertPresentation", () => {
  const base = {
    mode: "multi" as const,
    locationId: 10,
    accessLevel: "manage" as const,
    permissionRole: "Owner",
  }

  it("returns Soft lock Pilot body and Choose a plan for Owner Manage", () => {
    const alert = resolveLockAlertPresentation({
      ...base,
      billingStatus: "Soft lock",
      subscriptionPlan: "Pilot",
      isPilot: true,
    })

    expect(alert).toEqual({
      title: "Soft lock",
      body: "Your Pilot period has ended. Paid actions are paused. Existing Feedback links stay live.",
      buttonLabel: "Choose a plan",
      buttonHref:
        "/multi-dashboard/settings/billing-credits/manage-plan?location=10#plan-cards",
    })
  })

  it("returns Soft lock dunning body and Update payment method for Billing Admin Manage", () => {
    const alert = resolveLockAlertPresentation({
      ...base,
      billingStatus: "Soft lock",
      subscriptionPlan: "Growth",
      isPilot: false,
      permissionRole: "Billing Admin",
    })

    expect(alert).toEqual({
      title: "Soft lock",
      body: "Payment failed. Paid actions are paused. Existing Feedback links stay live.",
      buttonLabel: "Update payment method",
      buttonHref:
        "/multi-dashboard/settings/billing-credits?location=10&tab=payment-invoices#update-payment-method",
    })
  })

  it("returns Dormant Pilot body without Button for View", () => {
    const alert = resolveLockAlertPresentation({
      ...base,
      billingStatus: "Dormant",
      subscriptionPlan: "Pilot",
      isPilot: true,
      accessLevel: "view",
      permissionRole: "Admin",
    })

    expect(alert).toEqual({
      title: "Dormant",
      body: "Your account is Dormant. Guest QR links show as unavailable.",
      buttonLabel: null,
      buttonHref: null,
    })
  })

  it("returns Dormant dunning Alert without Button for No access", () => {
    const alert = resolveLockAlertPresentation({
      ...base,
      billingStatus: "Dormant",
      subscriptionPlan: "Growth",
      accessLevel: "none",
      permissionRole: "Staff",
    })

    expect(alert).toEqual({
      title: "Dormant",
      body: "Payment failed. Your account is Dormant. Guest QR links show as unavailable.",
      buttonLabel: null,
      buttonHref: null,
    })
  })

  it("hides Choose a plan for Billing Admin on unpaid Pilot lock", () => {
    const alert = resolveLockAlertPresentation({
      ...base,
      billingStatus: "Soft lock",
      subscriptionPlan: "Pilot",
      isPilot: true,
      permissionRole: "Billing Admin",
    })

    expect(alert?.buttonLabel).toBeNull()
    expect(alert?.buttonHref).toBeNull()
  })

  it("returns null when Billing status is not Soft lock or Dormant", () => {
    expect(
      resolveLockAlertPresentation({
        ...base,
        billingStatus: "Active",
        subscriptionPlan: "Growth",
      })
    ).toBeNull()
    expect(isAccountLockedBillingStatus("Past due")).toBe(false)
    expect(resolveLockRestorationCause({
      billingStatus: "Pilot",
      subscriptionPlan: "Pilot",
    })).toBeNull()
  })
})

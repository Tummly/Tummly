import { describe, expect, it } from "vitest"
import {
  billingAlertEventKindForCreditThreshold,
  resolveBillingAlertNotificationCta,
} from "@/lib/operatorBillingCredits/billingAlertNotificationPresentation"

describe("resolveBillingAlertNotificationCta", () => {
  const locationId = 42

  it("credit 80/90 returns View usage for view and manage", () => {
    for (const accessLevel of ["view", "manage"] as const) {
      const cta = resolveBillingAlertNotificationCta({
        eventKind: "credit-threshold-80-or-90",
        accessLevel,
        permissionRole: accessLevel === "manage" ? "Owner" : "Admin",
        mode: "single",
        locationId,
      })

      expect(cta.label).toBe("View usage")
      expect(cta.href).toBe(
        "/single-dashboard/settings/billing-credits?location=42&tab=credits-usage"
      )
    }
  })

  it("credit 100 paid returns Buy channel credits when user can write", () => {
    const cta = resolveBillingAlertNotificationCta({
      eventKind: "credit-threshold-100-paid",
      accessLevel: "manage",
      permissionRole: "Billing Admin",
      mode: "multi",
      locationId,
      channel: "email",
    })

    expect(cta.label).toBe("Buy Email credits")
    expect(cta.href).toBe(
      "/multi-dashboard/settings/billing-credits/manage-plan?location=42&section=credit-top-ups&channel=email"
    )
  })

  it("credit 100 paid returns View usage when user has view only", () => {
    const cta = resolveBillingAlertNotificationCta({
      eventKind: "credit-threshold-100-paid",
      accessLevel: "view",
      permissionRole: "Marketing",
      mode: "single",
      locationId,
      channel: "sms",
    })

    expect(cta.label).toBe("View usage")
  })

  it("credit 100 pilot returns Change plan for owner with manage", () => {
    const cta = resolveBillingAlertNotificationCta({
      eventKind: "credit-threshold-100-pilot",
      accessLevel: "manage",
      permissionRole: "Owner",
      mode: "single",
      locationId,
    })

    expect(cta.label).toBe("Change plan")
    expect(cta.href).toBe(
      "/single-dashboard/settings/billing-credits/manage-plan?location=42"
    )
  })

  it("credit 100 pilot returns View usage for non-owner with manage", () => {
    const cta = resolveBillingAlertNotificationCta({
      eventKind: "credit-threshold-100-pilot",
      accessLevel: "manage",
      permissionRole: "Billing Admin",
      mode: "single",
      locationId,
    })

    expect(cta.label).toBe("View usage")
  })

  it("dunning returns Update payment method when user can write", () => {
    const cta = resolveBillingAlertNotificationCta({
      eventKind: "payment-failure-dunning",
      accessLevel: "manage",
      permissionRole: "Owner",
      mode: "single",
      locationId,
    })

    expect(cta.label).toBe("Update payment method")
    expect(cta.href).toContain("tab=payment-invoices")
  })

  it("dunning returns Payment & invoices when user has view only", () => {
    const cta = resolveBillingAlertNotificationCta({
      eventKind: "payment-failure-dunning",
      accessLevel: "view",
      permissionRole: "Admin",
      mode: "single",
      locationId,
    })

    expect(cta.label).toBe("Payment & invoices")
  })

  it("unpaid pilot lock returns Choose a plan for owner with manage", () => {
    const cta = resolveBillingAlertNotificationCta({
      eventKind: "unpaid-pilot-lock",
      accessLevel: "manage",
      permissionRole: "Owner",
      mode: "single",
      locationId,
    })

    expect(cta.label).toBe("Choose a plan")
    expect(cta.href).toContain("tab=plan-subscription")
  })

  it("no access returns null CTA", () => {
    const cta = resolveBillingAlertNotificationCta({
      eventKind: "credit-threshold-80-or-90",
      accessLevel: "none",
      permissionRole: "Staff",
      mode: "single",
      locationId,
    })

    expect(cta.label).toBeNull()
    expect(cta.href).toBeNull()
  })
})

describe("billingAlertEventKindForCreditThreshold", () => {
  it("maps 80 and 90 to shared kind", () => {
    expect(billingAlertEventKindForCreditThreshold(80, false)).toBe(
      "credit-threshold-80-or-90"
    )
    expect(billingAlertEventKindForCreditThreshold(90, true)).toBe(
      "credit-threshold-80-or-90"
    )
  })

  it("maps 100 to pilot or paid kind", () => {
    expect(billingAlertEventKindForCreditThreshold(100, true)).toBe(
      "credit-threshold-100-pilot"
    )
    expect(billingAlertEventKindForCreditThreshold(100, false)).toBe(
      "credit-threshold-100-paid"
    )
  })
})

import { describe, expect, it } from "vitest"

import type { PlanSubscriptionSnapshot } from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"
import {
  buildManagePlanCardViewModels,
  buildPlanChangeConfirmCopy,
  defaultPreviewCadence,
  resolvePlanCardCta,
  resolvePlanChangeKind,
} from "@/lib/operatorBillingCredits/managePlanPresentation"

function paidPlan(
  overrides: Partial<PlanSubscriptionSnapshot> = {}
): PlanSubscriptionSnapshot {
  return {
    subscriptionPlan: "Growth",
    billingStatus: "Active",
    renewalDateLabel: "Renews 12 Aug 2026",
    emailCreditsRemaining: 1000,
    smsCreditsRemaining: 100,
    aiCreditsRemaining: 100,
    billingCycle: "Monthly",
    planPriceNet: "£99",
    includedLocations: 3,
    activeLocations: 2,
    includedEmailCreditsLabel: "10,000 / month",
    includedSmsCreditsLabel: "350 / month",
    includedAiCreditsLabel: "500 / month",
    starterKitState: "unused",
    pricebookId: "guest-loop-mvp-2026-07",
    scheduledChangeLine: null,
    isPilot: false,
    ...overrides,
  }
}

describe("resolvePlanCardCta", () => {
  it("marks Pilot as current plan on Pilot", () => {
    expect(
      resolvePlanCardCta({
        cardPlanId: "Pilot",
        currentPlanId: "Pilot",
        isPilot: true,
        liveCadence: null,
        previewCadence: "monthly",
      })
    ).toEqual({
      kind: "current",
      label: "Current plan",
      disabled: true,
    })
  })

  it("disables Pilot as a downgrade target on paid plans", () => {
    expect(
      resolvePlanCardCta({
        cardPlanId: "Pilot",
        currentPlanId: "Starter",
        isPilot: false,
        liveCadence: "monthly",
        previewCadence: "monthly",
      }).disabled
    ).toBe(true)
  })

  it("offers switch cadence on the current paid card when preview differs", () => {
    expect(
      resolvePlanCardCta({
        cardPlanId: "Growth",
        currentPlanId: "Growth",
        isPilot: false,
        liveCadence: "monthly",
        previewCadence: "annual",
      })
    ).toEqual({
      kind: "action",
      label: "Switch to annual",
      disabled: false,
      changeKind: "cadence-only",
    })
  })
})

describe("buildManagePlanCardViewModels", () => {
  it("shows pack prices and channel credit rows", () => {
    const cards = buildManagePlanCardViewModels({
      plan: paidPlan({ subscriptionPlan: "Pilot", isPilot: true }),
      previewCadence: "monthly",
    })

    expect(cards.map((card) => card.id)).toEqual([
      "Pilot",
      "Starter",
      "Growth",
      "Group",
    ])
    expect(cards[1]?.priceHeadline).toBe("£39 / month + VAT")
    expect(cards[1]?.emailCreditsLabel).toBe("2,500 / month")
    expect(cards[2]?.annualSaveLabel).toBeNull()
  })

  it("shows annual save when preview cadence is annual", () => {
    const cards = buildManagePlanCardViewModels({
      plan: paidPlan(),
      previewCadence: "annual",
    })

    expect(cards[1]?.priceHeadline).toBe("£398 / year + VAT")
    expect(cards[1]?.annualSaveLabel).toBe("Save £70 per year")
  })
})

describe("resolvePlanChangeKind", () => {
  it("treats plan and cadence changes as schedule-only", () => {
    expect(
      resolvePlanChangeKind({
        currentPlanId: "Starter",
        targetPlanId: "Growth",
        liveCadence: "monthly",
        previewCadence: "annual",
      })
    ).toBe("plan-and-cadence")
  })
})

describe("buildPlanChangeConfirmCopy", () => {
  it("uses pay copy for Pilot conversion", () => {
    expect(
      buildPlanChangeConfirmCopy({
        targetPlanId: "Starter",
        changeKind: "convert",
        previewCadence: "monthly",
        renewalDateLabel: null,
      }).requiresPay
    ).toBe(true)
  })

  it("uses schedule copy for downgrades", () => {
    const copy = buildPlanChangeConfirmCopy({
      targetPlanId: "Starter",
      changeKind: "downgrade",
      previewCadence: "monthly",
      renewalDateLabel: "Renews 12 Aug 2026",
    })
    expect(copy.requiresPay).toBe(false)
    expect(copy.body).toContain("12 Aug 2026")
  })
})

describe("defaultPreviewCadence", () => {
  it("defaults Pilot preview to monthly", () => {
    expect(
      defaultPreviewCadence(
        paidPlan({ subscriptionPlan: "Pilot", isPilot: true, billingCycle: null })
      )
    ).toBe("monthly")
  })
})

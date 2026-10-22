import { describe, expect, it } from "vitest"

import type { PlanSubscriptionSnapshot } from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"
import {
  buildManagePlanCardViewModels,
  buildManagePlanFaqItems,
  buildPlanChangeConfirmCopy,
  buildPlanRenewalDateMetric,
  defaultPreviewCadence,
  MANAGE_PLAN_CADENCE_ITEM_ACTIVE_CLASS,
  MANAGE_PLAN_CADENCE_ITEM_CLASS,
  MANAGE_PLAN_CADENCE_SHELL_CLASS,
  MANAGE_PLAN_FAQ_TRIGGER_CLASS,
  resolvePlanCardCta,
  resolvePlanChangeKind,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import { BILLING_CREDITS_PAGE_COPY } from "@/lib/operatorBillingCredits/billingCreditsPresentation"

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

  it("enables Start Pilot on Free", () => {
    expect(
      resolvePlanCardCta({
        cardPlanId: "Pilot",
        currentPlanId: "Free",
        isPilot: false,
        liveCadence: null,
        previewCadence: "monthly",
      })
    ).toEqual({
      kind: "action",
      label: "Start 30-day Pilot",
      disabled: false,
      changeKind: "convert",
    })
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
      vatRateBps: 2000,
    })

    expect(cards.map((card) => card.id)).toEqual([
      "Pilot",
      "Starter",
      "Growth",
      "Group",
    ])
    expect(cards[1]?.priceHeadline).toBe("£39 / month + VAT")
    expect(cards[1]?.emailCreditsLabel).toBe("2,500/month")
    expect(cards[1]?.annualSaveLabel).toBe("Save £70 per year")
    expect(cards[2]?.isMostPopular).toBe(true)
    expect(cards[2]?.coreFeatures.length).toBeGreaterThan(0)
    expect(cards[0]?.priceSubline).toBe("No payment card required")
  })

  it("hides annual save when preview cadence is annual", () => {
    const cards = buildManagePlanCardViewModels({
      plan: paidPlan(),
      previewCadence: "annual",
      vatRateBps: 2000,
    })

    expect(cards[1]?.priceHeadline).toBe("£398 / year + VAT")
    expect(cards[1]?.annualSaveLabel).toBeNull()
    expect(cards[1]?.priceSubline).toBeNull()
  })

  it("omits + VAT from price headlines when vatRateBps is 0", () => {
    const cards = buildManagePlanCardViewModels({
      plan: paidPlan({ subscriptionPlan: "Pilot", isPilot: true }),
      previewCadence: "monthly",
      vatRateBps: 0,
    })

    expect(cards[1]?.priceHeadline).toBe("£39 / month")
    expect(cards[1]?.priceSuffix).toBe("/ month")
  })
})

describe("buildManagePlanFaqItems", () => {
  it("includes + VAT on group location copy when vatRateBps is active", () => {
    const group = buildManagePlanFaqItems({ vatRateBps: 2000 }).find(
      (item) => item.id === "group-locations"
    )
    expect(group?.answerParagraphs[1]).toBe(
      "Additional Group Locations can be added for £39/month + VAT each, or £398/year + VAT on Annual."
    )
    expect(
      buildManagePlanFaqItems({ vatRateBps: 2000 }).some(
        (item) => item.id === "vat"
      )
    ).toBe(true)
  })

  it("omits + VAT and VAT FAQ when vatRateBps is 0", () => {
    const items = buildManagePlanFaqItems({ vatRateBps: 0 })
    const group = items.find((item) => item.id === "group-locations")
    expect(group?.answerParagraphs[1]).toBe(
      "Additional Group Locations can be added for £39/month each, or £398/year on Annual."
    )
    expect(items.some((item) => item.id === "vat")).toBe(false)
  })

  it("FAQ starter kit items describe one kit per Active Location", () => {
    const items = buildManagePlanFaqItems({ vatRateBps: 0 })
    const perLocation = items.find((i) => i.id === "starter-kit-per-location")
    expect(perLocation?.answerParagraphs.join(" ")).toMatch(/Active Location/i)
    expect(perLocation?.answerParagraphs.join(" ")).not.toMatch(
      /Billing Account lifetime/i
    )

    const qr = items.find((i) => i.id === "qr-materials")
    expect(qr?.answerParagraphs.join(" ")).not.toMatch(
      /Billing Account lifetime/i
    )
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
        currentPlanId: "Pilot",
        targetPlanId: "Starter",
        changeKind: "convert",
        previewCadence: "monthly",
        renewalDateLabel: null,
      }).requiresPay
    ).toBe(true)
  })

  it("uses activate copy for Free to Pilot", () => {
    const copy = buildPlanChangeConfirmCopy({
      currentPlanId: "Free",
      targetPlanId: "Pilot",
      changeKind: "convert",
      previewCadence: "monthly",
      renewalDateLabel: null,
    })
    expect(copy.requiresPay).toBe(false)
    expect(copy.primaryLabel).toBe("Start 30-day Pilot")
  })

  it("uses schedule copy for downgrades", () => {
    const copy = buildPlanChangeConfirmCopy({
      currentPlanId: "Growth",
      targetPlanId: "Starter",
      changeKind: "downgrade",
      previewCadence: "monthly",
      renewalDateLabel: "Renews 12 Aug 2026",
    })
    expect(copy.requiresPay).toBe(false)
    expect(copy.body).toContain("12 Aug 2026")
  })

  it("uses upgrade copy for plan and cadence upgrades", () => {
    expect(
      buildPlanChangeConfirmCopy({
        currentPlanId: "Starter",
        targetPlanId: "Growth",
        changeKind: "plan-and-cadence",
        previewCadence: "annual",
        renewalDateLabel: "Renews 12 Aug 2026",
      }).title
    ).toBe("Upgrade to Growth")
  })
})

describe("plan and cadence upgrade CTAs", () => {
  it("labels plan and cadence upgrade CTAs as upgrades", () => {
    const cards = buildManagePlanCardViewModels({
      plan: paidPlan({ subscriptionPlan: "Starter", billingCycle: "Monthly" }),
      previewCadence: "annual",
    })

    expect(cards.find((card) => card.id === "Growth")?.cta.label).toBe(
      "Upgrade to Growth"
    )
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

describe("buildPlanRenewalDateMetric", () => {
  it("shows renewal date before cancel is scheduled", () => {
    expect(
      buildPlanRenewalDateMetric(paidPlan(), {
        renewalDate: BILLING_CREDITS_PAGE_COPY.renewalDate,
        cancelDate: BILLING_CREDITS_PAGE_COPY.cancelDate,
      })
    ).toEqual({
      label: "Renewal date",
      value: "Renews 12 Aug 2026",
    })
  })

  it("shows cancel date after cancel is scheduled", () => {
    expect(
      buildPlanRenewalDateMetric(
        paidPlan({
          renewalDateLabel: "Renews 30 September 2026",
          scheduledChangeLine: "Cancels on 30 September 2026",
        }),
        {
          renewalDate: BILLING_CREDITS_PAGE_COPY.renewalDate,
          cancelDate: BILLING_CREDITS_PAGE_COPY.cancelDate,
        }
      )
    ).toEqual({
      label: "Cancel date",
      value: "Cancels on 30 September 2026",
    })
  })
})

describe("Manage Plan light-theme chrome tokens", () => {
  it("maps cadence tabs to Main Bg / Cards semantic tokens", () => {
    expect(MANAGE_PLAN_CADENCE_SHELL_CLASS).toContain("border-op-card-border")
    expect(MANAGE_PLAN_CADENCE_ITEM_CLASS).toContain("text-op-text-secondary")
    expect(MANAGE_PLAN_CADENCE_ITEM_ACTIVE_CLASS).toBe(
      "bg-op-background-secondary text-op-text-primary"
    )
    expect(MANAGE_PLAN_CADENCE_ITEM_ACTIVE_CLASS).not.toContain("bg-[#")
    expect(MANAGE_PLAN_CADENCE_ITEM_ACTIVE_CLASS).not.toContain(
      "bg-op-color-gray-"
    )
  })

  it("uses collapse-button tokens for FAQ chevron chips", () => {
    expect(MANAGE_PLAN_FAQ_TRIGGER_CLASS).toContain(
      "bg-op-button-collapse-background"
    )
    expect(MANAGE_PLAN_FAQ_TRIGGER_CLASS).toContain("text-op-text-primary")
    expect(MANAGE_PLAN_FAQ_TRIGGER_CLASS).not.toContain("bg-[#212121]")
    expect(MANAGE_PLAN_FAQ_TRIGGER_CLASS).not.toContain("text-white")
  })
})

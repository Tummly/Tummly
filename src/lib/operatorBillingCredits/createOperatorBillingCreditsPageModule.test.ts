import { describe, expect, it, vi } from "vitest"

import {
  billingCreditsHeaderActions,
  resolveBillingCreditsTabId,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  createOperatorBillingCreditsPageModule,
  type BillingCreditsPageAdapters,
  type BillingCreditsPageData,
} from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"

function sampleBillingContacts(
  overrides: Partial<BillingCreditsPageData["billingContacts"]> = {}
): BillingCreditsPageData["billingContacts"] {
  return {
    billingContactUserId: 1,
    billingEmail: "",
    eligibleMembers: [
      { userId: 1, fullName: "Owner", email: "owner@example.com" },
      { userId: 2, fullName: "Admin", email: "admin@example.com" },
    ],
    lowCreditAlerts: {
      owner: true,
      admin: false,
      billingContact: true,
    },
    paymentFailureAlerts: {
      owner: true,
      billingContact: true,
    },
    ...overrides,
  }
}

function samplePage(overrides: Partial<BillingCreditsPageData> = {}): BillingCreditsPageData {
  return {
    actorPermissionRole: "Owner",
    actorCanManage: true,
    actorCanPersistBillingContacts: true,
    planSubscription: {
      subscriptionPlan: "Pilot",
      billingStatus: "Pilot",
      renewalDateLabel: "Pilot ends 15 September 2026",
      emailCreditsRemaining: 500,
      smsCreditsRemaining: 20,
      aiCreditsRemaining: 20,
      billingCycle: null,
      planPriceNet: "£0",
      includedLocations: 1,
      activeLocations: 1,
      includedEmailCreditsLabel: "500 once",
      includedSmsCreditsLabel: "20 once",
      includedAiCreditsLabel: "20 once",
      starterKitState: "unused",
      pricebookId: "guest-loop-mvp-2026-07",
      scheduledChangeLine: null,
      isPilot: true,
    },
    billingContacts: sampleBillingContacts(),
    ...overrides,
  }
}

function createTestModule(
  pageOverrides: Partial<BillingCreditsPageData> = {},
  adapterOverrides: Partial<BillingCreditsPageAdapters> = {}
) {
  const page = samplePage(pageOverrides)
  return createOperatorBillingCreditsPageModule({
    getPage: vi.fn(async () => page),
    updateBillingContacts: vi.fn(async (payload) => ({
      ...page.billingContacts,
      billingContactUserId: payload.billingContactUserId,
      billingEmail: payload.billingEmail,
      lowCreditAlerts: { ...payload.lowCreditAlerts },
      paymentFailureAlerts: { ...payload.paymentFailureAlerts },
    })),
    ...adapterOverrides,
  })
}

describe("resolveBillingCreditsTabId", () => {
  it("defaults unknown tabs to plan-subscription", () => {
    expect(resolveBillingCreditsTabId(null)).toBe("plan-subscription")
    expect(resolveBillingCreditsTabId("nope")).toBe("plan-subscription")
  })

  it("accepts canonical tab ids", () => {
    expect(resolveBillingCreditsTabId("activity")).toBe("activity")
  })
})

describe("billingCreditsHeaderActions", () => {
  it("hides write CTAs for View", () => {
    expect(
      billingCreditsHeaderActions({
        accessLevel: "view",
        permissionRole: "Admin",
      })
    ).toEqual({
      showManagePlan: false,
      showBuyCredits: false,
      showChangePlan: false,
    })
  })

  it("shows both header CTAs for Owner Manage", () => {
    expect(
      billingCreditsHeaderActions({
        accessLevel: "manage",
        permissionRole: "Owner",
      })
    ).toEqual({
      showManagePlan: true,
      showBuyCredits: true,
      showChangePlan: true,
    })
  })

  it("hides Manage plan for Billing Admin Manage", () => {
    expect(
      billingCreditsHeaderActions({
        accessLevel: "manage",
        permissionRole: "Billing Admin",
      })
    ).toEqual({
      showManagePlan: false,
      showBuyCredits: true,
      showChangePlan: false,
    })
  })

  it("shows Buy credits for Admin Manage without Manage plan", () => {
    expect(
      billingCreditsHeaderActions({
        accessLevel: "manage",
        permissionRole: "Admin",
      })
    ).toEqual({
      showManagePlan: false,
      showBuyCredits: true,
      showChangePlan: false,
    })
  })
})

describe("createOperatorBillingCreditsPageModule", () => {
  it("returns a stable snapshot until state changes", async () => {
    const module = createTestModule()
    await module.load()
    expect(module.getSnapshot()).toBe(module.getSnapshot())
    module.requestTabChange("activity")
    expect(module.getSnapshot().activeTabId).toBe("activity")
  })

  it("round-trips tab id through requestTabChange", async () => {
    const module = createTestModule()
    module.setNavigationTargets({ mode: "single", locationId: 10 })
    await module.load()

    module.requestTabChange("payment-invoices")
    expect(module.getSnapshot().activeTabId).toBe("payment-invoices")
  })

  it("builds manage-plan and buy-credits hrefs", async () => {
    const module = createTestModule()
    module.setNavigationTargets({ mode: "multi", locationId: 7 })
    await module.load()

    expect(module.getSnapshot().managePlanHref).toBe(
      "/multi-dashboard/settings/billing-credits/manage-plan?location=7"
    )
    expect(module.getSnapshot().buyCreditsHref).toBe(
      "/multi-dashboard/settings/billing-credits/manage-plan?location=7&section=credit-top-ups"
    )
  })

  it("opens nested manage plan from header CTA", async () => {
    const module = createTestModule()
    module.setNavigationTargets({ mode: "single", locationId: 42 })
    await module.load()

    module.openManagePlan()
    expect(module.consumePendingNavigation()).toBe(
      "/single-dashboard/settings/billing-credits/manage-plan?location=42"
    )
  })

  it("opens credit top-ups section from Buy credits", async () => {
    const module = createTestModule()
    module.setNavigationTargets({ mode: "single", locationId: 42 })
    await module.load()

    module.openBuyCredits()
    expect(module.consumePendingNavigation()).toBe(
      "/single-dashboard/settings/billing-credits/manage-plan?location=42&section=credit-top-ups"
    )
  })

  it("breadcrumb returns to plan-subscription tab", async () => {
    const module = createTestModule()
    module.setNavigationTargets({ mode: "single", locationId: 42 })
    await module.load()
    module.requestTabChange("credits-usage")

    expect(module.getSnapshot().breadcrumbHref).toBe(
      "/single-dashboard/settings/billing-credits?location=42&tab=plan-subscription"
    )
  })

  it("marks forbidden when the adapter returns 403", async () => {
    const module = createOperatorBillingCreditsPageModule({
      getPage: async () => {
        throw { response: { status: 403 } }
      },
      updateBillingContacts: vi.fn(),
    })
    await module.load()
    expect(module.getSnapshot().loadStatus).toBe("forbidden")
  })

  it("breadcrumb targets plan-subscription while Back keeps the prior tab", async () => {
    const module = createTestModule()
    module.setNavigationTargets({ mode: "single", locationId: 42 })
    await module.load()
    module.requestTabChange("credits-usage")

    expect(module.getSnapshot().activeTabId).toBe("credits-usage")
    expect(module.getSnapshot().breadcrumbHref).toBe(
      "/single-dashboard/settings/billing-credits?location=42&tab=plan-subscription"
    )
  })

  it("hides write CTAs for View snapshot", async () => {
    const module = createTestModule({
      actorCanManage: false,
      actorCanPersistBillingContacts: false,
      actorPermissionRole: "Marketing",
    })
    await module.load()
    const snap = module.getSnapshot()
    expect(snap.accessLevel).toBe("view")
    expect(snap.showManagePlan).toBe(false)
    expect(snap.showBuyCredits).toBe(false)
    expect(snap.showChangePlan).toBe(false)
  })

  it("clears manage-plan section when scrolling to plan cards", async () => {
    const module = createTestModule()
    await module.load()
    module.setManagePlanSectionFromUrl("credit-top-ups")
    module.scrollManagePlanToCards()
    expect(module.getSnapshot().managePlanSection).toBeNull()
  })

  it("is dirty only on the billing-contacts tab", async () => {
    const module = createTestModule()
    await module.load()

    module.requestTabChange("billing-contacts")
    expect(module.getSnapshot().isDirty).toBe(false)

    module.setBillingEmail("billing@example.com")
    expect(module.getSnapshot().isDirty).toBe(true)

    module.requestTabChange("plan-subscription")
    expect(module.getSnapshot().leaveDirtyOpen).toBe(true)
    expect(module.getSnapshot().activeTabId).toBe("billing-contacts")

    module.confirmLeaveDirtyCancel()
    expect(module.getSnapshot().isDirty).toBe(false)
    expect(module.getSnapshot().activeTabId).toBe("plan-subscription")
  })

  it("does not mark other tabs dirty when billing contacts is clean", async () => {
    const module = createTestModule()
    await module.load()

    module.requestTabChange("credits-usage")
    expect(module.getSnapshot().isDirty).toBe(false)
    expect(module.getSnapshot().leaveDirtyOpen).toBe(false)
  })

  it("allows empty alert ticks", async () => {
    const updateBillingContacts = vi.fn(async (payload) => ({
      ...sampleBillingContacts(),
      lowCreditAlerts: payload.lowCreditAlerts,
      paymentFailureAlerts: payload.paymentFailureAlerts,
    }))
    const module = createTestModule({}, { updateBillingContacts })
    await module.load()
    module.requestTabChange("billing-contacts")

    module.setLowCreditAlertOwner(false)
    module.setLowCreditAlertAdmin(false)
    module.setLowCreditAlertBillingContact(false)
    module.setPaymentFailureAlertOwner(false)
    module.setPaymentFailureAlertBillingContact(false)

    await module.persistBillingContacts()

    expect(updateBillingContacts).toHaveBeenCalledWith(
      expect.objectContaining({
        lowCreditAlerts: {
          owner: false,
          admin: false,
          billingContact: false,
        },
        paymentFailureAlerts: {
          owner: false,
          billingContact: false,
        },
      })
    )
    expect(module.getSnapshot().isDirty).toBe(false)
  })
})

import { describe, expect, it, vi } from "vitest"

import {
  billingCreditsHeaderActions,
  BILLING_CREDITS_PAGE_COPY,
  resolveBillingCreditsTabId,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  createOperatorBillingCreditsPageModule,
  type BillingCreditsPageAdapters,
  type BillingCreditsPageData,
} from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"
import type { CreditsUsageSnapshot } from "@/lib/operatorBillingCredits/creditsUsagePresentation"

function sampleUsage(
  overrides: Partial<CreditsUsageSnapshot> = {}
): CreditsUsageSnapshot {
  return {
    periodLabel: "Account · Pilot allowance",
    starterKitState: "unused",
    isPilot: true,
    channels: [
      {
        channel: "email",
        combinedRemaining: 500,
        usedThisCycle: 0,
        includedThisPeriod: 500,
        purchasedRemaining: 0,
        purchasedExpiryLabel: null,
      },
      {
        channel: "sms",
        combinedRemaining: 428,
        usedThisCycle: 72,
        includedThisPeriod: 500,
        purchasedRemaining: 0,
        purchasedExpiryLabel: null,
      },
      {
        channel: "ai",
        combinedRemaining: 20,
        usedThisCycle: 0,
        includedThisPeriod: 20,
        purchasedRemaining: 0,
        purchasedExpiryLabel: null,
      },
    ],
    ...overrides,
  }
}

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
    paymentMethod: null,
    invoices: [],
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
      allowSms5000TopUp: false,
    },
    billingContacts: sampleBillingContacts(),
    ...overrides,
  }
}

function createTestModule(
  pageOverrides: Partial<BillingCreditsPageData> = {},
  usage: CreditsUsageSnapshot = sampleUsage(),
  adapterOverrides: Partial<BillingCreditsPageAdapters> = {}
) {
  const page = samplePage(pageOverrides)
  return createOperatorBillingCreditsPageModule({
    getPage: vi.fn(async () => page),
    getUsage: async () => usage,
    submitPlanChange: vi.fn(),
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
    const module = createOperatorBillingCreditsPageModule(
      {
        getPage: async () => samplePage(),
        getUsage: async () => sampleUsage(),
        submitPlanChange: vi.fn(),
      },
      { initialTabId: "credits-usage" }
    )
    module.setNavigationTargets({ mode: "single", locationId: 42 })
    await module.load()

    expect(module.getSnapshot().breadcrumbHref).toBe(
      "/single-dashboard/settings/billing-credits?location=42&tab=plan-subscription"
    )
  })

  it("marks forbidden when the adapter returns 403", async () => {
    const module = createOperatorBillingCreditsPageModule({
      getPage: async () => {
        throw { response: { status: 403 } }
      },
      getUsage: async () => sampleUsage(),
      submitPlanChange: vi.fn(),
    })
    await module.load()
    expect(module.getSnapshot().loadStatus).toBe("forbidden")
  })

  it("breadcrumb targets plan-subscription while Back keeps the prior tab", async () => {
    const module = createOperatorBillingCreditsPageModule(
      {
        getPage: async () => samplePage(),
        getUsage: async () => sampleUsage(),
        submitPlanChange: vi.fn(),
      },
      { initialSurface: "manage-plan" }
    )
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
      actorPermissionRole: "Marketing",
    })
    await module.load()
    const snap = module.getSnapshot()
    expect(snap.accessLevel).toBe("view")
    expect(snap.showManagePlan).toBe(false)
    expect(snap.showBuyCredits).toBe(false)
    expect(snap.showChangePlan).toBe(false)
    expect(snap.channelCards.every((card) => !card.showBuy && !card.showChangePlan)).toBe(
      true
    )
  })

  it("clears manage-plan section when scrolling to plan cards", async () => {
    const module = createOperatorBillingCreditsPageModule(
      {
        getPage: async () => samplePage(),
        getUsage: async () => sampleUsage(),
        submitPlanChange: vi.fn(),
      },
      {
        initialManagePlanSection: "credit-top-ups",
        initialSurface: "manage-plan",
      }
    )
    await module.load()
    module.scrollManagePlanToCards()
    expect(module.getSnapshot().managePlanSection).toBeNull()
  })

  it("projects combined remaining math onto channel cards", async () => {
    const module = createTestModule()
    await module.load()
    const sms = module.getSnapshot().channelCards.find((card) => card.channel === "sms")
    expect(sms?.headline).toBe("428 remaining")
    expect(sms?.subline).toBe("72 of 500 included used")
    expect(sms?.fillRatio).toBeCloseTo(428 / 500)
  })

  it("shows 100% copy on depleted channels", async () => {
    const module = createTestModule(
      {},
      sampleUsage({
        isPilot: false,
        channels: [
          {
            channel: "sms",
            combinedRemaining: 0,
            usedThisCycle: 500,
            includedThisPeriod: 500,
            purchasedRemaining: 0,
            purchasedExpiryLabel: null,
          },
          ...sampleUsage().channels.filter((row) => row.channel !== "sms"),
        ],
      })
    )
    await module.load()
    const sms = module.getSnapshot().channelCards.find((card) => card.channel === "sms")
    expect(sms?.headline).toBe("No SMS credits remaining.")
    expect(sms?.fillRatio).toBe(1)
  })

  it("hides Buy on Pilot channel cards", async () => {
    const module = createTestModule()
    await module.load()
    expect(
      module.getSnapshot().channelCards.every((card) => !card.showBuy)
    ).toBe(true)
  })

  it("opens credit top-ups from channel Buy", async () => {
    const module = createTestModule(
      {},
      sampleUsage({ isPilot: false })
    )
    module.setNavigationTargets({ mode: "single", locationId: 42 })
    await module.load()

    module.openBuyChannelCredits("sms")
    expect(module.consumePendingNavigation()).toBe(
      "/single-dashboard/settings/billing-credits/manage-plan?location=42&section=credit-top-ups&channel=sms"
    )
  })

  it("does not persist cadence toggle through submitPlanChange until confirm", async () => {
    const submitPlanChange = vi.fn()
    const module = createTestModule({}, sampleUsage(), { submitPlanChange })
    module.setNavigationTargets({ mode: "single", locationId: 42 })
    await module.load()

    module.setPreviewCadence("annual")
    expect(module.getSnapshot().previewCadence).toBe("annual")
    expect(submitPlanChange).not.toHaveBeenCalled()
  })

  it("marks Pilot card as not a downgrade target for paid plans", async () => {
    const module = createTestModule({
      planSubscription: {
        ...samplePage().planSubscription,
        subscriptionPlan: "Starter",
        isPilot: false,
        billingCycle: "Monthly",
        planPriceNet: "£39",
      },
    })
    await module.load()

    const pilotCard = module
      .getSnapshot()
      .managePlanCards.find((card) => card.id === "Pilot")
    expect(pilotCard?.cta.disabled).toBe(true)
  })

  it("navigates to plan-subscription after a scheduled plan change", async () => {
    const submitPlanChange = vi.fn(async () => ({
      outcome: "scheduled" as const,
      scheduledChangeLine: "Changes to Growth on 12 Aug 2026",
    }))
    const module = createTestModule({}, sampleUsage(), { submitPlanChange })
    module.setNavigationTargets({ mode: "single", locationId: 42 })
    await module.load()

    module.requestPlanChange("Growth")
    await module.confirmPlanChange()

    expect(submitPlanChange).toHaveBeenCalledWith({
      targetPlan: "Growth",
      targetCadence: "monthly",
    })
    expect(module.consumePendingNavigation()).toBe(
      "/single-dashboard/settings/billing-credits?location=42&tab=plan-subscription"
    )
  })

  it("keeps manage plan open when plan change fails", async () => {
    const submitPlanChange = vi.fn(async () => {
      throw new Error("failed")
    })
    const module = createOperatorBillingCreditsPageModule(
      {
        getPage: async () => samplePage(),
        getUsage: async () => sampleUsage(),
        submitPlanChange,
      },
      { initialSurface: "manage-plan" }
    )
    await module.load()

    module.requestPlanChange("Starter")
    await module.confirmPlanChange()

    expect(module.getSnapshot().surface).toBe("manage-plan")
    expect(module.getSnapshot().planChangeConfirm?.busy).toBe(false)
    expect(module.consumePendingNavigation()).toBeNull()
  })

  it("redirects to Revolut after a pay outcome", async () => {
    const submitPlanChange = vi.fn(async () => ({
      outcome: "pay" as const,
      redirectUrl: "https://checkout.revolut.com/pay/example",
    }))
    const module = createTestModule({}, sampleUsage(), { submitPlanChange })
    await module.load()

    module.requestPlanChange("Starter")
    await module.confirmPlanChange()

    expect(module.consumePendingPayRedirect()).toBe(
      "https://checkout.revolut.com/pay/example"
    )
    expect(module.consumePendingNavigation()).toBeNull()
  })

  it("shows Pilot empty payment method and no invoices yet", async () => {
    const module = createTestModule()
    await module.load()
    const snap = module.getSnapshot()
    expect(snap.showNoPaymentMethodOnFile).toBe(true)
    expect(snap.showNoInvoicesYet).toBe(true)
    expect(snap.showUpdatePaymentMethod).toBe(false)
  })

  it("exposes update payment method confirm copy", async () => {
    const module = createTestModule({
      planSubscription: {
        ...samplePage().planSubscription,
        isPilot: false,
      },
      paymentMethod: {
        kind: "card",
        brand: "Visa",
        last4: "4242",
        expiryLabel: "08/28",
      },
    })
    await module.load()
    module.openUpdatePaymentMethodConfirm()
    const snap = module.getSnapshot()
    expect(snap.updatePaymentMethodConfirmOpen).toBe(true)
    expect(snap.updatePaymentMethodConfirmCopy.body).toBe(
      BILLING_CREDITS_PAGE_COPY.updatePaymentMethodConfirmBody
    )
    expect(snap.updatePaymentMethodConfirmCopy.continueLabel).toBe(
      BILLING_CREDITS_PAGE_COPY.continue
    )
  })

  it("hides Update payment method for View snapshot", async () => {
    const module = createTestModule({
      actorCanManage: false,
      actorPermissionRole: "Marketing",
      planSubscription: {
        ...samplePage().planSubscription,
        isPilot: false,
      },
      paymentMethod: {
        kind: "card",
        brand: "Visa",
        last4: "4242",
        expiryLabel: "08/28",
      },
    })
    await module.load()
    expect(module.getSnapshot().showUpdatePaymentMethod).toBe(false)
  })

  it("redirects to Revolut after confirming payment method update", async () => {
    const module = createOperatorBillingCreditsPageModule({
      getPage: async () =>
        samplePage({
          planSubscription: {
            ...samplePage().planSubscription,
            isPilot: false,
          },
        }),
      getUsage: async () => sampleUsage(),
      submitPlanChange: vi.fn(),
      createPaymentMethodUpdateSession: async () => ({
        redirectUrl:
          "https://sandbox-merchant.revolut.com/hpp/update-payment-method",
      }),
    })
    await module.load()
    await module.confirmUpdatePaymentMethod()
    expect(module.consumePendingPaymentMethodRedirect()).toBe(
      "https://sandbox-merchant.revolut.com/hpp/update-payment-method"
    )
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
    const module = createTestModule({}, sampleUsage(), { updateBillingContacts })
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

  it("keeps top-up Buy disabled until a chip is selected", async () => {
    const module = createTestModule(
      {
        planSubscription: {
          ...samplePage().planSubscription,
          isPilot: false,
          subscriptionPlan: "Growth",
        },
      },
      sampleUsage({ isPilot: false })
    )
    await module.load()

    expect(
      module.getSnapshot().topUpCards.every((card) => card.buyDisabled)
    ).toBe(true)

    module.selectTopUpPack("sms", 500)
    const sms = module
      .getSnapshot()
      .topUpCards.find((card) => card.channel === "sms")
    expect(sms?.buyDisabled).toBe(false)
    expect(sms?.selectedNetLabel).toBe("£55 + VAT")
  })

  it("navigates to credits-usage after a successful top-up return", async () => {
    const module = createTestModule(
      {
        planSubscription: {
          ...samplePage().planSubscription,
          isPilot: false,
        },
      },
      sampleUsage({ isPilot: false })
    )
    module.setNavigationTargets({ mode: "single", locationId: 42 })
    await module.load()

    module.selectTopUpPack("email", 5000)
    module.handleTopUpPayReturn("success")

    expect(module.consumePendingNavigation()).toBe(
      "/single-dashboard/settings/billing-credits?location=42&tab=credits-usage"
    )
  })

  it("keeps the selected chip after a failed top-up return", async () => {
    const module = createTestModule(
      {
        planSubscription: {
          ...samplePage().planSubscription,
          isPilot: false,
        },
      },
      sampleUsage({ isPilot: false })
    )
    await module.load()

    module.selectTopUpPack("ai", 100)
    module.handleTopUpPayReturn("fail")

    expect(module.getSnapshot().managePlanSection).toBe("credit-top-ups")
    expect(module.getSnapshot().topUpCards.find((c) => c.channel === "ai")?.packs
      .find((p) => p.quantity === 100)?.selected).toBe(true)
  })

  it("keeps the selected chip after a cancelled top-up return", async () => {
    const module = createTestModule(
      {
        planSubscription: {
          ...samplePage().planSubscription,
          isPilot: false,
        },
      },
      sampleUsage({ isPilot: false })
    )
    await module.load()

    module.selectTopUpPack("ai", 100)
    module.handleTopUpPayReturn("cancel")

    expect(module.getSnapshot().managePlanSection).toBe("credit-top-ups")
    expect(
      module
        .getSnapshot()
        .topUpCards.find((c) => c.channel === "ai")
        ?.packs.find((p) => p.quantity === 100)?.selected
    ).toBe(true)
  })

  it("auto-opens credit top-ups only for Billing Admin without section", async () => {
    const billingAdmin = createTestModule({
      actorPermissionRole: "Billing Admin",
      actorCanManage: true,
    })
    await billingAdmin.load()
    expect(billingAdmin.shouldAutoOpenCreditTopUps()).toBe(true)

    const admin = createTestModule({
      actorPermissionRole: "Admin",
      actorCanManage: true,
    })
    await admin.load()
    expect(admin.shouldAutoOpenCreditTopUps()).toBe(false)
  })

  it("redirects to Revolut after confirming a top-up purchase", async () => {
    const payCreditTopUp = vi.fn(async () => ({
      redirectUrl: "https://checkout.revolut.com/pay/top-up/example",
    }))
    const confirmCreditTopUp = vi.fn(async () => ({
      channel: "sms" as const,
      quantity: 100,
      channelLabel: "SMS credits",
      netLabel: "£12",
      grossLabel: "£14.40",
      vatLabel: "£2.40",
    }))
    const module = createTestModule(
      {
        planSubscription: {
          ...samplePage().planSubscription,
          isPilot: false,
        },
      },
      sampleUsage({ isPilot: false }),
      { confirmCreditTopUp, payCreditTopUp }
    )
    await module.load()

    module.selectTopUpPack("sms", 100)
    await module.requestTopUpBuy("sms")
    await module.confirmTopUpBuy()

    expect(confirmCreditTopUp).toHaveBeenCalledWith({
      channel: "sms",
      quantity: 100,
    })
    expect(payCreditTopUp).toHaveBeenCalledWith({
      channel: "sms",
      quantity: 100,
    })
    expect(module.consumePendingPayRedirect()).toBe(
      "https://checkout.revolut.com/pay/top-up/example"
    )
  })

})

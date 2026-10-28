import { describe, expect, it, vi } from "vitest"

import { CUSTOM_PLAN_QUOTE_COPY } from "@/content/marketing/customPlanQuoteCopy"
import {
  createOperatorBillingCreditsPageModule,
  type BillingCreditsPageAdapters,
  type BillingCreditsPageData,
} from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"
import type { CreditsUsageSnapshot } from "@/lib/operatorBillingCredits/creditsUsagePresentation"

function pendingPage(
  pending: Partial<
    NonNullable<BillingCreditsPageData["planSubscription"]["pendingPayment"]>
  > = {},
): BillingCreditsPageData {
  return {
    actorPermissionRole: "Owner",
    actorCanManage: true,
    actorCanPersistBillingContacts: true,
    paymentMethod: null,
    invoices: [],
    planSubscription: {
      subscriptionPlan: "Free",
      billingStatus: "Active",
      renewalDateLabel: "",
      emailCreditsRemaining: 0,
      smsCreditsRemaining: 0,
      aiCreditsRemaining: 0,
      billingCycle: null,
      planPriceNet: "£0",
      includedLocations: 1,
      activeLocations: 1,
      includedEmailCreditsLabel: "0",
      includedSmsCreditsLabel: "0",
      includedAiCreditsLabel: "0",
      starterKitState: "unused",
      pricebookId: "p",
      scheduledChangeLine: null,
      isPilot: false,
      allowSms5000TopUp: false,
      pendingPayment: {
        plan: "Custom",
        cadence: "monthly",
        failedAtUtc: null,
        failureCode: "quote_changed",
        basePlan: "growth",
        locations: 3,
        packs: { offer: 2 },
        ...pending,
      },
    },
    billingContacts: {
      billingContactUserId: 1,
      billingEmail: "",
      eligibleMembers: [],
      lowCreditAlerts: { owner: true, admin: false, billingContact: true },
      paymentFailureAlerts: { owner: true, billingContact: true },
    },
    vatModeActive: false,
    currentCatalog: { vatRateBps: 0, plans: [], topUpPacks: [] },
  }
}

async function loadModule(
  page: BillingCreditsPageData,
  retryPendingPayment: BillingCreditsPageAdapters["retryPendingPayment"],
) {
  const module = createOperatorBillingCreditsPageModule({
    getPage: async () => page,
    getUsage: async () => ({
      periodLabel: "Account",
      starterKitState: "unused",
      isPilot: false,
      channels: [],
      rows: [],
    }) as unknown as CreditsUsageSnapshot,
    getBillingActivity: async () => ({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    }),
    submitPlanChange: vi.fn(),
    updateBillingContacts: vi.fn(),
    retryPendingPayment,
  })
  await module.load()
  return module
}

const CHANGED = CUSTOM_PLAN_QUOTE_COPY.changed
const UNAVAILABLE = CUSTOM_PLAN_QUOTE_COPY.optionUnavailable

describe("Manage Plan pending payment review", () => {
  it("shows the changed-quote copy and a review action for quote_changed", async () => {
    const module = await loadModule(pendingPage(), vi.fn())
    const review = module.getSnapshot().pendingPaymentReview
    expect(review?.code).toBe("quote_changed")
    expect(review?.message).toBe(CHANGED)
    expect(review?.open).toBe(false)
    expect(review?.savedChoices).toEqual({
      cadence: "monthly",
      basePlan: "growth",
      locations: 3,
      packs: { offer: 2 },
    })
  })

  it("shows the unavailable-option copy and no saved choices for legacy_pack_key", async () => {
    const module = await loadModule(
      pendingPage({ failureCode: "legacy_pack_key", packs: { qr_10: 1 } }),
      vi.fn(),
    )
    const review = module.getSnapshot().pendingPaymentReview
    expect(review?.code).toBe("legacy_pack_key")
    expect(review?.message).toBe(UNAVAILABLE)
    expect(review?.savedChoices).toBeNull()
  })

  it("has no review for other failures", async () => {
    const module = await loadModule(
      pendingPage({ failureCode: "revolut_http_error" }),
      vi.fn(),
    )
    expect(module.getSnapshot().pendingPaymentReview).toBeNull()
  })

  it("keeps the same snapshot object until the next change", async () => {
    const module = await loadModule(pendingPage(), vi.fn())
    expect(module.getSnapshot()).toBe(module.getSnapshot())
  })

  it("opens the review instead of retrying a stale saved quote", async () => {
    const retry = vi.fn()
    const module = await loadModule(pendingPage(), retry)

    await module.retryPendingPayment()

    expect(retry).not.toHaveBeenCalled()
    expect(module.getSnapshot().pendingPaymentReview?.open).toBe(true)
    expect(module.getSnapshot().pendingPayRedirectUrl).toBeNull()
  })

  it("opens the review when a retry is refused with quote_changed", async () => {
    const retry = vi.fn().mockRejectedValue({
      response: { status: 409, data: { code: "quote_changed" } },
    })
    const module = await loadModule(
      pendingPage({ failureCode: "revolut_http_error" }),
      retry,
    )
    expect(module.getSnapshot().pendingPaymentReview).toBeNull()

    await module.retryPendingPayment()

    const snap = module.getSnapshot()
    expect(snap.pendingPaymentReview?.code).toBe("quote_changed")
    expect(snap.pendingPaymentReview?.open).toBe(true)
    expect(snap.toast).toBeNull()
  })

  it("opens the review when a retry is refused with legacy_pack_key", async () => {
    const retry = vi.fn().mockRejectedValue({
      response: { status: 409, data: { code: "legacy_pack_key" } },
    })
    const module = await loadModule(
      pendingPage({ failureCode: "revolut_http_error" }),
      retry,
    )

    await module.retryPendingPayment()

    const snap = module.getSnapshot()
    expect(snap.pendingPaymentReview?.code).toBe("legacy_pack_key")
    expect(snap.pendingPaymentReview?.message).toBe(UNAVAILABLE)
    expect(snap.pendingPaymentReview?.savedChoices).toBeNull()
  })

  it("keeps the generic toast for another retry failure", async () => {
    const retry = vi.fn().mockRejectedValue({
      response: { status: 502, data: { code: "revolut_http_error" } },
    })
    const module = await loadModule(
      pendingPage({ failureCode: "revolut_http_error" }),
      retry,
    )

    await module.retryPendingPayment()

    expect(module.getSnapshot().toast?.message).toBe(
      "Could not open checkout. Try again.",
    )
    expect(module.getSnapshot().pendingPaymentReview).toBeNull()
  })

  it("keeps the generic toast when a retry fails with no code", async () => {
    const retry = vi.fn().mockRejectedValue(new Error("network"))
    const module = await loadModule(
      pendingPage({ failureCode: "revolut_http_error" }),
      retry,
    )

    await module.retryPendingPayment()

    expect(module.getSnapshot().toast?.message).toBe(
      "Could not open checkout. Try again.",
    )
    expect(module.getSnapshot().pendingPaymentReview).toBeNull()
  })

  it("pays only with the reviewed quote config and then opens checkout", async () => {
    const retry = vi.fn().mockResolvedValue({ checkoutUrl: "https://pay.test/1" })
    const module = await loadModule(pendingPage(), retry)
    module.openPendingPaymentReview()

    await module.confirmPendingPaymentReview('{"quoteHash":"fresh"}')

    expect(retry).toHaveBeenCalledWith({ customPlanConfigJson: '{"quoteHash":"fresh"}' })
    const snap = module.getSnapshot()
    expect(snap.pendingPayRedirectUrl).toBe("https://pay.test/1")
    expect(snap.pendingPaymentReview?.open).toBe(false)
  })

  it("stays open with the changed-quote copy when the reviewed quote is stale", async () => {
    const retry = vi.fn().mockRejectedValue({
      response: { status: 409, data: { code: "quote_changed" } },
    })
    const module = await loadModule(pendingPage(), retry)
    module.openPendingPaymentReview()

    await module.confirmPendingPaymentReview('{"quoteHash":"old"}')

    const snap = module.getSnapshot()
    expect(snap.pendingPayRedirectUrl).toBeNull()
    expect(snap.pendingPaymentReview?.open).toBe(true)
    expect(snap.pendingPaymentReview?.message).toBe(CHANGED)
    expect(snap.pendingPaymentReview?.busy).toBe(false)
  })

  it("moves to a new choice when the reviewed quote names a retired option", async () => {
    const retry = vi.fn().mockRejectedValue({
      response: { status: 409, data: { code: "legacy_pack_key" } },
    })
    const module = await loadModule(pendingPage(), retry)
    module.openPendingPaymentReview()

    await module.confirmPendingPaymentReview('{"quoteHash":"x"}')

    const review = module.getSnapshot().pendingPaymentReview
    expect(review?.code).toBe("legacy_pack_key")
    expect(review?.savedChoices).toBeNull()
    expect(review?.message).toBe(UNAVAILABLE)
    expect(review?.open).toBe(true)
  })

  it("closes the review without charging", async () => {
    const retry = vi.fn()
    const module = await loadModule(pendingPage(), retry)
    module.openPendingPaymentReview()
    module.closePendingPaymentReview()

    expect(module.getSnapshot().pendingPaymentReview?.open).toBe(false)
    expect(retry).not.toHaveBeenCalled()
  })
})

import { describe, expect, it } from "vitest"

import {
  buildCampaignsMessagingUsageViewModel,
  CAMPAIGNS_MESSAGING_BALANCES_FIXTURE,
  campaignsMessagingSkippedCount,
  formatCampaignsMessagingAfterSend,
  formatCampaignsMessagingSkipped,
  resolveBillingReserveUnavailableCopy,
  resolveCampaignsMessagingLockHelper,
  type CampaignsMessagingBalancesFixture,
} from "@/lib/operatorCampaigns/campaignsMessagingCreditChrome"

function withBalances(
  patch: Partial<CampaignsMessagingBalancesFixture> & {
    email?: Partial<CampaignsMessagingBalancesFixture["email"]>
    sms?: Partial<CampaignsMessagingBalancesFixture["sms"]>
  }
): CampaignsMessagingBalancesFixture {
  return {
    ...CAMPAIGNS_MESSAGING_BALANCES_FIXTURE,
    ...patch,
    email: { ...CAMPAIGNS_MESSAGING_BALANCES_FIXTURE.email, ...patch.email },
    sms: { ...CAMPAIGNS_MESSAGING_BALANCES_FIXTURE.sms, ...patch.sms },
  }
}

describe("buildCampaignsMessagingUsageViewModel", () => {
  it("uses lock 09 copy and combined remaining — no Current plan", () => {
    const viewModel = buildCampaignsMessagingUsageViewModel()

    expect(viewModel.title).toBe("Messaging usage")
    expect(viewModel.subtitle).toBe(
      "Review the Email credits and SMS credits available to this operator account."
    )
    expect(viewModel.email.title).toBe("Email credits")
    expect(viewModel.email.headline).toBe("6,760 remaining")
    expect(viewModel.email.subline).toBe("3,240 of 10,000 included used")
    expect(viewModel.sms.title).toBe("SMS credits")
    expect(viewModel.sms.headline).toBe("300 remaining")
    expect(viewModel.sectionActions).toEqual([
      { kind: "view-usage", label: "View usage" },
      { kind: "buy-sms-credits", label: "Buy SMS credits" },
    ])
    expect(viewModel).not.toHaveProperty("plan")
  })

  it("SMS 100% shows Buy SMS credits and Change plan for Owner Manage", () => {
    const viewModel = buildCampaignsMessagingUsageViewModel(
      withBalances({
        sms: {
          combinedRemaining: 0,
          usedThisCycle: 420,
          includedThisPeriod: 420,
          purchasedRemaining: 0,
          purchasedExpiryLabel: null,
        },
      }),
      { accessLevel: "manage", permissionRole: "Owner" }
    )

    expect(viewModel.sms.headline).toBe("No SMS credits remaining.")
    expect(viewModel.sms.actions).toEqual([
      { kind: "buy-sms-credits", label: "Buy SMS credits" },
      { kind: "change-plan", label: "Change plan" },
    ])
    expect(viewModel.email.actions).toEqual([])
  })

  it("Email 100% shows Change plan and View usage — no Buy Email credits", () => {
    const viewModel = buildCampaignsMessagingUsageViewModel(
      withBalances({
        email: {
          combinedRemaining: 0,
          usedThisCycle: 10000,
          includedThisPeriod: 10000,
          purchasedRemaining: 0,
          purchasedExpiryLabel: null,
        },
      }),
      { accessLevel: "manage", permissionRole: "Owner" }
    )

    expect(viewModel.email.headline).toBe("No Email credits remaining.")
    expect(viewModel.email.actions).toEqual([
      { kind: "change-plan", label: "Change plan" },
      { kind: "view-usage", label: "View usage" },
    ])
    expect(
      viewModel.email.actions.some((action) => action.kind === "buy-email-credits")
    ).toBe(false)
  })

  it("Pilot hides Buy on SMS 100% and keeps Change plan for Owner", () => {
    const viewModel = buildCampaignsMessagingUsageViewModel(
      withBalances({
        isPilot: true,
        sms: {
          combinedRemaining: 0,
          usedThisCycle: 420,
          includedThisPeriod: 420,
          purchasedRemaining: 0,
          purchasedExpiryLabel: null,
        },
      }),
      { accessLevel: "manage", permissionRole: "Owner" }
    )

    expect(viewModel.sms.actions).toEqual([
      { kind: "change-plan", label: "Change plan" },
    ])
  })

  it("View access hides Buy and Change plan on 100%", () => {
    const viewModel = buildCampaignsMessagingUsageViewModel(
      withBalances({
        sms: {
          combinedRemaining: 0,
          usedThisCycle: 420,
          includedThisPeriod: 420,
          purchasedRemaining: 0,
          purchasedExpiryLabel: null,
        },
      }),
      { accessLevel: "view", permissionRole: "Marketing" }
    )

    expect(viewModel.sms.actions).toEqual([
      { kind: "view-usage", label: "View usage" },
    ])
    expect(viewModel.sectionActions).toEqual([
      { kind: "view-usage", label: "View usage" },
    ])
  })
})

describe("campaignsMessagingSkippedCount", () => {
  it("Skipped is Matched minus channel-eligible", () => {
    expect(
      campaignsMessagingSkippedCount({ matched: 148, channelEligible: 121 })
    ).toBe(27)
    expect(formatCampaignsMessagingSkipped(27)).toBe("27")
  })

  it("Skipped is 0 when none and — when unknown", () => {
    expect(
      campaignsMessagingSkippedCount({ matched: 10, channelEligible: 10 })
    ).toBe(0)
    expect(formatCampaignsMessagingSkipped(0)).toBe("0")
    expect(
      campaignsMessagingSkippedCount({ matched: null, channelEligible: 5 })
    ).toBeNull()
    expect(formatCampaignsMessagingSkipped(null)).toBe("—")
  })
})

describe("formatCampaignsMessagingAfterSend", () => {
  it("shows remaining after send or Shortfall n", () => {
    expect(
      formatCampaignsMessagingAfterSend({ remaining: 300, estimate: 121 })
    ).toBe("179")
    expect(
      formatCampaignsMessagingAfterSend({ remaining: 40, estimate: 121 })
    ).toBe("Shortfall 81")
    expect(
      formatCampaignsMessagingAfterSend({ remaining: 40, estimate: null })
    ).toBe("—")
  })
})

describe("resolveBillingReserveUnavailableCopy", () => {
  it("keeps stub copy while Reserve IsLive is false", () => {
    expect(
      resolveBillingReserveUnavailableCopy({ billingReserveLive: false })
    ).toBe(
      "Billing Reserve is not available yet. Schedule and send stay blocked. You can still Save draft and Send test."
    )
  })

  it("uses live unexpected 503 copy after Reserve is live", () => {
    expect(
      resolveBillingReserveUnavailableCopy({ billingReserveLive: true })
    ).toBe(
      "Could not reserve credits for this campaign. Top up or reduce the audience, then try again."
    )
  })
})

describe("resolveCampaignsMessagingLockHelper", () => {
  it("returns Choose a plan for unpaid Pilot Owner Manage", () => {
    expect(
      resolveCampaignsMessagingLockHelper({
        softLocked: true,
        lockCause: "unpaid-pilot",
        accessLevel: "manage",
        permissionRole: "Owner",
      })
    ).toEqual({ kind: "choose-a-plan", label: "Choose a plan" })
  })

  it("returns Update payment method for dunning Billing Admin Manage", () => {
    expect(
      resolveCampaignsMessagingLockHelper({
        softLocked: true,
        lockCause: "dunning",
        accessLevel: "manage",
        permissionRole: "Billing Admin",
      })
    ).toEqual({
      kind: "update-payment-method",
      label: "Update payment method",
    })
  })

  it("hides helper when View-only or unlocked", () => {
    expect(
      resolveCampaignsMessagingLockHelper({
        softLocked: true,
        lockCause: "unpaid-pilot",
        accessLevel: "view",
        permissionRole: "Marketing",
      })
    ).toBeNull()
    expect(
      resolveCampaignsMessagingLockHelper({
        softLocked: false,
        lockCause: null,
        accessLevel: "manage",
        permissionRole: "Owner",
      })
    ).toBeNull()
  })
})

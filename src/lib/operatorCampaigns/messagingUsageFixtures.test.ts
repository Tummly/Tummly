import { describe, expect, it } from "vitest"

import {
  MESSAGING_USAGE_FIXTURE,
  messagingUsageViewModelFromFixture,
} from "@/lib/operatorCampaigns/messagingUsageFixtures"

describe("messagingUsageFixtures", () => {
  it("keeps combined remaining sample figures as the shared Channel-step source", () => {
    expect(MESSAGING_USAGE_FIXTURE).toEqual({
      email: {
        combinedRemaining: 6760,
        usedThisCycle: 3240,
        includedThisPeriod: 10000,
        purchasedRemaining: 0,
        purchasedExpiryLabel: null,
      },
      sms: {
        combinedRemaining: 300,
        usedThisCycle: 120,
        includedThisPeriod: 420,
        purchasedRemaining: 0,
        purchasedExpiryLabel: null,
      },
      isPilot: false,
      softLocked: false,
      lockCause: null,
    })
  })

  it("builds overview presentation lines from the shared fixture — no Current plan", () => {
    const viewModel = messagingUsageViewModelFromFixture()

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
})

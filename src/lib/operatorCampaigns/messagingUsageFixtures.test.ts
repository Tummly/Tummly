import { describe, expect, it } from "vitest"

import {
  MESSAGING_USAGE_FIXTURE,
  messagingUsageViewModelFromFixture,
} from "@/lib/operatorCampaigns/messagingUsageFixtures"

describe("messagingUsageFixtures", () => {
  it("keeps Figma sample figures as the shared Channel-step source", () => {
    expect(MESSAGING_USAGE_FIXTURE).toEqual({
      email: {
        used: 3240,
        allowance: 10000,
        remaining: 6760,
        refreshLabel: "15 August",
      },
      sms: {
        total: 420,
        reserved: 120,
        available: 300,
      },
      plan: {
        name: "Growth",
        locationCount: 3,
        billingLine: "Billed monthly · Next refresh 15 August",
      },
    })
  })

  it("builds overview presentation lines from the shared fixture", () => {
    const viewModel = messagingUsageViewModelFromFixture()

    expect(viewModel.title).toBe("Messaging usage")
    expect(viewModel.email.usageLine).toBe("3,240 of 10,000 used")
    expect(viewModel.email.detailLine).toBe(
      "6,760 remaining · Refreshes 15 August"
    )
    expect(viewModel.sms.usageLine).toBe("420 total")
    expect(viewModel.sms.detailLine).toBe("120 reserved · 300 available")
    expect(viewModel.plan.planLine).toBe("Growth · 3 locations")
    expect(viewModel.viewUsageLabel).toBe("View messaging usage")
    expect(viewModel.buySmsCreditsLabel).toBe("Buy SMS credits")
  })
})

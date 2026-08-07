import { describe, expect, it, vi } from "vitest"

import {
  CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK,
} from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import { resolveCampaignChannelSmsShortfall } from "@/lib/operatorCampaigns/campaignChannelPresentation"
import {
  CAMPAIGN_GOAL_OPTIONS,
  CAMPAIGN_WIZARD_COPY,
  CAMPAIGN_WIZARD_NUMBERED_STEPS,
  CAMPAIGN_WIZARD_SELECT_MENU_CLASS,
} from "@/lib/operatorCampaigns/campaignWizardPresentation"
import { createCampaignWizardModule } from "@/lib/operatorCampaigns/createCampaignWizardModule"
import { MESSAGING_USAGE_FIXTURE } from "@/lib/operatorCampaigns/messagingUsageFixtures"

describe("createCampaignWizardModule", () => {
  it("opens blank Create at Goal with no template and no server draft", () => {
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })

    const snapshot = wizard.getSnapshot()
    expect(snapshot.isOpen).toBe(true)
    expect(snapshot.stepId).toBe("goal")
    expect(snapshot.templateId).toBeNull()
    expect(snapshot.goalId).toBeNull()
    expect(snapshot.canContinue).toBe(false)
    expect(snapshot.showNumberedStepper).toBe(false)
    expect(snapshot.numberedSteps).toEqual([...CAMPAIGN_WIZARD_NUMBERED_STEPS])
    expect(snapshot.goals).toHaveLength(CAMPAIGN_GOAL_OPTIONS.length)
    expect(snapshot.pageTitle).toBe(CAMPAIGN_WIZARD_COPY.pageTitle)
    expect(snapshot.headerSubtitle).toBe("Camden · August")
    expect(snapshot.stepHeading).toBe(CAMPAIGN_WIZARD_COPY.goalStepHeading)
  })

  it("closes without Save and clears the client session (no Draft row)", () => {
    const wizard = createCampaignWizardModule()

    wizard.openBlankCreate({
      locationId: 7,
      locationName: "Soho",
    })
    wizard.setGoalId("thank-recent-guests")
    expect(wizard.getSnapshot().isOpen).toBe(true)

    wizard.close()

    const snapshot = wizard.getSnapshot()
    expect(snapshot.isOpen).toBe(false)
    expect(snapshot.stepId).toBe("goal")
    expect(snapshot.goalId).toBeNull()
    expect(snapshot.locationId).toBeNull()
    expect(snapshot.templateId).toBeNull()
  })

  it("continues from Goal into the numbered 1–6 stepper model", async () => {
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()

    const snapshot = wizard.getSnapshot()
    expect(snapshot.stepId).toBe("audience")
    expect(snapshot.showNumberedStepper).toBe(true)
    expect(snapshot.activeNumberedStepIndex).toBe(0)
    expect(snapshot.numberedSteps.map((step) => step.label)).toEqual([
      "Audience",
      "Channel",
      "Offer",
      "Message",
      "Schedule",
      "Review",
    ])
    expect(snapshot.headerSubtitle).toBe(
      "Thank recent guests · Camden · August"
    )
    expect(snapshot.stepHeading).toBeNull()
    expect(snapshot.placeholderBody).toBeNull()
    expect(snapshot.audience).not.toBeNull()
    expect(snapshot.audience!.options[0]?.title).toBe("All eligible guests")
  })

  it("wires live Smart Group counts for non-deferred groups and keeps deferred offer groups mock", async () => {
    const loadSmartGroupCounts = vi.fn(async () => ({
      smartGroupCounts: {
        "all-guests": 200,
        "new-guests": 31,
        "needs-recovery": 8,
        "positive-feedback": 44,
        "offer-not-redeemed": 99,
        "recent-redeemers": 88,
        "dormant-guests": 17,
      },
    }))

    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      loadSmartGroupCounts,
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()

    expect(loadSmartGroupCounts).toHaveBeenCalledWith({ locationId: 42 })

    const audience = wizard.getSnapshot().audience
    expect(audience).not.toBeNull()
    expect(audience!.loadStatus).toBe("loaded")

    const byId = Object.fromEntries(
      audience!.options.map((option) => [option.id, option])
    )

    expect(byId["all-eligible-guests"]?.countSource).toBe("live-smart-group")
    expect(byId["all-eligible-guests"]?.matched).toBe(200)
    expect(byId["all-eligible-guests"]?.currentlyEligible).toBe(
      CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK.currentlyEligible
    )
    expect(byId["new-guests"]?.countSource).toBe("live-smart-group")
    expect(byId["new-guests"]?.matched).toBe(31)
    expect(byId["positive-feedback"]?.countSource).toBe("live-smart-group")
    expect(byId["positive-feedback"]?.matched).toBe(44)
    expect(byId["dormant-guests"]?.countSource).toBe("live-smart-group")
    expect(byId["dormant-guests"]?.matched).toBe(17)

    expect(byId["offer-not-redeemed"]?.countSource).toBe("mock")
    expect(byId["offer-not-redeemed"]?.matched).toBe(
      CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK.matched
    )
    expect(byId["recent-redeemers"]?.countSource).toBe("mock")
    expect(byId["no-recent-tummly-activity"]?.countSource).toBe("mock")
    expect(byId["no-recent-tummly-activity"]?.title).toBe(
      "No recent Tummly activity"
    )
    expect(byId["dormant-guests"]?.title).toBe("Dormant guests")
    expect(byId["dormant-guests"]?.description).not.toBe(
      byId["no-recent-tummly-activity"]?.description
    )
  })

  it("exposes a mock Campaign eligibility breakdown that is not claimed as server eligibility", async () => {
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      loadSmartGroupCounts: async () => ({
        smartGroupCounts: { "all-guests": 10, "new-guests": 2 },
      }),
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()

    const breakdown = wizard.getSnapshot().audience?.eligibilityBreakdown
    expect(breakdown).toEqual({
      matched: CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK.matched,
      currentlyEligible: CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK.currentlyEligible,
      excluded: CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK.excluded,
      emailEligible: CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK.emailEligible,
      smsEligible: CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK.smsEligible,
      source: "mock",
    })
  })

  it("documents select/popover z-index above RecoveryWizardShell", () => {
    expect(CAMPAIGN_WIZARD_SELECT_MENU_CLASS).toContain("z-[140]")
  })

  it("continues from Audience into Channel with Email selected by default", async () => {
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()

    const snapshot = wizard.getSnapshot()
    expect(snapshot.stepId).toBe("channel")
    expect(snapshot.activeNumberedStepIndex).toBe(1)
    expect(snapshot.placeholderBody).toBeNull()
    expect(snapshot.audience).toBeNull()
    expect(snapshot.channel).not.toBeNull()
    expect(snapshot.channel!.selectedChannelId).toBe("email")
    expect(snapshot.channel!.options.map((option) => option.id)).toEqual([
      "email",
      "sms",
    ])
    expect(snapshot.canContinue).toBe(true)
  })

  it("builds Channel estimated usage from the shared overview messaging fixtures", async () => {
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    wizard.setAudienceId("new-guests")
    await wizard.continue()

    const channel = wizard.getSnapshot().channel
    expect(channel).not.toBeNull()
    expect(channel!.selectedChannelId).toBe("email")
    expect(channel!.usageSummary.audienceLine).toBe(
      "New guests · 162 eligible through at least one channel"
    )
    expect(channel!.usageSummary.rows).toEqual([
      { label: "Eligible recipients", value: "148" },
      { label: "Estimated email messages", value: "148" },
      { label: "Allowance remaining", value: "6,760" },
      { label: "Estimated remaining after send", value: "6,612" },
    ])
    expect(channel!.smsShortfall).toBeNull()

    // Ticket 24: Channel meters must equal overview Messaging usage fixtures.
    expect(channel!.messagingFixture).toEqual(MESSAGING_USAGE_FIXTURE)
    expect(channel!.messagingFixture.email.remaining).toBe(
      MESSAGING_USAGE_FIXTURE.email.remaining
    )
    expect(channel!.messagingFixture.sms.available).toBe(
      MESSAGING_USAGE_FIXTURE.sms.available
    )
    expect(channel!.messagingFixture.sms.reserved).toBe(
      MESSAGING_USAGE_FIXTURE.sms.reserved
    )
  })

  it("switches Channel estimate to SMS rows from the same shared fixtures", async () => {
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()
    wizard.setChannelId("sms")

    const channel = wizard.getSnapshot().channel
    expect(channel).not.toBeNull()
    expect(channel!.selectedChannelId).toBe("sms")
    expect(channel!.usageSummary.rows).toEqual([
      { label: "Eligible recipients", value: "121" },
      { label: "Estimated SMS parts", value: "At least 1 per recipient" },
      { label: "Estimated credits", value: "At least 121" },
      { label: "Available credits", value: "300" },
      { label: "Reserved credits", value: "120" },
      { label: "Estimated balance after send", value: "179" },
    ])
    expect(channel!.messagingFixture).toEqual(MESSAGING_USAGE_FIXTURE)
    // Shared fixtures have enough SMS credits — no shortfall banner.
    expect(channel!.smsShortfall).toBeNull()
  })
})

describe("resolveCampaignChannelSmsShortfall", () => {
  it("shows a shortfall only when SMS available credits are below required", () => {
    expect(
      resolveCampaignChannelSmsShortfall({
        channelId: "sms",
        fixture: MESSAGING_USAGE_FIXTURE,
      })
    ).toBeNull()

    expect(
      resolveCampaignChannelSmsShortfall({
        channelId: "email",
        fixture: {
          ...MESSAGING_USAGE_FIXTURE,
          sms: { total: 100, reserved: 20, available: 80 },
        },
      })
    ).toBeNull()

    const shortfall = resolveCampaignChannelSmsShortfall({
      channelId: "sms",
      fixture: {
        ...MESSAGING_USAGE_FIXTURE,
        sms: { total: 100, reserved: 20, available: 80 },
      },
    })
    expect(shortfall).toEqual({
      title: "More SMS credits are required",
      body: "This campaign requires at least 121 SMS credits. Your account currently has 80 available.",
      buyCreditsLabel: "Buy SMS credits",
    })
  })
})

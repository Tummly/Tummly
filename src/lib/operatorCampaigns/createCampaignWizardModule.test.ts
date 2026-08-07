import { describe, expect, it, vi } from "vitest"

import {
  CAMPAIGN_AUDIENCE_ELIGIBILITY_MOCK,
} from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import {
  CAMPAIGN_GOAL_OPTIONS,
  CAMPAIGN_WIZARD_COPY,
  CAMPAIGN_WIZARD_NUMBERED_STEPS,
  CAMPAIGN_WIZARD_SELECT_MENU_CLASS,
} from "@/lib/operatorCampaigns/campaignWizardPresentation"
import { createCampaignWizardModule } from "@/lib/operatorCampaigns/createCampaignWizardModule"

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
})

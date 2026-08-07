import { describe, expect, it } from "vitest"

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

  it("continues from Goal into the numbered 1–6 stepper model", () => {
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizard.setGoalId("thank-recent-guests")
    wizard.continue()

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
    expect(snapshot.placeholderBody).toBe(
      CAMPAIGN_WIZARD_COPY.placeholderAudience
    )
  })

  it("documents select/popover z-index above RecoveryWizardShell", () => {
    expect(CAMPAIGN_WIZARD_SELECT_MENU_CLASS).toContain("z-[140]")
  })
})

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
import type { CampaignTemplateDetail } from "@/types/operatorCampaigns"

function sampleTemplateDetail(
  overrides: Partial<CampaignTemplateDetail> = {}
): CampaignTemplateDetail {
  return {
    id: "thank-recent-guests",
    version: 1,
    title: "Thank recent guests",
    description: "Welcome recently captured guests.",
    goalLabel: "Thank recent guests",
    audienceLabel: "New guests",
    channelLabel: "Email",
    offerLabel: "Optional",
    suggestsGoal: true,
    suggestsAudience: true,
    suggestsChannel: true,
    suggestsOffer: true,
    suggestions: {
      goalId: "thank-recent-guests",
      audienceKey: "new-guests",
      channel: "email",
      offerStance: "optional",
    },
    ...overrides,
  }
}

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

  it("opens from template at Audience with Goal and suggestion defaults applied (no Draft)", async () => {
    const loadSmartGroupCounts = vi.fn(async () => ({
      smartGroupCounts: { "all-guests": 10, "new-guests": 4 },
    }))
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      loadSmartGroupCounts,
    })

    await wizard.openFromTemplate({
      locationId: 42,
      locationName: "Camden",
      template: sampleTemplateDetail(),
    })

    const snapshot = wizard.getSnapshot()
    expect(snapshot.isOpen).toBe(true)
    expect(snapshot.stepId).toBe("audience")
    expect(snapshot.activeNumberedStepIndex).toBe(0)
    expect(snapshot.showNumberedStepper).toBe(true)
    expect(snapshot.templateId).toBe("thank-recent-guests")
    expect(snapshot.goalId).toBe("thank-recent-guests")
    expect(snapshot.headerSubtitle).toBe(
      "Thank recent guests · Camden · August"
    )
    expect(snapshot.audience?.selectedAudienceId).toBe("new-guests")
    expect(snapshot.canContinue).toBe(true)
    expect(loadSmartGroupCounts).toHaveBeenCalledWith({ locationId: 42 })

    // Defaults applied for later Channel / Offer steps.
    await wizard.continue()
    expect(wizard.getSnapshot().channel?.selectedChannelId).toBe("email")
    await wizard.continue()
    expect(wizard.getSnapshot().offer?.selectedStanceId).toBe("no-offer")

    wizard.close()
    expect(wizard.getSnapshot().isOpen).toBe(false)
    expect(wizard.getSnapshot().templateId).toBeNull()
  })

  it("maps catalogue suggestion aliases onto wizard audience / channel / offer ids", async () => {
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
    })

    await wizard.openFromTemplate({
      locationId: 7,
      locationName: "Soho",
      template: sampleTemplateDetail({
        id: "quiet-time-boost",
        suggestions: {
          goalId: "boost-quieter-time",
          audienceKey: "all-eligible-or-saved-group",
          channel: "email-or-sms",
          offerStance: "recommended",
        },
      }),
    })

    const snapshot = wizard.getSnapshot()
    expect(snapshot.stepId).toBe("audience")
    expect(snapshot.goalId).toBe("boost-quieter-time")
    expect(snapshot.audience?.selectedAudienceId).toBe("all-eligible-guests")

    await wizard.continue()
    expect(wizard.getSnapshot().channel?.selectedChannelId).toBe("email")
    await wizard.continue()
    expect(wizard.getSnapshot().offer?.selectedStanceId).toBe("create-new-offer")
  })

  it("closes without Save and clears the client session (no Draft row)", async () => {
    const createDraft = vi.fn()
    const wizard = createCampaignWizardModule({
      createDraft,
    })

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
    expect(snapshot.draftId).toBeNull()
    expect(createDraft).not.toHaveBeenCalled()
  })

  it("Save and exit creates a Draft then closes", async () => {
    const createDraft = vi.fn(async () => ({
      id: 91,
      locationId: 7,
      status: "draft" as const,
      name: "Thank recent guests",
      goalId: "thank-recent-guests",
      templateId: null,
      templateVersion: null,
      audienceKey: "all-eligible-guests",
      channel: "email",
      offerStance: "no-offer",
      messageSubject: null,
      messageBody: "Hello guests",
      rowVersion: 1,
      createdAt: "2026-08-08T00:00:00Z",
      updatedAt: "2026-08-08T00:00:00Z",
    }))

    const wizard = createCampaignWizardModule({ createDraft })
    wizard.openBlankCreate({
      locationId: 7,
      locationName: "Soho",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue() // audience
    await wizard.continue() // channel
    await wizard.continue() // offer
    await wizard.continue() // message
    wizard.writeManually()
    wizard.setMessage("Hello guests")

    await wizard.saveAndExit()

    expect(createDraft).toHaveBeenCalledTimes(1)
    expect(createDraft).toHaveBeenCalledWith(
      expect.objectContaining({
        locationId: 7,
        goalId: "thank-recent-guests",
        audienceKey: "all-eligible-guests",
        channel: "email",
        offerStance: "no-offer",
        messageBody: "Hello guests",
        templateId: null,
      })
    )
    expect(wizard.getSnapshot().isOpen).toBe(false)
    expect(wizard.getSnapshot().draftId).toBeNull()
  })

  it("Save keeps the wizard open and later Save and exit PATCHes", async () => {
    const createDraft = vi.fn(async () => ({
      id: 44,
      locationId: 3,
      status: "draft" as const,
      name: "Thank recent guests",
      goalId: "thank-recent-guests",
      templateId: null,
      templateVersion: null,
      audienceKey: "all-eligible-guests",
      channel: "email",
      offerStance: "no-offer",
      messageSubject: null,
      messageBody: null,
      rowVersion: 1,
      createdAt: "2026-08-08T00:00:00Z",
      updatedAt: "2026-08-08T00:00:00Z",
    }))
    const updateDraft = vi.fn(async () => ({
      id: 44,
      locationId: 3,
      status: "draft" as const,
      name: "Thank recent guests",
      goalId: "thank-recent-guests",
      templateId: null,
      templateVersion: null,
      audienceKey: "all-eligible-guests",
      channel: "sms",
      offerStance: "no-offer",
      messageSubject: null,
      messageBody: null,
      rowVersion: 2,
      createdAt: "2026-08-08T00:00:00Z",
      updatedAt: "2026-08-08T00:01:00Z",
    }))

    const wizard = createCampaignWizardModule({ createDraft, updateDraft })
    wizard.openBlankCreate({
      locationId: 3,
      locationName: "Shoreditch",
    })
    wizard.setGoalId("thank-recent-guests")

    await wizard.save()
    expect(createDraft).toHaveBeenCalledTimes(1)
    expect(wizard.getSnapshot().isOpen).toBe(true)
    expect(wizard.getSnapshot().draftId).toBe(44)
    expect(wizard.getSnapshot().saveStatus).toBe("saved")

    await wizard.continue()
    await wizard.continue()
    wizard.setChannelId("sms")

    await wizard.saveAndExit()
    expect(updateDraft).toHaveBeenCalledTimes(1)
    expect(updateDraft).toHaveBeenCalledWith(
      44,
      expect.objectContaining({
        rowVersion: 1,
        channel: "sms",
      })
    )
    expect(wizard.getSnapshot().isOpen).toBe(false)
  })

  it("Save and exit from a template snapshots template id and version", async () => {
    const createDraft = vi.fn(async (body) => ({
      id: 12,
      locationId: body.locationId,
      status: "draft" as const,
      name: "Thank recent guests",
      goalId: body.goalId ?? null,
      templateId: body.templateId ?? null,
      templateVersion: body.templateVersion ?? null,
      audienceKey: body.audienceKey ?? null,
      channel: body.channel ?? null,
      offerStance: body.offerStance ?? null,
      messageSubject: body.messageSubject ?? null,
      messageBody: body.messageBody ?? null,
      rowVersion: 1,
      createdAt: "2026-08-08T00:00:00Z",
      updatedAt: "2026-08-08T00:00:00Z",
    }))

    const wizard = createCampaignWizardModule({ createDraft })
    await wizard.openFromTemplate({
      locationId: 9,
      locationName: "Brixton",
      template: sampleTemplateDetail(),
    })

    await wizard.saveAndExit()

    expect(createDraft).toHaveBeenCalledWith(
      expect.objectContaining({
        locationId: 9,
        templateId: "thank-recent-guests",
        templateVersion: 1,
        goalId: "thank-recent-guests",
      })
    )
    expect(wizard.getSnapshot().isOpen).toBe(false)
  })

  it("Continue editing opens get-by-id Draft at Schedule when message is saved", async () => {
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
    })

    await wizard.openFromDraft({
      locationName: "Camden",
      draft: {
        id: 55,
        locationId: 42,
        status: "draft",
        name: "Tuesday lunch reminder",
        goalId: "boost-quieter-time",
        templateId: null,
        templateVersion: null,
        audienceKey: "new-guests",
        channel: "sms",
        offerStance: "no-offer",
        messageSubject: null,
        messageBody: "Come for lunch",
        rowVersion: 3,
        createdAt: "2026-08-07T10:00:00Z",
        updatedAt: "2026-08-08T10:00:00Z",
      },
    })

    const snapshot = wizard.getSnapshot()
    expect(snapshot.isOpen).toBe(true)
    expect(snapshot.draftId).toBe(55)
    expect(snapshot.stepId).toBe("schedule")
    expect(snapshot.goalId).toBe("boost-quieter-time")
    expect(snapshot.locationId).toBe(42)
    expect(snapshot.headerSubtitle).toBe(
      "Boost a quieter time · Camden · August"
    )

    wizard.back()
    expect(wizard.getSnapshot().stepId).toBe("message")
    expect(wizard.getSnapshot().message?.body).toBe("Come for lunch")
    expect(wizard.getSnapshot().message?.writeEntry).toBe("editor")
  })

  it("Continue editing opens at Audience when Draft has goal but no message", async () => {
    const loadSmartGroupCounts = vi.fn(async () => ({
      smartGroupCounts: { "all-guests": 10 },
    }))
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      loadSmartGroupCounts,
    })

    await wizard.openFromDraft({
      locationName: "Camden",
      draft: {
        id: 56,
        locationId: 42,
        status: "draft",
        name: "Quiet time",
        goalId: "boost-quieter-time",
        templateId: null,
        templateVersion: null,
        audienceKey: "new-guests",
        channel: "sms",
        offerStance: "no-offer",
        messageSubject: null,
        messageBody: null,
        rowVersion: 1,
        createdAt: "2026-08-08T10:00:00Z",
        updatedAt: "2026-08-08T10:00:00Z",
      },
    })

    expect(wizard.getSnapshot().stepId).toBe("audience")
    expect(wizard.getSnapshot().audience?.selectedAudienceId).toBe("new-guests")
    expect(loadSmartGroupCounts).toHaveBeenCalledWith({ locationId: 42 })

    await wizard.continue()
    expect(wizard.getSnapshot().channel?.selectedChannelId).toBe("sms")
  })

  it("Continue editing opens at Goal when Draft has no goalId", async () => {
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
    })

    await wizard.openFromDraft({
      locationName: "Soho",
      draft: {
        id: 12,
        locationId: 7,
        status: "draft",
        name: "Untitled draft",
        goalId: null,
        templateId: null,
        templateVersion: null,
        audienceKey: null,
        channel: null,
        offerStance: null,
        messageSubject: null,
        messageBody: null,
        rowVersion: 1,
        createdAt: "2026-08-08T00:00:00Z",
        updatedAt: "2026-08-08T00:00:00Z",
      },
    })

    const snapshot = wizard.getSnapshot()
    expect(snapshot.isOpen).toBe(true)
    expect(snapshot.draftId).toBe(12)
    expect(snapshot.stepId).toBe("goal")
    expect(snapshot.goalId).toBeNull()
    expect(snapshot.showNumberedStepper).toBe(false)
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

  it("continues from Channel into Offer with No offer selected by default", async () => {
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
    await wizard.continue()

    const snapshot = wizard.getSnapshot()
    expect(snapshot.stepId).toBe("offer")
    expect(snapshot.activeNumberedStepIndex).toBe(2)
    expect(snapshot.placeholderBody).toBeNull()
    expect(snapshot.channel).toBeNull()
    expect(snapshot.offer).not.toBeNull()
    expect(snapshot.offer!.selectedStanceId).toBe("no-offer")
    expect(snapshot.offer!.options.map((option) => option.id)).toEqual([
      "no-offer",
      "existing-offer",
      "create-new-offer",
    ])
    expect(snapshot.offer!.options.find((o) => o.id === "no-offer")?.selected).toBe(
      true
    )
    expect(snapshot.canContinue).toBe(true)
  })

  it("persists No offer and select-path stance choices in wizard state without a live catalog", async () => {
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
    await wizard.continue()

    expect(wizard.getSnapshot().offer!.selectedStanceId).toBe("no-offer")

    wizard.setOfferStanceId("existing-offer")
    expect(wizard.getSnapshot().offer!.selectedStanceId).toBe("existing-offer")
    expect(
      wizard.getSnapshot().offer!.options.find((o) => o.id === "existing-offer")
        ?.selected
    ).toBe(true)
    // Shell select path only — no offer id / catalog attachment.
    expect(wizard.getSnapshot().offer!.attachedOfferId).toBeNull()

    wizard.setOfferStanceId("create-new-offer")
    expect(wizard.getSnapshot().offer!.selectedStanceId).toBe("create-new-offer")
    expect(wizard.getSnapshot().offer!.attachedOfferId).toBeNull()

    wizard.setOfferStanceId("no-offer")
    expect(wizard.getSnapshot().offer!.selectedStanceId).toBe("no-offer")
    expect(wizard.getSnapshot().offer!.attachedOfferId).toBeNull()

    // Stance survives leave/return via Continue → Back.
    await wizard.continue()
    expect(wizard.getSnapshot().stepId).toBe("message")
    wizard.back()
    expect(wizard.getSnapshot().stepId).toBe("offer")
    expect(wizard.getSnapshot().offer!.selectedStanceId).toBe("no-offer")

    wizard.setOfferStanceId("existing-offer")
    wizard.back()
    expect(wizard.getSnapshot().stepId).toBe("channel")
    await wizard.continue()
    expect(wizard.getSnapshot().offer!.selectedStanceId).toBe("existing-offer")
  })

  it("builds Offer estimated usage from the same Channel messaging fixtures", async () => {
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
    await wizard.continue()

    const offer = wizard.getSnapshot().offer
    expect(offer).not.toBeNull()
    expect(offer!.usageSummary.audienceLine).toBe(
      "New guests · 162 eligible through at least one channel"
    )
    expect(offer!.usageSummary.rows).toEqual([
      { label: "Eligible recipients", value: "148" },
      { label: "Estimated email messages", value: "148" },
      { label: "Allowance remaining", value: "6,760" },
      { label: "Estimated remaining after send", value: "6,612" },
    ])
    expect(offer!.messagingFixture).toEqual(MESSAGING_USAGE_FIXTURE)
  })

  it("Message starts on chooser; Write manually yields operator-owned subject and body", async () => {
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
    await wizard.continue()
    await wizard.continue()

    let snapshot = wizard.getSnapshot()
    expect(snapshot.stepId).toBe("message")
    expect(snapshot.placeholderBody).toBeNull()
    expect(snapshot.message).not.toBeNull()
    expect(snapshot.message!.writeEntry).toBe("chooser")
    expect(snapshot.message!.prepareAiLive).toBe(false)
    expect(snapshot.message!.showSubject).toBe(true)
    expect(snapshot.canContinue).toBe(false)

    wizard.writeManually()
    snapshot = wizard.getSnapshot()
    expect(snapshot.message!.writeEntry).toBe("editor")
    expect(snapshot.canContinue).toBe(false)

    wizard.setSubject("Thanks for visiting")
    wizard.setMessage("Hi guest,\n\nThank you for joining us.")
    snapshot = wizard.getSnapshot()
    expect(snapshot.message!.subject).toBe("Thanks for visiting")
    expect(snapshot.message!.body).toBe(
      "Hi guest,\n\nThank you for joining us."
    )
    expect(snapshot.canContinue).toBe(true)

    // Prepare stub must not call a network adapter or force editor entry alone.
    wizard.prepareDraftStub()
    expect(wizard.getSnapshot().message!.writeEntry).toBe("editor")
    expect(wizard.getSnapshot().message!.prepareAiLive).toBe(false)

    // Text survives leave/return via Continue → Back.
    await wizard.continue()
    expect(wizard.getSnapshot().stepId).toBe("schedule")
    wizard.back()
    snapshot = wizard.getSnapshot()
    expect(snapshot.stepId).toBe("message")
    expect(snapshot.message!.writeEntry).toBe("editor")
    expect(snapshot.message!.subject).toBe("Thanks for visiting")
    expect(snapshot.message!.body).toBe(
      "Hi guest,\n\nThank you for joining us."
    )
  })

  it("Guest preview opens from Message with Send test unavailable", async () => {
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
    await wizard.continue()
    await wizard.continue()

    wizard.writeManually()
    wizard.setSubject("Thanks for visiting")
    wizard.setMessage("Preview body")

    expect(wizard.getSnapshot().message!.guestPreviewOpen).toBe(false)
    expect(wizard.getSnapshot().message!.sendTestAvailable).toBe(false)

    wizard.openGuestPreview()
    expect(wizard.getSnapshot().message!.guestPreviewOpen).toBe(true)
    expect(wizard.getSnapshot().message!.sendTestAvailable).toBe(false)
    expect(wizard.getSnapshot().message!.channelId).toBe("email")
    expect(wizard.getSnapshot().message!.body).toBe("Preview body")

    wizard.closeGuestPreview()
    expect(wizard.getSnapshot().message!.guestPreviewOpen).toBe(false)
    expect(wizard.getSnapshot().message!.sendTestAvailable).toBe(false)
  })

  it("Schedule step offers Send now / Schedule for later chrome without reservation APIs", async () => {
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
    await wizard.continue()
    await wizard.continue()
    wizard.writeManually()
    wizard.setSubject("Thanks for visiting")
    wizard.setMessage("Hi guest,\n\nThank you for joining us.")
    await wizard.continue()

    let snapshot = wizard.getSnapshot()
    expect(snapshot.stepId).toBe("schedule")
    expect(snapshot.placeholderBody).toBeNull()
    expect(snapshot.schedule).not.toBeNull()
    expect(snapshot.schedule!.selectedModeId).toBe("send-now")
    expect(snapshot.schedule!.options.map((option) => option.id)).toEqual([
      "send-now",
      "schedule-later",
    ])
    expect(snapshot.schedule!.options[0]?.title).toBe("Send now")
    expect(snapshot.schedule!.options[1]?.title).toBe("Schedule for later")
    expect(snapshot.canContinue).toBe(true)
    expect(snapshot.primaryActionLabel).toBe(CAMPAIGN_WIZARD_COPY.continue)
    expect(snapshot.schedule!.usageSummary.rows).toEqual([
      { label: "Eligible recipients", value: "148" },
      { label: "Estimated email messages", value: "148" },
      { label: "Allowance remaining", value: "6,760" },
      { label: "Estimated remaining after send", value: "6,612" },
    ])

    wizard.setScheduleModeId("schedule-later")
    snapshot = wizard.getSnapshot()
    expect(snapshot.schedule!.selectedModeId).toBe("schedule-later")
    expect(snapshot.schedule!.options.find((o) => o.id === "schedule-later")?.selected).toBe(
      true
    )
    expect(snapshot.canContinue).toBe(true)

    // Timing selection is client-only — no reservation / send commands.
    expect(wizard).not.toHaveProperty("sendCampaign")
    expect(wizard).not.toHaveProperty("scheduleCommit")
    expect(wizard).not.toHaveProperty("reserveSchedule")
  })

  it("Review step summarises wizard state and blocks send / schedule-commit", async () => {
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
    wizard.setChannelId("email")
    await wizard.continue()
    wizard.setOfferStanceId("no-offer")
    await wizard.continue()
    wizard.writeManually()
    wizard.setSubject("Thanks for visiting")
    wizard.setMessage("Hi guest,\n\nThank you for joining us.")
    await wizard.continue()
    wizard.setScheduleModeId("send-now")
    await wizard.continue()

    let snapshot = wizard.getSnapshot()
    expect(snapshot.stepId).toBe("review")
    expect(snapshot.placeholderBody).toBeNull()
    expect(snapshot.canContinue).toBe(false)
    expect(snapshot.primaryActionLabel).toBe("Send campaign now")
    expect(snapshot.review).not.toBeNull()
    expect(snapshot.review!.sendAvailable).toBe(false)
    expect(snapshot.review!.sections.map((section) => section.id)).toEqual([
      "campaign",
      "audience",
      "channel",
      "message",
      "offer",
      "schedule",
      "usage",
    ])
    expect(snapshot.review!.sections[0]?.rows).toEqual([
      { label: "Goal", value: "Thank recent guests" },
      { label: "Location", value: "Camden" },
    ])
    expect(snapshot.review!.sections[1]?.rows).toEqual([
      { label: "Audience", value: "New guests" },
    ])
    expect(snapshot.review!.sections[2]?.rows).toEqual([
      { label: "Channel", value: "Email" },
      { label: "Sender", value: "—" },
    ])
    expect(snapshot.review!.sections[3]?.rows).toEqual([
      { label: "Subject", value: "Thanks for visiting" },
      {
        label: "Message",
        value: "Hi guest,\n\nThank you for joining us.",
      },
    ])
    expect(snapshot.review!.sections[4]?.rows).toEqual([
      { label: "Offer", value: "No offer" },
    ])
    expect(snapshot.review!.sections[5]?.rows).toEqual([
      { label: "Timing", value: "Send now" },
    ])
    expect(snapshot.review!.guestPreview.channelId).toBe("email")
    expect(snapshot.review!.guestPreview.sendTestAvailable).toBe(false)
    expect(snapshot.review!.guestPreview.body).toBe(
      "Hi guest,\n\nThank you for joining us."
    )

    // Continue on Review must not advance or hit execution endpoints.
    await wizard.continue()
    expect(wizard.getSnapshot().stepId).toBe("review")
    expect(wizard.getSnapshot().canContinue).toBe(false)

    // Schedule / send commit commands are absent (ticket 27 — no-send slice).
    expect(wizard).not.toHaveProperty("sendCampaign")
    expect(wizard).not.toHaveProperty("scheduleCommit")
    expect(wizard).not.toHaveProperty("reserveSchedule")

    wizard.openGuestPreview()
    expect(wizard.getSnapshot().review!.guestPreview.guestPreviewOpen).toBe(
      true
    )
    wizard.closeGuestPreview()
    expect(wizard.getSnapshot().review!.guestPreview.guestPreviewOpen).toBe(
      false
    )

    wizard.editMessageFromReview()
    snapshot = wizard.getSnapshot()
    expect(snapshot.stepId).toBe("message")
    expect(snapshot.message!.writeEntry).toBe("editor")
    expect(snapshot.message!.subject).toBe("Thanks for visiting")
  })

  it("opens from recommendation draftPrefill without creating a server Draft", async () => {
    const createDraft = vi.fn()
    const loadSmartGroupCounts = vi.fn(async () => ({
      smartGroupCounts: { "all-guests": 40, "new-guests": 5 },
    }))
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-08T12:00:00.000Z"),
      createDraft,
      loadSmartGroupCounts,
    })

    await wizard.openFromRecommendation({
      locationId: 42,
      locationName: "Camden",
      draftPrefill: {
        goalId: "thank-recent-guests",
        audienceKey: "new-guests",
        channel: "email",
        offerStance: "no-offer",
        campaignName: "Thank you for joining",
        messageSubject: "Thanks for joining us",
        messageBody: "Thank you for joining our guest list.",
      },
    })

    let snapshot = wizard.getSnapshot()
    expect(snapshot.isOpen).toBe(true)
    expect(snapshot.stepId).toBe("audience")
    expect(snapshot.goalId).toBe("thank-recent-guests")
    expect(snapshot.audience?.selectedAudienceId).toBe("new-guests")
    expect(snapshot.templateId).toBeNull()
    expect(snapshot.draftId).toBeNull()
    expect(createDraft).not.toHaveBeenCalled()
    expect(loadSmartGroupCounts).toHaveBeenCalledWith({ locationId: 42 })

    await wizard.continue()
    expect(wizard.getSnapshot().channel?.selectedChannelId).toBe("email")
    await wizard.continue()
    expect(wizard.getSnapshot().offer?.selectedStanceId).toBe("no-offer")
    await wizard.continue()
    snapshot = wizard.getSnapshot()
    expect(snapshot.message?.writeEntry).toBe("editor")
    expect(snapshot.message?.subject).toBe("Thanks for joining us")
    expect(snapshot.message?.body).toBe(
      "Thank you for joining our guest list."
    )
    await wizard.continue()
    expect(wizard.getSnapshot().schedule?.selectedModeId).toBe("send-now")

    wizard.close()
    expect(wizard.getSnapshot().isOpen).toBe(false)
    expect(createDraft).not.toHaveBeenCalled()
  })

  it("Save after recommendation prefill creates a Draft without template id", async () => {
    const createDraft = vi.fn(async (body) => ({
      id: 77,
      locationId: body.locationId,
      status: "draft" as const,
      name: body.name ?? "Thank recent guests",
      goalId: body.goalId ?? null,
      templateId: body.templateId ?? null,
      templateVersion: body.templateVersion ?? null,
      audienceKey: body.audienceKey ?? null,
      channel: body.channel ?? null,
      offerStance: body.offerStance ?? null,
      messageSubject: body.messageSubject ?? null,
      messageBody: body.messageBody ?? null,
      rowVersion: 1,
      createdAt: "2026-08-08T12:00:00.000Z",
      updatedAt: "2026-08-08T12:00:00.000Z",
    }))
    const wizard = createCampaignWizardModule({ createDraft })

    await wizard.openFromRecommendation({
      locationId: 42,
      locationName: "Camden",
      draftPrefill: {
        goalId: "thank-recent-guests",
        audienceKey: "new-guests",
        channel: "sms",
        offerStance: "no-offer",
        campaignName: "Thank you for joining",
        messageSubject: null,
        messageBody: "Thank you for joining our guest list.",
      },
    })
    expect(createDraft).not.toHaveBeenCalled()

    await wizard.save()

    expect(createDraft).toHaveBeenCalledTimes(1)
    expect(createDraft).toHaveBeenCalledWith(
      expect.objectContaining({
        locationId: 42,
        name: "Thank you for joining",
        goalId: "thank-recent-guests",
        audienceKey: "new-guests",
        channel: "sms",
        offerStance: "no-offer",
        messageSubject: null,
        messageBody: "Thank you for joining our guest list.",
        templateId: null,
        templateVersion: null,
      })
    )
    const createArg = createDraft.mock.calls[0]![0]
    expect(createArg).not.toHaveProperty("recipients")
    expect(wizard.getSnapshot().draftId).toBe(77)
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

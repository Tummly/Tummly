import { readFileSync } from "node:fs"
import { resolve } from "node:path"

import { describe, expect, it, vi } from "vitest"

import { CampaignDraftHttp409Error } from "@/lib/operatorCampaigns/campaignDraftHttp409Error"
import type {
  CampaignAudienceEligibilityBreakdown,
  CampaignAudienceId,
} from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import {
  unavailableCampaignAudienceEligibilityBreakdown,
} from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import { resolveCampaignChannelSmsShortfall } from "@/lib/operatorCampaigns/campaignChannelPresentation"
import { CAMPAIGN_COMMIT_COPY } from "@/lib/operatorCampaigns/campaignCommitPresentation"
import {
  CAMPAIGN_SEND_TEST_COPY,
  CAMPAIGN_SEND_TEST_SAMPLE_OFFER,
} from "@/lib/operatorCampaigns/campaignSendTestPresentation"
import { CampaignBillingReserveUnavailableError } from "@/lib/operatorCampaigns/campaignBillingReserveUnavailableError"
import {
  CAMPAIGN_GOAL_OPTIONS,
  CAMPAIGN_WIZARD_COPY,
  CAMPAIGN_WIZARD_NUMBERED_STEPS,
  CAMPAIGN_WIZARD_SELECT_MENU_CLASS,
} from "@/lib/operatorCampaigns/campaignWizardPresentation"
import { createCampaignWizardModule } from "@/lib/operatorCampaigns/createCampaignWizardModule"
import type { PrepareCampaignMessageDraftResult } from "@/lib/operatorCampaigns/createCampaignWizardModule"
import { MESSAGING_USAGE_FIXTURE } from "@/lib/operatorCampaigns/messagingUsageFixtures"
import type { CampaignTemplateDetail } from "@/types/operatorCampaigns"

const campaignWizardDialogSource = readFileSync(
  resolve(
    process.cwd(),
    "src/components/dashboard/operator/Campaigns/CampaignWizardDialog.tsx"
  ),
  "utf8"
)

const campaignsPageSource = readFileSync(
  resolve(
    process.cwd(),
    "src/components/dashboard/operator/Campaigns/CampaignsPage.tsx"
  ),
  "utf8"
)

function sampleDraftDetail() {
  return {
    id: 91,
    locationId: 42,
    status: "draft",
    name: "Thanks campaign",
    goalId: "thank-recent-guests",
    templateId: null,
    templateVersion: null,
    audienceKey: "all-eligible-guests",
    channel: "email",
    offerStance: "no-offer",
    offerId: null,
    messageSubject: "Thanks for visiting",
    messageBody: "Hi guest",
    rowVersion: "r1",
    createdAt: "2026-08-14T14:00:00.000Z",
    updatedAt: "2026-08-14T14:00:00.000Z",
  }
}

async function walkToReview(
  wizard: ReturnType<typeof createCampaignWizardModule>
) {
  wizard.openBlankCreate({ locationId: 42, locationName: "Camden" })
  wizard.setGoalId("thank-recent-guests")
  await wizard.continue()
  await wizard.continue()
  await wizard.continue()
  await wizard.continue()
  wizard.writeManually()
  wizard.setSubject("Thanks for visiting")
  wizard.setMessage("Hi guest")
  await wizard.continue()
  await wizard.continue()
}

function liveEligibility(
  overrides: Partial<CampaignAudienceEligibilityBreakdown> = {}
): CampaignAudienceEligibilityBreakdown {
  return {
    matched: 10,
    currentlyEligible: 8,
    excluded: 2,
    emailEligible: 7,
    smsEligible: 5,
    excludedReasons: [{ reason: "opt-out", count: 2 }],
    source: "live",
    ...overrides,
  }
}

function defaultAudienceAdapters(overrides: {
  loadSmartGroupCounts?: ReturnType<typeof vi.fn>
  loadAudienceEligibility?: (
    input: { locationId: number; audienceKey: CampaignAudienceId }
  ) => Promise<CampaignAudienceEligibilityBreakdown>
} = {}) {
  return {
    loadSmartGroupCounts:
      overrides.loadSmartGroupCounts
      ?? vi.fn(async () => ({
        smartGroupCounts: {
          "all-guests": 200,
          "new-guests": 31,
          "needs-recovery": 8,
          "positive-feedback": 44,
          "offer-not-redeemed": 99,
          "recent-redeemers": 88,
          "dormant-guests": 17,
        },
      })),
    loadAudienceEligibility:
      overrides.loadAudienceEligibility
      ?? (async () => liveEligibility()),
  }
}

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
    preview: {
      summary: {
        goal: "Thank guests who were recently captured.",
        bestFor: "New captures",
        suggestedAudience: "New guests",
        suggestedChannel: "Email",
        offer: "Optional",
      },
      suggestedChannels: ["email"],
      messages: [
        {
          channel: "email",
          estimatedUsageLabel: "12 email messages",
          body: "Thanks for visiting.",
          subject: "Thanks",
          offerBlock: null,
        },
      ],
      offerLogic: null,
      eligibility: {
        emailCount: 12,
        smsCount: 0,
        totalUniqueGuests: 12,
      },
      suggestedTiming: "Send within 48 hours of capture.",
      footerDisclaimer:
        "You'll be able to review the audience, offer, message, cost and final recipient count before anything is sent.",
    },
    ...overrides,
  }
}

describe("createCampaignWizardModule", () => {
  it("opens blank Create at Goal with no template and no server draft", () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
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
      ...defaultAudienceAdapters(),
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
      ...defaultAudienceAdapters(),
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
      ...defaultAudienceAdapters(),
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
      offerId: null,
      messageSubject: null,
      messageBody: "Hello guests",
      rowVersion: "AAAAAAAAB9E=",
      createdAt: "2026-08-08T00:00:00Z",
      updatedAt: "2026-08-08T00:00:00Z",
    }))

    const wizard = createCampaignWizardModule({ ...defaultAudienceAdapters(), createDraft })
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
      offerId: null,
      messageSubject: null,
      messageBody: null,
      rowVersion: "AAAAAAAAB9E=",
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
      offerId: null,
      messageSubject: null,
      messageBody: null,
      rowVersion: "AAAAAAAAB9F=",
      createdAt: "2026-08-08T00:00:00Z",
      updatedAt: "2026-08-08T00:01:00Z",
    }))

    const wizard = createCampaignWizardModule({ ...defaultAudienceAdapters(), createDraft, updateDraft })
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
        rowVersion: "AAAAAAAAB9E=",
        channel: "sms",
      })
    )
    expect(wizard.getSnapshot().isOpen).toBe(false)
  })

  it("Save maps Draft HTTP 409 message into saveError", async () => {
    const conflictMessage =
      "This campaign was updated elsewhere. Reload and try again."
    const createDraft = vi.fn(async () => ({
      id: 55,
      locationId: 3,
      status: "draft" as const,
      name: "Thank recent guests",
      goalId: "thank-recent-guests",
      templateId: null,
      templateVersion: null,
      audienceKey: "all-eligible-guests",
      channel: "email",
      offerStance: "no-offer",
      offerId: null,
      messageSubject: null,
      messageBody: null,
      rowVersion: "AAAAAAAAB9E=",
      createdAt: "2026-08-08T00:00:00Z",
      updatedAt: "2026-08-08T00:00:00Z",
    }))
    const updateDraft = vi.fn(async () => {
      throw new CampaignDraftHttp409Error(conflictMessage)
    })

    const wizard = createCampaignWizardModule({ ...defaultAudienceAdapters(), createDraft, updateDraft })
    wizard.openBlankCreate({
      locationId: 3,
      locationName: "Shoreditch",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.save()
    expect(wizard.getSnapshot().draftId).toBe(55)

    await wizard.save()
    expect(updateDraft).toHaveBeenCalledTimes(1)
    expect(wizard.getSnapshot().saveStatus).toBe("error")
    expect(wizard.getSnapshot().saveError).toBe(conflictMessage)
    expect(wizard.getSnapshot().isOpen).toBe(true)
  })

  it("Save and exit maps Draft HTTP 409 message into saveError and stays open", async () => {
    const conflictMessage = "Only draft campaigns can be updated."
    const createDraft = vi.fn(async () => ({
      id: 56,
      locationId: 3,
      status: "draft" as const,
      name: "Thank recent guests",
      goalId: "thank-recent-guests",
      templateId: null,
      templateVersion: null,
      audienceKey: "all-eligible-guests",
      channel: "email",
      offerStance: "no-offer",
      offerId: null,
      messageSubject: null,
      messageBody: null,
      rowVersion: "AAAAAAAAB9E=",
      createdAt: "2026-08-08T00:00:00Z",
      updatedAt: "2026-08-08T00:00:00Z",
    }))
    const updateDraft = vi.fn(async () => {
      throw new CampaignDraftHttp409Error(conflictMessage)
    })

    const wizard = createCampaignWizardModule({ ...defaultAudienceAdapters(), createDraft, updateDraft })
    wizard.openBlankCreate({
      locationId: 3,
      locationName: "Shoreditch",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.save()

    await wizard.saveAndExit()
    expect(updateDraft).toHaveBeenCalledTimes(1)
    expect(wizard.getSnapshot().saveStatus).toBe("error")
    expect(wizard.getSnapshot().saveError).toBe(conflictMessage)
    expect(wizard.getSnapshot().isOpen).toBe(true)
  })

  it("Save keeps the generic message when the failure is not HTTP 409", async () => {
    const createDraft = vi.fn(async () => {
      throw new Error("Request failed with status code 500")
    })

    const wizard = createCampaignWizardModule({ ...defaultAudienceAdapters(), createDraft })
    wizard.openBlankCreate({
      locationId: 3,
      locationName: "Shoreditch",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.save()

    expect(wizard.getSnapshot().saveStatus).toBe("error")
    expect(wizard.getSnapshot().saveError).toBe(
      "Could not save this campaign draft. Try again."
    )
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
      offerId: body.offerId ?? null,
      messageSubject: body.messageSubject ?? null,
      messageBody: body.messageBody ?? null,
      rowVersion: "AAAAAAAAB9E=",
      createdAt: "2026-08-08T00:00:00Z",
      updatedAt: "2026-08-08T00:00:00Z",
    }))

    const wizard = createCampaignWizardModule({ ...defaultAudienceAdapters(), createDraft })
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
      ...defaultAudienceAdapters(),
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
        offerId: null,
        messageSubject: null,
        messageBody: "Come for lunch",
        rowVersion: "AAAAAAAAB9E=",
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
      ...defaultAudienceAdapters(),
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
        offerId: null,
        messageSubject: null,
        messageBody: null,
        rowVersion: "AAAAAAAAB9E=",
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
      ...defaultAudienceAdapters(),
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
        offerId: null,
        messageSubject: null,
        messageBody: null,
        rowVersion: "AAAAAAAAB9E=",
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
      ...defaultAudienceAdapters(),
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

  it("wires live Smart Group counts for evaluable groups and keeps unevaluable cards honest", async () => {
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
      ...defaultAudienceAdapters({
        loadSmartGroupCounts,
        loadAudienceEligibility: async ({ audienceKey }) =>
          liveEligibility({
            matched:
              audienceKey === "all-eligible-guests"
                ? 200
                : audienceKey === "new-guests"
                  ? 31
                  : audienceKey === "positive-feedback"
                    ? 44
                    : audienceKey === "dormant-guests"
                      ? 17
                      : 10,
            currentlyEligible: 8,
          }),
      }),
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

    expect(byId["all-eligible-guests"]?.countSource).toBe("live-eligibility")
    expect(byId["all-eligible-guests"]?.matched).toBe(200)
    expect(byId["all-eligible-guests"]?.currentlyEligible).toBe(8)
    expect(byId["new-guests"]?.countSource).toBe("live-eligibility")
    expect(byId["new-guests"]?.matched).toBe(31)
    expect(byId["positive-feedback"]?.countSource).toBe("live-eligibility")
    expect(byId["positive-feedback"]?.matched).toBe(44)
    expect(byId["dormant-guests"]?.countSource).toBe("live-eligibility")
    expect(byId["dormant-guests"]?.matched).toBe(17)

    expect(byId["offer-not-redeemed"]?.countSource).toBe("unavailable")
    expect(byId["offer-not-redeemed"]?.matched).toBeNull()
    expect(byId["offer-not-redeemed"]?.currentlyEligible).toBeNull()
    expect(byId["offer-not-redeemed"]?.countLabel).toBe("Counts unavailable")
    expect(byId["recent-redeemers"]?.countSource).toBe("unavailable")
    expect(byId["recent-redeemers"]?.matched).toBeNull()
    expect(byId["no-recent-tummly-activity"]?.countSource).toBe("unavailable")
    expect(byId["no-recent-tummly-activity"]?.matched).toBeNull()
    expect(byId["no-recent-tummly-activity"]?.title).toBe(
      "No recent Tummly activity"
    )
    expect(byId["no-recent-tummly-activity"]?.description).toMatch(
      /30 days \(UTC\)/
    )
    expect(byId["dormant-guests"]?.title).toBe("Dormant guests")
    expect(byId["dormant-guests"]?.description).not.toBe(
      byId["no-recent-tummly-activity"]?.description
    )
    expect(byId["saved-group"]).toBeUndefined()
    expect(
      audience!.options.some((option) => option.id === "saved-group")
    ).toBe(false)
  })

  it("blocks Continue for unevaluable audiences and legacy saved-group drafts", async () => {
    const updateDraft = vi.fn(async () => ({
      id: 7,
      rowVersion: "rv-2",
      templateId: null,
      templateVersion: null,
    }))
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      updateDraft,
      ...defaultAudienceAdapters({
        loadSmartGroupCounts: vi.fn(async () => ({
          smartGroupCounts: { "all-guests": 10, "dormant-guests": 3 },
        })),
      }),
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()

    for (const audienceId of [
      "offer-not-redeemed",
      "recent-redeemers",
      "no-recent-tummly-activity",
    ] as const) {
      wizard.setAudienceId(audienceId)
      expect(wizard.getSnapshot().canContinue).toBe(false)
      await wizard.continue()
      expect(wizard.getSnapshot().stepId).toBe("audience")
    }

    wizard.setAudienceId("dormant-guests")
    expect(wizard.getSnapshot().canContinue).toBe(true)

    await wizard.openFromDraft({
      locationName: "Camden",
      draft: {
        id: 7,
        locationId: 42,
        status: "draft",
        name: "Legacy saved group",
        goalId: "thank-recent-guests",
        templateId: null,
        templateVersion: null,
        audienceKey: "saved-group",
        channel: "email",
        offerStance: "no-offer",
        messageSubject: null,
        messageBody: null,
        rowVersion: "AAAAAAAAB9E=",
        createdAt: "2026-08-01T10:00:00Z",
        updatedAt: "2026-08-01T10:00:00Z",
      },
    })

    expect(wizard.getSnapshot().audience?.selectedAudienceId).toBe(
      "saved-group"
    )
    expect(wizard.getSnapshot().canContinue).toBe(false)
    await wizard.save()
    expect(updateDraft).not.toHaveBeenCalled()
    expect(wizard.getSnapshot().saveStatus).toBe("error")
  })

  it("exposes live Campaign eligibility breakdown with Excluded reason rollup", async () => {
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      ...defaultAudienceAdapters({
        loadAudienceEligibility: async () =>
          liveEligibility({
            matched: 12,
            currentlyEligible: 9,
            excluded: 3,
            emailEligible: 8,
            smsEligible: 6,
            excludedReasons: [
              { reason: "opt-out", count: 2 },
              { reason: "invalid-contact", count: 1 },
            ],
          }),
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
      matched: 12,
      currentlyEligible: 9,
      excluded: 3,
      emailEligible: 8,
      smsEligible: 6,
      excludedReasons: [
        { reason: "opt-out", count: 2 },
        { reason: "invalid-contact", count: 1 },
      ],
      source: "live",
    })
    expect(wizard.getSnapshot().canContinue).toBe(true)
  })

  it("blocks Continue when Currently eligible is 0 or eligibility calc fails", async () => {
    const wizardZero = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      ...defaultAudienceAdapters({
        loadAudienceEligibility: async () =>
          liveEligibility({ currentlyEligible: 0, excluded: 10 }),
      }),
    })
    wizardZero.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizardZero.setGoalId("thank-recent-guests")
    await wizardZero.continue()
    expect(wizardZero.getSnapshot().canContinue).toBe(false)

    const wizardFail = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      ...defaultAudienceAdapters({
        loadAudienceEligibility: async () => {
          throw new Error("eligibility down")
        },
      }),
    })
    wizardFail.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizardFail.setGoalId("thank-recent-guests")
    await wizardFail.continue()
    expect(wizardFail.getSnapshot().canContinue).toBe(false)
    expect(wizardFail.getSnapshot().audience?.eligibilityBreakdown.source).toBe(
      "error"
    )
    expect(wizardFail.getSnapshot().audience?.selectedAudienceId).toBe(
      "all-eligible-guests"
    )
  })

  it("documents select/popover z-index above OperatorWizardShell", () => {
    expect(CAMPAIGN_WIZARD_SELECT_MENU_CLASS).toContain("z-[140]")
  })

  it("wires Campaign create dialog to OperatorWizardShell with confirm and success slots", () => {
    expect(campaignWizardDialogSource).toContain(
      'from "@/components/dashboard/operator/OperatorWizardShell"'
    )
    expect(campaignWizardDialogSource).toContain("<OperatorWizardShell")
    expect(campaignWizardDialogSource).not.toContain("RecoveryWizardShell")
    expect(campaignWizardDialogSource).toContain("confirmDialog")
    expect(campaignWizardDialogSource).toContain("RecoverySuccessStatusList")
    expect(campaignWizardDialogSource).toContain(
      "snapshot.showNumberedStepper ? snapshot.numberedSteps : null"
    )

    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
    })
    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    expect(wizard.getSnapshot().showNumberedStepper).toBe(false)
  })

  it("continues from Audience into Channel with Email selected by default", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
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

  it("builds Channel estimated usage from live eligibility and shared messaging fixtures", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters({
        loadAudienceEligibility: async () =>
          liveEligibility({
            emailEligible: 7,
            smsEligible: 5,
            currentlyEligible: 8,
          }),
      }),
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
      "Camden · Email · 7 estimated recipients"
    )
    expect(channel!.usageSummary.rows).toEqual([
      { label: "Eligible recipients", value: "7" },
      { label: "Estimated email messages", value: "7" },
      { label: "Allowance remaining", value: "6,760" },
      { label: "Estimated remaining after send", value: "6,753" },
    ])
    expect(channel!.smsShortfall).toBeNull()

    // Ticket 24/25: Channel meters must equal overview Messaging usage source.
    expect(channel!.messagingFixture).toEqual(MESSAGING_USAGE_FIXTURE)
    expect(channel!.messagingFixture!.email.remaining).toBe(
      MESSAGING_USAGE_FIXTURE.email.remaining
    )
    expect(channel!.messagingFixture!.sms.available).toBe(
      MESSAGING_USAGE_FIXTURE.sms.available
    )
    expect(channel!.messagingFixture!.sms.reserved).toBe(
      MESSAGING_USAGE_FIXTURE.sms.reserved
    )
  })

  it("switches Channel estimate to SMS rows from live eligibility and shared fixtures", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters({
        loadAudienceEligibility: async () =>
          liveEligibility({
            emailEligible: 7,
            smsEligible: 5,
            currentlyEligible: 8,
          }),
      }),
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
    expect(channel!.usageSummary.audienceLine).toBe(
      "Camden · SMS · 5 estimated recipients"
    )
    expect(channel!.usageSummary.rows).toEqual([
      { label: "Eligible recipients", value: "5" },
      { label: "Estimated SMS parts", value: "At least 1 per recipient" },
      { label: "Estimated credits", value: "At least 5" },
      { label: "Available credits", value: "300" },
      { label: "Reserved credits", value: "120" },
      { label: "Estimated balance after send", value: "295" },
    ])
    expect(channel!.messagingFixture).toEqual(MESSAGING_USAGE_FIXTURE)
    // Shared fixtures have enough SMS credits — no shortfall banner.
    expect(channel!.smsShortfall).toBeNull()
  })

  it("uses live Billing balances for Channel allowance rows after cutover", async () => {
    const loadMessagingBalances = vi.fn(async () => ({
      email: {
        used: 100,
        allowance: 500,
        remaining: 400,
        refreshLabel: "1 September",
      },
      sms: { total: 50, reserved: 10, available: 40 },
      plan: {
        name: "Starter",
        locationCount: 1,
        billingLine: "Billed monthly · Next refresh 1 September",
      },
      ai: { available: 5 },
      softLocked: false,
    }))

    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters({
        loadAudienceEligibility: async () =>
          liveEligibility({ emailEligible: 12, smsEligible: 9 }),
      }),
      getNow: () => new Date("2026-08-14T14:18:00"),
      loadMessagingBalances,
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    await vi.waitFor(() => {
      expect(loadMessagingBalances).toHaveBeenCalled()
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()

    const channel = wizard.getSnapshot().channel
    expect(channel).not.toBeNull()
    expect(channel!.messagingBalancesStatus).toBe("ready")
    expect(channel!.usageSummary.audienceLine).toBe(
      "Camden · Email · 12 estimated recipients"
    )
    expect(channel!.usageSummary.rows).toEqual([
      { label: "Eligible recipients", value: "12" },
      { label: "Estimated email messages", value: "12" },
      { label: "Allowance remaining", value: "400" },
      { label: "Estimated remaining after send", value: "388" },
    ])
  })

  it("shows load-failed Channel usage with retry and no silent fixture fallback", async () => {
    const loadMessagingBalances = vi
      .fn()
      .mockRejectedValueOnce(new Error("billing down"))
      .mockResolvedValueOnce({
        email: {
          used: 100,
          allowance: 500,
          remaining: 400,
          refreshLabel: "1 September",
        },
        sms: { total: 50, reserved: 10, available: 40 },
        plan: {
          name: "Starter",
          locationCount: 1,
          billingLine: "Billed monthly · Next refresh 1 September",
        },
        ai: { available: 5 },
        softLocked: false,
      })

    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters({
        loadAudienceEligibility: async () =>
          liveEligibility({ emailEligible: 12, smsEligible: 9 }),
      }),
      getNow: () => new Date("2026-08-14T14:18:00"),
      loadMessagingBalances,
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    await vi.waitFor(() => {
      expect(loadMessagingBalances).toHaveBeenCalledTimes(1)
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()

    let channel = wizard.getSnapshot().channel
    expect(channel).not.toBeNull()
    expect(channel!.messagingBalancesStatus).toBe("load-failed")
    expect(channel!.messagingFixture).toBeNull()
    expect(channel!.usageSummary.rows).toEqual([])
    expect(channel!.usageSummary.audienceLine.length).toBeGreaterThan(0)

    await wizard.retryMessagingBalances()
    await vi.waitFor(() => {
      expect(loadMessagingBalances).toHaveBeenCalledTimes(2)
    })

    channel = wizard.getSnapshot().channel
    expect(channel!.messagingBalancesStatus).toBe("ready")
    expect(channel!.usageSummary.rows).toEqual([
      { label: "Eligible recipients", value: "12" },
      { label: "Estimated email messages", value: "12" },
      { label: "Allowance remaining", value: "400" },
      { label: "Estimated remaining after send", value: "388" },
    ])
  })

  it("continues from Channel into Offer with No offer selected by default", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
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

  it("keeps Existing offer visible but disabled and ignores selection", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
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

    const existing = wizard
      .getSnapshot()
      .offer!.options.find((o) => o.id === "existing-offer")
    expect(existing).toMatchObject({
      disabled: true,
      description: "Browse existing offers coming later.",
    })

    wizard.setOfferStanceId("existing-offer")
    expect(wizard.getSnapshot().offer!.selectedStanceId).toBe("no-offer")
    expect(wizard.getSnapshot().offer!.attachedOfferId).toBeNull()
    expect(wizard.getSnapshot().offer!.createPanelOpen).toBe(false)
  })

  it("create-and-select attaches Active catalog OfferId and No offer clears it", async () => {
    const createOffer = vi.fn(async () => ({
      id: 501,
      locationId: 42,
      status: "active" as const,
      offerType: "percentage_discount",
      title: "10% off next visit",
      description: "Enjoy 10% off your next meal.",
      validity: "30_days_after_issue",
      expiryDate: null,
      discountPercentage: 10,
      discountAmount: null,
      freeItemText: null,
      purchaseRequirement: null,
      minimumSpend: null,
      additionalExclusions: null,
      replacementItemText: null,
      staffInstructions: "Ask for the code.",
      createdAt: "2026-08-09T00:00:00Z",
      updatedAt: "2026-08-09T00:00:00Z",
    }))

    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      createOffer,
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()

    wizard.setOfferStanceId("create-new-offer")
    expect(wizard.getSnapshot().offer!.selectedStanceId).toBe("create-new-offer")
    expect(wizard.getSnapshot().offer!.createPanelOpen).toBe(true)
    expect(wizard.getSnapshot().offer!.attachedOfferId).toBeNull()

    wizard.patchCreateOfferDraft({
      offerType: "percentage_discount",
      discountPercentage: "10",
      title: "10% off next visit",
      description: "Enjoy 10% off your next meal.",
      validity: "30_days_after_issue",
    })
    expect(wizard.getSnapshot().offer!.canConfirmCreateOffer).toBe(true)

    await wizard.confirmCreateOffer()

    expect(createOffer).toHaveBeenCalledTimes(1)
    expect(createOffer).toHaveBeenCalledWith(
      expect.objectContaining({
        locationId: 42,
        offerType: "percentage_discount",
        title: "10% off next visit",
        discountPercentage: 10,
      })
    )
    expect(wizard.getSnapshot().offer!.createPanelOpen).toBe(false)
    expect(wizard.getSnapshot().offer!.attachedOfferId).toBe(501)
    expect(wizard.getSnapshot().offer!.attachedOfferTitle).toBe(
      "10% off next visit"
    )
    expect(wizard.getSnapshot().offer!.selectedStanceId).toBe("create-new-offer")

    await wizard.editAttachedOffer()
    expect(wizard.getSnapshot().offer!.createPanelOpen).toBe(true)

    wizard.setOfferStanceId("no-offer")
    expect(wizard.getSnapshot().offer!.selectedStanceId).toBe("no-offer")
    expect(wizard.getSnapshot().offer!.attachedOfferId).toBeNull()
    expect(wizard.getSnapshot().offer!.attachedOfferTitle).toBeNull()
    expect(wizard.getSnapshot().offer!.createPanelOpen).toBe(false)
  })

  it("Continue editing loads attached offer title for OfferId", async () => {
    const getOffer = vi.fn(async () => ({
      id: 501,
      locationId: 42,
      status: "active" as const,
      offerType: "percentage_discount",
      title: "10% off next visit",
      description: "Enjoy 10% off your next meal.",
      validity: "30_days_after_issue",
      expiryDate: null,
      discountPercentage: 10,
      discountAmount: null,
      freeItemText: null,
      purchaseRequirement: null,
      minimumSpend: null,
      additionalExclusions: null,
      replacementItemText: null,
      staffInstructions: "Ask for the code.",
      createdAt: "2026-08-09T00:00:00Z",
      updatedAt: "2026-08-09T00:00:00Z",
    }))

    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      getOffer,
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
        offerStance: "create-new-offer",
        offerId: 501,
        messageSubject: null,
        messageBody: null,
        rowVersion: "AAAAAAAAB9E=",
        createdAt: "2026-08-07T10:00:00Z",
        updatedAt: "2026-08-08T10:00:00Z",
      },
    })

    expect(getOffer).toHaveBeenCalledWith(501)

    await wizard.continue()
    await wizard.continue()

    expect(wizard.getSnapshot().stepId).toBe("offer")
    expect(wizard.getSnapshot().offer!.attachedOfferId).toBe(501)
    expect(wizard.getSnapshot().offer!.attachedOfferTitle).toBe(
      "10% off next visit"
    )
  })

  it("persists attached OfferId on Draft save and clears it for No offer", async () => {
    const createOffer = vi.fn(async () => ({
      id: 77,
      locationId: 7,
      status: "active" as const,
      offerType: "fixed_discount",
      title: "£5 off",
      description: "Five pounds off your next visit.",
      validity: "14_days_after_issue",
      expiryDate: null,
      discountPercentage: null,
      discountAmount: 5,
      freeItemText: null,
      purchaseRequirement: null,
      minimumSpend: null,
      additionalExclusions: null,
      replacementItemText: null,
      staffInstructions: null,
      createdAt: "2026-08-09T00:00:00Z",
      updatedAt: "2026-08-09T00:00:00Z",
    }))
    const createDraft = vi.fn(async (body) => ({
      id: 91,
      locationId: body.locationId,
      status: "draft" as const,
      name: "Thank recent guests",
      goalId: body.goalId ?? null,
      templateId: body.templateId ?? null,
      templateVersion: body.templateVersion ?? null,
      audienceKey: body.audienceKey ?? null,
      channel: body.channel ?? null,
      offerStance: body.offerStance ?? null,
      offerId: body.offerId ?? null,
      messageSubject: body.messageSubject ?? null,
      messageBody: body.messageBody ?? null,
      rowVersion: "AAAAAAAAB9E=",
      createdAt: "2026-08-08T00:00:00Z",
      updatedAt: "2026-08-08T00:00:00Z",
    }))
    const updateDraft = vi.fn(async (_id, body) => ({
      id: 91,
      locationId: 7,
      status: "draft" as const,
      name: "Thank recent guests",
      goalId: body.goalId ?? "thank-recent-guests",
      templateId: body.templateId ?? null,
      templateVersion: body.templateVersion ?? null,
      audienceKey: body.audienceKey ?? "all-eligible-guests",
      channel: body.channel ?? "email",
      offerStance: body.offerStance ?? null,
      offerId: body.offerId ?? null,
      messageSubject: body.messageSubject ?? null,
      messageBody: body.messageBody ?? null,
      rowVersion: "AAAAAAAAB9F=",
      createdAt: "2026-08-08T00:00:00Z",
      updatedAt: "2026-08-08T00:01:00Z",
    }))

    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      createDraft,
      updateDraft,
      createOffer,
    })
    wizard.openBlankCreate({
      locationId: 7,
      locationName: "Soho",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()

    wizard.setOfferStanceId("create-new-offer")
    wizard.patchCreateOfferDraft({
      offerType: "fixed_discount",
      discountAmount: "5",
      title: "£5 off",
      description: "Five pounds off your next visit.",
      validity: "14_days_after_issue",
    })
    await wizard.confirmCreateOffer()
    await wizard.save()

    expect(createDraft).toHaveBeenCalledWith(
      expect.objectContaining({
        offerStance: "create-new-offer",
        offerId: 77,
      })
    )

    wizard.setOfferStanceId("no-offer")
    await wizard.save()
    expect(updateDraft).toHaveBeenCalledWith(
      91,
      expect.objectContaining({
        offerStance: "no-offer",
        offerId: null,
      })
    )
  })

  it("builds Offer estimated usage from live eligibility and Channel messaging fixtures", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters({
        loadAudienceEligibility: async () =>
          liveEligibility({
            emailEligible: 7,
            smsEligible: 5,
            currentlyEligible: 8,
          }),
      }),
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
      "Camden · Email · 7 estimated recipients"
    )
    expect(offer!.usageSummary.rows).toEqual([
      { label: "Eligible recipients", value: "7" },
      { label: "Estimated email messages", value: "7" },
      { label: "Allowance remaining", value: "6,760" },
      { label: "Estimated remaining after send", value: "6,753" },
    ])
    expect(offer!.messagingFixture).toEqual(MESSAGING_USAGE_FIXTURE)
  })

  it("Message starts on chooser; Write manually yields operator-owned subject and body", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
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

    // Without prepareMessageDraft adapter, prepare is a no-op.
    await wizard.prepareDraft()
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

  it("Prepare with AI fills editable subject/body from adapter; text stays client-only", async () => {
    const prepareMessageDraft = vi.fn(
      async (): Promise<PrepareCampaignMessageDraftResult> => ({
        status: "succeeded",
        body: "Thank you for joining us recently.",
        subject: "Thanks for visiting",
        channel: "email",
      })
    )
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      prepareMessageDraft,
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

    expect(wizard.getSnapshot().message!.prepareAiLive).toBe(true)
    expect(wizard.getSnapshot().message!.writeEntry).toBe("chooser")
    expect(wizard.getSnapshot().draftId).toBeNull()

    const preparePromise = wizard.prepareDraft()
    expect(wizard.getSnapshot().message).toMatchObject({
      aiDraftStatus: "running",
      preparingOverlayOpen: true,
      aiDraftMode: "prepare",
    })

    await preparePromise

    expect(prepareMessageDraft).toHaveBeenCalledWith(
      expect.objectContaining({
        locationId: 42,
        channel: "email",
        goalId: "thank-recent-guests",
        audienceKey: "all-eligible-guests",
        offerStance: "no-offer",
        mode: "prepare",
        currentBody: null,
        currentSubject: null,
      }),
      expect.any(AbortSignal)
    )
    expect(prepareMessageDraft).toHaveBeenCalled()
    expect(JSON.stringify(prepareMessageDraft.mock.calls[0])).not.toMatch(
      /@|phone|\+\d/
    )

    expect(wizard.getSnapshot().message).toMatchObject({
      writeEntry: "editor",
      subject: "Thanks for visiting",
      body: "Thank you for joining us recently.",
      aiDraftStatus: "idle",
      preparingOverlayOpen: false,
      sendTestAvailable: false,
    })
    expect(wizard.getSnapshot().draftId).toBeNull()
    expect(wizard.getSnapshot().canContinue).toBe(true)
  })

  it("Prepare failure unlocks retry; rewrite keeps prior text on fail", async () => {
    const prepareMessageDraft = vi.fn(
      async (): Promise<PrepareCampaignMessageDraftResult> => ({
        status: "failed",
        retryable: true,
      })
    )
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      prepareMessageDraft,
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

    await wizard.prepareDraft()
    expect(wizard.getSnapshot().message).toMatchObject({
      aiDraftStatus: "failed",
      aiDraftError: "We could not prepare a draft.",
      aiDraftRetryable: true,
      preparingOverlayOpen: false,
      body: "",
      subject: "",
    })

    prepareMessageDraft.mockResolvedValueOnce({
      status: "succeeded",
      body: "Prior body",
      subject: "Prior subject",
      channel: "email",
    })
    await wizard.retryAiDraft()
    expect(wizard.getSnapshot().message).toMatchObject({
      body: "Prior body",
      subject: "Prior subject",
      writeEntry: "editor",
      aiDraftStatus: "idle",
    })
  })

  it("Prepare failure on chooser keeps writeEntry chooser so Try again is available", async () => {
    const prepareMessageDraft = vi.fn(
      async (): Promise<PrepareCampaignMessageDraftResult> => ({
        status: "failed",
        retryable: true,
      })
    )
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      prepareMessageDraft,
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

    await wizard.prepareDraft()
    expect(wizard.getSnapshot().message).toMatchObject({
      writeEntry: "chooser",
      aiDraftStatus: "failed",
      aiDraftMode: "prepare",
      aiDraftRetryable: true,
      preparingOverlayOpen: false,
    })

    prepareMessageDraft.mockResolvedValueOnce({
      status: "succeeded",
      body: "Recovered body",
      subject: "Recovered subject",
      channel: "email",
    })
    await wizard.retryAiDraft()
    expect(wizard.getSnapshot().message).toMatchObject({
      writeEntry: "editor",
      body: "Recovered body",
      subject: "Recovered subject",
      aiDraftStatus: "idle",
    })
  })

  it("Rewrite keeps prior text on fail", async () => {
    const prepareMessageDraft = vi.fn(
      async (): Promise<PrepareCampaignMessageDraftResult> => ({
        status: "succeeded",
        body: "Prior body",
        subject: "Prior subject",
        channel: "email",
      })
    )
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      prepareMessageDraft,
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

    await wizard.prepareDraft()
    expect(wizard.getSnapshot().message!.writeEntry).toBe("editor")

    prepareMessageDraft.mockResolvedValueOnce({
      status: "failed",
      retryable: true,
    })
    await wizard.rewriteDraft("message")
    expect(prepareMessageDraft).toHaveBeenLastCalledWith(
      expect.objectContaining({
        mode: "rewrite_message",
        currentBody: "Prior body",
        currentSubject: "Prior subject",
      }),
      expect.any(AbortSignal)
    )
    expect(wizard.getSnapshot().message).toMatchObject({
      body: "Prior body",
      subject: "Prior subject",
      aiDraftStatus: "failed",
      preparingOverlayOpen: false,
    })

    prepareMessageDraft.mockResolvedValueOnce({
      status: "succeeded",
      body: "Should ignore body",
      subject: "Only subject",
      channel: "email",
    })
    await wizard.rewriteDraft("subject")
    expect(prepareMessageDraft).toHaveBeenLastCalledWith(
      expect.objectContaining({
        mode: "rewrite_subject",
      }),
      expect.any(AbortSignal)
    )
    expect(wizard.getSnapshot().message).toMatchObject({
      body: "Prior body",
      subject: "Only subject",
      aiDraftStatus: "idle",
      preparingOverlayOpen: false,
      sendTestAvailable: false,
    })
  })

  it("Preparing overlay: Write manually cancels AI; dismiss keeps AI running", async () => {
    let resolveDraft!: (value: PrepareCampaignMessageDraftResult) => void
    const prepareMessageDraft = vi.fn(
      (_request: unknown, signal?: AbortSignal) =>
        new Promise<PrepareCampaignMessageDraftResult>((resolve, reject) => {
          resolveDraft = resolve
          signal?.addEventListener("abort", () => {
            reject(new DOMException("Aborted", "AbortError"))
          })
        })
    )
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      prepareMessageDraft,
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

    const preparePromise = wizard.prepareDraft()
    expect(wizard.getSnapshot().message).toMatchObject({
      aiDraftStatus: "running",
      preparingOverlayOpen: true,
    })

    wizard.dismissPreparingOverlay()
    expect(wizard.getSnapshot().message).toMatchObject({
      preparingOverlayOpen: false,
      aiDraftStatus: "running",
    })

    resolveDraft({
      status: "succeeded",
      body: "Filled after dismiss",
      subject: "Subject after dismiss",
      channel: "email",
    })
    await preparePromise
    expect(wizard.getSnapshot().message).toMatchObject({
      body: "Filled after dismiss",
      subject: "Subject after dismiss",
      writeEntry: "editor",
      preparingOverlayOpen: false,
      aiDraftStatus: "idle",
    })

    const prepareMessageDraft2 = vi.fn(
      (_request: unknown, signal?: AbortSignal) =>
        new Promise<PrepareCampaignMessageDraftResult>((_resolve, reject) => {
          signal?.addEventListener("abort", () => {
            reject(new DOMException("Aborted", "AbortError"))
          })
        })
    )
    const wizard2 = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      prepareMessageDraft: prepareMessageDraft2,
    })
    wizard2.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizard2.setGoalId("thank-recent-guests")
    await wizard2.continue()
    await wizard2.continue()
    await wizard2.continue()
    await wizard2.continue()

    const cancelled = wizard2.prepareDraft()
    wizard2.writeManually()
    await cancelled

    expect(wizard2.getSnapshot().message).toMatchObject({
      writeEntry: "editor",
      aiDraftStatus: "idle",
      preparingOverlayOpen: false,
      body: "",
      subject: "",
    })
  })

  it("Guest preview opens from Message with Send test unavailable without adapter", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
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
    expect(wizard.getSnapshot().sendTest).toBeNull()

    wizard.openGuestPreview()
    expect(wizard.getSnapshot().message!.guestPreviewOpen).toBe(true)
    expect(wizard.getSnapshot().message!.sendTestAvailable).toBe(false)
    expect(wizard.getSnapshot().message!.channelId).toBe("email")
    expect(wizard.getSnapshot().message!.body).toBe("Preview body")

    wizard.closeGuestPreview()
    expect(wizard.getSnapshot().message!.guestPreviewOpen).toBe(false)
    expect(wizard.getSnapshot().message!.sendTestAvailable).toBe(false)
  })

  it("Send test opens from Message and Review; success closes dialog and keeps preview", async () => {
    const sendCampaignTest = vi.fn(async () => {})
    const getOperatorAccountEmail = vi.fn(async () => "ops@example.com")
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      ...defaultAudienceAdapters(),
      sendCampaignTest,
      getOperatorAccountEmail,
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
      locationAddress: "12 High Street",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()

    wizard.writeManually()
    wizard.setSubject("Thanks for visiting")
    wizard.setMessage("Preview body")
    expect(wizard.getSnapshot().message!.locationAddress).toBe("12 High Street")
    expect(wizard.getSnapshot().message!.sendTestAvailable).toBe(true)
    expect(wizard.getSnapshot().draftId).toBeNull()

    wizard.openGuestPreview()
    await wizard.openSendTestDialog()
    expect(wizard.getSnapshot().sendTest).toMatchObject({
      isOpen: true,
      email: "ops@example.com",
      status: "idle",
      canSubmit: true,
    })
    expect(getOperatorAccountEmail).toHaveBeenCalled()

    await wizard.confirmSendTest()
    expect(sendCampaignTest).toHaveBeenCalledWith({
      locationId: 42,
      toEmail: "ops@example.com",
      subject: "Thanks for visiting",
      body: "Preview body",
    })
    expect(wizard.getSnapshot().sendTest).toMatchObject({
      isOpen: false,
      status: "success",
    })
    expect(wizard.getSnapshot().message!.guestPreviewOpen).toBe(true)

    await wizard.continue()
    wizard.setScheduleModeId("send-now")
    await wizard.continue()

    expect(wizard.getSnapshot().review!.guestPreview.sendTestAvailable).toBe(
      true
    )
    expect(wizard.getSnapshot().review!.guestPreview.locationAddress).toBe(
      "12 High Street"
    )
    wizard.openGuestPreview()
    await wizard.openSendTestDialog()
    expect(wizard.getSnapshot().sendTest!.isOpen).toBe(true)
    wizard.setSendTestEmail("team@example.com")
    await wizard.confirmSendTest()
    expect(sendCampaignTest).toHaveBeenLastCalledWith({
      locationId: 42,
      toEmail: "team@example.com",
      subject: "Thanks for visiting",
      body: "Preview body",
    })
    expect(wizard.getSnapshot().review!.guestPreview.guestPreviewOpen).toBe(
      true
    )
  })

  it("Send test error stays open and allows retry", async () => {
    const sendCampaignTest = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(undefined)
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      ...defaultAudienceAdapters(),
      sendCampaignTest,
      getOperatorAccountEmail: async () => "ops@example.com",
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
    wizard.setSubject("Subject")
    wizard.setMessage("Body")
    wizard.openGuestPreview()
    await wizard.openSendTestDialog()

    await wizard.confirmSendTest()
    expect(wizard.getSnapshot().sendTest).toMatchObject({
      isOpen: true,
      status: "error",
      error: CAMPAIGN_SEND_TEST_COPY.errorMessage,
      canSubmit: true,
    })

    await wizard.confirmSendTest()
    expect(sendCampaignTest).toHaveBeenCalledTimes(2)
    expect(wizard.getSnapshot().sendTest).toMatchObject({
      isOpen: false,
      status: "success",
    })
  })

  it("Send test is unavailable for SMS channel", async () => {
    const sendCampaignTest = vi.fn(async () => {})
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      ...defaultAudienceAdapters(),
      sendCampaignTest,
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()
    wizard.setChannelId("sms")
    await wizard.continue()
    await wizard.continue()
    wizard.writeManually()
    wizard.setMessage("SMS body")
    wizard.openGuestPreview()

    expect(wizard.getSnapshot().message!.sendTestAvailable).toBe(false)
    expect(wizard.getSnapshot().sendTest).toBeNull()
    await wizard.openSendTestDialog()
    expect(wizard.getSnapshot().sendTest).toBeNull()
    expect(sendCampaignTest).not.toHaveBeenCalled()
  })

  it("Send test includes sample offer when stance is not no-offer", async () => {
    const sendCampaignTest = vi.fn(async () => {})
    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      ...defaultAudienceAdapters(),
      sendCampaignTest,
      getOperatorAccountEmail: async () => "ops@example.com",
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()
    wizard.setOfferStanceId("optional")
    await wizard.continue()
    wizard.writeManually()
    wizard.setSubject("Subject")
    wizard.setMessage("Body")
    wizard.openGuestPreview()
    await wizard.openSendTestDialog()
    await wizard.confirmSendTest()

    expect(sendCampaignTest).toHaveBeenCalledWith({
      locationId: 42,
      toEmail: "ops@example.com",
      subject: "Subject",
      body: "Body",
      offer: { ...CAMPAIGN_SEND_TEST_SAMPLE_OFFER },
    })
    expect(wizard.getSnapshot().draftId).toBeNull()
  })

  it("Schedule step offers Send now / Schedule for later chrome without reservation APIs", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
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
    expect(snapshot.schedule!.showDatetimeFields).toBe(false)
    expect(snapshot.canContinue).toBe(true)
    expect(snapshot.primaryActionLabel).toBe(CAMPAIGN_WIZARD_COPY.continue)
    expect(snapshot.schedule!.usageSummary.audienceLine).toBe(
      "Camden · Email · 7 estimated recipients"
    )
    expect(snapshot.schedule!.usageSummary.rows).toEqual([
      { label: "Eligible recipients", value: "7" },
      { label: "Estimated email messages", value: "7" },
      { label: "Allowance remaining", value: "6,760" },
      { label: "Estimated remaining after send", value: "6,753" },
    ])

    wizard.setScheduleModeId("schedule-later")
    snapshot = wizard.getSnapshot()
    expect(snapshot.schedule!.selectedModeId).toBe("schedule-later")
    expect(snapshot.schedule!.showDatetimeFields).toBe(true)
    expect(snapshot.schedule!.options.find((o) => o.id === "schedule-later")?.selected).toBe(
      true
    )
    // Schedule-later needs a future local datetime before Continue.
    expect(snapshot.canContinue).toBe(false)

    wizard.setScheduleDateLocal("2026-08-20")
    wizard.setScheduleTimeLocal("10:00")
    snapshot = wizard.getSnapshot()
    expect(snapshot.canContinue).toBe(true)
  })

  it("Review Guest preview shows sample-code offer block when wizard has an offer", async () => {
    const createOffer = vi.fn(async () => ({
      id: 501,
      locationId: 42,
      status: "active" as const,
      offerType: "percentage_discount",
      title: "10% off next visit",
      description: "Enjoy 10% off your next meal.",
      validity: "30_days_after_issue",
      expiryDate: null,
      discountPercentage: 10,
      discountAmount: null,
      freeItemText: null,
      purchaseRequirement: null,
      minimumSpend: null,
      additionalExclusions: null,
      replacementItemText: null,
      staffInstructions: "Ask for the code.",
      createdAt: "2026-08-09T00:00:00Z",
      updatedAt: "2026-08-09T00:00:00Z",
    }))

    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      createOffer,
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()
    wizard.setOfferStanceId("create-new-offer")
    wizard.patchCreateOfferDraft({
      offerType: "percentage_discount",
      discountPercentage: "10",
      title: "10% off next visit",
      description: "Enjoy 10% off your next meal.",
      validity: "30_days_after_issue",
    })
    await wizard.confirmCreateOffer()
    await wizard.continue()
    wizard.writeManually()
    wizard.setSubject("Thanks for visiting")
    wizard.setMessage("Hi guest,\n\nThank you for joining us.")
    await wizard.continue()
    wizard.setScheduleModeId("send-now")
    await wizard.continue()

    const coupon = wizard.getSnapshot().review!.guestPreview.offerCoupon
    expect(coupon).not.toBeNull()
    expect(coupon!.title).toBe("10% off next visit")
    expect(coupon!.description).toBe("Enjoy 10% off your next meal.")
    expect(coupon!.redemptionCode).toBe("PREVIEW-CODE")
    expect(coupon!.expiryLabel).toBe("Expires: 30 days after issue")
  })

  it("Review step summarises wizard state and hard-blocks send without commitCampaign", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
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
    expect(snapshot.review!.sendBlockedReason).toBe(
      CAMPAIGN_COMMIT_COPY.billingReserveUnavailable
    )
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
    expect(snapshot.review!.guestPreview.offerCoupon).toBeNull()

    // Continue on Review must not open confirm without commit adapter.
    await wizard.continue()
    expect(wizard.getSnapshot().stepId).toBe("review")
    expect(wizard.getSnapshot().canContinue).toBe(false)
    expect(wizard.getSnapshot().commitConfirm?.open).toBe(false)

    wizard.openCommitConfirm()
    expect(wizard.getSnapshot().commitConfirm?.open).toBe(false)

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

  it("hard-blocks Review commit when commitCampaign adapter is omitted", async () => {
    const createDraft = vi.fn(async () => sampleDraftDetail())
    const sendCampaignTest = vi.fn(async () => {})
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      createDraft,
      sendCampaignTest,
    })

    await walkToReview(wizard)

    const snapshot = wizard.getSnapshot()
    expect(snapshot.stepId).toBe("review")
    expect(snapshot.review!.sendAvailable).toBe(false)
    expect(snapshot.review!.sendBlockedReason).toBe(
      CAMPAIGN_COMMIT_COPY.billingReserveUnavailable
    )
    expect(snapshot.canContinue).toBe(false)
    expect(snapshot.review!.guestPreview.sendTestAvailable).toBe(true)
    wizard.openCommitConfirm()
    expect(wizard.getSnapshot().commitConfirm?.open).toBe(false)

    await wizard.save()
    expect(createDraft).toHaveBeenCalled()
  })

  it("CampaignsPage wires commitCampaign to commitCampaignSchedule", () => {
    expect(campaignsPageSource).toContain("commitCampaignSchedule")
    expect(campaignsPageSource).toContain("commitCampaign:")
    expect(campaignsPageSource).not.toContain(
      "do not wire commitCampaign until Billing Reserve is live"
    )
  })

  it("Review primary stays disabled with Reserve unavailable reason when commit adapter is omitted", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      createDraft: vi.fn(async () => sampleDraftDetail()),
    })

    await walkToReview(wizard)

    const snapshot = wizard.getSnapshot()
    expect(snapshot.review!.sendAvailable).toBe(false)
    expect(snapshot.review!.sendBlockedReason).toBe(
      CAMPAIGN_COMMIT_COPY.billingReserveUnavailable
    )
    expect(campaignWizardDialogSource).toContain("sendBlockedReason")
  })

  it("surfaces Soft-lock blocked reason on Review and keeps Save / Send test", async () => {
    const createDraft = vi.fn(async () => sampleDraftDetail())
    const sendCampaignTest = vi.fn(async () => {})
    const loadMessagingBalances = vi.fn(async () => ({
      email: {
        used: 100,
        allowance: 500,
        remaining: 400,
        refreshLabel: "1 September",
      },
      sms: { total: 50, reserved: 10, available: 40 },
      plan: {
        name: "Starter",
        locationCount: 1,
        billingLine: "Billed monthly · Next refresh 1 September",
      },
      ai: { available: 5 },
      softLocked: true,
    }))
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      createDraft,
      sendCampaignTest,
      commitCampaign: vi.fn(),
      loadMessagingBalances,
    })

    await walkToReview(wizard)
    await vi.waitFor(() => {
      expect(wizard.getSnapshot().review!.sendBlockedReason).toBe(
        CAMPAIGN_COMMIT_COPY.softLocked
      )
    })

    const snapshot = wizard.getSnapshot()
    expect(snapshot.review!.sendAvailable).toBe(false)
    expect(snapshot.review!.guestPreview.sendTestAvailable).toBe(true)

    await wizard.save()
    expect(createDraft).toHaveBeenCalled()
  })

  it("surfaces channel shortfall blocked reason on Review", async () => {
    const loadMessagingBalances = vi.fn(async () => ({
      email: {
        used: 497,
        allowance: 500,
        remaining: 3,
        refreshLabel: "1 September",
      },
      sms: { total: 50, reserved: 10, available: 40 },
      plan: {
        name: "Starter",
        locationCount: 1,
        billingLine: "Billed monthly · Next refresh 1 September",
      },
      ai: { available: 5 },
      softLocked: false,
    }))
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      createDraft: vi.fn(async () => sampleDraftDetail()),
      commitCampaign: vi.fn(),
      loadMessagingBalances,
    })

    await walkToReview(wizard)
    await vi.waitFor(() => {
      expect(wizard.getSnapshot().review!.sendBlockedReason).toBe(
        CAMPAIGN_COMMIT_COPY.channelHardStop
      )
    })

    expect(wizard.getSnapshot().review!.sendAvailable).toBe(false)
  })

  it("surfaces zero-eligible blocked reason on Review", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters({
        loadAudienceEligibility: async () =>
          liveEligibility({
            currentlyEligible: 8,
            emailEligible: 0,
            smsEligible: 5,
          }),
      }),
      getNow: () => new Date("2026-08-14T14:18:00"),
      createDraft: vi.fn(async () => sampleDraftDetail()),
      commitCampaign: vi.fn(),
    })

    await walkToReview(wizard)

    const snapshot = wizard.getSnapshot()
    expect(snapshot.review!.sendAvailable).toBe(false)
    expect(snapshot.review!.sendBlockedReason).toBe(
      CAMPAIGN_COMMIT_COPY.zeroEligible
    )
  })

  it("Review Channel Sender uses operator email when available", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      createDraft: vi.fn(async () => sampleDraftDetail()),
      commitCampaign: vi.fn(),
      getOperatorAccountEmail: async () => "ops@tummly.test",
    })

    await walkToReview(wizard)
    await vi.waitFor(() => {
      const channel = wizard
        .getSnapshot()
        .review!.sections.find((section) => section.id === "channel")
      expect(channel?.rows).toEqual([
        { label: "Channel", value: "Email" },
        { label: "Sender", value: "ops@tummly.test" },
      ])
    })
  })

  it("surfaces commit-not-ready reason when draft rowVersion is missing", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      commitCampaign: vi.fn(),
    })
    await wizard.openFromDraft({
      locationName: "Camden",
      draft: {
        ...sampleDraftDetail(),
        rowVersion: "",
        messageSubject: null,
        messageBody: null,
      },
    })
    // No message content → Audience, so eligibility loads before Review.
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()
    wizard.writeManually()
    wizard.setSubject("Thanks for visiting")
    wizard.setMessage("Hi guest")
    await wizard.continue()
    await wizard.continue()

    const snapshot = wizard.getSnapshot()
    expect(snapshot.stepId).toBe("review")
    expect(snapshot.review!.sendAvailable).toBe(false)
    expect(snapshot.review!.sendBlockedReason).toBe(
      CAMPAIGN_COMMIT_COPY.commitNotReady
    )
  })

  it("surfaces Billing Reserve unavailable on confirmCommit", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00.000Z"),
      createDraft: vi.fn(async () => sampleDraftDetail()),
      commitCampaign: vi.fn(async () => {
        throw new CampaignBillingReserveUnavailableError(
          "Billing Reserve is not available."
        )
      }),
    })

    await walkToReview(wizard)
    expect(wizard.getSnapshot().review!.sendAvailable).toBe(true)
    wizard.openCommitConfirm()
    await wizard.confirmCommit()

    const snapshot = wizard.getSnapshot()
    expect(snapshot.stepId).toBe("review")
    expect(snapshot.commitConfirm?.open).toBe(true)
    expect(snapshot.commitConfirm?.error).toBe(
      CAMPAIGN_COMMIT_COPY.billingReserveUnavailable
    )
  })

  it("commits send-now via confirmCommit and shows success chrome", async () => {
    const createDraft = vi.fn(async () => ({
      id: 91,
      locationId: 42,
      status: "draft",
      name: "Thanks campaign",
      goalId: "thank-recent-guests",
      templateId: null,
      templateVersion: null,
      audienceKey: "all-eligible-guests",
      channel: "email",
      offerStance: "no-offer",
      offerId: null,
      messageSubject: "Thanks for visiting",
      messageBody: "Hi guest",
      rowVersion: "r1",
      createdAt: "2026-08-14T14:00:00.000Z",
      updatedAt: "2026-08-14T14:00:00.000Z",
    }))
    const commitCampaign = vi.fn(async () => ({
      id: 91,
      locationId: 42,
      status: "sending",
      name: "Thanks campaign",
      scheduleMode: "send-now",
      scheduledAtUtc: null,
      scheduleTimeZone: "Europe/London",
      billingReservationRef: "res-1",
      reservedEstimate: 7,
      frozenRecipientCount: 7,
      rowVersion: "r2",
      updatedAt: "2026-08-14T14:20:00.000Z",
    }))
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00.000Z"),
      createDraft,
      commitCampaign,
    })

    wizard.openBlankCreate({ locationId: 42, locationName: "Camden" })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()
    wizard.writeManually()
    wizard.setSubject("Thanks for visiting")
    wizard.setMessage("Hi guest")
    await wizard.continue()
    await wizard.continue()

    let snapshot = wizard.getSnapshot()
    expect(snapshot.review!.sendAvailable).toBe(true)
    expect(snapshot.canContinue).toBe(true)
    expect(snapshot.primaryActionLabel).toBe("Send campaign now")

    await wizard.continue()
    snapshot = wizard.getSnapshot()
    expect(snapshot.commitConfirm?.open).toBe(true)

    await wizard.confirmCommit()
    snapshot = wizard.getSnapshot()
    expect(createDraft).toHaveBeenCalled()
    expect(commitCampaign).toHaveBeenCalledWith({
      campaignId: 91,
      body: expect.objectContaining({
        rowVersion: "r1",
        scheduleMode: "send-now",
      }),
    })
    expect(snapshot.stepId).toBe("success")
    expect(snapshot.success?.title).toBe("Campaign sending")
    expect(snapshot.footerLayout).toBe("end")
    expect(snapshot.commitConfirm).toBeNull()
  })

  it("surfaces reserve failure on confirmCommit", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00.000Z"),
      createDraft: vi.fn(async () => ({
        id: 91,
        locationId: 42,
        status: "draft",
        name: "Thanks campaign",
        goalId: "thank-recent-guests",
        templateId: null,
        templateVersion: null,
        audienceKey: "all-eligible-guests",
        channel: "email",
        offerStance: "no-offer",
        offerId: null,
        messageSubject: "Thanks for visiting",
        messageBody: "Hi guest",
        rowVersion: "r1",
        createdAt: "2026-08-14T14:00:00.000Z",
        updatedAt: "2026-08-14T14:00:00.000Z",
      })),
      commitCampaign: vi.fn(async () => {
        throw new Error("Insufficient credits to reserve this campaign.")
      }),
    })

    wizard.openBlankCreate({ locationId: 42, locationName: "Camden" })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()
    wizard.writeManually()
    wizard.setSubject("Thanks for visiting")
    wizard.setMessage("Hi guest")
    await wizard.continue()
    await wizard.continue()
    wizard.openCommitConfirm()
    await wizard.scheduleCommit()

    const snapshot = wizard.getSnapshot()
    expect(snapshot.stepId).toBe("review")
    expect(snapshot.commitConfirm?.open).toBe(true)
    expect(snapshot.commitConfirm?.error).toBe(
      "Insufficient credits to reserve this campaign."
    )
  })

  it("rejects schedule-later past datetime on Continue / canContinue", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
    })

    wizard.openBlankCreate({ locationId: 42, locationName: "Camden" })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()
    wizard.writeManually()
    wizard.setSubject("Thanks for visiting")
    wizard.setMessage("Hi guest")
    await wizard.continue()

    wizard.setScheduleModeId("schedule-later")
    wizard.setScheduleDateLocal("2026-08-10")
    wizard.setScheduleTimeLocal("09:00")
    expect(wizard.getSnapshot().canContinue).toBe(false)
    await wizard.continue()
    expect(wizard.getSnapshot().stepId).toBe("schedule")
  })

  it("switches Review primary label for schedule-later", async () => {
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
      getNow: () => new Date("2026-08-14T14:18:00"),
      commitCampaign: vi.fn(),
      createDraft: vi.fn(async () => ({
        id: 91,
        locationId: 42,
        status: "draft",
        name: "Thanks campaign",
        goalId: "thank-recent-guests",
        templateId: null,
        templateVersion: null,
        audienceKey: "all-eligible-guests",
        channel: "email",
        offerStance: "no-offer",
        offerId: null,
        messageSubject: "Thanks for visiting",
        messageBody: "Hi guest",
        rowVersion: "r1",
        createdAt: "2026-08-14T14:00:00.000Z",
        updatedAt: "2026-08-14T14:00:00.000Z",
      })),
    })

    wizard.openBlankCreate({ locationId: 42, locationName: "Camden" })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()
    wizard.writeManually()
    wizard.setSubject("Thanks for visiting")
    wizard.setMessage("Hi guest")
    await wizard.continue()
    wizard.setScheduleModeId("schedule-later")
    wizard.setScheduleDateLocal("2026-08-20")
    wizard.setScheduleTimeLocal("18:00")
    await wizard.continue()

    const snapshot = wizard.getSnapshot()
    expect(snapshot.stepId).toBe("review")
    expect(snapshot.primaryActionLabel).toBe("Schedule campaign")
    expect(snapshot.review!.sendAvailable).toBe(true)
  })

  it("opens from recommendation draftPrefill without creating a server Draft", async () => {
    const createDraft = vi.fn()
    const loadSmartGroupCounts = vi.fn(async () => ({
      smartGroupCounts: { "all-guests": 40, "new-guests": 5 },
    }))
    const wizard = createCampaignWizardModule({
      ...defaultAudienceAdapters(),
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
      offerId: body.offerId ?? null,
      messageSubject: body.messageSubject ?? null,
      messageBody: body.messageBody ?? null,
      rowVersion: "AAAAAAAAB9E=",
      createdAt: "2026-08-08T12:00:00.000Z",
      updatedAt: "2026-08-08T12:00:00.000Z",
    }))
    const wizard = createCampaignWizardModule({ ...defaultAudienceAdapters(), createDraft })

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
        offerId: null,
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
        smsEligible: 121,
        fixture: MESSAGING_USAGE_FIXTURE,
      })
    ).toBeNull()

    expect(
      resolveCampaignChannelSmsShortfall({
        channelId: "email",
        smsEligible: 121,
        fixture: {
          ...MESSAGING_USAGE_FIXTURE,
          sms: { total: 100, reserved: 20, available: 80 },
        },
      })
    ).toBeNull()

    expect(
      resolveCampaignChannelSmsShortfall({
        channelId: "sms",
        smsEligible: null,
        fixture: {
          ...MESSAGING_USAGE_FIXTURE,
          sms: { total: 100, reserved: 20, available: 80 },
        },
      })
    ).toBeNull()

    const shortfall = resolveCampaignChannelSmsShortfall({
      channelId: "sms",
      smsEligible: 121,
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

  it("debits 1 AI via ConsumeDirect on usable prepare success after live cutover", async () => {
    const prepareMessageDraft = vi.fn(
      async (): Promise<PrepareCampaignMessageDraftResult> => ({
        status: "succeeded",
        body: "Thank you for joining us recently.",
        subject: "Thanks for visiting",
        channel: "email",
      })
    )
    const consumeDirectAi = vi.fn(async () => {})
    const loadMessagingBalances = vi.fn(async () => ({
      email: {
        used: 100,
        allowance: 500,
        remaining: 400,
        refreshLabel: "1 September",
      },
      sms: { total: 50, reserved: 10, available: 40 },
      plan: {
        name: "Starter",
        locationCount: 1,
        billingLine: "Billed monthly · Next refresh 1 September",
      },
      ai: { available: 5 },
      softLocked: false,
    }))

    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      ...defaultAudienceAdapters(),
      prepareMessageDraft,
      consumeDirectAi,
      loadMessagingBalances,
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    await vi.waitFor(() => {
      expect(loadMessagingBalances).toHaveBeenCalled()
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()

    expect(wizard.getSnapshot().message!.aiPrepareAllowed).toBe(true)
    await wizard.prepareDraft()

    expect(consumeDirectAi).toHaveBeenCalledWith({
      locationId: 42,
      units: 1,
    })
    expect(wizard.getSnapshot().message!.aiActionCount).toBe(1)
  })

  it("does not call the model or ConsumeDirect when Soft-lock blocks Prepare", async () => {
    const prepareMessageDraft = vi.fn(
      async (): Promise<PrepareCampaignMessageDraftResult> => ({
        status: "succeeded",
        body: "Should not run",
        subject: "Blocked",
        channel: "email",
      })
    )
    const consumeDirectAi = vi.fn(async () => {})
    const loadMessagingBalances = vi.fn(async () => ({
      email: {
        used: 100,
        allowance: 500,
        remaining: 400,
        refreshLabel: "1 September",
      },
      sms: { total: 50, reserved: 10, available: 40 },
      plan: {
        name: "Starter",
        locationCount: 1,
        billingLine: "Billed monthly · Next refresh 1 September",
      },
      ai: { available: 5 },
      softLocked: true,
    }))

    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      ...defaultAudienceAdapters(),
      prepareMessageDraft,
      consumeDirectAi,
      loadMessagingBalances,
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    await vi.waitFor(() => {
      expect(loadMessagingBalances).toHaveBeenCalled()
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()
    await wizard.continue()

    expect(wizard.getSnapshot().message!.aiPrepareAllowed).toBe(false)
    await wizard.prepareDraft()

    expect(prepareMessageDraft).not.toHaveBeenCalled()
    expect(consumeDirectAi).not.toHaveBeenCalled()
    expect(wizard.getSnapshot().message!.aiDraftStatus).toBe("failed")
    expect(wizard.getSnapshot().message!.aiDraftError).toMatch(
      /limited access/i
    )
  })

  it("Channel SMS shortfall still allows Continue", async () => {
    const loadMessagingBalances = vi.fn(async () => ({
      email: {
        used: 100,
        allowance: 500,
        remaining: 400,
        refreshLabel: "1 September",
      },
      sms: { total: 50, reserved: 40, available: 10 },
      plan: {
        name: "Starter",
        locationCount: 1,
        billingLine: "Billed monthly · Next refresh 1 September",
      },
      ai: { available: 5 },
      softLocked: false,
    }))

    const wizard = createCampaignWizardModule({
      getNow: () => new Date("2026-08-14T14:18:00"),
      ...defaultAudienceAdapters({
        loadAudienceEligibility: async () =>
          liveEligibility({ emailEligible: 148, smsEligible: 121 }),
      }),
      loadMessagingBalances,
    })

    wizard.openBlankCreate({
      locationId: 42,
      locationName: "Camden",
    })
    await vi.waitFor(() => {
      expect(loadMessagingBalances).toHaveBeenCalled()
    })
    wizard.setGoalId("thank-recent-guests")
    await wizard.continue()
    await wizard.continue()
    wizard.setChannelId("sms")

    const channel = wizard.getSnapshot().channel
    expect(channel?.smsShortfall).not.toBeNull()
    expect(wizard.getSnapshot().canContinue).toBe(true)
  })
})

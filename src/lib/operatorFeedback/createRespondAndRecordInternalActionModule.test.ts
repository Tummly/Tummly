import { describe, expect, it, vi } from "vitest"

import type { FeedbackDetailsResponse } from "@/types/dashboard"
import {
  createRespondAndRecordInternalActionModule,
  type CompleteRecoveryResult,
  type PrepareRecoveryDraftRequest,
  type PrepareRecoveryDraftResult,
  type RespondAndRecordAdapters,
  type SendAndRecordRequest,
  type SendAndRecordResult,
} from "./createRespondAndRecordInternalActionModule"

const sampleDetails: FeedbackDetailsResponse = {
  success: true,
  id: 2418,
  guestName: "Mohamed M.",
  guestContact: "mohamed@email.com",
  contactType: "Email",
  comment: "Food was cold and delivery took too long.",
  createdAt: "2026-07-14T11:48:00.000Z",
  locationName: "Camden",
  address: "12 High Street",
  qrSource: "Delivery insert",
  classificationStatus: "Succeeded",
  sentiment: "negative",
  detectedTags: ["FoodTemperature"],
  locationGuestId: 501,
  workflowStatus: "in_progress",
  guestOffersOptOut: false,
  internalNotes: [],
  activityHistory: [],
}

function createAdapters(
  overrides: Partial<RespondAndRecordAdapters> = {}
): RespondAndRecordAdapters & {
  sendAndRecord: ReturnType<
    typeof vi.fn<(req: SendAndRecordRequest) => Promise<SendAndRecordResult>>
  >
  completeRecovery: ReturnType<
    typeof vi.fn<
      (
        feedbackId: number,
        intent: "respond_to_guest" | "record_internal_action_only" | "respond_and_record_internal_action"
      ) => Promise<CompleteRecoveryResult>
    >
  >
  prepareRecoveryDraft: ReturnType<
    typeof vi.fn<
      (
        request: PrepareRecoveryDraftRequest,
        signal?: AbortSignal
      ) => Promise<PrepareRecoveryDraftResult>
    >
  >
} {
  const sendAndRecord =
    overrides.sendAndRecord
    ?? vi.fn(async (): Promise<SendAndRecordResult> => ({
      workflowStatus: "in_progress",
      needsAttention: true,
      guestResponseActivityEvent: {
        kind: "guest_response_sent",
        at: "2026-08-03T12:00:00.000Z",
        actorDisplayName: "Alex",
        channel: "email",
        maskedDestination: "m••••@email.com",
      },
      internalActionActivityEvent: {
        kind: "internal_action_recorded",
        at: "2026-08-03T12:00:00.000Z",
        actorDisplayName: "Alex",
        category: "team_briefed",
        categoryLabel: "Team briefed",
        note: "Briefed the floor team.",
      },
    }))

  const completeRecovery =
    overrides.completeRecovery
    ?? vi.fn(async (): Promise<CompleteRecoveryResult> => ({
      workflowStatus: "resolved",
      needsAttention: false,
      activityEvent: {
        kind: "recovery_completed",
        at: "2026-08-03T12:05:00.000Z",
        actorDisplayName: "Alex",
        recoveryIntent: "respond_and_record_internal_action",
        fromWorkflowStatus: "in_progress",
        toWorkflowStatus: "resolved",
      },
    }))

  const prepareRecoveryDraft =
    overrides.prepareRecoveryDraft
    ?? vi.fn(async (): Promise<PrepareRecoveryDraftResult> => ({
      status: "succeeded",
      body: "Dear Mohamed, we briefed the team and are sorry.",
      subject: "Regarding your recent visit",
      channel: "email",
    }))

  return {
    getFeedbackDetails:
      overrides.getFeedbackDetails ?? (async () => ({ ...sampleDetails })),
    sendAndRecord: sendAndRecord as ReturnType<
      typeof vi.fn<(req: SendAndRecordRequest) => Promise<SendAndRecordResult>>
    >,
    completeRecovery: completeRecovery as ReturnType<
      typeof vi.fn<
        (
          feedbackId: number,
          intent: "respond_to_guest" | "record_internal_action_only" | "respond_and_record_internal_action"
        ) => Promise<CompleteRecoveryResult>
      >
    >,
    prepareRecoveryDraft: prepareRecoveryDraft as ReturnType<
      typeof vi.fn<
        (
          request: PrepareRecoveryDraftRequest,
          signal?: AbortSignal
        ) => Promise<PrepareRecoveryDraftResult>
      >
    >,
  }
}

async function fillRecorder(
  module: ReturnType<typeof createRespondAndRecordInternalActionModule>
) {
  module.setCategory("team_briefed")
  module.setNote("Briefed the floor team.")
  module.setUseConfirmedActionForGuestResponse(true)
  module.continueRecorder()
}

async function openAtSetup(
  module: ReturnType<typeof createRespondAndRecordInternalActionModule>
) {
  await module.open(2418)
  await fillRecorder(module)
}

async function openAtWrite(
  module: ReturnType<typeof createRespondAndRecordInternalActionModule>
) {
  await openAtSetup(module)
  module.setPurpose("acknowledge_feedback")
  module.setTone("warm_and_apologetic")
  module.continueSetup()
}

async function openAtReview(
  module: ReturnType<typeof createRespondAndRecordInternalActionModule>
) {
  await openAtWrite(module)
  module.writeManually()
  module.setSubject("Sorry about your visit")
  module.setMessage("Thank you for telling us.")
  module.continueWrite()
}

describe("createRespondAndRecordInternalActionModule", () => {
  it("opens at Internal action with checkbox unchecked gating Continue", async () => {
    const module = createRespondAndRecordInternalActionModule(createAdapters())
    await module.open(2418)

    expect(module.getSnapshot()).toMatchObject({
      isOpen: true,
      step: "recorder",
      feedbackId: 2418,
      useConfirmedActionForGuestResponse: false,
      canContinueRecorder: false,
    })

    module.setCategory("team_briefed")
    module.setNote("Briefed the floor team.")
    expect(module.getSnapshot().canContinueRecorder).toBe(false)

    module.setUseConfirmedActionForGuestResponse(true)
    expect(module.getSnapshot().canContinueRecorder).toBe(true)
    module.continueRecorder()
    expect(module.getSnapshot().step).toBe("setup")
  })

  it("runs recorder → setup → write → review", async () => {
    const module = createRespondAndRecordInternalActionModule(createAdapters())
    await openAtReview(module)
    expect(module.getSnapshot()).toMatchObject({
      step: "review",
      category: "team_briefed",
      note: "Briefed the floor team.",
      purpose: "acknowledge_feedback",
      message: "Thank you for telling us.",
    })
  })

  it("Edit text returns from Review to Guest response editor", async () => {
    const module = createRespondAndRecordInternalActionModule(createAdapters())
    await openAtReview(module)

    module.editText()

    expect(module.getSnapshot()).toMatchObject({
      step: "write",
      writeEntry: "editor",
      message: "Thank you for telling us.",
      sendConfirmOpen: false,
      guestPreviewOpen: false,
    })
  })

  it("open/close Guest preview does not mutate Subject or Message", async () => {
    const module = createRespondAndRecordInternalActionModule(createAdapters())
    await openAtReview(module)

    expect(module.getSnapshot().guestPreviewOpen).toBe(false)

    module.openGuestPreview()
    expect(module.getSnapshot()).toMatchObject({
      step: "review",
      guestPreviewOpen: true,
      subject: "Sorry about your visit",
      message: "Thank you for telling us.",
    })

    module.closeGuestPreview()
    expect(module.getSnapshot()).toMatchObject({
      step: "review",
      guestPreviewOpen: false,
      subject: "Sorry about your visit",
      message: "Thank you for telling us.",
    })
  })

  it("keeps Guest preview shut away from Review", async () => {
    const module = createRespondAndRecordInternalActionModule(createAdapters())
    await openAtWrite(module)

    module.openGuestPreview()

    expect(module.getSnapshot()).toMatchObject({
      step: "write",
      guestPreviewOpen: false,
    })
  })

  it("Edit text from Guest preview closes overlay and returns to editor", async () => {
    const module = createRespondAndRecordInternalActionModule(createAdapters())
    await openAtReview(module)
    module.openGuestPreview()

    module.editText()

    expect(module.getSnapshot()).toMatchObject({
      step: "write",
      writeEntry: "editor",
      guestPreviewOpen: false,
      subject: "Sorry about your visit",
      message: "Thank you for telling us.",
    })
  })

  it("Back and Edit internal action clear Guest preview", async () => {
    const module = createRespondAndRecordInternalActionModule(createAdapters())
    await openAtReview(module)

    module.openGuestPreview()
    module.back()
    expect(module.getSnapshot()).toMatchObject({
      step: "write",
      guestPreviewOpen: false,
    })

    module.continueWrite()
    module.openGuestPreview()
    expect(module.getSnapshot().guestPreviewOpen).toBe(true)
    module.editInternalAction()
    expect(module.getSnapshot()).toMatchObject({
      step: "recorder",
      guestPreviewOpen: false,
    })
  })

  it("send success and Save and exit clear Guest preview", async () => {
    const module = createRespondAndRecordInternalActionModule(createAdapters())
    await openAtReview(module)

    module.openGuestPreview()
    module.openSendConfirm()
    await module.confirmSend()

    expect(module.getSnapshot()).toMatchObject({
      step: "success",
      guestPreviewOpen: false,
    })

    await openAtReview(module)
    module.openGuestPreview()
    module.saveAndExit()
    expect(module.getSnapshot().guestPreviewOpen).toBe(false)
  })

  it("Back from recorder returns to shell; Edit internal action keeps guest draft", async () => {
    const module = createRespondAndRecordInternalActionModule(createAdapters())
    await module.open(2418)
    expect(module.back()).toBe("return-to-shell")
    expect(module.getSnapshot().isOpen).toBe(false)

    await openAtWrite(module)
    module.writeManually()
    module.setSubject("Keep me")
    module.setMessage("Keep this draft body.")
    module.editInternalAction()
    expect(module.getSnapshot()).toMatchObject({
      step: "recorder",
      subject: "Keep me",
      message: "Keep this draft body.",
      category: "team_briefed",
    })

    module.setUseConfirmedActionForGuestResponse(true)
    module.continueRecorder()
    expect(module.getSnapshot().step).toBe("write")
    expect(module.getSnapshot().message).toBe("Keep this draft body.")
  })

  it("Save and exit keeps intent-scoped draft; resume restores furthest step", async () => {
    const module = createRespondAndRecordInternalActionModule(createAdapters())
    await openAtWrite(module)
    module.writeManually()
    module.setSubject("Draft subject")
    module.setMessage("Draft body")
    module.saveAndExit()

    expect(module.getSnapshot().isOpen).toBe(false)

    await module.open(2418)
    expect(module.getSnapshot()).toMatchObject({
      isOpen: true,
      step: "write",
      subject: "Draft subject",
      message: "Draft body",
      category: "team_briefed",
      useConfirmedActionForGuestResponse: true,
    })
  })

  it("Prepare feeds confirmed internal action into the draft adapter", async () => {
    const adapters = createAdapters()
    const module = createRespondAndRecordInternalActionModule(adapters)
    await openAtWrite(module)

    await module.prepareDraft()

    expect(adapters.prepareRecoveryDraft).toHaveBeenCalledWith(
      expect.objectContaining({
        feedbackId: 2418,
        mode: "prepare",
        confirmedInternalAction: {
          category: "team_briefed",
          note: "Briefed the floor team.",
        },
      }),
      expect.any(AbortSignal)
    )
    expect(module.getSnapshot()).toMatchObject({
      subject: "Regarding your recent visit",
      message: "Dear Mohamed, we briefed the team and are sorry.",
      writeEntry: "editor",
    })
  })

  it("Send and record dual-writes and stays In progress without auto-resolve", async () => {
    const adapters = createAdapters()
    const module = createRespondAndRecordInternalActionModule(adapters)
    await openAtReview(module)

    module.openSendConfirm()
    await module.confirmSend()

    expect(adapters.sendAndRecord).toHaveBeenCalledWith({
      feedbackId: 2418,
      channel: "email",
      subject: "Sorry about your visit",
      body: "Thank you for telling us.",
      purpose: "acknowledge_feedback",
      tone: "warm_and_apologetic",
      includeNotes: null,
      category: "team_briefed",
      note: "Briefed the floor team.",
      intent: "respond_and_record_internal_action",
    })
    expect(adapters.completeRecovery).not.toHaveBeenCalled()
    expect(module.getSnapshot()).toMatchObject({
      step: "success",
      workflowStatus: "in_progress",
      sendConfirmOpen: false,
    })
    expect(module.getSnapshot().successReceipt).toMatchObject({
      kind: "guest_response_sent",
      channel: "email",
      actorDisplayName: "Alex",
    })

    module.saveAndExit()
    await module.open(2418)
    expect(module.getSnapshot().step).toBe("recorder")
    expect(module.getSnapshot().note).toBe("")
  })

  it("send failure stays on confirm; Mark resolved completes this intent", async () => {
    const adapters = createAdapters({
      sendAndRecord: vi.fn(async () => {
        throw new Error("dual-write failed")
      }),
    })
    const module = createRespondAndRecordInternalActionModule(adapters)
    await openAtReview(module)
    module.openSendConfirm()
    await module.confirmSend()

    expect(module.getSnapshot()).toMatchObject({
      step: "review",
      sendConfirmOpen: true,
      sendStatus: "error",
    })

    const okAdapters = createAdapters()
    const okModule = createRespondAndRecordInternalActionModule(okAdapters)
    await openAtReview(okModule)
    await okModule.confirmSend()
    okModule.keepInProgress()
    expect(okModule.getSnapshot().isOpen).toBe(false)
    expect(okAdapters.completeRecovery).not.toHaveBeenCalled()

    await openAtReview(okModule)
    await okModule.confirmSend()
    await okModule.markResolved()
    expect(okAdapters.completeRecovery).toHaveBeenCalledWith(
      2418,
      "respond_and_record_internal_action"
    )
    expect(okModule.getSnapshot().isOpen).toBe(false)
  })

  it("exposes location chrome and Delivery channel; meters successful AI only", async () => {
    const adapters = createAdapters()
    const module = createRespondAndRecordInternalActionModule(adapters)
    await module.open(2418)

    expect(module.getSnapshot()).toMatchObject({
      aiActionCount: 0,
      locationName: "Camden",
      locationAddress: "12 High Street",
      channel: "email",
    })

    await openAtWrite(module)
    await module.prepareDraft()
    expect(module.getSnapshot().aiActionCount).toBe(1)

    module.setMessage("Edited body")
    await module.rewriteDraft("message")
    expect(module.getSnapshot().aiActionCount).toBe(2)

    adapters.prepareRecoveryDraft.mockResolvedValueOnce({
      status: "failed",
      retryable: true,
    })
    await module.rewriteDraft("subject")
    expect(module.getSnapshot().aiActionCount).toBe(2)
  })
})

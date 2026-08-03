import { describe, expect, it, vi } from "vitest"

import type { FeedbackDetailsResponse } from "@/types/dashboard"
import {
  createRespondToGuestModule,
  type PrepareRecoveryDraftRequest,
  type PrepareRecoveryDraftResult,
  type RespondToGuestAdapters,
  type SendGuestResponseRequest,
  type SendGuestResponseResult,
  type CompleteRecoveryResult,
} from "./createRespondToGuestModule"

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
  detectedTags: ["FoodQuality", "WaitTime"],
  locationGuestId: 501,
  workflowStatus: "in_progress",
  guestOffersOptOut: false,
  internalNotes: [],
  activityHistory: [],
}

function createAdapters(
  overrides: Partial<RespondToGuestAdapters> = {}
): RespondToGuestAdapters & {
  sendGuestResponse: ReturnType<
    typeof vi.fn<(req: SendGuestResponseRequest) => Promise<SendGuestResponseResult>>
  >
  completeRecovery: ReturnType<
    typeof vi.fn<
      (
        feedbackId: number,
        intent:
          | "respond_to_guest"
          | "record_internal_action_only"
          | "respond_and_record_internal_action"
          | "respond_with_recovery_offer"
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
  const sendGuestResponse =
    overrides.sendGuestResponse
    ?? vi.fn(async (): Promise<SendGuestResponseResult> => ({
      workflowStatus: "in_progress",
      needsAttention: true,
      activityEvent: {
        kind: "guest_response_sent",
        at: "2026-08-03T12:00:00.000Z",
        actorDisplayName: "Alex",
        channel: "email",
        maskedDestination: "m••••@email.com",
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
        recoveryIntent: "respond_to_guest",
        fromWorkflowStatus: "in_progress",
        toWorkflowStatus: "resolved",
      },
    }))

  const prepareRecoveryDraft =
    overrides.prepareRecoveryDraft
    ?? vi.fn(async (): Promise<PrepareRecoveryDraftResult> => ({
      status: "succeeded",
      body: "Dear Mohamed, thank you for your feedback.",
      subject: "Regarding your recent visit",
      channel: "email",
    }))

  return {
    getFeedbackDetails:
      overrides.getFeedbackDetails ?? (async () => ({ ...sampleDetails })),
    sendGuestResponse: sendGuestResponse as ReturnType<
      typeof vi.fn<(req: SendGuestResponseRequest) => Promise<SendGuestResponseResult>>
    >,
    completeRecovery: completeRecovery as ReturnType<
      typeof vi.fn<
        (
          feedbackId: number,
          intent:
            | "respond_to_guest"
            | "record_internal_action_only"
            | "respond_and_record_internal_action"
            | "respond_with_recovery_offer"
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

async function openAtWrite(
  module: ReturnType<typeof createRespondToGuestModule>
) {
  await module.open(2418)
  module.setPurpose("acknowledge_feedback")
  module.setTone("warm_and_apologetic")
  module.continueSetup()
}

async function openAtEditor(
  module: ReturnType<typeof createRespondToGuestModule>
) {
  await openAtWrite(module)
  module.writeManually()
}

async function openAtReview(
  module: ReturnType<typeof createRespondToGuestModule>
) {
  await openAtEditor(module)
  module.setSubject("Sorry about your visit")
  module.setMessage("Thank you for telling us.")
  module.continueWrite()
}

describe("createRespondToGuestModule", () => {
  it("opens at Response setup with Email pre-selected and masked destination", async () => {
    const module = createRespondToGuestModule(createAdapters())
    await module.open(2418)

    const snapshot = module.getSnapshot()
    expect(snapshot).toMatchObject({
      isOpen: true,
      step: "setup",
      feedbackId: 2418,
      channel: "email",
      maskedDestination: "m••••@email.com",
      canContinueSetup: false,
    })
    expect(snapshot.maskedDestination).not.toContain("mohamed")
    expect(snapshot.availableChannels).toEqual(["email"])
    expect(snapshot.summary).toMatchObject({
      guestName: "Mohamed M.",
      classificationStatus: "Succeeded",
      classificationSentiment: "negative",
      contactLabel: "Email available",
      issueTagLabels: ["Food quality", "Wait time"],
      feedbackComment: "Food was cold and delivery took too long.",
    })
  })

  it("advances setup → write → review with required fields", async () => {
    const module = createRespondToGuestModule(createAdapters())
    await module.open(2418)

    expect(module.getSnapshot().canContinueSetup).toBe(false)
    module.setPurpose("acknowledge_feedback")
    module.setTone("warm_and_apologetic")
    expect(module.getSnapshot().canContinueSetup).toBe(true)
    module.continueSetup()
    expect(module.getSnapshot().step).toBe("write")

    module.writeManually()
    expect(module.getSnapshot().canContinueWrite).toBe(false)
    module.setSubject("Sorry")
    module.setMessage("We are looking into this.")
    expect(module.getSnapshot().canContinueWrite).toBe(true)
    module.continueWrite()
    expect(module.getSnapshot().step).toBe("review")
  })

  it("Back from setup signals return to entry shell; Back from write returns to setup", async () => {
    const module = createRespondToGuestModule(createAdapters())
    await module.open(2418)

    expect(module.back()).toBe("return-to-shell")
    expect(module.getSnapshot().isOpen).toBe(false)

    await module.open(2418)
    module.setPurpose("acknowledge_feedback")
    module.setTone("warm_and_apologetic")
    module.continueSetup()
    expect(module.back()).toBe("stayed")
    expect(module.getSnapshot().step).toBe("setup")
  })

  it("Save and exit keeps intent-scoped draft; resume restores furthest step", async () => {
    const module = createRespondToGuestModule(createAdapters())
    await openAtEditor(module)
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
      purpose: "acknowledge_feedback",
    })
  })

  it("Edit text returns from Review to Guest response editor", async () => {
    const module = createRespondToGuestModule(createAdapters())
    await openAtReview(module)

    module.editText()

    expect(module.getSnapshot()).toMatchObject({
      step: "write",
      writeEntry: "editor",
      subject: "Sorry about your visit",
      message: "Thank you for telling us.",
      sendConfirmOpen: false,
    })
  })

  it("successful send stays In progress, clears draft, and lands on success", async () => {
    const adapters = createAdapters()
    const module = createRespondToGuestModule(adapters)
    await openAtReview(module)

    module.openSendConfirm()
    expect(module.getSnapshot().sendConfirmOpen).toBe(true)
    expect(module.getSnapshot().maskedDestination).toBe("m••••@email.com")

    await module.confirmSend()

    expect(adapters.sendGuestResponse).toHaveBeenCalledWith(
      expect.objectContaining({
        feedbackId: 2418,
        channel: "email",
        subject: "Sorry about your visit",
        body: "Thank you for telling us.",
        intent: "respond_to_guest",
      })
    )
    expect(module.getSnapshot()).toMatchObject({
      step: "success",
      sendConfirmOpen: false,
      workflowStatus: "in_progress",
    })

    module.saveAndExit()
    await module.open(2418)
    expect(module.getSnapshot().step).toBe("setup")
    expect(module.getSnapshot().message).toBe("")
  })

  it("send failure stays on confirm with retry; no success step", async () => {
    const adapters = createAdapters({
      sendGuestResponse: vi.fn(async () => {
        throw new Error("delivery failed")
      }),
    })
    const module = createRespondToGuestModule(adapters)
    await openAtReview(module)
    module.openSendConfirm()

    await module.confirmSend()

    expect(module.getSnapshot()).toMatchObject({
      step: "review",
      sendConfirmOpen: true,
      sendStatus: "error",
    })
    expect(module.getSnapshot().sendError).toBeTruthy()
  })

  it("Keep in progress closes to inbox; Mark resolved completes recovery", async () => {
    const adapters = createAdapters()
    const module = createRespondToGuestModule(adapters)
    await openAtReview(module)
    await module.confirmSend()

    module.keepInProgress()
    expect(module.getSnapshot().isOpen).toBe(false)
    expect(adapters.completeRecovery).not.toHaveBeenCalled()

    await openAtReview(module)
    await module.confirmSend()
    await module.markResolved()

    expect(adapters.completeRecovery).toHaveBeenCalledWith(
      2418,
      "respond_to_guest"
    )
    expect(module.getSnapshot().isOpen).toBe(false)
  })

  it("SMS hides subject requirement", async () => {
    const adapters = createAdapters({
      getFeedbackDetails: async () => ({
        ...sampleDetails,
        contactType: "Phone",
        guestContact: "+447700900123",
      }),
    })
    const module = createRespondToGuestModule(adapters)
    await module.open(2418)

    expect(module.getSnapshot()).toMatchObject({
      channel: "sms",
      maskedDestination: "••••0123",
      availableChannels: ["sms"],
    })
    module.setPurpose("acknowledge_feedback")
    module.setTone("direct_and_practical")
    module.continueSetup()
    module.writeManually()
    module.setMessage("Thanks for your message.")
    expect(module.getSnapshot().canContinueWrite).toBe(true)
    module.continueWrite()
    expect(module.getSnapshot().step).toBe("review")
  })

  it("Prepare fills editable body and Email subject from adapter; no raw contact in inputs", async () => {
    const adapters = createAdapters()
    const module = createRespondToGuestModule(adapters)
    await openAtWrite(module)

    await module.prepareDraft()

    expect(adapters.prepareRecoveryDraft).toHaveBeenCalledWith(
      expect.objectContaining({
        feedbackId: 2418,
        channel: "email",
        purpose: "acknowledge_feedback",
        tone: "warm_and_apologetic",
        mode: "prepare",
      }),
      expect.any(AbortSignal)
    )
    const request = adapters.prepareRecoveryDraft.mock.calls[0]![0]
    expect(JSON.stringify(request)).not.toContain("mohamed@email.com")
    expect(module.getSnapshot()).toMatchObject({
      step: "write",
      subject: "Regarding your recent visit",
      message: "Dear Mohamed, thank you for your feedback.",
      aiDraftStatus: "idle",
      preparingOverlayOpen: false,
      actionsLocked: false,
      writeEntry: "editor",
    })
  })

  it("Preparing overlay: Write manually cancels AI; X dismisses overlay but AI continues; actions stay locked until settle", async () => {
    let resolveDraft!: (value: PrepareRecoveryDraftResult) => void
    const adapters = createAdapters({
      prepareRecoveryDraft: vi.fn(
        (_request, signal) =>
          new Promise<PrepareRecoveryDraftResult>((resolve, reject) => {
            resolveDraft = resolve
            signal?.addEventListener("abort", () => {
              reject(new DOMException("Aborted", "AbortError"))
            })
          })
      ),
    })
    const module = createRespondToGuestModule(adapters)
    await openAtWrite(module)

    const preparePromise = module.prepareDraft()
    expect(module.getSnapshot()).toMatchObject({
      aiDraftStatus: "running",
      preparingOverlayOpen: true,
      actionsLocked: true,
    })

    module.dismissPreparingOverlay()
    expect(module.getSnapshot()).toMatchObject({
      preparingOverlayOpen: false,
      actionsLocked: true,
      aiDraftStatus: "running",
    })
    expect(module.back()).toBe("stayed")
    module.saveAndExit()
    expect(module.getSnapshot().isOpen).toBe(true)

    resolveDraft({
      status: "succeeded",
      body: "Filled after dismiss",
      subject: "Subject after dismiss",
      channel: "email",
    })
    await preparePromise

    expect(module.getSnapshot()).toMatchObject({
      message: "Filled after dismiss",
      subject: "Subject after dismiss",
      actionsLocked: false,
      preparingOverlayOpen: false,
      writeEntry: "editor",
    })

    const adapters2 = createAdapters({
      prepareRecoveryDraft: vi.fn(
        (_request, signal) =>
          new Promise<PrepareRecoveryDraftResult>((_resolve, reject) => {
            signal?.addEventListener("abort", () => {
              reject(new DOMException("Aborted", "AbortError"))
            })
          })
      ),
    })
    const module2 = createRespondToGuestModule(adapters2)
    await openAtWrite(module2)
    const cancelled = module2.prepareDraft()
    module2.writeManually()
    await cancelled

    expect(module2.getSnapshot()).toMatchObject({
      writeEntry: "editor",
      aiDraftStatus: "idle",
      preparingOverlayOpen: false,
      actionsLocked: false,
      message: "",
      subject: "",
    })
  })

  it("Prepare failure unlocks, toasts, and offers retry; rewrite failure keeps prior text", async () => {
    const adapters = createAdapters({
      prepareRecoveryDraft: vi.fn(async () => ({
        status: "failed" as const,
        retryable: true,
      })),
    })
    const module = createRespondToGuestModule(adapters)
    await openAtWrite(module)
    await module.prepareDraft()

    expect(module.getSnapshot()).toMatchObject({
      aiDraftStatus: "failed",
      aiDraftError: "We could not prepare a draft.",
      aiDraftRetryable: true,
      actionsLocked: false,
      message: "",
      subject: "",
      preparingOverlayOpen: false,
    })

    adapters.prepareRecoveryDraft.mockResolvedValueOnce({
      status: "succeeded",
      body: "Prior body",
      subject: "Prior subject",
      channel: "email",
    })
    await module.retryAiDraft()
    expect(module.getSnapshot()).toMatchObject({
      message: "Prior body",
      subject: "Prior subject",
      writeEntry: "editor",
    })

    adapters.prepareRecoveryDraft.mockResolvedValueOnce({
      status: "failed",
      retryable: true,
    })
    await module.rewriteDraft()
    expect(adapters.prepareRecoveryDraft).toHaveBeenLastCalledWith(
      expect.objectContaining({
        mode: "rewrite",
        currentBody: "Prior body",
        currentSubject: "Prior subject",
      }),
      expect.any(AbortSignal)
    )
    expect(module.getSnapshot()).toMatchObject({
      message: "Prior body",
      subject: "Prior subject",
      aiDraftStatus: "failed",
      actionsLocked: false,
    })
  })
})

import { describe, expect, it, vi } from "vitest"

import type { FeedbackDetailsResponse } from "@/types/dashboard"
import type {
  CompleteRecoveryResult,
  PrepareRecoveryDraftResult,
} from "./createRespondToGuestModule"
import {
  createRespondWithRecoveryOfferModule,
  type PrepareRecoveryOfferDraftRequest,
  type RespondWithRecoveryOfferAdapters,
  type SendAndIssueRecoveryOfferRequest,
  type SendAndIssueRecoveryOfferResult,
} from "./createRespondWithRecoveryOfferModule"

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
  overrides: Partial<RespondWithRecoveryOfferAdapters> = {}
): RespondWithRecoveryOfferAdapters & {
  sendAndIssueRecoveryOffer: ReturnType<
    typeof vi.fn<
      (
        req: SendAndIssueRecoveryOfferRequest
      ) => Promise<SendAndIssueRecoveryOfferResult>
    >
  >
  completeRecovery: ReturnType<
    typeof vi.fn<
      (
        feedbackId: number,
        intent:
          | "respond_to_guest"
          | "record_internal_action_only"
          | "respond_with_recovery_offer"
      ) => Promise<CompleteRecoveryResult>
    >
  >
  prepareRecoveryDraft: ReturnType<
    typeof vi.fn<
      (
        request: PrepareRecoveryOfferDraftRequest,
        signal?: AbortSignal
      ) => Promise<PrepareRecoveryDraftResult>
    >
  >
} {
  const sendAndIssueRecoveryOffer =
    overrides.sendAndIssueRecoveryOffer
    ?? vi.fn(async (): Promise<SendAndIssueRecoveryOfferResult> => ({
      workflowStatus: "in_progress",
      needsAttention: true,
      guestResponseActivityEvent: {
        kind: "guest_response_sent",
        at: "2026-08-03T12:00:00.000Z",
        actorDisplayName: "Alex",
        channel: "email",
        maskedDestination: "m••••@email.com",
      },
      recoveryOfferActivityEvent: {
        kind: "recovery_offer_issued",
        at: "2026-08-03T12:00:00.000Z",
        actorDisplayName: "Alex",
        offerType: "percentage_discount",
        title: "20% off",
        validity: "30_days_after_issue",
        expiryAt: "2026-09-02T23:59:59.000Z",
        redemptionCode: "TUM-ABC123",
      },
      issuedOffer: {
        title: "20% off",
        redemptionCode: "TUM-ABC123",
        expiryAt: "2026-09-02T23:59:59.000Z",
        validity: "30_days_after_issue",
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
        recoveryIntent: "respond_with_recovery_offer",
        fromWorkflowStatus: "in_progress",
        toWorkflowStatus: "resolved",
      },
    }))

  const prepareRecoveryDraft =
    overrides.prepareRecoveryDraft
    ?? vi.fn(async (): Promise<PrepareRecoveryDraftResult> => ({
      status: "succeeded",
      body: "Dear Mohamed, here is 20% off your next visit.",
      subject: "A recovery offer from us",
      channel: "email",
    }))

  return {
    getFeedbackDetails:
      overrides.getFeedbackDetails ?? (async () => ({ ...sampleDetails })),
    sendAndIssueRecoveryOffer: sendAndIssueRecoveryOffer as ReturnType<
      typeof vi.fn<
        (
          req: SendAndIssueRecoveryOfferRequest
        ) => Promise<SendAndIssueRecoveryOfferResult>
      >
    >,
    completeRecovery: completeRecovery as ReturnType<
      typeof vi.fn<
        (
          feedbackId: number,
          intent:
            | "respond_to_guest"
            | "record_internal_action_only"
            | "respond_with_recovery_offer"
        ) => Promise<CompleteRecoveryResult>
      >
    >,
    prepareRecoveryDraft: prepareRecoveryDraft as ReturnType<
      typeof vi.fn<
        (
          request: PrepareRecoveryOfferDraftRequest,
          signal?: AbortSignal
        ) => Promise<PrepareRecoveryDraftResult>
      >
    >,
  }
}

async function fillValidOffer(
  module: ReturnType<typeof createRespondWithRecoveryOfferModule>
) {
  module.setOfferType("percentage_discount")
  module.setDiscountPercentage("20")
  module.setOfferDescription("Thanks for your feedback — enjoy 20% off.")
}

async function openAtOffer(
  module: ReturnType<typeof createRespondWithRecoveryOfferModule>
) {
  await module.open(2418)
  module.setTone("warm_and_apologetic")
  module.continueSetup()
}

async function openAtWrite(
  module: ReturnType<typeof createRespondWithRecoveryOfferModule>
) {
  await openAtOffer(module)
  await fillValidOffer(module)
  module.continueOffer()
}

async function openAtReview(
  module: ReturnType<typeof createRespondWithRecoveryOfferModule>
) {
  await openAtWrite(module)
  module.writeManually()
  module.setSubject("Sorry about your visit")
  module.setMessage("Please use this offer on your next visit.")
  module.continueWrite()
}

describe("createRespondWithRecoveryOfferModule", () => {
  it("opens at Response setup with purpose fixed to Include a recovery offer", async () => {
    const module = createRespondWithRecoveryOfferModule(createAdapters())
    await module.open(2418)

    const snap = module.getSnapshot()
    expect(snap.step).toBe("setup")
    expect(snap.purpose).toBe("include_a_recovery_offer")
    expect(snap.purposeLabel).toBe("Include a recovery offer")
    expect(snap.channel).toBe("email")
    expect(snap.maskedDestination).toBe("m••••@email.com")
    expect(snap.canContinueSetup).toBe(false)
  })

  it("moves setup → offer → write → review and sends with confirmed offer", async () => {
    const adapters = createAdapters()
    const module = createRespondWithRecoveryOfferModule(adapters)

    await openAtReview(module)
    expect(module.getSnapshot().step).toBe("review")
    expect(module.getSnapshot().offer.title).toBe("20% off")

    module.openSendConfirm()
    await module.confirmSend()

    expect(adapters.sendAndIssueRecoveryOffer).toHaveBeenCalledWith(
      expect.objectContaining({
        intent: "respond_with_recovery_offer",
        purpose: "include_a_recovery_offer",
        offer: expect.objectContaining({
          offerType: "percentage_discount",
          title: "20% off",
          discountPercentage: 20,
        }),
      })
    )

    const snap = module.getSnapshot()
    expect(snap.step).toBe("success")
    expect(snap.workflowStatus).toBe("in_progress")
    expect(snap.issuedOffer?.redemptionCode).toBe("TUM-ABC123")
  })

  it("passes confirmed offer into the draft adapter", async () => {
    const adapters = createAdapters()
    const module = createRespondWithRecoveryOfferModule(adapters)

    await openAtWrite(module)
    await module.prepareDraft()

    expect(adapters.prepareRecoveryDraft).toHaveBeenCalledWith(
      expect.objectContaining({
        purpose: "include_a_recovery_offer",
        confirmedOffer: expect.objectContaining({
          offerType: "percentage_discount",
          title: "20% off",
        }),
      }),
      expect.anything()
    )
    expect(module.getSnapshot().writeEntry).toBe("editor")
    expect(module.getSnapshot().message).toContain("20% off")
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
    const module = createRespondWithRecoveryOfferModule(adapters)
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
    const module2 = createRespondWithRecoveryOfferModule(adapters2)
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

  it("Edit offer returns to Offer details; Edit text returns to write", async () => {
    const module = createRespondWithRecoveryOfferModule(createAdapters())
    await openAtReview(module)

    module.editOffer()
    expect(module.getSnapshot().step).toBe("offer")

    module.continueOffer()
    module.writeManually()
    module.setSubject("Sorry about your visit")
    module.setMessage("Please use this offer on your next visit.")
    module.continueWrite()
    module.editText()
    expect(module.getSnapshot().step).toBe("write")
  })

  it("keeps an intent-scoped draft across Save and exit", async () => {
    const module = createRespondWithRecoveryOfferModule(createAdapters())
    await openAtOffer(module)
    await fillValidOffer(module)
    module.saveAndExit()
    expect(module.getSnapshot().isOpen).toBe(false)

    await module.open(2418)
    expect(module.getSnapshot().step).toBe("offer")
    expect(module.getSnapshot().offer.discountPercentage).toBe("20")
  })

  it("Mark resolved completes with respond_with_recovery_offer intent", async () => {
    const adapters = createAdapters()
    const module = createRespondWithRecoveryOfferModule(adapters)
    await openAtReview(module)
    await module.confirmSend()
    await module.markResolved()

    expect(adapters.completeRecovery).toHaveBeenCalledWith(
      2418,
      "respond_with_recovery_offer"
    )
    expect(module.getSnapshot().isOpen).toBe(false)
  })

  it("Back from setup returns to the entry shell", async () => {
    const module = createRespondWithRecoveryOfferModule(createAdapters())
    await module.open(2418)
    expect(module.back()).toBe("return-to-shell")
  })
})

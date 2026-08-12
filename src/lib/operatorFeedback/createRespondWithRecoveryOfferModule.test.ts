import { describe, expect, it, vi } from "vitest"

import type { FeedbackDetailsResponse } from "@/types/dashboard"
import type { CatalogOfferDetail } from "@/types/operatorCampaigns"
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

  const sendGuestPreviewTest =
    overrides.sendGuestPreviewTest
    ?? vi.fn(async () => {})

  return {
    getFeedbackDetails:
      overrides.getFeedbackDetails ?? (async () => ({ ...sampleDetails })),
    getRecoveryOfferAttach:
      overrides.getRecoveryOfferAttach
      ?? (async () => null),
    setRecoveryOfferAttach:
      overrides.setRecoveryOfferAttach
      ?? (async () => {}),
    getLocationId: overrides.getLocationId ?? (() => 7),
    createOffer:
      overrides.createOffer
      ?? (async () => sampleCatalogOffer()),
    getOffer: overrides.getOffer,
    updateOffer: overrides.updateOffer,
    sendAndIssueRecoveryOffer: sendAndIssueRecoveryOffer as ReturnType<
      typeof vi.fn<
        (
          req: SendAndIssueRecoveryOfferRequest
        ) => Promise<SendAndIssueRecoveryOfferResult>
      >
    >,
    sendGuestPreviewTest: sendGuestPreviewTest as RespondWithRecoveryOfferAdapters["sendGuestPreviewTest"],
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

function sampleCatalogOffer(
  overrides: Partial<CatalogOfferDetail> = {}
): CatalogOfferDetail {
  return {
    id: 501,
    locationId: 7,
    status: "active",
    offerType: "percentage_discount",
    title: "20% off",
    description: "Thanks for your feedback — enjoy 20% off.",
    validity: "30_days_after_issue",
    expiryDate: null,
    discountPercentage: 20,
    discountAmount: null,
    freeItemText: null,
    purchaseRequirement: null,
    minimumSpend: null,
    additionalExclusions: null,
    replacementItemText: null,
    staffInstructions: "Ask the guest to show this unique code.",
    issueCount: 0,
    createdAt: "2026-08-12T00:00:00.000Z",
    updatedAt: "2026-08-12T00:00:00.000Z",
    ...overrides,
  }
}

async function fillValidOffer(
  module: ReturnType<typeof createRespondWithRecoveryOfferModule>
) {
  module.setOfferStanceId("create-and-select")
  module.patchCreateOfferDraft({
    offerType: "percentage_discount",
    discountPercentage: "20",
    title: "20% off",
    description: "Thanks for your feedback — enjoy 20% off.",
    validity: "30_days_after_issue",
  })
  await module.confirmCreateOffer()
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
    expect(module.getSnapshot()).toMatchObject({
      step: "write",
      guestPreviewOpen: false,
    })
  })

  it("open/close Guest preview does not mutate Subject, Message, or offer", async () => {
    const module = createRespondWithRecoveryOfferModule(createAdapters())
    await openAtReview(module)

    expect(module.getSnapshot().guestPreviewOpen).toBe(false)

    module.openGuestPreview()
    expect(module.getSnapshot()).toMatchObject({
      step: "review",
      guestPreviewOpen: true,
      subject: "Sorry about your visit",
      message: "Please use this offer on your next visit.",
      offer: expect.objectContaining({
        title: "20% off",
        discountPercentage: "20",
      }),
    })

    module.closeGuestPreview()
    expect(module.getSnapshot()).toMatchObject({
      step: "review",
      guestPreviewOpen: false,
      subject: "Sorry about your visit",
      message: "Please use this offer on your next visit.",
      offer: expect.objectContaining({
        title: "20% off",
        discountPercentage: "20",
      }),
    })
  })

  it("Guest preview send test includes sample offer block and does not issue", async () => {
    const adapters = createAdapters()
    const module = createRespondWithRecoveryOfferModule(adapters)
    await openAtReview(module)

    await module.sendGuestPreviewTest()

    expect(adapters.sendGuestPreviewTest).toHaveBeenCalledWith({
      feedbackId: 2418,
      subject: "Sorry about your visit",
      body: "Please use this offer on your next visit.",
      offer: expect.objectContaining({
        title: "20% off",
        description: "Thanks for your feedback — enjoy 20% off.",
        expiryLabel: expect.stringMatching(/^Expires:/),
      }),
    })
    expect(module.getSnapshot()).toMatchObject({
      step: "review",
      sendTestStatus: "success",
      sendTestError: null,
    })
    expect(adapters.sendAndIssueRecoveryOffer).not.toHaveBeenCalled()
  })

  it("Guest preview send test failure stays retryable without issuing", async () => {
    const adapters = createAdapters({
      sendGuestPreviewTest: vi.fn(async () => {
        throw new Error("Resend failed")
      }),
    })
    const module = createRespondWithRecoveryOfferModule(adapters)
    await openAtReview(module)

    await module.sendGuestPreviewTest()

    expect(module.getSnapshot()).toMatchObject({
      step: "review",
      sendTestStatus: "error",
      sendTestError: "We could not send the test email. Try again.",
    })

    await module.sendGuestPreviewTest()
    expect(adapters.sendGuestPreviewTest).toHaveBeenCalledTimes(2)
    expect(adapters.sendAndIssueRecoveryOffer).not.toHaveBeenCalled()
  })

  it("Edit text from Guest preview closes overlay and returns to editor", async () => {
    const module = createRespondWithRecoveryOfferModule(createAdapters())
    await openAtReview(module)
    module.openGuestPreview()

    module.editText()

    expect(module.getSnapshot()).toMatchObject({
      step: "write",
      writeEntry: "editor",
      guestPreviewOpen: false,
      subject: "Sorry about your visit",
      message: "Please use this offer on your next visit.",
    })
  })

  it("successful send clears Guest preview and lands on success", async () => {
    const adapters = createAdapters()
    const module = createRespondWithRecoveryOfferModule(adapters)
    await openAtReview(module)
    module.openGuestPreview()
    expect(module.getSnapshot().guestPreviewOpen).toBe(true)

    module.openSendConfirm()
    await module.confirmSend()

    expect(module.getSnapshot()).toMatchObject({
      step: "success",
      guestPreviewOpen: false,
      sendConfirmOpen: false,
    })
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

  it("exposes location chrome; meters guest-message and offer-description AI", async () => {
    const adapters = createAdapters()
    const module = createRespondWithRecoveryOfferModule(adapters)
    await module.open(2418)

    expect(module.getSnapshot()).toMatchObject({
      aiActionCount: 0,
      locationName: "Camden",
      locationAddress: "12 High Street",
      channel: "email",
    })

    await openAtOffer(module)
    await fillValidOffer(module)
    await module.prepareOfferDescription()
    expect(module.getSnapshot().aiActionCount).toBe(1)
    expect(module.getSnapshot().offer.description.length).toBeGreaterThan(0)

    adapters.prepareRecoveryDraft.mockResolvedValueOnce({
      status: "failed",
      retryable: true,
    })
    await module.prepareOfferDescription()
    expect(module.getSnapshot().aiActionCount).toBe(1)

    module.continueOffer()
    await module.prepareDraft()
    expect(module.getSnapshot().aiActionCount).toBe(2)

    await module.rewriteDraft("message")
    expect(module.getSnapshot().aiActionCount).toBe(3)
  })

  it("persists offerId on set/clear/saveAndExit and hydrates on open", async () => {
    const store = new Map<number, number | null>()
    const getRecoveryOfferAttach = vi.fn(async (feedbackId: number) => {
      return store.has(feedbackId) ? (store.get(feedbackId) ?? null) : null
    })
    const setRecoveryOfferAttach = vi.fn(
      async (feedbackId: number, offerId: number | null) => {
        store.set(feedbackId, offerId)
      }
    )
    const adapters = createAdapters({
      getRecoveryOfferAttach,
      setRecoveryOfferAttach,
    })
    const module = createRespondWithRecoveryOfferModule(adapters)
    await module.open(2418)
    expect(module.getSnapshot().offerId).toBeNull()

    await module.setOfferId(77)
    expect(module.getSnapshot().offerId).toBe(77)
    expect(setRecoveryOfferAttach).toHaveBeenCalledWith(2418, 77)

    await module.setOfferId(88)
    expect(module.getSnapshot().offerId).toBe(88)

    await module.setOfferId(null)
    expect(module.getSnapshot().offerId).toBeNull()
    expect(setRecoveryOfferAttach).toHaveBeenCalledWith(2418, null)

    await module.setOfferId(91)
    module.saveAndExit()
    expect(setRecoveryOfferAttach).toHaveBeenCalledWith(2418, 91)
    expect(module.getSnapshot().isOpen).toBe(false)

    const module2 = createRespondWithRecoveryOfferModule(adapters)
    await module2.open(2418)
    expect(getRecoveryOfferAttach).toHaveBeenCalledWith(2418)
    expect(module2.getSnapshot().offerId).toBe(91)
  })

  it("confirmCreateOffer creates catalog Offer then attaches Recovery OfferId", async () => {
    const createOffer = vi.fn(async () => sampleCatalogOffer({ id: 501 }))
    const setRecoveryOfferAttach = vi.fn(async () => {})
    const adapters = createAdapters({ createOffer, setRecoveryOfferAttach })
    const module = createRespondWithRecoveryOfferModule(adapters)
    await openAtOffer(module)

    expect(module.getSnapshot().canContinueOffer).toBe(false)

    module.setOfferStanceId("create-and-select")
    expect(module.getSnapshot()).toMatchObject({
      offerStanceId: "create-and-select",
      createPanelOpen: true,
      attachedOfferTitle: null,
    })

    module.patchCreateOfferDraft({
      offerType: "percentage_discount",
      discountPercentage: "20",
      title: "20% off",
      description: "Thanks for your feedback — enjoy 20% off.",
      validity: "30_days_after_issue",
    })
    expect(module.getSnapshot().canConfirmCreateOffer).toBe(true)

    const result = await module.confirmCreateOffer()

    expect(result).toBe("created")
    expect(createOffer).toHaveBeenCalledTimes(1)
    expect(createOffer).toHaveBeenCalledWith(
      expect.objectContaining({
        locationId: 7,
        offerType: "percentage_discount",
        title: "20% off",
        discountPercentage: 20,
      })
    )
    expect(setRecoveryOfferAttach).toHaveBeenCalledWith(2418, 501)
    expect(module.getSnapshot()).toMatchObject({
      createPanelOpen: false,
      offerId: 501,
      attachedOfferTitle: "20% off",
      canContinueOffer: true,
      offer: expect.objectContaining({
        offerType: "percentage_discount",
        title: "20% off",
        discountPercentage: "20",
        description: "Thanks for your feedback — enjoy 20% off.",
      }),
    })

    module.continueOffer()
    expect(module.getSnapshot().step).toBe("write")
  })

  it("canContinueOffer stays false without Active attach after create panel close", async () => {
    const module = createRespondWithRecoveryOfferModule(createAdapters())
    await openAtOffer(module)

    module.setOfferStanceId("create-and-select")
    module.closeCreateOfferPanel()
    expect(module.getSnapshot()).toMatchObject({
      createPanelOpen: false,
      offerId: null,
      canContinueOffer: false,
    })
    module.continueOffer()
    expect(module.getSnapshot().step).toBe("offer")
  })

  it("confirmCreateOffer does not keep local attach when durable attach fails", async () => {
    const createOffer = vi.fn(async () => sampleCatalogOffer({ id: 501 }))
    const setRecoveryOfferAttach = vi.fn(async () => {
      throw new Error("attach failed")
    })
    const module = createRespondWithRecoveryOfferModule(
      createAdapters({ createOffer, setRecoveryOfferAttach })
    )
    await openAtOffer(module)

    module.setOfferStanceId("create-and-select")
    module.patchCreateOfferDraft({
      offerType: "percentage_discount",
      discountPercentage: "20",
      title: "20% off",
      description: "Thanks for your feedback — enjoy 20% off.",
      validity: "30_days_after_issue",
    })

    const result = await module.confirmCreateOffer()

    expect(result).toBe("error")
    expect(createOffer).toHaveBeenCalledTimes(1)
    expect(setRecoveryOfferAttach).toHaveBeenCalledWith(2418, 501)
    expect(module.getSnapshot()).toMatchObject({
      createPanelOpen: true,
      offerId: null,
      canContinueOffer: false,
      createOfferStatus: "error",
    })
  })

  it("canContinueOffer is false when hydrated attach is not Active", async () => {
    const getRecoveryOfferAttach = vi.fn(async () => 88)
    const getOffer = vi.fn(async () =>
      sampleCatalogOffer({ id: 88, status: "paused", title: "Paused offer" })
    )
    const module = createRespondWithRecoveryOfferModule(
      createAdapters({ getRecoveryOfferAttach, getOffer })
    )
    await openAtOffer(module)

    expect(getOffer).toHaveBeenCalledWith(88)
    expect(module.getSnapshot()).toMatchObject({
      offerId: 88,
      attachedOfferTitle: "Paused offer",
      canContinueOffer: false,
    })
    module.continueOffer()
    expect(module.getSnapshot().step).toBe("offer")
  })
})

import { describe, expect, it, vi } from "vitest"

import {
  createStubVoidRequestAdapters,
  type VoidCreatePreview,
  type VoidRequestAdapters,
  type VoidReviewDetail,
} from "@/lib/operatorOffers/voidRequestAdapters"
import { createVoidRequestModule } from "@/lib/operatorOffers/createVoidRequestModule"
import { VOID_REQUEST_COPY } from "@/lib/operatorOffers/voidRequestPresentation"

function samplePreview(
  overrides?: Partial<VoidCreatePreview>
): VoidCreatePreview {
  return {
    passId: "pass-1",
    redemptionId: "redemption-1",
    offerId: 10,
    locationId: 3,
    offerTitle: "10% off next visit",
    guestName: "Maya Patel",
    passCodeMasked: "•••• 84K2",
    currentStateText: "Redeemed",
    expiresText: "31 August 2026",
    locationName: "Camden",
    linkedCampaignText: "Not issued through a campaign",
    ...overrides,
  }
}

function sampleReview(
  overrides?: Partial<VoidReviewDetail>
): VoidReviewDetail {
  return {
    requestId: "void-1",
    passId: "pass-1",
    offerId: 10,
    locationId: 3,
    requestedByText: "Jamie R.",
    requestedAtText: "Today, 13:12",
    reasonId: "redeemed_by_mistake",
    reasonText: "Redeemed by mistake",
    explanation: "Guest showed the code but discount was not applied.",
    correctionId: "restore_one_use",
    correctionText: "Restore one redemption use",
    offerTitle: "10% off next visit",
    guestName: "Maya Patel",
    passCodeMasked: "•••• 84K2",
    currentStateText: "Redeemed",
    expiresText: "31 August 2026",
    locationName: "Camden",
    linkedCampaignText: "Not issued through a campaign",
    ...overrides,
  }
}

function createModule(adapters?: Partial<VoidRequestAdapters>) {
  return createVoidRequestModule(createStubVoidRequestAdapters(adapters))
}

describe("createVoidRequestModule", () => {
  it("caches getSnapshot for useSyncExternalStore identity", () => {
    const module = createModule()
    expect(module.getSnapshot()).toBe(module.getSnapshot())
  })

  it("openCreate opens create step with preview and no preselected correction", () => {
    const module = createModule()
    const preview = samplePreview()
    module.openCreate(preview)

    const snapshot = module.getSnapshot()
    expect(snapshot.open).toBe(true)
    expect(snapshot.step).toBe("create")
    expect(snapshot.createPreview).toEqual(preview)
    expect(snapshot.reasonId).toBeNull()
    expect(snapshot.explanation).toBe("")
    expect(snapshot.correctionId).toBeNull()
    expect(snapshot.formError).toBeNull()
  })

  it("openReview opens review step with detail", () => {
    const module = createModule()
    const detail = sampleReview()
    module.openReview(detail)

    const snapshot = module.getSnapshot()
    expect(snapshot.open).toBe(true)
    expect(snapshot.step).toBe("review")
    expect(snapshot.reviewDetail).toEqual(detail)
  })

  it("rejects send without reason or correction", async () => {
    const createRequest = vi.fn()
    const module = createModule({ createRequest })
    module.openCreate(samplePreview())

    const result = await module.sendRequest()

    expect(result).toBe("validation_failed")
    expect(createRequest).not.toHaveBeenCalled()
    expect(module.getSnapshot().formError).toBe(
      VOID_REQUEST_COPY.errors.reasonAndCorrectionRequired
    )
    expect(module.getSnapshot().open).toBe(true)
  })

  it("requires explanation when reason is Other", async () => {
    const createRequest = vi.fn()
    const module = createModule({ createRequest })
    module.openCreate(samplePreview())
    module.setReason("other")
    module.setCorrection("keep_unusable")
    module.setExplanation("   ")

    const result = await module.sendRequest()

    expect(result).toBe("validation_failed")
    expect(createRequest).not.toHaveBeenCalled()
    expect(module.getSnapshot().formError).toBe(
      VOID_REQUEST_COPY.errors.explanationRequired
    )
  })

  it("sendRequest creates pending and notifies approvers", async () => {
    const notifyApprovers = vi.fn(async () => undefined)
    const createRequest = vi.fn(async () => ({
      ok: true as const,
      requestId: "void-new",
    }))
    const module = createModule({ createRequest, notifyApprovers })
    module.openCreate(samplePreview())
    module.setReason("redeemed_by_mistake")
    module.setCorrection("keep_unusable")

    const result = await module.sendRequest()

    expect(result).toBe("sent")
    expect(createRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        passId: "pass-1",
        redemptionId: "redemption-1",
        reasonId: "redeemed_by_mistake",
        correctionId: "keep_unusable",
        explanation: null,
      })
    )
    expect(notifyApprovers).toHaveBeenCalledWith("void-new")
    expect(module.getSnapshot().open).toBe(false)
    expect(module.getSnapshot().step).toBe("closed")
  })

  it("fails send when a pending request already exists for the pass", async () => {
    const createRequest = vi.fn(async () => ({
      ok: false as const,
      reason: "pending_exists" as const,
    }))
    const module = createModule({ createRequest })
    module.openCreate(samplePreview())
    module.setReason("redeemed_by_mistake")
    module.setCorrection("restore_one_use")

    const result = await module.sendRequest()

    expect(result).toBe("pending_exists")
    expect(module.getSnapshot().formError).toBe(
      VOID_REQUEST_COPY.errors.pendingExists
    )
    expect(module.getSnapshot().open).toBe(true)
  })

  it("enforces one pending per pass in stub adapters across sends", async () => {
    const adapters = createStubVoidRequestAdapters()
    const first = createVoidRequestModule(adapters)
    first.openCreate(samplePreview({ passId: "pass-shared" }))
    first.setReason("duplicate_redemption")
    first.setCorrection("keep_unusable")
    expect(await first.sendRequest()).toBe("sent")

    const second = createVoidRequestModule(adapters)
    second.openCreate(samplePreview({ passId: "pass-shared", redemptionId: "r2" }))
    second.setReason("redeemed_by_mistake")
    second.setCorrection("keep_unusable")
    expect(await second.sendRequest()).toBe("pending_exists")
  })

  it("requestApprove and requestReject move to confirm steps", () => {
    const module = createModule()
    module.openReview(sampleReview({ requestId: "void-9" }))

    module.requestApprove()
    expect(module.getSnapshot().step).toBe("confirm-approve")
    expect(module.getSnapshot().activeRequestId).toBe("void-9")

    module.goBack()
    expect(module.getSnapshot().step).toBe("review")

    module.requestReject()
    expect(module.getSnapshot().step).toBe("confirm-reject")
  })

  it("openApproveConfirm and openRejectConfirm open confirms from request id", async () => {
    const detail = sampleReview({ requestId: "void-notify" })
    const getRequest = vi.fn(async () => detail)
    const module = createModule({ getRequest })

    await module.openApproveConfirm("void-notify")
    expect(getRequest).toHaveBeenCalledWith("void-notify")
    expect(module.getSnapshot().step).toBe("confirm-approve")
    expect(module.getSnapshot().reviewDetail).toEqual(detail)

    module.close()
    await module.openRejectConfirm("void-notify")
    expect(module.getSnapshot().step).toBe("confirm-reject")
  })

  it("confirmApprove approves and notifies submitter", async () => {
    const approveRequest = vi.fn(async () => ({ ok: true as const }))
    const notifySubmitter = vi.fn(async () => undefined)
    const module = createModule({ approveRequest, notifySubmitter })
    module.openReview(sampleReview({ requestId: "void-a" }))
    module.requestApprove()

    const result = await module.confirmApprove()

    expect(result).toBe("approved")
    expect(approveRequest).toHaveBeenCalledWith("void-a")
    expect(notifySubmitter).toHaveBeenCalledWith("void-a", "approved")
    expect(module.getSnapshot().open).toBe(false)
  })

  it("confirmReject rejects and notifies submitter", async () => {
    const rejectRequest = vi.fn(async () => ({ ok: true as const }))
    const notifySubmitter = vi.fn(async () => undefined)
    const module = createModule({ rejectRequest, notifySubmitter })
    module.openReview(sampleReview({ requestId: "void-r" }))
    module.requestReject()

    const result = await module.confirmReject()

    expect(result).toBe("rejected")
    expect(rejectRequest).toHaveBeenCalledWith("void-r")
    expect(notifySubmitter).toHaveBeenCalledWith("void-r", "rejected")
    expect(module.getSnapshot().open).toBe(false)
  })

  it("goBack from confirm returns to review", () => {
    const module = createModule()
    module.openReview(sampleReview())
    module.requestApprove()
    module.goBack()
    expect(module.getSnapshot().step).toBe("review")
    expect(module.getSnapshot().open).toBe(true)
  })

  it("close resets state", () => {
    const module = createModule()
    module.openCreate(samplePreview())
    module.setReason("other")
    module.setExplanation("because")
    module.close()

    const snapshot = module.getSnapshot()
    expect(snapshot.open).toBe(false)
    expect(snapshot.step).toBe("closed")
    expect(snapshot.createPreview).toBeNull()
    expect(snapshot.reasonId).toBeNull()
    expect(snapshot.explanation).toBe("")
  })

  it("clears form error when reason changes", async () => {
    const module = createModule()
    module.openCreate(samplePreview())
    await module.sendRequest()
    expect(module.getSnapshot().formError).not.toBeNull()

    module.setReason("redeemed_by_mistake")
    expect(module.getSnapshot().formError).toBeNull()
  })

  it("allows a new create after reject clears pending in stub", async () => {
    const adapters = createStubVoidRequestAdapters()
    const module = createVoidRequestModule(adapters)
    module.openCreate(samplePreview({ passId: "pass-retry" }))
    module.setReason("redeemed_by_mistake")
    module.setCorrection("keep_unusable")
    expect(await module.sendRequest()).toBe("sent")

    const pending = await adapters.getRequestByPassId("pass-retry")
    expect(pending).not.toBeNull()

    module.openReview(
      sampleReview({
        requestId: pending!.requestId,
        passId: "pass-retry",
      })
    )
    module.requestReject()
    expect(await module.confirmReject()).toBe("rejected")

    module.openCreate(samplePreview({ passId: "pass-retry", redemptionId: "r3" }))
    module.setReason("wrong_offer_pass")
    module.setCorrection("restore_one_use")
    expect(await module.sendRequest()).toBe("sent")
  })
})

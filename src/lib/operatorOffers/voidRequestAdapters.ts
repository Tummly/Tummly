import {
  VOID_REQUEST_COPY,
  type VoidRequestCorrectionId,
  type VoidRequestReasonId,
} from "@/lib/operatorOffers/voidRequestPresentation"

export type VoidPassSummary = {
  offerTitle: string
  guestName: string
  passCodeMasked: string
  currentStateText: string
  expiresText: string
  locationName: string
  linkedCampaignText: string
}

export type VoidCreatePreview = VoidPassSummary & {
  passId: string
  redemptionId: string
  offerId: number
  locationId: number
}

export type VoidReviewDetail = VoidPassSummary & {
  requestId: string
  passId: string
  offerId: number
  locationId: number
  requestedByText: string
  requestedAtText: string
  reasonId: VoidRequestReasonId
  reasonText: string
  explanation: string | null
  correctionId: VoidRequestCorrectionId
  correctionText: string
}

export type VoidCreateRequestInput = {
  passId: string
  redemptionId: string
  offerId: number
  locationId: number
  reasonId: VoidRequestReasonId
  explanation: string | null
  correctionId: VoidRequestCorrectionId
  summary: VoidPassSummary
}

export type VoidCreateRequestResult =
  | { ok: true; requestId: string }
  | { ok: false; reason: "pending_exists" | "failed" }

export type VoidOutcomeResult = { ok: true } | { ok: false }

/** Pending void attention facts for Offers Needs attention overview (ticket 33). */
export type OpenVoidAttentionOffer = {
  offerId: number
  offerTitle: string
  pendingCount: number
}

export type VoidRequestAdapters = {
  createRequest: (
    input: VoidCreateRequestInput
  ) => Promise<VoidCreateRequestResult>
  approveRequest: (requestId: string) => Promise<VoidOutcomeResult>
  rejectRequest: (requestId: string) => Promise<VoidOutcomeResult>
  notifyApprovers: (requestId: string) => Promise<void>
  notifySubmitter: (
    requestId: string,
    outcome: "approved" | "rejected"
  ) => Promise<void>
  getRequest: (requestId: string) => Promise<VoidReviewDetail | null>
  /**
   * Open (pending) void requests grouped by catalog offer at a location.
   * Stub returns in-memory pending rows; live API when Void persistence ships.
   */
  listOpenVoidAttention: (
    locationId: number
  ) => Promise<OpenVoidAttentionOffer[]>
}

export type VoidCreateFormValues = {
  reasonId: VoidRequestReasonId | null
  explanation: string
  correctionId: VoidRequestCorrectionId | null
}

export type VoidCreateValidationResult =
  | { ok: true; explanation: string | null }
  | {
      ok: false
      errorKey: "reasonAndCorrectionRequired" | "explanationRequired"
    }

export function validateVoidCreateForm(
  values: VoidCreateFormValues
): VoidCreateValidationResult {
  if (values.reasonId == null || values.correctionId == null) {
    return { ok: false, errorKey: "reasonAndCorrectionRequired" }
  }
  const trimmed = values.explanation.trim()
  if (values.reasonId === "other" && trimmed.length === 0) {
    return { ok: false, errorKey: "explanationRequired" }
  }
  return {
    ok: true,
    explanation:
      values.reasonId === "other"
        ? trimmed
        : trimmed.length > 0
          ? trimmed
          : null,
  }
}

export function voidRequestFormErrorMessage(
  errorKey: keyof typeof VOID_REQUEST_COPY.errors
): string {
  return VOID_REQUEST_COPY.errors[errorKey]
}

type StubPendingRecord = {
  status: "pending" | "approved" | "rejected"
  detail: VoidReviewDetail
}

/**
 * Honest stub until void write / notify APIs ship.
 * Enforces one Pending request per pass in shared in-memory state.
 */
export function createStubVoidRequestAdapters(
  overrides?: Partial<VoidRequestAdapters>
): VoidRequestAdapters & {
  getRequestByPassId: (passId: string) => Promise<VoidReviewDetail | null>
} {
  const byRequestId = new Map<string, StubPendingRecord>()
  const pendingByPassId = new Map<string, string>()
  let seq = 0

  const base: VoidRequestAdapters & {
    getRequestByPassId: (passId: string) => Promise<VoidReviewDetail | null>
  } = {
    async createRequest(input) {
      if (pendingByPassId.has(input.passId)) {
        return { ok: false, reason: "pending_exists" }
      }
      seq += 1
      const requestId = `void-stub-${seq}`
      const detail: VoidReviewDetail = {
        requestId,
        passId: input.passId,
        offerId: input.offerId,
        locationId: input.locationId,
        requestedByText: "You",
        requestedAtText: "Just now",
        reasonId: input.reasonId,
        reasonText: VOID_REQUEST_COPY.reasons[input.reasonId],
        explanation: input.explanation,
        correctionId: input.correctionId,
        correctionText: VOID_REQUEST_COPY.corrections[input.correctionId].title,
        ...input.summary,
      }
      byRequestId.set(requestId, { status: "pending", detail })
      pendingByPassId.set(input.passId, requestId)
      return { ok: true, requestId }
    },
    async approveRequest(requestId) {
      const record = byRequestId.get(requestId)
      if (record == null || record.status !== "pending") {
        return { ok: false }
      }
      record.status = "approved"
      pendingByPassId.delete(record.detail.passId)
      return { ok: true }
    },
    async rejectRequest(requestId) {
      const record = byRequestId.get(requestId)
      if (record == null || record.status !== "pending") {
        return { ok: false }
      }
      record.status = "rejected"
      pendingByPassId.delete(record.detail.passId)
      return { ok: true }
    },
    async notifyApprovers(_requestId) {
      return
    },
    async notifySubmitter(_requestId, _outcome) {
      return
    },
    async getRequest(requestId) {
      return byRequestId.get(requestId)?.detail ?? null
    },
    async getRequestByPassId(passId) {
      const requestId = pendingByPassId.get(passId)
      if (requestId == null) {
        return null
      }
      return byRequestId.get(requestId)?.detail ?? null
    },
    async listOpenVoidAttention(locationId) {
      const byOffer = new Map<
        number,
        { offerTitle: string; pendingCount: number }
      >()
      for (const record of byRequestId.values()) {
        if (record.status !== "pending") {
          continue
        }
        if (record.detail.locationId !== locationId) {
          continue
        }
        const existing = byOffer.get(record.detail.offerId)
        if (existing != null) {
          existing.pendingCount += 1
        } else {
          byOffer.set(record.detail.offerId, {
            offerTitle: record.detail.offerTitle,
            pendingCount: 1,
          })
        }
      }
      return [...byOffer.entries()].map(([offerId, value]) => ({
        offerId,
        offerTitle: value.offerTitle,
        pendingCount: value.pendingCount,
      }))
    },
  }

  return { ...base, ...overrides }
}

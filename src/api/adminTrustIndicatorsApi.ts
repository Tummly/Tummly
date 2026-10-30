import { isAxiosError } from "axios"

import axiosInstance from "./axiosInstance"

export const TRUST_EVIDENCE_CLASSES = [
  "VerifiedPublicRecord",
  "VerifiedSelfAssessment",
  "Roadmap",
  "OwnControlQaPending",
] as const

export type TrustEvidenceClass = (typeof TRUST_EVIDENCE_CLASSES)[number]

/** Internal status (CONTRACT §1). Stored as text; the public status is derived. */
export const TRUST_STATUSES = [
  "VerifiedPublicRecord",
  "VerifiedCredential",
  "Roadmap",
  "QaPending",
  "QaPassed",
  "UnderReview",
] as const

export type TrustIndicatorStatus = (typeof TRUST_STATUSES)[number]

export const TRUST_CTA_TYPES = [
  "None",
  "ExternalProof",
  "InternalPage",
  "CookieSettings",
] as const

export type TrustCtaType = (typeof TRUST_CTA_TYPES)[number]

/** Editable draft fields. Dates are yyyy-MM-dd. Also the PUT body. */
export type TrustIndicatorValues = {
  enabled: boolean
  status: TrustIndicatorStatus
  statusLabel: string
  ctaType: TrustCtaType
  ctaLabel: string | null
  ctaTarget: string | null
  evidenceUrl: string | null
  imageUrl: string | null
  imageRightsNote: string | null
  iconKey: string | null
  altText: string
  tooltip: string
  issuer: string | null
  scope: string | null
  issuedOn: string | null
  expiresOn: string | null
  reviewOn: string | null
}

export type AdminTrustIndicatorItem = {
  itemNumber: number
  title: string
  evidenceClass: TrustEvidenceClass
  values: TrustIndicatorValues
  lastEditedByAdminUserId: number | null
  lastEditedAtUtc: string | null
}

export type AdminTrustIndicatorReview = {
  lastEditedByAdminUserId: number | null
  lastEditedAtUtc: string | null
  submittedByAdminUserId: number | null
  submittedAtUtc: string | null
  /** Set only while a submission stands. Publish must send this value back. */
  draftRevision: string | null
}

/** The viewing admin. `canApprove` is the publish permission. */
export type AdminTrustIndicatorViewer = {
  adminUserId: number
  canApprove: boolean
}

export type AdminTrustIndicatorPublication = {
  version: number
  publishedAtUtc: string
  publishedByAdminUserId: number
  approvedByAdminUserId: number
  submittedByAdminUserId: number | null
  submittedAtUtc: string | null
  reason: string
  items: AdminTrustIndicatorItem[]
}

export type AdminTrustIndicatorsView = {
  draft: AdminTrustIndicatorItem[]
  review: AdminTrustIndicatorReview
  published: AdminTrustIndicatorPublication | null
  viewer: AdminTrustIndicatorViewer
}

/** Carries the server code (for example self_approval_denied) and detail lines. */
export class TrustIndicatorApiError extends Error {
  readonly status: number | null
  readonly code: string | null
  readonly details: string[]

  constructor(
    message: string,
    status: number | null,
    code: string | null,
    details: string[]
  ) {
    super(message)
    this.name = "TrustIndicatorApiError"
    this.status = status
    this.code = code
    this.details = details
  }
}

type ErrorBody = {
  message?: string
  code?: string
  details?: unknown
}

function toApiError(error: unknown, fallback: string): TrustIndicatorApiError {
  if (!isAxiosError(error)) {
    return new TrustIndicatorApiError(fallback, null, null, [])
  }
  const body = (error.response?.data ?? {}) as ErrorBody
  const details = Array.isArray(body.details)
    ? body.details.filter((line): line is string => typeof line === "string")
    : []
  return new TrustIndicatorApiError(
    body.message ?? fallback,
    error.response?.status ?? null,
    body.code ?? null,
    details
  )
}

export async function fetchAdminTrustIndicators(): Promise<AdminTrustIndicatorsView> {
  try {
    const { data } = await axiosInstance.get<{
      success: boolean
      item: AdminTrustIndicatorsView
    }>("/admin/trust-indicators")
    return data.item
  } catch (error) {
    throw toApiError(error, "Could not load trust indicators.")
  }
}

export async function saveTrustIndicatorItem(
  itemNumber: number,
  values: TrustIndicatorValues
): Promise<AdminTrustIndicatorItem> {
  try {
    const { data } = await axiosInstance.put<{
      success: boolean
      item: AdminTrustIndicatorItem
    }>(`/admin/trust-indicators/items/${itemNumber}`, values)
    return data.item
  } catch (error) {
    throw toApiError(error, "Could not save the draft item.")
  }
}

export async function submitTrustIndicatorDraft(): Promise<AdminTrustIndicatorReview> {
  try {
    const { data } = await axiosInstance.post<{
      success: boolean
      item: AdminTrustIndicatorReview
    }>("/admin/trust-indicators/submit")
    return data.item
  } catch (error) {
    throw toApiError(error, "Could not submit the draft for review.")
  }
}

/**
 * Approve and publish. `draftRevision` is the review.draftRevision the approver
 * read. Pass one idempotency key per publish attempt; repeating the same key
 * returns the version it already created.
 */
export async function publishTrustIndicators(input: {
  reason: string
  draftRevision: string
  idempotencyKey: string
}): Promise<AdminTrustIndicatorPublication> {
  try {
    const { data } = await axiosInstance.post<{
      success: boolean
      item: AdminTrustIndicatorPublication
    }>(
      "/admin/trust-indicators/publish",
      { reason: input.reason, draftRevision: input.draftRevision },
      { headers: { "Idempotency-Key": input.idempotencyKey } }
    )
    return data.item
  } catch (error) {
    throw toApiError(error, "Could not publish the trust indicators.")
  }
}

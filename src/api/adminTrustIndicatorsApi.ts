import { isAxiosError } from "axios"

import axiosInstance from "./axiosInstance"

export const TRUST_EVIDENCE_CLASSES = [
  "VerifiedPublicRecord",
  "VerifiedSelfAssessment",
  "Roadmap",
  "OwnControlQaPending",
] as const

export type TrustEvidenceClass = (typeof TRUST_EVIDENCE_CLASSES)[number]

export const TRUST_CTA_TYPES = ["None", "ExternalProof", "InternalPage"] as const

export type TrustCtaType = (typeof TRUST_CTA_TYPES)[number]

/** Editable draft fields. Dates are yyyy-MM-dd. Also the PUT body. */
export type TrustIndicatorValues = {
  enabled: boolean
  ctaType: TrustCtaType
  ctaTarget: string | null
  evidenceUrl: string | null
  imageUrl: string | null
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
}

export type PublicTrustIndicator = {
  itemNumber: number
  title: string
  evidenceClass: TrustEvidenceClass
  iconKey: string | null
  imageUrl: string | null
  altText: string
  tooltip: string
  ctaType: TrustCtaType
  ctaTarget: string | null
  evidenceUrl: string | null
  issuer: string | null
  issuedOn: string | null
  expiresOn: string | null
  scope: string | null
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
 * Approve and publish. Pass one idempotency key per publish attempt;
 * repeating the same key returns the version it already created.
 */
export async function publishTrustIndicators(input: {
  reason: string
  idempotencyKey: string
}): Promise<AdminTrustIndicatorPublication> {
  try {
    const { data } = await axiosInstance.post<{
      success: boolean
      item: AdminTrustIndicatorPublication
    }>(
      "/admin/trust-indicators/publish",
      { reason: input.reason },
      { headers: { "Idempotency-Key": input.idempotencyKey } }
    )
    return data.item
  } catch (error) {
    throw toApiError(error, "Could not publish the trust indicators.")
  }
}

/** Public read for the website. Not wired into any component yet. */
export async function fetchPublicTrustIndicators(): Promise<PublicTrustIndicator[]> {
  const { data } = await axiosInstance.get<PublicTrustIndicator[]>(
    "/public/trust-indicators"
  )
  return data
}

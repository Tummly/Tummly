import type {
  AdminTrustIndicatorItem,
  TrustCtaType,
  TrustEvidenceClass,
  TrustIndicatorStatus,
  TrustIndicatorValues,
} from "@/api/adminTrustIndicatorsApi"

export const TRUST_EVIDENCE_CLASS_LABELS: Record<TrustEvidenceClass, string> = {
  VerifiedPublicRecord: "Verified public record",
  VerifiedSelfAssessment: "Verified self-assessment completion, not certification",
  Roadmap: "Roadmap / not awarded",
  OwnControlQaPending: "Own control",
}

export const TRUST_STATUS_LABELS: Record<TrustIndicatorStatus, string> = {
  VerifiedPublicRecord: "Verified public record",
  VerifiedCredential: "Verified credential",
  Roadmap: "Roadmap (not awarded)",
  QaPending: "QA pending",
  QaPassed: "QA passed",
  UnderReview: "Under review",
}

export const TRUST_CTA_TYPE_LABELS: Record<TrustCtaType, string> = {
  None: "No CTA",
  ExternalProof: "External proof link",
  InternalPage: "Internal page",
  CookieSettings: "Open cookie settings",
}

const ALLOWED_STATUSES: Record<TrustEvidenceClass, TrustIndicatorStatus[]> = {
  VerifiedPublicRecord: ["VerifiedPublicRecord", "UnderReview"],
  VerifiedSelfAssessment: ["VerifiedCredential", "UnderReview"],
  Roadmap: ["Roadmap"],
  OwnControlQaPending: ["QaPending", "QaPassed"],
}

/** Statuses the Status select may offer for an evidence class (CONTRACT §1). */
export function allowedTrustStatuses(
  evidenceClass: TrustEvidenceClass
): TrustIndicatorStatus[] {
  return [...ALLOWED_STATUSES[evidenceClass]]
}

/**
 * CTA types the form may offer (CONTRACT §3). `None` is always allowed. A proof
 * link needs a Verified-mapped status on a verified class. Internal and cookie
 * CTAs need own-control items that have passed QA.
 */
export function allowedTrustCtaTypes(
  evidenceClass: TrustEvidenceClass,
  status: TrustIndicatorStatus
): TrustCtaType[] {
  const types: TrustCtaType[] = ["None"]
  const verifiedStatus =
    status === "VerifiedPublicRecord" || status === "VerifiedCredential"
  const verifiedClass =
    evidenceClass === "VerifiedPublicRecord" ||
    evidenceClass === "VerifiedSelfAssessment"
  if (verifiedStatus && verifiedClass) {
    types.push("ExternalProof")
  }
  if (evidenceClass === "OwnControlQaPending" && status === "QaPassed") {
    types.push("InternalPage", "CookieSettings")
  }
  return types
}

export type TrustPublishErrorMessage = {
  message: string
  /** True when the draft changed under the approver and the view must be reloaded. */
  reload: boolean
}

const DRAFT_CHANGED_MESSAGE =
  "The draft changed. It has been reloaded — review it again."

/** Maps a publish or edit refusal to the admin-facing text (CONTRACT §6, §7). */
export function trustPublishErrorMessage(
  code: string,
  message: string
): TrustPublishErrorMessage {
  const serverMessage = message.trim()
  switch (code) {
    case "approver_required":
      return {
        message: "Only a Director or compliance approver can publish.",
        reload: false,
      }
    case "self_approval_denied":
      return {
        message: orFallback(
          serverMessage,
          "You cannot publish a draft you submitted or edited. A second admin must publish it."
        ),
        reload: false,
      }
    case "draft_changed":
    case "draft_conflict":
    case "publish_conflict":
      return { message: DRAFT_CHANGED_MESSAGE, reload: true }
    case "idempotency_key_reused":
      return {
        message: orFallback(
          serverMessage,
          "This publish request was already used. Review the draft and publish again."
        ),
        reload: false,
      }
    case "not_submitted":
      return {
        message: "Submit the draft for review before publishing.",
        reload: false,
      }
    case "publish_gate_failed":
      return {
        message:
          "Some items cannot be published yet. Fix them, submit again, then publish.",
        reload: false,
      }
    default:
      return {
        message: orFallback(serverMessage, "Could not publish the trust indicators."),
        reload: false,
      }
  }
}

function orFallback(value: string, fallback: string): string {
  return value === "" ? fallback : value
}

const VALUE_FIELD_LABELS: Record<keyof TrustIndicatorValues, string> = {
  enabled: "Shown on website",
  status: "Status",
  statusLabel: "Status label",
  ctaType: "CTA type",
  ctaLabel: "CTA label",
  ctaTarget: "CTA target",
  evidenceUrl: "Evidence URL",
  imageUrl: "Image URL",
  imageRightsNote: "Image usage rights",
  iconKey: "Icon key",
  altText: "Alt text",
  tooltip: "Tooltip",
  issuer: "Issuer",
  scope: "Scope",
  issuedOn: "Issued on",
  expiresOn: "Expires on",
  reviewOn: "Review on",
}

const VALUE_FIELDS = Object.keys(VALUE_FIELD_LABELS) as Array<
  keyof TrustIndicatorValues
>

export type TrustValueField = keyof TrustIndicatorValues

export type TrustFieldDifference = {
  field: TrustValueField
  label: string
  published: string
  draft: string
}

export type TrustItemPublishState = "not_published" | "unchanged" | "changed"

export function describeTrustValue(
  field: TrustValueField,
  value: string | boolean | null | undefined
): string {
  if (field === "enabled") {
    return value === true ? "On" : "Off"
  }
  if (value === null || value === undefined || value === "") {
    return "—"
  }
  if (field === "ctaType" && typeof value === "string") {
    return TRUST_CTA_TYPE_LABELS[value as TrustCtaType] ?? value
  }
  if (field === "status" && typeof value === "string") {
    return TRUST_STATUS_LABELS[value as TrustIndicatorStatus] ?? value
  }
  return String(value)
}

export function diffTrustIndicatorValues(
  draft: TrustIndicatorValues,
  published: TrustIndicatorValues
): TrustFieldDifference[] {
  return VALUE_FIELDS.flatMap((field) => {
    const draftValue = normalizeForCompare(draft[field])
    const publishedValue = normalizeForCompare(published[field])
    if (draftValue === publishedValue) {
      return []
    }
    return [
      {
        field,
        label: VALUE_FIELD_LABELS[field],
        published: describeTrustValue(field, published[field]),
        draft: describeTrustValue(field, draft[field]),
      },
    ]
  })
}

export function trustItemPublishState(
  draft: AdminTrustIndicatorItem,
  published: AdminTrustIndicatorItem | undefined
): TrustItemPublishState {
  if (published === undefined) {
    return "not_published"
  }
  return diffTrustIndicatorValues(draft.values, published.values).length > 0
    ? "changed"
    : "unchanged"
}

/** Draft items whose values differ from the live version, with the field-level changes. */
export function trustDraftChanges(
  draft: AdminTrustIndicatorItem[],
  published: AdminTrustIndicatorItem[] | null
): Array<{ item: AdminTrustIndicatorItem; differences: TrustFieldDifference[] }> {
  return draft.flatMap((item) => {
    const live = published?.find((row) => row.itemNumber === item.itemNumber)
    if (live === undefined) {
      return []
    }
    const differences = diffTrustIndicatorValues(item.values, live.values)
    return differences.length > 0 ? [{ item, differences }] : []
  })
}

/** Turns blank form strings into null before save. Alt text and tooltip stay strings. */
export function toTrustSaveValues(form: TrustIndicatorValues): TrustIndicatorValues {
  return {
    ...form,
    statusLabel: form.statusLabel.trim(),
    ctaLabel: blankToNull(form.ctaLabel),
    ctaTarget: blankToNull(form.ctaTarget),
    evidenceUrl: blankToNull(form.evidenceUrl),
    imageUrl: blankToNull(form.imageUrl),
    imageRightsNote: blankToNull(form.imageRightsNote),
    iconKey: blankToNull(form.iconKey),
    issuer: blankToNull(form.issuer),
    scope: blankToNull(form.scope),
    issuedOn: blankToNull(form.issuedOn),
    expiresOn: blankToNull(form.expiresOn),
    reviewOn: blankToNull(form.reviewOn),
    altText: form.altText.trim(),
    tooltip: form.tooltip.trim(),
  }
}

function blankToNull(value: string | null): string | null {
  return value === null || value.trim() === "" ? null : value.trim()
}

function normalizeForCompare(value: string | boolean | null): string {
  if (typeof value === "boolean") {
    return value ? "on" : "off"
  }
  return value ?? ""
}

import type {
  AdminTrustIndicatorItem,
  TrustEvidenceClass,
  TrustIndicatorValues,
} from "@/api/adminTrustIndicatorsApi"

export const TRUST_EVIDENCE_CLASS_LABELS: Record<TrustEvidenceClass, string> = {
  VerifiedPublicRecord: "Verified public record",
  VerifiedSelfAssessment: "Verified self-assessment completion, not certification",
  Roadmap: "Roadmap / not awarded",
  OwnControlQaPending: "Own-control QA pending",
}

export const TRUST_CTA_TYPE_LABELS = {
  None: "No CTA",
  ExternalProof: "External proof link",
  InternalPage: "Internal page",
} as const

const VALUE_FIELD_LABELS: Record<keyof TrustIndicatorValues, string> = {
  enabled: "Shown on website",
  ctaType: "CTA type",
  ctaTarget: "CTA target",
  evidenceUrl: "Evidence URL",
  imageUrl: "Image URL",
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
    return TRUST_CTA_TYPE_LABELS[value as keyof typeof TRUST_CTA_TYPE_LABELS] ?? value
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
    ctaTarget: blankToNull(form.ctaTarget),
    evidenceUrl: blankToNull(form.evidenceUrl),
    imageUrl: blankToNull(form.imageUrl),
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

/** Wire value from the Locations API. The operator is never told why. */
export type PremisesReviewWire = "pending_review" | "rejected"

export const PREMISES_REVIEW_PENDING_LABEL = "Saved as Draft for review"

export const PREMISES_REVIEW_REJECTED_LABEL = "Review not approved"

export function normalizePremisesReviewWire(
  value: unknown
): PremisesReviewWire | null {
  return value === "pending_review" || value === "rejected" ? value : null
}

export function premisesReviewLabel(
  status: PremisesReviewWire | null | undefined
): string | null {
  if (status === "pending_review") return PREMISES_REVIEW_PENDING_LABEL
  if (status === "rejected") return PREMISES_REVIEW_REJECTED_LABEL
  return null
}

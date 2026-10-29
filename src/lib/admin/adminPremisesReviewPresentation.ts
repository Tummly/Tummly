import type {
  AdminPremisesReviewAction,
  AdminPremisesReviewMatch,
} from "@/types/adminPremisesReview"

export const PREMISES_REVIEW_REASON_MAX_LENGTH = 500

export const PREMISES_REVIEW_ACTION_LABELS: Record<
  AdminPremisesReviewAction,
  string
> = {
  clear: "Clear",
  reject: "Reject",
}

export const PREMISES_REVIEW_ACTION_DONE: Record<
  AdminPremisesReviewAction,
  string
> = {
  clear: "Premises cleared",
  reject: "Premises rejected",
}

/** A reason is required to clear or reject. Blank or over-long is not allowed. */
export function canSubmitPremisesReviewReason(reason: string): boolean {
  const trimmed = reason.trim()
  return trimmed.length > 0 && trimmed.length <= PREMISES_REVIEW_REASON_MAX_LENGTH
}

/** Short reason tags for one matched Location, in a fixed order. */
export function describePremisesMatch(
  match: AdminPremisesReviewMatch
): string[] {
  const tags: string[] = []
  tags.push(match.sameWorkspace ? "Same Workspace" : "Other Workspace")
  if (match.premisesKeyMatch) tags.push("Same premises")
  if (match.fsaMatch) tags.push("Same FSA id")
  if (match.priorPilot) tags.push("Prior Pilot")
  if (match.priorStarterKit) tags.push("Prior Starter Kit")
  return tags
}

export function formatMatchAddress(match: AdminPremisesReviewMatch): string {
  const parts = [match.address, match.city, match.postcode]
    .map((part) => part?.trim())
    .filter((part): part is string => Boolean(part))
  return parts.length > 0 ? parts.join(", ") : "Address no longer on file"
}

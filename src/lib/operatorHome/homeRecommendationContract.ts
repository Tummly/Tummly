/** Home Recommended next step allow-list + server cache contract (ticket 01). */

export const HOME_RECOMMENDATION_TYPES = [
  "review-open-feedback",
  "thank-or-follow-guest",
  "promote-or-fix-offer",
  "thank-recent-guests",
  "re-engage",
  "recovery-follow-up",
  "none",
] as const

export type HomeRecommendationType = (typeof HOME_RECOMMENDATION_TYPES)[number]

/** Home-native types completed by Azure copy (not Campaigns service). */
export type HomeRecommendationNativeType =
  | "review-open-feedback"
  | "thank-or-follow-guest"
  | "promote-or-fix-offer"

/** Campaign types completed via Campaigns recommendation service. */
export type HomeRecommendationCampaignType =
  | "thank-recent-guests"
  | "re-engage"
  | "recovery-follow-up"

/** Matches Campaigns recommendation server cache TTL. */
export const HOME_RECOMMENDATION_CACHE_TTL_MINUTES = 30

const ALLOWED = new Set<string>(HOME_RECOMMENDATION_TYPES)

export function isHomeRecommendationType(
  type: string
): type is HomeRecommendationType {
  return ALLOWED.has(type)
}

/**
 * Stable server cache key: operator + location + date window.
 * Named presets ignore exact from/to (UI recomputes those every call).
 * Custom uses UTC day granularity so clock drift does not bust the key.
 */
export function buildHomeRecommendationCacheKey(input: {
  operatorUserId: number
  locationId: number
  overviewDatePreset: string
  from?: Date | null
  to?: Date | null
}): string {
  const preset = input.overviewDatePreset.trim().toLowerCase()
  if (
    preset === "custom"
    && input.from != null
    && input.to != null
  ) {
    const fromDay = input.from.toISOString().slice(0, 10)
    const toDay = input.to.toISOString().slice(0, 10)
    return `home-recommendation:${input.operatorUserId}:${input.locationId}:custom:${fromDay}:${toDay}`
  }

  return `home-recommendation:${input.operatorUserId}:${input.locationId}:${preset}`
}

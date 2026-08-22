import type { HomePerformancePresetId } from "@/lib/operatorHome/homePerformanceDateRange"

/** Home Recommended next step allow-list + server cache contract (ticket 01). */

export const HOME_RECOMMENDATION_NATIVE_TYPES = [
  "review-open-feedback",
  "thank-or-follow-guest",
  "promote-or-fix-offer",
] as const

export const HOME_RECOMMENDATION_CAMPAIGN_TYPES = [
  "thank-recent-guests",
  "re-engage",
  "recovery-follow-up",
] as const

export const HOME_RECOMMENDATION_TYPES = [
  ...HOME_RECOMMENDATION_NATIVE_TYPES,
  ...HOME_RECOMMENDATION_CAMPAIGN_TYPES,
  "none",
] as const

export type HomeRecommendationType = (typeof HOME_RECOMMENDATION_TYPES)[number]

/** Home-native types completed by Azure copy (not Campaigns service). */
export type HomeRecommendationNativeType =
  (typeof HOME_RECOMMENDATION_NATIVE_TYPES)[number]

/** Campaign types completed via Campaigns recommendation service. */
export type HomeRecommendationCampaignType =
  (typeof HOME_RECOMMENDATION_CAMPAIGN_TYPES)[number]

/** Home performance presets plus `custom` — closed wire set for the request. */
export const HOME_RECOMMENDATION_OVERVIEW_DATE_PRESETS = [
  "last7",
  "last30",
  "thisMonth",
  "custom",
] as const satisfies ReadonlyArray<HomePerformancePresetId | "custom">

export type HomeRecommendationOverviewDatePreset =
  (typeof HOME_RECOMMENDATION_OVERVIEW_DATE_PRESETS)[number]

/** Domain primary CTA kinds for Home-native types. */
export const HOME_RECOMMENDATION_DOMAIN_ACTION_KINDS = [
  "open-feedback",
  "open-guest",
  "open-offer",
] as const

export type HomeRecommendationDomainActionKind =
  (typeof HOME_RECOMMENDATION_DOMAIN_ACTION_KINDS)[number]

/** Matches Campaigns recommendation server cache TTL. */
export const HOME_RECOMMENDATION_CACHE_TTL_MINUTES = 30

const ALLOWED_TYPES = new Set<string>(HOME_RECOMMENDATION_TYPES)
const ALLOWED_PRESETS = new Set<string>(HOME_RECOMMENDATION_OVERVIEW_DATE_PRESETS)
const ALLOWED_ACTION_KINDS = new Set<string>(
  HOME_RECOMMENDATION_DOMAIN_ACTION_KINDS
)

export function isHomeRecommendationType(
  type: string
): type is HomeRecommendationType {
  return ALLOWED_TYPES.has(type)
}

export function isHomeRecommendationOverviewDatePreset(
  preset: string
): preset is HomeRecommendationOverviewDatePreset {
  return ALLOWED_PRESETS.has(preset)
}

export function isHomeRecommendationDomainActionKind(
  kind: string
): kind is HomeRecommendationDomainActionKind {
  return ALLOWED_ACTION_KINDS.has(kind)
}

/**
 * Stable server cache key: operator + location + date window.
 * Named presets ignore exact from/to (UI recomputes those every call).
 * Custom uses UTC day granularity so clock drift does not bust the key.
 */
export function buildHomeRecommendationCacheKey(input: {
  operatorUserId: number
  locationId: number
  overviewDatePreset: HomeRecommendationOverviewDatePreset | string
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

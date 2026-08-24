/** Offer recommendation allow-list + client request (ticket 02). */

export const OFFER_RECOMMENDATION_TYPES = [
  "promote-this-offer",
  "fix-this-offer",
  "none",
] as const

export type OfferRecommendationType =
  (typeof OFFER_RECOMMENDATION_TYPES)[number]

export const OFFER_RECOMMENDATION_CACHE_TTL_MINUTES = 30

export type OfferRecommendationDraftPrefill = {
  offerId: number
  offerStance: "existing-offer"
  goalId: "promote-something-new"
  audienceKey: "all-eligible-guests"
  channel: "email" | "sms"
  campaignName: string
  messageSubject: string | null
  messageBody: string | null
}

export type OfferRecommendation = {
  type: OfferRecommendationType
  title?: string | null
  opportunity?: string | null
  whyBullets?: string[] | null
  suggestedChannel?: "email" | "sms" | null
  draftPrefill?: OfferRecommendationDraftPrefill | null
  locationName?: string | null
}

export type OfferRecommendationRequest = {
  locationId: number
  refresh?: boolean
}

export type OfferRecommendationResponse = {
  success: boolean
  recommendation?: OfferRecommendation
  message?: string
  retryable?: boolean
}

const ALLOWED_TYPES = new Set<string>(OFFER_RECOMMENDATION_TYPES)

export function isOfferRecommendationType(
  type: string
): type is OfferRecommendationType {
  return ALLOWED_TYPES.has(type)
}

export function buildOfferRecommendationRequest(input: {
  locationId: number
  refresh?: boolean
}): OfferRecommendationRequest {
  return {
    locationId: input.locationId,
    refresh: input.refresh === true,
  }
}

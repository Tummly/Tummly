export const UNSUBSCRIBE_SUCCESS_MESSAGE =
  "You're unsubscribed from marketing emails for this restaurant."

export const UNSUBSCRIBE_MISSING_RESTAURANT_MESSAGE =
  "Use the Unsubscribe link in your email, or ask the restaurant for their unsubscribe page."

export const UNSUBSCRIBE_INVALID_LINK_MESSAGE =
  "This unsubscribe link is invalid or has expired."

export const UNSUBSCRIBE_PREVIEW_NETWORK_ERROR_MESSAGE =
  "We couldn't load this unsubscribe link. Please try again."

export type UnsubscribePageMode =
  | { kind: "token"; token: string }
  | { kind: "form"; restaurantId: number }
  | { kind: "missing-restaurant" }

export function resolveUnsubscribePageMode(
  searchParams: URLSearchParams
): UnsubscribePageMode {
  const token = searchParams.get("t")?.trim() ?? ""
  if (token.length > 0) {
    return { kind: "token", token }
  }

  const restaurantIdRaw = searchParams.get("restaurantId")?.trim() ?? ""
  if (restaurantIdRaw.length > 0) {
    const restaurantId = Number(restaurantIdRaw)
    if (Number.isInteger(restaurantId) && restaurantId > 0) {
      return { kind: "form", restaurantId }
    }
  }

  return { kind: "missing-restaurant" }
}

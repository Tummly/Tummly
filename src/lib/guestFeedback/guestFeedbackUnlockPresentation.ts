/** Guest thank-you display helpers — shared privately body for success screen. */

export function displayGuestFeedbackRestaurant(name: string): string {
  return name.trim() || "this restaurant"
}

export function displayGuestFeedbackLocation(name: string): string {
  return name.trim() || "this location"
}

/** Shared thank-you body — Figma Guest-Loop-MVP 6807:1186. */
export function buildGuestFeedbackSharedPrivatelyBody(
  restaurantName: string,
  locationName: string
): string {
  const restaurant = displayGuestFeedbackRestaurant(restaurantName)
  const location = displayGuestFeedbackLocation(locationName)
  return `Your feedback has been shared privately with the team at ${restaurant} — ${location}.`
}

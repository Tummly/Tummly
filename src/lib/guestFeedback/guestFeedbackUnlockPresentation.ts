/** Post-submit unlock-offer copy (Figma Guest-Loop-MVP 6723:1094). */

export type GuestFeedbackUnlockChannel = "email" | "sms"

export type GuestFeedbackUnlockCopyInput = {
  restaurantName: string
  locationName: string
  offerTitle: string
  channel: GuestFeedbackUnlockChannel
}

export type GuestFeedbackUnlockCopy = {
  thankYouHeading: string
  sharedBody: string
  wantHeading: string
  joinBody: string
  unlockCta: string
  declineCta: string
  optOutNote: string
}

export function displayGuestFeedbackRestaurant(name: string): string {
  return name.trim() || "this restaurant"
}

export function displayGuestFeedbackLocation(name: string): string {
  return name.trim() || "this location"
}

function displayOfferTitle(title: string): string {
  return title.trim() || "this offer"
}

/** Shared thank-you body — Figma Guest-Loop-MVP 6778:28257 / unlock 6723:1094. */
export function buildGuestFeedbackSharedPrivatelyBody(
  restaurantName: string,
  locationName: string
): string {
  const restaurant = displayGuestFeedbackRestaurant(restaurantName)
  const location = displayGuestFeedbackLocation(locationName)
  return `Your feedback has been shared privately with the team at ${restaurant} — ${location}.`
}

/** Figma unlock screen strings with restaurant / location / offer filled in. */
export function buildGuestFeedbackUnlockCopy(
  input: GuestFeedbackUnlockCopyInput
): GuestFeedbackUnlockCopy {
  const restaurant = displayGuestFeedbackRestaurant(input.restaurantName)
  const offer = displayOfferTitle(input.offerTitle)

  return {
    thankYouHeading: "Thank you.",
    sharedBody: buildGuestFeedbackSharedPrivatelyBody(
      input.restaurantName,
      input.locationName
    ),
    wantHeading: `Want ${offer}?`,
    joinBody: `Join ${restaurant} for occasional offers and updates to unlock your welcome offer.`,
    unlockCta:
      input.channel === "email"
        ? `Yes — email me & unlock ${offer}`
        : `Yes — text me & unlock ${offer}`,
    declineCta: "No thanks",
    optOutNote:
      input.channel === "email"
        ? "You can unsubscribe at any time."
        : "You can opt out at any time.",
  }
}

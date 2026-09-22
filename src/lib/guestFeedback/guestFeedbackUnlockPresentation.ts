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

function displayRestaurant(name: string): string {
  return name.trim() || "this restaurant"
}

function displayLocation(name: string): string {
  return name.trim() || "this location"
}

function displayOfferTitle(title: string): string {
  return title.trim() || "this offer"
}

/** Figma unlock screen strings with restaurant / location / offer filled in. */
export function buildGuestFeedbackUnlockCopy(
  input: GuestFeedbackUnlockCopyInput
): GuestFeedbackUnlockCopy {
  const restaurant = displayRestaurant(input.restaurantName)
  const location = displayLocation(input.locationName)
  const offer = displayOfferTitle(input.offerTitle)

  return {
    thankYouHeading: "Thank you.",
    sharedBody: `Your feedback has been shared privately with the team at ${restaurant} — ${location}.`,
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

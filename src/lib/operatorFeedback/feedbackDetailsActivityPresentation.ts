/**
 * Feedback details activity timeline labels (ticket 06 Offer issue chrome).
 */

export function recoveryOfferIssuedActivityLabel(input: {
  offerTitle?: string | null
  redemptionCode?: string | null
}): string {
  const title = input.offerTitle?.trim() ?? ""
  const claimCode = input.redemptionCode?.trim() ?? ""

  if (title !== "" && claimCode !== "") {
    return `Recovery offer issued · ${title} · ${claimCode}`
  }
  if (claimCode !== "") {
    return `Recovery offer issued · ${claimCode}`
  }
  if (title !== "") {
    return `Recovery offer issued · ${title}`
  }
  return "Recovery offer issued"
}

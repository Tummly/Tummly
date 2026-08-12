/**
 * Feedback details activity timeline labels (ticket 06 Offer issue chrome).
 */

export function recoveryOfferIssuedActivityLabel(input: {
  offerTitle?: string | null
  redemptionCode?: string | null
  redemptionStatus?: "not_redeemed" | "redeemed" | null
}): string {
  const title = input.offerTitle?.trim() ?? ""
  const claimCode = input.redemptionCode?.trim() ?? ""
  const statusLabel =
    input.redemptionStatus === "redeemed"
      ? "Redeemed"
      : input.redemptionStatus === "not_redeemed"
        ? "Not redeemed"
        : null

  const parts = ["Recovery offer issued"]
  if (title !== "") {
    parts.push(title)
  }
  if (claimCode !== "") {
    parts.push(claimCode)
  }
  if (statusLabel != null) {
    parts.push(statusLabel)
  }
  return parts.join(" · ")
}

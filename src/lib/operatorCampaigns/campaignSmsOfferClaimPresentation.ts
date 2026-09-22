import { GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER } from "@/lib/operatorFeedback/guestPreviewPresentation"

/**
 * Campaign SMS is one-way — Offer claim QR cannot ship. Claim code text must
 * live in the SMS body for preview, AI draft, credit estimate, and send.
 */

export type CampaignSmsOfferClaimFields = {
  title: string
  redemptionCode: string
  expiryLabel: string
}

const CLAIM_CODE_LINE_PREFIX = "Claim code: "

/** Formats the Offer claim footer appended to Campaign SMS bodies. */
export function formatCampaignSmsOfferClaimBlock(
  offer: CampaignSmsOfferClaimFields
): string {
  const title = offer.title.trim()
  const code = offer.redemptionCode.trim()
  const expiry = offer.expiryLabel.trim()
  if (title === "" || code === "") {
    return ""
  }
  const expiryLine = expiry === "" ? "Expires: —" : expiry
  return `\n\n${title}\n${CLAIM_CODE_LINE_PREFIX}${code}\n${expiryLine}`
}

/**
 * Removes a prior Claim-code footer (sample or live) so re-attach / AI rewrite
 * does not stack duplicate offer blocks.
 */
export function stripCampaignSmsOfferClaimFooter(body: string): string {
  const marker = `\n${CLAIM_CODE_LINE_PREFIX}`
  const markerIndex = body.lastIndexOf(marker)
  if (markerIndex < 0) {
    return body
  }
  const blockStart = body.lastIndexOf("\n\n", markerIndex)
  const without =
    blockStart >= 0 ? body.slice(0, blockStart) : body.slice(0, markerIndex)
  return without.replace(/\s+$/u, "")
}

/** Ensures the SMS body ends with one Offer claim footer for the given offer. */
export function ensureCampaignSmsOfferClaimInBody(
  body: string,
  offer: CampaignSmsOfferClaimFields | null | undefined
): string {
  const base = stripCampaignSmsOfferClaimFooter(body)
  if (offer == null) {
    return base
  }
  const block = formatCampaignSmsOfferClaimBlock(offer)
  if (block === "") {
    return base
  }
  return `${base}${block}`
}

/** Preview / AI draft use the sample Claim code until fire issues a real one. */
export function campaignSmsOfferClaimPreviewFields(input: {
  title: string
  expiryLabel: string
}): CampaignSmsOfferClaimFields {
  return {
    title: input.title,
    redemptionCode: GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
    expiryLabel: input.expiryLabel,
  }
}

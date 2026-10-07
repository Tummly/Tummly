/** Campaign Detail / Campaign Preview drawer — Figma 5116:19403 (Campaign adapter). */

import { formatOfferDetailsOfferValue } from "@/lib/operatorOffers/offerDetailsPresentation"
import { formatOfferValidityLabel } from "@/lib/operatorOffers/offerListPresentation"
import type { CatalogOfferDetail } from "@/types/operatorCampaigns"

export const CAMPAIGN_DETAIL_PREVIEW_COPY = {
  subtitle:
    "See the audience, channel, offer, message and send logic for this campaign.",
  campaignSummary: "Campaign summary",
  goal: "Goal:",
  audience: "Audience:",
  channel: "Channel:",
  offer: "Offer:",
  guestMessage: "Guest-facing message",
  emailTab: "Email",
  smsTab: "SMS",
  /** Figma 5116:19586 */
  offerLogic: "Offer logic preview",
  offerType: "Offer type:",
  codeType: "Code type:",
  expiry: "Expiry:",
  usage: "Usage:",
  redemption: "Redemption:",
  codeTypeValue: "Unique guest code",
  usageValue: "Single-use",
  redemptionValue: "Staff verifies in Tummly redeem screen",
  /** Figma 5116:19438 */
  audienceEligibility: "Audience eligibility",
  emailEligible: "Email",
  smsEligible: "SMS",
  totalUniqueGuests: "Total unique guests",
  sendLogic: "Send logic",
  notScheduled: "Not scheduled yet",
  emptyValue: "—",
  footerDisclaimer:
    "This is a summary of the selected campaign. It is not a performance report.",
  editCampaign: "Edit campaign",
  close: "Close",
  closeAriaLabel: "Close campaign preview",
  loadError: "Could not load this campaign preview. Please try again.",
  retry: "Retry",
  preview: "Preview",
} as const

export type CampaignDetailOfferLogicRow = {
  label: string
  value: string
}

/** Offer logic preview rows from an attached catalog Offer (Figma 5116:19586). */
export function buildCampaignDetailOfferLogicRows(
  offer: CatalogOfferDetail
): CampaignDetailOfferLogicRow[] {
  const copy = CAMPAIGN_DETAIL_PREVIEW_COPY
  return [
    {
      label: copy.offerType,
      value: formatOfferDetailsOfferValue(offer),
    },
    {
      label: copy.codeType,
      value: copy.codeTypeValue,
    },
    {
      label: copy.expiry,
      value: formatOfferValidityLabel(offer.validity, offer.expiryDate),
    },
    {
      label: copy.usage,
      value: copy.usageValue,
    },
    {
      label: copy.redemption,
      value: copy.redemptionValue,
    },
  ]
}

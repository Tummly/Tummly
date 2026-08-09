/**
 * Campaign wizard Offer step — Figma 4730:53493 / tickets 25 + 22.
 * No offer + Create and select offer (live). Existing offer visible but disabled.
 */

export type CampaignOfferStanceId =
  | "no-offer"
  | "existing-offer"
  | "create-new-offer"

export type CampaignOfferOptionDef = {
  id: CampaignOfferStanceId
  title: string
  description: string
  /** Existing offer browse stays deferred — card visible but not selectable. */
  disabled: boolean
}

export const CAMPAIGN_OFFER_COPY = {
  stepHeading: "Would you like to include an offer?",
  stepDescription:
    "Choose an existing offer, create a controlled offer or continue without one.",
  usageTitle: "Estimated message usage",
  createPanelTitle: "Create and select offer",
  createPanelConfirm: "Create and select offer",
  attachedSummaryEdit: "Edit",
  createOfferError: "Could not create this offer. Try again.",
} as const

export const CAMPAIGN_OFFER_OPTIONS: readonly CampaignOfferOptionDef[] = [
  {
    id: "no-offer",
    title: "No offer",
    description: "Send this campaign without a discount or reward.",
    disabled: false,
  },
  {
    id: "existing-offer",
    title: "Existing offer",
    description: "Browse existing offers coming later.",
    disabled: true,
  },
  {
    id: "create-new-offer",
    title: "Create and select offer",
    description:
      "Define the benefit, validity and redemption rules before adding the offer to this campaign.",
    disabled: false,
  },
] as const

const DEFAULT_OFFER_STANCE_ID: CampaignOfferStanceId = "no-offer"

export function defaultCampaignOfferStanceId(): CampaignOfferStanceId {
  return DEFAULT_OFFER_STANCE_ID
}

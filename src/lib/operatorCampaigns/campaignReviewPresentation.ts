/**
 * Campaign wizard Review step — Figma 4752:67706 / ticket 27.
 * Summary chrome only. No send / approve / schedule-commit CTA.
 */

export type CampaignReviewSectionId =
  | "campaign"
  | "audience"
  | "channel"
  | "message"
  | "offer"
  | "schedule"
  | "usage"

export type CampaignReviewSectionDef = {
  id: CampaignReviewSectionId
  title: string
}

export const CAMPAIGN_REVIEW_COPY = {
  stepHeading: "Review campaign",
  stepDescription:
    "Check the audience, message, offer, timing and estimated usage.",
  /** Figma chrome label — primary action stays disabled (no send path). */
  primaryActionLabel: "Send campaign now",
  emptyValue: "—",
  goalLabel: "Goal",
  locationLabel: "Location",
  audienceLabel: "Audience",
  channelLabel: "Channel",
  senderLabel: "Sender",
  subjectLabel: "Subject",
  messageLabel: "Message",
  offerLabel: "Offer",
  scheduleLabel: "Timing",
} as const

export const CAMPAIGN_REVIEW_SECTIONS: readonly CampaignReviewSectionDef[] = [
  { id: "campaign", title: "Campaign" },
  { id: "audience", title: "Audience" },
  { id: "channel", title: "Channel and sender" },
  { id: "message", title: "Message" },
  { id: "offer", title: "Offer" },
  { id: "schedule", title: "Schedule" },
  { id: "usage", title: "Usage" },
] as const

/**
 * Figma Email preview `4752:67897` — 638×562.
 * Cap the rail so it does not grow with leftover row space.
 */
export const CAMPAIGN_REVIEW_GUEST_PREVIEW_RAIL_CLASS =
  "w-full lg:w-[638px] lg:max-w-[638px] lg:shrink-0"

import type { GuestMarketingStatusLabel } from "@/types/operatorGuests"

export type OperatorGuestProfileTabId =
  | "overview"
  | "feedbacks"
  | "offers"
  | "campaigns"
  | "activity"
  | "notes"

export type OperatorGuestProfileEligibilityStatus =
  | "eligible"
  | "unsubscribed"
  | "not_provided"

export type OperatorGuestProfileViewModel = {
  id: string
  locationId: number
  name: string
  marketingStatusLabel: GuestMarketingStatusLabel
  guestSinceDisplay: string
  lastActivityDisplay: string | null
  identitySubtitle: string
  lastInteractionLabel: string
  profileSummary: {
    emailDisplay: string
    mobileDisplay: string
    firstCapturedDisplay: string
    locationName: string
    feedbackSubmissionCount: number
    offerClaimsAndRedemptions: number
    lastInteractionDisplay: string
    lastInteractionLabel: string
    guestTagsDisplay: string
  }
  overviewDetails: {
    guestSinceDisplay: string
    totalInteractions: number
    feedbackReceived: number
    offersClaimed: number
    campaignsSent: number
    lastActivityDisplay: string
  }
  contactEligibility: Array<{
    channel: "email" | "sms"
    channelLabel: "Email" | "SMS"
    status: OperatorGuestProfileEligibilityStatus
    statusLabel: string
  }>
}

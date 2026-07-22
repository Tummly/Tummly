import {
  GUEST_PROFILE_CONTACT_STATUS_LABELS,
  GUEST_PROFILE_NOT_PROVIDED,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  formatRelativeTime,
  parseApiInstantMs,
} from "@/lib/operatorHome/relativeTime"
import type { GuestProfileResponse } from "@/types/dashboard"
import type { GuestMarketingStatusLabel } from "@/types/operatorGuests"
import type { OperatorGuestProfileViewModel } from "@/types/operatorGuestProfile"

export type MapGuestProfileApiResponseInput = {
  response: GuestProfileResponse
  nowMs?: number
}

function formatGuestProfileDate(iso: string): string {
  const ms = parseApiInstantMs(iso)
  if (Number.isNaN(ms)) {
    return ""
  }

  return new Date(ms).toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  })
}

function formatOptionalRelative(
  iso: string | null,
  nowMs: number
): string | null {
  if (iso == null) {
    return null
  }

  const label = formatRelativeTime(iso, nowMs)
  return label === "" ? null : label
}

function displayOrNotProvided(value: string | null | undefined): string {
  if (value == null || value.trim() === "") {
    return GUEST_PROFILE_NOT_PROVIDED
  }
  return value
}

function buildIdentitySubtitle(
  guestSinceDisplay: string,
  lastActivityDisplay: string | null
): string {
  if (lastActivityDisplay == null) {
    return `Guest since ${guestSinceDisplay}`
  }

  return `Guest since ${guestSinceDisplay} · Last activity ${lastActivityDisplay}`
}

export function mapGuestProfileApiResponseToViewModel(
  input: MapGuestProfileApiResponseInput
): OperatorGuestProfileViewModel {
  const { response } = input
  const nowMs = input.nowMs ?? Date.now()

  const guestSinceDisplay = formatGuestProfileDate(response.guestSinceAt)
  const lastActivityDisplay = formatOptionalRelative(
    response.lastActivityAt,
    nowMs
  )
  const firstCapturedDisplay = formatGuestProfileDate(
    response.profileSummary.firstCapturedAt
  )
  const overviewGuestSinceDisplay = formatGuestProfileDate(
    response.overviewDetails.guestSinceAt
  )
  const overviewLastActivityDisplay =
    formatOptionalRelative(response.overviewDetails.lastActivityAt, nowMs) ??
    "—"

  const lastInteractionRelative = formatOptionalRelative(
    response.profileSummary.lastInteractionAt,
    nowMs
  )
  const lastInteractionDisplay =
    lastInteractionRelative == null
      ? "—"
      : `${response.profileSummary.lastInteractionLabel} · ${lastInteractionRelative}`

  return {
    id: String(response.id),
    locationId: response.locationId,
    name: response.name,
    marketingStatusLabel:
      response.marketingStatus as GuestMarketingStatusLabel,
    guestSinceDisplay,
    lastActivityDisplay,
    identitySubtitle: buildIdentitySubtitle(
      guestSinceDisplay,
      lastActivityDisplay
    ),
    lastInteractionLabel: response.lastInteractionLabel,
    profileSummary: {
      emailDisplay: displayOrNotProvided(response.profileSummary.email),
      mobileDisplay: displayOrNotProvided(response.profileSummary.mobile),
      firstCapturedDisplay,
      locationName: response.profileSummary.locationName,
      feedbackSubmissionCount:
        response.profileSummary.feedbackSubmissionCount,
      offerClaimsAndRedemptions:
        response.profileSummary.offerClaimsAndRedemptions,
      lastInteractionDisplay,
      lastInteractionLabel: response.profileSummary.lastInteractionLabel,
      guestTagsDisplay: GUEST_PROFILE_NOT_PROVIDED,
    },
    overviewDetails: {
      guestSinceDisplay: overviewGuestSinceDisplay,
      totalInteractions: response.overviewDetails.totalInteractions,
      feedbackReceived: response.overviewDetails.feedbackReceived,
      offersClaimed: response.overviewDetails.offersClaimed,
      campaignsSent: response.overviewDetails.campaignsSent,
      lastActivityDisplay: overviewLastActivityDisplay,
    },
    contactEligibility: response.contactEligibility.map((row) => ({
      channel: row.channel,
      channelLabel: row.channel === "email" ? "Email" : "SMS",
      status: row.status,
      statusLabel: GUEST_PROFILE_CONTACT_STATUS_LABELS[row.status],
    })),
  }
}

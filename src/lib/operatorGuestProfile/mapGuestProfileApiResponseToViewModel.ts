import {
  GUEST_PROFILE_CONTACT_STATUS_LABELS,
  GUEST_PROFILE_FEEDBACK_RECOVERY_PLACEHOLDER,
  GUEST_PROFILE_FEEDBACK_SOURCE_LABEL,
  GUEST_PROFILE_NOT_PROVIDED,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import { labelForDetectedTag } from "@/lib/operatorHome/detectedTags"
import {
  formatRelativeTime,
  parseApiInstantMs,
} from "@/lib/operatorHome/relativeTime"
import type {
  GuestProfileLatestFeedbackItem,
  GuestProfileRecentNoteItem,
  GuestProfileResponse,
} from "@/types/dashboard"
import type { GuestMarketingStatusLabel } from "@/types/operatorGuests"
import type {
  OperatorGuestProfileLatestFeedbackRow,
  OperatorGuestProfileNoteRow,
  OperatorGuestProfileViewModel,
} from "@/types/operatorGuestProfile"

export type MapGuestProfileApiResponseInput = {
  response: GuestProfileResponse
  nowMs?: number
}

const FEEDBACK_COMMENT_PREVIEW_MAX_CHARS = 80

function formatGuestTagsDisplay(
  guestTags: GuestProfileResponse["profileSummary"]["guestTags"]
): string {
  if (guestTags.length === 0) {
    return GUEST_PROFILE_NOT_PROVIDED
  }
  return guestTags.map((tag) => tag.name).join(", ")
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

/** Absolute datetime for profile surfaces — Figma e.g. `15 July 2026, 7:42 PM`. */
export function formatGuestProfileAbsoluteDateTime(iso: string): string {
  const ms = parseApiInstantMs(iso)
  if (Number.isNaN(ms)) {
    return ""
  }

  const date = new Date(ms)
  const datePart = date.toLocaleDateString("en-GB", {
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone: "Europe/London",
  })
  const timePart = date
    .toLocaleTimeString("en-GB", {
      hour: "numeric",
      minute: "2-digit",
      hour12: true,
      timeZone: "Europe/London",
    })
    .replace(/\b(am|pm)\b/i, (match) => match.toUpperCase())

  return `${datePart}, ${timePart}`
}

export const formatGuestProfileFeedbackDateTime =
  formatGuestProfileAbsoluteDateTime

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

function truncateFeedbackComment(comment: string): string {
  const trimmed = comment.trim()
  if (trimmed.length <= FEEDBACK_COMMENT_PREVIEW_MAX_CHARS) {
    return trimmed
  }
  return `${trimmed.slice(0, FEEDBACK_COMMENT_PREVIEW_MAX_CHARS - 1)}…`
}

function mapLatestFeedbackRow(
  item: GuestProfileLatestFeedbackItem
): OperatorGuestProfileLatestFeedbackRow {
  const succeeded = item.classificationStatus === "Succeeded"

  return {
    id: item.id,
    classificationDisplay: succeeded ? item.sentiment : null,
    dateDisplay: formatGuestProfileAbsoluteDateTime(item.createdAt),
    locationName: item.locationName,
    sourceDisplay: GUEST_PROFILE_FEEDBACK_SOURCE_LABEL,
    feedbackDisplay: truncateFeedbackComment(item.comment),
    issueTagLabels: succeeded
      ? (item.detectedTags ?? []).map(labelForDetectedTag)
      : null,
    recoveryDisplay: GUEST_PROFILE_FEEDBACK_RECOVERY_PLACEHOLDER,
  }
}

export function mapGuestNoteItemToRow(
  item: GuestProfileRecentNoteItem
): OperatorGuestProfileNoteRow {
  return {
    id: item.id,
    body: item.body,
    authorDisplayName: item.authorDisplayName,
    createdAtDisplay: formatGuestProfileAbsoluteDateTime(item.createdAt),
  }
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
      guestTagsDisplay: formatGuestTagsDisplay(response.profileSummary.guestTags),
      guestTags: response.profileSummary.guestTags.map((tag) => ({
        id: String(tag.id),
        name: tag.name,
      })),
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
    latestFeedback: (response.latestFeedback ?? []).map(mapLatestFeedbackRow),
    recentNotes: (response.recentNotes ?? []).map(mapGuestNoteItemToRow),
  }
}

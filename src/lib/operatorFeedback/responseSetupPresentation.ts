import { labelForDetectedTag } from "@/lib/operatorHome/detectedTags"
import type { RespondToGuestChannel } from "@/lib/operatorFeedback/respondToGuestPresentation"
import {
  startRecoveryContactCapabilityLabel,
  type StartRecoveryContactCapability,
} from "@/lib/operatorFeedback/startRecoveryPresentation"
import type {
  ClassificationStatus,
  FeedbackDetailsResponse,
  FeedbackSentiment,
} from "@/types/dashboard"

/** Figma Response setup step heading (under stepper). */
export const RESPONSE_SETUP_STEP_HEADING =
  "How should the guest be contacted?"

export const RESPONSE_SETUP_STEP_DESCRIPTION =
  "Choose how the guest should be contacted and what the response should achieve."

export const RESPONSE_SETUP_PURPOSE_LABEL =
  "What should the response achieve?"

export const RESPONSE_SETUP_TONE_LABEL = "Response tone"

export const RESPONSE_SETUP_INCLUDE_NOTES_LABEL =
  "Anything the response should include?"

export const RESPONSE_SETUP_INCLUDE_NOTES_PLACEHOLDER =
  "Add any facts or actions the team has confirmed…"

/** Verified-facts helper under include-notes (Figma U-05). */
export const RESPONSE_SETUP_INCLUDE_NOTES_HELPER =
  "Only include information the restaurant has verified. Tummly will not invent refunds, compensation or operational actions."

const EMAIL_AVAILABILITY_LINE = "Available · No email credits required"
const SMS_AVAILABILITY_LINE = "Available · Estimated usage: 1 SMS credit"

export type ResponseSetupChannelCard = {
  channel: RespondToGuestChannel
  title: string
  availabilityLine: string
  selected: boolean
}

export type BuildResponseSetupChannelCardsInput = {
  availableChannels: readonly RespondToGuestChannel[]
  selectedChannel: RespondToGuestChannel | null
  /**
   * Single destination when only one channel is available (production today).
   * Ignored when `maskedDestinationByChannel` is set.
   */
  maskedDestination?: string | null
  /** Per-channel masks when more than one contact method is offered. */
  maskedDestinationByChannel?: Partial<
    Record<RespondToGuestChannel, string | null>
  >
}

function destinationForChannel(
  channel: RespondToGuestChannel,
  input: BuildResponseSetupChannelCardsInput
): string {
  const fromMap = input.maskedDestinationByChannel?.[channel]
  if (fromMap != null && fromMap.trim() !== "") {
    return fromMap.trim()
  }
  if (
    input.maskedDestination != null
    && input.maskedDestination.trim() !== ""
  ) {
    return input.maskedDestination.trim()
  }
  return "••••"
}

function titleForChannel(
  channel: RespondToGuestChannel,
  masked: string
): string {
  return channel === "email" ? `Email ${masked}` : `SMS ${masked}`
}

function availabilityLineForChannel(
  channel: RespondToGuestChannel
): string {
  return channel === "email" ? EMAIL_AVAILABILITY_LINE : SMS_AVAILABILITY_LINE
}

/** Email/SMS cards for Response setup — icon chrome stays in the UI layer. */
export function buildResponseSetupChannelCards(
  input: BuildResponseSetupChannelCardsInput
): ResponseSetupChannelCard[] {
  return input.availableChannels.map((channel) => ({
    channel,
    title: titleForChannel(channel, destinationForChannel(channel, input)),
    availabilityLine: availabilityLineForChannel(channel),
    selected: input.selectedChannel === channel,
  }))
}

/** Mid-flow Feedback summary chrome shared by guest-messaging Response setup. */
export type ResponseSetupSummaryChrome = {
  classificationStatus: ClassificationStatus
  classificationSentiment: FeedbackSentiment | null
  contactLabel: string
  issueTagLabels: string[] | null
}

export function mapResponseSetupIssueTagLabels(
  classificationStatus: ClassificationStatus,
  detectedTags: string[] | null | undefined
): string[] | null {
  if (classificationStatus !== "Succeeded" || detectedTags == null) {
    return null
  }
  return detectedTags.map((key) => labelForDetectedTag(key))
}

export function mapResponseSetupSummaryChrome(
  response: Pick<
    FeedbackDetailsResponse,
    "classificationStatus" | "sentiment" | "detectedTags"
  >,
  contactCapability: StartRecoveryContactCapability
): ResponseSetupSummaryChrome {
  return {
    classificationStatus: response.classificationStatus,
    classificationSentiment: response.sentiment,
    contactLabel: startRecoveryContactCapabilityLabel(contactCapability),
    issueTagLabels: mapResponseSetupIssueTagLabels(
      response.classificationStatus,
      response.detectedTags
    ),
  }
}

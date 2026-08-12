import type { RespondToGuestChannel } from "@/lib/operatorFeedback/respondToGuestPresentation"
import {
  INTERNAL_ACTION_FOLLOW_UP_STATUS_LABEL,
  INTERNAL_ACTION_RECOVERY_RECORDED_STATUS_LABEL,
  INTERNAL_ACTION_WORKFLOW_IN_PROGRESS_LABEL,
} from "@/lib/operatorFeedback/internalActionPresentation"

/** Success-screen status list chrome for recovery wizards (Figma U-07). */

export type RecoverySuccessStatusValueKind = "badge" | "text"

export type RecoverySuccessStatusRow = {
  label: string
  value: string
  valueKind: RecoverySuccessStatusValueKind
}

export type RecoverySuccessChrome = {
  title: string
  subtitle: string
  rows: RecoverySuccessStatusRow[]
}

const RESPONSE_STATUS_SENT = "Sent"
const RECOVERY_STATUS_RESPONSE_SENT = "Response sent"
const INTERNAL_ACTION_RECORDED = "Recorded"
const RECOVERY_STATUS_OFFER_ISSUED = "Offer issued"
const REDEMPTION_STATUS_NOT_REDEEMED = "Not redeemed"

function formatClockPart(at: Date): string {
  return new Intl.DateTimeFormat("en-GB", {
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  })
    .format(at)
    .replace(/\u202f/g, " ")
    .replace(/\s*(am|pm)$/i, (_, period: string) => ` ${period.toUpperCase()}`)
}

function formatLongDatePart(at: Date): string {
  return new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "long",
    year: "numeric",
  }).format(at)
}

/** Figma Success row: `29 July 2026 at 8:14 PM`. */
export function formatRecoverySuccessDateTime(at: Date): string {
  return `${formatLongDatePart(at)} at ${formatClockPart(at)}`
}

/** Figma Offer expiry: `31 August 2026`. */
export function formatRecoverySuccessDate(at: Date): string {
  return formatLongDatePart(at)
}

function channelLabel(channel: RespondToGuestChannel): string {
  return channel === "sms" ? "SMS" : "Email"
}

function channelSubtitleWord(channel: RespondToGuestChannel): string {
  return channel === "sms" ? "SMS" : "email"
}

function displayNameOrDash(name: string | null): string {
  const trimmed = name?.trim() ?? ""
  return trimmed === "" ? "—" : trimmed
}

function parseInstant(iso: string | null | undefined): Date | null {
  if (iso == null || iso.trim() === "") {
    return null
  }
  const at = new Date(iso)
  return Number.isNaN(at.getTime()) ? null : at
}

function badgeRow(label: string, value: string): RecoverySuccessStatusRow {
  return { label, value, valueKind: "badge" }
}

function textRow(label: string, value: string): RecoverySuccessStatusRow {
  return { label, value, valueKind: "text" }
}

export function recoverySuccessChromeForRespondToGuest(input: {
  maskedDestination: string | null
  channel: RespondToGuestChannel | null
  actorDisplayName: string | null
  sentAt: string | null
}): RecoverySuccessChrome {
  const destination = input.maskedDestination?.trim() || "the guest"
  const channel = input.channel ?? "email"
  const sentAt = parseInstant(input.sentAt)

  return {
    title: "Response sent",
    subtitle: `The response was sent to ${destination} and recorded against this feedback.`,
    rows: [
      badgeRow("Response status", RESPONSE_STATUS_SENT),
      badgeRow("Recovery status", RECOVERY_STATUS_RESPONSE_SENT),
      badgeRow("Workflow status", INTERNAL_ACTION_WORKFLOW_IN_PROGRESS_LABEL),
      textRow("Channel", channelLabel(channel)),
      textRow("Sent by", displayNameOrDash(input.actorDisplayName)),
      textRow(
        "Sent",
        sentAt == null ? "—" : formatRecoverySuccessDateTime(sentAt)
      ),
    ],
  }
}

export function recoverySuccessChromeForRespondAndRecord(input: {
  channel: RespondToGuestChannel | null
}): RecoverySuccessChrome {
  const channel = input.channel ?? "email"

  return {
    title: "Response sent and internal action recorded",
    subtitle: `The guest response was sent by ${channelSubtitleWord(channel)}, and the internal follow-up was added to this feedback.`,
    rows: [
      badgeRow("Response status", RESPONSE_STATUS_SENT),
      badgeRow("Internal action", INTERNAL_ACTION_RECORDED),
      badgeRow("Workflow status", INTERNAL_ACTION_WORKFLOW_IN_PROGRESS_LABEL),
    ],
  }
}

export function recoverySuccessChromeForRecordInternalAction(input: {
  actorDisplayName: string | null
  recordedAt: string | null
}): RecoverySuccessChrome {
  const recordedAt = parseInstant(input.recordedAt)

  return {
    title: "Internal follow-up recorded",
    subtitle:
      "The action has been added to this feedback’s activity history.",
    rows: [
      badgeRow(
        "Recovery status",
        INTERNAL_ACTION_RECOVERY_RECORDED_STATUS_LABEL
      ),
      badgeRow("Follow-up status", INTERNAL_ACTION_FOLLOW_UP_STATUS_LABEL),
      badgeRow("Workflow status", INTERNAL_ACTION_WORKFLOW_IN_PROGRESS_LABEL),
      textRow("Recorded by", displayNameOrDash(input.actorDisplayName)),
      textRow(
        "Recorded",
        recordedAt == null ? "—" : formatRecoverySuccessDateTime(recordedAt)
      ),
    ],
  }
}

export function recoverySuccessChromeForRespondWithRecoveryOffer(input: {
  maskedDestination: string | null
  offerTitle: string | null
  expiryAt: string | null
  claimCode: string | null
}): RecoverySuccessChrome {
  const destination = input.maskedDestination?.trim() || "the guest"
  const offerTitle = input.offerTitle?.trim() || "the recovery offer"
  const expiryAt = parseInstant(input.expiryAt)
  const claimCode = input.claimCode?.trim() || "—"

  return {
    title: "Response and recovery offer sent",
    subtitle: `The response was sent to ${destination}, and “${offerTitle}” was issued to the guest.`,
    rows: [
      badgeRow("Recovery status", RECOVERY_STATUS_OFFER_ISSUED),
      badgeRow("Response status", RESPONSE_STATUS_SENT),
      badgeRow("Workflow status", INTERNAL_ACTION_WORKFLOW_IN_PROGRESS_LABEL),
      textRow("Claim code", claimCode),
      textRow(
        "Offer expiry",
        expiryAt == null ? "—" : formatRecoverySuccessDate(expiryAt)
      ),
      badgeRow("Redemption status", REDEMPTION_STATUS_NOT_REDEEMED),
    ],
  }
}

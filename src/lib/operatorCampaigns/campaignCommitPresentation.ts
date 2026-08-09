/**
 * Campaign schedule / send confirm + success chrome (ticket 26).
 * Confirm dialog + Recovery-pattern success with Campaign-specific copy.
 */

import type { CampaignScheduleModeId } from "@/lib/operatorCampaigns/campaignSchedulePresentation"
import {
  formatRecoverySuccessDateTime,
  type RecoverySuccessChrome,
  type RecoverySuccessStatusRow,
} from "@/lib/operatorFeedback/recoverySuccessPresentation"

export const CAMPAIGN_COMMIT_COPY = {
  sendNowPrimary: "Send campaign now",
  scheduleLaterPrimary: "Schedule campaign",
  sendNowConfirmTitle: "Send this campaign now?",
  sendNowConfirmDescription:
    "Tummly will run one final eligibility check before processing emails. Emails already sent cannot be recalled.",
  sendNowConfirmLabel: "Send campaign",
  scheduleLaterConfirmTitle: "Schedule this campaign?",
  scheduleLaterConfirmDescription:
    "Tummly will reserve credits and freeze recipients for the scheduled send. You can unschedule later from the Campaigns list.",
  scheduleLaterConfirmLabel: "Schedule campaign",
  confirmCancelLabel: "Back to review",
  confirmBusyLabel: "Working…",
  billingReserveUnavailable:
    "Billing Reserve is not available yet. Schedule and send stay blocked. You can still Save draft and Send test.",
  reserveFailedDefault:
    "Could not reserve credits for this campaign. Top up or reduce the audience, then try again.",
  zeroEligible:
    "No recipients are eligible on the selected channel. Update audience or channel, then try again.",
  softLocked:
    "This account is soft-locked. Schedule and send stay blocked.",
  channelHardStop:
    "There are not enough credits left on this channel. Schedule and send stay blocked.",
  commitNotReady:
    "Schedule and send stay blocked until this campaign is ready to commit.",
  successDoneLabel: "Done",
} as const

function badgeRow(label: string, value: string): RecoverySuccessStatusRow {
  return { label, value, valueKind: "badge" }
}

function textRow(label: string, value: string): RecoverySuccessStatusRow {
  return { label, value, valueKind: "text" }
}

export function campaignCommitConfirmCopy(input: {
  modeId: CampaignScheduleModeId
}): {
  title: string
  description: string
  confirmLabel: string
  cancelLabel: string
} {
  if (input.modeId === "schedule-later") {
    return {
      title: CAMPAIGN_COMMIT_COPY.scheduleLaterConfirmTitle,
      description: CAMPAIGN_COMMIT_COPY.scheduleLaterConfirmDescription,
      confirmLabel: CAMPAIGN_COMMIT_COPY.scheduleLaterConfirmLabel,
      cancelLabel: CAMPAIGN_COMMIT_COPY.confirmCancelLabel,
    }
  }

  return {
    title: CAMPAIGN_COMMIT_COPY.sendNowConfirmTitle,
    description: CAMPAIGN_COMMIT_COPY.sendNowConfirmDescription,
    confirmLabel: CAMPAIGN_COMMIT_COPY.sendNowConfirmLabel,
    cancelLabel: CAMPAIGN_COMMIT_COPY.confirmCancelLabel,
  }
}

export function campaignReviewPrimaryActionLabel(
  modeId: CampaignScheduleModeId
): string {
  return modeId === "schedule-later"
    ? CAMPAIGN_COMMIT_COPY.scheduleLaterPrimary
    : CAMPAIGN_COMMIT_COPY.sendNowPrimary
}

export function campaignCommitSuccessChrome(input: {
  modeId: CampaignScheduleModeId
  campaignName: string
  scheduledAtUtc: string | null
  committedAt: Date
}): RecoverySuccessChrome {
  if (input.modeId === "schedule-later") {
    const when =
      input.scheduledAtUtc == null
        ? "—"
        : formatRecoverySuccessDateTime(new Date(input.scheduledAtUtc))
    return {
      title: "Campaign scheduled",
      subtitle: `“${input.campaignName}” is scheduled. Credits are reserved and recipients are frozen until send.`,
      rows: [
        badgeRow("Campaign status", "Scheduled"),
        textRow("Scheduled for", when),
      ],
    }
  }

  return {
    title: "Campaign sending",
    subtitle: `“${input.campaignName}” is now sending. Credits are reserved and recipients are frozen.`,
    rows: [
      badgeRow("Campaign status", "Sending"),
      textRow(
        "Started",
        formatRecoverySuccessDateTime(input.committedAt)
      ),
    ],
  }
}

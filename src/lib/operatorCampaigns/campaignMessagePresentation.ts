/**
 * Campaign wizard Message step — Figma 4747:66343 / tickets 26 + 33.
 * Chooser + live AI prepare/rewrite + Write manually + Guest preview (Send test off).
 */

export type CampaignMessageWriteEntry = "chooser" | "editor"

export type CampaignMessageDraftMode =
  | "prepare"
  | "rewrite_subject"
  | "rewrite_message"

export type CampaignMessageDraftRewriteTarget = "subject" | "message"

/** Fixed tone for message-draft AI — Campaign Message has no tone setup step. */
export const CAMPAIGN_MESSAGE_DRAFT_DEFAULT_TONE = "friendly_and_clear"

export const CAMPAIGN_MESSAGE_AI_DRAFT_ERROR =
  "We could not prepare a draft."

export const CAMPAIGN_MESSAGE_COPY = {
  stepHeading: "Compose your message",
  stepDescription:
    "Write the campaign message, add approved personalisation and review the guest preview before continuing.",
  prepareTitle: "Prepare with AI",
  prepareDescription:
    "Use your campaign goal, audience and offer choices to prepare an editable draft.",
  prepareActionLabel: "Prepare message draft",
  /** Display-only metering chrome — live debit stays out of slice 1 (ticket 33). */
  aiActionMeteringLabel: "Uses 1 AI action",
  writeManualTitle: "Write manually",
  writeManualDescription:
    "Write the complete campaign message yourself without using an AI action.",
  writeManualActionLabel: "Write message manually",
  subjectLabel: "Subject",
  messageLabel: "Message",
  rewriteWithAiLabel: "Rewrite with AI",
  retryAiLabel: "Try again",
  previewControlLabel: "Preview",
  usageTitle: "Estimated message usage",
  preparingOverlayTitle: "Preparing campaign draft…",
  preparingOverlayDescription:
    "Tummly is drafting subject and message from your campaign choices. You can edit everything before Save.",
} as const

export function isCampaignMessageDraftRewriteMode(
  mode: CampaignMessageDraftMode | null
): boolean {
  return mode === "rewrite_subject" || mode === "rewrite_message"
}

/** Subject / Message field label — Operator Input chrome (Campaign-owned). */
export const CAMPAIGN_MESSAGE_FIELD_LABEL_CLASS =
  "text-sm font-semibold leading-5 text-op-text-primary"

export const CAMPAIGN_MESSAGE_INPUT_CLASS =
  "rounded-[4px] border-op-input-border bg-transparent px-[15px] text-sm text-op-text-primary placeholder:text-op-input-placeholder dark:bg-transparent dark:disabled:bg-transparent"

export const CAMPAIGN_MESSAGE_TEXTAREA_CLASS =
  "rounded-[4px] border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary placeholder:text-op-input-placeholder dark:bg-transparent dark:disabled:bg-transparent"

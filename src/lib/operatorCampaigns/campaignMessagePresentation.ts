/**
 * Campaign wizard Message step — Figma 4747:66343 / ticket 26.
 * Chooser + Write manually + Guest preview (Send test off). Live AI prepare is ticket 33.
 */

export type CampaignMessageWriteEntry = "chooser" | "editor"

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
  previewControlLabel: "Preview",
  usageTitle: "Estimated message usage",
} as const

/** Subject / Message field label — Operator Input chrome (Campaign-owned). */
export const CAMPAIGN_MESSAGE_FIELD_LABEL_CLASS =
  "text-sm font-semibold leading-5 text-op-text-primary"

export const CAMPAIGN_MESSAGE_INPUT_CLASS =
  "rounded-[4px] border-op-input-border bg-transparent px-[15px] text-sm text-op-text-primary placeholder:text-op-input-placeholder dark:bg-transparent dark:disabled:bg-transparent"

export const CAMPAIGN_MESSAGE_TEXTAREA_CLASS =
  "rounded-[4px] border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary placeholder:text-op-input-placeholder dark:bg-transparent dark:disabled:bg-transparent"

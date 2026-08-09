/**
 * Above Operator wizard shell (`z-[130]`) and Guest preview overlay (`z-[135]`).
 * Same ladder as Operator wizard confirm dialog.
 */
export const CAMPAIGN_SEND_TEST_DIALOG_OVERLAY_CLASS = "z-[140]"

export const CAMPAIGN_SEND_TEST_DIALOG_CONTENT_CLASS =
  "z-[140] gap-[30px] overflow-y-auto bg-[var(--op-color-gray-995)] p-8 text-op-text-primary sm:max-w-[520px]"

/**
 * Campaign send test — Send test email dialog + Guest preview (ticket 24 / Figma 4752:71297).
 */

export const CAMPAIGN_SEND_TEST_COPY = {
  dialogTitle: "Send test email",
  dialogDescription:
    "Send a test version of this guest message to yourself or a team member.",
  emailLabel: "Email address",
  emailPlaceholder: "Enter an email address…",
  confirmLabel: "Send test email",
  cancelLabel: "Cancel",
  successToast: "Test email sent.",
  errorMessage: "We could not send the test email. Try again.",
} as const

/** Sample offer chrome when an Offer stance is selected but catalog fields are absent. */
export const CAMPAIGN_SEND_TEST_SAMPLE_OFFER = {
  title: "Sample offer",
  description: "Show this code to the team on your next visit.",
  expiryLabel: "Expires: —",
} as const

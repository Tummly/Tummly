/** Staff Redeem dialogue — Figma enter 3527:56860, confirm 3527:57426, toast 3527:58361. */

export const STAFF_REDEEM_COPY = {
  title: "Redeem offer",
  enterSubtitle:
    "Scan or enter the guest’s Offer code before applying the benefit.",
  confirmSubtitle:
    "Enter or scan the guest’s offer code before applying the discount.",
  codeLabel: "Code",
  codePlaceholder: "Enter code",
  checkOffer: "Check offer",
  scan: "Scan",
  scanAriaLabel: "Scan offer code with camera",
  scanHint: "Point the camera at the guest’s offer QR code.",
  scanCancel: "Cancel scan",
  scanOpening: "Opening camera…",
  scanUnavailable:
    "Could not open the camera. Enter the code instead.",
  markAsRedeemed: "Mark as redeemed",
  cancel: "Cancel",
  closeAriaLabel: "Close redeem dialogue",
  offerLabel: "Offer",
  guestLabel: "Guest",
  validAtLabel: "Valid at",
  expiresLabel: "Expires",
  usageLabel: "Usage",
  staffInstructionLabel: "Staff instruction",
  successToast: "Offer redeemed",
  errors: {
    invalid: "Offer code not found.",
    expired: "This offer has expired.",
    already_used: "This offer was already redeemed.",
    voided: "This offer was voided.",
    wrong_location: "This offer is not valid at this location.",
    redeem_failed: "Could not redeem this offer. Try again.",
    empty_code: "Enter an offer code.",
  },
} as const

/** Dark panel #171717 — closest token `--op-color-gray-1000`. */
export const STAFF_REDEEM_CONTENT_CLASS =
  "gap-10 rounded-op-md border-0 bg-op-surface-secondary p-8 text-op-text-primary shadow-lg sm:max-w-[560px] dark:bg-[var(--op-color-gray-1000)]"

export const STAFF_REDEEM_TITLE_CLASS =
  "pr-0 text-2xl font-bold leading-normal tracking-normal text-op-text-primary"

export const STAFF_REDEEM_SUBTITLE_CLASS =
  "text-sm font-medium leading-normal tracking-normal text-[var(--op-color-gray-550)]"

export const STAFF_REDEEM_LABEL_CLASS =
  "text-sm font-semibold leading-5 text-op-text-primary"

export const STAFF_REDEEM_CODE_FIELD_CLASS =
  "h-auto min-h-0 rounded-[4px] border border-op-border-default bg-transparent px-[15px] py-[15px] text-sm font-normal leading-5 text-op-text-primary placeholder:text-[var(--op-color-gray-550)] focus-visible:ring-1 focus-visible:ring-op-border-default"

export const STAFF_REDEEM_ERROR_CLASS =
  "m-0 text-sm font-medium leading-5 text-destructive"

export const STAFF_REDEEM_ROW_LABEL_CLASS =
  "text-base font-semibold leading-normal text-[var(--op-color-gray-550)]"

export const STAFF_REDEEM_ROW_VALUE_CLASS =
  "text-base font-medium leading-normal text-op-text-primary"

export const STAFF_REDEEM_DIVIDER_CLASS = "h-px w-full bg-op-border-default"

export const STAFF_REDEEM_INSTRUCTION_TITLE_CLASS =
  "m-0 text-sm font-semibold leading-5 text-op-text-primary"

export const STAFF_REDEEM_INSTRUCTION_BODY_CLASS =
  "m-0 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]"

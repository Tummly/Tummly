/**
 * Void request dialogues — Figma create 4783:24252, review 5223:75356,
 * approve confirm 5223:76624, reject confirm 5229:80122.
 */

import {
  STAFF_REDEEM_CONTENT_CLASS,
  STAFF_REDEEM_DIVIDER_CLASS,
  STAFF_REDEEM_ERROR_CLASS,
  STAFF_REDEEM_LABEL_CLASS,
  STAFF_REDEEM_ROW_LABEL_CLASS,
  STAFF_REDEEM_ROW_VALUE_CLASS,
  STAFF_REDEEM_SUBTITLE_CLASS,
  STAFF_REDEEM_TITLE_CLASS,
} from "@/lib/operatorOffers/staffRedeemPresentation"

export const VOID_REQUEST_REASON_IDS = [
  "redeemed_by_mistake",
  "wrong_offer_pass",
  "duplicate_redemption",
  "guest_did_not_receive",
  "incorrect_location",
  "other",
] as const

export type VoidRequestReasonId = (typeof VOID_REQUEST_REASON_IDS)[number]

export const VOID_REQUEST_CORRECTION_IDS = [
  "keep_unusable",
  "restore_one_use",
] as const

export type VoidRequestCorrectionId =
  (typeof VOID_REQUEST_CORRECTION_IDS)[number]

export const VOID_REQUEST_COPY = {
  createTitle: "Void this redemption?",
  createSubtitle:
    "The original redemption will remain in history. The pass may or may not become usable again depending on the selected correction.",
  reviewTitle: "Review void request",
  reviewSubtitle:
    "A staff member has requested a correction to this redemption. Approving will create a correction record in the redemption history.",
  approveConfirmTitle: "Approve void request?",
  approveConfirmSubtitle:
    "This will create a correction record in the redemption history. The original redemption will remain visible for audit purposes.",
  rejectConfirmTitle: "Reject void request?",
  rejectConfirmSubtitle:
    "The original redemption will stay unchanged. This rejection will be recorded in the redemption history.",
  closeAriaLabel: "Close void request dialogue",
  offerLabel: "Offer",
  guestLabel: "Guest",
  passCodeLabel: "Pass code",
  currentStateLabel: "Current state",
  expiresLabel: "Expires",
  locationLabel: "Location",
  linkedCampaignLabel: "Linked campaign",
  requestedByLabel: "Requested by",
  requestedAtLabel: "Requested at",
  reasonLabel: "Reason",
  reasonForRequestLabel: "Reason for request",
  reasonPlaceholder: "Select a reason",
  explanationLabel: "Explanation",
  explanationPlaceholder: "Explain why this redemption should be corrected…",
  requestedCorrectionLabel: "Requested correction",
  sendRequest: "Send request",
  cancel: "Cancel",
  approveRequest: "Approve request",
  rejectRequest: "Reject request",
  goBack: "Go back",
  successCreateToast: "Void request sent",
  successApproveToast: "Void request approved",
  successRejectToast: "Void request rejected",
  notRedeemedToast: "Void requests apply to redeemed passes only.",
  reasons: {
    redeemed_by_mistake: "Redeemed by mistake",
    wrong_offer_pass: "Wrong offer pass was used",
    duplicate_redemption: "Duplicate redemption recorded",
    guest_did_not_receive: "Guest did not receive the benefit",
    incorrect_location: "Incorrect location recorded",
    other: "Other",
  },
  corrections: {
    keep_unusable: {
      title: "Keep pass unusable",
      helper:
        "Void the redemption record but do not allow the guest to use the pass again.",
    },
    restore_one_use: {
      title: "Restore one redemption use",
      helper:
        "Request that the pass becomes usable one more time. This requires elevated permission.",
    },
  },
  errors: {
    reasonAndCorrectionRequired: "Select a reason and a requested correction.",
    explanationRequired: "Enter an explanation when reason is Other.",
    pendingExists: "A pending void request already exists for this pass.",
    createFailed: "Could not send this void request. Try again.",
    approveFailed: "Could not approve this void request. Try again.",
    rejectFailed: "Could not reject this void request. Try again.",
    requestNotFound: "Void request not found.",
  },
} as const

export const VOID_REQUEST_REASON_OPTIONS = VOID_REQUEST_REASON_IDS.map(
  (id) => ({
    id,
    label: VOID_REQUEST_COPY.reasons[id],
  })
)

export const VOID_REQUEST_CORRECTION_OPTIONS = VOID_REQUEST_CORRECTION_IDS.map(
  (id) => ({
    id,
    title: VOID_REQUEST_COPY.corrections[id].title,
    helper: VOID_REQUEST_COPY.corrections[id].helper,
  })
)

/** Reuse Staff Redeem dark panel tokens where identical. */
export const VOID_REQUEST_CONTENT_CLASS = STAFF_REDEEM_CONTENT_CLASS
export const VOID_REQUEST_TITLE_CLASS = STAFF_REDEEM_TITLE_CLASS
export const VOID_REQUEST_SUBTITLE_CLASS = STAFF_REDEEM_SUBTITLE_CLASS
export const VOID_REQUEST_LABEL_CLASS = STAFF_REDEEM_LABEL_CLASS
export const VOID_REQUEST_ROW_LABEL_CLASS = STAFF_REDEEM_ROW_LABEL_CLASS
export const VOID_REQUEST_ROW_VALUE_CLASS = STAFF_REDEEM_ROW_VALUE_CLASS
export const VOID_REQUEST_DIVIDER_CLASS = STAFF_REDEEM_DIVIDER_CLASS
export const VOID_REQUEST_ERROR_CLASS = STAFF_REDEEM_ERROR_CLASS

export const VOID_REQUEST_FIELD_CLASS =
  "h-auto min-h-0 rounded-[4px] border border-op-border-default bg-transparent px-[15px] py-[15px] text-sm font-normal leading-5 text-op-text-primary placeholder:text-[var(--op-color-gray-550)] focus-visible:ring-1 focus-visible:ring-op-border-default"

export const VOID_REQUEST_TEXTAREA_CLASS =
  `${VOID_REQUEST_FIELD_CLASS} min-h-[120px] resize-y`

export const VOID_REQUEST_SELECT_CONTENT_CLASS = "z-[130]"

export const VOID_REQUEST_CORRECTION_CARD_CLASS =
  "flex w-full flex-col items-start gap-1 rounded-op-md border border-op-border-default bg-transparent px-[18px] py-4 text-left transition-colors hover:bg-op-surface-secondary/40 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-op-border-default data-[selected=true]:border-[var(--op-color-gray-550)] data-[selected=true]:bg-op-surface-secondary/60"

export const VOID_REQUEST_CORRECTION_TITLE_CLASS =
  "m-0 text-sm font-medium leading-normal text-op-text-primary"

export const VOID_REQUEST_CORRECTION_HELPER_CLASS =
  "m-0 text-xs font-medium leading-normal text-[var(--op-color-gray-550)]"

export const VOID_REQUEST_SECTION_TITLE_CLASS =
  "m-0 text-lg font-semibold leading-normal text-op-text-primary"

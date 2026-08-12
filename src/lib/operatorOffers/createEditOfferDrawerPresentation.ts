/**
 * Shared Create/Edit Offer drawer chrome — Offers Figma is source of truth
 * (tickets 09 / 15 / 18; Create Figma `4770:99053` / `4770:100262`).
 */

export type CreateEditOfferDrawerMode = "create" | "edit"

/** Outcome of Create/Edit Offer save — pages toast on created/updated. */
export type ConfirmCatalogOfferWriteResult =
  | "created"
  | "updated"
  | "awaiting-edit-confirm"
  | "noop"
  | "error"

export const CREATE_EDIT_OFFER_DRAWER_COPY = {
  createTitle: "Create a new offer",
  createConfirm: "Create offer",
  editTitle: "Edit offer",
  editConfirm: "Save changes",
  editSaveGatedHelper: "Saving edits is not available yet",
  cancel: "Cancel",
  createOfferError: "Could not create this offer. Try again.",
  updateOfferError: "Could not save these changes. Try again.",
  createSuccessToast: "Offer created",
  updateSuccessToast: "Offer updated",
  editLoadError: "Could not load this offer. Try again.",
  editSaveConfirmTitle: "Save changes",
  editSaveConfirmDescription:
    "Changes apply to new issues only. Existing passes stay as they are.",
  redemptionLabel: "Redemption",
  redemptionValue: "Unique single-use code",
  redemptionHelper:
    "Each guest receives a unique offer pass containing a QR code and a manual offer code. Restaurant staff can scan the QR code or enter the code in Tummly, then confirm redemption.",
  recoveryCalloutTitle: "Recovery use only",
  recoveryCalloutBody:
    "Replacement offers should only be used after a private recovery case has been reviewed.",
  /** Offers Create Figma omits “(optional)” on this label. */
  staffInstructionsLabel: "Staff instructions",
  /** Discard confirm — Figma `4789:43034`. */
  discardTitle: "Discard this offer?",
  discardDescription: "Your unsaved offer details will be lost.",
  discardConfirm: "Discard",
  discardKeepEditing: "Keep editing",
  descriptionPlaceholder: "Explain the offer to the guest…",
  replacementPlaceholder: "Enter the item that can be replaced…",
  expiryDatePlaceholder: "Select a date",
} as const

/** Create/Cancel primary group — left-aligned per Offers Create Figma. */
export const CREATE_EDIT_OFFER_DRAWER_FOOTER_ACTIONS_CLASS =
  "flex justify-start gap-3"

/** Drawer shell padding — Figma Create Offer `p-[32px]`. */
export const CREATE_EDIT_OFFER_DRAWER_SHELL_CLASS =
  "flex h-full min-h-0 flex-col gap-5 p-8"

/**
 * Scroll body — bottom padding so the last field clears the sticky footer
 * while scrolling (shared body class has no pb).
 */
export const CREATE_EDIT_OFFER_DRAWER_BODY_CLASS =
  "min-h-0 flex-1 overflow-y-auto pb-8"

export const CREATE_EDIT_OFFER_DRAWER_DIVIDER_CLASS =
  "h-px w-full shrink-0 bg-op-card-border"

/**
 * Discard dialog must sit above Create/Edit drawer (`z-[138]`) —
 * same band as OperatorWizardShell nested dialogs.
 */
export const CREATE_EDIT_OFFER_DISCARD_OVERLAY_CLASS = "z-[150]"

export const CREATE_EDIT_OFFER_DISCARD_CONTENT_CLASS =
  "z-[151]"

export function createEditOfferDrawerTitle(
  mode: CreateEditOfferDrawerMode
): string {
  return mode === "edit"
    ? CREATE_EDIT_OFFER_DRAWER_COPY.editTitle
    : CREATE_EDIT_OFFER_DRAWER_COPY.createTitle
}

export function createEditOfferDrawerConfirmLabel(
  mode: CreateEditOfferDrawerMode
): string {
  return mode === "edit"
    ? CREATE_EDIT_OFFER_DRAWER_COPY.editConfirm
    : CREATE_EDIT_OFFER_DRAWER_COPY.createConfirm
}

/** Toast copy for a completed Create/Edit Offer write. */
export function catalogOfferWriteSuccessToast(
  result: ConfirmCatalogOfferWriteResult
): string | null {
  if (result === "created") {
    return CREATE_EDIT_OFFER_DRAWER_COPY.createSuccessToast
  }
  if (result === "updated") {
    return CREATE_EDIT_OFFER_DRAWER_COPY.updateSuccessToast
  }
  return null
}

/** Edit never shows the Create type picker (ticket 15), even before hydrate. */
export function createEditOfferDrawerShowsTypePicker(
  mode: CreateEditOfferDrawerMode
): boolean {
  return mode === "create"
}

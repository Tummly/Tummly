/**
 * Shared Create/Edit Offer drawer chrome — Offers Figma is source of truth
 * (tickets 09 / 15 / 18).
 */

export type CreateEditOfferDrawerMode = "create" | "edit"

export const CREATE_EDIT_OFFER_DRAWER_COPY = {
  createTitle: "Create a new offer",
  createConfirm: "Create offer",
  editTitle: "Edit offer",
  editConfirm: "Save changes",
  editSaveGatedHelper: "Saving edits is not available yet",
  cancel: "Cancel",
  createOfferError: "Could not create this offer. Try again.",
  updateOfferError: "Could not save these changes. Try again.",
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
} as const

/** Create/Cancel primary group — left-aligned per Offers Create Figma. */
export const CREATE_EDIT_OFFER_DRAWER_FOOTER_ACTIONS_CLASS =
  "flex justify-start gap-3"

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

/** Edit never shows the Create type picker (ticket 15), even before hydrate. */
export function createEditOfferDrawerShowsTypePicker(
  mode: CreateEditOfferDrawerMode
): boolean {
  return mode === "create"
}

import { describe, expect, it } from "vitest"

import {
  catalogOfferWriteSuccessToast,
  CREATE_EDIT_OFFER_DRAWER_COPY,
  CREATE_EDIT_OFFER_DRAWER_FOOTER_ACTIONS_CLASS,
  createEditOfferDrawerConfirmLabel,
  createEditOfferDrawerShowsTypePicker,
  createEditOfferDrawerTitle,
} from "@/lib/operatorOffers/createEditOfferDrawerPresentation"

describe("createEditOfferDrawerPresentation", () => {
  it("uses Create and Edit chrome labels", () => {
    expect(createEditOfferDrawerTitle("create")).toBe("Create a new offer")
    expect(createEditOfferDrawerTitle("edit")).toBe("Edit offer")
    expect(createEditOfferDrawerConfirmLabel("create")).toBe("Create offer")
    expect(createEditOfferDrawerConfirmLabel("edit")).toBe("Save changes")
  })

  it("matches Offers Create Figma Staff instructions label without optional", () => {
    expect(CREATE_EDIT_OFFER_DRAWER_COPY.staffInstructionsLabel).toBe(
      "Staff instructions"
    )
    expect(CREATE_EDIT_OFFER_DRAWER_COPY.staffInstructionsLabel).not.toMatch(
      /optional/i
    )
  })

  it("left-aligns Create/Cancel footer actions to match Offers Create Figma", () => {
    expect(CREATE_EDIT_OFFER_DRAWER_FOOTER_ACTIONS_CLASS).toContain(
      "justify-start"
    )
    expect(CREATE_EDIT_OFFER_DRAWER_FOOTER_ACTIONS_CLASS).not.toContain(
      "justify-end"
    )
  })

  it("never shows the Create type picker in Edit mode", () => {
    expect(createEditOfferDrawerShowsTypePicker("create")).toBe(true)
    expect(createEditOfferDrawerShowsTypePicker("edit")).toBe(false)
  })

  it("exposes Discard confirm copy from Create Offer Figma", () => {
    expect(CREATE_EDIT_OFFER_DRAWER_COPY.discardTitle).toBe(
      "Discard this offer?"
    )
    expect(CREATE_EDIT_OFFER_DRAWER_COPY.discardConfirm).toBe("Discard")
    expect(CREATE_EDIT_OFFER_DRAWER_COPY.discardKeepEditing).toBe(
      "Keep editing"
    )
  })

  it("maps create/update write results to success toast copy", () => {
    expect(catalogOfferWriteSuccessToast("created")).toBe("Offer created")
    expect(catalogOfferWriteSuccessToast("updated")).toBe("Offer updated")
    expect(catalogOfferWriteSuccessToast("noop")).toBeNull()
    expect(catalogOfferWriteSuccessToast("error")).toBeNull()
    expect(catalogOfferWriteSuccessToast("awaiting-edit-confirm")).toBeNull()
  })
})

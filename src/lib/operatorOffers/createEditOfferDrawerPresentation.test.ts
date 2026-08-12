import { describe, expect, it } from "vitest"

import {
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
})

import { describe, expect, it } from "vitest"

import {
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

  it("never shows the Create type picker in Edit mode", () => {
    expect(createEditOfferDrawerShowsTypePicker("create")).toBe(true)
    expect(createEditOfferDrawerShowsTypePicker("edit")).toBe(false)
  })
})

import { describe, expect, it } from "vitest"

import { HOME_PRICING, HOME_TRUST_CONTROL } from "@/content/marketing/homePage"

// Figma `4942:19219` (Trust & control) and `4902:19866` (Pricing).
describe("Home Trust & control copy", () => {
  it("has the four accordion items from Figma", () => {
    expect(HOME_TRUST_CONTROL.items.map((item) => item.title)).toEqual([
      "A private way to be heard",
      "A clear choice about marketing",
      "Access that fits the job",
      "Help with the work, oversight of the decision",
    ])
  })
})

describe("Home Pricing copy", () => {
  it("keeps the four plans in order with Growth highlighted", () => {
    expect(HOME_PRICING.cards.map((card) => card.id)).toEqual([
      "Pilot",
      "Starter",
      "Growth",
      "Group",
    ])
    expect(
      HOME_PRICING.cards.filter((card) => card.isHighlighted).map((c) => c.id),
    ).toEqual(["Growth"])
  })

  it("shows location limits as the grey line", () => {
    expect(HOME_PRICING.cards.map((card) => card.subline)).toEqual([
      "No payment card required.",
      "1 Location only",
      "Up to 3 Locations",
      "Up to 5 Locations",
    ])
  })
})

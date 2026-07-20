import { describe, expect, it } from "vitest"

import {
  LIVE_OFFERS_EMPTY_ACTION_BUTTON_CLASS,
  LIVE_OFFERS_EMPTY_ACTIONS,
  LIVE_OFFERS_EMPTY_HELPER_CLASS,
  LIVE_OFFERS_EMPTY_TITLE_CLASS,
  LIVE_OFFERS_SECTION_CLASS,
  LIVE_OFFERS_SUBTITLE_CLASS,
  LIVE_OFFERS_TITLE_CLASS,
  resolveLiveOffersEmptyActionVariant,
} from "./liveOffersSectionPresentation"

describe("liveOffersSectionPresentation", () => {
  it("uses Figma card chrome", () => {
    expect(LIVE_OFFERS_SECTION_CLASS).toContain("rounded-md")
    expect(LIVE_OFFERS_SECTION_CLASS).toContain("border-[#e5e5e5]")
    expect(LIVE_OFFERS_SECTION_CLASS).toContain("bg-[#f8f8f8]")
    expect(LIVE_OFFERS_SECTION_CLASS).toContain("p-4")
    expect(LIVE_OFFERS_SECTION_CLASS).toContain("sm:p-5")
    expect(LIVE_OFFERS_SECTION_CLASS).toContain("md:p-6")
    expect(LIVE_OFFERS_SECTION_CLASS).toContain("gap-6")
    expect(LIVE_OFFERS_SECTION_CLASS).toContain("sm:gap-8")
    expect(LIVE_OFFERS_SECTION_CLASS).toContain("md:gap-10")
  })

  it("uses Figma header typography", () => {
    expect(LIVE_OFFERS_TITLE_CLASS).toContain("text-lg")
    expect(LIVE_OFFERS_TITLE_CLASS).toContain("sm:text-xl")
    expect(LIVE_OFFERS_TITLE_CLASS).toContain("font-bold")
    expect(LIVE_OFFERS_SUBTITLE_CLASS).toContain("text-sm")
    expect(LIVE_OFFERS_SUBTITLE_CLASS).toContain("text-muted-foreground")
  })

  it("uses Figma empty copy typography", () => {
    expect(LIVE_OFFERS_EMPTY_TITLE_CLASS).toContain("text-base")
    expect(LIVE_OFFERS_EMPTY_HELPER_CLASS).toContain("max-w-[450px]")
    expect(LIVE_OFFERS_EMPTY_HELPER_CLASS).toContain("leading-[18px]")
  })

  it("maps empty-state CTAs to operator button variants", () => {
    expect(resolveLiveOffersEmptyActionVariant("create-offer")).toBe(
      "operator-secondary"
    )
    expect(resolveLiveOffersEmptyActionVariant("create-campaign")).toBe(
      "operator-tertiary"
    )
    expect(LIVE_OFFERS_EMPTY_ACTIONS.map((action) => action.label)).toEqual([
      "Create offer",
      "Create campaign",
    ])
    expect(LIVE_OFFERS_EMPTY_ACTION_BUTTON_CLASS).toContain("min-h-11")
    expect(LIVE_OFFERS_EMPTY_ACTION_BUTTON_CLASS).toContain("min-w-11")
    expect(LIVE_OFFERS_EMPTY_ACTION_BUTTON_CLASS).toContain("md:min-h-0")
  })
})

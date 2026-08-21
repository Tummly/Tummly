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
    expect(LIVE_OFFERS_SECTION_CLASS).toContain("rounded-op-lg")
    expect(LIVE_OFFERS_SECTION_CLASS).toContain("border-op-card-border")
    expect(LIVE_OFFERS_SECTION_CLASS).toContain("bg-op-card-background")
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
    expect(LIVE_OFFERS_SUBTITLE_CLASS).toContain("text-op-sm")
    expect(LIVE_OFFERS_SUBTITLE_CLASS).toContain("text-op-card-subtitle-color")
  })

  it("uses Figma empty copy typography", () => {
    expect(LIVE_OFFERS_EMPTY_TITLE_CLASS).toContain("text-base")
    expect(LIVE_OFFERS_EMPTY_TITLE_CLASS).toContain("text-op-empty-title-color")
    expect(LIVE_OFFERS_EMPTY_HELPER_CLASS).toContain("max-w-[450px]")
    expect(LIVE_OFFERS_EMPTY_HELPER_CLASS).toContain("leading-[18px]")
  })

  it("maps empty-state CTAs to operator button variants", () => {
    expect(resolveLiveOffersEmptyActionVariant("create-offer")).toBe(
      "op-secondary"
    )
    expect(resolveLiveOffersEmptyActionVariant("create-campaign")).toBe(
      "op-tertiary"
    )
    expect(LIVE_OFFERS_EMPTY_ACTIONS.map((action) => action.label)).toEqual([
      "Create offer",
      "Create campaign",
    ])
    expect(LIVE_OFFERS_EMPTY_ACTION_BUTTON_CLASS).toContain("min-h-11")
    expect(LIVE_OFFERS_EMPTY_ACTION_BUTTON_CLASS).toContain("min-w-11")
    expect(LIVE_OFFERS_EMPTY_ACTION_BUTTON_CLASS).toContain("md:min-h-0")
  })

  it("clips preview peek and uses live meta pane token", async () => {
    const {
      LIVE_OFFERS_CARD_CLASS,
      LIVE_OFFERS_CARD_META_CLASS,
      LIVE_OFFERS_CARD_PREVIEW_CLASS,
      LIVE_OFFERS_CARD_PREVIEW_OVERLAY_CLASS,
    } = await import("./liveOffersSectionPresentation")
    expect(LIVE_OFFERS_CARD_CLASS).toContain("max-h-[257px]")
    expect(LIVE_OFFERS_CARD_PREVIEW_CLASS).toContain("overflow-hidden")
    expect(LIVE_OFFERS_CARD_PREVIEW_CLASS).toContain("items-start")
    expect(LIVE_OFFERS_CARD_PREVIEW_CLASS).toContain("justify-start")
    expect(LIVE_OFFERS_CARD_PREVIEW_OVERLAY_CLASS).toContain("z-10")
    expect(LIVE_OFFERS_CARD_PREVIEW_OVERLAY_CLASS).toContain("0.82")
    expect(LIVE_OFFERS_CARD_META_CLASS).toContain(
      "bg-op-background-secondary"
    )
  })
})

describe("liveOffersSectionPresentation preview width", () => {
  it("stretches the cropped guest preview to the pane width", async () => {
    const { LIVE_OFFERS_CARD_PREVIEW_SCALE_CLASS } = await import(
      "./liveOffersSectionPresentation"
    )
    expect(LIVE_OFFERS_CARD_PREVIEW_SCALE_CLASS).toContain("w-full")
    expect(LIVE_OFFERS_CARD_PREVIEW_SCALE_CLASS).toContain("z-0")
    expect(LIVE_OFFERS_CARD_PREVIEW_SCALE_CLASS).not.toContain("scale-[")
  })
})

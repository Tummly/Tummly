import { describe, expect, it } from "vitest"

import {
  LIVE_OFFERS_CARD_ACTIONS_CLASS,
  LIVE_OFFERS_CARD_CLASS,
  LIVE_OFFERS_CARD_META_CLASS,
  LIVE_OFFERS_CARD_STATUS_BADGE_CLASS,
  LIVE_OFFERS_CARDS_STACK_CLASS,
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
    expect(LIVE_OFFERS_SECTION_CLASS).toContain("bg-op-surface-primary")
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

  it("uses Figma meta-only live card layout", () => {
    expect(LIVE_OFFERS_CARDS_STACK_CLASS).toContain("gap-[30px]")
    expect(LIVE_OFFERS_CARDS_STACK_CLASS).toContain("lg:flex-row")
    expect(LIVE_OFFERS_CARD_CLASS).toContain("bg-op-background-secondary")
    expect(LIVE_OFFERS_CARD_CLASS).toContain("p-3.5")
    expect(LIVE_OFFERS_CARD_CLASS).toContain("sm:items-end")
    expect(LIVE_OFFERS_CARD_CLASS).toContain("sm:justify-between")
    expect(LIVE_OFFERS_CARD_CLASS).not.toContain("max-h-[257px]")
    expect(LIVE_OFFERS_CARD_META_CLASS).toContain("flex-col")
    expect(LIVE_OFFERS_CARD_STATUS_BADGE_CLASS).toContain("bg-op-card-background")
    expect(LIVE_OFFERS_CARD_ACTIONS_CLASS).toContain("gap-3")
  })
})

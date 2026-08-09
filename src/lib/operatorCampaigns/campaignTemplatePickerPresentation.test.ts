import { describe, expect, it } from "vitest"

import {
  CAMPAIGN_TEMPLATE_CARD_DESCRIPTION_CLASS,
  CAMPAIGN_TEMPLATE_CARD_META_ROW_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_BODY_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_CONTENT_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_OVERLAY_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_SEARCH_FIELD_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_SUBTITLE_CLASS,
} from "./campaignTemplatePickerPresentation"

/** Figma Main Bg / Subtitle — Operator gray-550 (#7c7c7c). */
const SUBTITLE_GREY_TOKEN = "text-[var(--op-color-gray-550)]"

describe("campaignTemplatePickerPresentation", () => {
  it("uses Main Bg subtitle grey for dialog subtitle, card description, and meta", () => {
    expect(CAMPAIGN_TEMPLATE_PICKER_SUBTITLE_CLASS).toContain(SUBTITLE_GREY_TOKEN)
    expect(CAMPAIGN_TEMPLATE_CARD_DESCRIPTION_CLASS).toContain(SUBTITLE_GREY_TOKEN)
    expect(CAMPAIGN_TEMPLATE_CARD_META_ROW_CLASS).toContain(SUBTITLE_GREY_TOKEN)
    expect(CAMPAIGN_TEMPLATE_CARD_DESCRIPTION_CLASS).not.toContain(
      "text-op-card-subtitle-color"
    )
    expect(CAMPAIGN_TEMPLATE_CARD_META_ROW_CLASS).not.toContain(
      "text-op-card-subtitle-color"
    )
  })

  it("raises picker overlay with content on the Operator z-ladder", () => {
    expect(CAMPAIGN_TEMPLATE_PICKER_CONTENT_CLASS).toContain("z-[140]")
    expect(CAMPAIGN_TEMPLATE_PICKER_OVERLAY_CLASS).toContain("z-[140]")
  })

  it("keeps dialog height stable across loading and loaded states", () => {
    expect(CAMPAIGN_TEMPLATE_PICKER_CONTENT_CLASS).toContain(
      "min-h-[min(90vh,720px)]"
    )
    expect(CAMPAIGN_TEMPLATE_PICKER_BODY_CLASS).toContain("flex-1")
  })

  it("keeps shell padding outside the scroll body so the bottom is not clipped", () => {
    expect(CAMPAIGN_TEMPLATE_PICKER_CONTENT_CLASS).toContain("p-8")
    expect(CAMPAIGN_TEMPLATE_PICKER_CONTENT_CLASS).toContain("overflow-hidden")
    expect(CAMPAIGN_TEMPLATE_PICKER_CONTENT_CLASS).not.toContain(
      "overflow-y-auto"
    )
    expect(CAMPAIGN_TEMPLATE_PICKER_BODY_CLASS).toContain("overflow-y-auto")
  })

  it("uses theme-aware header search tokens on the search field", () => {
    expect(CAMPAIGN_TEMPLATE_PICKER_SEARCH_FIELD_CLASS).toContain(
      "bg-op-header-search-background"
    )
    expect(CAMPAIGN_TEMPLATE_PICKER_SEARCH_FIELD_CLASS).toContain(
      "placeholder:text-op-header-search-text"
    )
    expect(CAMPAIGN_TEMPLATE_PICKER_SEARCH_FIELD_CLASS).not.toContain(
      "op-color-gray-985"
    )
    expect(CAMPAIGN_TEMPLATE_PICKER_SEARCH_FIELD_CLASS).not.toContain(
      "op-color-gray-600"
    )
  })
})

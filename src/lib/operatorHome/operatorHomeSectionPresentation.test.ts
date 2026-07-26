import { describe, expect, it } from "vitest"

import {
  LATEST_ACTIVITY_FOOTER_CLASS,
  LATEST_ACTIVITY_HEADER_CLASS,
  LATEST_ACTIVITY_ROW_CLASS,
  LATEST_ACTIVITY_STEPPED_PADDING_CLASS,
  LATEST_ACTIVITY_TITLE_CLASS,
  LATEST_ACTIVITY_VIEW_ALL_LABEL,
  NEEDS_ATTENTION_EMPTY_COPY,
  OPERATOR_HOME_CARD_CLASS,
  OPERATOR_HOME_CARD_PADDED_CLASS,
  OPERATOR_HOME_CARD_STACK_CLASS,
  OPERATOR_HOME_CHROME_BUTTON_CLASS,
  OPERATOR_HOME_GRAY_SHELL_TITLE_CLASS,
  OPERATOR_HOME_WHITE_CARD_CLASS,
  OPERATOR_HOME_WHITE_CARD_TITLE_CLASS,
  RECOMMENDED_EMPTY_COPY,
  RECOMMENDED_HEADER_CLASS,
  RECOMMENDED_INNER_PANEL_CLASS,
  RECOMMENDED_SECTION_CLASS,
  WEEKLY_BRIEF_EMPTY_HELPER,
  WEEKLY_BRIEF_EMPTY_HELPER_CLASS,
  WEEKLY_BRIEF_EMPTY_TITLE,
  WEEKLY_BRIEF_EMPTY_TITLE_CLASS,
  WEEKLY_BRIEF_HEADER_CLASS,
  WEEKLY_BRIEF_SECTION_CLASS,
  WEEKLY_BRIEF_SUBTITLE,
} from "./operatorHomeSectionPresentation"

describe("operatorHomeSectionPresentation", () => {
  it("uses shared Figma card chrome with responsive padding", () => {
    expect(OPERATOR_HOME_CARD_CLASS).toContain("border-op-card-border")
    expect(OPERATOR_HOME_CARD_CLASS).toContain("bg-op-card-background")
    expect(OPERATOR_HOME_CARD_CLASS).toContain("rounded-op-lg")
    expect(OPERATOR_HOME_CARD_PADDED_CLASS).toContain("p-4")
    expect(OPERATOR_HOME_CARD_PADDED_CLASS).toContain("sm:p-5")
    expect(OPERATOR_HOME_CARD_PADDED_CLASS).toContain("md:p-6")
    expect(OPERATOR_HOME_CARD_PADDED_CLASS).toContain("gap-6")
    expect(OPERATOR_HOME_CARD_PADDED_CLASS).toContain("sm:gap-8")
    expect(OPERATOR_HOME_CARD_PADDED_CLASS).toContain("md:gap-10")
    expect(OPERATOR_HOME_CARD_STACK_CLASS).toContain("py-[25px]")
    expect(OPERATOR_HOME_WHITE_CARD_CLASS).toContain("px-4")
    expect(OPERATOR_HOME_WHITE_CARD_CLASS).toContain("md:py-[25px]")
  })

  it("uses responsive header and chrome button metrics", () => {
    expect(OPERATOR_HOME_GRAY_SHELL_TITLE_CLASS).toContain("text-lg")
    expect(OPERATOR_HOME_GRAY_SHELL_TITLE_CLASS).toContain("sm:text-xl")
    expect(OPERATOR_HOME_GRAY_SHELL_TITLE_CLASS).toContain("font-bold")
    expect(OPERATOR_HOME_WHITE_CARD_TITLE_CLASS).toContain("text-lg")
    expect(OPERATOR_HOME_WHITE_CARD_TITLE_CLASS).toContain("font-semibold")
    expect(OPERATOR_HOME_CHROME_BUTTON_CLASS).toContain("size-[42px]")
    expect(OPERATOR_HOME_CHROME_BUTTON_CLASS).toContain(
      "bg-op-button-collapse-background"
    )
    expect(OPERATOR_HOME_CHROME_BUTTON_CLASS).toContain("cursor-pointer")
    expect(OPERATOR_HOME_CHROME_BUTTON_CLASS).toContain("rounded-op-sm")
  })

  it("uses Figma weekly brief empty copy", () => {
    expect(WEEKLY_BRIEF_SUBTITLE).toContain("campaign performance")
    expect(WEEKLY_BRIEF_EMPTY_TITLE).toBe(
      "Your first weekly brief will be ready on Monday"
    )
    expect(WEEKLY_BRIEF_EMPTY_HELPER).toContain("summarise guest activity")
  })

  it("uses Figma recommended empty shell", () => {
    expect(RECOMMENDED_SECTION_CLASS).toContain("px-4")
    expect(RECOMMENDED_SECTION_CLASS).toContain("md:py-[25px]")
    expect(RECOMMENDED_HEADER_CLASS).toContain("pb-6")
    expect(RECOMMENDED_INNER_PANEL_CLASS).toContain("p-4")
    expect(RECOMMENDED_INNER_PANEL_CLASS).toContain("sm:p-5")
    expect(RECOMMENDED_INNER_PANEL_CLASS).toContain(
      "dark:bg-op-background-secondary"
    )
    expect(RECOMMENDED_EMPTY_COPY).toContain("recommended action")
    expect(NEEDS_ATTENTION_EMPTY_COPY).toBe(
      "Nothing needs attention right now."
    )
  })

  it("uses Figma weekly brief empty shell without CTA", () => {
    expect(WEEKLY_BRIEF_SECTION_CLASS).toContain("py-[25px]")
    expect(WEEKLY_BRIEF_SECTION_CLASS).toContain("px-px")
    expect(WEEKLY_BRIEF_HEADER_CLASS).toContain("border-b")
    expect(WEEKLY_BRIEF_HEADER_CLASS).toContain("px-4")
    expect(WEEKLY_BRIEF_HEADER_CLASS).toContain("md:px-6")
    expect(WEEKLY_BRIEF_EMPTY_TITLE_CLASS).toContain("font-semibold")
    expect(WEEKLY_BRIEF_EMPTY_TITLE_CLASS).toContain(
      "text-op-card-subtitle-color"
    )
    expect(WEEKLY_BRIEF_EMPTY_HELPER_CLASS).toContain("font-normal")
  })

  it("uses responsive latest activity chrome", () => {
    expect(LATEST_ACTIVITY_STEPPED_PADDING_CLASS).toBe(
      "px-4 sm:px-5 md:px-6"
    )
    expect(LATEST_ACTIVITY_TITLE_CLASS).toContain("text-lg sm:text-xl")
    expect(LATEST_ACTIVITY_HEADER_CLASS).toContain("flex-col")
    expect(LATEST_ACTIVITY_HEADER_CLASS).toContain("sm:flex-row")
    expect(LATEST_ACTIVITY_HEADER_CLASS).toContain(
      LATEST_ACTIVITY_STEPPED_PADDING_CLASS
    )
    expect(LATEST_ACTIVITY_ROW_CLASS).toContain("border-b")
    expect(LATEST_ACTIVITY_ROW_CLASS).not.toContain("last:border-b-0")
    expect(LATEST_ACTIVITY_FOOTER_CLASS).toBe(
      LATEST_ACTIVITY_STEPPED_PADDING_CLASS
    )
    expect(LATEST_ACTIVITY_VIEW_ALL_LABEL).toBe("View all activity")
  })
})

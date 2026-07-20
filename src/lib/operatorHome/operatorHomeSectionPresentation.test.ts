import { describe, expect, it } from "vitest"

import {
  NEEDS_ATTENTION_EMPTY_COPY,
  OPERATOR_HOME_CARD_CLASS,
  OPERATOR_HOME_CARD_PADDED_CLASS,
  OPERATOR_HOME_CARD_STACK_CLASS,
  OPERATOR_HOME_CHROME_BUTTON_CLASS,
  OPERATOR_HOME_TITLE_CLASS,
  RECOMMENDED_EMPTY_COPY,
  RECOMMENDED_HEADER_CLASS,
  RECOMMENDED_INNER_PANEL_CLASS,
  WEEKLY_BRIEF_EMPTY_HELPER,
  WEEKLY_BRIEF_EMPTY_TITLE,
  WEEKLY_BRIEF_SUBTITLE,
} from "./operatorHomeSectionPresentation"

describe("operatorHomeSectionPresentation", () => {
  it("uses shared Figma card chrome", () => {
    expect(OPERATOR_HOME_CARD_CLASS).toContain("border-[#e5e5e5]")
    expect(OPERATOR_HOME_CARD_CLASS).toContain("bg-[#f8f8f8]")
    expect(OPERATOR_HOME_CARD_CLASS).toContain("rounded-md")
    expect(OPERATOR_HOME_CARD_PADDED_CLASS).toContain("p-6")
    expect(OPERATOR_HOME_CARD_PADDED_CLASS).toContain("gap-10")
    expect(OPERATOR_HOME_CARD_STACK_CLASS).toContain("py-[25px]")
  })

  it("uses Figma header and chrome button metrics", () => {
    expect(OPERATOR_HOME_TITLE_CLASS).toContain("text-xl")
    expect(OPERATOR_HOME_TITLE_CLASS).toContain("font-bold")
    expect(OPERATOR_HOME_CHROME_BUTTON_CLASS).toContain("size-[42px]")
    expect(OPERATOR_HOME_CHROME_BUTTON_CLASS).toContain("bg-[#ebebeb]")
  })

  it("uses Figma weekly brief empty copy", () => {
    expect(WEEKLY_BRIEF_SUBTITLE).toContain("campaign performance")
    expect(WEEKLY_BRIEF_EMPTY_TITLE).toBe(
      "Your first weekly brief will be ready on Monday"
    )
    expect(WEEKLY_BRIEF_EMPTY_HELPER).toContain("summarise guest activity")
  })

  it("uses Figma recommended empty shell", () => {
    expect(RECOMMENDED_HEADER_CLASS).toContain("px-6")
    expect(RECOMMENDED_INNER_PANEL_CLASS).toContain("dark:bg-[#202020]")
    expect(RECOMMENDED_EMPTY_COPY).toContain("recommended action")
    expect(NEEDS_ATTENTION_EMPTY_COPY).toBe(
      "Nothing needs attention right now."
    )
  })
})

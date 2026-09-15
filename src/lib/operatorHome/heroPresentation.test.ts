import { describe, expect, it } from "vitest"

import type { ActivationPeriodBadgeCopy } from "./activationPeriod"
import {
  formatActivationPeriodBadgeAriaLabel,
  formatActivationPeriodBadgeFullVisibleText,
  OPERATOR_HOME_HERO_BADGE_CLASS,
  OPERATOR_HOME_HERO_CTA_ROW_CLASS,
  OPERATOR_HOME_HERO_INNER_CLASS,
  OPERATOR_HOME_HERO_PRIMARY_BUTTON_CLASS,
  OPERATOR_HOME_HERO_SECONDARY_BUTTON_CLASS,
  OPERATOR_HOME_HERO_SUBTITLE_CLASS,
  OPERATOR_HOME_HERO_TITLE_CLASS,
} from "./heroPresentation"

const sampleBadge: ActivationPeriodBadgeCopy = {
  remaining: "25 days left",
  endsOn: "13 Aug 2026",
  tone: "default",
}

describe("heroPresentation", () => {
  it("keeps the hero as a bare row (no card chrome)", () => {
    expect(OPERATOR_HOME_HERO_INNER_CLASS).not.toContain("border")
    expect(OPERATOR_HOME_HERO_INNER_CLASS).not.toContain("bg-op-card")
    expect(OPERATOR_HOME_HERO_INNER_CLASS).not.toContain("rounded")
    expect(OPERATOR_HOME_HERO_INNER_CLASS).toContain("lg:flex-row")
    expect(OPERATOR_HOME_HERO_INNER_CLASS).toContain("lg:items-end")
  })

  it("uses 40px minus HomeBody gap-5 under the hero", () => {
    expect(OPERATOR_HOME_HERO_INNER_CLASS).toContain("pb-5")
    expect(OPERATOR_HOME_HERO_INNER_CLASS).not.toContain("lg:pb-[50px]")
  })

  it("uses serif 36px title and title-colour body at sm+", () => {
    expect(OPERATOR_HOME_HERO_TITLE_CLASS).toContain("font-serif")
    expect(OPERATOR_HOME_HERO_TITLE_CLASS).toContain("text-2xl")
    expect(OPERATOR_HOME_HERO_TITLE_CLASS).toContain("sm:text-[36px]")
    expect(OPERATOR_HOME_HERO_TITLE_CLASS).toContain("text-op-card-title-color")
    expect(OPERATOR_HOME_HERO_SUBTITLE_CLASS).toContain(
      "text-op-card-title-color"
    )
  })

  it("wraps hero CTAs with auto-width and 44px hit area below md", () => {
    expect(OPERATOR_HOME_HERO_CTA_ROW_CLASS).toContain("flex-wrap")
    expect(OPERATOR_HOME_HERO_CTA_ROW_CLASS).toContain("gap-3")
    expect(OPERATOR_HOME_HERO_PRIMARY_BUTTON_CLASS).toContain("max-md:min-h-11")
    expect(OPERATOR_HOME_HERO_PRIMARY_BUTTON_CLASS).toContain("max-md:min-w-11")
    expect(OPERATOR_HOME_HERO_PRIMARY_BUTTON_CLASS).toContain("!text-white")
    expect(OPERATOR_HOME_HERO_SECONDARY_BUTTON_CLASS).toContain("max-md:min-h-11")
    expect(OPERATOR_HOME_HERO_SECONDARY_BUTTON_CLASS).toContain("max-md:min-w-11")
  })

  it("keeps activation badge single-line", () => {
    expect(OPERATOR_HOME_HERO_BADGE_CLASS).toContain("whitespace-nowrap")
    expect(OPERATOR_HOME_HERO_BADGE_CLASS).not.toContain("flex-wrap")
    expect(OPERATOR_HOME_HERO_BADGE_CLASS).not.toContain("whitespace-normal")
  })

  it("builds full visible badge copy for md and up", () => {
    expect(formatActivationPeriodBadgeFullVisibleText(sampleBadge)).toBe(
      "25 days left in your free trial · Ends 13 Aug 2026"
    )
  })

  it("keeps full aria-label regardless of visible breakpoint copy", () => {
    expect(formatActivationPeriodBadgeAriaLabel(sampleBadge)).toBe(
      "25 days left in your free trial. Ends 13 Aug 2026"
    )
  })
})

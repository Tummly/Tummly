import { describe, expect, it } from "vitest"

import {
  FAQS_PAGE_DEFAULT_OPEN_VALUE,
  FAQS_PAGE_HERO,
  FAQS_PAGE_SECTIONS,
  filterFaqsPageSections,
} from "./faqsPage"

describe("faqsPage content", () => {
  it("exposes Figma hero title, body, and search placeholder", () => {
    expect(FAQS_PAGE_HERO.title).toBe("Questions? Start here.")
    expect(FAQS_PAGE_HERO.body.length).toBeGreaterThan(0)
    expect(FAQS_PAGE_HERO.searchPlaceholder).toBe("Search Tummly FAQs")
  })

  it("has exactly seven sections with required stub ids", () => {
    expect(FAQS_PAGE_SECTIONS).toHaveLength(7)
    expect(FAQS_PAGE_SECTIONS.map((s) => s.id)).toEqual([
      "getting-started",
      "pilot-pricing",
      "guest-loop",
      "guest-list",
      "offers-campaigns",
      "ai-assistant",
      "trust-privacy",
    ])
  })

  it("uses stub answers for AI Assistant and Trust & Privacy", () => {
    const ai = FAQS_PAGE_SECTIONS.find((s) => s.id === "ai-assistant")
    const trust = FAQS_PAGE_SECTIONS.find((s) => s.id === "trust-privacy")
    expect(ai?.items).toHaveLength(1)
    expect(trust?.items).toHaveLength(1)
    expect(ai?.items[0]?.answer).toMatch(/published soon/i)
    expect(trust?.items[0]?.answer).toMatch(/published soon/i)
  })

  it("marks non-stub answers as placeholders", () => {
    for (const section of FAQS_PAGE_SECTIONS) {
      if (section.id === "ai-assistant" || section.id === "trust-privacy") {
        continue
      }
      expect(section.items.length).toBeGreaterThan(0)
      for (const item of section.items) {
        expect(item.answer).toContain("Placeholder answer — final copy pending.")
      }
    }
  })

  it("default open value matches the first item of the first section", () => {
    const first = FAQS_PAGE_SECTIONS[0]?.items[0]
    expect(first).toBeDefined()
    expect(FAQS_PAGE_DEFAULT_OPEN_VALUE).toBe(
      `${FAQS_PAGE_SECTIONS[0]!.id}::${first!.id}`,
    )
  })

  it("filters sections by search query", () => {
    const filtered = filterFaqsPageSections(FAQS_PAGE_SECTIONS, "POS")
    expect(filtered).toHaveLength(1)
    expect(filtered[0]?.id).toBe("getting-started")
    expect(filtered[0]?.items.map((i) => i.id)).toEqual(["need-pos"])
  })
})

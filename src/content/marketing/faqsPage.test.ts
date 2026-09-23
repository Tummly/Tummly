import { describe, expect, it } from "vitest"

import {
  FAQS_PAGE_DEFAULT_OPEN_VALUE,
  FAQS_PAGE_HERO,
  FAQS_PAGE_SECTIONS,
  FAQS_PAGE_SIGN_UP_CTA,
  filterFaqsPageSections,
} from "./faqsPage"

describe("faqsPage content", () => {
  it("exposes Figma hero title, body, and search placeholder", () => {
    expect(FAQS_PAGE_HERO.title).toBe("Questions? Start here.")
    expect(FAQS_PAGE_HERO.body.length).toBeGreaterThan(0)
    expect(FAQS_PAGE_HERO.searchPlaceholder).toBe("Search Tummly FAQs")
  })

  it("exposes Figma Sign up CTA copy", () => {
    expect(FAQS_PAGE_SIGN_UP_CTA.title).toBe(
      "Ready to see Guest Loop in a real restaurant?",
    )
    expect(FAQS_PAGE_SIGN_UP_CTA.primaryLabel).toBe("Start 30-day Pilot")
    expect(FAQS_PAGE_SIGN_UP_CTA.secondaryLabel).toBe("See pricing")
    expect(FAQS_PAGE_SIGN_UP_CTA.badge).toContain("No payment card required")
  })

  it("has exactly seven sections matching Figma order", () => {
    expect(FAQS_PAGE_SECTIONS).toHaveLength(7)
    expect(FAQS_PAGE_SECTIONS.map((s) => s.id)).toEqual([
      "getting-started",
      "pilot-pricing",
      "qr-materials",
      "feedback-guests",
      "campaigns-offers",
      "ai-assistant",
      "trust-privacy",
    ])
  })

  it("uses Figma questions for AI Assistant and Trust & privacy with stub answers", () => {
    const ai = FAQS_PAGE_SECTIONS.find((s) => s.id === "ai-assistant")
    const trust = FAQS_PAGE_SECTIONS.find((s) => s.id === "trust-privacy")
    expect(ai?.items).toHaveLength(6)
    expect(trust?.items).toHaveLength(6)
    expect(ai?.items[0]?.question).toBe("What can Tummly AI do?")
    expect(trust?.items[0]?.question).toBe(
      "How does Tummly handle guest permissions?",
    )
    for (const item of [...(ai?.items ?? []), ...(trust?.items ?? [])]) {
      expect(item.answer).toMatch(/published soon/i)
    }
  })

  it("ships real Figma answers for non-stub sections", () => {
    for (const section of FAQS_PAGE_SECTIONS) {
      if (section.id === "ai-assistant" || section.id === "trust-privacy") {
        continue
      }
      expect(section.items.length).toBeGreaterThan(0)
      for (const item of section.items) {
        expect(item.answer).not.toMatch(/placeholder|published soon/i)
        expect(item.answer.length).toBeGreaterThan(20)
      }
    }
  })

  it("includes Figma CTA rows for pilot signup and pricing compare", () => {
    const howToStart = FAQS_PAGE_SECTIONS[0]?.items.find(
      (item) => item.id === "how-to-start",
    )
    const cost = FAQS_PAGE_SECTIONS[1]?.items.find(
      (item) => item.id === "how-much-cost",
    )
    expect(howToStart?.cta).toEqual({
      label: "Start 30-day Pilot →",
      to: "/signup",
    })
    expect(cost?.cta).toEqual({
      label: "Compare all plans →",
      to: "/pricing",
    })
  })

  it("default open value matches the first item of the first section", () => {
    const first = FAQS_PAGE_SECTIONS[0]?.items[0]
    expect(first).toBeDefined()
    expect(FAQS_PAGE_DEFAULT_OPEN_VALUE).toBe(
      `${FAQS_PAGE_SECTIONS[0]!.id}::${first!.id}`,
    )
  })

  it("filters sections by search query", () => {
    const filtered = filterFaqsPageSections(
      FAQS_PAGE_SECTIONS,
      "POS integration",
    )
    expect(filtered).toHaveLength(1)
    expect(filtered[0]?.id).toBe("getting-started")
    expect(filtered[0]?.items.map((i) => i.id)).toEqual(["need-pos"])
  })
})

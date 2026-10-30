import { describe, expect, it } from "vitest"

import {
  QR_MATERIALS_FAQS,
  QR_MATERIALS_FEEDBACK_STEPS,
  QR_MATERIALS_PRICING,
  QR_MATERIALS_SIGN_UP_CTA,
} from "@/content/marketing/qrMaterialsPage"

describe("QR materials page content", () => {
  it("shows a location-limit line under every plan, Pilot keeps the card note", () => {
    const sublines = Object.fromEntries(
      QR_MATERIALS_PRICING.cards.map((card) => [card.id, card.subline]),
    )

    expect(sublines.Pilot).toBe("No payment card required.")
    expect(sublines.Starter).toBe("1 location only")
    expect(sublines.Growth).toMatch(/^Up to \d+ locations$/)
    expect(sublines.Group).toMatch(/^Up to \d+ locations$/)
  })

  it("lists four numbered steps and unique FAQ ids", () => {
    expect(QR_MATERIALS_FEEDBACK_STEPS.steps.map((step) => step.stepLabel)).toEqual(
      ["Step 1", "Step 2", "Step 3", "Step 4"],
    )

    const ids = QR_MATERIALS_FAQS.items.map((item) => item.id)
    expect(new Set(ids).size).toBe(ids.length)
    expect(ids).toHaveLength(7)
  })

  it("uses the read-style legal note on the bottom CTA", () => {
    expect(QR_MATERIALS_SIGN_UP_CTA.legalNote).toBe("read")
    expect(QR_MATERIALS_SIGN_UP_CTA.planIntent).toEqual({ plan: "Pilot" })
  })
})

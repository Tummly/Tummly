import { describe, expect, it } from "vitest"

import {
  buildHelpCentreContactMessage,
  getContactTopicFieldFlags,
  resolveHelpCentreContactBusinessName,
} from "@/lib/helpCentreContactForm"
import type { HelpCentreContactFormValues } from "@/schemas/helpCentreContact"

function values(
  overrides: Partial<HelpCentreContactFormValues>
): HelpCentreContactFormValues {
  return {
    topic: "something-else",
    submitterName: "Jane Doe",
    submitterEmail: "jane@example.com",
    businessName: "",
    locationCount: "",
    alreadyUsingTummly: "",
    alternateEmail: "",
    message: "Hello support",
    ...overrides,
  }
}

describe("getContactTopicFieldFlags", () => {
  it("shows QR hint and already-using for QR materials", () => {
    const flags = getContactTopicFieldFlags("qr-materials-or-starter-kit")
    expect(flags.showQrOrderHint).toBe(true)
    expect(flags.showAlreadyUsing).toBe(true)
    expect(flags.showLocationCount).toBe(true)
  })

  it("hides business fields for privacy requests", () => {
    const flags = getContactTopicFieldFlags("privacy-or-data-request")
    expect(flags.showBusinessName).toBe(false)
    expect(flags.showLocationCount).toBe(false)
  })
})

describe("buildHelpCentreContactMessage", () => {
  it("returns the body alone when there are no extras", () => {
    expect(buildHelpCentreContactMessage(values({}))).toBe("Hello support")
  })

  it("prepends location and already-using lines for QR materials", () => {
    const message = buildHelpCentreContactMessage(
      values({
        topic: "qr-materials-or-starter-kit",
        alreadyUsingTummly: "yes",
        locationCount: "2-3",
        message: "Need new stickers",
      })
    )

    expect(message).toContain("Already using Tummly: Yes")
    expect(message).toContain("Locations operated: 2–3 Locations")
    expect(message).toContain("Need new stickers")
  })

  it("prepends alternate billing email", () => {
    const message = buildHelpCentreContactMessage(
      values({
        topic: "billing-or-subscription",
        alternateEmail: "billing@example.com",
        message: "Invoice question",
      })
    )

    expect(message).toContain("Billing email: billing@example.com")
    expect(message).toContain("Invoice question")
  })
})

describe("resolveHelpCentreContactBusinessName", () => {
  it("returns trimmed business name for sales topics", () => {
    expect(
      resolveHelpCentreContactBusinessName(
        values({
          topic: "starting-with-tummly",
          businessName: "  Cafe  ",
        })
      )
    ).toBe("Cafe")
  })

  it("returns empty when the topic hides business name", () => {
    expect(
      resolveHelpCentreContactBusinessName(
        values({
          topic: "something-else",
          businessName: "Should ignore",
        })
      )
    ).toBe("")
  })
})

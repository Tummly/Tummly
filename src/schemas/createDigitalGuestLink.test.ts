import { describe, expect, it } from "vitest"

import {
  createDigitalGuestLinkFormSchema,
  createDigitalGuestLinkFormSchemaWithLocation,
} from "@/schemas/createDigitalGuestLink"

describe("createDigitalGuestLinkFormSchema", () => {
  it("accepts null connectedOfferId as optional", () => {
    const result = createDigitalGuestLinkFormSchema.safeParse({
      linkName: "WhatsApp promo",
      internalDescription: "",
      channel: "WhatsApp",
      status: "Active",
      connectedOfferId: null,
    })

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.data.connectedOfferId).toBeNull()
    }
  })

  it("accepts a selected connectedOfferId", () => {
    const result = createDigitalGuestLinkFormSchema.safeParse({
      linkName: "WhatsApp promo",
      internalDescription: "",
      channel: "WhatsApp",
      status: "Active",
      connectedOfferId: 88,
    })

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.data.connectedOfferId).toBe(88)
    }
  })

  it("defaults connectedOfferId to null when omitted", () => {
    const result = createDigitalGuestLinkFormSchema.safeParse({
      linkName: "WhatsApp promo",
      internalDescription: "",
      channel: "WhatsApp",
      status: "Active",
    })

    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.data.connectedOfferId).toBeNull()
    }
  })

  it("still requires location when location schema is on", () => {
    const result = createDigitalGuestLinkFormSchemaWithLocation(true).safeParse({
      linkName: "WhatsApp promo",
      internalDescription: "",
      channel: "WhatsApp",
      status: "Active",
      connectedOfferId: 88,
      locationId: null,
    })

    expect(result.success).toBe(false)
  })
})

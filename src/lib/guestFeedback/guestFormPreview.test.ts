import { describe, expect, it } from "vitest"

import {
  buildGuestFormPreviewUrl,
  GUEST_FORM_PREVIEW_PARAM,
  isGuestFormPreviewParam,
} from "./guestFormPreview"

describe("buildGuestFormPreviewUrl", () => {
  it("adds the preview flag to a scan URL", () => {
    expect(buildGuestFormPreviewUrl("https://guest.example/scan/abc")).toBe(
      `https://guest.example/scan/abc?${GUEST_FORM_PREVIEW_PARAM}=true`
    )
  })

  it("appends with & when the scan URL already has a query", () => {
    expect(
      buildGuestFormPreviewUrl("https://guest.example/scan/abc?lang=en")
    ).toBe(`https://guest.example/scan/abc?lang=en&${GUEST_FORM_PREVIEW_PARAM}=true`)
  })
})

describe("isGuestFormPreviewParam", () => {
  it("is true only for preview=true", () => {
    expect(isGuestFormPreviewParam("true")).toBe(true)
    expect(isGuestFormPreviewParam("false")).toBe(false)
    expect(isGuestFormPreviewParam("1")).toBe(false)
    expect(isGuestFormPreviewParam(null)).toBe(false)
  })
})

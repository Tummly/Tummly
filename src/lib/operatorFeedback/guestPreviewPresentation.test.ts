import { describe, expect, it } from "vitest"

import {
  GUEST_PREVIEW_CONTROL_LABEL,
  GUEST_PREVIEW_EDIT_TEXT_LABEL,
  GUEST_PREVIEW_HEADING,
  guestPreviewMockTitle,
} from "./guestPreviewPresentation"

describe("guestPreviewPresentation", () => {
  it("keeps Guest preview Review chrome copy as in Figma", () => {
    expect(GUEST_PREVIEW_HEADING).toBe("Guest preview")
    expect(GUEST_PREVIEW_CONTROL_LABEL).toBe("Preview")
    expect(GUEST_PREVIEW_EDIT_TEXT_LABEL).toBe("Edit text")
  })

  it("labels the mock by channel", () => {
    expect(guestPreviewMockTitle("email")).toBe("Email preview")
    expect(guestPreviewMockTitle("sms")).toBe("SMS preview")
    expect(guestPreviewMockTitle(null)).toBe("Email preview")
  })
})

import { describe, expect, it } from "vitest"

import {
  GUEST_RESPONSE_AI_ACTION_METERING_LABEL,
  GUEST_RESPONSE_CHOOSER_CARD_CLASS,
  GUEST_RESPONSE_PREPARE_ACTION_LABEL,
  GUEST_RESPONSE_PREPARE_DESCRIPTION,
  GUEST_RESPONSE_PREPARE_TITLE,
  GUEST_RESPONSE_PREPARING_OVERLAY_DESCRIPTION,
  GUEST_RESPONSE_PREPARING_OVERLAY_TITLE,
  GUEST_RESPONSE_REWRITE_AI_LABEL,
  GUEST_RESPONSE_REWRITE_RETRY_LABEL,
  GUEST_RESPONSE_STEP_DESCRIPTION,
  GUEST_RESPONSE_STEP_HEADING,
  GUEST_RESPONSE_WRITE_MANUAL_ACTION_LABEL,
  GUEST_RESPONSE_WRITE_MANUAL_DESCRIPTION,
  GUEST_RESPONSE_WRITE_MANUAL_TITLE,
} from "./guestResponseChooserPresentation"

describe("guestResponseChooserPresentation", () => {
  it("keeps Guest response chooser copy as in Figma", () => {
    expect(GUEST_RESPONSE_STEP_HEADING).toBe("Guest response")
    expect(GUEST_RESPONSE_STEP_DESCRIPTION).toBe(
      "Prepare and edit the private response that will be sent to the guest."
    )
    expect(GUEST_RESPONSE_PREPARE_TITLE).toBe("Prepare with AI")
    expect(GUEST_RESPONSE_PREPARE_DESCRIPTION).toBe(
      "Use the feedback and your confirmed information to prepare an editable response."
    )
    expect(GUEST_RESPONSE_PREPARE_ACTION_LABEL).toBe("Prepare response draft")
    expect(GUEST_RESPONSE_AI_ACTION_METERING_LABEL).toBe("Uses 1 AI action")
    expect(GUEST_RESPONSE_WRITE_MANUAL_TITLE).toBe("Write manually")
    expect(GUEST_RESPONSE_WRITE_MANUAL_DESCRIPTION).toBe(
      "Write the complete response yourself without using an AI action."
    )
    expect(GUEST_RESPONSE_WRITE_MANUAL_ACTION_LABEL).toBe(
      "Write response manually"
    )
    expect(GUEST_RESPONSE_REWRITE_AI_LABEL).toBe("Rewrite with AI")
    expect(GUEST_RESPONSE_REWRITE_RETRY_LABEL).toBe("Try Again")
    expect(GUEST_RESPONSE_PREPARING_OVERLAY_TITLE).toBe(
      "Preparing response draft…"
    )
    expect(GUEST_RESPONSE_PREPARING_OVERLAY_DESCRIPTION).toBe(
      "Tummly is using the feedback and confirmed information to prepare a private response."
    )
    expect(GUEST_RESPONSE_CHOOSER_CARD_CLASS).toContain("border-op-divider")
    expect(GUEST_RESPONSE_CHOOSER_CARD_CLASS).toContain("bg-op-color-gray-60")
  })
})

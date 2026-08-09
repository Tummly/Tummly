import { describe, expect, it } from "vitest"

import { GUEST_PREVIEW_OVERLAY_CLASS } from "@/lib/operatorFeedback/guestPreviewPresentation"

import {
  CAMPAIGN_SEND_TEST_DIALOG_CONTENT_CLASS,
  CAMPAIGN_SEND_TEST_DIALOG_OVERLAY_CLASS,
} from "./campaignSendTestPresentation"

function zIndexFromClass(className: string): number | null {
  const match = /z-\[(\d+)\]/.exec(className)
  if (match == null) {
    return null
  }
  return Number(match[1])
}

describe("campaignSendTestPresentation — stacking", () => {
  it("paints Send test above Operator wizard shell and Guest preview overlay", () => {
    const sendTestOverlayZ = zIndexFromClass(
      CAMPAIGN_SEND_TEST_DIALOG_OVERLAY_CLASS
    )
    const sendTestContentZ = zIndexFromClass(
      CAMPAIGN_SEND_TEST_DIALOG_CONTENT_CLASS
    )
    const guestPreviewZ = zIndexFromClass(GUEST_PREVIEW_OVERLAY_CLASS)

    expect(sendTestOverlayZ).toBeGreaterThanOrEqual(140)
    expect(sendTestContentZ).toBeGreaterThanOrEqual(140)
    expect(guestPreviewZ).toBeGreaterThan(130)
    expect(sendTestOverlayZ).toBeGreaterThan(guestPreviewZ!)
    expect(sendTestContentZ).toBeGreaterThan(guestPreviewZ!)
  })
})

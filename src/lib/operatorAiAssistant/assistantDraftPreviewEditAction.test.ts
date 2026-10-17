import { describe, expect, it } from "vitest"

import { assistantDraftPreviewEditAction } from "@/lib/operatorAiAssistant/assistantDraftPreviewEditAction"

describe("assistantDraftPreviewEditAction", () => {
  it("maps campaign draft to review-campaign Edit Draft", () => {
    expect(
      assistantDraftPreviewEditAction({
        kind: "campaign",
        statusLabel: "Draft · Not sent",
        campaignId: 9,
        name: "Thank recent guests",
      })
    ).toEqual({
      type: "review-campaign",
      label: "Edit Draft",
      campaignId: 9,
    })
  })

  it("maps recovery draft to open-recovery Edit Draft", () => {
    expect(
      assistantDraftPreviewEditAction({
        kind: "recovery",
        statusLabel: "Draft · Not sent",
        feedbackId: 12,
        intent: "respond-to-guest",
      })
    ).toEqual({
      type: "open-recovery",
      label: "Edit Draft",
      feedbackId: 12,
      intent: "respond-to-guest",
    })
  })
})

import { describe, expect, it } from "vitest"

import {
  campaignNeedsAttentionBody,
  parseCampaignTerminalReasonId,
} from "./campaignTerminalReasonPresentation"

describe("campaignNeedsAttentionBody", () => {
  it("uses generic failed copy when reason is missing", () => {
    expect(
      campaignNeedsAttentionBody({ status: "failed", terminalReason: null })
    ).toBe("This campaign failed.")
  })

  it("uses generic partially-sent copy when reason is missing", () => {
    expect(
      campaignNeedsAttentionBody({
        status: "partially-sent",
        terminalReason: undefined,
      })
    ).toBe("This campaign was only partially sent.")
  })

  it("names credit-hold exhaustion on partially-sent", () => {
    expect(
      campaignNeedsAttentionBody({
        status: "partially-sent",
        terminalReason: "credit-hold-exhausted",
      })
    ).toBe(
      "This campaign was only partially sent because the reserved credit hold ran out."
    )
  })

  it("names soft-lock on failed", () => {
    expect(
      campaignNeedsAttentionBody({
        status: "failed",
        terminalReason: "soft-locked",
      })
    ).toBe(
      "This campaign failed because the account was soft-locked at send time."
    )
  })

  it("falls back when reason id is unknown", () => {
    expect(
      campaignNeedsAttentionBody({
        status: "failed",
        terminalReason: "not-a-real-reason",
      })
    ).toBe("This campaign failed.")
  })
})

describe("parseCampaignTerminalReasonId", () => {
  it("accepts known ids and rejects unknown", () => {
    expect(parseCampaignTerminalReasonId("zero-eligible")).toBe("zero-eligible")
    expect(parseCampaignTerminalReasonId("  ")).toBeNull()
    expect(parseCampaignTerminalReasonId("nope")).toBeNull()
  })
})

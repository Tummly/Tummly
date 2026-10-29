import { describe, expect, it } from "vitest"

import {
  CAMPAIGN_AUDIENCE_OPTIONS,
  CAMPAIGN_RECENT_REDEEMERS_DEFAULT_LOOKBACK_DAYS,
  campaignAudienceUnavailableReasonLabel,
  formatExcludedReasonLabel,
  formatRecentRedeemersPeriodLabel,
} from "./campaignAudiencePresentation"

describe("formatExcludedReasonLabel", () => {
  it("maps handoff exclusion codes to operator labels", () => {
    expect(formatExcludedReasonLabel("not-granted")).toBe("Not granted")
    expect(formatExcludedReasonLabel("withdrawn")).toBe("Withdrawn")
    expect(formatExcludedReasonLabel("suppressed")).toBe("Suppressed")
    expect(formatExcludedReasonLabel("invalid-contact")).toBe("Invalid contact")
  })

  it("labels an email hard bounce as Previous hard bounce, not Suppressed", () => {
    expect(formatExcludedReasonLabel("previous_hard_bounce")).toBe(
      "Previous hard bounce"
    )
  })

  it("maps legacy wire codes to the same handoff labels", () => {
    expect(formatExcludedReasonLabel("opt-out")).toBe("Not granted")
    expect(formatExcludedReasonLabel("suppression")).toBe("Suppressed")
    expect(formatExcludedReasonLabel("channel")).toBe("Suppressed")
    expect(formatExcludedReasonLabel("channel-disabled")).toBe("Suppressed")
  })
})

describe("Campaign-owned audience cards", () => {
  it("offer-not-redeemed and recent-redeemers are evaluable now", () => {
    const byId = Object.fromEntries(
      CAMPAIGN_AUDIENCE_OPTIONS.map((option) => [option.id, option])
    )

    expect(byId["offer-not-redeemed"]?.unevaluable).toBe(false)
    expect(byId["recent-redeemers"]?.unevaluable).toBe(false)
    expect(byId["no-recent-tummly-activity"]?.unevaluable).toBe(true)
  })

  it("labels the recent redeemers period from the stored days", () => {
    expect(formatRecentRedeemersPeriodLabel(30)).toBe("Last 30 days")
    expect(formatRecentRedeemersPeriodLabel(7)).toBe("Last 7 days")
    expect(CAMPAIGN_RECENT_REDEEMERS_DEFAULT_LOOKBACK_DAYS).toBe(30)
  })

  it("maps the server reason for an Offer-free Campaign to a neutral message", () => {
    expect(campaignAudienceUnavailableReasonLabel("no-offer-attached")).toBe(
      "Attach an Offer to count this audience."
    )
    expect(campaignAudienceUnavailableReasonLabel(null)).toBeNull()
  })
})

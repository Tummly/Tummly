import { describe, expect, it } from "vitest"

import {
  formatBillingActivityCopy,
  formatBillingActivityOccurredAt,
} from "@/lib/operatorBillingCredits/billingActivityPresentation"

describe("formatBillingActivityCopy", () => {
  it("formats credit consumed for a campaign", () => {
    expect(
      formatBillingActivityCopy({
        kind: "credit_consumed",
        channel: "sms",
        qty: 212,
        campaignName: "Quiet Tuesday Boost",
        consumeSource: "campaign",
      })
    ).toBe("212 SMS credits used by Quiet Tuesday Boost.")
  })

  it("formats credit consumed for feedback recovery", () => {
    expect(
      formatBillingActivityCopy({
        kind: "credit_consumed",
        channel: "sms",
        qty: 1,
        consumeSource: "feedback_recovery",
      })
    ).toBe("1 SMS credit used by Feedback recovery.")
  })

  it("formats top-up purchased with en-GB qty", () => {
    expect(
      formatBillingActivityCopy({
        kind: "topup_purchased",
        channel: "sms",
        qty: 1000,
        actorDisplayName: "James Cole",
      })
    ).toBe("1,000 SMS credits added by James Cole.")
  })

  it("formats invoice paid with TM number", () => {
    expect(
      formatBillingActivityCopy({
        kind: "invoice_paid",
        invoiceNo: "TM-2026-000001",
      })
    ).toBe("Invoice TM-2026-000001 paid.")
  })

  it("returns empty for unknown kinds", () => {
    expect(
      formatBillingActivityCopy({
        kind: "member-removed",
      })
    ).toBe("")
  })
})

describe("formatBillingActivityOccurredAt", () => {
  const now = new Date("2026-08-26T12:00:00.000Z")

  it("uses Today, Yesterday, and calendar day in Europe/London", () => {
    expect(
      formatBillingActivityOccurredAt("2026-08-26T09:42:00.000Z", now)
    ).toBe("Today, 10:42")
    expect(
      formatBillingActivityOccurredAt("2026-08-25T15:05:00.000Z", now)
    ).toBe("Yesterday, 16:05")
    expect(
      formatBillingActivityOccurredAt("2026-08-23T15:05:00.000Z", now)
    ).toBe("23 Aug 2026, 16:05")
  })
})

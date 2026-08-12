import { describe, expect, it } from "vitest"

import {
  formatRecoverySuccessDate,
  formatRecoverySuccessDateTime,
  recoverySuccessChromeForRespondAndRecord,
  recoverySuccessChromeForRespondToGuest,
  recoverySuccessChromeForRespondWithRecoveryOffer,
  recoverySuccessChromeForRecordInternalAction,
} from "./recoverySuccessPresentation"

describe("recoverySuccessPresentation", () => {
  it("formats Sent / Recorded timestamps like Figma Success rows", () => {
    expect(formatRecoverySuccessDateTime(new Date("2026-07-29T20:14:00"))).toBe(
      "29 July 2026 at 8:14 PM"
    )
  })

  it("formats Offer expiry as a long date without time", () => {
    expect(formatRecoverySuccessDate(new Date("2026-08-31T00:00:00"))).toBe(
      "31 August 2026"
    )
  })

  it("builds Respond-to-guest Success chrome with Figma status rows", () => {
    const chrome = recoverySuccessChromeForRespondToGuest({
      maskedDestination: "m••••••@email.com",
      channel: "email",
      actorDisplayName: "Mohammed Mahmoud",
      sentAt: "2026-07-29T20:14:00.000Z",
    })

    expect(chrome.title).toBe("Response sent")
    expect(chrome.subtitle).toBe(
      "The response was sent to m••••••@email.com and recorded against this feedback."
    )
    expect(chrome.rows).toEqual([
      { label: "Response status", value: "Sent", valueKind: "badge" },
      { label: "Recovery status", value: "Response sent", valueKind: "badge" },
      { label: "Workflow status", value: "In progress", valueKind: "badge" },
      { label: "Channel", value: "Email", valueKind: "text" },
      { label: "Sent by", value: "Mohammed Mahmoud", valueKind: "text" },
      {
        label: "Sent",
        value: formatRecoverySuccessDateTime(new Date("2026-07-29T20:14:00.000Z")),
        valueKind: "text",
      },
    ])
  })

  it("builds Respond-and-record Success chrome with Figma status rows", () => {
    const chrome = recoverySuccessChromeForRespondAndRecord({
      channel: "email",
    })

    expect(chrome.title).toBe("Response sent and internal action recorded")
    expect(chrome.subtitle).toBe(
      "The guest response was sent by email, and the internal follow-up was added to this feedback."
    )
    expect(chrome.rows).toEqual([
      { label: "Response status", value: "Sent", valueKind: "badge" },
      { label: "Internal action", value: "Recorded", valueKind: "badge" },
      { label: "Workflow status", value: "In progress", valueKind: "badge" },
    ])
  })

  it("builds Record-internal Success chrome with Figma status rows", () => {
    const chrome = recoverySuccessChromeForRecordInternalAction({
      actorDisplayName: "Mohammed Mahmoud",
      recordedAt: "2026-07-29T19:58:00.000Z",
    })

    expect(chrome.title).toBe("Internal follow-up recorded")
    expect(chrome.subtitle).toBe(
      "The action has been added to this feedback’s activity history."
    )
    expect(chrome.rows).toEqual([
      {
        label: "Recovery status",
        value: "Internal action recorded",
        valueKind: "badge",
      },
      { label: "Follow-up status", value: "Complete", valueKind: "badge" },
      { label: "Workflow status", value: "In progress", valueKind: "badge" },
      { label: "Recorded by", value: "Mohammed Mahmoud", valueKind: "text" },
      {
        label: "Recorded",
        value: formatRecoverySuccessDateTime(
          new Date("2026-07-29T19:58:00.000Z")
        ),
        valueKind: "text",
      },
    ])
  })

  it("builds Recovery-offer Success chrome with Claim code and status rows", () => {
    const chrome = recoverySuccessChromeForRespondWithRecoveryOffer({
      maskedDestination: "m••••••@email.com",
      offerTitle: "10% off your next visit",
      expiryAt: "2026-08-31T00:00:00.000Z",
      claimCode: "TUM-ABC123",
    })

    expect(chrome.title).toBe("Response and recovery offer sent")
    expect(chrome.subtitle).toBe(
      'The response was sent to m••••••@email.com, and “10% off your next visit” was issued to the guest.'
    )
    expect(chrome.rows).toEqual([
      { label: "Recovery status", value: "Offer issued", valueKind: "badge" },
      { label: "Response status", value: "Sent", valueKind: "badge" },
      { label: "Workflow status", value: "In progress", valueKind: "badge" },
      { label: "Claim code", value: "TUM-ABC123", valueKind: "text" },
      {
        label: "Offer expiry",
        value: formatRecoverySuccessDate(new Date("2026-08-31T00:00:00.000Z")),
        valueKind: "text",
      },
      {
        label: "Redemption status",
        value: "Not redeemed",
        valueKind: "badge",
      },
    ])
  })

  it("shows a dash when Claim code is missing on Recovery-offer Success", () => {
    const chrome = recoverySuccessChromeForRespondWithRecoveryOffer({
      maskedDestination: null,
      offerTitle: null,
      expiryAt: null,
      claimCode: null,
    })

    expect(chrome.rows.find((row) => row.label === "Claim code")).toEqual({
      label: "Claim code",
      value: "—",
      valueKind: "text",
    })
  })
})

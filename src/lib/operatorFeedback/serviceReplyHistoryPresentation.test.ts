import { describe, expect, it } from "vitest"

import type { FeedbackDetailsActivityEventDto } from "@/types/dashboard"

import {
  SERVICE_REPLY_HISTORY_COPY,
  serviceReplyHistoryRows,
} from "./serviceReplyHistoryPresentation"

function replyEvent(
  overrides: Partial<FeedbackDetailsActivityEventDto> = {}
): FeedbackDetailsActivityEventDto {
  return {
    kind: "guest_response_sent",
    at: "2026-10-26T09:30:00.000Z",
    actorDisplayName: "Operator One",
    channel: "email",
    maskedDestination: "a••••@example.com",
    guestResponseId: 11,
    deliveryState: "delivered",
    deliveryStateAt: "2026-10-26T09:31:00.000Z",
    ...overrides,
  }
}

describe("serviceReplyHistoryPresentation", () => {
  it("shows pending delivery state copy", () => {
    expect(
      serviceReplyHistoryRows([replyEvent({ deliveryState: "pending" })])[0]
        .statusText
    ).toBe("Delivery status pending.")
  })

  it("shows accepted delivery state copy", () => {
    expect(
      serviceReplyHistoryRows([replyEvent({ deliveryState: "accepted" })])[0]
        .statusText
    ).toBe("Reply accepted for sending.")
  })

  it("shows delivered delivery state copy", () => {
    expect(
      serviceReplyHistoryRows([replyEvent({ deliveryState: "delivered" })])[0]
        .statusText
    ).toBe("Provider reports delivered.")
  })

  it("shows failed delivery state copy", () => {
    expect(
      serviceReplyHistoryRows([replyEvent({ deliveryState: "failed" })])[0]
        .statusText
    ).toBe(
      "The reply was not confirmed as sent. Check its status before trying again."
    )
  })

  it("shows unavailable delivery state copy", () => {
    expect(
      serviceReplyHistoryRows([replyEvent({ deliveryState: "unavailable" })])[0]
        .statusText
    ).toBe(
      "Delivery status is unavailable. This does not mean the message was delivered."
    )
  })

  it("uses muted tone for pending and unavailable, destructive for failed", () => {
    const tone = (deliveryState: FeedbackDetailsActivityEventDto["deliveryState"]) =>
      serviceReplyHistoryRows([replyEvent({ deliveryState })])[0].statusTone

    expect(tone("pending")).toBe("muted")
    expect(tone("unavailable")).toBe("muted")
    expect(tone("failed")).toBe("destructive")
    expect(tone("delivered")).toBe("default")
  })

  it("empty history returns no rows and the empty copy", () => {
    expect(serviceReplyHistoryRows([])).toEqual([])
    expect(SERVICE_REPLY_HISTORY_COPY.empty).toBe(
      "No service replies have been recorded for this feedback."
    )
  })

  it("rows are newest first and ignore other activity kinds", () => {
    const rows = serviceReplyHistoryRows([
      replyEvent({ guestResponseId: 1, at: "2026-10-26T08:00:00.000Z" }),
      { kind: "note_added", at: "2026-10-26T12:00:00.000Z" },
      replyEvent({ guestResponseId: 2, at: "2026-10-26T10:00:00.000Z" }),
      replyEvent({ guestResponseId: 3, at: "2026-10-26T09:00:00.000Z" }),
    ])

    expect(rows.map((row) => row.id)).toEqual([2, 3, 1])
  })

  it("maps channel, sender and the masked recipient", () => {
    const [row] = serviceReplyHistoryRows([
      replyEvent({
        channel: "sms",
        maskedDestination: "••••3456",
        actorDisplayName: "Sam Operator",
      }),
    ])

    expect(row.channelLabel).toBe("SMS")
    expect(row.sentBy).toBe("Sam Operator")
    expect(row.id).toBe(11)
  })

  it("maskedRecipient is passed through unchanged", () => {
    const masked = "a••••@example.com"
    expect(
      serviceReplyHistoryRows([replyEvent({ maskedDestination: masked })])[0]
        .maskedRecipient
    ).toBe(masked)
  })
})

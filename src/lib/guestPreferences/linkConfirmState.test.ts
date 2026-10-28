import { describe, expect, it } from "vitest"

import {
  LINK_CONFIRM_INITIAL,
  reduceLinkConfirm,
  type LinkConfirmState,
} from "@/lib/guestPreferences/linkConfirmState"

describe("reduceLinkConfirm", () => {
  it("starts idle", () => {
    expect(LINK_CONFIRM_INITIAL).toEqual({ status: "idle" })
  })

  it("Confirm moves idle to confirming", () => {
    expect(reduceLinkConfirm({ status: "idle" }, { type: "confirm" })).toEqual({
      status: "confirming",
    })
  })

  it("a successful confirm settles to confirmed", () => {
    expect(
      reduceLinkConfirm({ status: "confirming" }, { type: "settled", outcome: "ok" })
    ).toEqual({ status: "confirmed" })
  })

  it("a dead link (400) settles to expired", () => {
    expect(
      reduceLinkConfirm(
        { status: "confirming" },
        { type: "settled", outcome: "invalid" }
      )
    ).toEqual({ status: "expired" })
  })

  it("any other failure settles to error", () => {
    expect(
      reduceLinkConfirm(
        { status: "confirming" },
        { type: "settled", outcome: "unavailable" }
      )
    ).toEqual({ status: "error" })
  })

  it("error allows a retry: Confirm moves error to confirming", () => {
    expect(reduceLinkConfirm({ status: "error" }, { type: "confirm" })).toEqual({
      status: "confirming",
    })
  })

  it("Confirm is ignored while a confirm is in flight", () => {
    const confirming: LinkConfirmState = { status: "confirming" }

    expect(reduceLinkConfirm(confirming, { type: "confirm" })).toBe(confirming)
  })

  it("a settle with no confirm in flight is ignored", () => {
    const idle: LinkConfirmState = { status: "idle" }

    expect(reduceLinkConfirm(idle, { type: "settled", outcome: "ok" })).toBe(idle)
  })
})

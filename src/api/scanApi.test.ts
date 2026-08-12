import { afterEach, beforeEach, describe, expect, it } from "vitest"
import axios from "axios"
import MockAdapter from "axios-mock-adapter"

import { API_BASE_URL } from "@/config/api"

import { submitGuestFeedback } from "./scanApi"

describe("submitGuestFeedback", () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(axios)
  })

  afterEach(() => {
    mock.restore()
  })

  it("returns the issued thank-you offer from a successful submit", async () => {
    mock.onPost(`${API_BASE_URL}/scan/guest-token/feedback`).reply(200, {
      success: true,
      message: "Feedback submitted successfully.",
      offer: {
        title: "Thanks for visiting",
        description: "Guest form thank-you",
        claimCode: "TUM-ABC234",
        expiryLabel: "Expires: 26 August 2026",
      },
    })

    await expect(
      submitGuestFeedback("guest-token", {
        comment: "Great meal",
        guestName: "Alex Guest",
        guestContact: "alex@example.com",
        acceptsOffers: true,
      })
    ).resolves.toEqual({
      title: "Thanks for visiting",
      description: "Guest form thank-you",
      claimCode: "TUM-ABC234",
      expiryLabel: "Expires: 26 August 2026",
    })
  })

  it("returns null when submit succeeds without an issued offer", async () => {
    mock.onPost(`${API_BASE_URL}/scan/guest-token/feedback`).reply(200, {
      success: true,
      message: "Feedback submitted successfully.",
      offer: null,
    })

    await expect(
      submitGuestFeedback("guest-token", {
        comment: "Great meal",
        guestName: "Alex Guest",
        guestContact: "alex@example.com",
        acceptsOffers: false,
      })
    ).resolves.toBeNull()
  })
})

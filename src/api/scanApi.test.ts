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
      offer: {
        title: "Thanks for visiting",
        description: "Guest form thank-you",
        claimCode: "TUM-ABC234",
        expiryLabel: "Expires: 26 August 2026",
        alreadyClaimed: false,
      },
    })
  })

  it("maps alreadyClaimed when Scan reuses an existing thank-you Claim", async () => {
    mock.onPost(`${API_BASE_URL}/scan/guest-token/feedback`).reply(200, {
      success: true,
      message: "Feedback submitted successfully.",
      offer: {
        title: "Thanks for visiting",
        description: "Guest form thank-you",
        claimCode: "TUM-ABC234",
        expiryLabel: "Expires: 26 August 2026",
        alreadyClaimed: true,
      },
    })

    await expect(
      submitGuestFeedback("guest-token", {
        comment: "Great meal",
        guestName: "Alex Guest",
        guestContact: "alex@example.com",
        acceptsOffers: true,
      })
    ).resolves.toMatchObject({
      offer: { alreadyClaimed: true },
    })
  })

  it("submit result has no unlockOffer key", async () => {
    mock.onPost(`${API_BASE_URL}/scan/guest-token/feedback`).reply(200, {
      success: true,
      message: "Feedback submitted successfully.",
      offer: null,
      unlockOffer: {
        title: "Free dessert",
        channel: "sms",
        restaurantName: "KFC",
        locationName: "Camden High Street",
        unlockToken: "payload.sig",
      },
    })

    const result = await submitGuestFeedback("guest-token", {
      comment: "Great meal",
      guestName: "Alex Guest",
      guestContact: "07123456789",
      acceptsOffers: false,
    })

    expect(Object.keys(result)).not.toContain("unlockOffer")
    expect(result).toEqual({ offer: null })
  })

  it("returns null offer when neither applies", async () => {
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
    ).resolves.toEqual({
      offer: null,
    })
  })

  it("includes recaptchaToken in the submit body when provided", async () => {
    mock.onPost(`${API_BASE_URL}/scan/guest-token/feedback`).reply((config) => {
      const body = JSON.parse(config.data as string) as {
        recaptchaToken?: string
        guestName?: string
      }
      expect(body.recaptchaToken).toBe("captcha-token-1")
      expect(body.guestName).toBe("Alex Guest")
      return [
        200,
        {
          success: true,
          message: "Feedback submitted successfully.",
          offer: null,
        },
      ]
    })

    await expect(
      submitGuestFeedback(
        "guest-token",
        {
          comment: "Great meal",
          guestName: "Alex Guest",
          guestContact: "alex@example.com",
          acceptsOffers: true,
        },
        { recaptchaToken: "captcha-token-1" }
      )
    ).resolves.toEqual({
      offer: null,
    })
  })
})

import { describe, expect, it } from "vitest"

import {
  UNSUBSCRIBE_INVALID_LINK_MESSAGE,
  UNSUBSCRIBE_MISSING_RESTAURANT_MESSAGE,
  UNSUBSCRIBE_PREVIEW_NETWORK_ERROR_MESSAGE,
  UNSUBSCRIBE_SUCCESS_MESSAGE,
  resolveUnsubscribePageMode,
} from "./unsubscribePresentation"

describe("unsubscribePresentation", () => {
  describe("resolveUnsubscribePageMode", () => {
    it("prefers a token query over restaurantId", () => {
      expect(
        resolveUnsubscribePageMode(
          new URLSearchParams("t=signed-token&restaurantId=12")
        )
      ).toEqual({ kind: "token", token: "signed-token" })
    })

    it("returns form mode when restaurantId is present without a token", () => {
      expect(
        resolveUnsubscribePageMode(new URLSearchParams("restaurantId=42"))
      ).toEqual({ kind: "form", restaurantId: 42 })
    })

    it("returns missing-restaurant when neither token nor restaurantId is usable", () => {
      expect(resolveUnsubscribePageMode(new URLSearchParams())).toEqual({
        kind: "missing-restaurant",
      })
      expect(
        resolveUnsubscribePageMode(new URLSearchParams("restaurantId=0"))
      ).toEqual({ kind: "missing-restaurant" })
      expect(
        resolveUnsubscribePageMode(new URLSearchParams("restaurantId=abc"))
      ).toEqual({ kind: "missing-restaurant" })
      expect(resolveUnsubscribePageMode(new URLSearchParams("t="))).toEqual({
        kind: "missing-restaurant",
      })
    })
  })

  describe("copy", () => {
    it("exposes generic success and guidance copy", () => {
      expect(UNSUBSCRIBE_SUCCESS_MESSAGE).toBe(
        "You're unsubscribed from marketing emails for this restaurant."
      )
      expect(UNSUBSCRIBE_MISSING_RESTAURANT_MESSAGE).toBe(
        "Use the Unsubscribe link in your email, or ask the restaurant for their unsubscribe page."
      )
      expect(UNSUBSCRIBE_INVALID_LINK_MESSAGE).toBe(
        "This unsubscribe link is invalid or has expired."
      )
      expect(UNSUBSCRIBE_PREVIEW_NETWORK_ERROR_MESSAGE).toBe(
        "We couldn't load this unsubscribe link. Please try again."
      )
    })
  })
})

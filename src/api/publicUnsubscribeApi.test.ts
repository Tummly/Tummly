import { afterEach, beforeEach, describe, expect, it } from "vitest"
import axios from "axios"
import MockAdapter from "axios-mock-adapter"

import { API_BASE_URL } from "@/config/api"

import {
  confirmUnsubscribe,
  fetchUnsubscribePreview,
  submitUnsubscribeForm,
} from "./publicUnsubscribeApi"

describe("publicUnsubscribeApi", () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(axios)
  })

  afterEach(() => {
    mock.restore()
  })

  it("parses a valid preview response", async () => {
    mock
      .onGet(`${API_BASE_URL}/public/unsubscribe/preview`, {
        params: { t: "good-token" },
      })
      .reply(200, { valid: true, restaurantName: "Camden Kitchen" })

    await expect(fetchUnsubscribePreview("good-token")).resolves.toEqual({
      valid: true,
      restaurantName: "Camden Kitchen",
    })
  })

  it("parses an invalid preview response", async () => {
    mock
      .onGet(`${API_BASE_URL}/public/unsubscribe/preview`, {
        params: { t: "bad-token" },
      })
      .reply(200, { valid: false })

    await expect(fetchUnsubscribePreview("bad-token")).resolves.toEqual({
      valid: false,
    })
  })

  it("posts confirm with the token body", async () => {
    mock
      .onPost(`${API_BASE_URL}/public/unsubscribe/confirm`, {
        token: "good-token",
      })
      .reply(200, { success: true })

    await expect(confirmUnsubscribe("good-token")).resolves.toBeUndefined()
  })

  it("posts form with email and restaurantId", async () => {
    mock
      .onPost(`${API_BASE_URL}/public/unsubscribe/form`, {
        email: "guest@example.com",
        restaurantId: 7,
      })
      .reply(200, { success: true })

    await expect(
      submitUnsubscribeForm("guest@example.com", 7)
    ).resolves.toBeUndefined()
  })
})

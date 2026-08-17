import MockAdapter from "axios-mock-adapter"
import { afterEach, beforeEach, describe, expect, it } from "vitest"

import axiosInstance from "./axiosInstance"
import { exportSingleFeedback } from "./dashboardApi"

describe("exportSingleFeedback", () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(axiosInstance)
  })

  afterEach(() => {
    mock.restore()
  })

  it("uses the dedicated feedback export route", async () => {
    mock.onGet("/feedback/42/export").reply((config) => {
      expect(config.params).toEqual({
        locationId: 7,
        format: "xlsx",
        includeGuestContact: false,
      })
      return [
        200,
        new Blob(["xlsx"]),
        {
          "content-disposition":
            'attachment; filename="tummly-feedback-42.xlsx"',
        },
      ]
    })

    await expect(
      exportSingleFeedback({ feedbackId: 42, locationId: 7 })
    ).resolves.toMatchObject({
      filename: "tummly-feedback-42.xlsx",
    })
  })
})

import MockAdapter from "axios-mock-adapter"
import { afterEach, beforeEach, describe, expect, it } from "vitest"

import axiosInstance from "./axiosInstance"
import { exportAccountWorkspaceGuestData } from "./accountWorkspaceApi"

describe("exportAccountWorkspaceGuestData", () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(axiosInstance)
  })

  afterEach(() => {
    mock.restore()
  })

  it("downloads the restaurant guest-data file for the chosen format", async () => {
    mock.onGet("/account-workspace/guest-data-export").reply((config) => {
      expect(config.params).toEqual({ format: "csv" })
      return [
        200,
        new Blob(["csv"]),
        {
          "content-disposition":
            'attachment; filename="tummly-guest-data-9-20260824-120000Z.csv"',
        },
      ]
    })

    await expect(
      exportAccountWorkspaceGuestData("csv")
    ).resolves.toMatchObject({
      filename: "tummly-guest-data-9-20260824-120000Z.csv",
    })
  })
})

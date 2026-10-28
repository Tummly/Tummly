import MockAdapter from "axios-mock-adapter"
import { afterEach, beforeEach, describe, expect, it } from "vitest"

import axiosInstance from "./axiosInstance"
import { createOwnedLocation, importOwnedLocations } from "./locationsWriteApi"

const GENERIC_WARNING = {
  code: "possible_duplicate_premises",
  message: "Possible duplicate premises. Saved as Draft for review.",
  matchedRowIndex: null,
}

describe("locationsWriteApi premises warning", () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(axiosInstance)
  })

  afterEach(() => {
    mock.restore()
  })

  it("returns the generic warning from a single add", async () => {
    mock.onPost("/locations").reply(200, {
      success: true,
      locationId: 12,
      warning: GENERIC_WARNING,
    })

    const created = await createOwnedLocation({
      locationName: "Cafe",
      address: "1 High Street",
      city: "Leeds",
      postcode: "LS1 1AA",
    })

    expect(created.locationId).toBe(12)
    expect(created.warning).toEqual(GENERIC_WARNING)
    expect(Object.keys(created.warning ?? {}).sort()).toEqual([
      "code",
      "matchedRowIndex",
      "message",
    ])
  })

  it("returns a null warning when nothing matched", async () => {
    mock.onPost("/locations").reply(200, { success: true, locationId: 13 })

    const created = await createOwnedLocation({
      locationName: "Cafe",
      address: "1 High Street",
      city: "Leeds",
      postcode: "LS1 1AA",
    })

    expect(created.warning).toBeNull()
  })

  it("sends the optional FSA establishment id", async () => {
    mock.onPost("/locations").reply((config) => {
      expect(JSON.parse(config.data as string)).toMatchObject({
        fsaEstablishmentId: "123456",
      })
      return [200, { success: true, locationId: 14 }]
    })

    await createOwnedLocation({
      locationName: "Cafe",
      address: "1 High Street",
      city: "Leeds",
      postcode: "LS1 1AA",
      fsaEstablishmentId: "123456",
    })
  })

  it("passes import row FSA ids and warnings through", async () => {
    mock.onPost("/locations/import").reply((config) => {
      expect(JSON.parse(config.data as string).rows[0]).toMatchObject({
        fsaEstablishmentId: "999",
      })
      return [
        200,
        {
          success: true,
          created: [{ rowIndex: 0, locationId: 5, warning: GENERIC_WARNING }],
          errors: [],
        },
      ]
    })

    const result = await importOwnedLocations([
      {
        locationName: "Cafe",
        address: "1 High Street",
        city: "Leeds",
        postcode: "LS1 1AA",
        fsaEstablishmentId: "999",
      },
    ])

    expect(result.created[0]?.warning).toEqual(GENERIC_WARNING)
  })
})

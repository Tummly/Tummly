import { describe, expect, it } from "vitest"

import {
  parseWorkspaceLocation,
  parseWorkspaceLocationsResponse,
} from "./workspaceSetupFlow"

describe("parseWorkspaceLocation", () => {
  it("reads camelCase workspace fields", () => {
    expect(
      parseWorkspaceLocation({
        locationId: 12,
        locationName: "City Centre",
        restaurantName: "Tummly Group",
        address: "1 High Street",
      })
    ).toEqual({
      locationId: 12,
      locationName: "City Centre",
      restaurantName: "Tummly Group",
      address: "1 High Street",
    })
  })

  it("reads PascalCase workspace fields", () => {
    expect(
      parseWorkspaceLocation({
        LocationId: 7,
        LocationName: "Waterfront",
        RestaurantName: "Harbour Co",
        Address: "2 Pier Road",
      })
    ).toEqual({
      locationId: 7,
      locationName: "Waterfront",
      restaurantName: "Harbour Co",
      address: "2 Pier Road",
    })
  })
})

describe("parseWorkspaceLocationsResponse", () => {
  it("maps the workspaces list envelope", () => {
    expect(
      parseWorkspaceLocationsResponse({
        success: true,
        data: [
          {
            locationId: 1,
            locationName: "Main",
            restaurantName: "Group",
            address: "Street",
          },
        ],
      })
    ).toEqual([
      {
        locationId: 1,
        locationName: "Main",
        restaurantName: "Group",
        address: "Street",
      },
    ])
  })
})

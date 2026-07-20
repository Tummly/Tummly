import { describe, expect, it } from "vitest"

import { filterOwnedLocations } from "./filterOwnedLocations"

const locations = [
  { id: 1, name: "Mehmet's Grill — Leeds", address: "Leeds city centre" },
  { id: 2, name: "Mehmet's Grill — Manchester", address: "Deansgate" },
  { id: 3, name: "Harbour Kitchen", address: "" },
]

describe("filterOwnedLocations", () => {
  it("returns all Owned locations when the query is blank", () => {
    expect(filterOwnedLocations(locations, "")).toEqual(locations)
    expect(filterOwnedLocations(locations, "   ")).toEqual(locations)
  })

  it("matches Owned location name case-insensitively", () => {
    expect(filterOwnedLocations(locations, "manchester")).toEqual([
      locations[1],
    ])
  })

  it("matches Owned location address case-insensitively", () => {
    expect(filterOwnedLocations(locations, "city centre")).toEqual([
      locations[0],
    ])
  })

  it("returns an empty list when nothing matches", () => {
    expect(filterOwnedLocations(locations, "birmingham")).toEqual([])
  })
})

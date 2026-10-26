import { readFileSync } from "node:fs"
import { dirname, join } from "node:path"
import { fileURLToPath } from "node:url"
import { describe, expect, it } from "vitest"

const locationsDir = dirname(fileURLToPath(import.meta.url))

describe("Locations write chrome (WS2b Manage gate)", () => {
  it("renders Add and Import only when locationsAccess is not view or none", () => {
    const page = readFileSync(join(locationsDir, "LocationsPage.tsx"), "utf8")

    expect(page).toMatch(
      /locationsAccess\s*!==\s*"view"\s*&&\s*locationsAccess\s*!==\s*"none"/
    )
    expect(page).toMatch(/\{copy\.addLocation\}/)
    expect(page).toMatch(/\{copy\.importLocations\}/)
  })
})

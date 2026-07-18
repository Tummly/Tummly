import { describe, expect, it } from "vitest"

import { resolveHelpCentreContactPrefillLocationId } from "@/lib/helpCentreContactPrefill"

describe("resolveHelpCentreContactPrefillLocationId", () => {
  it("returns empty string when there are no locations", () => {
    expect(resolveHelpCentreContactPrefillLocationId([])).toBe("")
  })

  it("selects the only location for a single-location operator", () => {
    expect(
      resolveHelpCentreContactPrefillLocationId([
        { id: 42, label: "Main Street" },
      ])
    ).toBe("42")
  })

  it("selects the first location for a multi-location operator", () => {
    expect(
      resolveHelpCentreContactPrefillLocationId([
        { id: 7, label: "Soho" },
        { id: 9, label: "Shoreditch" },
      ])
    ).toBe("7")
  })
})

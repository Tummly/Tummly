import { describe, expect, it } from "vitest"

import {
  isPossibleDuplicatePremises,
  POSSIBLE_DUPLICATE_PREMISES_MESSAGE,
} from "@/lib/operatorLocations/importPremisesWarning"

describe("isPossibleDuplicatePremises", () => {
  it("is true only for the possible_duplicate_premises code", () => {
    const duplicate = {
      code: "possible_duplicate_premises",
      scope: "same_workspace",
    }
    const cap = { code: "location_cap_reached", scope: "x" }

    expect(isPossibleDuplicatePremises(duplicate)).toBe(true)
    expect(isPossibleDuplicatePremises(cap)).toBe(false)
  })

  it("is false when the row has no warning", () => {
    expect(isPossibleDuplicatePremises(null)).toBe(false)
    expect(isPossibleDuplicatePremises(undefined)).toBe(false)
  })

  it("keeps the short neutral row message", () => {
    expect(POSSIBLE_DUPLICATE_PREMISES_MESSAGE).toBe(
      "Possible duplicate premises. Saved as Draft for review."
    )
  })
})

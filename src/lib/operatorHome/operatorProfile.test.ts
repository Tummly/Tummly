import { describe, expect, it } from "vitest"

import {
  getOperatorFirstName,
  getOperatorInitials,
} from "./operatorProfile"

describe("getOperatorInitials", () => {
  it("uses the first letter of the first and last name", () => {
    expect(getOperatorInitials("Mohamed Mahmoud")).toBe("MM")
  })

  it("uses a single initial when only one name part exists", () => {
    expect(getOperatorInitials("Alex")).toBe("A")
  })

  it("skips empty parts and uppercases initials", () => {
    expect(getOperatorInitials("  jane   doe  ")).toBe("JD")
  })

  it("returns a fallback when the display name is empty", () => {
    expect(getOperatorInitials("")).toBe("?")
    expect(getOperatorInitials("   ")).toBe("?")
  })
})

describe("getOperatorFirstName", () => {
  it("uses the first name for the Profile chip", () => {
    expect(getOperatorFirstName("Mohamed Mahmoud")).toBe("Mohamed")
  })

  it("falls back when the display name is empty", () => {
    expect(getOperatorFirstName("")).toBe("Operator")
  })
})

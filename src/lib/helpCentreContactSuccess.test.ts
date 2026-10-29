import { describe, expect, it } from "vitest"

import {
  formatHelpCentreQueryReference,
  isHelpCentreContactSuccessState,
  maskHelpCentreContactEmail,
} from "@/lib/helpCentreContactSuccess"

describe("maskHelpCentreContactEmail", () => {
  it("masks the local part after the first character", () => {
    expect(maskHelpCentreContactEmail("jane@example.com")).toBe(
      "j***@example.com"
    )
  })

  it("lowercases the address", () => {
    expect(maskHelpCentreContactEmail("Jane@Example.COM")).toBe(
      "j***@example.com"
    )
  })
})

describe("formatHelpCentreQueryReference", () => {
  it("pads the id to six digits", () => {
    expect(formatHelpCentreQueryReference(42)).toBe("TUM-000042")
  })

  it("formats larger ids without truncating", () => {
    expect(formatHelpCentreQueryReference(482193)).toBe("TUM-482193")
  })
})

describe("isHelpCentreContactSuccessState", () => {
  it("accepts a query id with a non-blank email", () => {
    expect(
      isHelpCentreContactSuccessState({ queryId: 42, email: "jane@example.com" })
    ).toBe(true)
  })

  it("rejects missing router state (direct URL visit)", () => {
    expect(isHelpCentreContactSuccessState(null)).toBe(false)
    expect(isHelpCentreContactSuccessState(undefined)).toBe(false)
    expect(isHelpCentreContactSuccessState("success")).toBe(false)
  })

  it("rejects a non-numeric or non-finite query id", () => {
    expect(
      isHelpCentreContactSuccessState({ queryId: "42", email: "jane@example.com" })
    ).toBe(false)
    expect(
      isHelpCentreContactSuccessState({ queryId: Number.NaN, email: "jane@example.com" })
    ).toBe(false)
  })

  it("rejects a blank email", () => {
    expect(isHelpCentreContactSuccessState({ queryId: 42, email: "   " })).toBe(
      false
    )
  })
})

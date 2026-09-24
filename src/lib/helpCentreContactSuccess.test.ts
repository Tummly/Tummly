import { describe, expect, it } from "vitest"

import {
  formatHelpCentreQueryReference,
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

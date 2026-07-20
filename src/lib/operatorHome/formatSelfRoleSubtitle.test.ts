import { describe, expect, it } from "vitest"

import { formatSelfRoleSubtitle } from "./formatSelfRoleSubtitle"

describe("formatSelfRoleSubtitle", () => {
  it("shows the first segment of slash-joined Self role labels", () => {
    expect(formatSelfRoleSubtitle("owner-operator")).toBe("Owner")
    expect(formatSelfRoleSubtitle("Owner / operator")).toBe("Owner")
    expect(formatSelfRoleSubtitle("founder-director")).toBe("Founder")
    expect(formatSelfRoleSubtitle("area-operations-manager")).toBe("Area")
  })

  it("shows no-slash labels as-is", () => {
    expect(formatSelfRoleSubtitle("general-manager")).toBe("General manager")
    expect(formatSelfRoleSubtitle("General manager")).toBe("General manager")
  })

  it("omits a subtitle for Other or missing Self role", () => {
    expect(formatSelfRoleSubtitle("other")).toBeNull()
    expect(formatSelfRoleSubtitle("Other")).toBeNull()
    expect(formatSelfRoleSubtitle(null)).toBeNull()
    expect(formatSelfRoleSubtitle(undefined)).toBeNull()
    expect(formatSelfRoleSubtitle("")).toBeNull()
    expect(formatSelfRoleSubtitle("   ")).toBeNull()
  })
})

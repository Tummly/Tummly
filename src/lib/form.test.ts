import { describe, expect, it } from "vitest"

import { getCrossFieldPeers } from "@/lib/form"

describe("getCrossFieldPeers", () => {
  it("returns confirmPassword when password changes", () => {
    expect(getCrossFieldPeers("password")).toEqual(["confirmPassword"])
  })

  it("returns password and newPassword when confirmPassword changes", () => {
    expect(getCrossFieldPeers("confirmPassword")).toEqual([
      "password",
      "newPassword",
    ])
  })

  it("returns confirmPassword when newPassword changes", () => {
    expect(getCrossFieldPeers("newPassword")).toEqual(["confirmPassword"])
  })

  it("returns empty array for unrelated fields", () => {
    expect(getCrossFieldPeers("email")).toEqual([])
  })
})

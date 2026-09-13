import { describe, expect, it } from "vitest"

import {
  readGlobalSearchQueryParam,
  readGlobalSearchScopeParam,
  stripGlobalSearchListParams,
  withAllLocationsFilter,
} from "./applySearchQueryFromParam"

describe("applySearchQueryFromParam", () => {
  it("reads a non-empty q param", () => {
    const params = new URLSearchParams("q=cold+food&searchScope=all")
    expect(readGlobalSearchQueryParam(params)).toBe("cold food")
  })

  it("returns null for missing or blank q", () => {
    expect(readGlobalSearchQueryParam(new URLSearchParams())).toBeNull()
    expect(readGlobalSearchQueryParam(new URLSearchParams("q=%20"))).toBeNull()
  })

  it("reads searchScope all vs current", () => {
    expect(
      readGlobalSearchScopeParam(new URLSearchParams("searchScope=all"))
    ).toBe("all")
    expect(readGlobalSearchScopeParam(new URLSearchParams("q=mo"))).toBe(
      "current"
    )
  })

  it("merges location-scope all into filter selection", () => {
    const next = withAllLocationsFilter({
      marketing: { kind: "multi-select", ids: ["eligible"] },
    })
    expect(next.location).toEqual({
      kind: "location-scope",
      value: { kind: "all" },
    })
    expect(next.marketing).toEqual({
      kind: "multi-select",
      ids: ["eligible"],
    })
  })

  it("strips q and searchScope for replace navigation", () => {
    const next = stripGlobalSearchListParams(
      new URLSearchParams("q=mo&searchScope=all&location=3")
    )
    expect(next.get("q")).toBeNull()
    expect(next.get("searchScope")).toBeNull()
    expect(next.get("location")).toBe("3")
  })
})

import { describe, expect, it } from "vitest"

import {
  readGlobalSearchQueryParam,
  stripGlobalSearchListParams,
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

  it("strips q and searchScope for replace navigation", () => {
    const next = stripGlobalSearchListParams(
      new URLSearchParams("q=mo&searchScope=all&location=3")
    )
    expect(next.get("q")).toBeNull()
    expect(next.get("searchScope")).toBeNull()
    expect(next.get("location")).toBe("3")
  })
})

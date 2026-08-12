import { describe, expect, it } from "vitest"

import {
  offersListSearchMissLabel,
  resolveOffersListEmptyStateKind,
} from "@/lib/operatorOffers/resolveOffersListEmptyStateKind"

describe("resolveOffersListEmptyStateKind", () => {
  it("returns true-empty when All is 0 and there is no active query", () => {
    expect(
      resolveOffersListEmptyStateKind({
        allCount: 0,
        filteredTotalCount: 0,
        hasActiveQuery: false,
      })
    ).toBe("true-empty")
  })

  it("returns true-empty when All is 0 even if a query string is present", () => {
    expect(
      resolveOffersListEmptyStateKind({
        allCount: 0,
        filteredTotalCount: 0,
        hasActiveQuery: true,
      })
    ).toBe("true-empty")
  })

  it("returns view-scoped when All has rows but the active view is empty", () => {
    expect(
      resolveOffersListEmptyStateKind({
        allCount: 3,
        filteredTotalCount: 0,
        hasActiveQuery: false,
      })
    ).toBe("view-scoped")
  })

  it("returns filter-search when All has rows and a query matches nothing", () => {
    expect(
      resolveOffersListEmptyStateKind({
        allCount: 3,
        filteredTotalCount: 0,
        hasActiveQuery: true,
      })
    ).toBe("filter-search")
  })

  it("returns null when the filtered list has rows", () => {
    expect(
      resolveOffersListEmptyStateKind({
        allCount: 3,
        filteredTotalCount: 2,
        hasActiveQuery: false,
      })
    ).toBeNull()
  })
})

describe("offersListSearchMissLabel", () => {
  it("returns search-miss copy for a non-empty query", () => {
    expect(offersListSearchMissLabel("weekend")).toBe(
      'No offers found for “weekend”'
    )
  })

  it("returns null when search is blank", () => {
    expect(offersListSearchMissLabel("  ")).toBeNull()
  })
})

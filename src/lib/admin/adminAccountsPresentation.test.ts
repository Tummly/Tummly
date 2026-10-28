import { describe, expect, it } from "vitest"

import {
  ALL_FILTER_VALUE,
  billingStatusBadgeVariant,
  buildAccountListParams,
  capacityLabel,
  formatAccountDate,
  formatAccountDateTime,
  hasNextPage,
  hasPreviousPage,
  locationStatusBadgeVariant,
  pageSummary,
  scheduledChangeLabel,
  validateCreatedRange,
} from "@/lib/admin/adminAccountsPresentation"

const EMPTY_FILTERS = {
  search: "",
  plan: ALL_FILTER_VALUE,
  billingStatus: ALL_FILTER_VALUE,
  createdFrom: "",
  createdTo: "",
}

describe("buildAccountListParams", () => {
  it("drops empty and 'all' filters and trims the search", () => {
    expect(buildAccountListParams({ ...EMPTY_FILTERS, search: "  Harbour  " }, 0, 50)).toEqual({
      search: "Harbour",
      plan: undefined,
      billingStatus: undefined,
      createdFrom: undefined,
      createdTo: undefined,
      skip: 0,
      take: 50,
    })
  })

  it("passes plan, billing status, dates and paging through", () => {
    expect(
      buildAccountListParams(
        {
          search: "",
          plan: "Growth",
          billingStatus: "Past due",
          createdFrom: "2026-10-01",
          createdTo: "2026-10-28",
        },
        50,
        50
      )
    ).toEqual({
      search: undefined,
      plan: "Growth",
      billingStatus: "Past due",
      createdFrom: "2026-10-01",
      createdTo: "2026-10-28",
      skip: 50,
      take: 50,
    })
  })
})

describe("validateCreatedRange", () => {
  it("rejects a reversed range", () => {
    expect(validateCreatedRange("2026-10-20", "2026-10-10")).toBe(
      "Created from must be on or before created to."
    )
  })

  it("accepts an open or equal range", () => {
    expect(validateCreatedRange("", "2026-10-10")).toBeNull()
    expect(validateCreatedRange("2026-10-10", "2026-10-10")).toBeNull()
  })
})

describe("status badge variants", () => {
  it("maps billing statuses to distinct badge skins", () => {
    expect(billingStatusBadgeVariant("Active")).toBe("ready")
    expect(billingStatusBadgeVariant("Past due")).toBe("error")
    expect(billingStatusBadgeVariant("Soft lock")).toBe("neutral")
    expect(billingStatusBadgeVariant("Pilot")).toBe("secondary")
    expect(billingStatusBadgeVariant("Unknown")).toBe("outline")
  })

  it("maps location lifecycle statuses", () => {
    expect(locationStatusBadgeVariant("Active")).toBe("ready")
    expect(locationStatusBadgeVariant("Paused")).toBe("neutral")
    expect(locationStatusBadgeVariant("Draft")).toBe("outline")
  })
})

describe("formatAccountDate", () => {
  it("formats in London time, not UTC", () => {
    // 23:30 UTC on 10 Oct is 00:30 on 11 Oct in London (BST).
    expect(formatAccountDate("2026-10-10T23:30:00Z")).toBe("11 Oct 2026")
  })

  it("shows a dash for a missing date and keeps an invalid value as-is", () => {
    expect(formatAccountDate(null)).toBe("—")
    expect(formatAccountDate("not-a-date")).toBe("not-a-date")
  })
})

describe("capacityLabel", () => {
  it("shows current of cap, or Unavailable when entitlements cannot resolve", () => {
    expect(capacityLabel({ available: true, cap: 5, current: 2, atCap: false })).toBe("2 of 5")
    expect(capacityLabel({ available: false })).toBe("Unavailable")
  })
})

describe("paging helpers", () => {
  it("summarises the visible range", () => {
    expect(pageSummary(50, 25, 75)).toBe("Showing 51–75 of 75")
    expect(pageSummary(0, 0, 0)).toBe("No accounts")
  })

  it("reports next and previous pages", () => {
    expect(hasNextPage(0, 50, 51)).toBe(true)
    expect(hasNextPage(50, 50, 51)).toBe(false)
    expect(hasPreviousPage(0)).toBe(false)
    expect(hasPreviousPage(50)).toBe(true)
  })
})

describe("scheduledChangeLabel", () => {
  it("returns null when nothing is scheduled", () => {
    expect(
      scheduledChangeLabel({
        hasScheduledChange: false,
        targetSubscriptionPlan: null,
        targetBillingCycle: null,
        cancelScheduled: false,
        effectiveAtUtc: null,
      })
    ).toBeNull()
  })

  it("describes a cancellation at renewal", () => {
    expect(
      scheduledChangeLabel({
        hasScheduledChange: false,
        targetSubscriptionPlan: null,
        targetBillingCycle: null,
        cancelScheduled: true,
        effectiveAtUtc: "2026-11-30T00:00:00Z",
      })
    ).toBe("Cancels at renewal on 30 Nov 2026")
  })

  it("describes a plan or term change with its effective date", () => {
    expect(
      scheduledChangeLabel({
        hasScheduledChange: true,
        targetSubscriptionPlan: "Starter",
        targetBillingCycle: "Annual",
        cancelScheduled: false,
        effectiveAtUtc: "2026-11-30T00:00:00Z",
      })
    ).toBe("Changes to Starter Annual on 30 Nov 2026")
  })
})

describe("formatAccountDateTime", () => {
  it("formats in Europe/London with time and keeps null as a dash", () => {
    expect(formatAccountDateTime("2026-07-01T11:05:00Z")).toBe("01 Jul 2026, 12:05")
    expect(formatAccountDateTime(null)).toBe("—")
  })
})

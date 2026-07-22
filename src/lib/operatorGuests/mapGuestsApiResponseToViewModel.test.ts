import { describe, expect, it } from "vitest"

import { mapGuestsApiResponseToViewModel } from "@/lib/operatorGuests/mapGuestsApiResponseToViewModel"
import type { GuestsResponse } from "@/types/dashboard"

function createGuestsResponse(
  overrides: Partial<GuestsResponse> = {}
): GuestsResponse {
  return {
    success: true,
    locationId: 1,
    smartGroup: "all-guests",
    q: "",
    sort: "recent-activity",
    page: 1,
    pageSize: 25,
    totalFilteredCount: 2,
    overview: {
      totalGuests: 2,
      newThisMonth: 1,
      marketingEligible: 1,
      needsRecovery: 0,
    },
    smartGroupCounts: {
      "all-guests": 2,
      "new-guests": 1,
      "needs-recovery": 0,
      "positive-feedback": 1,
      "offer-not-redeemed": 0,
      "recent-redeemers": 0,
      "dormant-guests": 0,
    },
    rows: [
      {
        id: "10",
        name: "Amelia Hughes",
        email: "amelia@example.com",
        mobile: null,
        marketingStatus: "Eligible — Email",
        locationName: "Camden Street",
        latestFeedbackSentiment: "positive",
        feedbackSubmissionCount: 2,
        lastInteractionLabel: "Feedback submitted",
        lastInteractionAt: "2026-07-01T10:00:00.000Z",
        capturedAt: "2026-06-15T10:00:00.000Z",
      },
      {
        id: "11",
        name: "Isla Fraser",
        email: null,
        mobile: "+447700900123",
        marketingStatus: "Eligible — SMS",
        locationName: "Camden Street",
        latestFeedbackSentiment: "none",
        feedbackSubmissionCount: 0,
        lastInteractionLabel: "Feedback submitted",
        lastInteractionAt: null,
        capturedAt: "2026-07-10T10:00:00.000Z",
      },
    ],
    ...overrides,
  }
}

describe("mapGuestsApiResponseToViewModel", () => {
  it("maps overview KPIs, smart group counts, and table rows", () => {
    const viewModel = mapGuestsApiResponseToViewModel({
      response: createGuestsResponse(),
      activeSmartGroupId: "all-guests",
      sortId: "recent-activity",
    })

    expect(viewModel.overviewKpis).toEqual([
      expect.objectContaining({ id: "total-guests", value: 2 }),
      expect.objectContaining({ id: "new-this-month", value: 1 }),
      expect.objectContaining({ id: "marketing-eligible", value: 1 }),
      expect.objectContaining({ id: "needs-recovery", value: 0 }),
    ])
    expect(viewModel.smartGroupTabs).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ id: "all-guests", count: 2 }),
        expect.objectContaining({ id: "new-guests", count: 1 }),
        expect.objectContaining({ id: "positive-feedback", count: 1 }),
      ])
    )
    expect(viewModel.tableRows).toHaveLength(2)
    expect(viewModel.tableRows[0]).toEqual(
      expect.objectContaining({
        id: "10",
        name: "Amelia Hughes",
        email: "amelia@example.com",
        marketingStatusLabel: "Eligible — Email",
      })
    )
    expect(viewModel.tableRows[1]).toEqual(
      expect.objectContaining({
        id: "11",
        email: "",
        mobile: "+447700900123",
      })
    )
    expect(viewModel.totalFilteredCount).toBe(2)
    expect(viewModel.pageRangeLabel).toBe("Showing 1–2 of 2 guests")
    expect(viewModel.sortLabel).toBe("Recent activity")
    expect(viewModel.tableEmptyState).toBeNull()
  })

  it("returns no-guests-yet when the location has zero guests", () => {
    const viewModel = mapGuestsApiResponseToViewModel({
      response: createGuestsResponse({
        totalFilteredCount: 0,
        overview: {
          totalGuests: 0,
          newThisMonth: 0,
          marketingEligible: 0,
          needsRecovery: 0,
        },
        smartGroupCounts: {
          "all-guests": 0,
          "new-guests": 0,
          "needs-recovery": 0,
          "positive-feedback": 0,
          "offer-not-redeemed": 0,
          "recent-redeemers": 0,
          "dormant-guests": 0,
        },
        rows: [],
      }),
      activeSmartGroupId: "all-guests",
      sortId: "recent-activity",
    })

    expect(viewModel.tableEmptyState).toBe("no-guests-yet")
    expect(viewModel.pageRangeLabel).toBe("Showing 0 of 0 guests")
  })

  it("returns no-guests-found when filters yield zero rows but guests exist", () => {
    const viewModel = mapGuestsApiResponseToViewModel({
      response: createGuestsResponse({
        smartGroup: "positive-feedback",
        totalFilteredCount: 0,
        rows: [],
      }),
      activeSmartGroupId: "positive-feedback",
      sortId: "recent-activity",
    })

    expect(viewModel.tableEmptyState).toBe("no-guests-found")
    expect(viewModel.activeSmartGroupId).toBe("positive-feedback")
  })
})

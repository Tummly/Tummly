import { afterEach, describe, expect, it, vi, type Mock } from "vitest"

import {
  createOperatorGuestsPageModule,
  type OperatorGuestsPageAdapters,
} from "@/lib/operatorGuests/createOperatorGuestsPageModule"
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
    totalFilteredCount: 40,
    overview: {
      totalGuests: 40,
      newThisMonth: 15,
      marketingEligible: 20,
      needsRecovery: 0,
    },
    smartGroupCounts: {
      "all-guests": 40,
      "new-guests": 6,
      "needs-recovery": 0,
      "positive-feedback": 8,
      "offer-not-redeemed": 0,
      "recent-redeemers": 0,
      "dormant-guests": 4,
    },
    rows: Array.from({ length: 25 }, (_, index) => ({
      id: String(index + 1),
      name: `Guest ${index + 1}`,
      email: `guest${index + 1}@example.com`,
      mobile: null,
      marketingStatus: "Eligible — Email",
      locationName: "Camden Street",
      latestFeedbackSentiment: "positive",
      feedbackSubmissionCount: 1,
      lastInteractionLabel: "Feedback submitted",
      lastInteractionAt: "2026-07-01T10:00:00.000Z",
      capturedAt: "2026-06-15T10:00:00.000Z",
    })),
    ...overrides,
  }
}

function createAdapters(
  getGuests: Mock<OperatorGuestsPageAdapters["getGuests"]>
): OperatorGuestsPageAdapters {
  return {
    getGuests,
    debounceMs: 0,
  }
}

describe("createOperatorGuestsPageModule", () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it("loads guests when workspace syncs with a selected location", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule(createAdapters(getGuests))

    await module.syncWorkspace({ selectedLocationId: 1 })

    expect(getGuests).toHaveBeenCalledWith({
      locationId: 1,
      smartGroup: "all-guests",
      q: "",
      sort: "recent-activity",
      page: 1,
      pageSize: 25,
    })
    expect(module.getSnapshot().loadStatus).toBe("loaded")
    expect(module.getSnapshot().viewModel?.tableRows).toHaveLength(25)
    expect(module.getSnapshot().viewModel?.totalFilteredCount).toBe(40)
  })

  it("refetches when the active smart group changes and clears selection", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule(createAdapters(getGuests))

    await module.syncWorkspace({ selectedLocationId: 1 })
    module.toggleGuestSelection("1")
    getGuests.mockClear()

    module.setActiveSmartGroupId("positive-feedback")

    await vi.waitFor(() => {
      expect(getGuests).toHaveBeenCalledWith(
        expect.objectContaining({
          smartGroup: "positive-feedback",
          page: 1,
        })
      )
    })

    expect(module.getSnapshot().selectedCount).toBe(0)
    expect(module.getSnapshot().viewModel?.activeSmartGroupId).toBe(
      "positive-feedback"
    )
  })

  it("debounces search refetch and clears selection on search change", async () => {
    vi.useFakeTimers()
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule({
      getGuests,
      debounceMs: 300,
    })

    await module.syncWorkspace({ selectedLocationId: 1 })
    module.toggleGuestSelection("1")
    getGuests.mockClear()

    module.setSearchQuery("amelia")
    expect(getGuests).not.toHaveBeenCalled()
    expect(module.getSnapshot().searchQuery).toBe("amelia")
    expect(module.getSnapshot().selectedCount).toBe(0)

    await vi.advanceTimersByTimeAsync(300)

    expect(getGuests).toHaveBeenCalledWith(
      expect.objectContaining({
        q: "amelia",
        page: 1,
      })
    )
  })

  it("retains selection across page and sort changes", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule(createAdapters(getGuests))

    await module.syncWorkspace({ selectedLocationId: 1 })
    module.toggleGuestSelection("1")
    getGuests.mockClear()

    module.setSortId("guest-name-az")

    await vi.waitFor(() => {
      expect(getGuests).toHaveBeenCalled()
    })

    expect(module.getSnapshot().selectedCount).toBe(1)
    expect(module.getSnapshot().isGuestSelected("1")).toBe(true)

    getGuests.mockResolvedValueOnce(
      createGuestsResponse({
        page: 2,
        rows: [
          {
            id: "26",
            name: "Guest 26",
            email: "guest26@example.com",
            mobile: null,
            marketingStatus: "Eligible — Email",
            locationName: "Camden Street",
            latestFeedbackSentiment: "positive",
            feedbackSubmissionCount: 1,
            lastInteractionLabel: "Feedback submitted",
            lastInteractionAt: "2026-07-01T10:00:00.000Z",
            capturedAt: "2026-06-15T10:00:00.000Z",
          },
        ],
      })
    )
    getGuests.mockClear()

    module.goToNextPage()

    await vi.waitFor(() => {
      expect(getGuests).toHaveBeenCalledWith(
        expect.objectContaining({
          page: 2,
        })
      )
    })

    expect(module.getSnapshot().selectedCount).toBe(1)
    expect(module.getSnapshot().isGuestSelected("1")).toBe(true)
  })

  it("resets page and clears selection when the owned location changes", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule(createAdapters(getGuests))

    await module.syncWorkspace({ selectedLocationId: 1 })
    module.setSearchQuery("isla")
    await vi.waitFor(() => {
      expect(getGuests).toHaveBeenCalled()
    })
    module.toggleGuestSelection("1")
    getGuests.mockClear()

    await module.syncWorkspace({ selectedLocationId: 2 })

    expect(getGuests).toHaveBeenLastCalledWith(
      expect.objectContaining({
        locationId: 2,
        q: "",
        smartGroup: "all-guests",
        page: 1,
      })
    )
    expect(module.getSnapshot().selectedCount).toBe(0)
    expect(module.getSnapshot().searchQuery).toBe("")
  })

  it("maps no-guests-yet from an empty location response", async () => {
    const getGuests = vi.fn(async () =>
      createGuestsResponse({
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
      })
    )
    const module = createOperatorGuestsPageModule(createAdapters(getGuests))

    await module.syncWorkspace({ selectedLocationId: 1 })

    expect(module.getSnapshot().viewModel?.tableEmptyState).toBe("no-guests-yet")
  })

  it("maps no-guests-found when filters return zero rows", async () => {
    const getGuests = vi.fn(async () =>
      createGuestsResponse({
        smartGroup: "positive-feedback",
        totalFilteredCount: 0,
        rows: [],
      })
    )
    const module = createOperatorGuestsPageModule(createAdapters(getGuests))

    await module.syncWorkspace({ selectedLocationId: 1 })
    module.setActiveSmartGroupId("positive-feedback")

    await vi.waitFor(() => {
      expect(module.getSnapshot().viewModel?.tableEmptyState).toBe(
        "no-guests-found"
      )
    })
  })

  it("enters error state when the adapter fails and retries successfully", async () => {
    const getGuests = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(createGuestsResponse())
    const module = createOperatorGuestsPageModule(createAdapters(getGuests))

    await module.syncWorkspace({ selectedLocationId: 1 })

    expect(module.getSnapshot().loadStatus).toBe("error")
    expect(module.getSnapshot().viewModel).toBeNull()

    await module.retryLoad()

    expect(module.getSnapshot().loadStatus).toBe("loaded")
    expect(module.getSnapshot().viewModel?.tableRows).toHaveLength(25)
  })

  it("selects and deselects all visible rows via the header control", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule(createAdapters(getGuests))

    await module.syncWorkspace({ selectedLocationId: 1 })
    const visibleIds = module
      .getSnapshot()
      .viewModel!.tableRows.map((row) => row.id)

    module.toggleSelectAllVisibleRows()

    expect(module.getSnapshot().selectedGuestIds).toEqual([...visibleIds].sort())
    expect(module.getSnapshot().selectedCount).toBe(25)
    expect(module.getSnapshot().isAllVisibleSelected).toBe(true)

    module.toggleSelectAllVisibleRows()

    expect(module.getSnapshot().selectedGuestIds).toEqual([])
    expect(module.getSnapshot().selectedCount).toBe(0)
  })

  it("clears search and resets smart group to all guests", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule(createAdapters(getGuests))

    await module.syncWorkspace({ selectedLocationId: 1 })
    module.setActiveSmartGroupId("positive-feedback")
    module.setSearchQuery("missing")
    await vi.waitFor(() => {
      expect(getGuests).toHaveBeenCalled()
    })
    getGuests.mockClear()

    module.clearSearchAndFilters()

    await vi.waitFor(() => {
      expect(getGuests).toHaveBeenCalledWith(
        expect.objectContaining({
          smartGroup: "all-guests",
          q: "",
          page: 1,
        })
      )
    })

    expect(module.getSnapshot().searchQuery).toBe("")
    expect(module.getSnapshot().viewModel?.activeSmartGroupId).toBe("all-guests")
  })
})

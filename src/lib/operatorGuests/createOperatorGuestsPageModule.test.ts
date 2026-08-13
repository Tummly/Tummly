import { afterEach, describe, expect, it, vi, type Mock } from "vitest"

import {
  createOperatorGuestsPageModule,
  type OperatorGuestsPageAdapters,
} from "@/lib/operatorGuests/createOperatorGuestsPageModule"
import { DEFAULT_GUESTS_OVERVIEW_DATE_RANGE } from "@/lib/operatorGuests/guestsOverviewDateRange"
import { guestsFilterSheetSchema } from "@/lib/operatorGuests/guestsFilterSheetSchema"
import {
  emptySelection,
  type DateFilterValue,
  type LocationOverride,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import type { GuestsResponse } from "@/types/dashboard"

const GUESTS_SCHEMA = guestsFilterSheetSchema()

function multiSelect(ids: string[]) {
  return { kind: "multi-select" as const, ids }
}

function locationScope(value: LocationOverride) {
  return { kind: "location-scope" as const, value }
}

function dateFilter(value: DateFilterValue) {
  return { kind: "date" as const, value }
}

function filters(
  overrides: Record<string, OperatorFilterSelection[string]>
): OperatorFilterSelection {
  return { ...emptySelection(GUESTS_SCHEMA), ...overrides }
}

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
      newThisMonth: 0,
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
      tagIds: index === 0 ? [10, 20] : index === 1 ? [10] : [],
    })),
    ...overrides,
  }
}

function createAdapters(
  overrides: Partial<OperatorGuestsPageAdapters> & {
    getGuests: Mock<OperatorGuestsPageAdapters["getGuests"]>
  }
): OperatorGuestsPageAdapters {
  return {
    debounceMs: 0,
    getGuestsOverviewDateRange: () => DEFAULT_GUESTS_OVERVIEW_DATE_RANGE,
    exportGuestsCsv: vi.fn(async () => ({
      blob: new Blob(["Name\n"], { type: "text/csv" }),
      filename: "tummly-guests-1-20260722-120000Z.csv",
    })),
    listGuestTags: vi.fn(async () => [
      { id: "10", name: "VIP", guestCount: 2 },
      { id: "20", name: "Regular", guestCount: 1 },
    ]),
    createGuestTag: vi.fn(async ({ name }) => ({
      id: "99",
      name,
      guestCount: 0,
    })),
    applyGuestTags: vi.fn(async () => undefined),
    getGuestTagMemberships: vi.fn(async ({ guestIds }) => {
      const map = new Map<string, string[]>()
      for (const guestId of guestIds) {
        if (guestId === 1) {
          map.set("1", ["10", "20"])
        } else if (guestId === 2) {
          map.set("2", ["10"])
        } else {
          map.set(String(guestId), [])
        }
      }
      return map
    }),
    getGuestProfile: vi.fn(async () => {
      throw new Error("getGuestProfile not stubbed")
    }),
    createGuestNote: vi.fn(async () => {
      throw new Error("createGuestNote not stubbed")
    }),
    getFeedbackDetails: vi.fn(async () => {
      throw new Error("getFeedbackDetails not stubbed")
    }),
    sendGuestResponse: vi.fn(async () => {
      throw new Error("sendGuestResponse not stubbed")
    }),
    sendGuestPreviewTest: vi.fn(async () => {
      throw new Error("sendGuestPreviewTest not stubbed")
    }),
    completeRecovery: vi.fn(async () => {
      throw new Error("completeRecovery not stubbed")
    }),
    prepareRecoveryDraft: vi.fn(async () => {
      throw new Error("prepareRecoveryDraft not stubbed")
    }),
    recordInternalAction: vi.fn(async () => {
      throw new Error("recordInternalAction not stubbed")
    }),
    sendAndRecord: vi.fn(async () => {
      throw new Error("sendAndRecord not stubbed")
    }),
    sendAndIssueRecoveryOffer: vi.fn(async () => {
      throw new Error("sendAndIssueRecoveryOffer not stubbed")
    }),
    prepareRecoveryOfferDraft: vi.fn(async () => {
      throw new Error("prepareRecoveryOfferDraft not stubbed")
    }),
    getRecoveryOfferAttach: vi.fn(async () => null),
    setRecoveryOfferAttach: vi.fn(async () => {}),
    listCatalogOffers: vi.fn(async () => ({
      success: true,
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 100,
      tabCounts: {
        all: 0,
        needsAttention: 0,
        drafts: 0,
        inFlight: 0,
        sent: 0,
      },
    })),
    getOffer: vi.fn(async () => {
      throw new Error("getOffer not stubbed")
    }),
    correctClassification: vi.fn(async () => {
      throw new Error("correctClassification not stubbed")
    }),
    updateDetectedTags: vi.fn(async () => {
      throw new Error("updateDetectedTags not stubbed")
    }),
    setWorkflowStatus: vi.fn(async () => {
      throw new Error("setWorkflowStatus not stubbed")
    }),
    createInternalNote: vi.fn(async () => {
      throw new Error("createInternalNote not stubbed")
    }),
    updateInternalNote: vi.fn(async () => {
      throw new Error("updateInternalNote not stubbed")
    }),
    deleteInternalNote: vi.fn(async () => {
      throw new Error("deleteInternalNote not stubbed")
    }),
    closeOutFeedback: vi.fn(async () => {
      throw new Error("closeOutFeedback not stubbed")
    }),
    triggerBrowserDownload: vi.fn(),
    ...overrides,
  }
}

describe("createOperatorGuestsPageModule", () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it("loads guests when workspace syncs with a selected location", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule(
      createAdapters({ getGuests })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Camden Street" }],
    })

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
    const module = createOperatorGuestsPageModule(createAdapters({ getGuests }))

    await module.syncWorkspace({ selectedLocationId: 1, locations: [{ id: 1, locationName: "Loc 1" }] })
    module.toggleGuestSelection("1")
    getGuests.mockClear()

    module.setActiveSmartGroupId("positive-feedback")

    await vi.waitFor(() => {
      expect(getGuests).toHaveBeenCalledWith(
        expect.objectContaining({
          smartGroup: "positive-feedback",
          page: 1,
          includeAggregates: false,
        })
      )
    })

    expect(module.getSnapshot().selectedCount).toBe(0)
    expect(module.getSnapshot().viewModel?.activeSmartGroupId).toBe(
      "positive-feedback"
    )
  })

  it("selects the smart group tab immediately while the quiet refetch is in flight", async () => {
    let resolveSecond: ((value: ReturnType<typeof createGuestsResponse>) => void) | null =
      null
    const getGuests = vi
      .fn()
      .mockResolvedValueOnce(createGuestsResponse())
      .mockImplementationOnce(
        () =>
          new Promise((resolve) => {
            resolveSecond = resolve
          })
      )

    const module = createOperatorGuestsPageModule(createAdapters({ getGuests }))
    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Loc 1" }],
    })

    module.setActiveSmartGroupId("positive-feedback")

    expect(module.getSnapshot().viewModel?.activeSmartGroupId).toBe(
      "positive-feedback"
    )
    expect(module.getSnapshot().loadStatus).toBe("loaded")

    resolveSecond?.(
      createGuestsResponse({
        smartGroup: "positive-feedback",
        overview: undefined,
        smartGroupCounts: undefined,
        rows: [],
        totalFilteredCount: 0,
      })
    )

    await vi.waitFor(() => {
      expect(module.getSnapshot().viewModel?.totalFilteredCount).toBe(0)
    })

    // Tab counts / overview KPIs are preserved across table-only refetches.
    expect(module.getSnapshot().viewModel?.activeSmartGroupId).toBe(
      "positive-feedback"
    )
    expect(
      module.getSnapshot().viewModel?.smartGroupTabs.find(
        (tab) => tab.id === "all-guests"
      )?.count
    ).toBe(40)
    expect(
      module.getSnapshot().viewModel?.overviewKpis.find(
        (kpi) => kpi.id === "total-guests"
      )?.value
    ).toBe(40)
  })

  it("debounces search refetch and clears selection on search change", async () => {
    vi.useFakeTimers()
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule(
      createAdapters({ getGuests, debounceMs: 300 })
    )

    await module.syncWorkspace({ selectedLocationId: 1, locations: [{ id: 1, locationName: "Loc 1" }] })
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
    const module = createOperatorGuestsPageModule(createAdapters({ getGuests }))

    await module.syncWorkspace({ selectedLocationId: 1, locations: [{ id: 1, locationName: "Loc 1" }] })
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
    const module = createOperatorGuestsPageModule(createAdapters({ getGuests }))

    await module.syncWorkspace({ selectedLocationId: 1, locations: [{ id: 1, locationName: "Loc 1" }] })
    module.setSearchQuery("isla")
    await vi.waitFor(() => {
      expect(getGuests).toHaveBeenCalled()
    })
    module.toggleGuestSelection("1")
    getGuests.mockClear()

    await module.syncWorkspace({ selectedLocationId: 2, locations: [{ id: 2, locationName: "Loc 2" }] })

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
    const module = createOperatorGuestsPageModule(createAdapters({ getGuests }))

    await module.syncWorkspace({ selectedLocationId: 1, locations: [{ id: 1, locationName: "Loc 1" }] })

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
    const module = createOperatorGuestsPageModule(createAdapters({ getGuests }))

    await module.syncWorkspace({ selectedLocationId: 1, locations: [{ id: 1, locationName: "Loc 1" }] })
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
    const module = createOperatorGuestsPageModule(createAdapters({ getGuests }))

    await module.syncWorkspace({ selectedLocationId: 1, locations: [{ id: 1, locationName: "Loc 1" }] })

    expect(module.getSnapshot().loadStatus).toBe("error")
    expect(module.getSnapshot().viewModel).toBeNull()

    await module.retryLoad()

    expect(module.getSnapshot().loadStatus).toBe("loaded")
    expect(module.getSnapshot().viewModel?.tableRows).toHaveLength(25)
  })

  it("selects and deselects all visible rows via the header control", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule(createAdapters({ getGuests }))

    await module.syncWorkspace({ selectedLocationId: 1, locations: [{ id: 1, locationName: "Loc 1" }] })
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
    const module = createOperatorGuestsPageModule(createAdapters({ getGuests }))

    await module.syncWorkspace({ selectedLocationId: 1, locations: [{ id: 1, locationName: "Loc 1" }] })
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

  it("openFilters loads catalog via adapter and seeds filters session", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const listGuestTags = vi.fn(async () => [
      { id: "10", name: "VIP", guestCount: 2 },
      { id: "30", name: "Local", guestCount: 4 },
    ])
    const module = createOperatorGuestsPageModule(
      createAdapters({ getGuests, listGuestTags })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [
        { id: 1, locationName: "Camden" },
        { id: 2, locationName: "Soho" },
      ],
    })
    module.applyFilters(
      filters({
        marketing: multiSelect(["eligible"]),
        location: locationScope({ kind: "individual", locationIds: ["2"] }),
      })
    )
    await vi.waitFor(() => expect(getGuests).toHaveBeenCalled())

    await module.openFilters()

    expect(listGuestTags).toHaveBeenCalledWith({
      locationId: 1,
      locationIds: [2],
    })
    const snapshot = module.getSnapshot()
    expect(snapshot.filtersSession?.pending.marketing).toEqual(
      multiSelect(["eligible"])
    )
    expect(snapshot.filterCatalog.map((tag) => tag.id)).toEqual(["10", "30"])
  })

  it("resolves table date presets to local-calendar UTC bounds", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const now = new Date(2026, 6, 22, 15, 30, 0)
    const module = createOperatorGuestsPageModule(
      createAdapters({
        getGuests,
        getNow: () => now,
      })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Camden" }],
    })
    getGuests.mockClear()

    module.applyFilters(
      filters({
        date: dateFilter({
          kind: "preset",
          axis: "first-captured",
          preset: "last-7",
        }),
      })
    )

    await vi.waitFor(() => {
      expect(getGuests).toHaveBeenCalledWith(
        expect.objectContaining({
          dateAxis: "first-captured",
          datePreset: "last-7",
          utcOffsetMinutes: -now.getTimezoneOffset(),
        })
      )
    })

    const lastCall = getGuests.mock.calls.at(-1) as unknown as
      | [Record<string, unknown>]
      | undefined
    expect(lastCall?.[0]).not.toHaveProperty("dateFrom")
    expect(lastCall?.[0]).not.toHaveProperty("dateTo")
  })

  it("applies a live Smart group and Marketing eligible without reloadForOverviewDateRange", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule(
      createAdapters({ getGuests })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Camden" }],
    })
    getGuests.mockClear()
    const reload = vi.spyOn(module, "reloadForOverviewDateRange")

    module.setActiveSmartGroupId("positive-feedback")
    module.applyFilters(
      filters({
        marketing: multiSelect(["eligible"]),
      })
    )

    await vi.waitFor(() => {
      expect(getGuests).toHaveBeenCalledWith(
        expect.objectContaining({
          smartGroup: "positive-feedback",
          marketing: ["eligible"],
        })
      )
    })
    expect(reload).not.toHaveBeenCalled()
  })

  it("applies filters to list params, projects chips, and clears selection", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule(
      createAdapters({ getGuests })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [
        { id: 1, locationName: "Camden" },
        { id: 2, locationName: "Soho" },
      ],
    })
    module.toggleGuestSelection("1")
    getGuests.mockClear()

    module.applyFilters(
      filters({
        marketing: multiSelect(["eligible"]),
        contact: multiSelect(["email"]),
        location: locationScope({ kind: "individual", locationIds: ["2"] }),
        tag: multiSelect(["10"]),
      })
    )

    await vi.waitFor(() => {
      expect(getGuests).toHaveBeenCalledWith(
        expect.objectContaining({
          marketing: ["eligible"],
          contact: ["email"],
          locationIds: [2],
          tagIds: [10],
          page: 1,
        })
      )
    })

    const snapshot = module.getSnapshot()
    expect(snapshot.selectedCount).toBe(0)
    expect(snapshot.filterChips.map((chip) => chip.id)).toEqual([
      "marketing:eligible",
      "contact:email",
      "location:2",
      "tag:10",
    ])
    expect(snapshot.filterChipCount).toBe(4)
  })

  it("removes a filter chip and refetches", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule(
      createAdapters({ getGuests })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Camden" }],
    })
    module.applyFilters(
      filters({
        marketing: multiSelect(["eligible", "not-opted-in"]),
      })
    )
    await vi.waitFor(() => expect(getGuests).toHaveBeenCalled())
    getGuests.mockClear()

    module.removeFilterChip({
      id: "marketing:eligible",
      kind: "marketing",
      fieldId: "marketing",
      label: "Eligible to contact",
      value: "eligible",
    })

    await vi.waitFor(() => {
      expect(getGuests).toHaveBeenCalledWith(
        expect.objectContaining({
          marketing: ["not-opted-in"],
        })
      )
    })
    expect(module.getSnapshot().filterChipCount).toBe(1)
  })

  it("clears location override when shell location changes", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule(
      createAdapters({ getGuests })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [
        { id: 1, locationName: "Camden" },
        { id: 2, locationName: "Soho" },
      ],
    })
    module.applyFilters(
      filters({
        location: locationScope({ kind: "all" }),
        marketing: multiSelect(["eligible"]),
      })
    )
    await vi.waitFor(() => expect(getGuests).toHaveBeenCalled())
    getGuests.mockClear()

    await module.syncWorkspace({
      selectedLocationId: 2,
      locations: [
        { id: 1, locationName: "Camden" },
        { id: 2, locationName: "Soho" },
      ],
    })

    expect(getGuests).toHaveBeenLastCalledWith(
      expect.objectContaining({
        locationId: 2,
        marketing: ["eligible"],
      })
    )
    const lastCall = getGuests.mock.calls.at(-1) as unknown as
      | [Record<string, unknown>]
      | undefined
    expect(lastCall?.[0]).not.toHaveProperty("locationScope")
    expect(module.getSnapshot().filterChips.some((c) => c.kind === "location-all")).toBe(
      false
    )
  })

  it("sends overview date bounds from the visit store adapter", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const now = new Date(2026, 6, 22, 15, 0, 0)
    const module = createOperatorGuestsPageModule(
      createAdapters({
        getGuests,
        getGuestsOverviewDateRange: () => ({
          kind: "preset",
          presetId: "last7",
        }),
        getNow: () => now,
      })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Camden" }],
    })

    expect(getGuests).toHaveBeenCalledWith(
      expect.objectContaining({
        overviewDateFrom: new Date(2026, 6, 16).toISOString(),
        overviewDateTo: now.toISOString(),
      })
    )
  })

  it("exports full list CSV via download adapter", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const exportGuestsCsv = vi.fn(async () => ({
      blob: new Blob(["csv"], { type: "text/csv" }),
      filename: "full.csv",
    }))
    const triggerBrowserDownload = vi.fn()
    const module = createOperatorGuestsPageModule(
      createAdapters({ getGuests, exportGuestsCsv, triggerBrowserDownload })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Camden" }],
    })
    module.applyFilters(
      filters({
        marketing: multiSelect(["eligible"]),
      })
    )
    await vi.waitFor(() => expect(getGuests).toHaveBeenCalled())

    await module.exportCsv()

    expect(exportGuestsCsv).toHaveBeenCalledWith(
      expect.objectContaining({
        locationId: 1,
        marketing: ["eligible"],
      })
    )
    const exportArgs = (
      exportGuestsCsv.mock.calls[0] as unknown as
        | [Record<string, unknown>]
        | undefined
    )?.[0]
    expect(exportArgs).not.toHaveProperty("guestIds")
    expect(triggerBrowserDownload).toHaveBeenCalledWith(
      expect.any(Blob),
      "full.csv"
    )
  })

  it("exports selected guests and opens Add Tag with intersection pre-fill", async () => {
    const getGuests = vi.fn(async () =>
      createGuestsResponse({
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
          // Stale/empty list-row cache — memberships must come from adapter.
          tagIds: [],
        })),
      })
    )
    const exportGuestsCsv = vi.fn(async () => ({
      blob: new Blob(["csv"], { type: "text/csv" }),
      filename: "selected.csv",
    }))
    const triggerBrowserDownload = vi.fn()
    const applyGuestTags = vi.fn(async () => undefined)
    const getGuestTagMemberships = vi.fn(async () => {
      const map = new Map<string, string[]>()
      map.set("1", ["10", "20"])
      map.set("2", ["10"])
      return map
    })
    const module = createOperatorGuestsPageModule(
      createAdapters({
        getGuests,
        exportGuestsCsv,
        triggerBrowserDownload,
        applyGuestTags,
        getGuestTagMemberships,
      })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Camden" }],
    })
    module.toggleGuestSelection("1")
    module.toggleGuestSelection("2")

    await module.exportSelectedCsv()
    expect(exportGuestsCsv).toHaveBeenCalledWith(
      expect.objectContaining({
        guestIds: [1, 2],
      })
    )

    // US 14: Export Selected follows check order, not lexicographic id sort.
    module.clearSelection()
    module.toggleGuestSelection("2")
    module.toggleGuestSelection("1")
    exportGuestsCsv.mockClear()
    await module.exportSelectedCsv()
    expect(exportGuestsCsv).toHaveBeenCalledWith(
      expect.objectContaining({
        guestIds: [2, 1],
      })
    )

    module.clearSelection()
    module.toggleGuestSelection("1")
    module.toggleGuestSelection("2")

    await module.openAddTag()
    expect(getGuestTagMemberships).toHaveBeenCalledWith({
      locationId: 1,
      guestIds: [1, 2],
    })
    const addTag = module.getSnapshot().addTagSession
    expect(addTag?.guestIds).toEqual(["1", "2"])
    expect(addTag?.openTagIds).toEqual(["10"])
    expect(addTag?.pendingTagIds).toEqual(["10"])

    module.stageAddTag("20")
    expect(module.getSnapshot().addTagSession?.pendingTagIds).toEqual([
      "10",
      "20",
    ])

    await module.applyAddTag()
    expect(applyGuestTags).toHaveBeenCalledWith({
      locationId: 1,
      guestIds: [1, 2],
      tagIds: [20],
    })
    expect(module.getSnapshot().addTagSession).toBeNull()
  })

  it("openAddTag accepts an explicit guest id for row Manage tags", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const getGuestTagMemberships = vi.fn(async ({ guestIds }) => {
      const map = new Map<string, string[]>()
      for (const guestId of guestIds) {
        map.set(String(guestId), guestId === 1 ? ["10", "20"] : [])
      }
      return map
    })
    const module = createOperatorGuestsPageModule(
      createAdapters({ getGuests, getGuestTagMemberships })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Camden" }],
    })

    await module.openAddTag(["1"])

    expect(getGuestTagMemberships).toHaveBeenCalledWith({
      locationId: 1,
      guestIds: [1],
    })
    expect(module.getSnapshot().addTagSession?.guestIds).toEqual(["1"])
    expect(module.getSnapshot().addTagSession?.openTagIds).toEqual([
      "10",
      "20",
    ])
  })

  it("clearSearchAndFilters also clears applied Filters", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const module = createOperatorGuestsPageModule(
      createAdapters({ getGuests })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Camden" }],
    })
    module.applyFilters(
      filters({
        marketing: multiSelect(["eligible"]),
      })
    )
    module.setSearchQuery("x")
    await vi.waitFor(() => expect(getGuests).toHaveBeenCalled())
    getGuests.mockClear()

    module.clearSearchAndFilters()

    await vi.waitFor(() => {
      expect(getGuests).toHaveBeenCalledWith(
        expect.objectContaining({
          q: "",
          smartGroup: "all-guests",
          page: 1,
        })
      )
    })
    const lastCall = getGuests.mock.calls.at(-1) as unknown as
      | [Record<string, unknown>]
      | undefined
    expect(lastCall?.[0]).not.toHaveProperty("marketing")
    expect(module.getSnapshot().filterChipCount).toBe(0)
  })

  it("exposes Guest details open/close and resets on Owned location change", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const getGuestProfile = vi.fn(async ({ guestId, locationId }) => ({
      success: true,
      locationId,
      id: guestId,
      name: "Mohamed",
      marketingStatus: "Eligible — Email",
      offersOptOut: false,
      guestSinceAt: "2026-05-12T10:00:00.000Z",
      lastActivityAt: null,
      lastInteractionLabel: "—",
      profileSummary: {
        email: "mohamed@email.com",
        mobile: null,
        firstCapturedAt: "2026-05-12T10:00:00.000Z",
        locationName: "Camden",
        feedbackSubmissionCount: 0,
        offerClaimsAndRedemptions: 0,
        lastInteractionAt: null,
        lastInteractionLabel: "—",
        guestTags: [],
      },
      overviewDetails: {
        guestSinceAt: "2026-05-12T10:00:00.000Z",
        totalInteractions: 0,
        feedbackReceived: 0,
        offersClaimed: 0,
        campaignsSent: 0,
        lastActivityAt: null,
      },
      contactEligibility: [
        {
          channel: "email" as const,
          status: "eligible" as const,
          detailKind: "consent_captured" as const,
          detailAt: null,
        },
        {
          channel: "sms" as const,
          status: "not_provided" as const,
          detailKind: null,
          detailAt: null,
        },
      ],
      latestFeedback: [],
      recentNotes: [],
    }))
    const module = createOperatorGuestsPageModule(
      createAdapters({ getGuests, getGuestProfile })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Camden" }],
    })

    expect(module.getSnapshot().guestDetails.isOpen).toBe(false)

    await module.openGuestDetails(42)
    expect(getGuestProfile).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
    })
    expect(module.getSnapshot().guestDetails).toMatchObject({
      isOpen: true,
      loadStatus: "loaded",
      details: { name: "Mohamed" },
    })

    await module.syncWorkspace({
      selectedLocationId: 2,
      locations: [
        { id: 1, locationName: "Camden" },
        { id: 2, locationName: "Soho" },
      ],
    })

    expect(module.getSnapshot().guestDetails).toMatchObject({
      isOpen: false,
      loadStatus: "idle",
      details: null,
    })
  })

  it("stacks Feedback details on Guest details; close Feedback keeps Guest details; close Guest details closes Feedback", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const getGuestProfile = vi.fn(async ({ guestId, locationId }) => ({
      success: true,
      locationId,
      id: guestId,
      name: "Mohamed",
      marketingStatus: "Eligible — Email",
      offersOptOut: false,
      guestSinceAt: "2026-05-12T10:00:00.000Z",
      lastActivityAt: null,
      lastInteractionLabel: "—",
      profileSummary: {
        email: "mohamed@email.com",
        mobile: null,
        firstCapturedAt: "2026-05-12T10:00:00.000Z",
        locationName: "Camden",
        feedbackSubmissionCount: 1,
        offerClaimsAndRedemptions: 0,
        lastInteractionAt: null,
        lastInteractionLabel: "—",
        guestTags: [],
      },
      overviewDetails: {
        guestSinceAt: "2026-05-12T10:00:00.000Z",
        totalInteractions: 1,
        feedbackReceived: 1,
        offersClaimed: 0,
        campaignsSent: 0,
        lastActivityAt: null,
      },
      contactEligibility: [
        {
          channel: "email" as const,
          status: "eligible" as const,
          detailKind: "consent_captured" as const,
          detailAt: null,
        },
        {
          channel: "sms" as const,
          status: "not_provided" as const,
          detailKind: null,
          detailAt: null,
        },
      ],
      latestFeedback: [
        {
          id: 77,
          createdAt: "2026-07-14T11:00:00.000Z",
          comment: "Food was cold.",
          locationName: "Camden",
          classificationStatus: "Succeeded" as const,
          sentiment: "negative" as const,
          detectedTags: ["cold_food"],
        },
      ],
      recentNotes: [],
    }))
    const getFeedbackDetails = vi.fn(async (feedbackId: number) => ({
      success: true,
      id: feedbackId,
      guestName: "Mohamed",
      guestContact: "mohamed@email.com",
      contactType: "Email" as const,
      comment: "Food was cold.",
      createdAt: "2026-07-14T11:00:00.000Z",
      locationName: "Camden",
      address: "12 High Street",
      classificationStatus: "Succeeded" as const,
      sentiment: "negative" as const,
      detectedTags: ["cold_food"],
      locationGuestId: 42,
      workflowStatus: "new" as const,
      needsAttention: true,
      internalNotes: [],
      activityHistory: [],
    }))
    const module = createOperatorGuestsPageModule(
      createAdapters({ getGuests, getGuestProfile, getFeedbackDetails })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Camden" }],
    })

    await module.openGuestDetails(42)
    expect(module.getSnapshot().guestDetails.isOpen).toBe(true)
    expect(module.getSnapshot().feedbackDetails.isOpen).toBe(false)

    await module.openFeedbackDetails(77)
    expect(getFeedbackDetails).toHaveBeenCalledWith(77)
    expect(module.getSnapshot().guestDetails.isOpen).toBe(true)
    expect(module.getSnapshot().feedbackDetails).toMatchObject({
      isOpen: true,
      loadStatus: "loaded",
      details: { id: 77, workflowStatus: "new" },
    })

    module.closeFeedbackDetails()
    expect(module.getSnapshot().feedbackDetails.isOpen).toBe(false)
    expect(module.getSnapshot().guestDetails.isOpen).toBe(true)

    await module.openFeedbackDetails(77)
    expect(module.getSnapshot().feedbackDetails.isOpen).toBe(true)

    module.closeGuestDetails()
    expect(module.getSnapshot().guestDetails.isOpen).toBe(false)
    expect(module.getSnapshot().feedbackDetails.isOpen).toBe(false)
  })

  it("startRecovery opens the shared shell and closes Guest / Feedback details", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const getGuestProfile = vi.fn(async () => ({
      success: true,
      locationId: 1,
      id: 42,
      name: "Mohamed",
      marketingStatus: "Eligible — Email",
      offersOptOut: false,
      guestSinceAt: "2026-05-01T10:00:00.000Z",
      lastActivityAt: "2026-07-14T11:00:00.000Z",
      lastInteractionLabel: "Feedback submitted",
      profileSummary: {
        email: "mohamed@email.com",
        mobile: null,
        firstCapturedAt: "2026-05-01T10:00:00.000Z",
        locationName: "Camden",
        feedbackSubmissionCount: 1,
        offerClaimsAndRedemptions: 0,
        lastInteractionAt: "2026-07-14T11:00:00.000Z",
        lastInteractionLabel: "Feedback submitted",
        guestTags: [],
      },
      overviewDetails: {
        guestSinceAt: "2026-05-01T10:00:00.000Z",
        totalInteractions: 1,
        feedbackReceived: 1,
        offersClaimed: 0,
        campaignsSent: 0,
        lastActivityAt: "2026-07-14T11:00:00.000Z",
      },
      contactEligibility: [],
      latestFeedback: [
        {
          id: 77,
          createdAt: "2026-07-14T11:00:00.000Z",
          comment: "Food was cold.",
          locationName: "Camden",
          classificationStatus: "Succeeded" as const,
          sentiment: "negative" as const,
          detectedTags: ["cold_food"],
        },
      ],
      recentNotes: [],
    }))
    const getFeedbackDetails = vi.fn(async (feedbackId: number) => ({
      success: true,
      id: feedbackId,
      guestName: "Mohamed",
      guestContact: "mohamed@email.com",
      contactType: "Email" as const,
      comment: "Food was cold.",
      createdAt: "2026-07-14T11:00:00.000Z",
      locationName: "Camden",
      address: "12 High Street",
      classificationStatus: "Succeeded" as const,
      sentiment: "negative" as const,
      detectedTags: ["cold_food"],
      locationGuestId: 42,
      workflowStatus: "new" as const,
      guestOffersOptOut: false,
      internalNotes: [],
      activityHistory: [],
    }))
    const setWorkflowStatus = vi.fn(async () => ({
      workflowStatus: "in_progress" as const,
      needsAttention: true,
      activityEvent: null,
    }))
    const module = createOperatorGuestsPageModule(
      createAdapters({
        getGuests,
        getGuestProfile,
        getFeedbackDetails,
        setWorkflowStatus,
      })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Camden" }],
    })
    await module.openGuestDetails(42)
    await module.openFeedbackDetails(77)
    expect(module.getSnapshot().guestDetails.isOpen).toBe(true)
    expect(module.getSnapshot().feedbackDetails.isOpen).toBe(true)

    await module.startRecovery(77)

    expect(module.getSnapshot().guestDetails.isOpen).toBe(false)
    expect(module.getSnapshot().feedbackDetails.isOpen).toBe(false)
    expect(module.getSnapshot().startRecovery).toMatchObject({
      isOpen: true,
      loadStatus: "loaded",
      feedbackId: 77,
      workflowStatus: "in_progress",
    })
    expect(setWorkflowStatus).toHaveBeenCalledWith(77, "in_progress")

    module.closeStartRecovery()
    expect(module.getSnapshot().startRecovery.isOpen).toBe(false)
  })

  it.each([
    ["respond-to-guest", "respondToGuest"],
    ["record-internal-action-only", "recordInternalAction"],
    ["respond-and-record-internal-action", "respondAndRecord"],
    ["respond-with-recovery-offer", "respondWithRecoveryOffer"],
  ] as const)(
    "selectStartRecoveryIntent(%s) opens the matching recovery wizard",
    async (intentId, snapshotKey) => {
      const getGuests = vi.fn(async () => createGuestsResponse())
      const getFeedbackDetails = vi.fn(async (feedbackId: number) => ({
        success: true,
        id: feedbackId,
        guestName: "Mohamed",
        guestContact: "mohamed@email.com",
        contactType: "Email" as const,
        comment: "Food was cold.",
        createdAt: "2026-07-14T11:00:00.000Z",
        locationName: "Camden",
        address: "12 High Street",
        classificationStatus: "Succeeded" as const,
        sentiment: "negative" as const,
        detectedTags: ["cold_food"],
        locationGuestId: 42,
        workflowStatus: "new" as const,
        guestOffersOptOut: false,
        internalNotes: [],
        activityHistory: [],
      }))
      const setWorkflowStatus = vi.fn(async () => ({
        workflowStatus: "in_progress" as const,
        needsAttention: true,
        activityEvent: null,
      }))
      const module = createOperatorGuestsPageModule(
        createAdapters({ getGuests, getFeedbackDetails, setWorkflowStatus })
      )

      await module.syncWorkspace({
        selectedLocationId: 1,
        locations: [{ id: 1, locationName: "Camden" }],
      })
      await module.startRecovery(77)
      expect(module.getSnapshot().startRecovery.isOpen).toBe(true)

      const selected = module.selectStartRecoveryIntent(intentId)

      expect(selected).toBe(true)
      expect(module.getSnapshot().startRecovery.isOpen).toBe(false)
      expect(module.getSnapshot()[snapshotKey]).toMatchObject({
        isOpen: true,
        feedbackId: 77,
      })
    }
  )

  it("selectStartRecoveryIntent does not open a wizard for a disabled intent", async () => {
    const getGuests = vi.fn(async () => createGuestsResponse())
    const getFeedbackDetails = vi.fn(async (feedbackId: number) => ({
      success: true,
      id: feedbackId,
      guestName: "Mohamed",
      guestContact: "",
      contactType: "Unknown" as const,
      comment: "Food was cold.",
      createdAt: "2026-07-14T11:00:00.000Z",
      locationName: "Camden",
      address: "12 High Street",
      classificationStatus: "Succeeded" as const,
      sentiment: "negative" as const,
      detectedTags: ["cold_food"],
      locationGuestId: 42,
      workflowStatus: "new" as const,
      guestOffersOptOut: false,
      internalNotes: [],
      activityHistory: [],
    }))
    const setWorkflowStatus = vi.fn(async () => ({
      workflowStatus: "in_progress" as const,
      needsAttention: true,
      activityEvent: null,
    }))
    const module = createOperatorGuestsPageModule(
      createAdapters({ getGuests, getFeedbackDetails, setWorkflowStatus })
    )

    await module.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Camden" }],
    })
    await module.startRecovery(77)

    const selected = module.selectStartRecoveryIntent("respond-to-guest")

    expect(selected).toBe(false)
    expect(module.getSnapshot().startRecovery.isOpen).toBe(true)
    expect(module.getSnapshot().respondToGuest.isOpen).toBe(false)
  })
})

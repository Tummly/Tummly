import { describe, expect, it, vi, type Mock } from "vitest"
import {
  createOperatorFeedbackPageModule,
  type OperatorFeedbackPageAdapters,
} from "@/lib/operatorFeedback/createOperatorFeedbackPageModule"
import { FEEDBACK_INBOX_PAGE_SIZE } from "@/lib/operatorFeedback/feedbackInboxListQueryParams"
import { DEFAULT_HOME_PERFORMANCE_DATE_RANGE } from "@/lib/operatorHome/homePerformanceDateRange"
import type {
  FeedbackDetailsResponse,
  FeedbackInboxListItem,
  FeedbackInboxListResponse,
  FeedbackSummaryResponse,
} from "@/types/dashboard"
function summaryResponse(
  overrides: Partial<FeedbackSummaryResponse> = {}
): FeedbackSummaryResponse {
  return {
    success: true,
    total: 10,
    positive: 4,
    neutral: 2,
    negative: 3,
    totalPrevious: 8,
    positivePrevious: 3,
    neutralPrevious: 2,
    negativePrevious: 2,
    needsAttentionTotal: 2,
    ...overrides,
  }
}
function inboxItem(
  overrides: Partial<FeedbackInboxListItem> = {}
): FeedbackInboxListItem {
  return {
    id: 1,
    createdAt: "2026-07-14T11:48:00.000Z",
    comment: "Food was cold.",
    guestName: "Guest One",
    contactType: "Email",
    locationName: "Camden Street",
    qrSource: "CounterCard",
    classificationStatus: "Succeeded",
    sentiment: "negative",
    detectedTags: ["slow_service"],
    workflowStatus: "new",
    needsAttention: true,
    locationGuestId: null,
    ...overrides,
  }
}
function inboxResponse(
  overrides: Partial<FeedbackInboxListResponse> = {}
): FeedbackInboxListResponse {
  return {
    success: true,
    items: [inboxItem()],
    totalCount: 1,
    page: 1,
    pageSize: FEEDBACK_INBOX_PAGE_SIZE,
    tabCounts: {
      all: 12,
      needsAttention: 3,
      new: 4,
      inProgress: 2,
      resolved: 3,
    },
    digitalGuestLinks: [{ id: 9, linkName: "Instagram" }],
    ...overrides,
  }
}
const sampleDetails: FeedbackDetailsResponse = {
  success: true,
  id: 42,
  guestName: "Mohamed Mahmoud",
  guestContact: "mohamed@email.com",
  contactType: "Email",
  comment: "Food was cold and delivery took too long.",
  createdAt: "2026-07-14T11:48:00.000Z",
  locationName: "Camden",
  address: "12 High Street",
  classificationStatus: "Pending",
  sentiment: null,
  detectedTags: null,
  locationGuestId: null,
  internalNotes: [],
  activityHistory: [
    {
      kind: "feedback_received",
      at: "2026-07-14T11:48:00.000Z",
    },
  ],
}
function createAdapters(
  overrides: Partial<OperatorFeedbackPageAdapters> & {
    getFeedbackSummary?: Mock<OperatorFeedbackPageAdapters["getFeedbackSummary"]>
    getFeedbackInbox?: Mock<OperatorFeedbackPageAdapters["getFeedbackInbox"]>
  } = {}
): OperatorFeedbackPageAdapters {
  return {
    getFeedbackSummary:
      overrides.getFeedbackSummary
      ?? vi.fn(async () => summaryResponse()),
    getFeedbackInbox:
      overrides.getFeedbackInbox ?? vi.fn(async () => inboxResponse()),
    getFeedbackPageDateRange:
      overrides.getFeedbackPageDateRange
      ?? (() => DEFAULT_HOME_PERFORMANCE_DATE_RANGE),
    getNow: overrides.getNow ?? (() => new Date("2026-07-17T12:00:00.000Z")),
    scheduleReady: overrides.scheduleReady ?? (async () => undefined),
    debounceMs: overrides.debounceMs ?? 0,
    getFeedbackDetails:
      overrides.getFeedbackDetails
      ?? vi.fn(async (feedbackId: number) => ({
        ...sampleDetails,
        id: feedbackId,
      })),
    correctClassification:
      overrides.correctClassification
      ?? vi.fn(async () => {
        throw new Error("not implemented in test")
      }),
    setWorkflowStatus:
      overrides.setWorkflowStatus
      ?? vi.fn(async () => ({
        workflowStatus: "in_progress" as const,
        needsAttention: false,
        activityEvent: null,
      })),
    createInternalNote:
      overrides.createInternalNote
      ?? vi.fn(async () => {
        throw new Error("not implemented in test")
      }),
    updateInternalNote:
      overrides.updateInternalNote
      ?? vi.fn(async () => {
        throw new Error("not implemented in test")
      }),
    deleteInternalNote:
      overrides.deleteInternalNote
      ?? vi.fn(async () => {
        throw new Error("not implemented in test")
      }),
  }
}
describe("createOperatorFeedbackPageModule", () => {
  it("loads Feedback summary KPIs for the selected location and date range", async () => {
    const getFeedbackSummary = vi.fn(async () =>
      summaryResponse({
        total: 10,
        positive: 4,
        neutral: 2,
        negative: 3,
        needsAttentionTotal: 2,
      })
    )
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({ getFeedbackSummary })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 7,
      locations: [{ id: 7, locationName: "Camden Street" }],
    })
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(getFeedbackSummary).toHaveBeenCalledWith({
      locationId: 7,
      from: expect.any(String),
      to: expect.any(String),
    })
    expect(snapshot.viewModel).toMatchObject({
      locationId: 7,
      locationName: "Camden Street",
      dateRangeLabel: "Last 7 days",
      needsAttentionCount: 2,
      summary: { kind: "kpis" },
    })
    expect(snapshot.viewModel?.summary.kind === "kpis"
      ? snapshot.viewModel.summary.kpis.map((kpi) => kpi.id)
      : []).toEqual(["total", "positive", "neutral", "negative"])
  })
  it("loads inbox with tab counts from the API", async () => {
    const getFeedbackInbox = vi.fn(async () =>
      inboxResponse({
        tabCounts: {
          all: 20,
          needsAttention: 5,
          new: 6,
          inProgress: 4,
          resolved: 5,
        },
      })
    )
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({ getFeedbackInbox })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 2,
      locations: [{ id: 2, locationName: "Main" }],
    })
    expect(getFeedbackInbox).toHaveBeenCalledWith(
      expect.objectContaining({
        locationId: 2,
        tab: "all",
        page: 1,
        pageSize: 25,
        sort: "newest-submitted",
      })
    )
    const tabs = pageModule.getSnapshot().viewModel?.inbox.tabs ?? []
    expect(tabs).toEqual([
      { id: "all", label: "All", count: 20 },
      { id: "needs-attention", label: "Needs attention", count: 5 },
      { id: "new", label: "New", count: 6 },
      { id: "in-progress", label: "In progress", count: 4 },
      { id: "resolved", label: "Resolved", count: 5 },
    ])
  })
  it("reloads summary when the Feedback page date range changes", async () => {
    let range = DEFAULT_HOME_PERFORMANCE_DATE_RANGE
    const getFeedbackSummary = vi.fn(async () => summaryResponse())
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({
        getFeedbackSummary,
        getFeedbackInbox: vi.fn(async () => inboxResponse()),
        getFeedbackPageDateRange: () => range,
      })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 3,
      locations: [{ id: 3, locationName: "Main" }],
    })
    expect(getFeedbackSummary).toHaveBeenCalledTimes(1)
    range = { kind: "preset", presetId: "last30" }
    await pageModule.reloadForFeedbackPageDateRange()
    expect(getFeedbackSummary).toHaveBeenCalledTimes(2)
    expect(pageModule.getSnapshot().viewModel?.dateRangeLabel).toBe(
      "Last 30 days"
    )
  })
  it("shows empty summary when Total is 0", async () => {
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({
        getFeedbackSummary: vi.fn(async () =>
          summaryResponse({
            total: 0,
            positive: 0,
            neutral: 0,
            negative: 0,
            needsAttentionTotal: 0,
          })
        ),
        getFeedbackInbox: vi.fn(async () =>
          inboxResponse({
            items: [],
            totalCount: 0,
            tabCounts: {
              all: 0,
              needsAttention: 0,
              new: 0,
              inProgress: 0,
              resolved: 0,
            },
          })
        ),
      })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Main" }],
    })
    expect(pageModule.getSnapshot().viewModel?.summary).toEqual({
      kind: "empty",
    })
  })
  it("shows KPI zeros when Total > 0 and all unclassified", async () => {
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({
        getFeedbackSummary: vi.fn(async () =>
          summaryResponse({
            total: 4,
            positive: 0,
            neutral: 0,
            negative: 0,
            needsAttentionTotal: 0,
          })
        ),
      })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Main" }],
    })
    const summary = pageModule.getSnapshot().viewModel?.summary
    expect(summary?.kind).toBe("kpis")
    if (summary?.kind !== "kpis") {
      return
    }
    expect(summary.kpis.map((kpi) => kpi.value)).toEqual([4, 0, 0, 0])
  })
  it("tab change resets page and refetches with the new tab", async () => {
    const getFeedbackInbox = vi.fn(async () => inboxResponse())
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({ getFeedbackInbox })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Main" }],
    })
    getFeedbackInbox.mockClear()
    pageModule.setActiveInboxTabId("new")
    await vi.waitFor(() => {
      expect(getFeedbackInbox).toHaveBeenCalledWith(
        expect.objectContaining({
          tab: "new",
          page: 1,
        })
      )
    })
    expect(pageModule.getSnapshot().activeInboxTabId).toBe("new")
  })
  it("search and filters compose in getFeedbackInbox params", async () => {
    const getFeedbackInbox = vi.fn(async () => inboxResponse())
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({ getFeedbackInbox })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Main" }],
    })
    getFeedbackInbox.mockClear()
    pageModule.setSearchQuery("cold food")
    pageModule.applyFilters({
      sentiment: { kind: "multi-select", ids: ["negative"] },
      detectedTag: { kind: "multi-select", ids: [] },
      qrSource: { kind: "multi-select", ids: [] },
      contact: { kind: "multi-select", ids: [] },
      date: { kind: "date", value: { kind: "none" } },
    })
    await vi.waitFor(() => {
      expect(getFeedbackInbox).toHaveBeenCalledWith(
        expect.objectContaining({
          q: "cold food",
          sentiment: ["negative"],
          page: 1,
        })
      )
    })
  })
  it("paginates with page size 25 and fetches the next page", async () => {
    const getFeedbackInbox = vi.fn(async (params) =>
      inboxResponse({
        page: params.page,
        totalCount: 30,
        items:
          params.page === 1
            ? [inboxItem({ id: 10 }), inboxItem({ id: 20 })]
            : [inboxItem({ id: 30 })],
      })
    )
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({ getFeedbackInbox })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Main" }],
    })
    expect(pageModule.getSnapshot().viewModel?.inbox.pageRangeLabel).toBe(
      "Showing 1–25 of 30 feedback items"
    )
    expect(pageModule.getSnapshot().viewModel?.inbox.canGoNext).toBe(true)
    getFeedbackInbox.mockClear()
    pageModule.goToNextPage()
    await vi.waitFor(() => {
      expect(getFeedbackInbox).toHaveBeenCalledWith(
        expect.objectContaining({
          page: 2,
          pageSize: 25,
        })
      )
    })
    await vi.waitFor(() => {
      expect(
        pageModule.getSnapshot().viewModel?.inbox.tableRows.map((r) => r.id)
      ).toEqual([30])
    })
  })
  it("openFeedbackDetails and openNextFeedback use current list context ids", async () => {
    const getFeedbackInbox = vi.fn(async () =>
      inboxResponse({
        items: [
          inboxItem({ id: 10, guestName: "Alpha" }),
          inboxItem({ id: 20, guestName: "Beta" }),
          inboxItem({ id: 30, guestName: "Gamma" }),
        ],
        totalCount: 3,
      })
    )
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({ getFeedbackInbox })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Main" }],
    })
    await pageModule.openFeedbackDetails(10)
    expect(pageModule.getSnapshot().canGoPreviousFeedback).toBe(false)
    expect(pageModule.getSnapshot().canGoNextFeedback).toBe(true)
    expect(pageModule.getSnapshot().feedbackDetails.feedbackId).toBe(10)
    await pageModule.openNextFeedback()
    expect(pageModule.getSnapshot().feedbackDetails.feedbackId).toBe(20)
    expect(pageModule.getSnapshot().canGoPreviousFeedback).toBe(true)
    expect(pageModule.getSnapshot().canGoNextFeedback).toBe(true)
  })
  it("Review needs attention switches tab and refetches needs-attention", async () => {
    const getFeedbackInbox = vi.fn(async () => inboxResponse())
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({
        getFeedbackSummary: vi.fn(async () =>
          summaryResponse({ needsAttentionTotal: 5 })
        ),
        getFeedbackInbox,
      })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Main" }],
    })
    expect(pageModule.getSnapshot().activeInboxTabId).toBe("all")
    expect(pageModule.getSnapshot().viewModel?.needsAttentionCount).toBe(5)
    getFeedbackInbox.mockClear()
    pageModule.reviewNeedsAttention()
    await vi.waitFor(() => {
      expect(getFeedbackInbox).toHaveBeenCalledWith(
        expect.objectContaining({
          tab: "needs-attention",
          page: 1,
        })
      )
    })
    const after = pageModule.getSnapshot()
    expect(after.activeInboxTabId).toBe("needs-attention")
    expect(after.viewModel?.needsAttentionCount).toBe(5)
    expect(after.scrollToInboxRequestId).toBe(1)
  })
})

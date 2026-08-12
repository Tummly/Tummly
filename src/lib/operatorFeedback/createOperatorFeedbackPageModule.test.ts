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
    exportFeedback:
      overrides.exportFeedback
      ?? vi.fn(async () => ({
        blob: new Blob(["id"], { type: "text/csv" }),
        filename: "tummly-feedback-1-20260717-120000Z.csv",
      })),
    triggerBrowserDownload: overrides.triggerBrowserDownload ?? vi.fn(),
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
    updateDetectedTags:
      overrides.updateDetectedTags
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
    closeOutFeedback:
      overrides.closeOutFeedback
      ?? vi.fn(async () => {
        throw new Error("not implemented in test")
      }),
    sendGuestResponse:
      overrides.sendGuestResponse
      ?? vi.fn(async () => ({
        workflowStatus: "in_progress" as const,
        needsAttention: true,
        activityEvent: {
          kind: "guest_response_sent" as const,
          at: "2026-07-17T12:00:00.000Z",
          actorDisplayName: "Alex",
          channel: "email" as const,
          maskedDestination: "m••••@email.com",
        },
      })),
    sendGuestPreviewTest:
      overrides.sendGuestPreviewTest
      ?? vi.fn(async () => {}),
    completeRecovery:
      overrides.completeRecovery
      ?? vi.fn(async () => ({
        workflowStatus: "resolved" as const,
        needsAttention: false,
        activityEvent: {
          kind: "recovery_completed" as const,
          at: "2026-07-17T12:05:00.000Z",
          actorDisplayName: "Alex",
          recoveryIntent: "respond_to_guest" as const,
          fromWorkflowStatus: "in_progress" as const,
          toWorkflowStatus: "resolved" as const,
        },
      })),
    prepareRecoveryDraft:
      overrides.prepareRecoveryDraft
      ?? vi.fn(async () => ({
        status: "succeeded" as const,
        body: "Draft body",
        subject: "Draft subject",
        channel: "email" as const,
      })),
    recordInternalAction:
      overrides.recordInternalAction
      ?? vi.fn(async () => ({
        workflowStatus: "in_progress" as const,
        needsAttention: true,
        activityEvent: {
          kind: "internal_action_recorded" as const,
          at: "2026-07-17T12:00:00.000Z",
          actorDisplayName: "Alex",
          category: "team_briefed" as const,
          categoryLabel: "Team briefed",
          note: "Briefed the floor team.",
        },
      })),
    sendAndRecord:
      overrides.sendAndRecord
      ?? vi.fn(async () => ({
        workflowStatus: "in_progress" as const,
        needsAttention: true,
        guestResponseActivityEvent: {
          kind: "guest_response_sent" as const,
          at: "2026-07-17T12:00:00.000Z",
          actorDisplayName: "Alex",
          channel: "email" as const,
          maskedDestination: "m••••@email.com",
        },
        internalActionActivityEvent: {
          kind: "internal_action_recorded" as const,
          at: "2026-07-17T12:00:00.000Z",
          actorDisplayName: "Alex",
          category: "team_briefed" as const,
          categoryLabel: "Team briefed",
          note: "Briefed the floor team.",
        },
      })),
    sendAndIssueRecoveryOffer:
      overrides.sendAndIssueRecoveryOffer
      ?? vi.fn(async () => ({
        workflowStatus: "in_progress" as const,
        needsAttention: true,
        guestResponseActivityEvent: {
          kind: "guest_response_sent" as const,
          at: "2026-07-17T12:00:00.000Z",
          actorDisplayName: "Alex",
          channel: "email" as const,
          maskedDestination: "m••••@email.com",
        },
        recoveryOfferActivityEvent: {
          kind: "recovery_offer_issued" as const,
          at: "2026-07-17T12:00:00.000Z",
          actorDisplayName: "Alex",
          offerType: "percentage_discount" as const,
          title: "20% off",
          validity: "30_days_after_issue" as const,
          expiryAt: "2026-08-16T12:00:00.000Z",
          redemptionCode: "TUM-ABC123",
        },
        issuedOffer: {
          title: "20% off",
          redemptionCode: "TUM-ABC123",
          expiryAt: "2026-08-16T12:00:00.000Z",
          validity: "30_days_after_issue" as const,
        },
      })),
    prepareRecoveryOfferDraft:
      overrides.prepareRecoveryOfferDraft
      ?? vi.fn(async () => ({
        status: "succeeded" as const,
        body: "Offer draft body",
        subject: "Offer draft subject",
        channel: "email" as const,
      })),
    getRecoveryOfferAttach:
      overrides.getRecoveryOfferAttach ?? vi.fn(async () => null),
    setRecoveryOfferAttach:
      overrides.setRecoveryOfferAttach ?? vi.fn(async () => {}),
    listCatalogOffers:
      overrides.listCatalogOffers
      ?? vi.fn(async () => ({
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
    getOffer:
      overrides.getOffer
      ?? vi.fn(async () => {
        throw new Error("getOffer not stubbed")
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

  it("opens Export dialog with Current results default and live scope counts", async () => {
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({
        getFeedbackInbox: vi.fn(async () =>
          inboxResponse({
            totalCount: 3,
            tabCounts: {
              all: 12,
              needsAttention: 3,
              new: 4,
              inProgress: 2,
              resolved: 3,
            },
          })
        ),
      })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 7,
      locations: [{ id: 7, locationName: "Camden Street" }],
    })

    expect(pageModule.getSnapshot().exportDialog).toBeNull()
    pageModule.openExportDialog()
    const dialog = pageModule.getSnapshot().exportDialog
    expect(dialog).toMatchObject({
      scope: "current",
      format: "xlsx",
      includeGuestContact: false,
      currentResultsCount: 3,
      allInPeriodCount: 12,
      selectedCount: 3,
      canDownload: true,
      locationName: "Camden Street",
      isPreparing: false,
      errorMessage: null,
    })

    pageModule.setExportScope("all-in-period")
    expect(pageModule.getSnapshot().exportDialog).toMatchObject({
      scope: "all-in-period",
      selectedCount: 12,
      canDownload: true,
    })
  })

  it("disables Download export when selected scope count is 0", async () => {
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({
        getFeedbackInbox: vi.fn(async () =>
          inboxResponse({
            totalCount: 0,
            items: [],
            tabCounts: {
              all: 5,
              needsAttention: 0,
              new: 0,
              inProgress: 0,
              resolved: 5,
            },
          })
        ),
      })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Main" }],
    })
    pageModule.openExportDialog()
    expect(pageModule.getSnapshot().exportDialog?.canDownload).toBe(false)
    expect(pageModule.getSnapshot().exportDialog?.selectedCount).toBe(0)

    pageModule.setExportScope("all-in-period")
    expect(pageModule.getSnapshot().exportDialog?.canDownload).toBe(true)
    expect(pageModule.getSnapshot().exportDialog?.selectedCount).toBe(5)
  })

  it("downloads Current results CSV then closes the dialog", async () => {
    const exportFeedback = vi.fn(async () => ({
      blob: new Blob(["csv"], { type: "text/csv" }),
      filename: "tummly-feedback-7-20260717-120000Z.csv",
    }))
    const triggerBrowserDownload = vi.fn()
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({ exportFeedback, triggerBrowserDownload })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 7,
      locations: [{ id: 7, locationName: "Camden Street" }],
    })
    pageModule.openExportDialog()
    pageModule.setExportFormat("csv")
    pageModule.setExportIncludeGuestContact(true)
    await pageModule.downloadExport()

    expect(exportFeedback).toHaveBeenCalledWith(
      expect.objectContaining({
        locationId: 7,
        scope: "current",
        format: "csv",
        includeGuestContact: true,
        tab: "all",
      })
    )
    expect(triggerBrowserDownload).toHaveBeenCalledWith(
      expect.any(Blob),
      "tummly-feedback-7-20260717-120000Z.csv"
    )
    expect(pageModule.getSnapshot().exportDialog).toBeNull()
  })

  it("keeps dialog open with soft-max error and does not download", async () => {
    const exportFeedback = vi.fn(async () => {
      throw new Error(
        "Export exceeds 10,000 rows. Narrow filters and try again."
      )
    })
    const triggerBrowserDownload = vi.fn()
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({
        getFeedbackInbox: vi.fn(async () =>
          inboxResponse({ totalCount: 10001 })
        ),
        exportFeedback,
        triggerBrowserDownload,
      })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Main" }],
    })
    pageModule.openExportDialog()
    await pageModule.downloadExport()

    expect(triggerBrowserDownload).not.toHaveBeenCalled()
    const dialog = pageModule.getSnapshot().exportDialog
    expect(dialog).not.toBeNull()
    expect(dialog?.isPreparing).toBe(false)
    expect(dialog?.errorMessage).toBe(
      "Export exceeds 10,000 rows. Narrow filters and try again."
    )
  })

  it("exports All in period without inbox tab filters", async () => {
    const exportFeedback = vi.fn(async () => ({
      blob: new Blob(["x"], {
        type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      }),
      filename: "tummly-feedback-1-20260717-120000Z.xlsx",
    }))
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({ exportFeedback })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Main" }],
    })
    pageModule.setActiveInboxTabId("needs-attention")
    await vi.waitFor(() => {
      expect(pageModule.getSnapshot().activeInboxTabId).toBe("needs-attention")
    })
    pageModule.openExportDialog()
    pageModule.setExportScope("all-in-period")
    await pageModule.downloadExport()

    expect(exportFeedback).toHaveBeenCalledWith(
      expect.objectContaining({
        scope: "all-in-period",
        format: "xlsx",
        includeGuestContact: false,
      })
    )
    const callArgs = exportFeedback.mock.calls.at(0) as
      | [{ tab?: string; q?: string }]
      | undefined
    expect(callArgs).toBeDefined()
    const call = callArgs![0]
    expect(call.tab).toBeUndefined()
    expect(call.q).toBeUndefined()
  })

  it("startInboxRecovery closes Feedback details and opens the shared Start recovery shell", async () => {
    const setWorkflowStatus = vi.fn(async () => ({
      workflowStatus: "in_progress" as const,
      needsAttention: true,
      activityEvent: null,
    }))
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({
        getFeedbackDetails: vi.fn(async (feedbackId: number) => ({
          ...sampleDetails,
          id: feedbackId,
          workflowStatus: "new" as const,
          guestOffersOptOut: false,
        })),
        setWorkflowStatus,
      })
    )
    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Main" }],
    })
    await pageModule.openFeedbackDetails(42)
    expect(pageModule.getSnapshot().feedbackDetails.isOpen).toBe(true)

    await pageModule.startInboxRecovery(42)

    expect(pageModule.getSnapshot().feedbackDetails.isOpen).toBe(false)
    expect(pageModule.getSnapshot().startRecovery).toMatchObject({
      isOpen: true,
      loadStatus: "loaded",
      feedbackId: 42,
      workflowStatus: "in_progress",
    })
    expect(setWorkflowStatus).toHaveBeenCalledWith(42, "in_progress")
  })
})

import { describe, expect, it, vi } from "vitest"

import {
  createOperatorHomePageModule,
  type FeedbackHomeRealtimeHandlers,
} from "./createOperatorHomePageModule"
import type { FeedbackItem, LocationItem } from "@/types/dashboard"

const locations: LocationItem[] = [
  {
    id: 1,
    locationName: "First Venue",
    address: "1 High St",
    guestUrl: "https://guest.example/1",
    createdAt: "2026-01-01T00:00:00.000Z",
  },
  {
    id: 2,
    locationName: "Second Venue",
    address: "2 High St",
    guestUrl: "https://guest.example/2",
    createdAt: "2026-02-01T00:00:00.000Z",
  },
]

const recentFeedback: FeedbackItem[] = [
  {
    id: 10,
    guestName: "Alex",
    guestContact: "alex@example.com",
    contactType: "Email",
    comment: "Great food",
    createdAt: "2026-07-10T12:00:00.000Z",
    classificationStatus: "Pending",
    sentiment: null,
    detectedTags: null,
  },
]

function workspaceInput(
  overrides: Partial<{
    locations: LocationItem[]
    selectedLocationId: number | null
  }> = {}
) {
  return {
    locations,
    selectedLocationId: 1 as number | null,
    ...overrides,
  }
}

function asLatestActivityItems(items: FeedbackItem[] = recentFeedback) {
  return items.map((item) => ({
    kind: "feedback" as const,
    locationGuestId: null,
    ...item,
  }))
}

function createAdapters(overrides: {
  getFeedback?: (locationId: number) => Promise<{
    success: boolean
    total: number
    recent: FeedbackItem[]
  }>
  getHomeLatestActivity?: (locationId: number) => Promise<{
    success: boolean
    items: ReturnType<typeof asLatestActivityItems>
  }>
  getHomePerformance?: (
    locationId: number,
    from: string,
    to: string
  ) => Promise<{
    success: boolean
    feedbackSubmitted: number
    feedbackSubmittedPrevious: number
    guestsJoined: number
    guestsJoinedPrevious: number
    qrScans: number
    qrScansPrevious: number
  }>
  getHomePerformanceDateRange?: () =>
    | {
        kind: "preset"
        presetId: "last7" | "last30" | "thisMonth"
      }
    | {
        kind: "custom"
        startDate: string
        endDate: string
      }
  getFeedbackDetails?: (feedbackId: number) => Promise<{
    success: boolean
    id: number
    guestName: string
    guestContact: string
    contactType: "Email" | "Phone" | "Unknown"
    comment: string
    createdAt: string
    locationName: string
    address: string
    classificationStatus: "Pending" | "Succeeded" | "Failed"
    sentiment: "positive" | "neutral" | "negative" | null
    detectedTags: string[] | null
    locationGuestId: number | null
  }>
  correctClassification?: (
    feedbackId: number,
    sentiment: "positive" | "neutral" | "negative"
  ) => Promise<{
    classificationStatus: "Pending" | "Succeeded" | "Failed"
    sentiment: "positive" | "neutral" | "negative" | null
    detectedTags: string[] | null
  }>
  setWorkflowStatus?: (
    feedbackId: number,
    workflowStatus: "new" | "in_progress" | "resolved"
  ) => Promise<{
    workflowStatus: "new" | "in_progress" | "resolved"
    needsAttention: boolean
    activityEvent?: {
      kind: "workflow_status_changed"
      at: string
      actorDisplayName?: string | null
      fromWorkflowStatus?: "new" | "in_progress" | "resolved" | null
      toWorkflowStatus?: "new" | "in_progress" | "resolved" | null
    } | null
  }>
  createInternalNote?: (
    feedbackId: number,
    body: string
  ) => Promise<{
    id: number
    body: string
    authorDisplayName: string
    createdAt: string
  }>
  updateInternalNote?: (
    feedbackId: number,
    noteId: number,
    body: string
  ) => Promise<{
    id: number
    body: string
    authorDisplayName: string
    createdAt: string
    updatedAt?: string
  }>
  deleteInternalNote?: (feedbackId: number, noteId: number) => Promise<{ deletedAt: string; deletedByDisplayName: string }>
  getChecklistAcks?: (locationId: number) => Promise<{
    success: boolean
    locationId: number
    guestFormPreviewed: boolean
    qrPlacementGuideViewed: boolean
    logoUploaded: boolean
    guestFormPreviewedAt: string | null
    qrPlacementGuideViewedAt: string | null
    logoUploadedAt: string | null
  }>
  setChecklistAcks?: (
    locationId: number,
    body: {
      guestFormPreviewed?: boolean
      qrPlacementGuideViewed?: boolean
      logoUploaded?: boolean
    }
  ) => Promise<{
    success: boolean
    locationId: number
    guestFormPreviewed: boolean
    qrPlacementGuideViewed: boolean
    logoUploaded: boolean
    guestFormPreviewedAt: string | null
    qrPlacementGuideViewedAt: string | null
    logoUploadedAt: string | null
  }>
  copyText?: (
    text: string
  ) => Promise<{ ok: true } | { ok: false; error: string }>
  openSmartGuestLink?: (url: string) => void
  connectRealtime?: (
    handlers: FeedbackHomeRealtimeHandlers
  ) => Promise<{ stop: () => Promise<void> }>
  onPerformanceLoadError?: (message: string) => void
} = {}) {
  return {
    getFeedback:
      overrides.getFeedback ??
      (async () => ({
        success: true,
        total: recentFeedback.length,
        recent: recentFeedback,
      })),
    getHomeLatestActivity:
      overrides.getHomeLatestActivity ??
      (async () => ({
        success: true,
        items: asLatestActivityItems(),
      })),
    getHomePerformance:
      overrides.getHomePerformance ??
      (async () => ({
        success: true,
        feedbackSubmitted: recentFeedback.length,
        feedbackSubmittedPrevious: 0,
        guestsJoined: 0,
        guestsJoinedPrevious: 0,
        qrScans: 0,
        qrScansPrevious: 0,
      })),
    getHomePerformanceDateRange:
      overrides.getHomePerformanceDateRange ??
      (() => ({
        kind: "preset" as const,
        presetId: "last7" as const,
      })),
    getFeedbackDetails:
      overrides.getFeedbackDetails ??
      (async (feedbackId: number) => ({
        success: true,
        id: feedbackId,
        guestName: "Alex",
        guestContact: "alex@example.com",
        contactType: "Email" as const,
        comment: "Great food",
        createdAt: "2026-07-14T11:00:00.000Z",
        locationName: "First Venue",
        address: "1 High St",
        classificationStatus: "Pending" as const,
        sentiment: null,
        detectedTags: null,
        locationGuestId: null,
      })),
    correctClassification:
      overrides.correctClassification
      ?? (async (_feedbackId, sentiment) => ({
        classificationStatus: "Succeeded" as const,
        sentiment,
        detectedTags: [] as string[],
      })),
    setWorkflowStatus:
      overrides.setWorkflowStatus
      ?? (async (
        _feedbackId: number,
        workflowStatus: "new" | "in_progress" | "resolved"
      ) => ({
        workflowStatus,
        needsAttention: false,
        activityEvent: null as null,
      })),
    createInternalNote:
      overrides.createInternalNote
      ?? (async (_feedbackId, body) => ({
        id: 1,
        body,
        authorDisplayName: "Test Operator",
        createdAt: "2026-07-14T12:00:00.000Z",
      })),
    updateInternalNote:
      overrides.updateInternalNote
      ?? (async (_feedbackId, noteId, body) => ({
        id: noteId,
        body,
        authorDisplayName: "Test Operator",
        createdAt: "2026-07-14T12:00:00.000Z",
        updatedAt: "2026-07-14T12:30:00.000Z",
      })),
    deleteInternalNote: overrides.deleteInternalNote ?? (async () => ({ deletedAt: "2026-07-14T13:00:00.000Z", deletedByDisplayName: "Ada Operator" })),
    getChecklistAcks:
      overrides.getChecklistAcks ??
      (async () => ({
        success: true,
        locationId: 1,
        guestFormPreviewed: false,
        qrPlacementGuideViewed: false,
        logoUploaded: false,
        guestFormPreviewedAt: null,
        qrPlacementGuideViewedAt: null,
        logoUploadedAt: null,
      })),
    setChecklistAcks:
      overrides.setChecklistAcks ??
      (async (_locationId, body) => ({
        success: true,
        locationId: 1,
        guestFormPreviewed: body.guestFormPreviewed ?? false,
        qrPlacementGuideViewed: body.qrPlacementGuideViewed ?? false,
        logoUploaded: body.logoUploaded ?? false,
        guestFormPreviewedAt: body.guestFormPreviewed
          ? "2026-07-14T12:00:00.000Z"
          : null,
        qrPlacementGuideViewedAt: null,
        logoUploadedAt: body.logoUploaded ? "2026-07-14T12:00:00.000Z" : null,
      })),
    copyText: overrides.copyText ?? (async () => ({ ok: true as const })),
    openSmartGuestLink: overrides.openSmartGuestLink ?? vi.fn(),
    connectRealtime:
      overrides.connectRealtime
      ?? (async () => ({ stop: async () => {} })),
    onPerformanceLoadError: overrides.onPerformanceLoadError,
  }
}

describe("createOperatorHomePageModule", () => {
  it("loads Guests joined from home performance with hasRealData true including zero", async () => {
    const getHomePerformance = vi.fn(async (_locationId: number, _from: string, _to: string) => ({
      success: true,
      feedbackSubmitted: 3,
      feedbackSubmittedPrevious: 0,
      guestsJoined: 0,
      guestsJoinedPrevious: 0,
      qrScans: 0,
      qrScansPrevious: 0,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({ getHomePerformance })
    )

    await home.syncWorkspace(workspaceInput())

    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "guests-joined")
    ).toMatchObject({
      value: 0,
      hasRealData: true,
    })
  })

  it("maps a non-zero Guests joined count from home performance", async () => {
    const getHomePerformance = vi.fn(async (_locationId: number, _from: string, _to: string) => ({
      success: true,
      feedbackSubmitted: 2,
      feedbackSubmittedPrevious: 0,
      guestsJoined: 5,
      guestsJoinedPrevious: 0,
      qrScans: 0,
      qrScansPrevious: 0,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({ getHomePerformance })
    )

    await home.syncWorkspace(workspaceInput())

    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "guests-joined")
    ).toMatchObject({
      value: 5,
      hasRealData: true,
    })
  })

  it("maps Feedback submitted trend vs previous equal-length period", async () => {
    const getHomePerformance = vi.fn(async (_locationId: number, _from: string, _to: string) => ({
      success: true,
      feedbackSubmitted: 12,
      feedbackSubmittedPrevious: 8,
      guestsJoined: 3,
      guestsJoinedPrevious: 6,
      qrScans: 20,
      qrScansPrevious: 10,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({ getHomePerformance })
    )

    await home.syncWorkspace(workspaceInput())

    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
    ).toMatchObject({
      value: 12,
      trendPercent: 50,
      hasRealData: true,
    })
    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "guests-joined")
    ).toMatchObject({
      value: 3,
      trendPercent: -50,
      hasRealData: true,
    })
    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "qr-scans")
    ).toMatchObject({
      value: 20,
      trendPercent: 100,
      hasRealData: true,
    })
  })

  it("loads Feedback submitted from home performance for the default Last 7 days window", async () => {
    const getHomePerformance = vi.fn(async (_locationId: number, _from: string, _to: string) => ({
      success: true,
      feedbackSubmitted: 7,
      feedbackSubmittedPrevious: 0,
      guestsJoined: 0,
      guestsJoinedPrevious: 0,
      qrScans: 0,
      qrScansPrevious: 0,
    }))
    const getFeedback = vi.fn(async () => ({
      success: true,
      total: 99,
      recent: recentFeedback,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({ getFeedback, getHomePerformance })
    )

    await home.syncWorkspace(workspaceInput())

    expect(getHomePerformance).toHaveBeenCalledTimes(1)
    const [, from, to] = getHomePerformance.mock.calls[0]
    const spanMs = new Date(to).getTime() - new Date(from).getTime()
    expect(spanMs).toBeGreaterThanOrEqual(6 * 24 * 60 * 60 * 1000)
    expect(spanMs).toBeLessThanOrEqual(7 * 24 * 60 * 60 * 1000)
    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
    ).toMatchObject({
      value: 7,
      hasRealData: true,
    })
    expect(home.getSnapshot().viewModel?.dateRangeLabel).toBe("Last 7 days")
  })

  it("keeps the Home body loaded when the initial performance fetch fails", async () => {
    const onPerformanceLoadError = vi.fn()
    const getHomePerformance = vi.fn(async () => {
      throw new Error("performance unavailable")
    })
    const getFeedback = vi.fn(async () => ({
      success: true,
      total: 3,
      recent: recentFeedback,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({
        getFeedback,
        getHomePerformance,
        onPerformanceLoadError,
      })
    )

    await home.syncWorkspace(workspaceInput())

    expect(home.getSnapshot().loadStatus).toBe("loaded")
    expect(home.getSnapshot().performanceLoadStatus).toBe("error")
    expect(home.getSnapshot().viewModel).not.toBeNull()
    expect(
      home.getSnapshot().viewModel?.activityByTab.feedback
    ).toHaveLength(recentFeedback.length)
    expect(onPerformanceLoadError).toHaveBeenCalledWith(
      "Could not load performance stats. Please try again."
    )
  })

  it("refetches Feedback submitted with Last 30 days bounds when that preset is applied", async () => {
    let range: {
      kind: "preset"
      presetId: "last7" | "last30" | "thisMonth"
    } = { kind: "preset", presetId: "last7" }
    const getHomePerformance = vi.fn(async (_locationId: number, _from: string, _to: string) => ({
      success: true,
      feedbackSubmitted: range.presetId === "last30" ? 12 : 3,
      feedbackSubmittedPrevious: 0,
      guestsJoined: 0,
      guestsJoinedPrevious: 0,
      qrScans: 0,
      qrScansPrevious: 0,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({
        getHomePerformance,
        getHomePerformanceDateRange: () => range,
      })
    )

    await home.syncWorkspace(workspaceInput())
    expect(getHomePerformance).toHaveBeenCalledTimes(1)

    range = { kind: "preset", presetId: "last30" }
    await home.reloadForHomePerformanceDateRange()

    expect(getHomePerformance).toHaveBeenCalledTimes(2)
    const [, from, to] = getHomePerformance.mock.calls[1]
    const spanMs = new Date(to).getTime() - new Date(from).getTime()
    expect(spanMs).toBeGreaterThanOrEqual(29 * 24 * 60 * 60 * 1000)
    expect(spanMs).toBeLessThanOrEqual(30 * 24 * 60 * 60 * 1000)
    expect(home.getSnapshot().viewModel?.dateRangeLabel).toBe("Last 30 days")
    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
    ).toMatchObject({ value: 12 })
  })

  it("does not reload Latest activity when the Home performance date range changes", async () => {
    let range: {
      kind: "preset"
      presetId: "last7" | "last30" | "thisMonth"
    } = { kind: "preset", presetId: "last7" }
    const getFeedback = vi.fn(async () => ({
      success: true,
      total: 1,
      recent: recentFeedback,
    }))
    const getHomeLatestActivity = vi.fn(async () => ({
      success: true,
      items: asLatestActivityItems(),
    }))
    const getHomePerformance = vi.fn(async (_locationId: number, _from: string, _to: string) => ({
      success: true,
      feedbackSubmitted: range.presetId === "last30" ? 12 : 3,
      feedbackSubmittedPrevious: 0,
      guestsJoined: 0,
      guestsJoinedPrevious: 0,
      qrScans: 0,
      qrScansPrevious: 0,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({
        getFeedback,
        getHomeLatestActivity,
        getHomePerformance,
        getHomePerformanceDateRange: () => range,
      })
    )

    await home.syncWorkspace(workspaceInput())
    expect(getFeedback).toHaveBeenCalledTimes(1)
    expect(getHomeLatestActivity).toHaveBeenCalledTimes(1)
    expect(getHomePerformance).toHaveBeenCalledTimes(1)
    expect(home.getSnapshot().loadStatus).toBe("loaded")

    range = { kind: "preset", presetId: "last30" }
    await home.reloadForHomePerformanceDateRange()

    expect(getFeedback).toHaveBeenCalledTimes(1)
    expect(getHomeLatestActivity).toHaveBeenCalledTimes(1)
    expect(getHomePerformance).toHaveBeenCalledTimes(2)
    expect(home.getSnapshot().loadStatus).toBe("loaded")
    expect(home.getSnapshot().performanceLoadStatus).toBe("loaded")
  })

  it("refetches Feedback submitted with This month bounds when that preset is applied", async () => {
    let range: {
      kind: "preset"
      presetId: "last7" | "last30" | "thisMonth"
    } = { kind: "preset", presetId: "last7" }
    const getHomePerformance = vi.fn(async (_locationId: number, _from: string, _to: string) => ({
      success: true,
      feedbackSubmitted: 4,
      feedbackSubmittedPrevious: 0,
      guestsJoined: 0,
      guestsJoinedPrevious: 0,
      qrScans: 0,
      qrScansPrevious: 0,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({
        getHomePerformance,
        getHomePerformanceDateRange: () => range,
      })
    )

    await home.syncWorkspace(workspaceInput())
    range = { kind: "preset", presetId: "thisMonth" }
    await home.reloadForHomePerformanceDateRange()

    const [, from, to] = getHomePerformance.mock.calls[1]
    const fromDate = new Date(from)
    const toDate = new Date(to)
    expect(fromDate.getDate()).toBe(1)
    expect(fromDate.getMonth()).toBe(toDate.getMonth())
    expect(fromDate.getFullYear()).toBe(toDate.getFullYear())
    expect(home.getSnapshot().viewModel?.dateRangeLabel).toBe("This month")
  })

  it("keeps the committed Home performance date range when switching Owned location", async () => {
    let range: {
      kind: "preset"
      presetId: "last7" | "last30" | "thisMonth"
    } = { kind: "preset", presetId: "last30" }
    const getHomePerformance = vi.fn(async (_locationId: number, _from: string, _to: string) => ({
      success: true,
      feedbackSubmitted: 9,
      feedbackSubmittedPrevious: 0,
      guestsJoined: 0,
      guestsJoinedPrevious: 0,
      qrScans: 0,
      qrScansPrevious: 0,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({
        getHomePerformance,
        getHomePerformanceDateRange: () => range,
      })
    )

    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    await home.syncWorkspace(workspaceInput({ selectedLocationId: 2 }))

    expect(getHomePerformance).toHaveBeenLastCalledWith(
      2,
      expect.any(String),
      expect.any(String)
    )
    const [, from, to] = getHomePerformance.mock.calls.at(-1)!
    const spanMs = new Date(to).getTime() - new Date(from).getTime()
    expect(spanMs).toBeGreaterThanOrEqual(29 * 24 * 60 * 60 * 1000)
    expect(spanMs).toBeLessThanOrEqual(30 * 24 * 60 * 60 * 1000)
    expect(home.getSnapshot().viewModel?.dateRangeLabel).toBe("Last 30 days")
  })

  it("refetches Feedback submitted with Custom inclusive local bounds when applied", async () => {
    let range:
      | {
          kind: "preset"
          presetId: "last7" | "last30" | "thisMonth"
        }
      | { kind: "custom"; startDate: string; endDate: string } = {
      kind: "preset",
      presetId: "last7",
    }
    const getHomePerformance = vi.fn(async (_locationId: number, _from: string, _to: string) => ({
      success: true,
      feedbackSubmitted: 5,
      feedbackSubmittedPrevious: 0,
      guestsJoined: 0,
      guestsJoinedPrevious: 0,
      qrScans: 0,
      qrScansPrevious: 0,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({
        getHomePerformance,
        getHomePerformanceDateRange: () => range,
      })
    )

    await home.syncWorkspace(workspaceInput())
    range = {
      kind: "custom",
      startDate: "2026-07-12",
      endDate: "2026-07-18",
    }
    await home.reloadForHomePerformanceDateRange()

    expect(getHomePerformance).toHaveBeenCalledTimes(2)
    const [, from, to] = getHomePerformance.mock.calls[1]
    const fromDate = new Date(from)
    const toDate = new Date(to)
    expect(fromDate.getFullYear()).toBe(2026)
    expect(fromDate.getMonth()).toBe(6)
    expect(fromDate.getDate()).toBe(12)
    expect(fromDate.getHours()).toBe(0)
    expect(toDate.getFullYear()).toBe(2026)
    expect(toDate.getMonth()).toBe(6)
    expect(toDate.getDate()).toBe(19)
    expect(toDate.getHours()).toBe(0)
    expect(home.getSnapshot().viewModel?.dateRangeLabel).toBe("12–18 Jul 2026")
    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
    ).toMatchObject({ value: 5 })
  })

  it("loads feedback and checklist acks for the selected Owned location", async () => {
    const adapters = createAdapters()
    const home = createOperatorHomePageModule(adapters)

    expect(home.getSnapshot().loadStatus).toBe("idle")

    const loadPromise = home.syncWorkspace(workspaceInput())
    expect(home.getSnapshot().loadStatus).toBe("loading")
    expect(home.getSnapshot().viewModel?.selectedLocationId).toBe(1)

    await loadPromise

    const snapshot = home.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel?.kpis.find((kpi) => kpi.id === "feedback")).toMatchObject({
      value: 1,
      hasRealData: true,
    })
    expect(
      snapshot.viewModel?.setupSteps.find((step) => step.id === "guest-form")?.status
    ).toBe("partial")
  })

  it("reloads Home data when the selected Owned location changes", async () => {
    const getFeedback = vi.fn(async (locationId: number) => ({
      success: true,
      total: locationId === 2 ? 0 : 1,
      recent: locationId === 2 ? [] : recentFeedback,
    }))
    const getHomePerformance = vi.fn(async (locationId: number, _from: string, _to: string) => ({
      success: true,
      feedbackSubmitted: locationId === 2 ? 0 : 1,
      feedbackSubmittedPrevious: 0,
      guestsJoined: 0,
      guestsJoinedPrevious: 0,
      qrScans: 0,
      qrScansPrevious: 0,
    }))
    const getChecklistAcks = vi.fn(async (locationId: number) => ({
      success: true,
      locationId,
      guestFormPreviewed: locationId === 2,
      qrPlacementGuideViewed: false,
      logoUploaded: false,
      guestFormPreviewedAt: locationId === 2 ? "2026-07-14T12:00:00.000Z" : null,
      qrPlacementGuideViewedAt: null,
      logoUploadedAt: null,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({ getFeedback, getHomePerformance, getChecklistAcks })
    )

    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    await home.syncWorkspace(workspaceInput({ selectedLocationId: 2 }))

    expect(getFeedback).toHaveBeenLastCalledWith(2)
    expect(getHomePerformance).toHaveBeenLastCalledWith(
      2,
      expect.any(String),
      expect.any(String)
    )
    expect(home.getSnapshot().viewModel?.selectedLocationId).toBe(2)
    expect(
      home
        .getSnapshot()
        .viewModel?.setupSteps.find((step) => step.id === "guest-form")?.status
    ).toBe("complete")
    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
    ).toMatchObject({ value: 0 })
  })

  it("keeps previous Feedback submitted while switching Owned location", async () => {
    const deferred: {
      resolve: ((value: {
        success: boolean
        feedbackSubmitted: number
        feedbackSubmittedPrevious: number
        guestsJoined: number
        guestsJoinedPrevious: number
    qrScans: number
    qrScansPrevious: number
      }) => void) | null
    } = { resolve: null }
    let call = 0
    const getHomePerformance = vi.fn(
      (_locationId: number, _from: string, _to: string) => {
        call += 1
        if (call === 1) {
          return Promise.resolve({ success: true, feedbackSubmitted: 5, feedbackSubmittedPrevious: 0, guestsJoined: 0, guestsJoinedPrevious: 0, qrScans: 0, qrScansPrevious: 0 })
        }
        return new Promise<{ success: boolean; feedbackSubmitted: number; feedbackSubmittedPrevious: number; guestsJoined: number; guestsJoinedPrevious: number
    qrScans: number
    qrScansPrevious: number }>(
          (resolve) => {
            deferred.resolve = resolve
          }
        )
      }
    )
    const home = createOperatorHomePageModule(
      createAdapters({ getHomePerformance })
    )

    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
    ).toMatchObject({ value: 5 })

    const switchPromise = home.syncWorkspace(
      workspaceInput({ selectedLocationId: 2 })
    )

    await vi.waitFor(() => {
      expect(home.getSnapshot().performanceLoadStatus).toBe("loading")
    })
    expect(home.getSnapshot().loadStatus).toBe("loaded")
    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
    ).toMatchObject({ value: 5 })

    deferred.resolve?.({ success: true, feedbackSubmitted: 2, feedbackSubmittedPrevious: 0, guestsJoined: 0, guestsJoinedPrevious: 0, qrScans: 0, qrScansPrevious: 0 })
    await switchPromise

    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
    ).toMatchObject({ value: 2 })
  })

  it("previews the guest form, acknowledges optimistically, and persists", async () => {
    const setChecklistAcks = vi.fn(async () => ({
      success: true,
      locationId: 1,
      guestFormPreviewed: true,
      qrPlacementGuideViewed: false,
      logoUploaded: false,
      guestFormPreviewedAt: "2026-07-14T12:00:00.000Z",
      qrPlacementGuideViewedAt: null,
      logoUploadedAt: null,
    }))
    const openSmartGuestLink = vi.fn()
    const home = createOperatorHomePageModule(
      createAdapters({ setChecklistAcks, openSmartGuestLink })
    )
    await home.syncWorkspace(workspaceInput())

    home.previewGuestForm()

    expect(openSmartGuestLink).toHaveBeenCalledWith("https://guest.example/1")
    expect(
      home
        .getSnapshot()
        .viewModel?.setupSteps.find((step) => step.id === "guest-form")?.status
    ).toBe("complete")
    expect(home.getSnapshot().previewBusy).toBe(true)

    await vi.waitFor(() => {
      expect(home.getSnapshot().previewBusy).toBe(false)
    })

    expect(setChecklistAcks).toHaveBeenCalledWith(1, {
      guestFormPreviewed: true,
    })
    expect(setChecklistAcks).not.toHaveBeenCalledWith(
      1,
      expect.objectContaining({ logoUploaded: true })
    )
    expect(home.getSnapshot().actionError).toBeNull()
  })

  it("does not re-POST Preview acknowledgement when already previewed", async () => {
    const setChecklistAcks = vi.fn()
    const home = createOperatorHomePageModule(
      createAdapters({
        setChecklistAcks,
        getChecklistAcks: async () => ({
          success: true,
          locationId: 1,
          guestFormPreviewed: true,
          qrPlacementGuideViewed: false,
          logoUploaded: false,
          guestFormPreviewedAt: "2026-07-14T12:00:00.000Z",
          qrPlacementGuideViewedAt: null,
          logoUploadedAt: null,
        }),
      })
    )
    await home.syncWorkspace(workspaceInput())

    home.previewGuestForm()

    expect(setChecklistAcks).not.toHaveBeenCalled()
    expect(home.getSnapshot().previewBusy).toBe(false)
  })

  it("rolls back Preview acknowledgement and surfaces a recoverable error on failure", async () => {
    const home = createOperatorHomePageModule(
      createAdapters({
        setChecklistAcks: async () => {
          throw new Error("network")
        },
      })
    )
    await home.syncWorkspace(workspaceInput())

    home.previewGuestForm()
    await vi.waitFor(() => {
      expect(home.getSnapshot().previewBusy).toBe(false)
    })

    expect(
      home
        .getSnapshot()
        .viewModel?.setupSteps.find((step) => step.id === "guest-form")?.status
    ).toBe("partial")
    expect(home.getSnapshot().actionError).toBe(
      "Could not save checklist progress. Please try again."
    )
  })

  it("copies the selected Owned location Smart Guest Link and surfaces copy errors", async () => {
    const copyText = vi
      .fn()
      .mockResolvedValueOnce({ ok: true })
      .mockResolvedValueOnce({
        ok: false,
        error: "Could not copy Smart Guest Link. Please try again.",
      })
    const home = createOperatorHomePageModule(createAdapters({ copyText }))
    await home.syncWorkspace(workspaceInput())

    await expect(home.copySmartGuestLink()).resolves.toBe("copied")
    expect(copyText).toHaveBeenCalledWith("https://guest.example/1")
    expect(home.getSnapshot().actionError).toBeNull()

    await expect(home.copySmartGuestLink()).resolves.toBe("failed")
    expect(home.getSnapshot().actionError).toBe(
      "Could not copy Smart Guest Link. Please try again."
    )
  })

  it("does not copy when the selected location has no Smart Guest Link", async () => {
    const copyText = vi.fn()
    const home = createOperatorHomePageModule(createAdapters({ copyText }))
    await home.syncWorkspace({
      ...workspaceInput(),
      locations: [
        {
          ...workspaceInput().locations[0],
          guestUrl: "",
        },
      ],
    })

    await expect(home.copySmartGuestLink()).resolves.toBe("noop")
    expect(copyText).not.toHaveBeenCalled()
  })

  it("keeps the shell usable when Home feedback load fails and recovers on retry", async () => {
    const getFeedback = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce({
        success: true,
        total: 1,
        recent: recentFeedback,
      })
    const home = createOperatorHomePageModule(createAdapters({ getFeedback }))

    await home.syncWorkspace(workspaceInput())
    expect(home.getSnapshot().loadStatus).toBe("error")
    expect(home.getSnapshot().viewModel?.selectedLocationId).toBe(1)

    await home.retryLoad()
    expect(home.getSnapshot().loadStatus).toBe("loaded")
    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
        ?.value
    ).toBe(1)
  })

  it("degrades missing checklist acks to empty acknowledgements", async () => {
    const home = createOperatorHomePageModule(
      createAdapters({
        getChecklistAcks: async () => {
          throw new Error("acks unavailable")
        },
      })
    )

    await home.syncWorkspace(workspaceInput())

    expect(home.getSnapshot().loadStatus).toBe("loaded")
    expect(
      home
        .getSnapshot()
        .viewModel?.setupSteps.find((step) => step.id === "guest-form")?.status
    ).toBe("partial")
  })

  it("clears the Home body when there is no selected Owned location", async () => {
    const home = createOperatorHomePageModule(createAdapters())
    await home.syncWorkspace(workspaceInput())
    await home.syncWorkspace(workspaceInput({ selectedLocationId: null }))

    expect(home.getSnapshot()).toMatchObject({
      loadStatus: "idle",
      viewModel: null,
    })
  })

  it("forwards Feedback details open/close and exposes the details snapshot", async () => {
    const getFeedbackDetails = vi.fn(async (feedbackId: number) => ({
      success: true,
      id: feedbackId,
      guestName: "Alex",
      guestContact: "alex@example.com",
      contactType: "Email" as const,
      comment: "Great food",
      createdAt: "2026-07-14T11:00:00.000Z",
      locationName: "First Venue",
      address: "1 High St",
      qrSource: "Counter card",
      classificationStatus: "Pending" as const,
      sentiment: null,
      detectedTags: null,
      locationGuestId: null,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({ getFeedbackDetails })
    )
    await home.syncWorkspace(workspaceInput())

    expect(
      home.getSnapshot().viewModel?.activityByTab.feedback[0]
    ).toMatchObject({
      feedbackId: 10,
      canViewFeedback: true,
      canViewGuest: false,
    })

    const openPromise = home.openFeedbackDetails(10)
    expect(home.getSnapshot().feedbackDetails).toMatchObject({
      isOpen: true,
      loadStatus: "loading",
      feedbackId: 10,
    })

    await openPromise

    expect(getFeedbackDetails).toHaveBeenCalledWith(10)
    expect(home.getSnapshot().feedbackDetails).toMatchObject({
      isOpen: true,
      loadStatus: "loaded",
      details: {
        id: 10,
        guestName: "Alex",
        venueLine: "First Venue · Counter card",
        feedbackReference: "FDB-000010",
        contactAvailability: "Email",
      },
    })

    home.closeFeedbackDetails()
    expect(home.getSnapshot().feedbackDetails).toMatchObject({
      isOpen: false,
      loadStatus: "idle",
      details: null,
    })
  })

  it("resets Feedback details when the selected Owned location changes", async () => {
    const getFeedbackDetails = vi.fn(async (feedbackId: number) => ({
      success: true,
      id: feedbackId,
      guestName: "Alex",
      guestContact: "alex@example.com",
      contactType: "Email" as const,
      comment: "Great food",
      createdAt: "2026-07-14T11:00:00.000Z",
      locationName: "First Venue",
      address: "1 High St",
      classificationStatus: "Pending" as const,
      sentiment: null,
      detectedTags: null,
      locationGuestId: null,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({ getFeedbackDetails })
    )
    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    await home.openFeedbackDetails(10)
    expect(home.getSnapshot().feedbackDetails.isOpen).toBe(true)

    await home.syncWorkspace(workspaceInput({ selectedLocationId: 2 }))

    expect(home.getSnapshot().feedbackDetails).toMatchObject({
      isOpen: false,
      loadStatus: "idle",
      details: null,
    })
  })

  it("connect starts Feedback/Home realtime session", async () => {
    const connectRealtime = vi.fn(async () => ({ stop: async () => {} }))
    const home = createOperatorHomePageModule(
      createAdapters({ connectRealtime })
    )

    await home.connect()

    expect(connectRealtime).toHaveBeenCalledTimes(1)
  })

  it("matching classification-terminal signal refetches Latest activity", async () => {
    const realtime = {
      handlers: null as FeedbackHomeRealtimeHandlers | null,
    }
    let recent = recentFeedback
    let feedbackSubmitted = 1
    const getFeedback = vi.fn(async () => ({
      success: true,
      total: recent.length,
      recent,
    }))
    const getHomeLatestActivity = vi.fn(async () => ({
      success: true,
      items: recent.map((item) => ({
        kind: "feedback" as const,
        locationGuestId: null,
        ...item,
      })),
    }))
    const getHomePerformance = vi.fn(async (_locationId: number, _from: string, _to: string) => ({
      success: true,
      feedbackSubmitted,
      feedbackSubmittedPrevious: 0,
      guestsJoined: 0,
      guestsJoinedPrevious: 0,
      qrScans: 0,
      qrScansPrevious: 0,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({
        getFeedback,
        getHomeLatestActivity,
        getHomePerformance,
        connectRealtime: async (handlers) => {
          realtime.handlers = handlers
          return { stop: async () => {} }
        },
      })
    )

    await home.connect()
    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    expect(getFeedback).toHaveBeenCalledTimes(1)
    expect(getHomeLatestActivity).toHaveBeenCalledTimes(1)
    expect(getHomePerformance).toHaveBeenCalledTimes(1)

    recent = [
      {
        ...recentFeedback[0],
        classificationStatus: "Succeeded",
        sentiment: "negative",
        detectedTags: ["FoodQuality"],
      },
    ]
    feedbackSubmitted = 2
    realtime.handlers?.onClassificationTerminal({
      feedbackId: 10,
      locationId: 1,
    })

    await vi.waitFor(() => {
      expect(getFeedback).toHaveBeenCalledTimes(2)
      expect(getHomeLatestActivity).toHaveBeenCalledTimes(2)
      expect(getHomePerformance).toHaveBeenCalledTimes(2)
      expect(
        home.getSnapshot().viewModel?.activityByTab.feedback[0]
      ).toMatchObject({
        feedbackId: 10,
        sentiment: "negative",
      })
      expect(
        home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
      ).toMatchObject({ value: 2 })
    })
  })

  it("keeps previous Feedback submitted value while a performance recount is in flight", async () => {
    const realtime = {
      handlers: null as FeedbackHomeRealtimeHandlers | null,
    }
    const deferred: {
      resolve: ((value: {
        success: boolean
        feedbackSubmitted: number
        feedbackSubmittedPrevious: number
        guestsJoined: number
        guestsJoinedPrevious: number
    qrScans: number
    qrScansPrevious: number
      }) => void) | null
    } = { resolve: null }
    let call = 0
    const getHomePerformance = vi.fn(
      (_locationId: number, _from: string, _to: string) => {
        call += 1
        if (call === 1) {
          return Promise.resolve({ success: true, feedbackSubmitted: 5, feedbackSubmittedPrevious: 0, guestsJoined: 0, guestsJoinedPrevious: 0, qrScans: 0, qrScansPrevious: 0 })
        }
        return new Promise<{ success: boolean; feedbackSubmitted: number; feedbackSubmittedPrevious: number; guestsJoined: number; guestsJoinedPrevious: number
    qrScans: number
    qrScansPrevious: number }>(
          (resolve) => {
            deferred.resolve = resolve
          }
        )
      }
    )
    const home = createOperatorHomePageModule(
      createAdapters({
        getHomePerformance,
        connectRealtime: async (handlers) => {
          realtime.handlers = handlers
          return { stop: async () => {} }
        },
      })
    )

    await home.connect()
    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
    ).toMatchObject({ value: 5 })

    realtime.handlers?.onClassificationTerminal({
      feedbackId: 10,
      locationId: 1,
    })

    await vi.waitFor(() => {
      expect(home.getSnapshot().performanceLoadStatus).toBe("loading")
    })
    expect(home.getSnapshot().loadStatus).toBe("loaded")
    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
    ).toMatchObject({ value: 5 })

    deferred.resolve?.({ success: true, feedbackSubmitted: 8, feedbackSubmittedPrevious: 0, guestsJoined: 0, guestsJoinedPrevious: 0, qrScans: 0, qrScansPrevious: 0 })

    await vi.waitFor(() => {
      expect(home.getSnapshot().performanceLoadStatus).toBe("loaded")
      expect(
        home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
      ).toMatchObject({ value: 8 })
    })
  })

  it("keeps last good Feedback submitted when a recount fails", async () => {
    const realtime = {
      handlers: null as FeedbackHomeRealtimeHandlers | null,
    }
    let call = 0
    const onPerformanceLoadError = vi.fn()
    const getHomePerformance = vi.fn(
      async (_locationId: number, _from: string, _to: string) => {
        call += 1
        if (call === 1) {
          return { success: true, feedbackSubmitted: 4, feedbackSubmittedPrevious: 0, guestsJoined: 0, guestsJoinedPrevious: 0, qrScans: 0, qrScansPrevious: 0 }
        }
        throw new Error("network")
      }
    )
    const home = createOperatorHomePageModule(
      createAdapters({
        getHomePerformance,
        onPerformanceLoadError,
        connectRealtime: async (handlers) => {
          realtime.handlers = handlers
          return { stop: async () => {} }
        },
      })
    )

    await home.connect()
    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
    ).toMatchObject({ value: 4 })

    realtime.handlers?.onClassificationTerminal({
      feedbackId: 10,
      locationId: 1,
    })

    await vi.waitFor(() => {
      expect(home.getSnapshot().performanceLoadStatus).toBe("error")
    })
    expect(home.getSnapshot().loadStatus).toBe("loaded")
    expect(onPerformanceLoadError).toHaveBeenCalledWith(
      "Could not load performance stats. Please try again."
    )
    expect(
      home.getSnapshot().viewModel?.kpis.find((kpi) => kpi.id === "feedback")
    ).toMatchObject({ value: 4 })
  })

  it("ignores classification-terminal signals for other Owned locations", async () => {
    const realtime = {
      handlers: null as FeedbackHomeRealtimeHandlers | null,
    }
    const getFeedback = vi.fn(async () => ({
      success: true,
      total: 1,
      recent: recentFeedback,
    }))
    const getFeedbackDetails = vi.fn(async (feedbackId: number) => ({
      success: true,
      id: feedbackId,
      guestName: "Alex",
      guestContact: "alex@example.com",
      contactType: "Email" as const,
      comment: "Great food",
      createdAt: "2026-07-14T11:00:00.000Z",
      locationName: "First Venue",
      address: "1 High St",
      classificationStatus: "Pending" as const,
      sentiment: null,
      detectedTags: null,
      locationGuestId: null,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({
        getFeedback,
        getFeedbackDetails,
        connectRealtime: async (handlers) => {
          realtime.handlers = handlers
          return { stop: async () => {} }
        },
      })
    )

    await home.connect()
    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    await home.openFeedbackDetails(10)
    expect(getFeedback).toHaveBeenCalledTimes(1)
    expect(getFeedbackDetails).toHaveBeenCalledTimes(1)

    realtime.handlers?.onClassificationTerminal({
      feedbackId: 10,
      locationId: 2,
    })

    await new Promise((resolve) => setTimeout(resolve, 20))
    expect(getFeedback).toHaveBeenCalledTimes(1)
    expect(getFeedbackDetails).toHaveBeenCalledTimes(1)
  })

  it("matching signal refetches open Feedback details for that Feedback", async () => {
    const realtime = {
      handlers: null as FeedbackHomeRealtimeHandlers | null,
    }
    let detailsStatus: "Pending" | "Succeeded" = "Pending"
    const getFeedbackDetails = vi.fn(async (feedbackId: number) => ({
      success: true,
      id: feedbackId,
      guestName: "Alex",
      guestContact: "alex@example.com",
      contactType: "Email" as const,
      comment: "Great food",
      createdAt: "2026-07-14T11:00:00.000Z",
      locationName: "First Venue",
      address: "1 High St",
      classificationStatus: detailsStatus,
      sentiment: detailsStatus === "Succeeded" ? ("negative" as const) : null,
      detectedTags: detailsStatus === "Succeeded" ? ["FoodQuality"] : null,
      locationGuestId: null,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({
        getFeedbackDetails,
        connectRealtime: async (handlers) => {
          realtime.handlers = handlers
          return { stop: async () => {} }
        },
      })
    )

    await home.connect()
    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    await home.openFeedbackDetails(10)
    expect(getFeedbackDetails).toHaveBeenCalledTimes(1)
    expect(home.getSnapshot().feedbackDetails.details?.classificationStatus).toBe(
      "Pending"
    )

    detailsStatus = "Succeeded"
    realtime.handlers?.onClassificationTerminal({
      feedbackId: 10,
      locationId: 1,
    })

    await vi.waitFor(() => {
      expect(getFeedbackDetails).toHaveBeenCalledTimes(2)
      expect(
        home.getSnapshot().feedbackDetails.details?.classificationStatus
      ).toBe("Succeeded")
    })
  })

  it("does not refetch Feedback details when a different Feedback is open", async () => {
    const realtime = {
      handlers: null as FeedbackHomeRealtimeHandlers | null,
    }
    const getFeedbackDetails = vi.fn(async (feedbackId: number) => ({
      success: true,
      id: feedbackId,
      guestName: "Alex",
      guestContact: "alex@example.com",
      contactType: "Email" as const,
      comment: "Great food",
      createdAt: "2026-07-14T11:00:00.000Z",
      locationName: "First Venue",
      address: "1 High St",
      classificationStatus: "Pending" as const,
      sentiment: null,
      detectedTags: null,
      locationGuestId: null,
    }))
    const getFeedback = vi.fn(async () => ({
      success: true,
      total: 1,
      recent: recentFeedback,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({
        getFeedback,
        getFeedbackDetails,
        connectRealtime: async (handlers) => {
          realtime.handlers = handlers
          return { stop: async () => {} }
        },
      })
    )

    await home.connect()
    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    await home.openFeedbackDetails(10)
    expect(getFeedbackDetails).toHaveBeenCalledTimes(1)

    realtime.handlers?.onClassificationTerminal({
      feedbackId: 99,
      locationId: 1,
    })

    await vi.waitFor(() => {
      expect(getFeedback).toHaveBeenCalledTimes(2)
    })
    expect(getFeedbackDetails).toHaveBeenCalledTimes(1)
  })

  it("reconnect runs REST catch-up for Latest activity and open details", async () => {
    const realtime = {
      handlers: null as FeedbackHomeRealtimeHandlers | null,
    }
    const getFeedback = vi.fn(async () => ({
      success: true,
      total: 1,
      recent: recentFeedback,
    }))
    const getHomePerformance = vi.fn(async (_locationId: number, _from: string, _to: string) => ({
      success: true,
      feedbackSubmitted: 1,
      feedbackSubmittedPrevious: 0,
      guestsJoined: 0,
      guestsJoinedPrevious: 0,
      qrScans: 0,
      qrScansPrevious: 0,
    }))
    const getFeedbackDetails = vi.fn(async (feedbackId: number) => ({
      success: true,
      id: feedbackId,
      guestName: "Alex",
      guestContact: "alex@example.com",
      contactType: "Email" as const,
      comment: "Great food",
      createdAt: "2026-07-14T11:00:00.000Z",
      locationName: "First Venue",
      address: "1 High St",
      classificationStatus: "Pending" as const,
      sentiment: null,
      detectedTags: null,
      locationGuestId: null,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({
        getFeedback,
        getHomePerformance,
        getFeedbackDetails,
        connectRealtime: async (handlers) => {
          realtime.handlers = handlers
          return { stop: async () => {} }
        },
      })
    )

    await home.connect()
    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    await home.openFeedbackDetails(10)
    expect(getFeedback).toHaveBeenCalledTimes(1)
    expect(getHomePerformance).toHaveBeenCalledTimes(1)
    expect(getFeedbackDetails).toHaveBeenCalledTimes(1)

    realtime.handlers?.onReconnected()

    await vi.waitFor(() => {
      expect(getFeedback).toHaveBeenCalledTimes(2)
      expect(getHomePerformance).toHaveBeenCalledTimes(2)
      expect(getFeedbackDetails).toHaveBeenCalledTimes(2)
    })
  })

  it("disconnect stops the Feedback/Home realtime session", async () => {
    const stop = vi.fn(async () => {})
    const home = createOperatorHomePageModule(
      createAdapters({
        connectRealtime: async () => ({ stop }),
      })
    )

    await home.connect()
    await home.disconnect()

    expect(stop).toHaveBeenCalledTimes(1)
  })

  it("does not refetch Feedback details on reconnect while correcting classification", async () => {
    const realtime = {
      handlers: null as FeedbackHomeRealtimeHandlers | null,
    }
    const getFeedbackDetails = vi.fn(async () => ({
      success: true,
      id: 10,
      guestName: "Alex",
      guestContact: "alex@example.com",
      contactType: "Email" as const,
      comment: "Cold food",
      createdAt: "2026-07-14T11:00:00.000Z",
      locationName: "First Venue",
      address: "1 High St",
      classificationStatus: "Succeeded" as const,
      sentiment: "negative" as const,
      detectedTags: [] as string[],
      locationGuestId: null,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({
        getFeedbackDetails,
        connectRealtime: async (handlers) => {
          realtime.handlers = handlers
          return { stop: async () => {} }
        },
      })
    )

    await home.connect()
    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    await home.openFeedbackDetails(10)
    home.startClassificationCorrection()
    home.setClassificationDraftSentiment("positive")
    expect(getFeedbackDetails).toHaveBeenCalledTimes(1)

    realtime.handlers?.onReconnected()

    await vi.waitFor(() => {
      expect(home.getSnapshot().loadStatus).toBe("loaded")
    })
    expect(getFeedbackDetails).toHaveBeenCalledTimes(1)
    expect(home.getSnapshot().feedbackDetails.correction).toMatchObject({
      isEditing: true,
      draftSentiment: "positive",
    })
  })

  it("patches Latest activity sentiment after a successful classification correction", async () => {
    const correctClassification = vi.fn(async () => ({
      classificationStatus: "Succeeded" as const,
      sentiment: "positive" as const,
      detectedTags: [] as string[],
      locationGuestId: null,
    }))
    const getFeedback = vi.fn(async () => ({
      success: true,
      total: 1,
      recent: [
        {
          id: 10,
          guestName: "Alex",
          guestContact: "alex@example.com",
          contactType: "Email" as const,
          comment: "Cold food",
          createdAt: "2026-07-14T11:00:00.000Z",
          classificationStatus: "Succeeded" as const,
          sentiment: "negative" as const,
          detectedTags: [] as string[],
        },
      ],
    }))
    const getHomeLatestActivity = vi.fn(async () => ({
      success: true,
      items: [
        {
          kind: "feedback" as const,
          id: 10,
          guestName: "Alex",
          guestContact: "alex@example.com",
          contactType: "Email" as const,
          comment: "Cold food",
          createdAt: "2026-07-14T11:00:00.000Z",
          classificationStatus: "Succeeded" as const,
          sentiment: "negative" as const,
          detectedTags: [] as string[],
          locationGuestId: null,
        },
      ],
    }))
    const getFeedbackDetails = vi.fn(async () => ({
      success: true,
      id: 10,
      guestName: "Alex",
      guestContact: "alex@example.com",
      contactType: "Email" as const,
      comment: "Cold food",
      createdAt: "2026-07-14T11:00:00.000Z",
      locationName: "First Venue",
      address: "1 High St",
      classificationStatus: "Succeeded" as const,
      sentiment: "negative" as const,
      detectedTags: [] as string[],
      locationGuestId: null,
    }))

    const home = createOperatorHomePageModule(
      createAdapters({
        getFeedback,
        getHomeLatestActivity,
        getFeedbackDetails,
        correctClassification,
      })
    )

    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    await home.openFeedbackDetails(10)

    const beforeBadge = home
      .getSnapshot()
      .viewModel?.activityByTab.feedback.find(
        (item) => item.feedbackId === 10
      )
    expect(beforeBadge?.sentiment).toBe("negative")

    home.startClassificationCorrection()
    home.setClassificationDraftSentiment("positive")
    await home.saveClassificationCorrection()

    expect(correctClassification).toHaveBeenCalledWith(10, "positive")
    expect(home.getSnapshot().feedbackDetails.details?.sentiment).toBe(
      "positive"
    )
    const afterBadge = home
      .getSnapshot()
      .viewModel?.activityByTab.feedback.find(
        (item) => item.feedbackId === 10
      )
    expect(afterBadge?.sentiment).toBe("positive")
  })

  it("loads merged Latest activity with guest-joined rows on Guests tab only", async () => {
    const getHomeLatestActivity = vi.fn(async () => ({
      success: true,
      items: [
        {
          kind: "guest-joined" as const,
          locationGuestId: 501,
          guestName: "Jordan Guest",
          offersOptOut: false,
          createdAt: "2026-07-13T09:00:00.000Z",
        },
        {
          kind: "feedback" as const,
          locationGuestId: null,
          ...recentFeedback[0],
        },
      ],
    }))
    const home = createOperatorHomePageModule(
      createAdapters({ getHomeLatestActivity })
    )

    await home.syncWorkspace(workspaceInput())

    expect(getHomeLatestActivity).toHaveBeenCalledWith(1)
    expect(home.getSnapshot().viewModel?.activityByTab.guests).toEqual([
      {
        id: "guest-joined-501",
        kind: "guest-joined",
        locationGuestId: 501,
        guestName: "Jordan Guest",
        initials: "JG",
        headline: "Jordan joined your customer club",
        joinSourceLabel: "From QR scan",
        consentLabel: "Opted in",
        createdAt: "2026-07-13T09:00:00.000Z",
        canViewGuest: true,
        canSendOffer: false,
      },
    ])
    expect(home.getSnapshot().viewModel?.activityByTab.feedback).toEqual([
      expect.objectContaining({
        kind: "feedback",
        feedbackId: 10,
        canViewFeedback: true,
      }),
    ])
    expect(home.getSnapshot().viewModel?.activityByTab.all).toEqual([
      expect.objectContaining({ kind: "guest-joined", locationGuestId: 501 }),
      expect.objectContaining({ kind: "feedback", feedbackId: 10 }),
    ])
  })
})

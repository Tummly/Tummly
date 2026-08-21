import { describe, expect, it, vi } from "vitest"

import type { FeedbackDetailsAdapters } from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import {
  createOperatorHomePageModule,
  type FeedbackHomeRealtimeHandlers,
  type OperatorHomePageAdapters,
} from "./createOperatorHomePageModule"
import type { FeedbackItem, LocationItem } from "@/types/dashboard"
import type { HomeRecommendationResponse } from "@/types/operatorHome"

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
  getHomeLatestActivity?: OperatorHomePageAdapters["getHomeLatestActivity"]
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
  correctClassification?: FeedbackDetailsAdapters["correctClassification"]
  updateDetectedTags?: FeedbackDetailsAdapters["updateDetectedTags"]
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
  closeOutFeedback?: (
    feedbackId: number,
    input: {
      intent: "mark_resolved" | "mark_no_action_needed"
      reason:
        | "positive_no_follow_up"
        | "duplicate_submission"
        | "test_or_invalid"
        | "already_handled_outside"
        | "no_appropriate_follow_up"
        | "other"
      noteBody?: string
    }
  ) => Promise<{
    workflowStatus: "new" | "in_progress" | "resolved"
    needsAttention: boolean
    activityEvent: {
      kind: "feedback_closed_out"
      at: string
      actorDisplayName?: string | null
      fromWorkflowStatus?: "new" | "in_progress" | "resolved" | null
      toWorkflowStatus?: "new" | "in_progress" | "resolved" | null
      closeOutIntent?: "mark_resolved" | "mark_no_action_needed" | null
      closeOutReason?:
        | "positive_no_follow_up"
        | "duplicate_submission"
        | "test_or_invalid"
        | "already_handled_outside"
        | "no_appropriate_follow_up"
        | "other"
        | null
    }
    noteActivityEvent?: {
      kind: "note_added"
      at: string
      actorDisplayName?: string | null
    } | null
    note?: {
      id: number
      body: string
      authorDisplayName: string
      createdAt: string
    } | null
  }>
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
  hasCreatedOffer?: (locationId: number) => Promise<boolean>
  hasCreatedCampaign?: (locationId: number) => Promise<boolean>
  loadHomeRecommendation?: OperatorHomePageAdapters["loadHomeRecommendation"]
  copyText?: (
    text: string
  ) => Promise<{ ok: true } | { ok: false; error: string }>
  openSmartGuestLink?: (url: string) => void
  connectRealtime?: (
    handlers: FeedbackHomeRealtimeHandlers
  ) => Promise<{ stop: () => Promise<void> }>
  onPerformanceLoadError?: (message: string) => void
  listLiveOffers?: OperatorHomePageAdapters["listLiveOffers"]
  listLiveCampaigns?: OperatorHomePageAdapters["listLiveCampaigns"]
  getNeedsAttentionFeedback?: OperatorHomePageAdapters["getNeedsAttentionFeedback"]
  listNeedsAttentionCampaigns?: OperatorHomePageAdapters["listNeedsAttentionCampaigns"]
  listNeedsAttentionOffers?: OperatorHomePageAdapters["listNeedsAttentionOffers"]
  listOpenVoidAttention?: OperatorHomePageAdapters["listOpenVoidAttention"]
  pauseCampaign?: OperatorHomePageAdapters["pauseCampaign"]
  duplicateCampaign?: OperatorHomePageAdapters["duplicateCampaign"]
  getCampaignDraftById?: OperatorHomePageAdapters["getCampaignDraftById"]
} = {}): OperatorHomePageAdapters {
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
    loadHomeRecommendation:
      overrides.loadHomeRecommendation
      ?? (async (): Promise<HomeRecommendationResponse> => ({
        success: true,
        recommendation: { type: "none" },
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
      ?? (async (_feedbackId, input) => ({
        classificationStatus: "Succeeded" as const,
        sentiment: input.sentiment,
        detectedTags: [] as string[],
      })),
    updateDetectedTags:
      overrides.updateDetectedTags
      ?? (async () => {
        throw new Error("updateDetectedTags not stubbed")
      }),
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
    closeOutFeedback:
      overrides.closeOutFeedback
      ?? (async () => ({
        workflowStatus: "resolved" as const,
        needsAttention: false,
        activityEvent: {
          kind: "feedback_closed_out" as const,
          at: "2026-07-14T13:00:00.000Z",
          actorDisplayName: "Ada Operator",
          fromWorkflowStatus: "new" as const,
          toWorkflowStatus: "resolved" as const,
          closeOutIntent: "mark_resolved" as const,
          closeOutReason: "duplicate_submission" as const,
        },
        noteActivityEvent: null,
        note: null,
      })),
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
    hasCreatedOffer: overrides.hasCreatedOffer ?? (async () => false),
    hasCreatedCampaign: overrides.hasCreatedCampaign ?? (async () => false),
    copyText: overrides.copyText ?? (async () => ({ ok: true as const })),
    openSmartGuestLink: overrides.openSmartGuestLink ?? vi.fn(),
    connectRealtime:
      overrides.connectRealtime
      ?? (async () => ({ stop: async () => {} })),
    onPerformanceLoadError: overrides.onPerformanceLoadError,
    listLiveOffers: overrides.listLiveOffers ?? (async () => []),
    listLiveCampaigns: overrides.listLiveCampaigns ?? (async () => []),
    getNeedsAttentionFeedback:
      overrides.getNeedsAttentionFeedback
      ?? (async () => ({ count: 0, newestSubmittedAt: null })),
    listNeedsAttentionCampaigns:
      overrides.listNeedsAttentionCampaigns ?? (async () => []),
    listNeedsAttentionOffers:
      overrides.listNeedsAttentionOffers ?? (async () => []),
    listOpenVoidAttention: overrides.listOpenVoidAttention ?? (async () => []),
    pauseCampaign:
      overrides.pauseCampaign
      ?? (async () => {
        throw new Error("pauseCampaign not stubbed")
      }),
    duplicateCampaign:
      overrides.duplicateCampaign
      ?? (async () => {
        throw new Error("duplicateCampaign not stubbed")
      }),
    getCampaignDraftById: overrides.getCampaignDraftById,
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
        (item) => item.kind === "feedback" && item.feedbackId === 10
      )
    expect(beforeBadge?.kind === "feedback" ? beforeBadge.sentiment : null).toBe(
      "negative"
    )

    home.startClassificationCorrection()
    home.setClassificationDraftSentiment("positive")
    home.setClassificationDraftReason("incorrect_ai_classification")
    await home.saveClassificationCorrection()

    expect(correctClassification).toHaveBeenCalledWith(10, {
      sentiment: "positive",
      reason: "incorrect_ai_classification",
    })
    expect(home.getSnapshot().feedbackDetails.details?.sentiment).toBe(
      "positive"
    )
    const afterBadge = home
      .getSnapshot()
      .viewModel?.activityByTab.feedback.find(
        (item) => item.kind === "feedback" && item.feedbackId === 10
      )
    expect(afterBadge?.kind === "feedback" ? afterBadge.sentiment : null).toBe(
      "positive"
    )
  })

  it("loads merged Latest activity with guest-joined rows on Guests tab only", async () => {
    const getHomeLatestActivity = vi.fn(async () => ({
      success: true,
      items: [
        {
          kind: "guest-joined" as const,
          locationGuestId: 501,
          guestName: "Jordan Guest",
          marketingPreference: "allowed" as const,
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


  it("loads a success recommendation onto the module snapshot", async () => {
    const loadHomeRecommendation = vi.fn(async () => ({
      success: true,
      recommendation: {
        type: "review-open-feedback" as const,
        title: "Review open feedback",
        opportunity: "Guests left feedback that still needs a response.",
        whyBullets: ["Open feedback is waiting"],
        action: { kind: "open-feedback" as const, feedbackId: 10 },
      },
    }))
    const home = createOperatorHomePageModule(
      createAdapters({ loadHomeRecommendation })
    )

    await home.syncWorkspace(workspaceInput())

    expect(loadHomeRecommendation).toHaveBeenCalledWith({
      request: expect.objectContaining({
        locationId: 1,
        overviewDatePreset: "last7",
        refresh: false,
      }),
    })
    const recommendation = home.getSnapshot().recommendation
    expect(recommendation.status).toBe("ready")
    expect(recommendation.isNone).toBe(false)
    expect(recommendation.recommendation?.title).toBe("Review open feedback")
    expect(home.getSnapshot().viewModel).not.toHaveProperty("recommendation")
  })

  it("maps type none to the empty recommendation card state", async () => {
    const loadHomeRecommendation = vi.fn(async () => ({
      success: true,
      recommendation: { type: "none" as const },
    }))
    const home = createOperatorHomePageModule(
      createAdapters({ loadHomeRecommendation })
    )

    await home.syncWorkspace(workspaceInput())

    const recommendation = home.getSnapshot().recommendation
    expect(recommendation.status).toBe("ready")
    expect(recommendation.isNone).toBe(true)
    expect(recommendation.recommendation).toBeNull()
  })

  it("fail then retryRecommendation sets refresh and recovers", async () => {
    const loadHomeRecommendation = vi
      .fn()
      .mockResolvedValueOnce({
        success: false,
        message: "Azure timed out",
        retryable: true,
      })
      .mockResolvedValueOnce({
        success: true,
        recommendation: {
          type: "thank-or-follow-guest" as const,
          title: "Thank a recent guest",
          opportunity: "A guest joined recently.",
          whyBullets: ["New guest joined"],
          action: { kind: "open-guest" as const, locationGuestId: 501 },
        },
      })
    const home = createOperatorHomePageModule(
      createAdapters({ loadHomeRecommendation })
    )

    await home.syncWorkspace(workspaceInput())

    let recommendation = home.getSnapshot().recommendation
    expect(recommendation.status).toBe("error")
    expect(recommendation.errorRetryable).toBe(true)

    await home.retryRecommendation()

    recommendation = home.getSnapshot().recommendation
    expect(recommendation.status).toBe("ready")
    expect(recommendation.recommendation?.type).toBe("thank-or-follow-guest")
    expect(loadHomeRecommendation).toHaveBeenLastCalledWith({
      request: expect.objectContaining({
        locationId: 1,
        refresh: true,
      }),
    })
  })

  it("reload with same cache key keeps last ready recommendation visible (soft refresh)", async () => {
    const successResponse = {
      success: true as const,
      recommendation: {
        type: "review-open-feedback" as const,
        title: "Review open feedback",
        opportunity: "Guests left feedback that still needs a response.",
        whyBullets: ["Open feedback is waiting"],
        action: { kind: "open-feedback" as const, feedbackId: 10 },
      },
    }
    const softRefreshGate: {
      resolve: ((value: typeof successResponse) => void) | null
    } = { resolve: null }
    const loadHomeRecommendation = vi
      .fn()
      .mockResolvedValueOnce(successResponse)
      .mockImplementationOnce(
        () =>
          new Promise<typeof successResponse>((resolve) => {
            softRefreshGate.resolve = resolve
          })
      )

    const home = createOperatorHomePageModule(
      createAdapters({ loadHomeRecommendation })
    )

    await home.syncWorkspace(workspaceInput())
    expect(home.getSnapshot().recommendation.status).toBe("ready")

    const reloadPromise = home.retryLoad()
    await vi.waitFor(() => {
      expect(home.getSnapshot().loadStatus).toBe("loaded")
    })

    const midReload = home.getSnapshot().recommendation
    expect(midReload.status).toBe("ready")
    expect(midReload.recommendation?.title).toBe("Review open feedback")
    expect(midReload.recommendation).not.toBeNull()

    const resolveSoftRefresh = softRefreshGate.resolve
    if (resolveSoftRefresh == null) {
      throw new Error("Expected deferred soft-refresh recommendation load.")
    }
    resolveSoftRefresh(successResponse)
    await reloadPromise

    expect(loadHomeRecommendation).toHaveBeenCalledTimes(2)
    expect(loadHomeRecommendation).toHaveBeenLastCalledWith({
      request: expect.objectContaining({
        locationId: 1,
        refresh: false,
      }),
    })
    expect(home.getSnapshot().recommendation.status).toBe("ready")
  })

  it("retryRecommendation may show loading and sets refresh", async () => {
    const successResponse = {
      success: true as const,
      recommendation: {
        type: "review-open-feedback" as const,
        title: "Review open feedback",
        opportunity: "Guests left feedback that still needs a response.",
        whyBullets: ["Open feedback is waiting"],
        action: { kind: "open-feedback" as const, feedbackId: 10 },
      },
    }
    const retryGate: {
      resolve: ((value: typeof successResponse) => void) | null
    } = { resolve: null }
    const loadHomeRecommendation = vi
      .fn()
      .mockResolvedValueOnce(successResponse)
      .mockImplementationOnce(
        () =>
          new Promise<typeof successResponse>((resolve) => {
            retryGate.resolve = resolve
          })
      )

    const home = createOperatorHomePageModule(
      createAdapters({ loadHomeRecommendation })
    )

    await home.syncWorkspace(workspaceInput())
    expect(home.getSnapshot().recommendation.status).toBe("ready")

    const retryPromise = home.retryRecommendation()
    await vi.waitFor(() => {
      expect(home.getSnapshot().recommendation.status).toBe("loading")
    })

    const resolveRetry = retryGate.resolve
    if (resolveRetry == null) {
      throw new Error("Expected deferred recommendation retry load.")
    }
    resolveRetry(successResponse)
    await retryPromise

    expect(loadHomeRecommendation).toHaveBeenLastCalledWith({
      request: expect.objectContaining({
        refresh: true,
      }),
    })
    expect(home.getSnapshot().recommendation.status).toBe("ready")
  })

  it("Not now hides the recommendation for the session without calling the API again", async () => {
    const loadHomeRecommendation = vi.fn(async () => ({
      success: true,
      recommendation: {
        type: "promote-or-fix-offer" as const,
        title: "Promote a live offer",
        opportunity: "An offer is ready to promote.",
        whyBullets: ["Offer is live"],
        action: { kind: "open-offer" as const, offerId: 9 },
      },
    }))
    const home = createOperatorHomePageModule(
      createAdapters({ loadHomeRecommendation })
    )

    await home.syncWorkspace(workspaceInput())
    expect(home.getSnapshot().recommendation.status).toBe("ready")
    expect(loadHomeRecommendation).toHaveBeenCalledTimes(1)

    home.dismissRecommendation()

    const recommendation = home.getSnapshot().recommendation
    expect(recommendation.status).toBe("dismissed")
    expect(recommendation.recommendation).toBeNull()

    await home.retryLoad()
    expect(loadHomeRecommendation).toHaveBeenCalledTimes(1)
    expect(home.getSnapshot().recommendation.status).toBe("dismissed")
  })

  it("date range change loads recommendation for the new soft-cache key", async () => {
    let dateRange:
      | { kind: "preset"; presetId: "last7" | "last30" | "thisMonth" }
      | { kind: "custom"; startDate: string; endDate: string } = {
      kind: "preset",
      presetId: "last7",
    }
    const loadHomeRecommendation = vi
      .fn()
      .mockResolvedValueOnce({
        success: true,
        recommendation: {
          type: "review-open-feedback" as const,
          title: "Review open feedback",
          whyBullets: ["Open feedback"],
        },
      })
      .mockResolvedValueOnce({
        success: true,
        recommendation: {
          type: "thank-or-follow-guest" as const,
          title: "Thank a recent guest",
          whyBullets: ["New guest"],
        },
      })
    const home = createOperatorHomePageModule(
      createAdapters({
        loadHomeRecommendation,
        getHomePerformanceDateRange: () => dateRange,
      })
    )

    await home.syncWorkspace(workspaceInput())
    expect(home.getSnapshot().recommendation.recommendation?.title).toBe(
      "Review open feedback"
    )

    dateRange = { kind: "preset", presetId: "last30" }
    await home.reloadForHomePerformanceDateRange()

    expect(loadHomeRecommendation).toHaveBeenCalledTimes(2)
    expect(loadHomeRecommendation).toHaveBeenLastCalledWith({
      request: expect.objectContaining({
        overviewDatePreset: "last30",
        refresh: false,
      }),
    })
    expect(home.getSnapshot().recommendation.recommendation?.title).toBe(
      "Thank a recent guest"
    )
  })


  it("loads live offer and campaign cards in parallel with home feedback", async () => {
    const listLiveOffers = vi.fn(async () => [
      {
        id: 11,
        locationId: 1,
        title: "10% off your next visit",
        status: "active" as const,
        offerType: "percentage-discount",
        validity: "custom-date",
        expiryDate: "2026-07-31",
        attachKinds: ["campaign"],
        lifetimeClaims: 5,
        lifetimeRedeemed: 2,
        createdAt: "2026-08-01T12:00:00.000Z",
        updatedAt: "2026-08-21T12:00:00.000Z",
      },
    ])
    const listLiveCampaigns = vi.fn(async () => [
      {
        id: 2,
        name: "Thank-you campaign",
        status: "sending",
        goalId: null,
        locationId: 1,
        locationName: "First Venue",
        channel: "email",
        audienceKey: null,
        offerStance: null,
        updatedAt: "2026-08-21T12:00:00.000Z",
        rowVersion: "rv-2",
        sendDate: null,
        delivery: "80%",
        engagement: null,
        redemptions: "3",
      },
    ])
    const getCampaignDraftById = vi.fn(async () => ({
      success: true,
      campaign: {
        id: 2,
        locationId: 1,
        status: "sending",
        name: "Thank-you campaign",
        goalId: null,
        templateId: null,
        templateVersion: null,
        audienceKey: null,
        channel: "email",
        offerStance: null,
        offerId: 11,
        messageSubject: "Thanks for visiting",
        messageBody: "We would love to see you again.",
        rowVersion: "rv-2",
        createdAt: "2026-08-01T12:00:00.000Z",
        updatedAt: "2026-08-21T12:00:00.000Z",
      },
    }))
    const home = createOperatorHomePageModule(
      createAdapters({
        listLiveOffers,
        listLiveCampaigns,
        getCampaignDraftById,
      })
    )

    await home.syncWorkspace(workspaceInput())
    await vi.waitFor(() => {
      expect(home.getSnapshot().liveOffersLoadStatus).toBe("loaded")
    })

    expect(listLiveOffers).toHaveBeenCalledWith(1)
    expect(listLiveCampaigns).toHaveBeenCalledWith(1)
    expect(home.getSnapshot().liveCards).toHaveLength(2)
    expect(home.getSnapshot().liveCards[0]).toMatchObject({
      kind: "campaign",
      id: 2,
      messageSubject: "Thanks for visiting",
      messageBody: "We would love to see you again.",
    })
    expect(home.getSnapshot().liveCards[1]).toMatchObject({
      kind: "offer",
      id: 11,
    })
  })

  it("exposes empty live cards when both live lists succeed empty", async () => {
    const home = createOperatorHomePageModule(createAdapters())

    await home.syncWorkspace(workspaceInput())
    await vi.waitFor(() => {
      expect(home.getSnapshot().liveOffersLoadStatus).toBe("loaded")
    })

    expect(home.getSnapshot().liveCards).toEqual([])
    expect(home.getSnapshot().liveOffersError).toBeNull()
  })

  it("sets live offers error and keeps empty cards when a live list fails", async () => {
    const home = createOperatorHomePageModule(
      createAdapters({
        listLiveOffers: async () => {
          throw new Error("offers down")
        },
      })
    )

    await home.syncWorkspace(workspaceInput())
    await vi.waitFor(() => {
      expect(home.getSnapshot().liveOffersLoadStatus).toBe("error")
    })

    expect(home.getSnapshot().liveCards).toEqual([])
    expect(home.getSnapshot().liveOffersError).toMatch(/Could not load live/)
  })

  it("retries live offers without reloading feedback", async () => {
    const getFeedback = vi.fn(async () => ({
      success: true,
      total: recentFeedback.length,
      recent: recentFeedback,
    }))
    const listLiveOffers = vi
      .fn()
      .mockRejectedValueOnce(new Error("boom"))
      .mockResolvedValueOnce([])
    const home = createOperatorHomePageModule(
      createAdapters({ getFeedback, listLiveOffers })
    )

    await home.syncWorkspace(workspaceInput())
    await vi.waitFor(() => {
      expect(home.getSnapshot().liveOffersLoadStatus).toBe("error")
    })
    expect(getFeedback).toHaveBeenCalledTimes(1)

    await home.retryLiveOffers()
    await vi.waitFor(() => {
      expect(home.getSnapshot().liveOffersLoadStatus).toBe("loaded")
    })
    expect(getFeedback).toHaveBeenCalledTimes(1)
    expect(listLiveOffers).toHaveBeenCalledTimes(2)
  })

  it("pauses a live campaign with rowVersion then refreshes the section", async () => {
    const pauseCampaign = vi.fn(async () => ({
      success: true,
      campaign: {
        id: 2,
        locationId: 1,
        status: "paused",
        name: "Thank-you campaign",
        scheduleMode: null,
        scheduledAtUtc: null,
        scheduleTimeZone: null,
        billingReservationRef: null,
        reservedEstimate: null,
        frozenRecipientCount: 0,
        rowVersion: "rv-3",
        updatedAt: "2026-08-21T13:00:00.000Z",
      },
    }))
    const listLiveCampaigns = vi
      .fn()
      .mockResolvedValueOnce([
        {
          id: 2,
          name: "Thank-you campaign",
          status: "sending",
          goalId: null,
          locationId: 1,
          locationName: "First Venue",
          channel: "email",
          audienceKey: null,
          offerStance: null,
          updatedAt: "2026-08-21T12:00:00.000Z",
          rowVersion: "rv-2",
          sendDate: null,
          delivery: null,
          engagement: null,
          redemptions: null,
        },
      ])
      .mockResolvedValueOnce([])
    const home = createOperatorHomePageModule(
      createAdapters({ listLiveCampaigns, pauseCampaign })
    )

    await home.syncWorkspace(workspaceInput())
    await vi.waitFor(() => {
      expect(home.getSnapshot().liveOffersLoadStatus).toBe("loaded")
    })
    expect(home.getSnapshot().liveCards[0]).toMatchObject({
      kind: "campaign",
      id: 2,
    })

    const paused = await home.pauseLiveCampaign(2)
    expect(paused).toBe(true)
    expect(pauseCampaign).toHaveBeenCalledWith(2, { rowVersion: "rv-2" })
    expect(home.getSnapshot().liveOffersLoadStatus).toBe("loaded")
    await vi.waitFor(() => {
      expect(home.getSnapshot().liveCards).toEqual([])
    })
    expect(home.getSnapshot().liveOffersLoadStatus).toBe("loaded")
  })

  it("pause refresh keeps section loaded while lists reload", async () => {
    let resolveCampaigns: ((value: unknown[]) => void) | null = null
    const pauseCampaign = vi.fn(async () => ({
      success: true,
      campaign: {
        id: 2,
        locationId: 1,
        status: "paused",
        name: "Thank-you campaign",
        scheduleMode: null,
        scheduledAtUtc: null,
        scheduleTimeZone: null,
        billingReservationRef: null,
        reservedEstimate: null,
        frozenRecipientCount: 0,
        rowVersion: "rv-3",
        updatedAt: "2026-08-21T13:00:00.000Z",
      },
    }))
    const listLiveCampaigns = vi
      .fn()
      .mockResolvedValueOnce([
        {
          id: 2,
          name: "Thank-you campaign",
          status: "sending",
          goalId: null,
          locationId: 1,
          locationName: "First Venue",
          channel: "email",
          audienceKey: null,
          offerStance: null,
          updatedAt: "2026-08-21T12:00:00.000Z",
          rowVersion: "rv-2",
          sendDate: null,
          delivery: null,
          engagement: null,
          redemptions: null,
        },
      ])
      .mockImplementationOnce(
        () =>
          new Promise((resolve) => {
            resolveCampaigns = resolve
          })
      )
    const home = createOperatorHomePageModule(
      createAdapters({ listLiveCampaigns, pauseCampaign })
    )

    await home.syncWorkspace(workspaceInput())
    await vi.waitFor(() => {
      expect(home.getSnapshot().liveOffersLoadStatus).toBe("loaded")
    })

    const pausePromise = home.pauseLiveCampaign(2)
    await vi.waitFor(() => {
      expect(listLiveCampaigns).toHaveBeenCalledTimes(2)
    })
    expect(home.getSnapshot().liveOffersLoadStatus).toBe("loaded")
    expect(home.getSnapshot().liveCards[0]).toMatchObject({
      kind: "campaign",
      id: 2,
    })

    resolveCampaigns?.([])
    await expect(pausePromise).resolves.toBe(true)
    expect(home.getSnapshot().liveCards).toEqual([])
    expect(home.getSnapshot().liveOffersLoadStatus).toBe("loaded")
  })

  it("loads an empty Needs attention projection when all sources succeed empty", async () => {
    const home = createOperatorHomePageModule(createAdapters())

    await home.syncWorkspace(workspaceInput())
    await vi.waitFor(() => {
      expect(home.getSnapshot().needsAttentionLoadStatus).toBe("loaded")
    })

    expect(home.getSnapshot().needsAttention).toMatchObject({
      isEmpty: true,
      allRows: [],
      visibleRows: [],
      showViewAll: false,
    })
    expect(home.getSnapshot().needsAttentionError).toBeNull()
  })

  it("assembles mixed Needs attention kinds from parallel source adapters", async () => {
    const getNeedsAttentionFeedback = vi.fn(async () => ({
      count: 2,
      newestSubmittedAt: "2026-08-21T11:48:00.000Z",
    }))
    const listNeedsAttentionCampaigns = vi.fn(async () => [
      {
        id: 41,
        name: "Weekend SMS blast",
        status: "failed",
        goalId: null,
        locationId: 1,
        locationName: "First Venue",
        channel: "sms",
        audienceKey: null,
        offerStance: null,
        updatedAt: "2026-08-21T11:00:00.000Z",
        rowVersion: "rv-41",
        sendDate: null,
        delivery: null,
        engagement: null,
        redemptions: null,
      },
    ])
    const listNeedsAttentionOffers = vi.fn(async () => [
      {
        id: 10,
        locationId: 1,
        title: "Lunch deal",
        status: "active" as const,
        offerType: "percentage-discount",
        validity: "custom-date",
        expiryDate: "2026-08-26",
        attachKinds: ["campaign"],
        lifetimeClaims: 4,
        lifetimeRedeemed: 1,
        createdAt: "2026-08-01T12:00:00.000Z",
        updatedAt: "2026-08-21T09:00:00.000Z",
      },
    ])
    const listOpenVoidAttention = vi.fn(async () => [
      {
        offerId: 10,
        offerTitle: "Lunch deal",
        pendingCount: 1,
      },
    ])
    const home = createOperatorHomePageModule(
      createAdapters({
        getNeedsAttentionFeedback,
        listNeedsAttentionCampaigns,
        listNeedsAttentionOffers,
        listOpenVoidAttention,
      })
    )

    await home.syncWorkspace(workspaceInput())
    await vi.waitFor(() => {
      expect(home.getSnapshot().needsAttentionLoadStatus).toBe("loaded")
    })

    expect(getNeedsAttentionFeedback).toHaveBeenCalledWith(1)
    expect(listNeedsAttentionCampaigns).toHaveBeenCalledWith(1)
    expect(listNeedsAttentionOffers).toHaveBeenCalledWith(1)
    expect(listOpenVoidAttention).toHaveBeenCalledWith(1)

    const rows = home.getSnapshot().needsAttention?.allRows ?? []
    expect(rows.map((row) => row.sourceKind)).toEqual([
      "feedback",
      "campaign",
      "offer",
    ])
    expect(rows[0]).toMatchObject({
      sourceKind: "feedback",
      title: "2 feedback items need attention",
    })
    expect(rows[1]).toMatchObject({
      sourceKind: "campaign",
      campaignId: 41,
      title: "Weekend SMS blast",
    })
    expect(rows[2]).toMatchObject({
      sourceKind: "offer",
      offerId: 10,
      title: "Open void request",
    })
  })

  it("fails Needs attention when one source fails and Retry re-fetches sources", async () => {
    const getFeedback = vi.fn(async () => ({
      success: true,
      total: recentFeedback.length,
      recent: recentFeedback,
    }))
    const listNeedsAttentionCampaigns = vi
      .fn()
      .mockRejectedValueOnce(new Error("campaigns down"))
      .mockResolvedValueOnce([])
    const home = createOperatorHomePageModule(
      createAdapters({ getFeedback, listNeedsAttentionCampaigns })
    )

    await home.syncWorkspace(workspaceInput())
    await vi.waitFor(() => {
      expect(home.getSnapshot().loadStatus).toBe("loaded")
    })
    await vi.waitFor(() => {
      expect(home.getSnapshot().needsAttentionLoadStatus).toBe("error")
    })

    expect(home.getSnapshot().needsAttention).toBeNull()
    expect(home.getSnapshot().needsAttentionError).toMatch(
      /Could not load Needs attention/
    )
    expect(getFeedback).toHaveBeenCalledTimes(1)

    await home.retryNeedsAttention()
    await vi.waitFor(() => {
      expect(home.getSnapshot().needsAttentionLoadStatus).toBe("loaded")
    })
    expect(getFeedback).toHaveBeenCalledTimes(1)
    expect(listNeedsAttentionCampaigns).toHaveBeenCalledTimes(2)
    expect(home.getSnapshot().needsAttention?.isEmpty).toBe(true)
  })

  it("reloads Needs attention when the selected Owned location changes", async () => {
    const getNeedsAttentionFeedback = vi.fn(async (locationId: number) => ({
      count: locationId === 1 ? 3 : 0,
      newestSubmittedAt:
        locationId === 1 ? "2026-08-21T11:48:00.000Z" : null,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({ getNeedsAttentionFeedback })
    )

    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    await vi.waitFor(() => {
      expect(home.getSnapshot().needsAttention?.allRows[0]).toMatchObject({
        sourceKind: "feedback",
        title: "3 feedback items need attention",
      })
    })

    await home.syncWorkspace(workspaceInput({ selectedLocationId: 2 }))
    await vi.waitFor(() => {
      expect(getNeedsAttentionFeedback).toHaveBeenLastCalledWith(2)
      expect(home.getSnapshot().needsAttentionLoadStatus).toBe("loaded")
    })
    expect(home.getSnapshot().needsAttention?.isEmpty).toBe(true)
  })

  it("ignores a stale Needs attention load after the selected location changes", async () => {
    const firstFeedback = Promise.withResolvers<{
      count: number
      newestSubmittedAt: string | null
    }>()
    const getNeedsAttentionFeedback = vi.fn((locationId: number) => {
      if (locationId === 1) {
        return firstFeedback.promise
      }
      return Promise.resolve({ count: 0, newestSubmittedAt: null })
    })
    const home = createOperatorHomePageModule(
      createAdapters({ getNeedsAttentionFeedback })
    )

    const firstLoad = home.syncWorkspace(
      workspaceInput({ selectedLocationId: 1 })
    )
    await vi.waitFor(() => {
      expect(home.getSnapshot().loadStatus).toBe("loaded")
    })

    await home.syncWorkspace(workspaceInput({ selectedLocationId: 2 }))
    await vi.waitFor(() => {
      expect(home.getSnapshot().needsAttentionLoadStatus).toBe("loaded")
    })
    expect(home.getSnapshot().needsAttention?.isEmpty).toBe(true)

    firstFeedback.resolve({
      count: 9,
      newestSubmittedAt: "2026-08-21T11:48:00.000Z",
    })
    await firstLoad

    expect(home.getSnapshot().needsAttention?.isEmpty).toBe(true)
    expect(
      home.getSnapshot().needsAttention?.allRows.some(
        (row) => row.sourceKind === "feedback"
      )
    ).toBe(false)
  })

  it("does not reload Needs attention when the Home performance date range changes", async () => {
    const getNeedsAttentionFeedback = vi.fn(async () => ({
      count: 0,
      newestSubmittedAt: null,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({ getNeedsAttentionFeedback })
    )

    await home.syncWorkspace(workspaceInput())
    await vi.waitFor(() => {
      expect(home.getSnapshot().needsAttentionLoadStatus).toBe("loaded")
    })
    expect(getNeedsAttentionFeedback).toHaveBeenCalledTimes(1)

    await home.reloadForHomePerformanceDateRange()
    expect(getNeedsAttentionFeedback).toHaveBeenCalledTimes(1)
  })

  it("duplicates a Failed Needs attention campaign as a Draft and returns the new id", async () => {
    const duplicateCampaign = vi.fn(async () => ({
      success: true,
      campaign: {
        id: 99,
        locationId: 1,
        status: "draft",
        name: "Weekend SMS blast - Draft",
        scheduleMode: null,
        scheduledAtUtc: null,
        scheduleTimeZone: null,
        billingReservationRef: null,
        reservedEstimate: null,
        frozenRecipientCount: 0,
        rowVersion: "rv-99",
        updatedAt: "2026-08-21T13:00:00.000Z",
      },
    }))
    const listNeedsAttentionCampaigns = vi.fn(async () => [
      {
        id: 41,
        name: "Weekend SMS blast",
        status: "failed",
        goalId: null,
        locationId: 1,
        locationName: "First Venue",
        channel: "sms",
        audienceKey: null,
        offerStance: null,
        updatedAt: "2026-08-21T11:00:00.000Z",
        rowVersion: "rv-41",
        sendDate: null,
        delivery: null,
        engagement: null,
        redemptions: null,
      },
    ])
    const home = createOperatorHomePageModule(
      createAdapters({ listNeedsAttentionCampaigns, duplicateCampaign })
    )

    await home.syncWorkspace(workspaceInput())
    await vi.waitFor(() => {
      expect(home.getSnapshot().needsAttentionLoadStatus).toBe("loaded")
    })

    const result = await home.duplicateNeedsAttentionCampaign(41)

    expect(duplicateCampaign).toHaveBeenCalledWith(41, { rowVersion: "rv-41" })
    expect(result).toEqual({ ok: true, campaignId: 99 })
  })

  it("returns an error when Duplicate as Draft cannot write the new Draft", async () => {
    const duplicateCampaign = vi.fn(async () => {
      throw new Error(
        "This campaign was updated elsewhere. Reload and try again."
      )
    })
    const listNeedsAttentionCampaigns = vi.fn(async () => [
      {
        id: 41,
        name: "Weekend SMS blast",
        status: "failed",
        goalId: null,
        locationId: 1,
        locationName: "First Venue",
        channel: "sms",
        audienceKey: null,
        offerStance: null,
        updatedAt: "2026-08-21T11:00:00.000Z",
        rowVersion: "rv-41",
        sendDate: null,
        delivery: null,
        engagement: null,
        redemptions: null,
      },
    ])
    const home = createOperatorHomePageModule(
      createAdapters({ listNeedsAttentionCampaigns, duplicateCampaign })
    )

    await home.syncWorkspace(workspaceInput())
    await vi.waitFor(() => {
      expect(home.getSnapshot().needsAttentionLoadStatus).toBe("loaded")
    })

    const result = await home.duplicateNeedsAttentionCampaign(41)

    expect(result).toEqual({
      ok: false,
      error: "This campaign was updated elsewhere. Reload and try again.",
    })
  })
})

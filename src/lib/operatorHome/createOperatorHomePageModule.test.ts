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

function createAdapters(overrides: {
  getFeedback?: (locationId: number) => Promise<{
    success: boolean
    total: number
    recent: FeedbackItem[]
  }>
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
  }>
  correctClassification?: (
    feedbackId: number,
    sentiment: "positive" | "neutral" | "negative"
  ) => Promise<{
    classificationStatus: "Pending" | "Succeeded" | "Failed"
    sentiment: "positive" | "neutral" | "negative" | null
    detectedTags: string[] | null
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
  copyText?: (
    text: string
  ) => Promise<{ ok: true } | { ok: false; error: string }>
  openSmartGuestLink?: (url: string) => void
  connectRealtime?: (
    handlers: FeedbackHomeRealtimeHandlers
  ) => Promise<{ stop: () => Promise<void> }>
} = {}) {
  return {
    getFeedback:
      overrides.getFeedback ??
      (async () => ({
        success: true,
        total: recentFeedback.length,
        recent: recentFeedback,
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
      })),
    correctClassification:
      overrides.correctClassification
      ?? (async (_feedbackId, sentiment) => ({
        classificationStatus: "Succeeded" as const,
        sentiment,
        detectedTags: [] as string[],
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
    copyText: overrides.copyText ?? (async () => ({ ok: true as const })),
    openSmartGuestLink: overrides.openSmartGuestLink ?? vi.fn(),
    connectRealtime:
      overrides.connectRealtime
      ?? (async () => ({ stop: async () => {} })),
  }
}

describe("createOperatorHomePageModule", () => {
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
      createAdapters({ getFeedback, getChecklistAcks })
    )

    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    await home.syncWorkspace(workspaceInput({ selectedLocationId: 2 }))

    expect(getFeedback).toHaveBeenLastCalledWith(2)
    expect(home.getSnapshot().viewModel?.selectedLocationId).toBe(2)
    expect(
      home
        .getSnapshot()
        .viewModel?.setupSteps.find((step) => step.id === "guest-form")?.status
    ).toBe("complete")
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
      classificationStatus: "Pending" as const,
      sentiment: null,
      detectedTags: null,
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
        venueLine: "First Venue · 1 High St",
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
    const getFeedback = vi.fn(async () => ({
      success: true,
      total: recent.length,
      recent,
    }))
    const home = createOperatorHomePageModule(
      createAdapters({
        getFeedback,
        connectRealtime: async (handlers) => {
          realtime.handlers = handlers
          return { stop: async () => {} }
        },
      })
    )

    await home.connect()
    await home.syncWorkspace(workspaceInput({ selectedLocationId: 1 }))
    expect(getFeedback).toHaveBeenCalledTimes(1)

    recent = [
      {
        ...recentFeedback[0],
        classificationStatus: "Succeeded",
        sentiment: "negative",
        detectedTags: ["FoodQuality"],
      },
    ]
    realtime.handlers?.onClassificationTerminal({
      feedbackId: 10,
      locationId: 1,
    })

    await vi.waitFor(() => {
      expect(getFeedback).toHaveBeenCalledTimes(2)
      expect(
        home.getSnapshot().viewModel?.activityByTab.feedback[0]
      ).toMatchObject({
        feedbackId: 10,
        sentiment: "negative",
      })
    })
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

    realtime.handlers?.onReconnected()

    await vi.waitFor(() => {
      expect(getFeedback).toHaveBeenCalledTimes(2)
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
    }))

    const home = createOperatorHomePageModule(
      createAdapters({
        getFeedback,
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
})

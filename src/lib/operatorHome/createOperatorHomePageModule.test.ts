import { describe, expect, it, vi } from "vitest"

import { createOperatorHomePageModule } from "./createOperatorHomePageModule"
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
  }>
  getChecklistAcks?: (locationId: number) => Promise<{
    success: boolean
    locationId: number
    guestFormPreviewed: boolean
    qrPlacementGuideViewed: boolean
    guestFormPreviewedAt: string | null
    qrPlacementGuideViewedAt: string | null
  }>
  setChecklistAcks?: (
    locationId: number,
    body: { guestFormPreviewed?: boolean; qrPlacementGuideViewed?: boolean }
  ) => Promise<{
    success: boolean
    locationId: number
    guestFormPreviewed: boolean
    qrPlacementGuideViewed: boolean
    guestFormPreviewedAt: string | null
    qrPlacementGuideViewedAt: string | null
  }>
  downloadQr?: (input: {
    locationId: number
    locationName: string
  }) => Promise<{ ok: true } | { ok: false; error: string }>
  openSmartGuestLink?: (url: string) => void
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
      })),
    getChecklistAcks:
      overrides.getChecklistAcks ??
      (async () => ({
        success: true,
        locationId: 1,
        guestFormPreviewed: false,
        qrPlacementGuideViewed: false,
        guestFormPreviewedAt: null,
        qrPlacementGuideViewedAt: null,
      })),
    setChecklistAcks:
      overrides.setChecklistAcks ??
      (async (_locationId, body) => ({
        success: true,
        locationId: 1,
        guestFormPreviewed: body.guestFormPreviewed ?? false,
        qrPlacementGuideViewed: body.qrPlacementGuideViewed ?? false,
        guestFormPreviewedAt: body.guestFormPreviewed
          ? "2026-07-14T12:00:00.000Z"
          : null,
        qrPlacementGuideViewedAt: null,
      })),
    downloadQr:
      overrides.downloadQr ?? (async () => ({ ok: true as const })),
    openSmartGuestLink: overrides.openSmartGuestLink ?? vi.fn(),
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
      guestFormPreviewedAt: locationId === 2 ? "2026-07-14T12:00:00.000Z" : null,
      qrPlacementGuideViewedAt: null,
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
      guestFormPreviewedAt: "2026-07-14T12:00:00.000Z",
      qrPlacementGuideViewedAt: null,
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
          guestFormPreviewedAt: "2026-07-14T12:00:00.000Z",
          qrPlacementGuideViewedAt: null,
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

  it("downloads QR for the selected Owned location and surfaces download errors", async () => {
    const downloadQr = vi
      .fn()
      .mockResolvedValueOnce({ ok: true })
      .mockResolvedValueOnce({
        ok: false,
        error: "Could not download QR code. Please try again.",
      })
    const home = createOperatorHomePageModule(createAdapters({ downloadQr }))
    await home.syncWorkspace(workspaceInput())

    home.downloadQr()
    await vi.waitFor(() => {
      expect(home.getSnapshot().downloadBusy).toBe(false)
    })
    expect(downloadQr).toHaveBeenCalledWith({
      locationId: 1,
      locationName: "First Venue",
    })
    expect(home.getSnapshot().actionError).toBeNull()

    home.downloadQr()
    await vi.waitFor(() => {
      expect(home.getSnapshot().actionError).toBe(
        "Could not download QR code. Please try again."
      )
    })
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
})

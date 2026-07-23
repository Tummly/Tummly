import { AxiosError } from "axios"
import { describe, expect, it, vi, type Mock } from "vitest"

import {
  createOperatorGuestProfilePageModule,
  type OperatorGuestProfilePageAdapters,
} from "@/lib/operatorGuestProfile/createOperatorGuestProfilePageModule"
import type { GuestProfileResponse } from "@/types/dashboard"

function createGuestProfileResponse(
  overrides: Partial<GuestProfileResponse> = {}
): GuestProfileResponse {
  return {
    success: true,
    locationId: 1,
    id: 42,
    name: "Amelia Hart",
    marketingStatus: "Eligible — Email",
    offersOptOut: false,
    guestSinceAt: "2026-05-12T10:00:00Z",
    lastActivityAt: "2026-07-20T14:22:00Z",
    lastInteractionLabel: "Feedback submitted",
    profileSummary: {
      email: "amelia@example.com",
      mobile: null,
      firstCapturedAt: "2026-05-12T10:00:00Z",
      locationName: "Camden Street",
      feedbackSubmissionCount: 2,
      offerClaimsAndRedemptions: 0,
      lastInteractionAt: "2026-07-20T14:22:00Z",
      lastInteractionLabel: "Feedback submitted",
      guestTags: [],
    },
    overviewDetails: {
      guestSinceAt: "2026-05-12T10:00:00Z",
      totalInteractions: 2,
      feedbackReceived: 2,
      offersClaimed: 0,
      campaignsSent: 0,
      lastActivityAt: "2026-07-20T14:22:00Z",
    },
    contactEligibility: [
      {
        channel: "email",
        status: "eligible",
        detailKind: "consent_captured",
        detailAt: null,
      },
      {
        channel: "sms",
        status: "not_provided",
        detailKind: null,
        detailAt: null,
      },
    ],
    latestFeedback: [],
    recentNotes: [],
    ...overrides,
  }
}

function createAdapters(
  getGuestProfile: Mock<OperatorGuestProfilePageAdapters["getGuestProfile"]>,
  overrides: Partial<OperatorGuestProfilePageAdapters> = {}
): OperatorGuestProfilePageAdapters {
  return {
    getGuestProfile,
    listGuestNotes:
      overrides.listGuestNotes ??
      (async () => ({
        items: [],
        totalCount: 0,
      })),
    createGuestNote:
      overrides.createGuestNote ??
      (async () => {
        throw new Error("createGuestNote not stubbed")
      }),
    getFeedbackDetails:
      overrides.getFeedbackDetails ??
      (async () => {
        throw new Error("getFeedbackDetails not stubbed")
      }),
    correctClassification:
      overrides.correctClassification ??
      (async () => {
        throw new Error("correctClassification not stubbed")
      }),
    exportGuestsCsv:
      overrides.exportGuestsCsv ??
      (async () => ({
        blob: new Blob(["id\n42\n"]),
        filename: "tummly-guests.csv",
      })),
    triggerBrowserDownload: overrides.triggerBrowserDownload ?? vi.fn(),
  }
}

function axiosStatusError(status: number): AxiosError {
  return new AxiosError(
    "Request failed",
    undefined,
    undefined,
    undefined,
    {
      status,
      statusText: "Error",
      headers: {},
      config: {} as never,
      data: { success: false },
    }
  )
}

describe("createOperatorGuestProfilePageModule", () => {
  it("loads a guest profile when workspace syncs", async () => {
    const getGuestProfile = vi.fn(async () => createGuestProfileResponse())
    const pageModule = createOperatorGuestProfilePageModule(
      createAdapters(getGuestProfile)
    )

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    expect(getGuestProfile).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
    })
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
    expect(pageModule.getSnapshot().viewModel?.name).toBe("Amelia Hart")
    expect(pageModule.getSnapshot().viewModel?.id).toBe("42")
  })

  it("does not refetch when the same guest and location pair syncs again", async () => {
    const getGuestProfile = vi.fn(async () => createGuestProfileResponse())
    const pageModule = createOperatorGuestProfilePageModule(
      createAdapters(getGuestProfile)
    )

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    getGuestProfile.mockClear()

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    expect(getGuestProfile).not.toHaveBeenCalled()
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
  })

  it("refetches when guest id or location changes", async () => {
    const getGuestProfile = vi.fn(async () => createGuestProfileResponse())
    const pageModule = createOperatorGuestProfilePageModule(
      createAdapters(getGuestProfile)
    )

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    getGuestProfile.mockClear()

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 2 })

    expect(getGuestProfile).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 2,
    })

    getGuestProfile.mockClear()
    getGuestProfile.mockResolvedValueOnce(
      createGuestProfileResponse({ id: 99, name: "Other Guest" })
    )

    await pageModule.syncWorkspace({ guestId: 99, selectedLocationId: 2 })

    expect(getGuestProfile).toHaveBeenCalledWith({
      guestId: 99,
      locationId: 2,
    })
    expect(pageModule.getSnapshot().viewModel?.name).toBe("Other Guest")
  })

  it("clears a loaded profile to unavailable when guest id becomes invalid", async () => {
    const getGuestProfile = vi.fn(async () => createGuestProfileResponse())
    const pageModule = createOperatorGuestProfilePageModule(
      createAdapters(getGuestProfile)
    )

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")

    await pageModule.syncWorkspace({ guestId: null, selectedLocationId: 1 })

    expect(pageModule.getSnapshot().loadStatus).toBe("unavailable")
    expect(pageModule.getSnapshot().viewModel).toBeNull()
    expect(getGuestProfile).toHaveBeenCalledTimes(1)
  })

  it("maps 404 and 403 to unavailable", async () => {
    for (const status of [404, 403] as const) {
      const getGuestProfile = vi.fn(async () => {
        throw axiosStatusError(status)
      })
      const pageModule = createOperatorGuestProfilePageModule(
        createAdapters(getGuestProfile)
      )

      await pageModule.syncWorkspace({ guestId: 99, selectedLocationId: 1 })

      expect(pageModule.getSnapshot().loadStatus).toBe("unavailable")
      expect(pageModule.getSnapshot().viewModel).toBeNull()
    }
  })

  it("enters error state on other failures and retries successfully", async () => {
    const getGuestProfile = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(createGuestProfileResponse())
    const pageModule = createOperatorGuestProfilePageModule(
      createAdapters(getGuestProfile)
    )

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    expect(pageModule.getSnapshot().loadStatus).toBe("error")

    await pageModule.retryLoad()

    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
    expect(pageModule.getSnapshot().viewModel?.name).toBe("Amelia Hart")
  })

  it("resets to idle when selected location is null", async () => {
    const getGuestProfile = vi.fn(async () => createGuestProfileResponse())
    const pageModule = createOperatorGuestProfilePageModule(
      createAdapters(getGuestProfile)
    )

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: null })

    expect(pageModule.getSnapshot().loadStatus).toBe("idle")
    expect(pageModule.getSnapshot().viewModel).toBeNull()
    expect(getGuestProfile).toHaveBeenCalledTimes(1)
  })

  it("opens Feedback details by feedback id from the profile module", async () => {
    const getGuestProfile = vi.fn(async () =>
      createGuestProfileResponse({
        latestFeedback: [
          {
            id: 77,
            createdAt: "2026-07-15T18:42:00Z",
            comment: "Slow service",
            locationName: "Camden Street",
            classificationStatus: "Succeeded",
            sentiment: "negative",
            detectedTags: ["WaitTime"],
          },
        ],
      })
    )
    const getFeedbackDetails = vi.fn(async (feedbackId: number) => ({
      success: true as const,
      id: feedbackId,
      guestName: "Amelia Hart",
      guestContact: "amelia@example.com",
      contactType: "Email" as const,
      comment: "Slow service",
      createdAt: "2026-07-15T18:42:00Z",
      locationName: "Camden Street",
      address: "1 High Street",
      classificationStatus: "Succeeded" as const,
      sentiment: "negative" as const,
      detectedTags: ["WaitTime"],
      locationGuestId: 42,
    }))
    const pageModule = createOperatorGuestProfilePageModule(
      createAdapters(getGuestProfile, { getFeedbackDetails })
    )

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    expect(pageModule.getSnapshot().viewModel?.latestFeedback).toHaveLength(1)

    const openPromise = pageModule.openFeedbackDetails(77)
    expect(pageModule.getSnapshot().feedbackDetails.isOpen).toBe(true)
    expect(pageModule.getSnapshot().feedbackDetails.loadStatus).toBe("loading")

    await openPromise

    expect(getFeedbackDetails).toHaveBeenCalledWith(77)
    expect(pageModule.getSnapshot().feedbackDetails.loadStatus).toBe("loaded")
    expect(pageModule.getSnapshot().feedbackDetails.details?.id).toBe(77)
    expect(
      pageModule.getSnapshot().feedbackDetails.details?.canViewGuestProfile
    ).toBe(true)

    pageModule.closeFeedbackDetails()
    expect(pageModule.getSnapshot().feedbackDetails.isOpen).toBe(false)
  })

  it("loads notes list on demand and refreshes after create", async () => {
    const getGuestProfile = vi
      .fn()
      .mockResolvedValueOnce(createGuestProfileResponse())
      .mockResolvedValueOnce(
        createGuestProfileResponse({
          recentNotes: [
            {
              id: 9,
              body: "Followed up by phone.",
              authorDisplayName: "Notes Owner",
              createdAt: "2026-07-22T12:00:00Z",
            },
          ],
        })
      )
    const listGuestNotes = vi
      .fn()
      .mockResolvedValueOnce({ items: [], totalCount: 0 })
      .mockResolvedValueOnce({
        items: [
          {
            id: 9,
            body: "Followed up by phone.",
            authorDisplayName: "Notes Owner",
            createdAt: "2026-07-22T12:00:00Z",
          },
        ],
        totalCount: 1,
      })
    const createGuestNote = vi.fn(async () => ({
      id: 9,
      body: "Followed up by phone.",
      authorDisplayName: "Notes Owner",
      createdAt: "2026-07-22T12:00:00Z",
    }))
    const pageModule = createOperatorGuestProfilePageModule(
      createAdapters(getGuestProfile, { listGuestNotes, createGuestNote })
    )

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    expect(pageModule.getSnapshot().notes.loadStatus).toBe("idle")

    await pageModule.ensureNotesLoaded()
    expect(listGuestNotes).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
    })
    expect(pageModule.getSnapshot().notes.loadStatus).toBe("loaded")
    expect(pageModule.getSnapshot().notes.items).toHaveLength(0)

    const created = await pageModule.createNote("Followed up by phone.")
    expect(created).toBe(true)
    expect(createGuestNote).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
      body: "Followed up by phone.",
    })
    expect(getGuestProfile).toHaveBeenCalledTimes(2)
    expect(listGuestNotes).toHaveBeenCalledTimes(2)
    expect(pageModule.getSnapshot().viewModel?.recentNotes).toHaveLength(1)
    expect(pageModule.getSnapshot().notes.items).toHaveLength(1)
    expect(pageModule.getSnapshot().notes.items[0]?.body).toBe(
      "Followed up by phone."
    )
  })

  it("exports this Location Guest via selected guestIds N=1 and downloads", async () => {
    const getGuestProfile = vi.fn(async () => createGuestProfileResponse())
    const exportGuestsCsv = vi.fn(async () => ({
      blob: new Blob(["id\n42\n"]),
      filename: "tummly-guests.csv",
    }))
    const triggerBrowserDownload = vi.fn()
    const pageModule = createOperatorGuestProfilePageModule(
      createAdapters(getGuestProfile, {
        exportGuestsCsv,
        triggerBrowserDownload,
      })
    )

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    const result = await pageModule.exportGuestRecord()

    expect(result).toEqual({ status: "exported" })
    expect(exportGuestsCsv).toHaveBeenCalledWith({
      locationId: 1,
      smartGroup: "all-guests",
      q: "",
      sort: "recent-activity",
      guestIds: [42],
    })
    expect(triggerBrowserDownload).toHaveBeenCalledWith(
      expect.any(Blob),
      "tummly-guests.csv"
    )
  })

  it("returns an error when export fails and stays on profile", async () => {
    const getGuestProfile = vi.fn(async () => createGuestProfileResponse())
    const pageModule = createOperatorGuestProfilePageModule(
      createAdapters(getGuestProfile, {
        exportGuestsCsv: vi.fn(async () => {
          throw new Error("export failed")
        }),
      })
    )

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    const result = await pageModule.exportGuestRecord()

    expect(result).toEqual({
      status: "error",
      message: "Could not export guest record. Please try again.",
    })
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
  })
})

import { AxiosError } from "axios"
import { describe, expect, it, vi, type Mock } from "vitest"

import {
  createOperatorGuestEditPageModule,
  type OperatorGuestEditPageAdapters,
} from "@/lib/operatorGuestProfile/createOperatorGuestEditPageModule"
import type { GuestTag } from "@/lib/operatorGuests/guestTag"
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
      guestTags: [
        { id: 10, name: "VIP" },
        { id: 11, name: "Regular" },
      ],
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

const defaultCatalog: GuestTag[] = [
  { id: "10", name: "VIP", guestCount: 4 },
  { id: "11", name: "Regular", guestCount: 2 },
  { id: "12", name: "New", guestCount: 1 },
]

function createAdapters(
  overrides: Partial<OperatorGuestEditPageAdapters> = {}
): {
  adapters: OperatorGuestEditPageAdapters
  getGuestProfile: Mock<OperatorGuestEditPageAdapters["getGuestProfile"]>
  patchGuestIdentity: Mock<OperatorGuestEditPageAdapters["patchGuestIdentity"]>
  listGuestTags: Mock<OperatorGuestEditPageAdapters["listGuestTags"]>
  syncGuestTags: Mock<OperatorGuestEditPageAdapters["syncGuestTags"]>
  createGuestNote: Mock<OperatorGuestEditPageAdapters["createGuestNote"]>
  getFeedbackDetails: Mock<OperatorGuestEditPageAdapters["getFeedbackDetails"]>
  exportGuestsCsv: Mock<OperatorGuestEditPageAdapters["exportGuestsCsv"]>
  triggerBrowserDownload: Mock<
    OperatorGuestEditPageAdapters["triggerBrowserDownload"]
  >
  deleteLocationGuest: Mock<
    OperatorGuestEditPageAdapters["deleteLocationGuest"]
  >
} {
  const getGuestProfile =
    overrides.getGuestProfile ??
    vi.fn(async () => createGuestProfileResponse())
  const patchGuestIdentity =
    overrides.patchGuestIdentity ??
    vi.fn(async () => ({ success: true, changedFields: ["name"] }))
  const listGuestTags =
    overrides.listGuestTags ?? vi.fn(async () => defaultCatalog)
  const syncGuestTags =
    overrides.syncGuestTags ?? vi.fn(async () => undefined)
  const createGuestNote =
    overrides.createGuestNote ??
    vi.fn(async () => ({
      id: 1,
      body: "Note",
      authorDisplayName: "Notes Owner",
      createdAt: "2026-07-22T12:00:00Z",
    }))
  const getFeedbackDetails =
    overrides.getFeedbackDetails ??
    vi.fn(async () => {
      throw new Error("getFeedbackDetails not stubbed")
    })
  const exportGuestsCsv =
    overrides.exportGuestsCsv ??
    vi.fn(async () => ({
      blob: new Blob(["csv"]),
      filename: "tummly-guests-selected-1-2026-07-23T12-00-00Z.csv",
    }))
  const triggerBrowserDownload =
    overrides.triggerBrowserDownload ?? vi.fn()
  const deleteLocationGuest =
    overrides.deleteLocationGuest ?? vi.fn(async () => undefined)

  return {
    adapters: {
      getGuestProfile,
      patchGuestIdentity,
      listGuestTags,
      syncGuestTags,
      createGuestNote,
      getFeedbackDetails,
      correctClassification:
        overrides.correctClassification ??
        (async () => {
          throw new Error("correctClassification not stubbed")
        }),
      exportGuestsCsv,
      triggerBrowserDownload,
      deleteLocationGuest,
    },
    getGuestProfile: getGuestProfile as Mock<
      OperatorGuestEditPageAdapters["getGuestProfile"]
    >,
    patchGuestIdentity: patchGuestIdentity as Mock<
      OperatorGuestEditPageAdapters["patchGuestIdentity"]
    >,
    listGuestTags: listGuestTags as Mock<
      OperatorGuestEditPageAdapters["listGuestTags"]
    >,
    syncGuestTags: syncGuestTags as Mock<
      OperatorGuestEditPageAdapters["syncGuestTags"]
    >,
    createGuestNote: createGuestNote as Mock<
      OperatorGuestEditPageAdapters["createGuestNote"]
    >,
    getFeedbackDetails: getFeedbackDetails as Mock<
      OperatorGuestEditPageAdapters["getFeedbackDetails"]
    >,
    exportGuestsCsv: exportGuestsCsv as Mock<
      OperatorGuestEditPageAdapters["exportGuestsCsv"]
    >,
    triggerBrowserDownload: triggerBrowserDownload as Mock<
      OperatorGuestEditPageAdapters["triggerBrowserDownload"]
    >,
    deleteLocationGuest: deleteLocationGuest as Mock<
      OperatorGuestEditPageAdapters["deleteLocationGuest"]
    >,
  }
}

function axiosStatusError(
  status: number,
  message?: string
): AxiosError {
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
      data: { success: false, message },
    }
  )
}

describe("createOperatorGuestEditPageModule", () => {
  it("loads guest identity into editable draft fields", async () => {
    const { adapters, getGuestProfile } = createAdapters()
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    expect(getGuestProfile).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
    })
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.draft).toEqual({
      firstName: "Amelia",
      lastName: "Hart",
      email: "amelia@example.com",
      phone: "",
    })
  })

  it("seeds tag draft from live memberships and catalog", async () => {
    const { adapters, listGuestTags } = createAdapters()
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    expect(listGuestTags).toHaveBeenCalledWith({ locationId: 1 })
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.serverTagIds).toEqual(["10", "11"])
    expect(snapshot.pendingTagIds).toEqual(["10", "11"])
    expect(snapshot.tagsDirty).toBe(false)
    expect(snapshot.tagCatalog.map((tag) => tag.id)).toEqual([
      "10",
      "11",
      "12",
    ])
  })

  it("stages and unstages tags independently of identity Save", async () => {
    const { adapters, patchGuestIdentity, syncGuestTags } = createAdapters()
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.stageTag("12")
    pageModule.unstageTag("11")

    expect(pageModule.getSnapshot().pendingTagIds).toEqual(["10", "12"])
    expect(pageModule.getSnapshot().tagsDirty).toBe(true)
    expect(patchGuestIdentity).not.toHaveBeenCalled()
    expect(syncGuestTags).not.toHaveBeenCalled()
  })

  it("Cancel restores server tag memberships without calling sync", async () => {
    const { adapters, syncGuestTags } = createAdapters()
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.stageTag("12")
    pageModule.unstageTag("10")
    pageModule.cancelTagDraft()

    expect(pageModule.getSnapshot().pendingTagIds).toEqual(["10", "11"])
    expect(pageModule.getSnapshot().tagsDirty).toBe(false)
    expect(syncGuestTags).not.toHaveBeenCalled()
  })

  it("Apply tags syncs the pending set and clears dirty", async () => {
    const { adapters, syncGuestTags } = createAdapters()
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.unstageTag("11")
    pageModule.stageTag("12")

    const result = await pageModule.applyTags()

    expect(result).toEqual({ status: "applied" })
    expect(syncGuestTags).toHaveBeenCalledWith({
      locationId: 1,
      guestIds: [42],
      tagIds: [10, 12],
    })
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.serverTagIds).toEqual(["10", "12"])
    expect(snapshot.pendingTagIds).toEqual(["10", "12"])
    expect(snapshot.tagsDirty).toBe(false)
    expect(snapshot.viewModel?.profileSummary.guestTags).toEqual([
      { id: "10", name: "VIP" },
      { id: "12", name: "New" },
    ])
    expect(snapshot.viewModel?.profileSummary.guestTagsDisplay).toBe(
      "VIP, New"
    )
  })

  it("rejects save when both contacts are cleared without calling PATCH", async () => {
    const { adapters, patchGuestIdentity } = createAdapters()
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.setDraftField("email", "")
    pageModule.setDraftField("phone", "")

    const result = await pageModule.saveChanges()

    expect(result).toEqual({ status: "validation" })
    expect(patchGuestIdentity).not.toHaveBeenCalled()
    expect(pageModule.getSnapshot().fieldErrors.form).toMatch(/contact/i)
  })

  it("rejects invalid UK phone on the client before PATCH", async () => {
    const { adapters, patchGuestIdentity } = createAdapters()
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.setDraftField("phone", "12345")

    const result = await pageModule.saveChanges()

    expect(result).toEqual({ status: "validation" })
    expect(patchGuestIdentity).not.toHaveBeenCalled()
    expect(pageModule.getSnapshot().fieldErrors.phone).toMatch(/UK phone/i)
  })

  it("PATCHes identity and reports saved on success", async () => {
    const { adapters, patchGuestIdentity } = createAdapters({
      patchGuestIdentity: vi.fn(async () => ({
        success: true,
        changedFields: ["email"],
      })),
    })
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.setDraftField("email", "new@example.com")

    const result = await pageModule.saveChanges()

    expect(result).toEqual({ status: "saved" })
    expect(patchGuestIdentity).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
      body: {
        firstName: "Amelia",
        lastName: "Hart",
        email: "new@example.com",
        phone: null,
      },
    })
  })

  it("surfaces API errors from identity PATCH", async () => {
    const { adapters } = createAdapters({
      patchGuestIdentity: vi.fn(async () => {
        throw axiosStatusError(400, "At least one contact method is required.")
      }),
    })
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.setDraftField("email", "new@example.com")

    const result = await pageModule.saveChanges()

    expect(result).toEqual({
      status: "error",
      message: "At least one contact method is required.",
    })
    expect(pageModule.getSnapshot().saveStatus).toBe("error")
  })

  it("surfaces collision errors from PATCH 409", async () => {
    const { adapters } = createAdapters({
      patchGuestIdentity: vi.fn(async () => {
        throw axiosStatusError(
          409,
          "Email already belongs to another guest."
        )
      }),
    })
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.setDraftField("email", "taken@example.com")

    const result = await pageModule.saveChanges()

    expect(result).toEqual({
      status: "error",
      message: "Email already belongs to another guest.",
    })
    expect(pageModule.getSnapshot().saveError).toBe(
      "Email already belongs to another guest."
    )
  })

  it("marks workspace unavailable for invalid guest id", async () => {
    const { adapters, getGuestProfile } = createAdapters()
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: null, selectedLocationId: 1 })

    expect(getGuestProfile).not.toHaveBeenCalled()
    expect(pageModule.getSnapshot().loadStatus).toBe("unavailable")
  })

  it("saves an internal note immediately, clears draft, and refreshes profile", async () => {
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
    const createGuestNote = vi.fn(async () => ({
      id: 9,
      body: "Followed up by phone.",
      authorDisplayName: "Notes Owner",
      createdAt: "2026-07-22T12:00:00Z",
    }))
    const { adapters } = createAdapters({ getGuestProfile, createGuestNote })
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.setDraftField("email", "unsaved@example.com")
    pageModule.setNoteDraft("Followed up by phone.")
    expect(pageModule.getSnapshot().noteDraft).toBe("Followed up by phone.")

    const saved = await pageModule.saveNote()

    expect(saved).toBe(true)
    expect(createGuestNote).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
      body: "Followed up by phone.",
    })
    expect(pageModule.getSnapshot().noteDraft).toBe("")
    expect(pageModule.getSnapshot().noteSaveStatus).toBe("idle")
    expect(getGuestProfile).toHaveBeenCalledTimes(2)
    expect(pageModule.getSnapshot().viewModel?.recentNotes).toHaveLength(1)
    expect(pageModule.getSnapshot().draft.email).toBe("unsaved@example.com")
  })

  it("cancels an internal note draft without posting", async () => {
    const { adapters, createGuestNote } = createAdapters()
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.setNoteDraft("Draft that should clear")
    pageModule.cancelNoteDraft()

    expect(pageModule.getSnapshot().noteDraft).toBe("")
    expect(createGuestNote).not.toHaveBeenCalled()
  })

  it("does not create notes when saving guest identity", async () => {
    const { adapters, createGuestNote, patchGuestIdentity } = createAdapters()
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.setNoteDraft("Should not POST with identity save")
    pageModule.setDraftField("email", "new@example.com")

    const result = await pageModule.saveChanges()

    expect(result).toEqual({ status: "saved" })
    expect(patchGuestIdentity).toHaveBeenCalled()
    expect(createGuestNote).not.toHaveBeenCalled()
    expect(pageModule.getSnapshot().noteDraft).toBe(
      "Should not POST with identity save"
    )
  })

  it("opens Feedback details by feedback id from the edit module", async () => {
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
    const { adapters } = createAdapters({ getGuestProfile, getFeedbackDetails })
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    expect(pageModule.getSnapshot().viewModel?.latestFeedback).toHaveLength(1)

    const openPromise = pageModule.openFeedbackDetails(77)
    expect(pageModule.getSnapshot().feedbackDetails.isOpen).toBe(true)
    expect(pageModule.getSnapshot().feedbackDetails.loadStatus).toBe("loading")

    await openPromise

    expect(getFeedbackDetails).toHaveBeenCalledWith(77)
    expect(pageModule.getSnapshot().feedbackDetails.loadStatus).toBe("loaded")
    expect(pageModule.getSnapshot().feedbackDetails.details?.id).toBe(77)

    pageModule.closeFeedbackDetails()
    expect(pageModule.getSnapshot().feedbackDetails.isOpen).toBe(false)
  })

  it("exposes View all feedbacks navigation to the profile Feedbacks tab", async () => {
    const { adapters } = createAdapters({
      getGuestProfile: vi.fn(async () =>
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
      ),
    })
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    expect(pageModule.getViewAllFeedbacksNavigation()).toEqual({
      guestId: 42,
      tab: "feedbacks",
    })
  })

  it("exports this Location Guest via selected guestIds N=1 and downloads", async () => {
    const { adapters, exportGuestsCsv, triggerBrowserDownload } =
      createAdapters()
    const pageModule = createOperatorGuestEditPageModule(adapters)

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
      "tummly-guests-selected-1-2026-07-23T12-00-00Z.csv"
    )
    expect(pageModule.getSnapshot().exportStatus).toBe("idle")
  })

  it("surfaces export failure without leaving the page state", async () => {
    const { adapters } = createAdapters({
      exportGuestsCsv: vi.fn(async () => {
        throw new Error("network")
      }),
    })
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    const result = await pageModule.exportGuestRecord()

    expect(result).toEqual({
      status: "error",
      message: "Could not export guest record. Please try again.",
    })
    expect(pageModule.getSnapshot().exportStatus).toBe("idle")
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
  })

  it("deletes this Location Guest and reports deleted", async () => {
    const { adapters, deleteLocationGuest } = createAdapters()
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.setDraftField("firstName", "Draft")

    const result = await pageModule.deleteGuest()

    expect(result).toEqual({ status: "deleted" })
    expect(deleteLocationGuest).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
    })
    expect(pageModule.getSnapshot().deleteStatus).toBe("idle")
  })

  it("keeps edit workspace and re-enables delete after delete failure", async () => {
    const { adapters } = createAdapters({
      deleteLocationGuest: vi.fn(async () => {
        throw axiosStatusError(500, "Server error")
      }),
    })
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    const result = await pageModule.deleteGuest()

    expect(result).toEqual({
      status: "error",
      message: "Server error",
    })
    expect(pageModule.getSnapshot().deleteStatus).toBe("idle")
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
  })

  it("sets deleteStatus deleting while delete is in flight", async () => {
    let resolveDelete: (() => void) | undefined
    const { adapters } = createAdapters({
      deleteLocationGuest: vi.fn(
        () =>
          new Promise<void>((resolve) => {
            resolveDelete = resolve
          })
      ),
    })
    const pageModule = createOperatorGuestEditPageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    const pending = pageModule.deleteGuest()
    expect(pageModule.getSnapshot().deleteStatus).toBe("deleting")

    resolveDelete?.()
    await pending
    expect(pageModule.getSnapshot().deleteStatus).toBe("idle")
  })
})

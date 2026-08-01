import { AxiosError } from "axios"
import { describe, expect, it, vi, type Mock } from "vitest"

import {
  createOperatorGuestProfilePageModule,
  type OperatorGuestProfilePageAdapters,
} from "@/lib/operatorGuestProfile/createOperatorGuestProfilePageModule"
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
  overrides: Partial<OperatorGuestProfilePageAdapters> = {}
): {
  adapters: OperatorGuestProfilePageAdapters
  getGuestProfile: Mock<OperatorGuestProfilePageAdapters["getGuestProfile"]>
  listGuestNotes: Mock<OperatorGuestProfilePageAdapters["listGuestNotes"]>
  createGuestNote: Mock<OperatorGuestProfilePageAdapters["createGuestNote"]>
  updateGuestNote: Mock<OperatorGuestProfilePageAdapters["updateGuestNote"]>
  softDeleteGuestNote: Mock<
    OperatorGuestProfilePageAdapters["softDeleteGuestNote"]
  >
  patchGuestIdentity: Mock<OperatorGuestProfilePageAdapters["patchGuestIdentity"]>
  listGuestTags: Mock<OperatorGuestProfilePageAdapters["listGuestTags"]>
  syncGuestTags: Mock<OperatorGuestProfilePageAdapters["syncGuestTags"]>
  getGuestActivity: Mock<OperatorGuestProfilePageAdapters["getGuestActivity"]>
  getGuestFeedbacks: Mock<OperatorGuestProfilePageAdapters["getGuestFeedbacks"]>
  getFeedbackDetails: Mock<OperatorGuestProfilePageAdapters["getFeedbackDetails"]>
  exportGuestsCsv: Mock<OperatorGuestProfilePageAdapters["exportGuestsCsv"]>
  triggerBrowserDownload: Mock<
    OperatorGuestProfilePageAdapters["triggerBrowserDownload"]
  >
  deleteLocationGuest: Mock<
    OperatorGuestProfilePageAdapters["deleteLocationGuest"]
  >
} {
  const getGuestProfile =
    overrides.getGuestProfile ??
    vi.fn(async () => createGuestProfileResponse())
  const listGuestNotes =
    overrides.listGuestNotes ??
    vi.fn(async () => ({
      items: [],
      totalCount: 0,
    }))
  const createGuestNote =
    overrides.createGuestNote ??
    vi.fn(async () => ({
      id: 1,
      body: "Note",
      authorDisplayName: "Notes Owner",
      createdAt: "2026-07-22T12:00:00Z",
    }))
  const updateGuestNote =
    overrides.updateGuestNote ??
    vi.fn(async (params) => ({
      id: params.noteId,
      body: params.body,
      authorDisplayName: "Notes Owner",
      createdAt: "2026-07-22T12:00:00Z",
      updatedAt: "2026-07-22T12:30:00Z",
    }))
  const softDeleteGuestNote = overrides.softDeleteGuestNote ?? vi.fn(async () => ({ deletedAt: "2026-07-14T13:00:00.000Z", deletedByDisplayName: "Ada Operator" }))
  const patchGuestIdentity =
    overrides.patchGuestIdentity ??
    vi.fn(async () => ({ success: true, changedFields: ["name"] }))
  const listGuestTags =
    overrides.listGuestTags ?? vi.fn(async () => defaultCatalog)
  const syncGuestTags =
    overrides.syncGuestTags ?? vi.fn(async () => undefined)
  const getGuestActivity =
    overrides.getGuestActivity ??
    vi.fn(async () => ({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    }))
  const getGuestFeedbacks =
    overrides.getGuestFeedbacks ??
    vi.fn(async () => ({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    }))
  const getFeedbackDetails =
    overrides.getFeedbackDetails ??
    vi.fn(async () => {
      throw new Error("getFeedbackDetails not stubbed")
    })
  const exportGuestsCsv =
    overrides.exportGuestsCsv ??
    vi.fn(async () => ({
      blob: new Blob(["id\n42\n"]),
      filename: "tummly-guests.csv",
    }))
  const triggerBrowserDownload =
    overrides.triggerBrowserDownload ?? vi.fn()
  const deleteLocationGuest =
    overrides.deleteLocationGuest ?? vi.fn(async () => undefined)

  return {
    adapters: {
      getGuestProfile,
      listGuestNotes,
      createGuestNote,
      updateGuestNote,
      softDeleteGuestNote,
      patchGuestIdentity,
      listGuestTags,
      syncGuestTags,
      getGuestActivity,
      getGuestFeedbacks,
      getFeedbackDetails,
      correctClassification:
        overrides.correctClassification ??
        (async () => {
          throw new Error("correctClassification not stubbed")
        }),
      setWorkflowStatus:
        overrides.setWorkflowStatus ??
        (async (_feedbackId, workflowStatus) => ({
          workflowStatus,
          needsAttention: false,
          activityEvent: null,
        })),
      createInternalNote:
        overrides.createInternalNote ??
        (async (_feedbackId, body) => ({
          id: 1,
          body,
          authorDisplayName: "Test Operator",
          createdAt: "2026-07-14T12:00:00.000Z",
        })),
      updateInternalNote:
        overrides.updateInternalNote ??
        (async (_feedbackId, noteId, body) => ({
          id: noteId,
          body,
          authorDisplayName: "Test Operator",
          createdAt: "2026-07-14T12:00:00.000Z",
          updatedAt: "2026-07-14T12:30:00.000Z",
        })),
      deleteInternalNote: overrides.deleteInternalNote ?? (async () => ({ deletedAt: "2026-07-14T13:00:00.000Z", deletedByDisplayName: "Ada Operator" })),
      exportGuestsCsv,
      triggerBrowserDownload,
      deleteLocationGuest,
    },
    getGuestProfile: getGuestProfile as Mock<
      OperatorGuestProfilePageAdapters["getGuestProfile"]
    >,
    listGuestNotes: listGuestNotes as Mock<
      OperatorGuestProfilePageAdapters["listGuestNotes"]
    >,
    createGuestNote: createGuestNote as Mock<
      OperatorGuestProfilePageAdapters["createGuestNote"]
    >,
    updateGuestNote: updateGuestNote as Mock<
      OperatorGuestProfilePageAdapters["updateGuestNote"]
    >,
    softDeleteGuestNote: softDeleteGuestNote as Mock<
      OperatorGuestProfilePageAdapters["softDeleteGuestNote"]
    >,
    patchGuestIdentity: patchGuestIdentity as Mock<
      OperatorGuestProfilePageAdapters["patchGuestIdentity"]
    >,
    listGuestTags: listGuestTags as Mock<
      OperatorGuestProfilePageAdapters["listGuestTags"]
    >,
    syncGuestTags: syncGuestTags as Mock<
      OperatorGuestProfilePageAdapters["syncGuestTags"]
    >,
    getGuestActivity: getGuestActivity as Mock<
      OperatorGuestProfilePageAdapters["getGuestActivity"]
    >,
    getGuestFeedbacks: getGuestFeedbacks as Mock<
      OperatorGuestProfilePageAdapters["getGuestFeedbacks"]
    >,
    getFeedbackDetails: getFeedbackDetails as Mock<
      OperatorGuestProfilePageAdapters["getFeedbackDetails"]
    >,
    exportGuestsCsv: exportGuestsCsv as Mock<
      OperatorGuestProfilePageAdapters["exportGuestsCsv"]
    >,
    triggerBrowserDownload: triggerBrowserDownload as Mock<
      OperatorGuestProfilePageAdapters["triggerBrowserDownload"]
    >,
    deleteLocationGuest: deleteLocationGuest as Mock<
      OperatorGuestProfilePageAdapters["deleteLocationGuest"]
    >,
  }
}

function axiosStatusError(status: number, message?: string): AxiosError {
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

describe("createOperatorGuestProfilePageModule", () => {
  it("loads a guest profile when workspace syncs", async () => {
    const { adapters, getGuestProfile } = createAdapters()
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    expect(getGuestProfile).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
    })
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
    expect(pageModule.getSnapshot().viewModel?.name).toBe("Amelia Hart")
    expect(pageModule.getSnapshot().viewModel?.id).toBe("42")
  })

  it("does not refetch when the same guest and location pair syncs again (Profile↔Edit)", async () => {
    const { adapters, getGuestProfile, listGuestTags } = createAdapters()
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    getGuestProfile.mockClear()
    listGuestTags.mockClear()

    // Simulate Edit route remount calling syncWorkspace on the same layout module.
    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    expect(getGuestProfile).not.toHaveBeenCalled()
    expect(listGuestTags).not.toHaveBeenCalled()
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
  })

  it("exposes one internal Activity and Feedbacks tab module instance for the visit", async () => {
    const { adapters } = createAdapters()
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    expect(pageModule.activityTab).toBeDefined()
    expect(pageModule.feedbacksTab).toBeDefined()
    expect(pageModule.activityTab).toBe(pageModule.activityTab)
    expect(pageModule.feedbacksTab).toBe(pageModule.feedbacksTab)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    const activityBefore = pageModule.activityTab
    const feedbacksBefore = pageModule.feedbacksTab

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    expect(pageModule.activityTab).toBe(activityBefore)
    expect(pageModule.feedbacksTab).toBe(feedbacksBefore)
  })

  it("refetches when guest id or location changes", async () => {
    const { adapters, getGuestProfile } = createAdapters()
    const pageModule = createOperatorGuestProfilePageModule(adapters)

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
    const { adapters, getGuestProfile } = createAdapters()
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")

    await pageModule.syncWorkspace({ guestId: null, selectedLocationId: 1 })

    expect(pageModule.getSnapshot().loadStatus).toBe("unavailable")
    expect(pageModule.getSnapshot().viewModel).toBeNull()
    expect(getGuestProfile).toHaveBeenCalledTimes(1)
  })

  it("maps 404 and 403 to unavailable", async () => {
    for (const status of [404, 403] as const) {
      const { adapters } = createAdapters({
        getGuestProfile: vi.fn(async () => {
          throw axiosStatusError(status)
        }),
      })
      const pageModule = createOperatorGuestProfilePageModule(adapters)

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
    const { adapters } = createAdapters({ getGuestProfile })
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    expect(pageModule.getSnapshot().loadStatus).toBe("error")

    await pageModule.retryLoad()

    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
    expect(pageModule.getSnapshot().viewModel?.name).toBe("Amelia Hart")
  })

  it("resets to idle when selected location is null", async () => {
    const { adapters, getGuestProfile } = createAdapters()
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: null })

    expect(pageModule.getSnapshot().loadStatus).toBe("idle")
    expect(pageModule.getSnapshot().viewModel).toBeNull()
    expect(getGuestProfile).toHaveBeenCalledTimes(1)
  })

  it("opens Feedback details by feedback id from the profile module", async () => {
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
      getFeedbackDetails,
    })
    const pageModule = createOperatorGuestProfilePageModule(adapters)

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

  it("createNote patches recentNotes and invalidates notes + Activity without full profile refetch", async () => {
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
    const getGuestActivity = vi.fn(async () => ({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    }))
    const { adapters, getGuestProfile } = createAdapters({
      listGuestNotes,
      createGuestNote,
      getGuestActivity,
    })
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    expect(getGuestProfile).toHaveBeenCalledTimes(1)

    await pageModule.activityTab.syncWorkspace({
      guestId: 42,
      selectedLocationId: 1,
      active: true,
    })
    expect(getGuestActivity).toHaveBeenCalledTimes(1)
    getGuestActivity.mockClear()

    await pageModule.ensureNotesLoaded()
    expect(listGuestNotes).toHaveBeenCalledTimes(1)

    const created = await pageModule.createNote("Followed up by phone.")
    expect(created).toBe(true)
    expect(createGuestNote).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
      body: "Followed up by phone.",
    })
    // Explicit invalidate map: notes + activity, not full profile.
    expect(getGuestProfile).toHaveBeenCalledTimes(1)
    expect(listGuestNotes).toHaveBeenCalledTimes(2)
    expect(getGuestActivity).toHaveBeenCalledTimes(1)
    expect(pageModule.getSnapshot().viewModel?.recentNotes).toHaveLength(1)
    expect(pageModule.getSnapshot().viewModel?.recentNotes[0]?.body).toBe(
      "Followed up by phone."
    )
    expect(pageModule.getSnapshot().notes.items).toHaveLength(1)
  })

  it("exports this Location Guest via selected guestIds N=1 and downloads", async () => {
    const { adapters, exportGuestsCsv, triggerBrowserDownload } =
      createAdapters()
    const pageModule = createOperatorGuestProfilePageModule(adapters)

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
    const { adapters } = createAdapters({
      exportGuestsCsv: vi.fn(async () => {
        throw new Error("export failed")
      }),
    })
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    const result = await pageModule.exportGuestRecord()

    expect(result).toEqual({
      status: "error",
      message: "Could not export guest record. Please try again.",
    })
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
  })

  it("loads guest identity into editable draft fields", async () => {
    const { adapters, getGuestProfile } = createAdapters()
    const pageModule = createOperatorGuestProfilePageModule(adapters)

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
    const pageModule = createOperatorGuestProfilePageModule(adapters)

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
    const pageModule = createOperatorGuestProfilePageModule(adapters)

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
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.stageTag("12")
    pageModule.unstageTag("10")
    pageModule.cancelTagDraft()

    expect(pageModule.getSnapshot().pendingTagIds).toEqual(["10", "11"])
    expect(pageModule.getSnapshot().tagsDirty).toBe(false)
    expect(syncGuestTags).not.toHaveBeenCalled()
  })

  it("Apply tags syncs the pending set, patches profile tags, and invalidates Activity", async () => {
    const getGuestActivity = vi.fn(async () => ({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    }))
    const { adapters, syncGuestTags, getGuestProfile } = createAdapters({
      getGuestActivity,
    })
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    await pageModule.activityTab.syncWorkspace({
      guestId: 42,
      selectedLocationId: 1,
      active: true,
    })
    getGuestActivity.mockClear()
    getGuestProfile.mockClear()

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
    // Tag sync → local tags + Activity; no full profile refetch.
    expect(getGuestProfile).not.toHaveBeenCalled()
    expect(getGuestActivity).toHaveBeenCalledTimes(1)
  })

  it("rejects save when both contacts are cleared without calling PATCH", async () => {
    const { adapters, patchGuestIdentity } = createAdapters()
    const pageModule = createOperatorGuestProfilePageModule(adapters)

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
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.setDraftField("phone", "12345")

    const result = await pageModule.saveChanges()

    expect(result).toEqual({ status: "validation" })
    expect(patchGuestIdentity).not.toHaveBeenCalled()
    expect(pageModule.getSnapshot().fieldErrors.phone).toMatch(/UK phone/i)
  })

  it("PATCHes identity and refetches profile snapshot on success", async () => {
    const { adapters, patchGuestIdentity, getGuestProfile } = createAdapters({
      patchGuestIdentity: vi.fn(async () => ({
        success: true,
        changedFields: ["email"],
      })),
    })
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    expect(getGuestProfile).toHaveBeenCalledTimes(1)
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
    // Identity save → full profile invalidate.
    expect(getGuestProfile).toHaveBeenCalledTimes(2)
  })

  it("surfaces API errors from identity PATCH", async () => {
    const { adapters } = createAdapters({
      patchGuestIdentity: vi.fn(async () => {
        throw axiosStatusError(400, "At least one contact method is required.")
      }),
    })
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.setDraftField("email", "new@example.com")

    const result = await pageModule.saveChanges()

    expect(result).toEqual({
      status: "error",
      message: "At least one contact method is required.",
    })
    expect(pageModule.getSnapshot().saveStatus).toBe("error")
  })

  it("saves an internal note, clears draft, patches recentNotes without full profile refetch", async () => {
    const createGuestNote = vi.fn(async () => ({
      id: 9,
      body: "Followed up by phone.",
      authorDisplayName: "Notes Owner",
      createdAt: "2026-07-22T12:00:00Z",
    }))
    const { adapters, getGuestProfile } = createAdapters({ createGuestNote })
    const pageModule = createOperatorGuestProfilePageModule(adapters)

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
    expect(getGuestProfile).toHaveBeenCalledTimes(1)
    expect(pageModule.getSnapshot().viewModel?.recentNotes).toHaveLength(1)
    expect(pageModule.getSnapshot().draft.email).toBe("unsaved@example.com")
  })

  it("cancels an internal note draft without posting", async () => {
    const { adapters, createGuestNote } = createAdapters()
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.setNoteDraft("Draft that should clear")
    pageModule.cancelNoteDraft()

    expect(pageModule.getSnapshot().noteDraft).toBe("")
    expect(createGuestNote).not.toHaveBeenCalled()
  })

  it("does not create notes when saving guest identity", async () => {
    const { adapters, createGuestNote, patchGuestIdentity } = createAdapters()
    const pageModule = createOperatorGuestProfilePageModule(adapters)

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

  it("exposes View all feedbacks navigation to the profile Feedbacks tab", async () => {
    const { adapters } = createAdapters()
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    expect(pageModule.getViewAllFeedbacksNavigation()).toEqual({
      guestId: 42,
      tab: "feedbacks",
    })
  })

  it("deletes this Location Guest and reports deleted", async () => {
    const { adapters, deleteLocationGuest } = createAdapters()
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    pageModule.setDraftField("firstName", "Draft")

    const result = await pageModule.deleteLocationGuest()

    expect(result).toEqual({ status: "deleted" })
    expect(deleteLocationGuest).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
    })
    expect(pageModule.getSnapshot().deleteStatus).toBe("idle")
  })

  it("keeps workspace and re-enables delete after delete failure", async () => {
    const { adapters } = createAdapters({
      deleteLocationGuest: vi.fn(async () => {
        throw axiosStatusError(500, "Server error")
      }),
    })
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    const result = await pageModule.deleteLocationGuest()

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
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })

    const pending = pageModule.deleteLocationGuest()
    expect(pageModule.getSnapshot().deleteStatus).toBe("deleting")

    resolveDelete?.()
    await pending
    expect(pageModule.getSnapshot().deleteStatus).toBe("idle")
  })

  it("updates a Location Guest note and invalidates notes, profile, and activity", async () => {
    const listGuestNotes = vi
      .fn()
      .mockResolvedValueOnce({
        items: [
          {
            id: 9,
            body: "Original note",
            authorDisplayName: "Notes Owner",
            createdAt: "2026-07-22T12:00:00Z",
          },
        ],
        totalCount: 1,
      })
      .mockResolvedValueOnce({
        items: [
          {
            id: 9,
            body: "Updated note",
            authorDisplayName: "Notes Owner",
            createdAt: "2026-07-22T12:00:00Z",
            updatedAt: "2026-07-22T12:30:00Z",
          },
        ],
        totalCount: 1,
      })
    const updateGuestNote = vi.fn(async () => ({
      id: 9,
      body: "Updated note",
      authorDisplayName: "Notes Owner",
      createdAt: "2026-07-22T12:00:00Z",
      updatedAt: "2026-07-22T12:30:00Z",
    }))
    const getGuestProfile = vi.fn(async () => createGuestProfileResponse())
    const getGuestActivity = vi.fn(async () => ({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    }))
    const { adapters } = createAdapters({
      listGuestNotes,
      updateGuestNote,
      getGuestProfile,
      getGuestActivity,
    })
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    await pageModule.activityTab.syncWorkspace({
      guestId: 42,
      selectedLocationId: 1,
      active: true,
    })
    await pageModule.ensureNotesLoaded()
    getGuestProfile.mockClear()
    getGuestActivity.mockClear()

    const updated = await pageModule.updateNote(9, "Updated note")

    expect(updated).toBe(true)
    expect(updateGuestNote).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
      noteId: 9,
      body: "Updated note",
    })
    expect(listGuestNotes).toHaveBeenCalledTimes(2)
    expect(getGuestProfile).toHaveBeenCalledTimes(1)
    expect(getGuestActivity).toHaveBeenCalledTimes(1)
    expect(pageModule.getSnapshot().notes.items[0]?.body).toBe("Updated note")
    expect(pageModule.getSnapshot().notes.items[0]?.isEdited).toBe(true)
  })

  it("soft-deletes a Location Guest note and invalidates notes, profile, and activity", async () => {
    const listGuestNotes = vi
      .fn()
      .mockResolvedValueOnce({
        items: [
          {
            id: 9,
            body: "To delete",
            authorDisplayName: "Notes Owner",
            createdAt: "2026-07-22T12:00:00Z",
          },
        ],
        totalCount: 1,
      })
      .mockResolvedValueOnce({
        items: [],
        totalCount: 0,
      })
    const softDeleteGuestNote = vi.fn(async () => ({ deletedAt: "2026-07-14T13:00:00.000Z", deletedByDisplayName: "Ada Operator" }))
    const getGuestProfile = vi
      .fn()
      .mockResolvedValueOnce(
        createGuestProfileResponse({
          recentNotes: [
            {
              id: 9,
              body: "To delete",
              authorDisplayName: "Notes Owner",
              createdAt: "2026-07-22T12:00:00Z",
            },
          ],
        })
      )
      .mockResolvedValueOnce(
        createGuestProfileResponse({
          recentNotes: [],
        })
      )
    const getGuestActivity = vi.fn(async () => ({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    }))
    const { adapters } = createAdapters({
      listGuestNotes,
      softDeleteGuestNote,
      getGuestProfile,
      getGuestActivity,
    })
    const pageModule = createOperatorGuestProfilePageModule(adapters)

    await pageModule.syncWorkspace({ guestId: 42, selectedLocationId: 1 })
    await pageModule.activityTab.syncWorkspace({
      guestId: 42,
      selectedLocationId: 1,
      active: true,
    })
    await pageModule.ensureNotesLoaded()
    getGuestProfile.mockClear()
    getGuestActivity.mockClear()

    const deleted = await pageModule.softDeleteNote(9)

    expect(deleted).toBe(true)
    expect(softDeleteGuestNote).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
      noteId: 9,
    })
    expect(listGuestNotes).toHaveBeenCalledTimes(2)
    expect(getGuestProfile).toHaveBeenCalledTimes(1)
    expect(getGuestActivity).toHaveBeenCalledTimes(1)
    expect(pageModule.getSnapshot().notes.items).toHaveLength(0)
    expect(pageModule.getSnapshot().viewModel?.recentNotes).toHaveLength(0)
  })
})

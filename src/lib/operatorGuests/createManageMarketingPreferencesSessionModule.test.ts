import { describe, expect, it, vi } from "vitest"

import {
  createManageMarketingPreferencesSessionModule,
  type ManageMarketingPreferencesAdapters,
} from "./createManageMarketingPreferencesSessionModule"
import { MANAGE_MARKETING_PREFERENCES_COPY } from "./manageMarketingPreferencesPresentation"
import type { GuestProfileResponse } from "@/types/dashboard"

function sampleProfile(
  overrides: Partial<GuestProfileResponse> = {}
): GuestProfileResponse {
  return {
    success: true,
    locationId: 1,
    id: 42,
    name: "Amelia Hart",
    marketingStatus: "Eligible — Email",
    marketingPreference: "allowed",
    guestSinceAt: "2026-05-12T10:00:00.000Z",
    lastActivityAt: "2026-07-20T14:22:00.000Z",
    lastInteractionLabel: "Feedback submitted",
    profileSummary: {
      email: "amelia@example.com",
      mobile: null,
      firstCapturedAt: "2026-05-12T10:00:00.000Z",
      locationName: "Camden Street",
      feedbackSubmissionCount: 2,
      offerClaimsAndRedemptions: 0,
      lastInteractionAt: "2026-07-20T14:22:00.000Z",
      lastInteractionLabel: "Feedback submitted",
      guestTags: [],
    },
    overviewDetails: {
      guestSinceAt: "2026-05-12T10:00:00.000Z",
      totalInteractions: 2,
      feedbackReceived: 2,
      offersClaimed: 0,
      campaignsSent: 0,
      lastActivityAt: "2026-07-20T14:22:00.000Z",
    },
    contactEligibility: [
      {
        channel: "email",
        status: "eligible",
        detailKind: "consent_captured",
        detailAt: "2026-08-06T10:00:00.000Z",
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
  overrides: Partial<ManageMarketingPreferencesAdapters> = {}
): ManageMarketingPreferencesAdapters & {
  getGuestProfile: ReturnType<typeof vi.fn>
  patchMarketingPreference: ReturnType<typeof vi.fn>
} {
  const getGuestProfile =
    overrides.getGuestProfile ?? vi.fn(async () => sampleProfile())
  const patchMarketingPreference =
    overrides.patchMarketingPreference ??
    vi.fn(async () => ({
      success: true,
      preference: "opted_out" as const,
      preferenceChanged: true,
      noteCreated: false,
      noteError: null,
    }))
  return {
    getGuestProfile: getGuestProfile as ManageMarketingPreferencesAdapters["getGuestProfile"] &
      ReturnType<typeof vi.fn>,
    patchMarketingPreference: patchMarketingPreference as ManageMarketingPreferencesAdapters["patchMarketingPreference"] &
      ReturnType<typeof vi.fn>,
  }
}

describe("createManageMarketingPreferencesSessionModule", () => {
  it("opens from a loaded profile with Available Email, Unavailable SMS, and Save disabled", () => {
    const session = createManageMarketingPreferencesSessionModule(
      createAdapters()
    )

    session.openFromLoaded(sampleProfile())
    const snap = session.getSnapshot()

    expect(snap.isOpen).toBe(true)
    expect(snap.loadStatus).toBe("loaded")
    expect(snap.guestName).toBe("Amelia Hart")
    expect(snap.subtitle).toBe(
      MANAGE_MARKETING_PREFERENCES_COPY.subtitle("Amelia Hart")
    )
    expect(snap.currentPreference).toBe("allowed")
    expect(snap.draftPreference).toBe("allowed")
    expect(snap.emailAvailable).toBe(true)
    expect(snap.smsAvailable).toBe(false)
    expect(snap.permissionSourceDisplay).toBe("Guest feedback form")
    expect(snap.recordedOnDisplay).toBe("06/08/2026")
    expect(snap.consequenceHelper).toBe(null)
    expect(snap.canSave).toBe(false)
    expect(
      snap.statusCards.find((card) => card.id === "allowed")
    ).toMatchObject({ selected: true, disabled: false })
  })

  it("keeps Available channel cards in lockstep and ignores an illegal Allowed selection", () => {
    const session = createManageMarketingPreferencesSessionModule(
      createAdapters()
    )
    session.openFromLoaded(
      sampleProfile({
        marketingPreference: "opted_out",
        profileSummary: {
          ...sampleProfile().profileSummary,
          email: "amelia@example.com",
          mobile: "+447700900123",
        },
      })
    )

    expect(
      session.getSnapshot().statusCards.find((card) => card.id === "allowed")
    ).toMatchObject({ disabled: true })

    session.setDraftPreference("allowed")
    expect(session.getSnapshot().draftPreference).toBe("opted_out")

    session.setDraftPreference("not_recorded")
    const snap = session.getSnapshot()
    expect(snap.draftPreference).toBe("not_recorded")
    expect(snap.emailAvailable).toBe(true)
    expect(snap.smsAvailable).toBe(true)
    expect(snap.consequenceHelper).toBe(
      MANAGE_MARKETING_PREFERENCES_COPY.notRecordedConsequence
    )
    expect(snap.canSave).toBe(true)
    expect(snap.permissionSourceDisplay).toBe("Guest feedback form")
    expect(snap.recordedOnDisplay).toBe("06/08/2026")
  })

  it("enables Save for a note-only change and discards the draft on close", () => {
    const session = createManageMarketingPreferencesSessionModule(
      createAdapters()
    )
    session.openFromLoaded(sampleProfile())
    session.setDraftNote(" Guest asked. ")
    expect(session.getSnapshot().canSave).toBe(true)
    expect(session.getSnapshot().draftNote).toBe(" Guest asked. ")

    session.close()
    const snap = session.getSnapshot()
    expect(snap.isOpen).toBe(false)
    expect(snap.draftNote).toBe("")
    expect(snap.draftPreference).toBe(null)
  })

  it("saves preference and optional note, then closes without keeping the draft", async () => {
    const adapters = createAdapters()
    const session = createManageMarketingPreferencesSessionModule(adapters)
    session.openFromLoaded(sampleProfile())
    session.setDraftPreference("opted_out")
    session.setDraftNote("Requested suppression.")

    const result = await session.save()

    expect(result).toEqual({ status: "saved" })
    expect(adapters.patchMarketingPreference).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
      body: {
        preference: "opted_out",
        note: "Requested suppression.",
      },
    })
    expect(session.getSnapshot().isOpen).toBe(false)
  })

  it("omits the note from Save when the draft note is empty", async () => {
    const adapters = createAdapters()
    const session = createManageMarketingPreferencesSessionModule(adapters)
    session.openFromLoaded(sampleProfile())
    session.setDraftPreference("not_recorded")

    await session.save()

    expect(adapters.patchMarketingPreference).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
      body: { preference: "not_recorded" },
    })
  })

  it("keeps the dialog open with the draft when Save fails", async () => {
    const adapters = createAdapters({
      patchMarketingPreference: vi.fn(async () => {
        throw new Error("network")
      }),
    })
    const session = createManageMarketingPreferencesSessionModule(adapters)
    session.openFromLoaded(sampleProfile())
    session.setDraftPreference("opted_out")

    const result = await session.save()
    const snap = session.getSnapshot()

    expect(result).toEqual({
      status: "error",
      message: MANAGE_MARKETING_PREFERENCES_COPY.saveError,
    })
    expect(snap.isOpen).toBe(true)
    expect(snap.draftPreference).toBe("opted_out")
    expect(snap.saveError).toBe(MANAGE_MARKETING_PREFERENCES_COPY.saveError)
    expect(snap.saveStatus).toBe("error")
  })

  it("keeps the preference and reports note failure when the note then fails", async () => {
    const adapters = createAdapters({
      patchMarketingPreference: vi.fn(async () => ({
        success: true,
        preference: "opted_out" as const,
        preferenceChanged: true,
        noteCreated: false,
        noteError: "Could not save the note.",
      })),
    })
    const session = createManageMarketingPreferencesSessionModule(adapters)
    session.openFromLoaded(sampleProfile())
    session.setDraftPreference("opted_out")
    session.setDraftNote("Please suppress.")

    const result = await session.save()

    expect(result).toEqual({
      status: "saved_with_note_error",
      message: "Could not save the note.",
    })
    expect(session.getSnapshot().isOpen).toBe(false)
  })

  it("does not call Save when nothing changed", async () => {
    const adapters = createAdapters()
    const session = createManageMarketingPreferencesSessionModule(adapters)
    session.openFromLoaded(sampleProfile())

    const result = await session.save()

    expect(result).toEqual({ status: "noop" })
    expect(adapters.patchMarketingPreference).not.toHaveBeenCalled()
  })

  it("still reports Save success if the operator closes while Save is in flight", async () => {
    let finishSave: (value: {
      success: boolean
      preference: "opted_out"
      preferenceChanged: boolean
      noteCreated: boolean
      noteError: string | null
    }) => void = () => {}
    const adapters = createAdapters({
      patchMarketingPreference: vi.fn(
        () =>
          new Promise((resolve) => {
            finishSave = resolve
          })
      ),
    })
    const session = createManageMarketingPreferencesSessionModule(adapters)
    session.openFromLoaded(sampleProfile())
    session.setDraftPreference("opted_out")

    const pending = session.save()
    session.close()
    finishSave({
      success: true,
      preference: "opted_out",
      preferenceChanged: true,
      noteCreated: false,
      noteError: null,
    })

    expect(await pending).toEqual({ status: "saved" })
    expect(session.getSnapshot().isOpen).toBe(false)
  })

  it("loads Guest Profile data when opened from the list", async () => {
    const adapters = createAdapters({
      getGuestProfile: vi.fn(async () => sampleProfile()),
    })
    const session = createManageMarketingPreferencesSessionModule(adapters)

    const pending = session.openFromList({ guestId: 42, locationId: 1 })
    expect(session.getSnapshot().loadStatus).toBe("loading")
    expect(session.getSnapshot().isOpen).toBe(true)

    await pending

    expect(adapters.getGuestProfile).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 1,
    })
    expect(session.getSnapshot().loadStatus).toBe("loaded")
    expect(session.getSnapshot().draftPreference).toBe("allowed")
  })

  it("shows an error in the dialog when list open load fails", async () => {
    const adapters = createAdapters({
      getGuestProfile: vi.fn(async () => {
        throw new Error("missing")
      }),
    })
    const session = createManageMarketingPreferencesSessionModule(adapters)

    await session.openFromList({ guestId: 42, locationId: 1 })
    const snap = session.getSnapshot()

    expect(snap.isOpen).toBe(true)
    expect(snap.loadStatus).toBe("error")
    expect(snap.loadError).toBe(MANAGE_MARKETING_PREFERENCES_COPY.loadError)
  })
})

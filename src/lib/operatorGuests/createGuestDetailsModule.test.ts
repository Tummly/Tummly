import { describe, expect, it, vi } from "vitest"

import {
  createGuestDetailsModule,
  createInMemoryGuestDetailsAdapters,
  type GuestDetailsAdapters,
} from "./createGuestDetailsModule"
import type { GuestProfileResponse } from "@/types/dashboard"

const NOW = Date.parse("2026-07-14T12:00:00.000Z")

function sampleProfile(
  overrides: Partial<GuestProfileResponse> = {}
): GuestProfileResponse {
  return {
    success: true,
    locationId: 7,
    id: 42,
    name: "Mohamed",
    marketingStatus: "Eligible — Email",
    marketingPreference: "allowed",
    guestSinceAt: "2026-05-12T10:00:00.000Z",
    lastActivityAt: "2026-07-14T11:48:00.000Z",
    lastInteractionLabel: "Feedback submitted",
    profileSummary: {
      email: "mohamed@email.com",
      mobile: null,
      firstCapturedAt: "2026-05-12T10:00:00.000Z",
      locationName: "Camden",
      feedbackSubmissionCount: 2,
      offerClaimsAndRedemptions: 1,
      lastInteractionAt: "2026-07-14T11:48:00.000Z",
      lastInteractionLabel: "Feedback submitted",
      guestTags: [{ id: 10, name: "Regular" }],
    },
    overviewDetails: {
      guestSinceAt: "2026-05-12T10:00:00.000Z",
      totalInteractions: 2,
      feedbackReceived: 2,
      offersClaimed: 1,
      campaignsSent: 0,
      lastActivityAt: "2026-07-14T11:48:00.000Z",
    },
    contactEligibility: [
      {
        channel: "email",
        status: "eligible",
        detailKind: "consent_captured",
        detailAt: "2026-05-12T10:05:00.000Z",
      },
      {
        channel: "sms",
        status: "not_provided",
        detailKind: null,
        detailAt: null,
      },
    ],
    latestFeedback: [
      {
        id: 99,
        createdAt: "2026-07-14T11:48:00.000Z",
        comment: "Food was cold and delivery took too long.",
        locationName: "Camden",
        classificationStatus: "Succeeded",
        sentiment: "negative",
        detectedTags: ["FoodQuality"],
      },
    ],
    recentNotes: [],
    ...overrides,
  }
}

describe("createGuestDetailsModule", () => {
  it("opens Guest details and loads from the profile adapter (not the list row)", async () => {
    const adapters = createInMemoryGuestDetailsAdapters({
      profiles: { "7:42": sampleProfile() },
    })
    const details = createGuestDetailsModule(adapters, { now: () => NOW })

    expect(details.getSnapshot()).toMatchObject({
      isOpen: false,
      loadStatus: "idle",
      guestId: null,
      locationId: null,
      details: null,
      loadError: null,
    })

    const openPromise = details.open({ guestId: 42, locationId: 7 })
    expect(details.getSnapshot()).toMatchObject({
      isOpen: true,
      loadStatus: "loading",
      guestId: 42,
      locationId: 7,
      details: null,
      loadError: null,
    })

    await openPromise

    expect(details.getSnapshot()).toMatchObject({
      isOpen: true,
      loadStatus: "loaded",
      guestId: 42,
      locationId: 7,
      loadError: null,
      details: {
        id: 42,
        locationId: 7,
        name: "Mohamed",
        marketingStatusLabel: "Eligible — Email",
        identitySubtitle:
          "Guest since 12 May 2026 · Last activity 12 minutes ago",
        email: "mohamed@email.com",
        emailDisplay: "mohamed@email.com",
        mobileDisplay: "Not provided",
        emailMarketingLabel: "Opted in",
        smsMarketingLabel: "Not opted in",
        firstCapturedDisplay: "12 May 2026",
        locationName: "Camden",
        feedbackSubmissionCount: 2,
        offerRedemptionsDisplay: "1",
        guestTags: [{ id: "10", name: "Regular" }],
        latestFeedback: {
          id: 99,
          quote: "Food was cold and delivery took too long.",
          sentiment: "negative",
          isNew: true,
        },
        hasOffersOrCampaigns: false,
        recentNotes: [],
        recentActivity: [
          {
            at: "2026-07-14T11:48:00.000Z",
            description:
              "Feedback received and classified as Negative.",
          },
        ],
        consentCapturedDisplay: "12 May 2026, 11:05 AM",
      },
      noteDraft: "",
      noteCreateStatus: "idle",
      noteCreateError: null,
    })
  })

  it("shows Consent captured from guestSinceAt when eligibility detailAt is missing", async () => {
    const adapters = createInMemoryGuestDetailsAdapters({
      profiles: {
        "7:42": sampleProfile({
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
        }),
      },
    })
    const details = createGuestDetailsModule(adapters, { now: () => NOW })

    await details.open({ guestId: 42, locationId: 7 })

    expect(details.getSnapshot().details?.consentCapturedDisplay).toBe(
      "12 May 2026, 11:00 AM"
    )
  })

  it("maps the latest feedback into Recent activity, and leaves it empty when none exists", async () => {
    const withFeedback = createGuestDetailsModule(
      createInMemoryGuestDetailsAdapters({
        profiles: { "7:42": sampleProfile() },
      }),
      { now: () => NOW }
    )
    await withFeedback.open({ guestId: 42, locationId: 7 })
    expect(withFeedback.getSnapshot().details?.recentActivity).toEqual([
      {
        at: "2026-07-14T11:48:00.000Z",
        description: "Feedback received and classified as Negative.",
      },
    ])
    expect(withFeedback.getSnapshot().details?.latestFeedback?.sentiment).toBe(
      "negative"
    )

    const withoutFeedback = createGuestDetailsModule(
      createInMemoryGuestDetailsAdapters({
        profiles: {
          "7:42": sampleProfile({ latestFeedback: [] }),
        },
      }),
      { now: () => NOW }
    )
    await withoutFeedback.open({ guestId: 42, locationId: 7 })
    expect(withoutFeedback.getSnapshot().details?.recentActivity).toEqual([])
    expect(withoutFeedback.getSnapshot().details?.latestFeedback).toBeNull()
  })

  it("keeps the drawer open with a recoverable error when details fail to load", async () => {
    const adapters: GuestDetailsAdapters = {
      getGuestProfile: async () => {
        throw new Error("network")
      },
      createGuestNote: async () => {
        throw new Error("unused")
      },
    }
    const details = createGuestDetailsModule(adapters, { now: () => NOW })

    await details.open({ guestId: 42, locationId: 7 })

    expect(details.getSnapshot()).toMatchObject({
      isOpen: true,
      loadStatus: "error",
      guestId: 42,
      locationId: 7,
      details: null,
      loadError: "Could not load Guest details. Please try again.",
    })
  })

  it("retries a failed load without hydrating from a list row", async () => {
    let attempts = 0
    const adapters: GuestDetailsAdapters = {
      getGuestProfile: async () => {
        attempts += 1
        if (attempts === 1) {
          throw new Error("network")
        }
        return sampleProfile()
      },
      createGuestNote: async () => {
        throw new Error("unused")
      },
    }
    const details = createGuestDetailsModule(adapters, { now: () => NOW })

    await details.open({ guestId: 42, locationId: 7 })
    expect(details.getSnapshot().loadStatus).toBe("error")

    await details.retry()

    expect(details.getSnapshot()).toMatchObject({
      loadStatus: "loaded",
      details: { id: 42, name: "Mohamed" },
      loadError: null,
    })
  })

  it("resets on close so the next open starts clean", async () => {
    const adapters = createInMemoryGuestDetailsAdapters({
      profiles: { "7:42": sampleProfile() },
    })
    const details = createGuestDetailsModule(adapters, { now: () => NOW })

    await details.open({ guestId: 42, locationId: 7 })
    details.setNoteDraft("draft note")
    details.close()

    expect(details.getSnapshot()).toMatchObject({
      isOpen: false,
      loadStatus: "idle",
      guestId: null,
      locationId: null,
      details: null,
      loadError: null,
      noteDraft: "",
      noteCreateStatus: "idle",
      noteCreateError: null,
    })
  })

  it("ignores a stale resolution after close", async () => {
    let resolveLoad!: (value: GuestProfileResponse) => void
    const adapters: GuestDetailsAdapters = {
      getGuestProfile: () =>
        new Promise((resolve) => {
          resolveLoad = resolve
        }),
      createGuestNote: async () => {
        throw new Error("unused")
      },
    }
    const details = createGuestDetailsModule(adapters, { now: () => NOW })

    const openPromise = details.open({ guestId: 42, locationId: 7 })
    details.close()
    resolveLoad(sampleProfile())
    await openPromise

    expect(details.getSnapshot()).toMatchObject({
      isOpen: false,
      loadStatus: "idle",
      details: null,
    })
  })

  it("ignores a stale resolution after a newer open", async () => {
    const resolvers: Array<(value: GuestProfileResponse) => void> = []
    const adapters: GuestDetailsAdapters = {
      getGuestProfile: () =>
        new Promise((resolve) => {
          resolvers.push(resolve)
        }),
      createGuestNote: async () => {
        throw new Error("unused")
      },
    }
    const details = createGuestDetailsModule(adapters, { now: () => NOW })

    const firstOpen = details.open({ guestId: 1, locationId: 7 })
    const secondOpen = details.open({ guestId: 2, locationId: 7 })

    resolvers[0]!(sampleProfile({ id: 1, name: "Stale Guest" }))
    resolvers[1]!(sampleProfile({ id: 2, name: "Fresh Guest" }))
    await Promise.all([firstOpen, secondOpen])

    expect(details.getSnapshot()).toMatchObject({
      isOpen: true,
      loadStatus: "loaded",
      guestId: 2,
      details: { id: 2, name: "Fresh Guest" },
    })
  })

  it("creates a note and prepends it to recent notes on success", async () => {
    const adapters = createInMemoryGuestDetailsAdapters({
      profiles: { "7:42": sampleProfile() },
    })
    const createSpy = vi.spyOn(adapters, "createGuestNote")
    const details = createGuestDetailsModule(adapters, { now: () => NOW })

    await details.open({ guestId: 42, locationId: 7 })
    details.setNoteDraft("Called about cold food")
    await details.createNote()

    expect(createSpy).toHaveBeenCalledWith({
      guestId: 42,
      locationId: 7,
      body: "Called about cold food",
    })
    expect(details.getSnapshot()).toMatchObject({
      noteDraft: "",
      noteCreateStatus: "idle",
      noteCreateError: null,
      details: {
        recentNotes: [
          {
            body: "Called about cold food",
          },
        ],
      },
    })
  })

  it("keeps the draft and surfaces an error when note create fails", async () => {
    const adapters: GuestDetailsAdapters = {
      getGuestProfile: async () => sampleProfile(),
      createGuestNote: async () => {
        throw new Error("network")
      },
    }
    const details = createGuestDetailsModule(adapters, { now: () => NOW })

    await details.open({ guestId: 42, locationId: 7 })
    details.setNoteDraft("Will retry")
    await details.createNote()

    expect(details.getSnapshot()).toMatchObject({
      noteDraft: "Will retry",
      noteCreateStatus: "error",
      noteCreateError: "Could not add note. Please try again.",
    })
  })

  it("maps empty optional sections honestly", async () => {
    const adapters = createInMemoryGuestDetailsAdapters({
      profiles: {
        "7:42": sampleProfile({
          latestFeedback: [],
          profileSummary: {
            ...sampleProfile().profileSummary,
            guestTags: [],
            mobile: null,
            email: null,
            offerClaimsAndRedemptions: 0,
          },
          contactEligibility: [
            {
              channel: "email",
              status: "not_provided",
              detailKind: null,
              detailAt: null,
            },
            {
              channel: "sms",
              status: "not_provided",
              detailKind: null,
              detailAt: null,
            },
          ],
        }),
      },
    })
    const details = createGuestDetailsModule(adapters, { now: () => NOW })

    await details.open({ guestId: 42, locationId: 7 })

    expect(details.getSnapshot().details).toMatchObject({
      email: null,
      emailDisplay: "Not provided",
      mobileDisplay: "Not provided",
      emailMarketingLabel: "Not opted in",
      smsMarketingLabel: "Not opted in",
      guestTags: [],
      latestFeedback: null,
      hasOffersOrCampaigns: false,
      recentActivity: [],
      recentNotes: [],
    })
  })

  it("notifies subscribers when the snapshot changes", async () => {
    const details = createGuestDetailsModule(
      createInMemoryGuestDetailsAdapters({
        profiles: { "7:42": sampleProfile() },
      }),
      { now: () => NOW }
    )
    const listener = vi.fn()
    const unsubscribe = details.subscribe(listener)

    await details.open({ guestId: 42, locationId: 7 })
    expect(listener).toHaveBeenCalled()

    unsubscribe()
    listener.mockClear()
    details.close()
    expect(listener).not.toHaveBeenCalled()
  })
})

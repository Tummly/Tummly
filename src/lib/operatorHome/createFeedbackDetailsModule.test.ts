import { describe, expect, it, vi } from "vitest"

import {
  createFeedbackDetailsModule,
  createInMemoryFeedbackDetailsAdapters,
  type FeedbackDetailsAdapters,
  type FeedbackDetailsResponse,
} from "./createFeedbackDetailsModule"

const NOW = Date.parse("2026-07-14T12:00:00.000Z")

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

describe("createFeedbackDetailsModule", () => {
  it("opens Feedback details and loads live fields via the HTTP adapter", async () => {
    const adapters = createInMemoryFeedbackDetailsAdapters({
      42: sampleDetails,
    })
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    expect(details.getSnapshot()).toMatchObject({
      isOpen: false,
      loadStatus: "idle",
      feedbackId: null,
      details: null,
      loadError: null,
    })

    const openPromise = details.open(42)
    expect(details.getSnapshot()).toMatchObject({
      isOpen: true,
      loadStatus: "loading",
      feedbackId: 42,
      details: null,
      loadError: null,
    })

    await openPromise

    expect(details.getSnapshot()).toMatchObject({
      isOpen: true,
      loadStatus: "loaded",
      feedbackId: 42,
      loadError: null,
      details: {
        id: 42,
        guestName: "Mohamed Mahmoud",
        guestContact: "mohamed@email.com",
        contactType: "Email",
        comment: "Food was cold and delivery took too long.",
        createdAt: "2026-07-14T11:48:00.000Z",
        locationName: "Camden",
        address: "12 High Street",
        venueLine: "Camden · 12 High Street",
        isNew: true,
        classificationStatus: "Pending",
        sentiment: null,
        detectedTags: null,
        canCorrectClassification: false,
        locationGuestId: null,
        canViewGuestProfile: false,
        canAddInternalNote: true,
        internalNotes: [],
        activityHistory: [
          {
            kind: "feedback_received",
            at: "2026-07-14T11:48:00.000Z",
          },
        ],
      },
      correction: {
        isEditing: false,
        draftSentiment: null,
        saveStatus: "idle",
        saveError: null,
        canSave: false,
      },
      noteDraft: "",
      noteCreateStatus: "idle",
      noteCreateError: null,
    })
  })

  it("enables View guest profile when locationGuestId is present", async () => {
    const adapters = createInMemoryFeedbackDetailsAdapters({
      42: {
        ...sampleDetails,
        locationGuestId: 501,
      },
    })
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)

    expect(details.getSnapshot().details).toMatchObject({
      locationGuestId: 501,
      canViewGuestProfile: true,
    })
  })

  it("maps Succeeded classification with detected tags", async () => {
    const adapters = createInMemoryFeedbackDetailsAdapters({
      42: {
        ...sampleDetails,
        classificationStatus: "Succeeded",
        sentiment: "negative",
        detectedTags: ["FoodQuality", "WaitTime"],
      },
    })
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)

    expect(details.getSnapshot().details).toMatchObject({
      classificationStatus: "Succeeded",
      sentiment: "negative",
      detectedTags: [
        { key: "FoodQuality", label: "Food quality" },
        { key: "WaitTime", label: "Wait time" },
      ],
      canCorrectClassification: true,
    })
  })

  it("maps Succeeded with empty tags as a calm success empty set", async () => {
    const adapters = createInMemoryFeedbackDetailsAdapters({
      42: {
        ...sampleDetails,
        classificationStatus: "Succeeded",
        sentiment: "positive",
        detectedTags: [],
      },
    })
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)

    expect(details.getSnapshot().details).toMatchObject({
      classificationStatus: "Succeeded",
      sentiment: "positive",
      detectedTags: [],
    })
  })

  it("maps Failed without inventing sentiment or tags", async () => {
    const adapters = createInMemoryFeedbackDetailsAdapters({
      42: {
        ...sampleDetails,
        classificationStatus: "Failed",
        sentiment: null,
        detectedTags: null,
      },
    })
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)

    expect(details.getSnapshot().details).toMatchObject({
      classificationStatus: "Failed",
      sentiment: null,
      detectedTags: null,
    })
  })

  it("omits the venue separator when address is empty", async () => {
    const adapters = createInMemoryFeedbackDetailsAdapters({
      7: { ...sampleDetails, id: 7, address: "  " },
    })
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(7)

    expect(details.getSnapshot().details?.venueLine).toBe("Camden")
  })

  it("marks New false when CreatedAt is outside the rolling 24 hours", async () => {
    const adapters = createInMemoryFeedbackDetailsAdapters({
      8: {
        ...sampleDetails,
        id: 8,
        createdAt: "2026-07-12T11:00:00.000Z",
      },
    })
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(8)

    expect(details.getSnapshot().details?.isNew).toBe(false)
  })

  it("enables correction only when classification Succeeded", async () => {
    const adapters = createInMemoryFeedbackDetailsAdapters({
      42: {
        ...sampleDetails,
        classificationStatus: "Succeeded",
        sentiment: "negative",
        detectedTags: [],
      },
    })
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)
    details.startCorrection()

    expect(details.getSnapshot().correction).toMatchObject({
      isEditing: true,
      draftSentiment: "negative",
      saveStatus: "idle",
      saveError: null,
      canSave: false,
    })

    details.setDraftSentiment("positive")
    expect(details.getSnapshot().correction).toMatchObject({
      draftSentiment: "positive",
      canSave: true,
    })

    details.cancelCorrection()
    expect(details.getSnapshot()).toMatchObject({
      details: { sentiment: "negative" },
      correction: {
        isEditing: false,
        draftSentiment: null,
        canSave: false,
      },
    })
  })

  it("saves a changed sentiment and exits edit mode", async () => {
    const adapters = createInMemoryFeedbackDetailsAdapters({
      42: {
        ...sampleDetails,
        classificationStatus: "Succeeded",
        sentiment: "negative",
        detectedTags: ["FoodQuality"],
      },
    })
    const correctSpy = vi.spyOn(adapters, "correctClassification")
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)
    details.startCorrection()
    details.setDraftSentiment("neutral")
    await details.saveCorrection()

    expect(correctSpy).toHaveBeenCalledWith(42, "neutral")
    expect(details.getSnapshot()).toMatchObject({
      details: {
        sentiment: "neutral",
        detectedTags: [{ key: "FoodQuality", label: "Food quality" }],
        activityHistory: [
          { kind: "feedback_received", at: "2026-07-14T11:48:00.000Z" },
          {
            kind: "classification_corrected",
            actorDisplayName: "Ada Operator",
            fromSentiment: "negative",
            toSentiment: "neutral",
          },
        ],
      },
      correction: {
        isEditing: false,
        draftSentiment: null,
        saveStatus: "idle",
        saveError: null,
        canSave: false,
      },
    })
  })

  it("keeps classification corrections in activity history after reopen", async () => {
    const adapters = createInMemoryFeedbackDetailsAdapters({
      42: {
        ...sampleDetails,
        classificationStatus: "Succeeded",
        sentiment: "negative",
        detectedTags: ["FoodQuality"],
      },
    })
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)
    details.startCorrection()
    details.setDraftSentiment("neutral")
    await details.saveCorrection()
    details.close()
    await details.open(42)

    expect(details.getSnapshot().details?.activityHistory).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          kind: "classification_corrected",
          actorDisplayName: "Ada Operator",
          fromSentiment: "negative",
          toSentiment: "neutral",
        }),
      ])
    )
  })

  it("stays in edit mode with the draft when save fails", async () => {
    const adapters: FeedbackDetailsAdapters = {
      getFeedbackDetails: async () => ({
        ...sampleDetails,
        classificationStatus: "Succeeded",
        sentiment: "negative",
        detectedTags: [],
      }),
      correctClassification: async () => {
        throw new Error("network")
      },
      createInternalNote: async () => {
        throw new Error("unused")
      },
    }
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)
    details.startCorrection()
    details.setDraftSentiment("positive")
    await details.saveCorrection()

    expect(details.getSnapshot()).toMatchObject({
      details: { sentiment: "negative" },
      correction: {
        isEditing: true,
        draftSentiment: "positive",
        saveStatus: "error",
        saveError: "Could not save classification. Please try again.",
        canSave: true,
      },
    })
  })

  it("does not enter correction when classification is not Succeeded", async () => {
    const adapters = createInMemoryFeedbackDetailsAdapters({
      42: sampleDetails,
    })
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)
    details.startCorrection()

    expect(details.getSnapshot().correction.isEditing).toBe(false)
    expect(details.getSnapshot().details?.canCorrectClassification).toBe(false)
  })

  it("keeps the drawer open with a recoverable error when details fail to load", async () => {
    const adapters: FeedbackDetailsAdapters = {
      getFeedbackDetails: async () => {
        throw new Error("network")
      },
      correctClassification: async () => {
        throw new Error("unused")
      },
      createInternalNote: async () => {
        throw new Error("unused")
      },
    }
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)

    expect(details.getSnapshot()).toMatchObject({
      isOpen: true,
      loadStatus: "error",
      feedbackId: 42,
      details: null,
      loadError: "Could not load Feedback details. Please try again.",
    })
  })

  it("retries a failed load without hydrating from a list row", async () => {
    let attempts = 0
    const adapters: FeedbackDetailsAdapters = {
      getFeedbackDetails: async (feedbackId) => {
        attempts += 1
        if (attempts === 1) {
          throw new Error("network")
        }
        return { ...sampleDetails, id: feedbackId }
      },
      correctClassification: async () => {
        throw new Error("unused")
      },
      createInternalNote: async () => {
        throw new Error("unused")
      },
    }
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)
    expect(details.getSnapshot().loadStatus).toBe("error")

    await details.retry()

    expect(details.getSnapshot()).toMatchObject({
      loadStatus: "loaded",
      details: { id: 42, guestName: "Mohamed Mahmoud" },
      loadError: null,
    })
  })

  it("resets on close so the next open starts clean", async () => {
    const adapters = createInMemoryFeedbackDetailsAdapters({
      42: sampleDetails,
    })
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)
    details.close()

    expect(details.getSnapshot()).toMatchObject({
      isOpen: false,
      loadStatus: "idle",
      feedbackId: null,
      details: null,
      loadError: null,
    })
  })

  it("ignores a stale resolution after close", async () => {
    let resolveLoad!: (value: FeedbackDetailsResponse) => void
    const adapters: FeedbackDetailsAdapters = {
      getFeedbackDetails: () =>
        new Promise((resolve) => {
          resolveLoad = resolve
        }),
      correctClassification: async () => {
        throw new Error("unused")
      },
      createInternalNote: async () => {
        throw new Error("unused")
      },
    }
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    const openPromise = details.open(42)
    details.close()
    resolveLoad(sampleDetails)
    await openPromise

    expect(details.getSnapshot()).toMatchObject({
      isOpen: false,
      loadStatus: "idle",
      details: null,
    })
  })

  it("ignores a stale resolution after a newer open", async () => {
    const resolvers: Array<(value: FeedbackDetailsResponse) => void> = []
    const adapters: FeedbackDetailsAdapters = {
      getFeedbackDetails: () =>
        new Promise((resolve) => {
          resolvers.push(resolve)
        }),
      correctClassification: async () => {
        throw new Error("unused")
      },
      createInternalNote: async () => {
        throw new Error("unused")
      },
    }
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    const firstOpen = details.open(1)
    const secondOpen = details.open(2)

    resolvers[0]!({ ...sampleDetails, id: 1, guestName: "Stale Guest" })
    resolvers[1]!({ ...sampleDetails, id: 2, guestName: "Fresh Guest" })
    await Promise.all([firstOpen, secondOpen])

    expect(details.getSnapshot()).toMatchObject({
      isOpen: true,
      loadStatus: "loaded",
      feedbackId: 2,
      details: { id: 2, guestName: "Fresh Guest" },
    })
  })

  it("notifies subscribers when the snapshot changes", async () => {
    const details = createFeedbackDetailsModule(
      createInMemoryFeedbackDetailsAdapters({ 42: sampleDetails }),
      { now: () => NOW }
    )
    const listener = vi.fn()
    const unsubscribe = details.subscribe(listener)

    await details.open(42)
    expect(listener).toHaveBeenCalled()

    unsubscribe()
    listener.mockClear()
    details.close()
    expect(listener).not.toHaveBeenCalled()
  })

  it("loads internal notes and derived note_added activity from details", async () => {
    const adapters = createInMemoryFeedbackDetailsAdapters({
      42: {
        ...sampleDetails,
        internalNotes: [
          {
            id: 2,
            body: "Newer note",
            authorDisplayName: "Ada",
            createdAt: "2026-07-14T11:55:00.000Z",
          },
          {
            id: 1,
            body: "Older note",
            authorDisplayName: "Ada",
            createdAt: "2026-07-14T11:50:00.000Z",
          },
        ],
        activityHistory: [
          {
            kind: "feedback_received",
            at: "2026-07-14T11:48:00.000Z",
          },
          {
            kind: "note_added",
            at: "2026-07-14T11:50:00.000Z",
          },
          {
            kind: "note_added",
            at: "2026-07-14T11:55:00.000Z",
          },
        ],
      },
    })
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)

    expect(details.getSnapshot().details).toMatchObject({
      canAddInternalNote: true,
      internalNotes: [
        {
          id: 2,
          body: "Newer note",
          authorDisplayName: "Ada",
          createdAt: "2026-07-14T11:55:00.000Z",
        },
        {
          id: 1,
          body: "Older note",
          authorDisplayName: "Ada",
          createdAt: "2026-07-14T11:50:00.000Z",
        },
      ],
      activityHistory: [
        { kind: "feedback_received", at: "2026-07-14T11:48:00.000Z" },
        { kind: "note_added", at: "2026-07-14T11:50:00.000Z" },
        { kind: "note_added", at: "2026-07-14T11:55:00.000Z" },
      ],
    })
  })

  it("creates an internal note, prepends it, clears draft, and appends activity", async () => {
    const adapters = createInMemoryFeedbackDetailsAdapters({
      42: sampleDetails,
    })
    const createSpy = vi.spyOn(adapters, "createInternalNote")
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)
    details.setNoteDraft("  Called the kitchen  ")
    expect(details.getSnapshot().noteDraft).toBe("  Called the kitchen  ")

    const createPromise = details.createNote()
    expect(details.getSnapshot().noteCreateStatus).toBe("saving")
    const ok = await createPromise

    expect(ok).toBe(true)
    expect(createSpy).toHaveBeenCalledWith(42, "Called the kitchen")
    expect(details.getSnapshot()).toMatchObject({
      noteDraft: "",
      noteCreateStatus: "idle",
      noteCreateError: null,
      details: {
        internalNotes: [
          {
            body: "Called the kitchen",
            authorDisplayName: "Ada Operator",
          },
        ],
        activityHistory: [
          { kind: "feedback_received", at: "2026-07-14T11:48:00.000Z" },
          {
            kind: "note_added",
            actorDisplayName: "Ada Operator",
          },
        ],
      },
    })
  })

  it("keeps the draft and surfaces an error when note create fails", async () => {
    const adapters: FeedbackDetailsAdapters = {
      getFeedbackDetails: async () => sampleDetails,
      correctClassification: async () => {
        throw new Error("unused")
      },
      createInternalNote: async () => {
        throw new Error("network")
      },
    }
    const details = createFeedbackDetailsModule(adapters, { now: () => NOW })

    await details.open(42)
    details.setNoteDraft("Will retry")
    await details.createNote()

    expect(details.getSnapshot()).toMatchObject({
      noteDraft: "Will retry",
      noteCreateStatus: "error",
      noteCreateError: "Could not add note. Please try again.",
      details: { internalNotes: [] },
    })
  })
})

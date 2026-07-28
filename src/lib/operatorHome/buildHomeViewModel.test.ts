import { describe, expect, it } from "vitest"

import {
  buildOperatorHomeViewModel,
  resolveInitialLocationId,
} from "./buildHomeViewModel"
import type { FeedbackItem, LocationItem } from "@/types/dashboard"

const locations: LocationItem[] = [
  {
    id: 2,
    locationName: "Later Venue",
    address: "2 High St",
    guestUrl: "https://guest.example/later",
    createdAt: "2026-02-01T00:00:00.000Z",
  },
  {
    id: 1,
    locationName: "First Venue",
    address: "1 High St",
    guestUrl: "https://guest.example/first",
    createdAt: "2026-01-01T00:00:00.000Z",
  },
]

const recentFeedback: FeedbackItem[] = [
  {
    id: 10,
    guestName: "Alex Guest",
    guestContact: "alex@example.com",
    contactType: "Email",
    comment: "Great service.",
    createdAt: "2026-07-12T10:00:00.000Z",
    classificationStatus: "Pending",
    sentiment: null,
    detectedTags: null,
  },
  {
    id: 11,
    guestName: "Sam Guest",
    guestContact: "+441234",
    contactType: "Phone",
    comment: "Food was cold.",
    createdAt: "2026-07-12T11:00:00.000Z",
    classificationStatus: "Pending",
    sentiment: null,
    detectedTags: null,
  },
]

function asLatestFeedbackItems(items: FeedbackItem[]) {
  return items.map((item) => ({
    kind: "feedback" as const,
    locationGuestId: null,
    ...item,
  }))
}

describe("resolveInitialLocationId", () => {
  it("prefers a persisted or query Owned location when it is owned", () => {
    expect(resolveInitialLocationId(locations, 2)).toBe(2)
  })

  it("falls back to the earliest created Owned location", () => {
    expect(resolveInitialLocationId(locations, 99)).toBe(1)
    expect(resolveInitialLocationId(locations, null)).toBe(1)
  })
})

describe("buildOperatorHomeViewModel", () => {
  it("scopes Home body to the selected Owned location core fields", () => {
    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 2,
      feedback: { total: 0, recent: [] },
    })

    expect(viewModel).toMatchObject({
      selectedLocationId: 2,
      selectedLocationName: "Later Venue",
      smartGuestLink: "https://guest.example/later",
      canCopySmartGuestLink: true,
      canPreviewGuestForm: true,
      dateRangeLabel: "Last 7 days",
    })
    expect(viewModel).not.toHaveProperty("needsAttention")
    expect(viewModel).not.toHaveProperty("liveOffersAndCampaigns")
    expect(viewModel).not.toHaveProperty("recommendedAction")
    expect(viewModel).not.toHaveProperty("weeklyBrief")
    expect(viewModel).not.toHaveProperty("canDownloadQr")
    expect(viewModel).not.toHaveProperty("canExport")
    expect(viewModel).not.toHaveProperty("canCreateCampaign")
    expect(viewModel).not.toHaveProperty("locations")
    expect(viewModel).not.toHaveProperty("operatorDisplayName")
    expect(viewModel).not.toHaveProperty("activationExpiresAt")
    expect(viewModel).not.toHaveProperty("locationSwitcherInteractive")
    expect(viewModel).not.toHaveProperty("activeNavId")
    expect(viewModel).not.toHaveProperty("pageTitle")
  })

  it("disables Preview and Copy when the selected location has no Smart Guest Link", () => {
    const withoutGuestUrl: LocationItem[] = [
      {
        ...locations[0],
        guestUrl: "",
      },
    ]

    expect(
      buildOperatorHomeViewModel({
        locations: withoutGuestUrl,
        selectedLocationId: 2,
      })
    ).toMatchObject({
      selectedLocationId: 2,
      smartGuestLink: null,
      canCopySmartGuestLink: false,
      canPreviewGuestForm: false,
    })
  })

  it("updates Smart Guest Link targets when the selected Owned location changes", () => {
    const first = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
    })
    const second = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 2,
    })

    expect(first).toMatchObject({
      selectedLocationId: 1,
      selectedLocationName: "First Venue",
      smartGuestLink: "https://guest.example/first",
      canCopySmartGuestLink: true,
      canPreviewGuestForm: true,
    })
    expect(second).toMatchObject({
      selectedLocationId: 2,
      selectedLocationName: "Later Venue",
      smartGuestLink: "https://guest.example/later",
      canCopySmartGuestLink: true,
      canPreviewGuestForm: true,
    })
  })

  it("builds seven Finish-setting-up steps with complete/partial/incomplete from acks and feedback", () => {
    const withoutPreview = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: { total: 0, recent: [] },
      checklistAcks: {
        guestFormPreviewed: false,
        qrPlacementGuideViewed: false,
        logoUploaded: false,
      },
    })

    expect(withoutPreview?.canPreviewGuestForm).toBe(true)
    expect(
      withoutPreview?.setupSteps.map((step) => [step.id, step.status])
    ).toEqual([
      ["account-ready", "complete"],
      ["upload-logo", "partial"],
      ["guest-form", "partial"],
      ["first-response", "partial"],
      ["qr-placement", "incomplete"],
      ["first-offer", "incomplete"],
      ["first-campaign", "incomplete"],
    ])
    expect(withoutPreview?.setupSteps[1]?.actions).toEqual([
      { id: "upload-logo", label: "Upload logo", available: false },
    ])
    expect(withoutPreview?.setupSteps[2]?.actions).toEqual([
      { id: "preview-guest-form", label: "Preview form", available: true },
    ])
    expect(withoutPreview?.setupSteps[3]?.actions).toEqual([
      { id: "preview-guest-form", label: "Preview form", available: true },
    ])
    expect(withoutPreview?.setupSteps[4]?.actions).toEqual([
      {
        id: "view-placement-guide",
        label: "View placement guide",
        available: false,
      },
      {
        id: "order-qr-materials",
        label: "Order QR materials",
        available: false,
      },
    ])
    expect(withoutPreview?.setupSteps[5]?.actions).toEqual([
      { id: "create-offer", label: "Create offer", available: false },
    ])
    expect(withoutPreview?.setupSteps[6]?.actions).toEqual([
      { id: "create-campaign", label: "Create campaign", available: false },
    ])

    const afterPreviewAndFeedback = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: { total: 3, recent: recentFeedback },
      checklistAcks: {
        guestFormPreviewed: true,
        qrPlacementGuideViewed: false,
        logoUploaded: false,
      },
    })

    expect(
      afterPreviewAndFeedback?.setupSteps.map((step) => [step.id, step.status])
    ).toEqual([
      ["account-ready", "complete"],
      ["upload-logo", "partial"],
      ["guest-form", "complete"],
      ["first-response", "complete"],
      ["qr-placement", "incomplete"],
      ["first-offer", "incomplete"],
      ["first-campaign", "incomplete"],
    ])
  })

  it("counts only complete steps for setup progress and marks logo complete when acknowledged", () => {
    const fresh = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: { total: 0, recent: [] },
      checklistAcks: {
        guestFormPreviewed: false,
        qrPlacementGuideViewed: false,
        logoUploaded: false,
      },
    })

    expect(fresh?.setupSteps).toHaveLength(7)
    expect(
      fresh?.setupSteps.filter((step) => step.status === "complete")
    ).toHaveLength(1)

    const withLogo = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: { total: 0, recent: [] },
      checklistAcks: {
        guestFormPreviewed: false,
        qrPlacementGuideViewed: false,
        logoUploaded: true,
      },
    })

    expect(
      withLogo?.setupSteps.find((step) => step.id === "upload-logo")?.status
    ).toBe("complete")
  })

  it("maps real feedback into Feedback submitted KPI and All/Feedback activity newest-first", () => {
    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: { total: 42, recent: recentFeedback },
      latestActivity: asLatestFeedbackItems(recentFeedback),
      feedbackSubmitted: 42,
      feedbackSubmittedPrevious: 21,
      guestsJoined: 0,
      guestsJoinedPrevious: 0,
      qrScans: 10,
      qrScansPrevious: 5,
    })

    expect(viewModel?.kpis).toEqual([
      {
        id: "qr-scans",
        label: "QR scans",
        value: 10,
        trendPercent: 100,
        hasRealData: true,
      },
      {
        id: "feedback",
        label: "Feedback submitted",
        value: 42,
        trendPercent: 100,
        hasRealData: true,
      },
      {
        id: "guests-joined",
        label: "Guests joined",
        value: 0,
        trendPercent: null,
        hasRealData: true,
      },
      {
        id: "offer-redemptions",
        label: "Offer redemptions",
        value: 0,
        trendPercent: null,
        hasRealData: false,
      },
    ])

    expect(viewModel?.activityByTab.feedback).toEqual([
      {
        id: "feedback-11",
        kind: "feedback",
        feedbackId: 11,
        locationGuestId: null,
        comment: "Food was cold.",
        guestName: "Sam Guest",
        createdAt: "2026-07-12T11:00:00.000Z",
        sentiment: null,
        canViewFeedback: true,
        canViewGuest: false,
      },
      {
        id: "feedback-10",
        kind: "feedback",
        feedbackId: 10,
        locationGuestId: null,
        comment: "Great service.",
        guestName: "Alex Guest",
        createdAt: "2026-07-12T10:00:00.000Z",
        sentiment: null,
        canViewFeedback: true,
        canViewGuest: false,
      },
    ])
    expect(viewModel?.activityByTab.all).toEqual(
      viewModel?.activityByTab.feedback
    )
    expect(viewModel?.activityByTab.guests).toEqual([])
    expect(viewModel?.activityByTab.offers).toEqual([])
    expect(viewModel?.activityByTab.campaigns).toEqual([])
  })

  it("passes sentiment badges only when classification Succeeded", () => {
    const latestItems = [
      {
        id: 1,
        guestName: "A",
        guestContact: "a@example.com",
        contactType: "Email" as const,
        comment: "Cold food",
        createdAt: "2026-07-12T12:00:00.000Z",
        classificationStatus: "Succeeded" as const,
        sentiment: "negative" as const,
        detectedTags: ["FoodQuality"],
      },
      {
        id: 2,
        guestName: "B",
        guestContact: "b@example.com",
        contactType: "Email" as const,
        comment: "Pending note",
        createdAt: "2026-07-12T11:00:00.000Z",
        classificationStatus: "Pending" as const,
        sentiment: null,
        detectedTags: null,
      },
      {
        id: 3,
        guestName: "C",
        guestContact: "c@example.com",
        contactType: "Email" as const,
        comment: "Failed note",
        createdAt: "2026-07-12T10:00:00.000Z",
        classificationStatus: "Failed" as const,
        sentiment: null,
        detectedTags: null,
      },
    ]

    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: {
        total: 3,
        recent: latestItems,
      },
      latestActivity: asLatestFeedbackItems(latestItems),
    })

    expect(viewModel?.activityByTab.feedback).toEqual([
      expect.objectContaining({ feedbackId: 1, sentiment: "negative" }),
      expect.objectContaining({ feedbackId: 2, sentiment: null }),
      expect.objectContaining({ feedbackId: 3, sentiment: null }),
    ])
  })

  it("sets Guests joined KPI hasRealData true when guestsJoined is loaded including zero", () => {
    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: { total: 0, recent: [] },
      guestsJoined: 0,
    })

    expect(
      viewModel?.kpis.find((kpi) => kpi.id === "guests-joined")
    ).toMatchObject({
      value: 0,
      hasRealData: true,
    })
  })

  it("maps a non-zero Guests joined count with hasRealData true", () => {
    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: { total: 0, recent: [] },
      guestsJoined: 8,
      guestsJoinedPrevious: 4,
    })

    expect(
      viewModel?.kpis.find((kpi) => kpi.id === "guests-joined")
    ).toMatchObject({
      value: 8,
      trendPercent: 100,
      hasRealData: true,
    })
  })

  it("keeps KPI trends null when previous period counts are not loaded", () => {
    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedbackSubmitted: 10,
      guestsJoined: 4,
    })

    expect(viewModel?.kpis.find((kpi) => kpi.id === "feedback")?.trendPercent).toBe(
      null
    )
    expect(
      viewModel?.kpis.find((kpi) => kpi.id === "guests-joined")?.trendPercent
    ).toBe(null)
  })

  it("treats zero previous period with activity as +100% trend", () => {
    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedbackSubmitted: 5,
      feedbackSubmittedPrevious: 0,
      guestsJoined: 0,
      guestsJoinedPrevious: 0,
    })

    expect(viewModel?.kpis.find((kpi) => kpi.id === "feedback")?.trendPercent).toBe(
      100
    )
    expect(
      viewModel?.kpis.find((kpi) => kpi.id === "guests-joined")?.trendPercent
    ).toBe(null)
  })

  it("keeps Guests joined KPI stubbed when guestsJoined is not loaded", () => {
    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: { total: 0, recent: [] },
    })

    expect(
      viewModel?.kpis.find((kpi) => kpi.id === "guests-joined")
    ).toMatchObject({
      value: 0,
      hasRealData: false,
    })
    expect(
      viewModel?.kpis.find((kpi) => kpi.id === "qr-scans")
    ).toMatchObject({
      value: 0,
      hasRealData: false,
    })
  })

  it("maps QR scans into the Performance KPI with period-over-period trend", () => {
    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      qrScans: 12,
      qrScansPrevious: 10,
    })

    expect(viewModel?.kpis.find((kpi) => kpi.id === "qr-scans")).toEqual({
      id: "qr-scans",
      label: "QR scans",
      value: 12,
      trendPercent: 20,
      hasRealData: true,
    })
  })

  it("keeps activity empty copy and honest KPI/activity defaults without static section shells", () => {
    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: { total: 0, recent: [] },
      latestActivity: [],
    })

    expect(viewModel?.kpis.every((kpi) => kpi.trendPercent === null)).toBe(
      true
    )
    expect(viewModel?.activityByTab.feedback).toEqual([])
    expect(viewModel?.activityEmpty).toEqual({
      emptyCopy: "No activity yet",
      emptyHelper:
        "Feedback, guest sign-ups, offer activity and campaign events will appear here.",
    })
    expect(viewModel?.dateRangeLabel).toBe("Last 7 days")
  })

  it("routes guest-joined items to Guests tab and merges All by time", () => {
    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: { total: 1, recent: recentFeedback.slice(0, 1) },
      latestActivity: [
        {
          kind: "guest-joined",
          locationGuestId: 501,
          guestName: "Jordan Guest",
          offersOptOut: false,
          createdAt: "2026-07-13T09:00:00.000Z",
        },
        ...asLatestFeedbackItems(recentFeedback.slice(0, 1)),
      ],
    })

    expect(viewModel?.activityByTab.guests).toEqual([
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
    expect(viewModel?.activityByTab.feedback).toEqual([
      expect.objectContaining({
        kind: "feedback",
        feedbackId: 10,
      }),
    ])
    expect(viewModel?.activityByTab.all).toEqual([
      expect.objectContaining({
        kind: "guest-joined",
        locationGuestId: 501,
        consentLabel: "Opted in",
      }),
      expect.objectContaining({
        kind: "feedback",
        feedbackId: 10,
      }),
    ])
  })

  it("maps guest-joined opt-out to Opted out consent label", () => {
    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      latestActivity: [
        {
          kind: "guest-joined",
          locationGuestId: 502,
          guestName: "Mohamed Mahmoud",
          offersOptOut: true,
          createdAt: "2026-07-13T09:00:00.000Z",
        },
      ],
    })

    expect(viewModel?.activityByTab.guests[0]).toEqual({
      id: "guest-joined-502",
      kind: "guest-joined",
      locationGuestId: 502,
      guestName: "Mohamed Mahmoud",
      initials: "MM",
      headline: "Mohamed joined your customer club",
      joinSourceLabel: "From QR scan",
      consentLabel: "Opted out",
      createdAt: "2026-07-13T09:00:00.000Z",
      canViewGuest: true,
      canSendOffer: false,
    })
  })

  it("enables View guest on feedback rows when locationGuestId is present", () => {
    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      latestActivity: [
        {
          kind: "feedback",
          locationGuestId: 501,
          ...recentFeedback[0],
        },
      ],
    })

    expect(viewModel?.activityByTab.feedback[0]).toMatchObject({
      kind: "feedback",
      feedbackId: 10,
      locationGuestId: 501,
      canViewGuest: true,
    })
  })
})

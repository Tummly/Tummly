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
  },
  {
    id: 11,
    guestName: "Sam Guest",
    guestContact: "+441234",
    contactType: "Phone",
    comment: "Food was cold.",
    createdAt: "2026-07-12T11:00:00.000Z",
  },
]

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
      canDownloadQr: true,
      canPreviewGuestForm: true,
      dateRangeLabel: "Last 7 days",
    })
    expect(viewModel).not.toHaveProperty("needsAttention")
    expect(viewModel).not.toHaveProperty("liveOffersAndCampaigns")
    expect(viewModel).not.toHaveProperty("recommendedAction")
    expect(viewModel).not.toHaveProperty("weeklyBrief")
    expect(viewModel).not.toHaveProperty("canCopyGuestLink")
    expect(viewModel).not.toHaveProperty("canExport")
    expect(viewModel).not.toHaveProperty("canCreateCampaign")
    expect(viewModel).not.toHaveProperty("locations")
    expect(viewModel).not.toHaveProperty("operatorDisplayName")
    expect(viewModel).not.toHaveProperty("activationExpiresAt")
    expect(viewModel).not.toHaveProperty("locationSwitcherInteractive")
    expect(viewModel).not.toHaveProperty("activeNavId")
    expect(viewModel).not.toHaveProperty("pageTitle")
  })

  it("disables Preview when the selected location has no Smart Guest Link", () => {
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
      canDownloadQr: true,
      canPreviewGuestForm: false,
    })
  })

  it("updates QR and guest link targets when the selected Owned location changes", () => {
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
      canDownloadQr: true,
      canPreviewGuestForm: true,
    })
    expect(second).toMatchObject({
      selectedLocationId: 2,
      selectedLocationName: "Later Venue",
      smartGuestLink: "https://guest.example/later",
      canDownloadQr: true,
      canPreviewGuestForm: true,
    })
  })

  it("builds six Finish-setting-up steps with complete/partial/incomplete from acks and feedback", () => {
    const withoutPreview = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: { total: 0, recent: [] },
      checklistAcks: {
        guestFormPreviewed: false,
        qrPlacementGuideViewed: false,
      },
    })

    expect(withoutPreview?.canPreviewGuestForm).toBe(true)
    expect(withoutPreview?.setupSteps.map((step) => [step.id, step.status])).toEqual([
      ["account-ready", "complete"],
      ["guest-form", "partial"],
      ["qr-placement", "partial"],
      ["first-response", "incomplete"],
      ["first-offer", "incomplete"],
      ["first-campaign", "incomplete"],
    ])
    expect(withoutPreview?.setupSteps[1]?.actions).toEqual([
      { id: "preview-guest-form", label: "Preview form", available: true },
      { id: "edit-form", label: "Edit form", available: false },
    ])
    expect(withoutPreview?.setupSteps[2]?.actions).toEqual([
      {
        id: "view-placement-guide",
        label: "View placement guide",
        available: false,
      },
    ])
    expect(withoutPreview?.setupSteps[4]?.actions).toEqual([
      { id: "create-offer", label: "Create offer", available: false },
    ])
    expect(withoutPreview?.setupSteps[5]?.actions).toEqual([
      { id: "create-campaign", label: "Create campaign", available: false },
    ])

    const afterPreviewAndFeedback = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: { total: 3, recent: recentFeedback },
      checklistAcks: {
        guestFormPreviewed: true,
        qrPlacementGuideViewed: false,
      },
    })

    expect(
      afterPreviewAndFeedback?.setupSteps.map((step) => [step.id, step.status])
    ).toEqual([
      ["account-ready", "complete"],
      ["guest-form", "complete"],
      ["qr-placement", "partial"],
      ["first-response", "complete"],
      ["first-offer", "incomplete"],
      ["first-campaign", "incomplete"],
    ])
  })

  it("maps real feedback into Feedback submitted KPI and All/Feedback activity newest-first", () => {
    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: { total: 42, recent: recentFeedback },
    })

    expect(viewModel?.kpis).toEqual([
      {
        id: "qr-scans",
        label: "QR scans",
        value: 0,
        trendPercent: null,
        hasRealData: false,
      },
      {
        id: "feedback",
        label: "Feedback submitted",
        value: 42,
        trendPercent: null,
        hasRealData: true,
      },
      {
        id: "guests-joined",
        label: "Guests joined",
        value: 0,
        trendPercent: null,
        hasRealData: false,
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
        comment: "Food was cold.",
        guestName: "Sam Guest",
        createdAt: "2026-07-12T11:00:00.000Z",
        sentiment: null,
        canViewFeedback: false,
        canViewGuest: false,
      },
      {
        id: "feedback-10",
        kind: "feedback",
        comment: "Great service.",
        guestName: "Alex Guest",
        createdAt: "2026-07-12T10:00:00.000Z",
        sentiment: null,
        canViewFeedback: false,
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

  it("keeps activity empty copy and honest KPI/activity defaults without static section shells", () => {
    const viewModel = buildOperatorHomeViewModel({
      locations,
      selectedLocationId: 1,
      feedback: { total: 0, recent: [] },
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
})

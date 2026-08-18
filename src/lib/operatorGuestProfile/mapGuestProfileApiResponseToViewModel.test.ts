import { describe, expect, it } from "vitest"

import { mapGuestProfileApiResponseToViewModel } from "@/lib/operatorGuestProfile/mapGuestProfileApiResponseToViewModel"
import type { GuestProfileResponse } from "@/types/dashboard"

function createGuestProfileResponse(
  overrides: Partial<GuestProfileResponse> = {}
): GuestProfileResponse {
  return {
    success: true,
    locationId: 12,
    id: 1842,
    name: "Amelia Hart",
    marketingStatus: "Eligible — Email",
    marketingPreference: "allowed",
    guestSinceAt: "2026-05-12T10:00:00Z",
    lastActivityAt: "2026-07-20T14:22:00Z",
    lastInteractionLabel: "Feedback submitted",
    profileSummary: {
      email: "amelia@example.com",
      mobile: null,
      firstCapturedAt: "2026-05-12T10:00:00Z",
      locationName: "Camden Street",
      feedbackSubmissionCount: 3,
      offerClaimsAndRedemptions: 0,
      lastInteractionAt: "2026-07-20T14:22:00Z",
      lastInteractionLabel: "Feedback submitted",
      guestTags: [],
    },
    overviewDetails: {
      guestSinceAt: "2026-05-12T10:00:00Z",
      totalInteractions: 3,
      feedbackReceived: 3,
      offersClaimed: 0,
      campaignsSent: 0,
      lastActivityAt: "2026-07-20T14:22:00Z",
    },
    contactEligibility: [
      {
        channel: "email",
        status: "eligible",
        detailKind: "consent_captured",
        detailAt: "2026-05-12T10:00:00Z",
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

describe("mapGuestProfileApiResponseToViewModel", () => {
  const nowMs = Date.parse("2026-07-22T12:00:00Z")

  it("maps live header, summary, overview, and eligibility presentation", () => {
    const viewModel = mapGuestProfileApiResponseToViewModel({
      response: createGuestProfileResponse(),
      nowMs,
    })

    expect(viewModel.id).toBe("1842")
    expect(viewModel.name).toBe("Amelia Hart")
    expect(viewModel.marketingStatusLabel).toBe("Eligible — Email")
    expect(viewModel.guestSinceDisplay).toBe("12 May 2026")
    expect(viewModel.lastActivityDisplay).toBe("1 day ago")
    expect(viewModel.identitySubtitle).toBe(
      "Guest since 12 May 2026 · Last activity 1 day ago"
    )
    expect(viewModel.profileSummary.emailDisplay).toBe("amelia@example.com")
    expect(viewModel.profileSummary.mobileDisplay).toBe("Not provided")
    expect(viewModel.profileSummary.guestTagsDisplay).toBe("Not provided")
    expect(viewModel.profileSummary.guestTags).toEqual([])
    expect(viewModel.profileSummary.offerClaimsAndRedemptions).toBe(0)
    expect(viewModel.overviewDetails.totalInteractions).toBe(3)
    expect(viewModel.overviewDetails.offersClaimed).toBe(0)
    expect(viewModel.overviewDetails.campaignsSent).toBe(0)
    expect(viewModel.contactEligibility).toEqual([
      {
        channel: "email",
        channelLabel: "Email",
        status: "eligible",
        statusLabel: "Eligible",
        detailDisplay: "Consent captured 12 May 2026, 11:00 AM",
      },
      {
        channel: "sms",
        channelLabel: "SMS",
        status: "not_provided",
        statusLabel: "Not provided",
        detailDisplay: "—",
      },
    ])
    expect(viewModel.latestFeedback).toEqual([])
    expect(viewModel.recentNotes).toEqual([])
  })

  it("maps live guestTags into display and chip rows", () => {
    const viewModel = mapGuestProfileApiResponseToViewModel({
      response: createGuestProfileResponse({
        profileSummary: {
          email: "amelia@example.com",
          mobile: null,
          firstCapturedAt: "2026-05-12T10:00:00Z",
          locationName: "Camden Street",
          feedbackSubmissionCount: 3,
          offerClaimsAndRedemptions: 0,
          lastInteractionAt: "2026-07-20T14:22:00Z",
          lastInteractionLabel: "Feedback submitted",
          guestTags: [
            { id: 7, name: "Regular" },
            { id: 3, name: "VIP Guest" },
          ],
        },
      }),
      nowMs,
    })

    expect(viewModel.profileSummary.guestTagsDisplay).toBe(
      "Regular, VIP Guest"
    )
    expect(viewModel.profileSummary.guestTags).toEqual([
      { id: "7", name: "Regular" },
      { id: "3", name: "VIP Guest" },
    ])
  })

  it("maps latestFeedback preview with honesty rules for classification and source", () => {
    const viewModel = mapGuestProfileApiResponseToViewModel({
      response: createGuestProfileResponse({
        latestFeedback: [
          {
            id: 91,
            createdAt: "2026-07-15T18:42:00Z",
            comment:
              "Service was very slow and the table was not ready when we arrived for our booking",
            locationName: "Soho",
            classificationStatus: "Succeeded",
            sentiment: "negative",
            detectedTags: ["WaitTime", "Service"],
          },
          {
            id: 90,
            createdAt: "2026-07-14T10:00:00Z",
            comment: "Pending review",
            locationName: "Soho",
            classificationStatus: "Pending",
            sentiment: null,
            detectedTags: null,
          },
        ],
      }),
      nowMs,
    })

    expect(viewModel.latestFeedback).toEqual([
      {
        id: 91,
        classificationDisplay: "negative",
        dateDisplay: "15 July 2026, 7:42 PM",
        locationName: "Soho",
        sourceDisplay: "Guest QR form",
        feedbackDisplay:
          "Service was very slow and the table was not ready when we arrived for our booki…",
        feedbackFullDisplay:
          "Service was very slow and the table was not ready when we arrived for our booking",
        issueTagLabels: ["Wait time", "Service"],
        recoveryDisplay: "—",
      },
      {
        id: 90,
        classificationDisplay: null,
        dateDisplay: "14 July 2026, 11:00 AM",
        locationName: "Soho",
        sourceDisplay: "Guest QR form",
        feedbackDisplay: "Pending review",
        feedbackFullDisplay: "Pending review",
        issueTagLabels: null,
        recoveryDisplay: "—",
      },
    ])
  })

  it("maps recentNotes preview with author and absolute datetime", () => {
    const viewModel = mapGuestProfileApiResponseToViewModel({
      response: createGuestProfileResponse({
        recentNotes: [
          {
            id: 5,
            body: "Guest contacted after slow service report.",
            authorDisplayName: "Sarah Jones",
            createdAt: "2026-07-15T19:10:00Z",
          },
        ],
      }),
      nowMs,
    })

    expect(viewModel.recentNotes).toEqual([
      {
        id: 5,
        body: "Guest contacted after slow service report.",
        authorDisplayName: "Sarah Jones",
        createdAtDisplay: "15 July 2026, 8:10 PM",
        isEdited: false,
      },
    ])
  })

  it("maps unsubscribed contact status labels", () => {
    const viewModel = mapGuestProfileApiResponseToViewModel({
      response: createGuestProfileResponse({
        marketingStatus: "Not eligible",
        marketingPreference: "opted_out",
        contactEligibility: [
          {
            channel: "email",
            status: "unsubscribed",
            detailKind: "unsubscribed",
            detailAt: "2026-06-01T09:30:00Z",
          },
          {
            channel: "sms",
            status: "unsubscribed",
            detailKind: "unsubscribed",
            detailAt: "2026-06-01T09:30:00Z",
          },
        ],
      }),
      nowMs,
    })

    expect(viewModel.contactEligibility).toEqual([
      {
        channel: "email",
        channelLabel: "Email",
        status: "unsubscribed",
        statusLabel: "Unsubscribed",
        detailDisplay: "Unsubscribed 1 June 2026, 10:30 AM",
      },
      {
        channel: "sms",
        channelLabel: "SMS",
        status: "unsubscribed",
        statusLabel: "Unsubscribed",
        detailDisplay: "Unsubscribed 1 June 2026, 10:30 AM",
      },
    ])
  })

  it("maps not_recorded contact status labels without inventing Not provided", () => {
    const viewModel = mapGuestProfileApiResponseToViewModel({
      response: createGuestProfileResponse({
        marketingStatus: "Not eligible",
        marketingPreference: "not_recorded",
        contactEligibility: [
          {
            channel: "email",
            status: "not_recorded",
            detailKind: "not_recorded",
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
      nowMs,
    })

    expect(viewModel.contactEligibility).toEqual([
      {
        channel: "email",
        channelLabel: "Email",
        status: "not_recorded",
        statusLabel: "Not recorded",
        detailDisplay: "—",
      },
      {
        channel: "sms",
        channelLabel: "SMS",
        status: "not_provided",
        statusLabel: "Not provided",
        detailDisplay: "—",
      },
    ])
  })

  it("does not label not_recorded Detail as Unsubscribed when a date exists", () => {
    const viewModel = mapGuestProfileApiResponseToViewModel({
      response: createGuestProfileResponse({
        marketingStatus: "Not eligible",
        marketingPreference: "not_recorded",
        contactEligibility: [
          {
            channel: "email",
            status: "not_recorded",
            detailKind: "not_recorded",
            detailAt: "2026-06-01T09:30:00Z",
          },
          {
            channel: "sms",
            status: "not_provided",
            detailKind: null,
            detailAt: null,
          },
        ],
      }),
      nowMs,
    })

    expect(viewModel.contactEligibility[0]).toMatchObject({
      status: "not_recorded",
      statusLabel: "Not recorded",
      detailDisplay: "Not recorded 1 June 2026, 10:30 AM",
    })
  })

  it("shows empty Detail when consent_captured has no detailAt", () => {
    const viewModel = mapGuestProfileApiResponseToViewModel({
      response: createGuestProfileResponse({
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
      nowMs,
    })

    expect(viewModel.contactEligibility[0]?.detailDisplay).toBe("—")
  })

  it("omits last activity from the identity subtitle when null", () => {
    const viewModel = mapGuestProfileApiResponseToViewModel({
      response: createGuestProfileResponse({
        lastActivityAt: null,
        overviewDetails: {
          guestSinceAt: "2026-05-12T10:00:00Z",
          totalInteractions: 0,
          feedbackReceived: 0,
          offersClaimed: 0,
          campaignsSent: 0,
          lastActivityAt: null,
        },
        profileSummary: {
          email: null,
          mobile: null,
          firstCapturedAt: "2026-05-12T10:00:00Z",
          locationName: "Camden Street",
          feedbackSubmissionCount: 0,
          offerClaimsAndRedemptions: 0,
          lastInteractionAt: null,
          lastInteractionLabel: "Feedback submitted",
          guestTags: [],
        },
      }),
      nowMs,
    })

    expect(viewModel.identitySubtitle).toBe("Guest since 12 May 2026")
    expect(viewModel.overviewDetails.lastActivityDisplay).toBe("—")
    expect(viewModel.profileSummary.lastInteractionDisplay).toBe("—")
  })
})

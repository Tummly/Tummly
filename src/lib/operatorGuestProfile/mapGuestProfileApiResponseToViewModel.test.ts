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
    offersOptOut: false,
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
      guestTags: null,
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
        detailAt: null,
      },
      {
        channel: "sms",
        status: "not_provided",
        detailKind: null,
        detailAt: null,
      },
    ],
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
      },
      {
        channel: "sms",
        channelLabel: "SMS",
        status: "not_provided",
        statusLabel: "Not provided",
      },
    ])
  })

  it("maps unsubscribed contact status labels", () => {
    const viewModel = mapGuestProfileApiResponseToViewModel({
      response: createGuestProfileResponse({
        marketingStatus: "Not eligible",
        offersOptOut: true,
        contactEligibility: [
          {
            channel: "email",
            status: "unsubscribed",
            detailKind: "unsubscribed",
            detailAt: null,
          },
          {
            channel: "sms",
            status: "unsubscribed",
            detailKind: "unsubscribed",
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
        status: "unsubscribed",
        statusLabel: "Unsubscribed",
      },
      {
        channel: "sms",
        channelLabel: "SMS",
        status: "unsubscribed",
        statusLabel: "Unsubscribed",
      },
    ])
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
          guestTags: null,
        },
      }),
      nowMs,
    })

    expect(viewModel.identitySubtitle).toBe("Guest since 12 May 2026")
    expect(viewModel.overviewDetails.lastActivityDisplay).toBe("—")
    expect(viewModel.profileSummary.lastInteractionDisplay).toBe("—")
  })
})

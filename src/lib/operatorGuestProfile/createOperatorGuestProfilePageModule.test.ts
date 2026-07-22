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
      guestTags: null,
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
    ...overrides,
  }
}

function createAdapters(
  getGuestProfile: Mock<OperatorGuestProfilePageAdapters["getGuestProfile"]>
): OperatorGuestProfilePageAdapters {
  return { getGuestProfile }
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
})

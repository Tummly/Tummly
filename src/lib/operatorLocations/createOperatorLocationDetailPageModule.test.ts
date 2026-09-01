import { describe, expect, it, vi } from "vitest"
import { AxiosError, type AxiosResponse } from "axios"

import { createOperatorLocationDetailPageModule } from "@/lib/operatorLocations/createOperatorLocationDetailPageModule"
import type { LocationDetailApiResponse } from "@/lib/operatorLocations/locationDetailApi"
import {
  formatLocationDetailHeaderMeta,
  formatLocationDetailMonthMetric,
  resolveLocationDetailTabId,
} from "@/lib/operatorLocations/locationDetailPresentation"

function detailResponse(
  overrides: Partial<LocationDetailApiResponse> = {}
): LocationDetailApiResponse {
  return {
    success: true,
    header: {
      id: 42,
      name: "KFC Chicken — Camden",
      city: "Camden",
      lifecycleStatus: "active",
      setupStatus: "ready",
      managerName: "Aisha",
      managerUserId: 7,
      address: "1 High Street",
      postcode: "NW1 1AA",
      locationPhone: null,
      localContact: null,
      liveQrCount: 6,
      guestsCapturedThisMonth: 842,
      ...(overrides.header ?? {}),
    },
    setupChecklist: {
      locationDetailsAdded: "complete",
      qrCodePublishedLive: "complete",
      guestFormConnected: "complete",
      teamAccessAssigned: "complete",
      guestPrivacyNotice: "complete",
      firstOfferCreated: "complete",
      atLeastOneQrCreated: "complete",
      ...(overrides.setupChecklist ?? {}),
    },
    locationControls: {
      lastScanAt: null,
      lastFeedbackAt: null,
      ...(overrides.locationControls ?? {}),
    },
    overviewMetrics: {
      qrScans: 1204,
      formStarts: 0,
      feedback: 18,
      guestsCaptured: 842,
      optIns: 96,
      offersClaimed: 44,
      offersRedeemed: 12,
      ...(overrides.overviewMetrics ?? {}),
    },
    qrRows: overrides.qrRows ?? [
      {
        qrCodeId: 9,
        name: "Counter card",
        placement: "Counter card",
        statusLabel: "Active",
        scans: 1204,
        starts: 0,
        submissions: 18,
        optIns: 96,
        claims: 0,
        lastScanAtUtc: "2026-09-01T10:00:00Z",
      },
    ],
    offerCards: overrides.offerCards ?? [
      {
        entityId: 10,
        kind: "offer",
        statusLabel: "Active",
        title: "Welcome offer",
        meta: "44 claims · 12 redemptions",
        primaryCta: "View offer",
        secondaryCta: "View redemptions",
      },
    ],
    guestActivityChecklist: {
      guestProfilesCreated: "optional",
      offerClaims: "optional",
      consentOptIns: "optional",
      offerRedemptions: "optional",
      feedbackSubmitted: "optional",
      unsubscribes: "optional",
      needsRecovery: "complete",
      ...(overrides.guestActivityChecklist ?? {}),
    },
    latestFeedbackRows: overrides.latestFeedbackRows ?? [],
    teamAccessRows: overrides.teamAccessRows ?? [
      {
        membershipId: 7,
        userId: 7,
        name: "Aisha",
        role: "Location Manager",
        accessLabel: "KFC Chicken — Camden only",
        lastActiveAt: null,
      },
    ],
    ...overrides,
  }
}

function axios404(): AxiosError {
  return new AxiosError(
    "Not Found",
    "ERR_BAD_REQUEST",
    undefined,
    undefined,
    { status: 404 } as AxiosResponse
  )
}

describe("resolveLocationDetailTabId", () => {
  it("accepts known tabs and defaults to overview", () => {
    expect(resolveLocationDetailTabId("overview")).toBe("overview")
    expect(resolveLocationDetailTabId("setup-details")).toBe("setup-details")
    expect(resolveLocationDetailTabId("guest-loop")).toBe("guest-loop")
    expect(resolveLocationDetailTabId("team-access")).toBe("team-access")
    expect(resolveLocationDetailTabId("location-controls")).toBe(
      "location-controls"
    )
    expect(resolveLocationDetailTabId("nope")).toBe("overview")
    expect(resolveLocationDetailTabId(null)).toBe("overview")
    expect(resolveLocationDetailTabId("")).toBe("overview")
  })
})

describe("formatLocationDetailHeaderMeta", () => {
  it("formats city, QR count and guest count", () => {
    expect(
      formatLocationDetailHeaderMeta({
        city: "Camden",
        qrCount: 6,
        guestCount: 842,
      })
    ).toBe("Camden · 6 QR codes · 842 guests captured")
  })

  it("uses singular labels for one", () => {
    expect(
      formatLocationDetailHeaderMeta({
        city: "Soho",
        qrCount: 1,
        guestCount: 1,
      })
    ).toBe("Soho · 1 QR code · 1 guest captured")
  })
})

describe("formatLocationDetailMonthMetric", () => {
  it("appends this month with grouped thousands", () => {
    expect(formatLocationDetailMonthMetric(1204)).toBe("1,204 this month")
  })
})

describe("createOperatorLocationDetailPageModule", () => {
  it("keeps getSnapshot identity until emit", async () => {
    const getDetail = vi.fn().mockResolvedValue(detailResponse())
    const pageModule = createOperatorLocationDetailPageModule(
      42,
      { getDetail },
      { dashboardMode: "multi" }
    )

    expect(pageModule.getSnapshot()).toBe(pageModule.getSnapshot())

    await pageModule.load()

    expect(pageModule.getSnapshot()).toBe(pageModule.getSnapshot())
    expect(getDetail).toHaveBeenCalledWith(42)
    expect(pageModule.getSnapshot().name).toBe("KFC Chicken — Camden")
    expect(pageModule.getSnapshot().city).toBe("Camden")
    expect(pageModule.getSnapshot().headerMeta).toBe(
      "Camden · 6 QR codes · 842 guests captured"
    )
    expect(pageModule.getSnapshot().setupChecklist.locationDetailsAdded).toBe(
      "complete"
    )
    expect(pageModule.getSnapshot().setupChecklist.teamAccessAssigned).toBe(
      "complete"
    )
    expect(pageModule.getSnapshot().teamAccessRows).toEqual([
      {
        id: "7",
        name: "Aisha",
        role: "Location Manager",
        accessLabel: "KFC Chicken — Camden only",
        lastActiveLabel: "—",
      },
    ])
    expect(pageModule.getSnapshot().locationControlsStatus.locationStatus).toBe(
      "Active"
    )
    expect(pageModule.getSnapshot().locationControlsActions[0]?.id).toBe(
      "pause"
    )
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
  })

  it("maps overview metrics, qr rows, and offer cards from detail response", async () => {
    const fixedNow = Date.parse("2026-09-01T12:00:00Z")
    const getDetail = vi.fn().mockResolvedValue(detailResponse())
    const pageModule = createOperatorLocationDetailPageModule(
      42,
      { getDetail },
      {
        dashboardMode: "single",
        nowMs: () => fixedNow,
      }
    )

    await pageModule.load()

    expect(pageModule.getSnapshot().overviewMetrics).toEqual({
      qrScans: 1204,
      formStarts: 0,
      feedback: 18,
      guestsCaptured: 842,
      optIns: 96,
      offersClaimed: 44,
      offersRedeemed: 12,
    })
    expect(pageModule.getSnapshot().qrRows).toEqual([
      {
        id: "9",
        name: "Counter card",
        placement: "Counter card",
        statusLabel: "Active",
        scans: "1,204 opens",
        starts: "0 starts",
        submissions: "18 submissions",
        optIns: "96 opt-ins",
        claims: "0 claims",
        lastScannedLabel: "2 hours ago",
      },
    ])
    expect(pageModule.getSnapshot().offerCards[0]).toMatchObject({
      id: "offer-10",
      kind: "offer",
      title: "Welcome offer",
      hrefPrimary: "/single-dashboard/offers/10?location=42",
      hrefSecondary:
        "/single-dashboard/offers/10?location=42&tab=redemptions",
    })
  })

  it("maps teamAccessRows from detail response including last active", async () => {
    vi.useFakeTimers({ now: new Date("2026-08-26T12:00:00.000Z") })
    const getDetail = vi.fn().mockResolvedValue(
      detailResponse({
        teamAccessRows: [
          {
            membershipId: 3,
            userId: 3,
            name: "Sam",
            role: "Admin",
            accessLabel: "All locations",
            lastActiveAt: "2026-08-26T09:42:00.000Z",
          },
        ],
      })
    )
    const pageModule = createOperatorLocationDetailPageModule(42, { getDetail })

    await pageModule.load()

    expect(pageModule.getSnapshot().teamAccessRows).toEqual([
      {
        id: "3",
        name: "Sam",
        role: "Admin",
        accessLabel: "All locations",
        lastActiveLabel: "Today, 10:42",
      },
    ])
    vi.useRealTimers()
  })

  it("uses server setup checklist without client heuristics", async () => {
    const getDetail = vi.fn().mockResolvedValue(
      detailResponse({
        setupChecklist: {
          locationDetailsAdded: "incomplete",
          qrCodePublishedLive: "incomplete",
          guestFormConnected: "complete",
          teamAccessAssigned: "optional",
          guestPrivacyNotice: "incomplete",
          firstOfferCreated: "optional",
          atLeastOneQrCreated: "incomplete",
        },
      })
    )
    const pageModule = createOperatorLocationDetailPageModule(42, { getDetail })

    await pageModule.load()

    expect(pageModule.getSnapshot().setupChecklist).toEqual({
      locationDetailsAdded: "incomplete",
      qrCodePublishedLive: "incomplete",
      guestFormConnected: "complete",
      teamAccessAssigned: "optional",
      guestPrivacyNotice: "incomplete",
      firstOfferCreated: "optional",
      atLeastOneQrCreated: "incomplete",
    })
  })

  it("maps guest activity checklist and latest feedback from detail GET", async () => {
    const getDetail = vi.fn().mockResolvedValue(
      detailResponse({
        guestActivityChecklist: {
          guestProfilesCreated: "complete",
          offerClaims: "complete",
          consentOptIns: "optional",
          offerRedemptions: "optional",
          feedbackSubmitted: "needs-action",
          unsubscribes: "optional",
          needsRecovery: "needs-action",
        },
        latestFeedbackRows: [
          {
            feedbackId: 88,
            comment: "Food was cold",
            guestName: "Alex Rivera",
            sentiment: "negative",
            timeLabel: "2 hours ago",
            canStartRecovery: true,
            locationGuestId: 501,
          },
        ],
      })
    )
    const pageModule = createOperatorLocationDetailPageModule(42, { getDetail })

    await pageModule.load()

    expect(pageModule.getSnapshot().guestActivityChecklist).toEqual({
      guestProfilesCreated: "complete",
      offerClaims: "complete",
      consentOptIns: "optional",
      offerRedemptions: "optional",
      feedbackSubmitted: "needs-action",
      unsubscribes: "optional",
      needsRecovery: "needs-action",
    })
    expect(pageModule.getSnapshot().latestFeedbackRows).toEqual([
      {
        id: "88",
        feedbackId: 88,
        comment: "Food was cold",
        guestName: "Alex Rivera",
        sentiment: "negative",
        timeLabel: "2 hours ago",
        canStartRecovery: true,
        locationGuestId: 501,
      },
    ])
  })

  it("marks not-found when detail GET returns 404", async () => {
    const getDetail = vi.fn().mockRejectedValue(axios404())
    const pageModule = createOperatorLocationDetailPageModule(
      99,
      { getDetail },
      { fallbackName: "Best effort" }
    )

    await pageModule.load()

    expect(pageModule.getSnapshot().loadStatus).toBe("not-found")
  })

  it("marks error when detail GET fails with a non-404 status", async () => {
    const getDetail = vi.fn().mockRejectedValue(new Error("network"))
    const pageModule = createOperatorLocationDetailPageModule(42, { getDetail })

    await pageModule.load()

    expect(pageModule.getSnapshot().loadStatus).toBe("error")
  })

  it("honours initial tab from the URL", () => {
    const setupModule = createOperatorLocationDetailPageModule(
      42,
      { getDetail: vi.fn() },
      { initialTabId: "setup-details" }
    )
    expect(setupModule.getSnapshot().activeTabId).toBe("setup-details")

    const guestLoopModule = createOperatorLocationDetailPageModule(
      42,
      { getDetail: vi.fn() },
      { initialTabId: "guest-loop" }
    )
    expect(guestLoopModule.getSnapshot().activeTabId).toBe("guest-loop")
  })

  it("changes tabs through requestTabChange", () => {
    const pageModule = createOperatorLocationDetailPageModule(42, {
      getDetail: vi.fn(),
    })

    pageModule.requestTabChange("team-access")
    expect(pageModule.getSnapshot().activeTabId).toBe("team-access")
  })

  it("maps location controls timestamps from detail GET", async () => {
    const getDetail = vi.fn().mockResolvedValue(
      detailResponse({
        locationControls: {
          lastScanAt: "2026-08-31T12:42:00.000Z",
          lastFeedbackAt: "2026-08-31T12:50:00.000Z",
        },
      })
    )
    const pageModule = createOperatorLocationDetailPageModule(42, {
      getDetail,
      getNow: () => new Date("2026-08-31T14:00:00.000Z"),
    })

    await pageModule.load()

    expect(pageModule.getSnapshot().locationControlsStatus.lastScan).toBe(
      "13:42"
    )
    expect(pageModule.getSnapshot().locationControlsStatus.lastFeedback).toBe(
      "Today, 13:50"
    )
  })

  it("saveEditDetails refreshes snapshot after PUT succeeds", async () => {
    const getDetail = vi
      .fn()
      .mockResolvedValueOnce(detailResponse())
      .mockResolvedValueOnce(
        detailResponse({
          header: {
            name: "Updated Camden",
            address: "99 High Street",
            city: "Camden",
            postcode: "NW1 2BB",
            liveQrCount: 6,
            guestsCapturedThisMonth: 842,
          },
        })
      )
    const updateDetails = vi.fn(async () => undefined)
    const pageModule = createOperatorLocationDetailPageModule(42, {
      getDetail,
      updateDetails,
    })

    await pageModule.load()
    await pageModule.saveEditDetails({
      locationName: "Updated Camden",
      address: "99 High Street",
      city: "Camden",
      postcode: "NW1 2BB",
    })

    expect(updateDetails).toHaveBeenCalledWith(42, {
      locationName: "Updated Camden",
      address: "99 High Street",
      city: "Camden",
      postcode: "NW1 2BB",
    })
    expect(getDetail).toHaveBeenCalledTimes(2)
    expect(pageModule.getSnapshot().name).toBe("Updated Camden")
    expect(pageModule.getSnapshot().editFields.postcode).toBe("NW1 2BB")
  })

  it("requestLifecycleAction refetches detail after mutate succeeds", async () => {
    const getDetail = vi
      .fn()
      .mockResolvedValueOnce(detailResponse())
      .mockResolvedValueOnce(
        detailResponse({
          header: {
            lifecycleStatus: "paused",
            liveQrCount: 6,
            guestsCapturedThisMonth: 842,
          },
        })
      )
    const mutateLifecycle = vi.fn(async () => undefined)
    const pageModule = createOperatorLocationDetailPageModule(42, {
      getDetail,
      mutateLifecycle,
    })

    await pageModule.load()
    await pageModule.requestLifecycleAction("pause")

    expect(mutateLifecycle).toHaveBeenCalledWith(42, "pause")
    expect(getDetail).toHaveBeenCalledTimes(2)
    expect(pageModule.getSnapshot().lifecycleStatus).toBe("paused")
  })

  it("keeps loaded snapshot when lifecycle mutate fails", async () => {
    const getDetail = vi.fn().mockResolvedValue(detailResponse())
    const mutateLifecycle = vi.fn(async () => {
      throw new Error("network")
    })
    const pageModule = createOperatorLocationDetailPageModule(42, {
      getDetail,
      mutateLifecycle,
    })

    await pageModule.load()
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")

    await expect(pageModule.requestLifecycleAction("pause")).rejects.toThrow(
      "Could not update location."
    )

    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
    expect(pageModule.getSnapshot().lifecycleStatus).toBe("active")
    expect(pageModule.getSnapshot().lifecycleMutationPending).toBe(false)
  })

  it("disables lifecycle actions until detail GET completes", async () => {
    let resolveDetail: (value: LocationDetailApiResponse) => void = () => {}
    const getDetail = vi.fn(
      () =>
        new Promise<LocationDetailApiResponse>((resolve) => {
          resolveDetail = resolve
        })
    )
    const pageModule = createOperatorLocationDetailPageModule(42, { getDetail })

    const loadPromise = pageModule.load()
    expect(pageModule.getSnapshot().loadStatus).toBe("loading")
    expect(
      pageModule.getSnapshot().locationControlsActions.every(
        (action) => !action.enabled
      )
    ).toBe(true)

    resolveDetail(detailResponse())
    await loadPromise

    expect(
      pageModule.getSnapshot().locationControlsActions.some(
        (action) => action.enabled
      )
    ).toBe(true)
  })
})

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
    expect(resolveLocationDetailTabId("setup-details")).toBe("setup-details")
    expect(resolveLocationDetailTabId("guest-loop")).toBe("guest-loop")
    expect(resolveLocationDetailTabId("nope")).toBe("overview")
    expect(resolveLocationDetailTabId(null)).toBe("overview")
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
        role: "Manager",
        accessLabel: "This location only",
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

  it("changes tabs through requestTabChange", () => {
    const pageModule = createOperatorLocationDetailPageModule(42, {
      getDetail: vi.fn(),
    })

    pageModule.requestTabChange("team-access")
    expect(pageModule.getSnapshot().activeTabId).toBe("team-access")
  })
})

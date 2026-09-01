import { describe, expect, it, vi } from "vitest"

import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"
import { createOperatorLocationDetailPageModule } from "@/lib/operatorLocations/createOperatorLocationDetailPageModule"
import type { LocationDetailApiResponse } from "@/lib/operatorLocations/locationDetailApi"
import {
  resolveLocationDetailTabId,
  type LocationDetailTabId,
} from "@/lib/operatorLocations/locationDetailPresentation"
import {
  resolveLocationRowActionNavigation,
  type LocationRowActionId,
} from "@/lib/operatorLocations/locationsPresentation"

function tabParamFromDetailPath(path: string): string | null {
  return new URL(path, "http://test").searchParams.get("tab")
}

function tabFromListRowAction(
  mode: OperatorDashboardMode,
  locationId: number,
  actionId: LocationRowActionId
): LocationDetailTabId {
  const path = resolveLocationRowActionNavigation(mode, locationId, actionId)
  expect(path).not.toBeNull()
  return resolveLocationDetailTabId(tabParamFromDetailPath(path!))
}

function detailResponse(
  overrides: Partial<LocationDetailApiResponse> = {}
): LocationDetailApiResponse {
  return {
    success: true,
    header: {
      id: 9,
      name: "Paused Venue — Shoreditch",
      city: "Shoreditch",
      lifecycleStatus: "paused",
      setupStatus: "ready",
      managerName: null,
      managerUserId: null,
      address: "2 Brick Lane",
      postcode: "E1 6PU",
      locationPhone: null,
      localContact: null,
      liveQrCount: 0,
      guestsCapturedThisMonth: 0,
      ...(overrides.header ?? {}),
    },
    setupChecklist: {
      locationDetailsAdded: "complete",
      qrCodePublishedLive: "complete",
      guestFormConnected: "complete",
      teamAccessAssigned: "optional",
      guestPrivacyNotice: "complete",
      firstOfferCreated: "optional",
      atLeastOneQrCreated: "complete",
    },
    ...overrides,
  }
}

/** Smoke test — list row navigation paths resolve to the expected detail tab. */
describe("location detail entry from Locations list", () => {
  it.each([
    ["edit-location", "setup-details"],
    ["view-historical-activity", "guest-loop"],
    ["view-location", "overview"],
    ["view-historical-record", "overview"],
  ] as const)(
    "row action %s opens detail tab %s",
    (actionId, expectedTab) => {
      for (const mode of ["single", "multi"] as const) {
        expect(tabFromListRowAction(mode, 9, actionId)).toBe(expectedTab)
      }
    }
  )

  it("explicit overview tab param resolves to overview", () => {
    expect(resolveLocationDetailTabId("overview")).toBe("overview")
  })

  it("invalid tab param falls back to overview without breaking load", async () => {
    const getDetail = vi.fn().mockResolvedValue(detailResponse())
    const pageModule = createOperatorLocationDetailPageModule(
      9,
      { getDetail },
      { initialTabId: "not-a-real-tab", fallbackName: "List row name" }
    )

    expect(pageModule.getSnapshot().activeTabId).toBe("overview")
    expect(pageModule.getSnapshot().name).toBe("List row name")

    await pageModule.load()

    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
    expect(pageModule.getSnapshot().activeTabId).toBe("overview")
    expect(getDetail).toHaveBeenCalledWith(9)
  })

  it("detail GET replaces fallback name while keeping deep-linked tab", async () => {
    const getDetail = vi.fn().mockResolvedValue(detailResponse())
    const pageModule = createOperatorLocationDetailPageModule(
      9,
      { getDetail },
      {
        initialTabId: "guest-loop",
        fallbackName: "Stale list label",
      }
    )

    expect(pageModule.getSnapshot().activeTabId).toBe("guest-loop")
    expect(pageModule.getSnapshot().name).toBe("Stale list label")

    await pageModule.load()

    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
    expect(pageModule.getSnapshot().activeTabId).toBe("guest-loop")
    expect(pageModule.getSnapshot().name).toBe("Paused Venue — Shoreditch")
    expect(pageModule.getSnapshot().headerMeta).toBe(
      "Shoreditch · 0 QR codes · 0 guests captured"
    )
  })

  it("syncs tab when URL search param changes after mount", () => {
    const pageModule = createOperatorLocationDetailPageModule(9, {
      getDetail: vi.fn(),
    })
    let notifyCount = 0
    pageModule.subscribe(() => {
      notifyCount += 1
    })

    pageModule.setActiveTabFromUrl("setup-details")
    expect(pageModule.getSnapshot().activeTabId).toBe("setup-details")
    expect(notifyCount).toBe(1)

    pageModule.setActiveTabFromUrl("setup-details")
    expect(notifyCount).toBe(1)

    pageModule.setActiveTabFromUrl("guest-loop")
    expect(pageModule.getSnapshot().activeTabId).toBe("guest-loop")
    expect(notifyCount).toBe(2)
  })
})

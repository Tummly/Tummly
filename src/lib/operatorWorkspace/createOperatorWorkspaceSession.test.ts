import { describe, expect, it, vi } from "vitest"

import { createOperatorWorkspaceSession } from "./createOperatorWorkspaceSession"
import type { LocationItem } from "@/types/dashboard"

const locations: LocationItem[] = [
  {
    id: 1,
    locationName: "First Venue",
    address: "1 High St",
    guestUrl: "https://guest.example/1",
    createdAt: "2026-01-01T00:00:00.000Z",
  },
  {
    id: 2,
    locationName: "Second Venue",
    address: "2 High St",
    guestUrl: "https://guest.example/2",
    createdAt: "2026-02-01T00:00:00.000Z",
  },
]

function createAdapters(overrides: {
  getLocations?: () => Promise<{
    success: boolean
    locations: LocationItem[]
    restaurantName?: string
    aiAssistantAccess?: boolean
    teamPermissionsAccess?: "none" | "view" | "manage"
    offersAccess?: "none" | "view" | "manage"
    privacyConsentAccess?: "none" | "view" | "manage"
  }>
  fetchCurrentUser?: () => Promise<unknown>
  getPersistedLocationId?: () => number | null
  persistSelectedLocation?: (locationId: number) => void
} = {}) {
  const persistSelectedLocation = vi.fn(overrides.persistSelectedLocation)
  return {
    getLocations:
      overrides.getLocations ??
      (async () => ({
        success: true,
        restaurantName: "Mehmet's Grill",
        locations,
      })),
    fetchCurrentUser:
      overrides.fetchCurrentUser ??
      (async () => ({
        success: true,
        data: {
          fullName: "Mohamed Mahmoud",
          activationExpiresAt: "2026-07-26T12:00:00.000Z",
        },
      })),
    getPersistedLocationId: overrides.getPersistedLocationId ?? (() => null),
    persistSelectedLocation,
  }
}

describe("createOperatorWorkspaceSession", () => {
  it("bootstraps Owned locations and profile into a loaded snapshot", async () => {
    const adapters = createAdapters()
    const session = createOperatorWorkspaceSession({ mode: "multi" }, adapters)

    expect(session.getSnapshot().status).toBe("idle")

    const loadingPromise = session.load({ queryLocationId: null })
    expect(session.getSnapshot().status).toBe("loading")

    await loadingPromise

    expect(session.getSnapshot()).toMatchObject({
      status: "loaded",
      mode: "multi",
      selectedLocationId: 1,
      restaurantName: "Mehmet's Grill",
      operatorDisplayName: "Mohamed Mahmoud",
      activationExpiresAt: "2026-07-26T12:00:00.000Z",
      selfRole: null,
      teamPermissionsAccess: "manage",
      locationSwitcherInteractive: true,
    })
    expect(session.getSnapshot().locations).toHaveLength(2)
    expect(session.getSnapshot().aiAssistantAccess).toBe(true)
    expect(adapters.persistSelectedLocation).toHaveBeenCalledWith(1)
  })

  it("hides Assistant when locations payload sets aiAssistantAccess false", async () => {
    const session = createOperatorWorkspaceSession(
      { mode: "multi" },
      createAdapters({
        getLocations: async () => ({
          success: true,
          locations,
          aiAssistantAccess: false,
        }),
      })
    )

    await session.load({ queryLocationId: null })

    expect(session.getSnapshot().aiAssistantAccess).toBe(false)
  })

  it("hides Team & permissions when locations payload sets teamPermissionsAccess none", async () => {
    const session = createOperatorWorkspaceSession(
      { mode: "multi" },
      createAdapters({
        getLocations: async () => ({
          success: true,
          locations,
          teamPermissionsAccess: "none",
        }),
      })
    )

    await session.load({ queryLocationId: null })

    expect(session.getSnapshot().teamPermissionsAccess).toBe("none")
  })

  it("defaults offersAccess to manage when locations payload omits it", async () => {
    const session = createOperatorWorkspaceSession(
      { mode: "multi" },
      createAdapters()
    )

    await session.load({ queryLocationId: null })

    expect(session.getSnapshot().offersAccess).toBe("manage")
  })

  it("sets offersAccess none when locations payload sets offersAccess none", async () => {
    const session = createOperatorWorkspaceSession(
      { mode: "multi" },
      createAdapters({
        getLocations: async () => ({
          success: true,
          locations,
          offersAccess: "none",
        }),
      })
    )

    await session.load({ queryLocationId: null })

    expect(session.getSnapshot().offersAccess).toBe("none")
  })

  it("defaults privacyConsentAccess to manage when locations payload omits it", async () => {
    const session = createOperatorWorkspaceSession(
      { mode: "multi" },
      createAdapters()
    )

    await session.load({ queryLocationId: null })

    expect(session.getSnapshot().privacyConsentAccess).toBe("manage")
  })

  it("sets privacyConsentAccess none when locations payload sets privacyConsentAccess none", async () => {
    const session = createOperatorWorkspaceSession(
      { mode: "multi" },
      createAdapters({
        getLocations: async () => ({
          success: true,
          locations,
          privacyConsentAccess: "none",
        }),
      })
    )

    await session.load({ queryLocationId: null })

    expect(session.getSnapshot().privacyConsentAccess).toBe("none")
  })

  it("carries Self role from /auth/me into the workspace snapshot", async () => {
    const adapters = createAdapters({
      fetchCurrentUser: async () => ({
        success: true,
        data: {
          fullName: "Mohamed Mahmoud",
          activationExpiresAt: "2026-07-26T12:00:00.000Z",
          selfRole: "founder-director",
          role: "Owner",
        },
      }),
    })
    const session = createOperatorWorkspaceSession({ mode: "multi" }, adapters)

    await session.load({ queryLocationId: null })

    expect(session.getSnapshot().selfRole).toBe("founder-director")
  })

  it("prefers a valid query location over persistence on load", async () => {
    const adapters = createAdapters({
      getPersistedLocationId: () => 1,
    })
    const session = createOperatorWorkspaceSession({ mode: "multi" }, adapters)

    await session.load({ queryLocationId: 2 })

    expect(session.getSnapshot().selectedLocationId).toBe(2)
    expect(adapters.persistSelectedLocation).toHaveBeenCalledWith(2)
  })

  it("ignores invalid or foreign query location ids and falls back to persistence", async () => {
    const adapters = createAdapters({
      getPersistedLocationId: () => 2,
    })
    const session = createOperatorWorkspaceSession({ mode: "multi" }, adapters)

    await session.load({ queryLocationId: 99 })

    expect(session.getSnapshot().selectedLocationId).toBe(2)
  })

  it("switches Owned location in multi mode and persists the selection", async () => {
    const adapters = createAdapters()
    const session = createOperatorWorkspaceSession({ mode: "multi" }, adapters)
    await session.load({ queryLocationId: null })
    adapters.persistSelectedLocation.mockClear()

    session.selectLocation(2)

    expect(session.getSnapshot().selectedLocationId).toBe(2)
    expect(adapters.persistSelectedLocation).toHaveBeenCalledWith(2)
  })

  it("does not switch location in single mode", async () => {
    const adapters = createAdapters()
    const session = createOperatorWorkspaceSession({ mode: "single" }, adapters)
    await session.load({ queryLocationId: null })
    adapters.persistSelectedLocation.mockClear()

    session.selectLocation(2)

    expect(session.getSnapshot().selectedLocationId).toBe(1)
    expect(session.getSnapshot().locationSwitcherInteractive).toBe(false)
    expect(adapters.persistSelectedLocation).not.toHaveBeenCalled()
  })

  it("applies a later query location without reloading locations", async () => {
    const getLocations = vi.fn(async () => ({ success: true, locations }))
    const adapters = createAdapters({ getLocations })
    const session = createOperatorWorkspaceSession({ mode: "multi" }, adapters)
    await session.load({ queryLocationId: 1 })
    expect(getLocations).toHaveBeenCalledTimes(1)

    session.preferLocationFromQuery(2)

    expect(session.getSnapshot().selectedLocationId).toBe(2)
    expect(getLocations).toHaveBeenCalledTimes(1)
  })

  it("surfaces bootstrap failures and recovers on retry", async () => {
    const getLocations = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce({ success: true, locations })
    const adapters = createAdapters({ getLocations })
    const session = createOperatorWorkspaceSession({ mode: "multi" }, adapters)

    await session.load({ queryLocationId: null })
    expect(session.getSnapshot().status).toBe("error")

    await session.retry()
    expect(session.getSnapshot().status).toBe("loaded")
    expect(session.getSnapshot().selectedLocationId).toBe(1)
  })

  it("loads an empty Owned-location list without selecting a venue", async () => {
    const adapters = createAdapters({
      getLocations: async () => ({ success: true, locations: [] }),
    })
    const session = createOperatorWorkspaceSession({ mode: "multi" }, adapters)

    await session.load({ queryLocationId: null })

    expect(session.getSnapshot()).toMatchObject({
      status: "loaded",
      locations: [],
      selectedLocationId: null,
    })
    expect(adapters.persistSelectedLocation).not.toHaveBeenCalled()
  })

  it("notifies subscribers when the snapshot changes", async () => {
    const adapters = createAdapters()
    const session = createOperatorWorkspaceSession({ mode: "multi" }, adapters)
    const listener = vi.fn()
    const unsubscribe = session.subscribe(listener)

    await session.load({ queryLocationId: null })
    expect(listener).toHaveBeenCalled()

    listener.mockClear()
    session.selectLocation(2)
    expect(listener).toHaveBeenCalledTimes(1)

    unsubscribe()
    listener.mockClear()
    session.selectLocation(1)
    expect(listener).not.toHaveBeenCalled()
  })
})

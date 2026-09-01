import { afterEach, describe, expect, it, vi } from "vitest"

import { createOperatorPrivacyConsentPageModule } from "@/lib/operatorPrivacyConsent/createOperatorPrivacyConsentPageModule"
import type { PrivacyConsentPageApiData } from "@/lib/operatorPrivacyConsent/mapPrivacyConsentApiResponse"
import type { PermissionRecordsListResponse } from "@/lib/operatorPrivacyConsent/permissionRecordsListQueryParams"

function pageData(
  overrides: Partial<PrivacyConsentPageApiData> = {}
): PrivacyConsentPageApiData {
  return {
    success: true,
    privacySetupRows: [
      {
        id: "privacy-notice",
        requirement: "Privacy notice",
        status: "Configured",
      },
      {
        id: "email-marketing",
        requirement: "Email marketing",
        status: "Enabled",
      },
    ],
    emailMarketingPermissionEnabled: true,
    smsMarketingPermissionEnabled: false,
    feedbackFollowUpPermissionEnabled: true,
    smsConsentWording: "",
    emailConsentWording: "We may email you.",
    privacyReady: false,
    actorCanManage: true,
    canViewGuests: true,
    ...overrides,
  }
}

function recordsResponse(
  overrides: Partial<PermissionRecordsListResponse> = {}
): PermissionRecordsListResponse {
  return {
    success: true,
    totalCount: 1,
    page: 1,
    pageSize: 25,
    rows: [
      {
        id: "42",
        locationGuestId: 7,
        locationId: 3,
        guestName: "Amira Khan",
        permissionId: "email-marketing",
        permissionLabel: "Email marketing",
        currentState: "granted",
        locationLabel: "Camden",
        sourceLabel: "Guest form",
        recordedAt: "2026-08-22T14:26:00.000Z",
      },
    ],
    ...overrides,
  }
}

function adapters(overrides: Partial<Parameters<typeof createOperatorPrivacyConsentPageModule>[0]> = {}) {
  return {
    getPage: vi.fn(async () => pageData()),
    patchToggles: vi.fn(async () => {}),
    getPermissionRecords: vi.fn(async () => recordsResponse()),
    getActivity: vi.fn(async () => ({
      items: [
        {
          id: 1,
          locationId: null,
          kind: "consent-copy-changed",
          description: "James updated SMS consent wording.",
          occurredAt: "2026-08-31T10:42:00.000Z",
        },
      ],
    })),
    getLocationFilterOptions: () => [{ id: "3", label: "Camden" }],
    ...overrides,
  }
}

describe("createOperatorPrivacyConsentPageModule", () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it("starts on Privacy setup with Figma status rows in demo mode", () => {
    const pageModule = createOperatorPrivacyConsentPageModule(adapters(), {
      demo: {},
    })
    const snap = pageModule.getSnapshot()

    expect(snap.activeTabId).toBe("privacy-setup")
    expect(snap.tabs.map((tab) => tab.label)).toEqual([
      "Privacy setup",
      "Guest permissions",
      "Permission records",
      "Activity",
    ])
    expect(snap.privacySetupRows).toHaveLength(5)
    expect(snap.privacySetupRows[0]).toMatchObject({
      requirement: "Privacy notice",
      status: "Configured",
    })
    expect(snap.guestPermissions).toHaveLength(3)
    expect(snap.guestPermissions[0]).toMatchObject({
      title: "Email marketing",
      enabled: true,
    })
  })

  it("maps API page data into the snapshot without demo rows", async () => {
    const api = adapters({
      debounceMs: 0,
      getNow: () => new Date("2026-08-31T15:00:00.000Z"),
    })
    const pageModule = createOperatorPrivacyConsentPageModule(api)
    await pageModule.load()
    const snap = pageModule.getSnapshot()

    expect(snap.loadStatus).toBe("loaded")
    expect(snap.privacySetupRows).toEqual([
      {
        id: "privacy-notice",
        requirement: "Privacy notice",
        status: "Configured",
      },
      {
        id: "email-marketing",
        requirement: "Email marketing",
        status: "Enabled",
      },
    ])
    expect(snap.guestPermissions.find((card) => card.id === "sms-marketing")?.enabled).toBe(false)
    expect(snap.permissionRecordsRows).toEqual([
      expect.objectContaining({
        guestName: "Amira Khan",
        locationGuestId: 7,
        locationId: "3",
      }),
    ])
    expect(snap.activityItems[0]).toMatchObject({
      description: "James updated SMS consent wording.",
    })
    expect(api.getPage).toHaveBeenCalledTimes(1)
    expect(api.getPermissionRecords).toHaveBeenCalledTimes(1)
    expect(api.getActivity).toHaveBeenCalledTimes(1)
  })

  it("honours initial tab from the URL", () => {
    const pageModule = createOperatorPrivacyConsentPageModule(adapters(), {
      demo: {},
      initialTabId: "activity",
    })
    expect(pageModule.getSnapshot().activeTabId).toBe("activity")
  })

  it("updates the active tab and notifies subscribers", () => {
    const pageModule = createOperatorPrivacyConsentPageModule(adapters(), {
      demo: {},
    })
    let calls = 0
    pageModule.subscribe(() => {
      calls += 1
    })

    pageModule.requestTabChange("guest-permissions")
    expect(pageModule.getSnapshot().activeTabId).toBe("guest-permissions")
    expect(calls).toBe(1)

    pageModule.setActiveTabFromUrl("permission-records")
    expect(pageModule.getSnapshot().activeTabId).toBe("permission-records")
    expect(calls).toBe(2)

    pageModule.setActiveTabFromUrl("permission-records")
    expect(calls).toBe(2)
  })

  it("persists toggle changes via PATCH when actor can manage", async () => {
    const getPage = vi
      .fn()
      .mockResolvedValueOnce(pageData())
      .mockResolvedValueOnce(pageData({ smsMarketingPermissionEnabled: true }))
    const api = adapters({ getPage })
    const pageModule = createOperatorPrivacyConsentPageModule(api)
    await pageModule.load()

    await pageModule.setGuestPermissionEnabled("sms-marketing", true)

    expect(api.patchToggles).toHaveBeenCalledWith({
      smsMarketingPermissionEnabled: true,
    })
    expect(
      pageModule.getSnapshot().guestPermissions.find(
        (card) => card.id === "sms-marketing"
      )?.enabled
    ).toBe(true)
  })

  it("keeps guest permission switches read-only when actor cannot manage", async () => {
    const api = adapters({
      getPage: vi.fn(async () => pageData({ actorCanManage: false })),
    })
    const pageModule = createOperatorPrivacyConsentPageModule(api)
    await pageModule.load()

    await pageModule.setGuestPermissionEnabled("sms-marketing", true)

    expect(api.patchToggles).not.toHaveBeenCalled()
    expect(
      pageModule.getSnapshot().guestPermissions.find(
        (card) => card.id === "sms-marketing"
      )?.enabled
    ).toBe(false)
  })

  it("maps permission-records filters to list query params", async () => {
    vi.useFakeTimers()
    const api = adapters({ debounceMs: 300 })
    const pageModule = createOperatorPrivacyConsentPageModule(api)
    await pageModule.load()

    pageModule.openPermissionRecordsFilters()
    pageModule.setPermissionRecordsSearchQuery("amira")
    vi.advanceTimersByTime(300)
    await Promise.resolve()
    await Promise.resolve()

    expect(api.getPermissionRecords).toHaveBeenLastCalledWith(
      expect.objectContaining({ q: "amira", page: 1 })
    )
  })

  it("disables guest profile navigation without Guests View", async () => {
    const navigateToGuestProfile = vi.fn()
    const api = adapters({
      getPage: vi.fn(async () => pageData({ canViewGuests: false })),
      navigateToGuestProfile,
    })
    const pageModule = createOperatorPrivacyConsentPageModule(api)
    await pageModule.load()

    pageModule.viewPermissionRecord("42")
    expect(navigateToGuestProfile).not.toHaveBeenCalled()
    expect(pageModule.getSnapshot().canViewGuests).toBe(false)
  })

  it("navigates to guest profile when Guests View is allowed", async () => {
    const navigateToGuestProfile = vi.fn()
    const api = adapters({ navigateToGuestProfile })
    const pageModule = createOperatorPrivacyConsentPageModule(api)
    await pageModule.load()

    pageModule.viewPermissionRecord("42")
    expect(navigateToGuestProfile).toHaveBeenCalledWith(7, 3)
  })

  it("seeds Permission records with Figma filters and rows in demo mode", () => {
    const pageModule = createOperatorPrivacyConsentPageModule(adapters(), {
      demo: {},
    })
    const snap = pageModule.getSnapshot()

    expect(snap.permissionRecordsFilterChipCount).toBe(3)
    expect(snap.permissionRecordsFilterChips.map((chip) => chip.label)).toEqual(
      ["Eligible to contact", "Negative", "Camden"]
    )
    expect(snap.permissionRecordsRows.map((row) => row.guestName)).toEqual([
      "Amira Khan",
      "Liam Chen",
      "Sophia Martinez",
    ])
  })

  it("filters Permission records by guest search in demo mode", () => {
    const pageModule = createOperatorPrivacyConsentPageModule(adapters(), {
      demo: {},
    })
    pageModule.setPermissionRecordsSearchQuery("liam")
    expect(
      pageModule.getSnapshot().permissionRecordsRows.map((row) => row.guestName)
    ).toEqual(["Liam Chen"])
  })

  it("clears Permission records search and filters in demo mode", () => {
    const pageModule = createOperatorPrivacyConsentPageModule(adapters(), {
      demo: {},
    })
    pageModule.setPermissionRecordsSearchQuery("amira")
    pageModule.clearPermissionRecordsSearchAndFilters()
    const snap = pageModule.getSnapshot()
    expect(snap.permissionRecordsSearchQuery).toBe("")
    expect(snap.permissionRecordsFilterChipCount).toBe(0)
    expect(snap.permissionRecordsRows).toHaveLength(3)
  })

  it("shows empty activity state when API returns no rows", async () => {
    const api = adapters({
      getActivity: vi.fn(async () => ({ items: [] })),
    })
    const pageModule = createOperatorPrivacyConsentPageModule(api)
    await pageModule.load()
    expect(pageModule.getSnapshot().activityItems).toEqual([])
  })
})

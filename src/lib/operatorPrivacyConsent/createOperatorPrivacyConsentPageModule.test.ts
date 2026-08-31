import { describe, expect, it } from "vitest"

import { createOperatorPrivacyConsentPageModule } from "@/lib/operatorPrivacyConsent/createOperatorPrivacyConsentPageModule"

describe("createOperatorPrivacyConsentPageModule", () => {
  it("starts on Privacy setup with Figma status rows", () => {
    const pageModule = createOperatorPrivacyConsentPageModule()
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

  it("honours initial tab from the URL", () => {
    const pageModule = createOperatorPrivacyConsentPageModule({
      initialTabId: "activity",
    })
    expect(pageModule.getSnapshot().activeTabId).toBe("activity")
  })

  it("updates the active tab and notifies subscribers", () => {
    const pageModule = createOperatorPrivacyConsentPageModule()
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

  it("toggles a guest permission and notifies subscribers", () => {
    const pageModule = createOperatorPrivacyConsentPageModule()
    let calls = 0
    pageModule.subscribe(() => {
      calls += 1
    })

    pageModule.setGuestPermissionEnabled("sms-marketing", false)
    expect(
      pageModule.getSnapshot().guestPermissions.find(
        (card) => card.id === "sms-marketing"
      )?.enabled
    ).toBe(false)
    expect(calls).toBe(1)

    pageModule.setGuestPermissionEnabled("sms-marketing", false)
    expect(calls).toBe(1)
  })

  it("seeds Permission records with Figma filters and rows", () => {
    const pageModule = createOperatorPrivacyConsentPageModule()
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

  it("filters Permission records by guest search", () => {
    const pageModule = createOperatorPrivacyConsentPageModule()
    pageModule.setPermissionRecordsSearchQuery("liam")
    expect(
      pageModule.getSnapshot().permissionRecordsRows.map((row) => row.guestName)
    ).toEqual(["Liam Chen"])
  })

  it("clears Permission records search and filters", () => {
    const pageModule = createOperatorPrivacyConsentPageModule()
    pageModule.setPermissionRecordsSearchQuery("amira")
    pageModule.clearPermissionRecordsSearchAndFilters()
    const snap = pageModule.getSnapshot()
    expect(snap.permissionRecordsSearchQuery).toBe("")
    expect(snap.permissionRecordsFilterChipCount).toBe(0)
    expect(snap.permissionRecordsRows).toHaveLength(3)
  })

  it("exposes Figma Privacy activity items", () => {
    const pageModule = createOperatorPrivacyConsentPageModule()
    const snap = pageModule.getSnapshot()
    expect(snap.activityItems).toHaveLength(5)
    expect(snap.activityItems[3]).toMatchObject({
      timeLabel: "Today, 10:42",
      description: "James updated SMS consent wording.",
    })
  })
})

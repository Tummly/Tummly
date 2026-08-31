import { describe, expect, it } from "vitest"

import {
  GUEST_PERMISSIONS_DEMO_CARDS,
  PERMISSION_RECORDS_DEMO_ROWS,
  PRIVACY_ACTIVITY_DEMO_ITEMS,
  PRIVACY_CONSENT_TAB_IDS,
  PRIVACY_CONSENT_TAB_LABELS,
  PRIVACY_SETUP_STATUS_DEMO_ROWS,
  guestPermissionStatusLabel,
  resolvePrivacyConsentTabId,
} from "@/lib/operatorPrivacyConsent/privacyConsentPresentation"

describe("resolvePrivacyConsentTabId", () => {
  it("accepts known tab ids", () => {
    expect(resolvePrivacyConsentTabId("privacy-setup")).toBe("privacy-setup")
    expect(resolvePrivacyConsentTabId("guest-permissions")).toBe(
      "guest-permissions"
    )
    expect(resolvePrivacyConsentTabId("permission-records")).toBe(
      "permission-records"
    )
    expect(resolvePrivacyConsentTabId("activity")).toBe("activity")
  })

  it("defaults unknown or empty values to Privacy setup", () => {
    expect(resolvePrivacyConsentTabId(null)).toBe("privacy-setup")
    expect(resolvePrivacyConsentTabId(undefined)).toBe("privacy-setup")
    expect(resolvePrivacyConsentTabId("")).toBe("privacy-setup")
    expect(resolvePrivacyConsentTabId("other")).toBe("privacy-setup")
  })
})

describe("PRIVACY_CONSENT_TAB_LABELS", () => {
  it("matches Figma header tab labels", () => {
    expect(PRIVACY_CONSENT_TAB_IDS.map((id) => PRIVACY_CONSENT_TAB_LABELS[id])).toEqual([
      "Privacy setup",
      "Guest permissions",
      "Permission records",
      "Activity",
    ])
  })
})

describe("PRIVACY_SETUP_STATUS_DEMO_ROWS", () => {
  it("matches Figma Privacy setup status rows", () => {
    expect(
      PRIVACY_SETUP_STATUS_DEMO_ROWS.map((row) => [row.requirement, row.status])
    ).toEqual([
      ["Privacy notice", "Configured"],
      ["Guest permission wording", "Configured"],
      ["Email marketing", "Enabled"],
      ["SMS marketing", "Not used"],
      ["Feedback follow-up", "Enabled"],
    ])
  })
})

describe("GUEST_PERMISSIONS_DEMO_CARDS", () => {
  it("matches Figma Guest permissions tiles", () => {
    expect(
      GUEST_PERMISSIONS_DEMO_CARDS.map((card) => [
        card.title,
        card.usedIn,
        card.enabled,
      ])
    ).toEqual([
      ["Email marketing", "Email Campaigns", true],
      ["SMS marketing", "SMS Campaigns", true],
      ["Feedback follow-up", "Private Feedback follow-up", true],
    ])
  })
})

describe("guestPermissionStatusLabel", () => {
  it("maps enabled state to status chip copy", () => {
    expect(guestPermissionStatusLabel(true)).toBe("Enabled")
    expect(guestPermissionStatusLabel(false)).toBe("Not used")
  })
})

describe("PERMISSION_RECORDS_DEMO_ROWS", () => {
  it("matches Figma Permission records rows", () => {
    expect(
      PERMISSION_RECORDS_DEMO_ROWS.map((row) => [
        row.guestName,
        row.permissionLabel,
        row.currentState,
      ])
    ).toEqual([
      ["Amira Khan", "Email marketing", "granted"],
      ["Liam Chen", "SMS marketing", "withdrawn"],
      ["Sophia Martinez", "Feedback follow-up", "granted"],
    ])
  })
})

describe("PRIVACY_ACTIVITY_DEMO_ITEMS", () => {
  it("matches Figma Privacy activity rows", () => {
    expect(
      PRIVACY_ACTIVITY_DEMO_ITEMS.map((item) => [
        item.timeLabel,
        item.description,
      ])
    ).toEqual([
      ["24 Aug 2026, 10:42", "James updated SMS marketing wording"],
      ["24 Aug 2026, 10:42", "James updated SMS marketing wording"],
      ["24 Aug 2026, 10:42", "James updated SMS marketing wording"],
      ["Today, 10:42", "James updated SMS consent wording."],
      ["24 Aug 2026, 10:42", "James updated SMS marketing wording"],
    ])
  })
})

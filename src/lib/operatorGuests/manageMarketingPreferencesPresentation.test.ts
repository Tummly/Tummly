import { describe, expect, it } from "vitest"

import {
  MANAGE_MARKETING_PREFERENCES_COPY,
  MARKETING_PREFERENCE_STATUS_CARDS,
  channelHasContact,
  formatMarketingPreferenceRecordedOn,
  isMarketingPreferenceSaveDirty,
  marketingPreferenceConsequenceHelper,
  marketingPreferencePermissionSource,
  operatorMaySelectMarketingPreference,
  resolveMarketingPreferenceConsentAt,
} from "./manageMarketingPreferencesPresentation"

describe("manageMarketingPreferencesPresentation", () => {
  it("uses Figma dialog copy and Allowed / Opted out / Not recorded card labels", () => {
    expect(MANAGE_MARKETING_PREFERENCES_COPY.dialogTitle).toBe(
      "Manage marketing preferences"
    )
    expect(MANAGE_MARKETING_PREFERENCES_COPY.subtitle("Amelia Hart")).toBe(
      "Choose how Amelia Hart may receive marketing. Transactional messages are not affected."
    )
    expect(MANAGE_MARKETING_PREFERENCES_COPY.saveLabel).toBe("Save preferences")
    expect(MANAGE_MARKETING_PREFERENCES_COPY.cancelLabel).toBe("Cancel")
    expect(MANAGE_MARKETING_PREFERENCES_COPY.notePlaceholder).toBe(
      "Why was this preference changed?"
    )
    expect(MARKETING_PREFERENCE_STATUS_CARDS.map((card) => card.id)).toEqual([
      "allowed",
      "opted_out",
      "not_recorded",
    ])
    expect(MARKETING_PREFERENCE_STATUS_CARDS.map((card) => card.label)).toEqual([
      "Allowed",
      "Opted out",
      "Not recorded",
    ])
    expect(MARKETING_PREFERENCE_STATUS_CARDS.map((card) => card.helper)).toEqual(
      [
        "May receive campaigns",
        "Suppress all marketing",
        "Treat as ineligible",
      ]
    )
  })

  it("allows keep or Opted out / Not recorded from Allowed, and blocks Allowed from other statuses", () => {
    expect(operatorMaySelectMarketingPreference("allowed", "allowed")).toBe(
      true
    )
    expect(operatorMaySelectMarketingPreference("allowed", "opted_out")).toBe(
      true
    )
    expect(
      operatorMaySelectMarketingPreference("allowed", "not_recorded")
    ).toBe(true)
    expect(operatorMaySelectMarketingPreference("opted_out", "allowed")).toBe(
      false
    )
    expect(
      operatorMaySelectMarketingPreference("opted_out", "opted_out")
    ).toBe(true)
    expect(
      operatorMaySelectMarketingPreference("opted_out", "not_recorded")
    ).toBe(true)
    expect(
      operatorMaySelectMarketingPreference("not_recorded", "allowed")
    ).toBe(false)
    expect(
      operatorMaySelectMarketingPreference("not_recorded", "opted_out")
    ).toBe(true)
    expect(
      operatorMaySelectMarketingPreference("not_recorded", "not_recorded")
    ).toBe(true)
  })

  it("marks Email / SMS available only when that contact exists", () => {
    expect(channelHasContact("amelia@example.com")).toBe(true)
    expect(channelHasContact("  ")).toBe(false)
    expect(channelHasContact(null)).toBe(false)
  })

  it("shows Guest feedback form and DD/MM/YYYY when a consent timestamp exists", () => {
    expect(
      marketingPreferencePermissionSource("2026-08-06T10:00:00.000Z")
    ).toBe("Guest feedback form")
    expect(
      formatMarketingPreferenceRecordedOn("2026-08-06T10:00:00.000Z")
    ).toBe("06/08/2026")
    expect(marketingPreferencePermissionSource(null)).toBe("—")
    expect(formatMarketingPreferenceRecordedOn(null)).toBe("—")
  })

  it("keeps the first consent timestamp from contact eligibility as evidence", () => {
    expect(
      resolveMarketingPreferenceConsentAt([
        {
          channel: "email",
          status: "eligible",
          detailKind: "consent_captured",
          detailAt: "2026-08-06T10:00:00.000Z",
        },
        {
          channel: "sms",
          status: "not_provided",
          detailKind: null,
          detailAt: null,
        },
      ])
    ).toBe("2026-08-06T10:00:00.000Z")
    expect(
      resolveMarketingPreferenceConsentAt([
        {
          channel: "email",
          status: "not_recorded",
          detailKind: "not_recorded",
          detailAt: null,
        },
      ])
    ).toBe(null)
  })

  it("shows Opted out / Not recorded consequence copy and none under Allowed", () => {
    expect(marketingPreferenceConsequenceHelper("allowed")).toBe(null)
    expect(marketingPreferenceConsequenceHelper("opted_out")).toBe(
      "This guest will be added to the marketing suppression list."
    )
    expect(marketingPreferenceConsequenceHelper("not_recorded")).toBe(
      "Without recorded permission, this guest is treated as ineligible."
    )
  })

  it("enables Save when status changed or the note is non-empty", () => {
    expect(
      isMarketingPreferenceSaveDirty("allowed", "allowed", "")
    ).toBe(false)
    expect(
      isMarketingPreferenceSaveDirty("allowed", "allowed", "   ")
    ).toBe(false)
    expect(
      isMarketingPreferenceSaveDirty("allowed", "opted_out", "")
    ).toBe(true)
    expect(
      isMarketingPreferenceSaveDirty("allowed", "allowed", " Guest asked. ")
    ).toBe(true)
  })
})

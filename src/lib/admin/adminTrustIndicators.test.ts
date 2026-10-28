import { describe, expect, it } from "vitest"

import type {
  AdminTrustIndicatorItem,
  TrustIndicatorValues,
} from "@/api/adminTrustIndicatorsApi"

import {
  describeTrustValue,
  diffTrustIndicatorValues,
  toTrustSaveValues,
  trustDraftChanges,
  trustItemPublishState,
} from "./adminTrustIndicators"

function values(overrides: Partial<TrustIndicatorValues> = {}): TrustIndicatorValues {
  return {
    enabled: false,
    ctaType: "None",
    ctaTarget: null,
    evidenceUrl: null,
    imageUrl: null,
    iconKey: null,
    altText: "",
    tooltip: "",
    issuer: null,
    scope: null,
    issuedOn: null,
    expiresOn: null,
    reviewOn: null,
    ...overrides,
  }
}

function item(
  itemNumber: number,
  overrides: Partial<TrustIndicatorValues> = {}
): AdminTrustIndicatorItem {
  return {
    itemNumber,
    title: `Item ${itemNumber}`,
    evidenceClass: "Roadmap",
    values: values(overrides),
    lastEditedByAdminUserId: null,
    lastEditedAtUtc: null,
  }
}

describe("diffTrustIndicatorValues", () => {
  it("returns nothing when draft and published values match", () => {
    expect(diffTrustIndicatorValues(values(), values())).toEqual([])
  })

  it("lists each changed field with its published and draft text", () => {
    const diff = diffTrustIndicatorValues(
      values({ enabled: true, ctaType: "ExternalProof", evidenceUrl: "https://a.example" }),
      values({ enabled: false })
    )

    expect(diff).toEqual([
      { field: "enabled", label: "Shown on website", published: "Off", draft: "On" },
      {
        field: "ctaType",
        label: "CTA type",
        published: "No CTA",
        draft: "External proof link",
      },
      {
        field: "evidenceUrl",
        label: "Evidence URL",
        published: "—",
        draft: "https://a.example",
      },
    ])
  })
})

describe("trustItemPublishState", () => {
  it("marks an item as not published before any publication", () => {
    expect(trustItemPublishState(item(1), undefined)).toBe("not_published")
  })

  it("marks an item that matches the live version as unchanged", () => {
    expect(trustItemPublishState(item(1), item(1))).toBe("unchanged")
  })

  it("marks an item whose draft differs from the live version as changed", () => {
    expect(trustItemPublishState(item(1, { tooltip: "New" }), item(1))).toBe("changed")
  })
})

describe("trustDraftChanges", () => {
  it("returns only draft items that differ from their live version", () => {
    const draft = [item(1, { enabled: true }), item(2), item(3)]
    const live = [item(1), item(2), item(3)]

    const changes = trustDraftChanges(draft, live)

    expect(changes.map((row) => row.item.itemNumber)).toEqual([1])
    expect(changes[0].differences.map((row) => row.field)).toEqual(["enabled"])
  })

  it("returns nothing when no version is live", () => {
    expect(trustDraftChanges([item(1, { enabled: true })], null)).toEqual([])
  })
})

describe("toTrustSaveValues", () => {
  it("turns blank optional fields and dates into null, and trims text", () => {
    const saved = toTrustSaveValues(
      values({
        evidenceUrl: "   ",
        issuer: "  ICO  ",
        issuedOn: "",
        expiresOn: "2027-01-31",
        altText: "  Badge  ",
        tooltip: "  ",
      })
    )

    expect(saved.evidenceUrl).toBeNull()
    expect(saved.issuer).toBe("ICO")
    expect(saved.issuedOn).toBeNull()
    expect(saved.expiresOn).toBe("2027-01-31")
    expect(saved.altText).toBe("Badge")
    expect(saved.tooltip).toBe("")
  })
})

describe("describeTrustValue", () => {
  it("formats the switch, empty values and CTA labels for people", () => {
    expect(describeTrustValue("enabled", true)).toBe("On")
    expect(describeTrustValue("enabled", false)).toBe("Off")
    expect(describeTrustValue("scope", "")).toBe("—")
    expect(describeTrustValue("ctaType", "InternalPage")).toBe("Internal page")
  })
})

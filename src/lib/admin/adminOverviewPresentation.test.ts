import { describe, expect, it } from "vitest"

import type { AdminOverview } from "@/types/adminOps"

import {
  adminOverviewAsOfText,
  buildAdminOverviewCards,
  buildAdminProviderReadinessRows,
  hasUnavailableAdminOverviewCounts,
  UNAVAILABLE_COUNT_TEXT,
} from "./adminOverviewPresentation"

const SUPPORT_URL = "/admin-dashboard/support"

function overview(overrides: Partial<AdminOverview> = {}): AdminOverview {
  return {
    generatedAtUtc: "2026-10-28T09:00:00Z",
    workspacesByBillingStatus: {
      available: true,
      value: [
        { billingStatus: "Active", count: 3 },
        { billingStatus: "Free", count: 4 },
      ],
    },
    shopOrdersPaidNotDispatched: { available: true, value: 2 },
    shopOrdersFlaggedForReview: { available: true, value: 0 },
    printAssetsFailed: { available: true, value: 1 },
    pilotsEndingWithin7Days: { available: true, value: 5 },
    openHelpCentreQueries: { available: true, value: 7 },
    failedAuditEventsLast7Days: { available: true, value: 0 },
    ...overrides,
  }
}

function card(cards: ReturnType<typeof buildAdminOverviewCards>, id: string) {
  const found = cards.find((item) => item.id === id)
  if (!found) throw new Error(`missing card ${id}`)
  return found
}

describe("buildAdminOverviewCards", () => {
  it("shows an unavailable count as Unavailable, never as zero", () => {
    const cards = buildAdminOverviewCards(
      overview({ shopOrdersPaidNotDispatched: { available: false, value: null } }),
      SUPPORT_URL
    )

    const dispatch = card(cards, "shop-to-dispatch")
    expect(dispatch.valueText).toBe(UNAVAILABLE_COUNT_TEXT)
    expect(dispatch.available).toBe(false)
  })

  it("sums workspaces across billing statuses and lists each status", () => {
    const workspaces = card(buildAdminOverviewCards(overview(), SUPPORT_URL), "workspaces")

    expect(workspaces.valueText).toBe("7")
    expect(workspaces.detailText).toBe("Active 3 · Free 4")
    expect(workspaces.target).toEqual({ kind: "tab", tab: "customers" })
  })

  it("links each count to an existing tab or the support route", () => {
    const cards = buildAdminOverviewCards(overview(), SUPPORT_URL)

    expect(card(cards, "shop-to-dispatch").target).toEqual({ kind: "tab", tab: "orders" })
    expect(card(cards, "pilots-ending").target).toEqual({ kind: "tab", tab: "pilots" })
    expect(card(cards, "audit-failed").target).toEqual({ kind: "tab", tab: "audit" })
    expect(card(cards, "support-open").target).toEqual({ kind: "route", to: SUPPORT_URL })
  })

  it("formats counts with thousands separators", () => {
    const cards = buildAdminOverviewCards(
      overview({ openHelpCentreQueries: { available: true, value: 12345 } }),
      SUPPORT_URL
    )

    expect(card(cards, "support-open").valueText).toBe("12,345")
  })
})

describe("hasUnavailableAdminOverviewCounts", () => {
  it("is false when every count loaded", () => {
    expect(hasUnavailableAdminOverviewCounts(overview())).toBe(false)
  })

  it("is true when any count failed", () => {
    expect(
      hasUnavailableAdminOverviewCounts(
        overview({ pilotsEndingWithin7Days: { available: false, value: null } })
      )
    ).toBe(true)
  })
})

describe("adminOverviewAsOfText", () => {
  it("labels the freshness time in Europe/London", () => {
    expect(adminOverviewAsOfText("2026-10-28T09:00:00Z")).toBe(
      "As of 28 Oct 2026, 09:00 (Europe/London)"
    )
  })
})

describe("buildAdminProviderReadinessRows", () => {
  it("labels configuration only and never says healthy", () => {
    const rows = buildAdminProviderReadinessRows({
      environmentName: "QA",
      isProduction: false,
      commitSha: "unknown",
      apiStartedAtUtc: "2026-10-28T08:00:00Z",
      latestMigrationId: null,
      revolut: { status: "configured", mode: "sandbox" },
      resend: { status: "not_configured", senderDomain: null },
      twilio: { status: "configured", fromNumberMasked: "•••• 1234" },
      vatModeActive: false,
    })

    expect(rows.map((row) => [row.id, row.statusText, row.detailText])).toEqual([
      ["revolut", "Configured", "sandbox"],
      ["resend", "Not configured", null],
      ["twilio", "Configured", "•••• 1234"],
    ])
    expect(rows.some((row) => /healthy/i.test(row.statusText))).toBe(false)
  })
})

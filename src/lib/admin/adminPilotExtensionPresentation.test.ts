import { describe, expect, it } from "vitest"

import {
  buildPilotRowPresentation,
  pilotAllowanceRows,
  pilotExtendedToastMessage,
  pilotExtensionHistoryRows,
  pilotExtensionReasonError,
  previewPilotExtension,
  replacePilotAccountRow,
} from "@/lib/admin/adminPilotExtensionPresentation"
import type { AdminPilotAccount } from "@/types/admin"

const NOW = new Date("2026-10-26T12:00:00Z")

function account(overrides: Partial<AdminPilotAccount> = {}): AdminPilotAccount {
  return {
    restaurantId: 7,
    ownerUserId: 42,
    restaurantName: "Harbour Kitchen",
    ownerEmail: "owner@example.com",
    billingStatus: "Pilot",
    pilotStartedAtUtc: "2026-10-01T09:30:00Z",
    pilotOriginalEndUtc: "2026-10-31T12:00:00Z",
    pilotPeriodEnd: "2026-10-31T12:00:00Z",
    daysUsed: 25,
    hasPaidHistory: false,
    extensionCount: 0,
    extensionDaysTotal: 0,
    extensionHistory: [],
    allowances: [],
    canExtend: true,
    cannotExtendReason: null,
    ...overrides,
  }
}

describe("buildPilotRowPresentation", () => {
  it("allows extension and shows zero usage for a fresh Pilot", () => {
    const row = buildPilotRowPresentation(account(), NOW)

    expect(row.canExtend).toBe(true)
    expect(row.disabledReason).toBeNull()
    expect(row.extensionsLabel).toBe("0 of 2 · 0 of 90 days")
    expect(row.startLabel).toBe("1 Oct 2026")
    expect(row.originalEndLabel).toBe("31 Oct 2026")
    expect(row.endLabel).toBe("31 Oct 2026")
  })

  it("shows the original end apart from an extended effective end", () => {
    const row = buildPilotRowPresentation(
      account({
        pilotOriginalEndUtc: "2026-10-31T12:00:00Z",
        pilotPeriodEnd: "2026-11-14T12:00:00Z",
        extensionCount: 1,
        extensionDaysTotal: 14,
      }),
      NOW
    )

    expect(row.originalEndLabel).toBe("31 Oct 2026")
    expect(row.endLabel).toBe("14 Nov 2026")
  })

  it("passes the server reason through when extension is refused", () => {
    const reason =
      "This Pilot has used its extensions. The limit is 2 extensions and 90 days in total."
    const row = buildPilotRowPresentation(
      account({
        extensionCount: 2,
        extensionDaysTotal: 90,
        canExtend: false,
        cannotExtendReason: reason,
      }),
      NOW
    )

    expect(row.canExtend).toBe(false)
    expect(row.disabledReason).toBe(reason)
    expect(row.extensionsLabel).toBe("2 of 2 · 90 of 90 days")
  })

  it("shows a dash for missing dates", () => {
    const row = buildPilotRowPresentation(
      account({
        pilotStartedAtUtc: null,
        pilotOriginalEndUtc: null,
        pilotPeriodEnd: null,
      }),
      NOW
    )

    expect(row.startLabel).toBe("—")
    expect(row.originalEndLabel).toBe("—")
    expect(row.endLabel).toBe("—")
  })
})

describe("previewPilotExtension", () => {
  it("counts from the current end while the Pilot is still running", () => {
    const preview = previewPilotExtension(account(), 14, NOW)

    expect(preview.countsFrom).toBe("current-end")
    expect(preview.newEnd.toISOString()).toBe("2026-11-14T12:00:00.000Z")
    expect(preview.newEndLabel).toBe("14 Nov 2026")
  })

  it("counts from now when the effective end has passed", () => {
    const preview = previewPilotExtension(
      account({ pilotPeriodEnd: "2026-10-20T12:00:00Z" }),
      30,
      NOW
    )

    expect(preview.countsFrom).toBe("now")
    expect(preview.newEnd.toISOString()).toBe("2026-11-25T12:00:00.000Z")
    expect(preview.newEndLabel).toBe("25 Nov 2026")
  })

  it("counts from now when there is no effective end", () => {
    const preview = previewPilotExtension(
      account({ pilotPeriodEnd: null }),
      14,
      NOW
    )

    expect(preview.countsFrom).toBe("now")
    expect(preview.newEnd.toISOString()).toBe("2026-11-09T12:00:00.000Z")
  })

  it("treats an end equal to now as passed", () => {
    const preview = previewPilotExtension(
      account({ pilotPeriodEnd: NOW.toISOString() }),
      14,
      NOW
    )

    expect(preview.countsFrom).toBe("now")
  })
})

describe("pilotExtensionReasonError", () => {
  it("rejects blank and too-short reasons after trimming", () => {
    expect(pilotExtensionReasonError("")).not.toBe("")
    expect(pilotExtensionReasonError("     ")).not.toBe("")
    expect(pilotExtensionReasonError("  abcd  ")).not.toBe("")
  })

  it("accepts five characters and exactly 500", () => {
    expect(pilotExtensionReasonError("abcde")).toBe("")
    expect(pilotExtensionReasonError("r".repeat(500))).toBe("")
  })

  it("rejects 501 characters", () => {
    expect(pilotExtensionReasonError("r".repeat(501))).not.toBe("")
  })
})

describe("pilotAllowanceRows", () => {
  it("lists AI, Email and SMS in a fixed order", () => {
    const rows = pilotAllowanceRows(
      account({
        allowances: [
          { channel: "sms", granted: 100, used: 4, remaining: 96 },
          { channel: "ai", granted: 500, used: 20, remaining: 480 },
          { channel: "email", granted: 5000, used: 0, remaining: 5000 },
        ],
      })
    )

    expect(rows?.map((row) => row.label)).toEqual(["AI", "Email", "SMS"])
    expect(rows?.[0]).toEqual({
      channel: "ai",
      label: "AI",
      granted: 500,
      used: 20,
      remaining: 480,
    })
  })

  it("returns null when the balance is unavailable", () => {
    expect(pilotAllowanceRows(account({ allowances: null }))).toBeNull()
  })
})

describe("pilotExtensionHistoryRows", () => {
  it("shows a placeholder for extensions recorded without a reason", () => {
    const rows = pilotExtensionHistoryRows(
      account({
        extensionHistory: [
          {
            occurredAtUtc: "2026-10-20T09:00:00Z",
            days: 14,
            actorIdentity: "admin@tummly.com",
            reason: null,
          },
        ],
      })
    )

    expect(rows).toEqual([
      {
        dateLabel: "20 Oct 2026",
        days: 14,
        actor: "admin@tummly.com",
        reason: "No reason recorded",
      },
    ])
  })
})

describe("replacePilotAccountRow", () => {
  it("replaces only the row with the same restaurantId", () => {
    const first = account({ restaurantId: 7, ownerUserId: 42 })
    const second = account({ restaurantId: 9, ownerUserId: 42 })
    const updated = account({
      restaurantId: 9,
      ownerUserId: 42,
      extensionCount: 1,
      extensionDaysTotal: 14,
    })

    const rows = replacePilotAccountRow([first, second], updated)

    expect(rows[0]).toBe(first)
    expect(rows[1]).toEqual(updated)
  })
})

describe("pilotExtendedToastMessage", () => {
  it("states the new end from the server response", () => {
    expect(pilotExtendedToastMessage(account())).toBe(
      "Pilot extended. New end: 31 Oct 2026."
    )
  })

  it("omits the end when the server sends no end date", () => {
    expect(
      pilotExtendedToastMessage(account({ pilotPeriodEnd: null }))
    ).toBe("Pilot extended.")
  })
})

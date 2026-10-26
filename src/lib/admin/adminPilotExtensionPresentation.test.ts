import { describe, expect, it } from "vitest"

import {
  buildPilotRowPresentation,
  pilotExtendedToastMessage,
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
    pilotPeriodEnd: "2026-10-31T12:00:00Z",
    extensionCount: 0,
    extensionDaysTotal: 0,
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
    expect(row.endLabel).toBe("31 Oct 2026")
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
      account({ pilotStartedAtUtc: null, pilotPeriodEnd: null }),
      NOW
    )

    expect(row.startLabel).toBe("—")
    expect(row.endLabel).toBe("—")
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

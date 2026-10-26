import { readFileSync } from "node:fs"
import { dirname, join } from "node:path"
import { fileURLToPath } from "node:url"
import { describe, expect, it } from "vitest"

const guestsDir = dirname(fileURLToPath(import.meta.url))

function readGuestsSource(fileName: string): string {
  return readFileSync(join(guestsDir, fileName), "utf8")
}

describe("guest contact chrome (WS2b masked contact)", () => {
  it("renders the guest table contact as plain text, not mailto or tel links", () => {
    const table = readGuestsSource("GuestsSmartGroupsSection.tsx")

    expect(table).not.toMatch(/mailto:|tel:/)
    expect(table).not.toMatch(/formatPhoneForDisplay/)
  })

  it("renders the guest drawer contact without an href", () => {
    const drawer = readGuestsSource("GuestDetailsDrawer.tsx")

    expect(drawer).not.toMatch(/mailto:|tel:/)
    expect(drawer).not.toMatch(/details\.email\b/)
  })

  it("tells the operator that contact search needs the full value", () => {
    const table = readGuestsSource("GuestsSmartGroupsSection.tsx")

    expect(table).toMatch(
      /placeholder="Search by name, or full email or mobile number"/
    )
    expect(table).not.toMatch(/placeholder="Search guests by name, email or mobile number"/)
  })
})

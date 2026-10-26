import { readFileSync } from "node:fs"
import { dirname, join } from "node:path"
import { fileURLToPath } from "node:url"
import { describe, expect, it } from "vitest"

const sidebarSource = readFileSync(
  join(dirname(fileURLToPath(import.meta.url)), "DashboardSidebar.tsx"),
  "utf8"
)

describe("DashboardSidebar Settings group (R7.1)", () => {
  it("gates the whole Settings group, collapsed and expanded, on visible children", () => {
    expect(sidebarSource).toMatch(
      /const showSettingsGroup = shouldRenderSettingsGroup\(sidebarNav\.settings\)/
    )
    expect(sidebarSource).toMatch(/\{showSettingsGroup \? \(/)
    expect(sidebarSource).toMatch(/aria-label="Settings"/)
  })

  it("does not render the Settings button outside the showSettingsGroup gate", () => {
    const gateIndex = sidebarSource.indexOf("{showSettingsGroup ? (")
    const buttonIndex = sidebarSource.indexOf('aria-label="Settings"')
    const gateEnd = sidebarSource.indexOf(") : null}", gateIndex)

    expect(gateIndex).toBeGreaterThan(-1)
    expect(buttonIndex).toBeGreaterThan(gateIndex)
    expect(buttonIndex).toBeLessThan(gateEnd)
  })
})

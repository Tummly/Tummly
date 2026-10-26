import { readFileSync } from "node:fs"
import { dirname, join } from "node:path"
import { fileURLToPath } from "node:url"
import { describe, expect, it } from "vitest"

const componentsDir = join(
  dirname(fileURLToPath(import.meta.url)),
  ".."
)

function readSource(relativePath: string): string {
  return readFileSync(join(componentsDir, relativePath), "utf8")
}

describe("Locations write chrome (WS2b Manage gate)", () => {
  it("renders Add and Import only when the Manage gate allows writes", () => {
    const page = readSource("Locations/LocationsPage.tsx")

    expect(page).toMatch(/canWriteOperatorArea\(locationsAccess\)/)
    expect(page).toMatch(/\{copy\.addLocation\}/)
    expect(page).toMatch(/\{copy\.importLocations\}/)
  })
})

describe("Add location control chrome (R7.3 every entry point)", () => {
  it("renders the panel Add location link only when canAddLocation is true", () => {
    const switcher = readSource("LocationSwitcher.tsx")
    // The rendered link text comes after the gate; the prop doc comment does not.
    const addIndex = switcher.lastIndexOf("Add location")

    expect(addIndex).toBeGreaterThan(-1)
    expect(switcher).toMatch(/canAddLocation: boolean/)
    expect(switcher).toMatch(/\{canAddLocation \? \(/)
    expect(switcher.lastIndexOf("{canAddLocation ? (", addIndex)).toBeGreaterThan(
      -1
    )
    expect(switcher).toMatch(/canAddLocation=\{canAddLocation\}/)
  })

  it("passes canAddLocation from the navbar through the shell", () => {
    const shell = readSource("DashboardShell.tsx")
    const navbar = readSource("DashboardNavbar.tsx")

    expect(navbar).toMatch(/canAddLocation: boolean/)
    expect(navbar).toMatch(/<LocationSwitcher[\s\S]*canAddLocation=\{canAddLocation\}/)
    expect(shell).toMatch(/canAddLocation: boolean/)
    expect(shell).toMatch(/<DashboardNavbar[\s\S]*canAddLocation=\{canAddLocation\}/)
  })

  it("computes the navbar gate from the workspace locations access", () => {
    const dashboard = readSource("Dashboard.tsx")

    expect(dashboard).toMatch(
      /canAddLocation=\{canWriteOperatorArea\(workspace\.snapshot\.locationsAccess\)\}/
    )
  })

  it("gates the Shop picker Add location from outlet locationsAccess", () => {
    const picker = readSource("Shop/ShopLocationPicker.tsx")

    expect(picker).toMatch(
      /canAddLocation=\{canWriteOperatorArea\(locationsAccess\)\}/
    )
    expect(picker).toMatch(/useOutletContext<DashboardOutletContext>\(\)/)
  })
})

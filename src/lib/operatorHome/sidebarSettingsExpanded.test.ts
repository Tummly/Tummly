import { afterEach, describe, expect, it } from "vitest"

import {
  OPERATOR_SIDEBAR_SETTINGS_EXPANDED_KEY,
  readSidebarSettingsExpanded,
  writeSidebarSettingsExpanded,
} from "./sidebarSettingsExpanded"

describe("sidebarSettingsExpanded persistence", () => {
  afterEach(() => {
    localStorage.removeItem(OPERATOR_SIDEBAR_SETTINGS_EXPANDED_KEY)
  })

  it("defaults to open when unset", () => {
    expect(readSidebarSettingsExpanded()).toBe(true)
  })

  it("round-trips Settings disclosure preference", () => {
    writeSidebarSettingsExpanded(false)
    expect(readSidebarSettingsExpanded()).toBe(false)
    writeSidebarSettingsExpanded(true)
    expect(readSidebarSettingsExpanded()).toBe(true)
  })
})

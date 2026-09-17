import { afterEach, describe, expect, it } from "vitest"

import {
  OPERATOR_SIDEBAR_COLLAPSED_KEY,
  readSidebarCollapsed,
  writeSidebarCollapsed,
} from "./sidebarCollapsed"

describe("sidebarCollapsed persistence", () => {
  afterEach(() => {
    localStorage.removeItem(OPERATOR_SIDEBAR_COLLAPSED_KEY)
  })

  it("defaults to collapsed when unset so hover can peek open", () => {
    expect(readSidebarCollapsed()).toBe(true)
  })

  it("round-trips collapsed preference", () => {
    writeSidebarCollapsed(true)
    expect(readSidebarCollapsed()).toBe(true)
    writeSidebarCollapsed(false)
    expect(readSidebarCollapsed()).toBe(false)
  })
})

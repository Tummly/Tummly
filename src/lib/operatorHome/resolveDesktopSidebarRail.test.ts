import { describe, expect, it } from "vitest"

import { resolveDesktopSidebarRail } from "./resolveDesktopSidebarRail"

describe("resolveDesktopSidebarRail", () => {
  it("keeps a pinned-open rail open without hover", () => {
    expect(
      resolveDesktopSidebarRail({
        preferenceCollapsed: false,
        hoverExpanded: false,
        assistantExpanded: false,
      })
    ).toEqual({
      effectiveCollapsed: false,
      toggleLocked: false,
    })
  })

  it("opens a collapsed rail on hover and closes when hover ends", () => {
    expect(
      resolveDesktopSidebarRail({
        preferenceCollapsed: true,
        hoverExpanded: true,
        assistantExpanded: false,
      })
    ).toEqual({
      effectiveCollapsed: false,
      toggleLocked: false,
    })
    expect(
      resolveDesktopSidebarRail({
        preferenceCollapsed: true,
        hoverExpanded: false,
        assistantExpanded: false,
      })
    ).toEqual({
      effectiveCollapsed: true,
      toggleLocked: false,
    })
  })

  it("forces the rail closed and locks toggle while Assistant Expand is active", () => {
    expect(
      resolveDesktopSidebarRail({
        preferenceCollapsed: false,
        hoverExpanded: true,
        assistantExpanded: true,
      })
    ).toEqual({
      effectiveCollapsed: true,
      toggleLocked: true,
    })
  })
})

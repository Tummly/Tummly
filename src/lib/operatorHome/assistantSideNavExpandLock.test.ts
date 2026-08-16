import { describe, expect, it } from "vitest"

import { assistantSideNavExpandLock } from "./assistantSideNavExpandLock"

describe("assistantSideNavExpandLock", () => {
  it("forces the rail and locks its open control while Assistant Expand is active", () => {
    expect(
      assistantSideNavExpandLock({
        priorCollapsed: false,
        assistantExpanded: true,
      })
    ).toEqual({
      effectiveCollapsed: true,
      toggleLocked: true,
    })
  })

  it("restores the stored open or collapsed state after Assistant Expand", () => {
    expect(
      assistantSideNavExpandLock({
        priorCollapsed: false,
        assistantExpanded: false,
      })
    ).toEqual({
      effectiveCollapsed: false,
      toggleLocked: false,
    })
    expect(
      assistantSideNavExpandLock({
        priorCollapsed: true,
        assistantExpanded: false,
      })
    ).toEqual({
      effectiveCollapsed: true,
      toggleLocked: false,
    })
  })
})

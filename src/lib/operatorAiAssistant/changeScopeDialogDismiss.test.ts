import { describe, expect, it } from "vitest"

import { isChangeScopeNestedMenuTarget } from "./changeScopeDialogDismiss"

function fakeElement(options: {
  slot?: string
  ancestorSlot?: string
}): EventTarget {
  return {
    closest(selector: string) {
      if (
        options.slot != null &&
        selector === `[data-slot="${options.slot}"]`
      ) {
        return this
      }
      if (
        options.ancestorSlot != null &&
        selector === `[data-slot="${options.ancestorSlot}"]`
      ) {
        return { slot: options.ancestorSlot }
      }
      return null
    },
  } as unknown as EventTarget
}

describe("isChangeScopeNestedMenuTarget", () => {
  it("returns false for null and targets without closest", () => {
    expect(isChangeScopeNestedMenuTarget(null)).toBe(false)
    expect(isChangeScopeNestedMenuTarget({} as EventTarget)).toBe(false)
  })

  it("returns true for Select and Popover menu content (and descendants)", () => {
    expect(
      isChangeScopeNestedMenuTarget(fakeElement({ slot: "select-content" }))
    ).toBe(true)
    expect(
      isChangeScopeNestedMenuTarget(
        fakeElement({ ancestorSlot: "select-content" })
      )
    ).toBe(true)
    expect(
      isChangeScopeNestedMenuTarget(fakeElement({ slot: "popover-content" }))
    ).toBe(true)
    expect(
      isChangeScopeNestedMenuTarget(
        fakeElement({ ancestorSlot: "popover-content" })
      )
    ).toBe(true)
  })

  it("returns false for Dialog chrome outside nested menus", () => {
    expect(
      isChangeScopeNestedMenuTarget(fakeElement({ slot: "dialog-content" }))
    ).toBe(false)
    expect(isChangeScopeNestedMenuTarget(fakeElement({}))).toBe(false)
  })
})

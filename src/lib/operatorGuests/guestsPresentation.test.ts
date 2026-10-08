import { describe, expect, it } from "vitest"

import {
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_GUEST_AVATAR_CLASS,
  GUESTS_TABLE_GUEST_NAME_CELL_INNER_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

describe("guestsPresentation table chrome", () => {
  it("gives body rows a non-transparent hover fill (REP-06 / UI-04)", () => {
    expect(GUESTS_TABLE_BODY_ROW_CLASS).toContain("hover:bg-op-color-gray-60")
    expect(GUESTS_TABLE_BODY_ROW_CLASS).not.toContain("hover:bg-transparent")
  })

  it("keeps head rows without interactive hover fill", () => {
    expect(GUESTS_TABLE_HEAD_ROW_CLASS).toContain("hover:bg-transparent")
  })

  it("exposes guest name cell avatar chrome (GST-02)", () => {
    expect(GUESTS_TABLE_GUEST_NAME_CELL_INNER_CLASS).toContain("items-center")
    expect(GUESTS_TABLE_GUEST_AVATAR_CLASS).toContain("size-8")
  })
})

import { describe, expect, it } from "vitest"

import { SWITCH_THUMB_CLASS } from "@/components/ui/switch"

/**
 * Regression: `group-data-[size=…]/switch:data-checked:translate-x-*` compiles to
 * a selector that requires `data-state=checked` on the thumb. Radix only sets
 * `data-state` on the root, so the thumb never moves and the control looks Off
 * while Status stays Enabled (Guest permissions cards).
 */
describe("SWITCH_THUMB_CLASS", () => {
  it("moves the thumb from the root checked state, not a thumb data-checked attr", () => {
    expect(SWITCH_THUMB_CLASS).toContain(
      "group-data-[state=checked]/switch:translate-x-[calc(100%-2px)]"
    )
    expect(SWITCH_THUMB_CLASS).toContain(
      "group-data-[state=unchecked]/switch:translate-x-0"
    )
    expect(SWITCH_THUMB_CLASS).not.toContain(
      "group-data-[size=default]/switch:data-checked:translate-x"
    )
    expect(SWITCH_THUMB_CLASS).not.toContain(
      "group-data-[size=default]/switch:data-unchecked:translate-x"
    )
  })
})

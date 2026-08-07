import { describe, expect, it } from "vitest"

import { CAMPAIGNS_TRUE_EMPTY_HELPER_CLASS } from "./campaignsPresentation"

describe("campaignsPresentation", () => {
  it("uses Operator subtitle tokens for true-empty helper (no feature hex)", () => {
    expect(CAMPAIGNS_TRUE_EMPTY_HELPER_CLASS).toContain(
      "text-[var(--op-color-gray-550)]"
    )
    expect(CAMPAIGNS_TRUE_EMPTY_HELPER_CLASS).not.toContain("#7c7c7c")
    expect(CAMPAIGNS_TRUE_EMPTY_HELPER_CLASS).not.toContain("dark:text-[")
  })
})

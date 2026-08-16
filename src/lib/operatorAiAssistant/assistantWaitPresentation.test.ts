import { describe, expect, it } from "vitest"

import {
  ASSISTANT_WAIT_ICON_CLASS,
  ASSISTANT_WAIT_TEXT_CLASS,
} from "./assistantWaitPresentation"

describe("assistantWaitPresentation", () => {
  it("uses motion-safe spin and shimmer classes", () => {
    expect(ASSISTANT_WAIT_ICON_CLASS).toContain("animate-spin")
    expect(ASSISTANT_WAIT_ICON_CLASS).toContain("motion-reduce:hidden")
    expect(ASSISTANT_WAIT_TEXT_CLASS).toContain("assistant-wait-shimmer")
  })
})

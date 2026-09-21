import { describe, expect, it } from "vitest"

import {
  ASSISTANT_WAIT_BODY,
  ASSISTANT_WAIT_GERUND_INTERVAL_MS,
  ASSISTANT_WAIT_PREPARING_BODY,
  ASSISTANT_WAIT_RETRIEVING_BODY,
  isAssistantCheckingWaitBody,
} from "./assistantWaitGerunds"

describe("assistantWaitGerunds compatibility shim", () => {
  it("re-exports wait phrase defaults", () => {
    expect(ASSISTANT_WAIT_GERUND_INTERVAL_MS).toBe(1250)
    expect(ASSISTANT_WAIT_BODY).toBe("Matching your question to venue data…")
    expect(ASSISTANT_WAIT_RETRIEVING_BODY).toContain("Loading")
    expect(ASSISTANT_WAIT_PREPARING_BODY).toContain("Writing")
    expect(isAssistantCheckingWaitBody(ASSISTANT_WAIT_BODY)).toBe(true)
    expect(isAssistantCheckingWaitBody(ASSISTANT_WAIT_RETRIEVING_BODY)).toBe(
      false
    )
  })
})

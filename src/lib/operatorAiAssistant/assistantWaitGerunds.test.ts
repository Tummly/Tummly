import { describe, expect, it } from "vitest"

import {
  ASSISTANT_WAIT_BODY,
  ASSISTANT_WAIT_GERUNDS,
  ASSISTANT_WAIT_PREPARING_BODY,
  ASSISTANT_WAIT_RETRIEVING_BODY,
  assistantWaitGerundAt,
  formatAssistantWaitGerund,
  isAssistantCheckingWaitBody,
} from "./assistantWaitGerunds"

describe("assistantWaitGerunds", () => {
  it("has 184 unique Title Case gerunds", () => {
    expect(ASSISTANT_WAIT_GERUNDS).toHaveLength(184)
    expect(new Set(ASSISTANT_WAIT_GERUNDS).size).toBe(184)
    for (const word of ASSISTANT_WAIT_GERUNDS) {
      expect(word).toMatch(/^[A-Z][a-z]+$/)
    }
  })

  it("formats checking wait bodies and wraps the list", () => {
    expect(ASSISTANT_WAIT_BODY).toBe("Pondering…")
    expect(formatAssistantWaitGerund("Rummaging")).toBe("Rummaging…")
    expect(assistantWaitGerundAt(0)).toBe("Pondering")
    expect(assistantWaitGerundAt(184)).toBe("Pondering")
    expect(assistantWaitGerundAt(-1)).toBe("Capering")
    expect(isAssistantCheckingWaitBody("Pondering…")).toBe(true)
    expect(isAssistantCheckingWaitBody(ASSISTANT_WAIT_RETRIEVING_BODY)).toBe(
      false
    )
    expect(isAssistantCheckingWaitBody(ASSISTANT_WAIT_PREPARING_BODY)).toBe(
      false
    )
  })
})

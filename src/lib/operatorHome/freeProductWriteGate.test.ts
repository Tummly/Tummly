import { describe, expect, it } from "vitest"

import { shouldGateFreeProductWrite } from "./freeProductWriteGate"

describe("shouldGateFreeProductWrite", () => {
  it("gates Free only", () => {
    expect(shouldGateFreeProductWrite("Free")).toBe(true)
    expect(shouldGateFreeProductWrite("Pilot")).toBe(false)
    expect(shouldGateFreeProductWrite("Starter")).toBe(false)
  })
})

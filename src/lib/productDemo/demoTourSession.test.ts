import { afterEach, describe, expect, it } from "vitest"

import {
  clearDemoTourSession,
  isDemoTourActive,
  readDemoTourSession,
  startDemoTourSession,
  writeDemoTourSession,
} from "@/lib/productDemo/demoTourSession"

describe("demoTourSession", () => {
  afterEach(() => {
    clearDemoTourSession()
  })

  it("starts, reads, and clears the tour flag", () => {
    expect(isDemoTourActive()).toBe(false)
    startDemoTourSession(1)
    expect(isDemoTourActive()).toBe(true)
    expect(readDemoTourSession()).toEqual({
      stepIndex: 1,
      exploreOpen: true,
      phase: "tour",
    })
    writeDemoTourSession({
      stepIndex: 2,
      exploreOpen: false,
      phase: "exit",
    })
    expect(readDemoTourSession()?.phase).toBe("exit")
    clearDemoTourSession()
    expect(isDemoTourActive()).toBe(false)
  })
})

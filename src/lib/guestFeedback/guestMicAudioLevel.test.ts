import { describe, expect, it } from "vitest"

import {
  computeBarHeights,
  computeRmsLevel,
  GUEST_MIC_BAR_BASELINE,
  smoothLevel,
} from "./guestMicAudioLevel"

describe("computeRmsLevel", () => {
  it("returns 0 for empty data", () => {
    expect(computeRmsLevel(new Uint8Array())).toBe(0)
  })

  it("returns 0 for silence (all samples at midpoint 128)", () => {
    expect(computeRmsLevel(new Uint8Array(64).fill(128))).toBe(0)
  })

  it("returns 1 for a full-scale square wave", () => {
    const data = new Uint8Array(64)
    data.fill(0, 0, 32)
    data.fill(255, 32)
    expect(computeRmsLevel(data)).toBeCloseTo(1, 1)
  })

  it("returns a mid-range level for a half-amplitude signal", () => {
    const data = new Uint8Array(64)
    data.fill(64, 0, 32)
    data.fill(192, 32)
    expect(computeRmsLevel(data)).toBeCloseTo(0.5, 1)
  })
})

describe("smoothLevel", () => {
  it("rises quickly toward louder input (attack)", () => {
    expect(smoothLevel(0, 1, 0.5, 0.15)).toBe(0.5)
  })

  it("falls slowly toward quieter input (decay)", () => {
    expect(smoothLevel(1, 0, 0.5, 0.15)).toBeCloseTo(0.85)
  })

  it("is stable when input equals the previous level", () => {
    expect(smoothLevel(0.4, 0.4)).toBeCloseTo(0.4)
  })
})

describe("computeBarHeights", () => {
  it("returns the requested number of bars", () => {
    expect(computeBarHeights(0.5, 24)).toHaveLength(24)
  })

  it("returns a flat baseline strip at level 0", () => {
    const heights = computeBarHeights(0, 12)
    expect(heights.every((h) => h === GUEST_MIC_BAR_BASELINE)).toBe(true)
  })

  it("keeps every bar within [baseline, 1] even for out-of-range levels", () => {
    for (const level of [-1, 0.3, 0.9, 5]) {
      for (const height of computeBarHeights(level, 24)) {
        expect(height).toBeGreaterThanOrEqual(GUEST_MIC_BAR_BASELINE)
        expect(height).toBeLessThanOrEqual(1)
      }
    }
  })

  it("grows bars monotonically with level", () => {
    const quiet = computeBarHeights(0.2, 16)
    const loud = computeBarHeights(0.8, 16)
    quiet.forEach((height, index) => {
      expect(loud[index]).toBeGreaterThan(height)
    })
  })

  it("varies neighbouring bars so the strip does not move as one block", () => {
    const heights = computeBarHeights(1, 8)
    expect(new Set(heights.map((h) => h.toFixed(4))).size).toBeGreaterThan(1)
  })
})

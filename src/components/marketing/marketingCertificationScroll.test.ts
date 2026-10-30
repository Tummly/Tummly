import { describe, expect, it } from "vitest"

import {
  carouselScrollStep,
  measureCarouselEdges,
} from "@/components/marketing/marketingCertificationScroll"

const TRACK = { clientWidth: 400, scrollWidth: 1200 }

describe("measureCarouselEdges", () => {
  it("reports the start edge and an open end at scroll 0", () => {
    expect(measureCarouselEdges({ ...TRACK, scrollLeft: 0 })).toEqual({
      start: true,
      end: false,
    })
  })

  it("reports neither edge in the middle", () => {
    expect(measureCarouselEdges({ ...TRACK, scrollLeft: 300 })).toEqual({
      start: false,
      end: false,
    })
  })

  it("reports the end edge at the last scroll position", () => {
    expect(
      measureCarouselEdges({
        ...TRACK,
        scrollLeft: TRACK.scrollWidth - TRACK.clientWidth,
      }),
    ).toEqual({ start: false, end: true })
  })

  it("allows 1px of sub-pixel drift at both ends", () => {
    expect(measureCarouselEdges({ ...TRACK, scrollLeft: 1 }).start).toBe(true)
    expect(measureCarouselEdges({ ...TRACK, scrollLeft: 2 }).start).toBe(false)
    expect(measureCarouselEdges({ ...TRACK, scrollLeft: 799 }).end).toBe(true)
    expect(measureCarouselEdges({ ...TRACK, scrollLeft: 798 }).end).toBe(false)
  })

  it("holds both edges when the content fits the track", () => {
    expect(
      measureCarouselEdges({ scrollLeft: 0, clientWidth: 400, scrollWidth: 400 }),
    ).toEqual({ start: true, end: true })
  })
})

describe("carouselScrollStep", () => {
  it("scrolls 75% of the visible width, forward or back", () => {
    expect(carouselScrollStep(1, 400)).toBe(300)
    expect(carouselScrollStep(-1, 400)).toBe(-300)
  })
})

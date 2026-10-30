/** Share of the visible track one previous / next click scrolls. */
export const CAROUSEL_STEP_RATIO = 0.75

export type CarouselTrackMetrics = {
  scrollLeft: number
  clientWidth: number
  scrollWidth: number
}

export type CarouselEdges = { start: boolean; end: boolean }

/** Which ends of the track are in view (1px tolerance for sub-pixel scroll). */
export function measureCarouselEdges(track: CarouselTrackMetrics): CarouselEdges {
  return {
    start: track.scrollLeft <= 1,
    end: track.scrollLeft + track.clientWidth >= track.scrollWidth - 1,
  }
}

/** Pixels one click scrolls: negative for previous, positive for next. */
export function carouselScrollStep(
  direction: -1 | 1,
  clientWidth: number,
): number {
  return direction * clientWidth * CAROUSEL_STEP_RATIO
}

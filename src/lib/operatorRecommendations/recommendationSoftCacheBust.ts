/** Soft-cache bust for Home / Campaigns Recommended next step (ticket 10). */

let generation = 0

export function bumpRecommendedNextStepSoftCaches(): void {
  generation += 1
}

export function recommendedNextStepSoftCacheGeneration(): number {
  return generation
}

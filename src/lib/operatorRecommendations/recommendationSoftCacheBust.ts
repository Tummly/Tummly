/** Soft-cache bust for Home / Campaigns / Offer Details Recommended next step. */

let generation = 0

export function bumpRecommendedNextStepSoftCaches(): void {
  generation += 1
}

export function recommendedNextStepSoftCacheGeneration(): number {
  return generation
}

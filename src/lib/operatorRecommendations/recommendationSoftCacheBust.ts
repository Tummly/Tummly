/** Soft-cache bust for Home Recommended next step, Campaign recommendation, and Offer recommendation. */

let generation = 0

export function bumpRecommendedNextStepSoftCaches(): void {
  generation += 1
}

export function recommendedNextStepSoftCacheGeneration(): number {
  return generation
}

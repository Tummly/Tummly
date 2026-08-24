/** Soft-cache bust for Home / Campaigns Recommended next step (ticket 10). */

let generation = 0
const listeners = new Set<() => void>()

export function bumpRecommendedNextStepSoftCaches(): void {
  generation += 1
  for (const listener of listeners) {
    listener()
  }
}

export function recommendedNextStepSoftCacheGeneration(): number {
  return generation
}

export function subscribeRecommendedNextStepSoftCacheBust(
  listener: () => void
): () => void {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

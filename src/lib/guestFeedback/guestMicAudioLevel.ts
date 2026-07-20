/**
 * Pure audio-level math for the guest mic waveform.
 *
 * The rAF loop and AnalyserNode wiring live elsewhere (browser adapter +
 * waveform component); everything here is deterministic and unit-testable.
 */

/** Speech RMS rarely exceeds ~0.25, so boost it toward the visible range. */
export const GUEST_MIC_LEVEL_GAIN = 4

/** Bar height (fraction of strip height) when there is no signal. */
export const GUEST_MIC_BAR_BASELINE = 0.14

export type GuestMicAudioLevelSource = {
  /** Instantaneous level in [0, 1]; 0 when no live analyser is attached. */
  getLevel: () => number
  /** True while a live analyser is running for an active recording. */
  isActive: () => boolean
}

export const INACTIVE_GUEST_MIC_LEVEL_SOURCE: GuestMicAudioLevelSource = {
  getLevel: () => 0,
  isActive: () => false,
}

/**
 * Root-mean-square amplitude of 8-bit time-domain samples (128 = silence),
 * normalised to [0, 1].
 */
export function computeRmsLevel(timeDomainData: Uint8Array): number {
  if (timeDomainData.length === 0) {
    return 0
  }

  let sumOfSquares = 0
  for (const sample of timeDomainData) {
    const centered = (sample - 128) / 128
    sumOfSquares += centered * centered
  }

  return Math.sqrt(sumOfSquares / timeDomainData.length)
}

/**
 * Asymmetric smoothing: react quickly to louder input (attack) and relax
 * slowly to quieter input (decay) so the bars feel alive but not jittery.
 */
export function smoothLevel(
  previous: number,
  next: number,
  attack = 0.5,
  decay = 0.15
): number {
  const rate = next > previous ? attack : decay
  return previous + (next - previous) * rate
}

function barWeight(index: number): number {
  // Deterministic pseudo-random variation in [0.55, 1] so neighbouring bars
  // differ slightly instead of moving as one solid block.
  return 0.55 + 0.45 * Math.abs(Math.sin(index * 1.7 + 0.9))
}

/**
 * Height of each bar as a fraction of the strip height in
 * [GUEST_MIC_BAR_BASELINE, 1]. Level 0 yields a flat baseline strip
 * (the static fallback for reduced motion / missing Web Audio).
 */
export function computeBarHeights(level: number, barCount: number): number[] {
  const clamped = Math.min(1, Math.max(0, level))

  return Array.from({ length: barCount }, (_, index) => {
    return (
      GUEST_MIC_BAR_BASELINE +
      clamped * barWeight(index) * (1 - GUEST_MIC_BAR_BASELINE)
    )
  })
}

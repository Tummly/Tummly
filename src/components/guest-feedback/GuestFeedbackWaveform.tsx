import { useReducedMotion } from "framer-motion"
import { useEffect, useRef } from "react"

import {
  computeBarHeights,
  GUEST_MIC_BAR_BASELINE,
  smoothLevel,
  type GuestMicAudioLevelSource,
} from "@/lib/guestFeedback/guestMicAudioLevel"

const BAR_COUNT = 28

const baselinePercent = `${GUEST_MIC_BAR_BASELINE * 100}%`

type GuestFeedbackWaveformProps = {
  levelSource: GuestMicAudioLevelSource
}

/**
 * Audio-reactive bar strip shown while recording (Guest-Loop-MVP node
 * 3216:26436). Bar heights are written straight to DOM refs from a
 * requestAnimationFrame loop — no React state per frame. Falls back to a
 * static baseline strip under prefers-reduced-motion or when Web Audio is
 * unavailable; recording itself is unaffected either way.
 */
export function GuestFeedbackWaveform({
  levelSource,
}: GuestFeedbackWaveformProps) {
  const shouldReduceMotion = useReducedMotion()
  const barRefs = useRef<(HTMLSpanElement | null)[]>([])

  useEffect(() => {
    if (shouldReduceMotion) {
      return
    }

    let frame = 0
    let level = 0

    const tick = () => {
      level = smoothLevel(level, levelSource.getLevel())
      const heights = computeBarHeights(level, BAR_COUNT)
      barRefs.current.forEach((bar, index) => {
        if (bar) {
          bar.style.height = `${heights[index] * 100}%`
        }
      })
      frame = requestAnimationFrame(tick)
    }

    frame = requestAnimationFrame(tick)
    return () => {
      cancelAnimationFrame(frame)
    }
  }, [levelSource, shouldReduceMotion])

  return (
    <div
      className="flex h-6 min-w-0 flex-1 items-center justify-between gap-px"
      role="status"
      aria-label="Recording"
    >
      {Array.from({ length: BAR_COUNT }, (_, index) => (
        <span
          key={index}
          ref={(node) => {
            barRefs.current[index] = node
          }}
          aria-hidden
          className="w-0.5 shrink-0 rounded-full bg-guest-feedback-muted transition-[height] duration-75 ease-linear"
          style={{ height: baselinePercent }}
        />
      ))}
    </div>
  )
}

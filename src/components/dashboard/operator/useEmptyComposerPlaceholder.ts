import { useEffect, useRef, useState } from "react"

const TYPE_MS_PER_CHAR = 40
const PAUSE_MS = 1800
const DELETE_MS_PER_CHAR = 20

function readPrefersReducedMotion(): boolean {
  if (typeof window === "undefined") {
    return false
  }
  return window.matchMedia("(prefers-reduced-motion: reduce)").matches
}

/** Typewriter for empty-conversation composer placeholders. Timing is UI-only. */
export function useEmptyComposerPlaceholder(input: {
  placeholders: readonly string[]
  cycleGeneration: number
  isPaused: boolean
}): string {
  const [display, setDisplay] = useState("")
  const [reducedMotion, setReducedMotion] = useState(readPrefersReducedMotion)
  const pausedRef = useRef(input.isPaused)
  pausedRef.current = input.isPaused
  const placeholdersRef = useRef(input.placeholders)
  placeholdersRef.current = input.placeholders

  useEffect(() => {
    const media = window.matchMedia("(prefers-reduced-motion: reduce)")
    const onChange = () => {
      setReducedMotion(media.matches)
    }
    onChange()
    media.addEventListener("change", onChange)
    return () => {
      media.removeEventListener("change", onChange)
    }
  }, [])

  useEffect(() => {
    const first = placeholdersRef.current[0] ?? ""
    if (reducedMotion || placeholdersRef.current.length === 0) {
      setDisplay(first)
      return
    }

    let cancelled = false
    let stringIndex = 0
    let charCount = 0
    let phase: "type" | "pause" | "delete" = "type"
    let timeoutId = 0

    const schedule = (ms: number) => {
      timeoutId = window.setTimeout(tick, ms)
    }

    const tick = () => {
      if (cancelled) {
        return
      }
      if (pausedRef.current) {
        schedule(100)
        return
      }

      const strings = placeholdersRef.current
      if (strings.length === 0) {
        setDisplay("")
        return
      }
      if (stringIndex >= strings.length) {
        stringIndex = 0
      }
      const current = strings[stringIndex] ?? ""

      if (phase === "type") {
        charCount = Math.min(charCount + 1, current.length)
        setDisplay(current.slice(0, charCount))
        if (charCount >= current.length) {
          phase = "pause"
          schedule(PAUSE_MS)
        } else {
          schedule(TYPE_MS_PER_CHAR)
        }
        return
      }

      if (phase === "pause") {
        phase = "delete"
        schedule(DELETE_MS_PER_CHAR)
        return
      }

      charCount = Math.max(charCount - 1, 0)
      setDisplay(current.slice(0, charCount))
      if (charCount <= 0) {
        stringIndex = (stringIndex + 1) % strings.length
        phase = "type"
        schedule(TYPE_MS_PER_CHAR)
      } else {
        schedule(DELETE_MS_PER_CHAR)
      }
    }

    setDisplay("")
    schedule(TYPE_MS_PER_CHAR)
    return () => {
      cancelled = true
      window.clearTimeout(timeoutId)
    }
  }, [input.cycleGeneration, reducedMotion, input.placeholders.length])

  return display
}

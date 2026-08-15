import { useEffect, useState } from "react"

import {
  ASSISTANT_WAIT_PHRASE_MS,
  assistantWaitPhraseAt,
} from "@/lib/operatorAiAssistant/assistantWaitPresentation"

function readPrefersReducedMotion(): boolean {
  if (typeof window === "undefined") {
    return false
  }
  return window.matchMedia("(prefers-reduced-motion: reduce)").matches
}

/** Cycles Claude-style wait phrases. Timing is UI-only. */
export function useAssistantWaitPhrase(): string {
  const [elapsedMs, setElapsedMs] = useState(0)
  const [reducedMotion, setReducedMotion] = useState(readPrefersReducedMotion)

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
    if (reducedMotion) {
      setElapsedMs(0)
      return
    }
    const startedAt = Date.now()
    const intervalId = window.setInterval(() => {
      setElapsedMs(Date.now() - startedAt)
    }, ASSISTANT_WAIT_PHRASE_MS)
    return () => {
      window.clearInterval(intervalId)
    }
  }, [reducedMotion])

  return assistantWaitPhraseAt(elapsedMs, reducedMotion)
}

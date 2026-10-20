import { useEffect, useMemo, useRef, useState } from "react"
import { motion, useReducedMotion } from "framer-motion"

import { AiIcon } from "@/components/ui/ai-icon"
import { ThoughtLine } from "@/components/ui/thought-line"
import {
  assistantWaitPhrasesFor,
  planAssistantWaitPhrases,
  type AssistantWaitStep,
} from "@/lib/operatorAiAssistant/assistantWaitPhrases"

const MAX_TRACE_STEPS = 4
const WAIT_STEPS: readonly AssistantWaitStep[] = [
  "checking",
  "retrieving",
  "preparing",
]

type AssistantPreparingAnswerProps = {
  /** Live wait phrase from the turn pipeline. */
  phrase: string
  /** Latest user prompt — drives which step lines belong to this ask. */
  prompt?: string
  /** False when the turn is done — settles to “Worked for …”. */
  working?: boolean
  /** Frozen seconds when settled (from the turn clock). */
  elapsed?: number
  /**
   * When false, omit the ThoughtLine glyph so a title-row AiIcon can own the mark.
   * Defaults to true while working; settled answer chrome usually passes false.
   */
  showGlyph?: boolean
}

/** Slow spin with uneven pace — eases, then a short quicker stretch. */
function PreparingAiGlyph({ spinning }: { spinning: boolean }) {
  const reduceMotion = useReducedMotion()

  if (!spinning || reduceMotion) {
    return <AiIcon size={16} className="size-full text-op-text-primary" />
  }

  return (
    <motion.span
      className="inline-flex size-full origin-center"
      animate={{ rotate: [0, 70, 200, 290, 360] }}
      transition={{
        duration: 1.15,
        times: [0, 0.28, 0.52, 0.78, 1],
        ease: ["easeIn", "easeOut", "easeIn", "easeOut"],
        repeat: Infinity,
      }}
    >
      <AiIcon size={16} className="size-full text-op-text-primary" />
    </motion.span>
  )
}

function plannedStepLines(prompt: string): string[] {
  const trimmed = prompt.trim()
  if (trimmed.length === 0) {
    return []
  }
  const plan = planAssistantWaitPhrases(trimmed)
  const lines: string[] = []
  for (const step of WAIT_STEPS) {
    const phrases = assistantWaitPhrasesFor(plan, step)
    const first = phrases[0]
    if (first != null && first.length > 0 && !lines.includes(first)) {
      lines.push(first)
    }
  }
  return lines
}

/**
 * Preparing chrome — AI Assistant logo + fixed “Preparing…” head.
 * Trace lines come from the prompt plan and advance with the live wait phrase.
 * When settled, shows “Worked for” plus the frozen turn timer.
 */
export function AssistantPreparingAnswer({
  phrase,
  prompt = "",
  working = true,
  elapsed,
  showGlyph,
}: AssistantPreparingAnswerProps) {
  const live = phrase.trim()
  const planned = useMemo(() => plannedStepLines(prompt), [prompt])
  const [steps, setSteps] = useState<string[]>(() =>
    live.length > 0 ? [live] : planned.slice(0, 1)
  )
  const prevLiveRef = useRef(live)
  const glyphVisible = showGlyph ?? working

  useEffect(() => {
    if (!working || live.length === 0) {
      return
    }
    const prev = prevLiveRef.current
    if (prev === live) {
      return
    }
    prevLiveRef.current = live
    setSteps((history) => {
      const withoutDup = history.filter((step) => step !== live)
      const next = [...withoutDup, live]
      return next.length > MAX_TRACE_STEPS
        ? next.slice(-MAX_TRACE_STEPS)
        : next
    })
  }, [live, working])

  useEffect(() => {
    if (!working || live.length > 0 || planned.length === 0) {
      return
    }
    setSteps(planned.slice(0, 1))
  }, [live, planned, working])

  return (
    <ThoughtLine
      working={working}
      label="Preparing…"
      steps={working ? steps : []}
      doneLabel="Worked for"
      glyph={
        glyphVisible ? <PreparingAiGlyph spinning={working} /> : "none"
      }
      fontSize={14}
      breathPeriod={1.6}
      breathDepth={0.2}
      settleDuration={350}
      settleBlur={2}
      collapsible
      collapseOnSettle
      showTimer
      elapsed={elapsed}
      shimmer={false}
      color="var(--op-color-gray-550)"
      glyphColor="var(--op-text-primary)"
      className="text-op-assistant-list-title"
    />
  )
}

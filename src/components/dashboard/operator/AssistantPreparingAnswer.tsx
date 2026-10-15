import { useEffect, useRef, useState } from "react"
import { ThoughtLine } from "@/components/ui/thought-line"

const MAX_TRACE_STEPS = 3

type AssistantPreparingAnswerProps = {
  /** Live wait phrase from the turn pipeline (gerund / Retrieving / Preparing). */
  phrase: string
}

export function AssistantPreparingAnswer({
  phrase,
}: AssistantPreparingAnswerProps) {
  const label = phrase.trim() || "Preparing answer…"
  /** Prior wait phrases only — current line stays on `label`. */
  const [steps, setSteps] = useState<string[]>([])
  const prevLabelRef = useRef(label)

  useEffect(() => {
    const prev = prevLabelRef.current
    if (prev === label) return
    setSteps((history) => {
      const next = [...history, prev]
      return next.length > MAX_TRACE_STEPS
        ? next.slice(-MAX_TRACE_STEPS)
        : next
    })
    prevLabelRef.current = label
  }, [label])

  return (
    <ThoughtLine
      working
      label={label}
      steps={steps}
      doneLabel="Thought for"
      glyph="sparkle"
      fontSize={14}
      breathPeriod={1.6}
      breathDepth={0.45}
      settleDuration={350}
      settleBlur={2}
      collapsible
      collapseOnSettle
      showTimer
      color="var(--op-color-gray-550)"
      glyphColor="var(--op-text-primary)"
      className="text-op-assistant-list-title"
    />
  )
}

/**
 * PROTOTYPE — floating controls for Guest Loop Account Setup QA walkthrough.
 */
import { useCallback, useEffect, useRef, useState } from "react"
import {
  ChevronLeftIcon,
  ChevronRightIcon,
  GripHorizontalIcon,
} from "lucide-react"

import { cn } from "@/lib/utils"

export type PrototypeAccountType = "single" | "multi"

export type PrototypeReadyMode = "animate" | "complete" | "error"

type GuestLoopAccountSetupPrototypeBarProps = {
  accountType: PrototypeAccountType
  step: number
  maxStep: number
  readyMode: PrototypeReadyMode
  stepLabels: readonly string[]
  onAccountTypeChange: (type: PrototypeAccountType) => void
  onStepChange: (step: number) => void
  onReadyModeChange: (mode: PrototypeReadyMode) => void
  onSeedPrefill: () => void
  onSeedAll: () => void
  onLogPayload: () => void
}

const PANEL_WIDTH = 248
const PANEL_HEIGHT_ESTIMATE = 168

function clampPosition(x: number, y: number) {
  const margin = 8
  const maxX = Math.max(margin, window.innerWidth - PANEL_WIDTH - margin)
  const maxY = Math.max(
    margin,
    window.innerHeight - PANEL_HEIGHT_ESTIMATE - margin
  )

  return {
    x: Math.min(Math.max(margin, x), maxX),
    y: Math.min(Math.max(margin, y), maxY),
  }
}

function getDefaultPosition() {
  return clampPosition(
    window.innerWidth - PANEL_WIDTH - 16,
    window.innerHeight - 220
  )
}

export function GuestLoopAccountSetupPrototypeBar({
  accountType,
  step,
  maxStep,
  readyMode,
  stepLabels,
  onAccountTypeChange,
  onStepChange,
  onReadyModeChange,
  onSeedPrefill,
  onSeedAll,
  onLogPayload,
}: GuestLoopAccountSetupPrototypeBarProps) {
  const readyStep = maxStep
  const panelRef = useRef<HTMLDivElement>(null)
  const dragOffset = useRef({ x: 0, y: 0 })
  const [position, setPosition] = useState(getDefaultPosition)
  const [isDragging, setIsDragging] = useState(false)

  useEffect(() => {
    const handleResize = () => {
      setPosition((current) => clampPosition(current.x, current.y))
    }

    window.addEventListener("resize", handleResize)
    return () => window.removeEventListener("resize", handleResize)
  }, [])

  const handlePointerMove = useCallback((event: PointerEvent) => {
    if (!isDragging) {
      return
    }

    setPosition(
      clampPosition(
        event.clientX - dragOffset.current.x,
        event.clientY - dragOffset.current.y
      )
    )
  }, [isDragging])

  const handlePointerUp = useCallback(() => {
    setIsDragging(false)
  }, [])

  useEffect(() => {
    if (!isDragging) {
      return
    }

    window.addEventListener("pointermove", handlePointerMove)
    window.addEventListener("pointerup", handlePointerUp)

    return () => {
      window.removeEventListener("pointermove", handlePointerMove)
      window.removeEventListener("pointerup", handlePointerUp)
    }
  }, [handlePointerMove, handlePointerUp, isDragging])

  const handleDragStart = (event: React.PointerEvent<HTMLDivElement>) => {
    if (event.button !== 0) {
      return
    }

    const panel = panelRef.current
    if (!panel) {
      return
    }

    const rect = panel.getBoundingClientRect()
    dragOffset.current = {
      x: event.clientX - rect.left,
      y: event.clientY - rect.top,
    }
    setIsDragging(true)
    event.preventDefault()
  }

  const cycleStep = (direction: -1 | 1) => {
    const next = step + direction
    if (next < 1) {
      onStepChange(maxStep)
      return
    }
    if (next > maxStep) {
      onStepChange(1)
      return
    }
    onStepChange(next)
  }

  const chipClass = (active: boolean) =>
    cn(
      "rounded px-1.5 py-0.5 text-[10px] font-semibold leading-none transition-colors",
      active
        ? "bg-white text-[#1a1a1a]"
        : "bg-white/10 text-white hover:bg-white/20"
    )

  return (
    <div
      ref={panelRef}
      className={cn(
        "fixed z-50 select-none rounded-lg border border-[#1a1a1a]/80 bg-[#1a1a1a]/95 p-2 text-white shadow-lg backdrop-blur-sm",
        isDragging && "cursor-grabbing"
      )}
      style={{
        left: position.x,
        top: position.y,
        width: PANEL_WIDTH,
      }}
      aria-label="Prototype controls"
    >
      <div
        className="mb-1.5 flex cursor-grab items-center gap-1.5 border-b border-white/10 pb-1.5 active:cursor-grabbing"
        onPointerDown={handleDragStart}
        title="Drag to move"
      >
        <GripHorizontalIcon className="size-3.5 shrink-0 text-white/40" />
        <span className="rounded bg-amber-400 px-1 py-px text-[9px] font-bold uppercase tracking-wide text-[#1a1a1a]">
          Proto
        </span>
        <span className="min-w-0 flex-1 truncate text-[10px] text-white/55">
          console.log only
        </span>
      </div>

      <div className="flex flex-col gap-1.5">
        <div className="flex items-center gap-1">
          <span className="w-7 shrink-0 text-[9px] font-medium uppercase text-white/45">
            Type
          </span>
          {(["single", "multi"] as const).map((type) => (
            <button
              key={type}
              type="button"
              onClick={() => onAccountTypeChange(type)}
              className={chipClass(accountType === type)}
            >
              {type}
            </button>
          ))}
        </div>

        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => cycleStep(-1)}
            className="rounded bg-white/10 p-0.5 hover:bg-white/20"
            aria-label="Previous step"
          >
            <ChevronLeftIcon className="size-3" />
          </button>

          {stepLabels.map((label, index) => {
            const stepNumber = index + 1
            return (
              <button
                key={label}
                type="button"
                onClick={() => onStepChange(stepNumber)}
                title={label}
                className={cn(
                  chipClass(step === stepNumber),
                  step === stepNumber && "bg-primary text-white"
                )}
              >
                {stepNumber}
              </button>
            )
          })}

          <button
            type="button"
            onClick={() => cycleStep(1)}
            className="rounded bg-white/10 p-0.5 hover:bg-white/20"
            aria-label="Next step"
          >
            <ChevronRightIcon className="size-3" />
          </button>

          <span className="ml-auto truncate text-[9px] text-white/45">
            {stepLabels[step - 1]}
          </span>
        </div>

        {step === readyStep ? (
          <div className="flex items-center gap-1">
            <span className="w-7 shrink-0 text-[9px] font-medium uppercase text-white/45">
              Ready
            </span>
            {(
              [
                ["animate", "Run"],
                ["complete", "Done"],
                ["error", "Err"],
              ] as const
            ).map(([mode, label]) => (
              <button
                key={mode}
                type="button"
                onClick={() => onReadyModeChange(mode)}
                className={chipClass(readyMode === mode)}
              >
                {label}
              </button>
            ))}
          </div>
        ) : null}

        <div className="flex flex-wrap gap-1 border-t border-white/10 pt-1.5">
          <button
            type="button"
            onClick={onSeedPrefill}
            className={chipClass(false)}
          >
            Prefill
          </button>
          <button type="button" onClick={onSeedAll} className={chipClass(false)}>
            Seed all
          </button>
          <button
            type="button"
            onClick={onLogPayload}
            className={chipClass(false)}
          >
            Log
          </button>
        </div>
      </div>
    </div>
  )
}

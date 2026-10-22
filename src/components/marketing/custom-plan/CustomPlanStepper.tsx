import { useRef, useState, type KeyboardEvent } from "react"
import { Minus, Plus } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { cn } from "@/lib/utils"

type CustomPlanStepperProps = {
  value: number
  onChange: (n: number) => void
  min: number
  max: number
  /** Accessible name, e.g. "Restaurant Locations" or "Additional team users packs". */
  label?: string
  disabled?: boolean
  className?: string
}

function clamp(n: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, n))
}

/** Figma counter `6777:40810` — 136 × 30, editable centre value. */
export function CustomPlanStepper({
  value,
  onChange,
  min,
  max,
  label = "Count",
  disabled,
  className,
}: CustomPlanStepperProps) {
  // Draft text while typing; null when the field shows the committed value.
  const [draft, setDraft] = useState<string | null>(null)
  // Escape cancels: the blur that follows must not commit the draft.
  const cancelNextCommit = useRef(false)

  const commit = (raw: string) => {
    setDraft(null)
    if (cancelNextCommit.current) {
      cancelNextCommit.current = false
      return
    }
    const parsed = Number.parseInt(raw.replace(/[^\d]/g, ""), 10)
    if (Number.isNaN(parsed)) {
      return
    }
    const next = clamp(parsed, min, max)
    if (next !== value) {
      onChange(next)
    }
  }

  const step = (delta: number) => {
    setDraft(null)
    const next = clamp(value + delta, min, max)
    if (next !== value) {
      onChange(next)
    }
  }

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Enter") {
      event.preventDefault()
      // Blur commits the draft.
      event.currentTarget.blur()
    } else if (event.key === "Escape") {
      cancelNextCommit.current = true
      event.currentTarget.blur()
    } else if (event.key === "ArrowUp") {
      event.preventDefault()
      step(1)
    } else if (event.key === "ArrowDown") {
      event.preventDefault()
      step(-1)
    }
  }

  const iconButtonClass =
    "h-full w-9 shrink-0 rounded-none px-2.5 py-1.5 text-[#141414] shadow-none hover:bg-[#f4f4f4] focus-visible:z-10 disabled:opacity-30"

  return (
    <div
      role="group"
      aria-label={label}
      className={cn(
        "flex h-[30px] w-[136px] shrink-0 items-stretch overflow-hidden rounded-lg border border-[#e5e5e5] bg-white",
        disabled && "opacity-60",
        className,
      )}
    >
      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        className={cn(iconButtonClass, "border-r border-[#e5e5e5]")}
        disabled={disabled || value <= min}
        onClick={() => step(-1)}
        aria-label={`Decrease ${label}`}
      >
        <Minus className="size-4" strokeWidth={0.8} absoluteStrokeWidth />
      </Button>
      <Input
        type="text"
        inputMode="numeric"
        aria-label={label}
        disabled={disabled}
        value={draft ?? String(value)}
        onChange={(event) =>
          setDraft(event.target.value.replace(/[^\d]/g, "").slice(0, 4))
        }
        onFocus={(event) => {
          // Defer so the click's mouseup does not clear the selection.
          const input = event.currentTarget
          requestAnimationFrame(() => input.select())
        }}
        onBlur={(event) => commit(event.currentTarget.value)}
        onKeyDown={onKeyDown}
        className="h-full min-w-0 flex-1 rounded-none border-0 border-r border-[#e5e5e5] bg-transparent px-1 py-1.5 text-center font-sans text-sm text-black tabular-nums shadow-none focus-visible:ring-0 focus-visible:bg-[#f4f4f4] md:text-sm"
      />
      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        className={iconButtonClass}
        disabled={disabled || value >= max}
        onClick={() => step(1)}
        aria-label={`Increase ${label}`}
      >
        <Plus className="size-4" strokeWidth={0.8} absoluteStrokeWidth />
      </Button>
    </div>
  )
}

import { useState } from "react"
import { CalendarIcon, ChevronDownIcon } from "lucide-react"
import type { DateRange } from "react-day-picker"

import { Button } from "@/components/ui/button"
import { Calendar } from "@/components/ui/calendar"
import {
  Popover,
  PopoverContent,
  PopoverTitle,
  PopoverTrigger,
} from "@/components/ui/popover"
import {
  HOME_PERFORMANCE_CUSTOM_MAX_SPAN_DAYS,
  homePerformancePresetOptions,
  inclusiveLocalDateSpanDays,
  isHomePerformanceCustomSpanAllowed,
  parseLocalDateKey,
  toLocalDateKey,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import {
  PERFORMANCE_DATE_BUTTON_CLASS,
  PERFORMANCE_DATE_BUTTON_ENABLED_CLASS,
  PERFORMANCE_DATE_CUSTOM_ACTIONS_CLASS,
  PERFORMANCE_DATE_CUSTOM_HINT_CLASS,
  PERFORMANCE_DATE_ICON_CLASS,
  PERFORMANCE_DATE_PRESET_ITEM_CLASS,
  PERFORMANCE_DATE_PRESET_LIST_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import { OPERATOR_SHELL_MENU_ITEM_CLASS, OPERATOR_SHELL_MENU_ITEM_SELECTED_CLASS, OPERATOR_SHELL_MENU_PANEL_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"

type PopoverStep = "presets" | "custom"

export type PerformanceDateRangeLeadingOption = {
  id: string
  label: string
  selected: boolean
}

export type PerformanceDateRangeControlProps = {
  dateRangeLabel: string
  /** Null when a leading option (e.g. All time) is selected. */
  selectedRange: HomePerformanceDateRange | null
  onCommitRange: (range: HomePerformanceDateRange) => void
  title: string
  leadingOptions?: readonly PerformanceDateRangeLeadingOption[]
  onCommitLeadingOption?: (id: string) => void
  /** Extra classes for the portaled menu (e.g. z-index above a Dialog). */
  contentClassName?: string
  /** Extra classes for the trigger. Merges after Family A chrome. */
  triggerClassName?: string
  /** Initial step when opening popover (presets or custom). */
  defaultStep?: PopoverStep
  /** Controlled open state for popover. */
  open?: boolean
  /** Controlled open change callback. */
  onOpenChange?: (open: boolean) => void
}

function parseCommittedCustomDraft(
  selectedRange: HomePerformanceDateRange | null
): DateRange | undefined {
  if (selectedRange?.kind !== "custom") {
    return undefined
  }
  return {
    from: parseLocalDateKey(selectedRange.startDate),
    to: parseLocalDateKey(selectedRange.endDate),
  }
}

/** Shared Performance-style date control — presets + Custom; optional leading options. */
export function PerformanceDateRangeControl({
  dateRangeLabel,
  selectedRange,
  onCommitRange,
  title,
  leadingOptions = [],
  onCommitLeadingOption,
  contentClassName,
  triggerClassName,
  defaultStep = "presets",
  open: controlledOpen,
  onOpenChange: controlledOnOpenChange,
}: PerformanceDateRangeControlProps) {
  const [uncontrolledOpen, setUncontrolledOpen] = useState(false)
  const isControlled = controlledOpen !== undefined
  const open = isControlled ? controlledOpen : uncontrolledOpen

  const [step, setStep] = useState<PopoverStep>(defaultStep)
  const [draftRange, setDraftRange] = useState<DateRange | undefined>(() =>
    defaultStep === "custom" ? parseCommittedCustomDraft(selectedRange) : undefined
  )

  const resetTransientState = () => {
    setStep(defaultStep)
    setDraftRange(
      defaultStep === "custom" ? parseCommittedCustomDraft(selectedRange) : undefined
    )
  }

  const handleOpenChange = (nextOpen: boolean) => {
    if (isControlled) {
      controlledOnOpenChange?.(nextOpen)
    } else {
      setUncontrolledOpen(nextOpen)
    }
    if (nextOpen && defaultStep === "custom") {
      setStep("custom")
      setDraftRange(parseCommittedCustomDraft(selectedRange))
    }
    if (!nextOpen) {
      resetTransientState()
    }
  }

  const openCustomStep = () => {
    setDraftRange(parseCommittedCustomDraft(selectedRange))
    setStep("custom")
  }

  const draftStartKey =
    draftRange?.from != null ? toLocalDateKey(draftRange.from) : null
  const draftEndKey =
    draftRange?.to != null ? toLocalDateKey(draftRange.to) : null
  const draftComplete = draftStartKey != null && draftEndKey != null
  const draftOverMax =
    draftComplete &&
    !isHomePerformanceCustomSpanAllowed(draftStartKey, draftEndKey)
  const canApply = draftComplete && !draftOverMax

  return (
    <Popover open={open} onOpenChange={handleOpenChange}>
      <PopoverTrigger asChild>
          <Button
            type="button"
            variant="op-tertiary"
            aria-label={dateRangeLabel}
            className={cn(
              PERFORMANCE_DATE_BUTTON_CLASS,
              PERFORMANCE_DATE_BUTTON_ENABLED_CLASS,
              triggerClassName
            )}
          >
          <CalendarIcon className={PERFORMANCE_DATE_ICON_CLASS} aria-hidden />
          {dateRangeLabel}
          <ChevronDownIcon
            className={PERFORMANCE_DATE_ICON_CLASS}
            aria-hidden
          />
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="end"
        sideOffset={8}
        className={cn(
          "gap-0",
          OPERATOR_SHELL_MENU_PANEL_CLASS,
          // Match Account menu padding (`px-0 py-1`); custom calendar needs flush edges.
          step === "presets" ? "w-auto min-w-44 px-0 py-1" : "w-auto p-0",
          contentClassName
        )}
        onOpenAutoFocus={(event) => {
          // Focus the selected preset — default first-item focus made "All time" look active.
          event.preventDefault()
          const root = event.currentTarget
          if (!(root instanceof HTMLElement)) return
          root
            .querySelector<HTMLButtonElement>(
              '[role="option"][aria-selected="true"] button'
            )
            ?.focus()
        }}
      >
        <PopoverTitle className="sr-only">{title}</PopoverTitle>
        {step === "presets" ? (
          <ul className={PERFORMANCE_DATE_PRESET_LIST_CLASS} role="listbox">
            {leadingOptions.map((option) => (
              <li
                key={option.id}
                role="option"
                aria-selected={option.selected}
              >
                <Button
                  type="button"
                  variant="op-ghost"
                  className={cn(
                    OPERATOR_SHELL_MENU_ITEM_CLASS,
                    PERFORMANCE_DATE_PRESET_ITEM_CLASS,
                    option.selected && OPERATOR_SHELL_MENU_ITEM_SELECTED_CLASS
                  )}
                  onClick={() => {
                    onCommitLeadingOption?.(option.id)
                    handleOpenChange(false)
                  }}
                >
                  {option.label}
                </Button>
              </li>
            ))}
            {homePerformancePresetOptions().map((option) => {
              const selected =
                selectedRange?.kind === "preset" &&
                selectedRange.presetId === option.presetId
              return (
                <li
                  key={option.presetId}
                  role="option"
                  aria-selected={selected}
                >
                  <Button
                    type="button"
                    variant="op-ghost"
                    className={cn(
                      OPERATOR_SHELL_MENU_ITEM_CLASS,
                      PERFORMANCE_DATE_PRESET_ITEM_CLASS,
                      selected && OPERATOR_SHELL_MENU_ITEM_SELECTED_CLASS
                    )}
                    onClick={() => {
                      onCommitRange({
                        kind: "preset",
                        presetId: option.presetId,
                      })
                      handleOpenChange(false)
                    }}
                  >
                    {option.label}
                  </Button>
                </li>
              )
            })}
            <li
              role="option"
              aria-selected={selectedRange?.kind === "custom"}
            >
              <Button
                type="button"
                variant="op-ghost"
                className={cn(
                  OPERATOR_SHELL_MENU_ITEM_CLASS,
                  PERFORMANCE_DATE_PRESET_ITEM_CLASS,
                  selectedRange?.kind === "custom" &&
                    OPERATOR_SHELL_MENU_ITEM_SELECTED_CLASS
                )}
                onClick={openCustomStep}
              >
                Custom
              </Button>
            </li>
          </ul>
        ) : (
          <div className="flex flex-col gap-2 p-2">
            <Calendar
              mode="range"
              numberOfMonths={2}
              selected={draftRange}
              onSelect={setDraftRange}
              defaultMonth={draftRange?.from}
            />
            {draftOverMax ? (
              <p className={PERFORMANCE_DATE_CUSTOM_HINT_CLASS} role="status">
                Choose a range of {HOME_PERFORMANCE_CUSTOM_MAX_SPAN_DAYS} days
                or fewer
                {draftComplete
                  ? ` (selected ${inclusiveLocalDateSpanDays(draftStartKey!, draftEndKey!)} days)`
                  : ""}
                .
              </p>
            ) : (
              <p className={PERFORMANCE_DATE_CUSTOM_HINT_CLASS}>
                Select a start and end date (max{" "}
                {HOME_PERFORMANCE_CUSTOM_MAX_SPAN_DAYS} days).
              </p>
            )}
            <div className={PERFORMANCE_DATE_CUSTOM_ACTIONS_CLASS}>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => {
                  setDraftRange(undefined)
                  if (defaultStep === "custom") {
                    handleOpenChange(false)
                  } else {
                    setStep("presets")
                  }
                }}
              >
                {defaultStep === "custom" ? "Close" : "Back"}
              </Button>
              <div className="flex items-center gap-1">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => handleOpenChange(false)}
                >
                  Cancel
                </Button>
                <Button
                  type="button"
                  size="sm"
                  disabled={!canApply}
                  onClick={() => {
                    if (
                      !canApply ||
                      draftStartKey == null ||
                      draftEndKey == null
                    ) {
                      return
                    }
                    onCommitRange({
                      kind: "custom",
                      startDate: draftStartKey,
                      endDate: draftEndKey,
                    })
                    handleOpenChange(false)
                  }}
                >
                  Apply
                </Button>
              </div>
            </div>
          </div>
        )}
      </PopoverContent>
    </Popover>
  )
}

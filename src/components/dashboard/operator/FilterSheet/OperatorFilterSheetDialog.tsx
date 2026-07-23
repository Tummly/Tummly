import { ChevronDownIcon } from "lucide-react"
import { Fragment, useState, type ReactNode } from "react"
import type { DateRange } from "react-day-picker"

import { OperatorRemovableChip } from "@/components/dashboard/operator/FilterSheet/OperatorRemovableChip"
import { Button } from "@/components/ui/button"
import { Calendar } from "@/components/ui/calendar"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"
import { Separator } from "@/components/ui/separator"
import {
  applyPending,
  backToLocationMode,
  beginDatePick,
  beginLocationIndividual,
  beginLocationPick,
  changeDateAxis,
  clearAllPending,
  clearDate,
  clearLocation,
  closeDatePick,
  isApplyDirty,
  pickDateAxis,
  pickDatePreset,
  projectChips,
  removePendingChip,
  setLocationAll,
  toggleLocationId,
  toggleMultiSelect,
  type ChipLabelResolvers,
  type DateAxisId,
  type DateFieldSchema,
  type DatePresetId,
  type FilterChip,
  type FilterSheetSchema,
  type FilterSheetSession,
  type LocationScopeFieldSchema,
  type MultiSelectFieldSchema,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import {
  HOME_PERFORMANCE_CUSTOM_MAX_SPAN_DAYS,
  inclusiveLocalDateSpanDays,
  isHomePerformanceCustomSpanAllowed,
  parseLocalDateKey,
  toLocalDateKey,
} from "@/lib/operatorHome/homePerformanceDateRange"
import {
  PERFORMANCE_DATE_CUSTOM_ACTIONS_CLASS,
  PERFORMANCE_DATE_CUSTOM_HINT_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import { cn } from "@/lib/utils"

export type OperatorFilterSheetDialogProps = {
  open: boolean
  title: string
  schema: FilterSheetSchema
  session: FilterSheetSession | null
  chipResolvers?: ChipLabelResolvers
  onSessionChange: (session: FilterSheetSession) => void
  onOpenChange: (open: boolean) => void
  onApply: (filters: OperatorFilterSelection) => void
}

const SELECT_TRIGGER_CLASS =
  "h-[50px] w-full justify-between rounded border border-[rgba(74,74,76,0.4)] bg-transparent px-[15px] text-left text-sm font-normal shadow-none hover:bg-transparent"
const SELECT_MENU_CLASS =
  "z-[140] w-[var(--radix-popover-trigger-width)] gap-0 rounded-lg p-1"
const SELECT_LIST_CLASS = "flex flex-col gap-0.5"
const SELECT_ITEM_CLASS =
  "h-auto w-full justify-start rounded-md px-2.5 py-1.5 text-left text-sm font-normal text-foreground"
const SELECT_ITEM_ACTIVE_CLASS = "bg-accent font-medium"
const SELECT_ITEM_MUTED_CLASS =
  "h-auto w-full justify-start rounded-md px-2.5 py-1.5 text-left text-sm text-muted-foreground"
const SELECT_ITEM_NAV_CLASS =
  "h-auto w-full justify-start rounded-md px-2.5 py-1.5 text-left text-xs font-medium text-muted-foreground"

function FieldLabel({ children }: { children: ReactNode }) {
  return (
    <span className="text-sm font-semibold leading-5 text-foreground">
      {children}
    </span>
  )
}

function RemovableChips({
  chips,
  onRemove,
}: {
  chips: FilterChip[]
  onRemove: (chip: FilterChip) => void
}) {
  if (chips.length === 0) {
    return null
  }
  return (
    <div className="flex flex-wrap gap-3">
      {chips.map((chip) => (
        <OperatorRemovableChip
          key={chip.id}
          label={chip.label}
          removeLabel={`Remove ${chip.label}`}
          onRemove={() => onRemove(chip)}
        />
      ))}
    </div>
  )
}

function useDraftRangeCalc(draftRange: DateRange | undefined) {
  const draftStartKey =
    draftRange?.from != null ? toLocalDateKey(draftRange.from) : null
  const draftEndKey =
    draftRange?.to != null ? toLocalDateKey(draftRange.to) : null
  const draftComplete = draftStartKey != null && draftEndKey != null
  const draftOverMax =
    draftComplete &&
    !isHomePerformanceCustomSpanAllowed(draftStartKey, draftEndKey)
  const canApplyCustom = draftComplete && !draftOverMax
  return { draftStartKey, draftEndKey, draftComplete, draftOverMax, canApplyCustom }
}

function MultiSelectControl({
  field,
  session,
  open,
  onOpenChange,
  onSessionChange,
  chips,
  onRemoveChip,
}: {
  field: MultiSelectFieldSchema
  session: FilterSheetSession
  open: boolean
  onOpenChange: (open: boolean) => void
  onSessionChange: (session: FilterSheetSession) => void
  chips: FilterChip[]
  onRemoveChip: (chip: FilterChip) => void
}) {
  const value = session.pending[field.id]
  const ids = value?.kind === "multi-select" ? value.ids : []

  return (
    <div className="flex flex-col gap-2">
      <FieldLabel>{field.label}</FieldLabel>
      <Popover open={open} onOpenChange={onOpenChange}>
        <PopoverTrigger asChild>
          <Button
            type="button"
            variant="outline"
            className={cn(SELECT_TRIGGER_CLASS, ids.length === 0 && "text-[#7d7d7d]")}
          >
            <span>{ids.length > 0 ? `${ids.length} selected` : "Select"}</span>
            <ChevronDownIcon className="size-4 shrink-0 opacity-60" />
          </Button>
        </PopoverTrigger>
        <PopoverContent align="start" className={SELECT_MENU_CLASS}>
          <ul className={SELECT_LIST_CLASS} role="listbox">
            {field.options.map((option) => (
              <li key={option.id}>
                <Button
                  type="button"
                  variant="ghost"
                  role="option"
                  aria-selected={ids.includes(option.id)}
                  className={cn(
                    SELECT_ITEM_CLASS,
                    ids.includes(option.id) && SELECT_ITEM_ACTIVE_CLASS
                  )}
                  onClick={() => {
                    onSessionChange(toggleMultiSelect(session, field.id, option.id))
                  }}
                >
                  {option.label}
                </Button>
              </li>
            ))}
            {field.options.length === 0 ? (
              <li className="rounded-md px-2.5 py-1.5 text-sm text-muted-foreground">
                No options yet
              </li>
            ) : null}
          </ul>
        </PopoverContent>
      </Popover>
      <RemovableChips chips={chips} onRemove={onRemoveChip} />
    </div>
  )
}

function LocationScopeControl({
  field,
  session,
  open,
  onOpenChange,
  onSessionChange,
  chips,
  onRemoveChip,
}: {
  field: LocationScopeFieldSchema
  session: FilterSheetSession
  open: boolean
  onOpenChange: (open: boolean) => void
  onSessionChange: (session: FilterSheetSession) => void
  chips: FilterChip[]
  onRemoveChip: (chip: FilterChip) => void
}) {
  const value = session.pending[field.id]
  const location = value?.kind === "location-scope" ? value.value : { kind: "none" as const }
  const locationName = (id: string) =>
    field.locations.find((location) => location.id === id)?.label ?? id

  const triggerLabel = (): string => {
    if (location.kind === "all") {
      return "All permitted locations"
    }
    if (location.kind === "individual") {
      if (location.locationIds.length === 0) {
        return "Individual permitted locations…"
      }
      return location.locationIds.map(locationName).join(", ")
    }
    return "Select"
  }

  return (
    <div className="flex flex-col gap-2">
      <FieldLabel>{field.label}</FieldLabel>
      <Popover
        open={open}
        onOpenChange={(next) => {
          onOpenChange(next)
          if (next) {
            onSessionChange(beginLocationPick(session, field.id))
          }
        }}
      >
        <PopoverTrigger asChild>
          <Button
            type="button"
            variant="outline"
            className={cn(SELECT_TRIGGER_CLASS, location.kind === "none" && "text-[#7d7d7d]")}
          >
            <span className="truncate">{triggerLabel()}</span>
            <ChevronDownIcon className="size-4 shrink-0 opacity-60" />
          </Button>
        </PopoverTrigger>
        <PopoverContent align="start" className={SELECT_MENU_CLASS}>
          {session.locationStep !== "individual" ? (
            <ul className={SELECT_LIST_CLASS} role="listbox">
              <li>
                <Button
                  type="button"
                  variant="ghost"
                  className={SELECT_ITEM_CLASS}
                  onClick={() => {
                    onSessionChange(setLocationAll(session, field.id))
                    onOpenChange(false)
                  }}
                >
                  All permitted locations
                </Button>
              </li>
              <li>
                <Button
                  type="button"
                  variant="ghost"
                  className={SELECT_ITEM_CLASS}
                  onClick={() => {
                    onSessionChange(beginLocationIndividual(session, field.id))
                  }}
                >
                  Individual permitted locations
                </Button>
              </li>
              {location.kind !== "none" ? (
                <li>
                  <Button
                    type="button"
                    variant="ghost"
                    className={SELECT_ITEM_MUTED_CLASS}
                    onClick={() => {
                      onSessionChange(clearLocation(session, field.id))
                      onOpenChange(false)
                    }}
                  >
                    Clear location override
                  </Button>
                </li>
              ) : null}
            </ul>
          ) : (
            <ul className={SELECT_LIST_CLASS} role="listbox">
              <li>
                <Button
                  type="button"
                  variant="ghost"
                  className={SELECT_ITEM_NAV_CLASS}
                  onClick={() => {
                    onSessionChange(backToLocationMode(session))
                  }}
                >
                  ← Back
                </Button>
              </li>
              {field.locations.map((locationOption) => {
                const selected =
                  location.kind === "individual" &&
                  location.locationIds.includes(locationOption.id)
                return (
                  <li key={locationOption.id}>
                    <Button
                      type="button"
                      variant="ghost"
                      role="option"
                      aria-selected={selected}
                      className={cn(SELECT_ITEM_CLASS, selected && SELECT_ITEM_ACTIVE_CLASS)}
                      onClick={() => {
                        onSessionChange(
                          toggleLocationId(session, field.id, locationOption.id)
                        )
                      }}
                    >
                      {locationOption.label}
                    </Button>
                  </li>
                )
              })}
            </ul>
          )}
        </PopoverContent>
      </Popover>
      <RemovableChips chips={chips} onRemove={onRemoveChip} />
    </div>
  )
}

function DateControl({
  field,
  session,
  open,
  onOpenChange,
  onSessionChange,
  chips,
  onRemoveChip,
  dateCustomStep,
  setDateCustomStep,
  draftRange,
  setDraftRange,
}: {
  field: DateFieldSchema
  session: FilterSheetSession
  open: boolean
  onOpenChange: (open: boolean) => void
  onSessionChange: (session: FilterSheetSession) => void
  chips: FilterChip[]
  onRemoveChip: (chip: FilterChip) => void
  dateCustomStep: boolean
  setDateCustomStep: (value: boolean) => void
  draftRange: DateRange | undefined
  setDraftRange: (value: DateRange | undefined) => void
}) {
  const value = session.pending[field.id]
  const dateValue = value?.kind === "date" ? value.value : { kind: "none" as const }
  const fieldRef = { id: field.id, hasAxis: field.hasAxis }

  const { draftStartKey, draftEndKey, draftComplete, draftOverMax, canApplyCustom } =
    useDraftRangeCalc(draftRange)

  const triggerLabel = (): string => {
    if (dateCustomStep) {
      return "Custom date range…"
    }
    if (field.hasAxis && session.dateStep === "axis") {
      const axisNames = Object.values(field.axisLabels ?? {})
      return `Choose ${axisNames.join(" or ")}…`
    }
    if (session.dateStep === "preset") {
      if (field.hasAxis) {
        const axis = session.dateDraftAxis
        return axis != null
          ? `${field.axisLabels?.[axis]} → choose range…`
          : "Choose range…"
      }
      return "Choose range…"
    }
    return chips[0]?.label ?? "Select"
  }

  return (
    <div className="flex flex-col gap-2">
      <FieldLabel>{field.label}</FieldLabel>
      <Popover
        open={open}
        onOpenChange={(next) => {
          onOpenChange(next)
          if (next) {
            setDateCustomStep(false)
            setDraftRange(undefined)
            onSessionChange(beginDatePick(session, fieldRef))
          } else {
            setDateCustomStep(false)
            setDraftRange(undefined)
            onSessionChange(closeDatePick(session, field.id))
          }
        }}
      >
        <PopoverTrigger asChild>
          <Button
            type="button"
            variant="outline"
            className={cn(
              SELECT_TRIGGER_CLASS,
              dateValue.kind === "none" &&
                session.dateStep == null &&
                !dateCustomStep &&
                "text-[#7d7d7d]"
            )}
          >
            <span className="truncate">{triggerLabel()}</span>
            <ChevronDownIcon className="size-4 shrink-0 opacity-60" />
          </Button>
        </PopoverTrigger>
        <PopoverContent
          align="start"
          className={cn("gap-0", dateCustomStep ? "w-auto p-0" : SELECT_MENU_CLASS)}
        >
          {dateCustomStep ? (
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
                  Choose a range of {HOME_PERFORMANCE_CUSTOM_MAX_SPAN_DAYS} days or
                  fewer
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
                    setDateCustomStep(false)
                  }}
                >
                  Back
                </Button>
                <div className="flex items-center gap-1">
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => onOpenChange(false)}
                  >
                    Cancel
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    disabled={!canApplyCustom}
                    onClick={() => {
                      if (!canApplyCustom || draftStartKey == null || draftEndKey == null) {
                        return
                      }
                      onSessionChange(
                        pickDatePreset(session, fieldRef, "custom", {
                          dateFrom: draftStartKey,
                          dateTo: draftEndKey,
                        })
                      )
                      onOpenChange(false)
                      setDateCustomStep(false)
                      setDraftRange(undefined)
                    }}
                  >
                    Apply
                  </Button>
                </div>
              </div>
            </div>
          ) : field.hasAxis && session.dateStep !== "preset" ? (
            <ul className={SELECT_LIST_CLASS} role="listbox">
              {Object.entries(field.axisLabels ?? {}).map(([id, label]) => (
                <li key={id}>
                  <Button
                    type="button"
                    variant="ghost"
                    className={SELECT_ITEM_CLASS}
                    onClick={() => {
                      onSessionChange(
                        pickDateAxis(session, field.id, id as DateAxisId)
                      )
                    }}
                  >
                    {label}
                  </Button>
                </li>
              ))}
              {dateValue.kind !== "none" ? (
                <li>
                  <Button
                    type="button"
                    variant="ghost"
                    className={SELECT_ITEM_MUTED_CLASS}
                    onClick={() => {
                      onSessionChange(clearDate(session, field.id))
                      onOpenChange(false)
                    }}
                  >
                    Any time (clear)
                  </Button>
                </li>
              ) : null}
            </ul>
          ) : (
            <ul className={SELECT_LIST_CLASS} role="listbox">
              {field.hasAxis ? (
                <li>
                  <Button
                    type="button"
                    variant="ghost"
                    className={SELECT_ITEM_NAV_CLASS}
                    onClick={() => {
                      onSessionChange(changeDateAxis(session))
                    }}
                  >
                    ← Change axis
                  </Button>
                </li>
              ) : null}
              <li>
                <Button
                  type="button"
                  variant="ghost"
                  className={SELECT_ITEM_CLASS}
                  onClick={() => {
                    onSessionChange(pickDatePreset(session, fieldRef, "any-time"))
                    onOpenChange(false)
                  }}
                >
                  Any time
                </Button>
              </li>
              {Object.entries(field.presetLabels).map(([id, label]) => (
                <li key={id}>
                  <Button
                    type="button"
                    variant="ghost"
                    className={SELECT_ITEM_CLASS}
                    onClick={() => {
                      onSessionChange(
                        pickDatePreset(
                          session,
                          fieldRef,
                          id as Exclude<DatePresetId, "any-time" | "custom">
                        )
                      )
                      onOpenChange(false)
                    }}
                  >
                    {label}
                  </Button>
                </li>
              ))}
              <li>
                <Button
                  type="button"
                  variant="ghost"
                  className={SELECT_ITEM_CLASS}
                  onClick={() => {
                    const existing =
                      dateValue.kind === "custom"
                        ? {
                            from: parseLocalDateKey(dateValue.dateFrom),
                            to: parseLocalDateKey(dateValue.dateTo),
                          }
                        : undefined
                    setDraftRange(existing)
                    setDateCustomStep(true)
                  }}
                >
                  Custom date range…
                </Button>
              </li>
              {!field.hasAxis && dateValue.kind !== "none" ? (
                <li>
                  <Button
                    type="button"
                    variant="ghost"
                    className={SELECT_ITEM_MUTED_CLASS}
                    onClick={() => {
                      onSessionChange(clearDate(session, field.id))
                      onOpenChange(false)
                    }}
                  >
                    Clear date
                  </Button>
                </li>
              ) : null}
            </ul>
          )}
        </PopoverContent>
      </Popover>
      <RemovableChips chips={chips} onRemove={onRemoveChip} />
    </div>
  )
}

function chunkPairs<T>(items: readonly T[]): T[][] {
  const rows: T[][] = []
  for (let i = 0; i < items.length; i += 2) {
    rows.push(items.slice(i, i + 2))
  }
  return rows
}

export function OperatorFilterSheetDialog({
  open,
  title,
  schema,
  session,
  chipResolvers,
  onSessionChange,
  onOpenChange,
  onApply,
}: OperatorFilterSheetDialogProps) {
  const [openPopovers, setOpenPopovers] = useState<Record<string, boolean>>({})
  const [dateCustomStep, setDateCustomStep] = useState(false)
  const [draftRange, setDraftRange] = useState<DateRange | undefined>()

  if (session == null) {
    return null
  }

  const dirty = isApplyDirty(session)
  const pendingChips = projectChips(schema, session.pending, chipResolvers)
  const chipsByFieldId = new Map<string, FilterChip[]>()
  for (const chip of pendingChips) {
    const existing = chipsByFieldId.get(chip.fieldId) ?? []
    existing.push(chip)
    chipsByFieldId.set(chip.fieldId, existing)
  }

  const removeChip = (chip: FilterChip) => {
    onSessionChange(removePendingChip(schema, session, chip))
  }

  const resetPopovers = () => {
    setOpenPopovers({})
    setDateCustomStep(false)
    setDraftRange(undefined)
  }

  const rows = chunkPairs(schema.fields)

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) {
          resetPopovers()
        }
        onOpenChange(next)
      }}
    >
      <DialogContent
        showCloseButton
        className="max-h-[90vh] gap-[60px] overflow-y-auto p-8 sm:max-w-[1019px]"
      >
        <div className="flex flex-col gap-5">
          <DialogHeader className="gap-0 pr-10">
            <DialogTitle className="text-2xl font-bold tracking-normal">
              {title}
            </DialogTitle>
          </DialogHeader>

          {rows.map((row, rowIndex) => (
            <Fragment key={row.map((field) => field.id).join("-")}>
              <div className="grid gap-5 sm:grid-cols-2">
                {row.map((field) => {
                  const chips = chipsByFieldId.get(field.id) ?? []
                  const open = openPopovers[field.id] ?? false
                  const setOpen = (next: boolean) => {
                    setOpenPopovers((prev) => ({ ...prev, [field.id]: next }))
                  }

                  if (field.kind === "multi-select") {
                    return (
                      <MultiSelectControl
                        key={field.id}
                        field={field}
                        session={session}
                        open={open}
                        onOpenChange={setOpen}
                        onSessionChange={onSessionChange}
                        chips={chips}
                        onRemoveChip={removeChip}
                      />
                    )
                  }

                  if (field.kind === "location-scope") {
                    return (
                      <LocationScopeControl
                        key={field.id}
                        field={field}
                        session={session}
                        open={open}
                        onOpenChange={setOpen}
                        onSessionChange={onSessionChange}
                        chips={chips}
                        onRemoveChip={removeChip}
                      />
                    )
                  }

                  return (
                    <DateControl
                      key={field.id}
                      field={field}
                      session={session}
                      open={open}
                      onOpenChange={setOpen}
                      onSessionChange={onSessionChange}
                      chips={chips}
                      onRemoveChip={removeChip}
                      dateCustomStep={dateCustomStep}
                      setDateCustomStep={setDateCustomStep}
                      draftRange={draftRange}
                      setDraftRange={setDraftRange}
                    />
                  )
                })}
                {row.length === 1 ? <div /> : null}
              </div>
              {rowIndex < rows.length - 1 ? <Separator /> : null}
            </Fragment>
          ))}
        </div>

        <DialogFooter className="flex-row flex-wrap gap-3 sm:justify-between">
          <div className="flex flex-wrap items-center gap-3">
            <Button
              type="button"
              disabled={!dirty}
              className="h-auto min-h-0 rounded-[2px] px-4 py-2.5 text-sm"
              onClick={() => {
                onApply(applyPending(session))
                resetPopovers()
                onOpenChange(false)
              }}
            >
              Apply filters
            </Button>
            <Button
              type="button"
              variant="operator-tertiary"
              className="h-auto min-h-0 rounded-[2px]"
              onClick={() => {
                resetPopovers()
                onOpenChange(false)
              }}
            >
              Cancel
            </Button>
          </div>
          <Button
            type="button"
            variant="operator-tertiary"
            className="h-auto min-h-0 rounded-[2px]"
            disabled={pendingChips.length === 0}
            onClick={() => {
              onSessionChange(clearAllPending(schema, session))
            }}
          >
            Clear all
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

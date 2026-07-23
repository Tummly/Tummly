import { ChevronDownIcon } from "lucide-react"
import { useState, type ReactNode } from "react"
import type { DateRange } from "react-day-picker"

import { GuestsRemovableChip } from "@/components/dashboard/operator/Guests/GuestsRemovableChip"
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
  ACTIVITY_TYPE_OPTIONS,
  applyPendingActivityFilters,
  beginActivityDatePick,
  clearAllActivityPending,
  clearActivityDate,
  DATE_PRESET_LABELS,
  isActivityFiltersApplyDirty,
  pickActivityDatePreset,
  projectActivityFilterChips,
  removePendingActivityFilterChip,
  toggleActivityType,
  type ActivityFilterChip,
  type ActivityFiltersPanelSession,
  type ActivityTypeId,
  type GuestActivityFilterSelection,
} from "@/lib/operatorGuestProfile/guestActivityFilterSelection"
import {
  HOME_PERFORMANCE_CUSTOM_MAX_SPAN_DAYS,
  inclusiveLocalDateSpanDays,
  isHomePerformanceCustomSpanAllowed,
  toLocalDateKey,
} from "@/lib/operatorHome/homePerformanceDateRange"
import {
  PERFORMANCE_DATE_CUSTOM_ACTIONS_CLASS,
  PERFORMANCE_DATE_CUSTOM_HINT_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import type { DatePresetId } from "@/lib/operatorGuests/guestsFilterSelection"
import { cn } from "@/lib/utils"

type ActivityFiltersDialogProps = {
  open: boolean
  session: ActivityFiltersPanelSession | null
  onSessionChange: (session: ActivityFiltersPanelSession) => void
  onOpenChange: (open: boolean) => void
  onApply: (filters: GuestActivityFilterSelection) => void
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
  chips: ActivityFilterChip[]
  onRemove: (chip: ActivityFilterChip) => void
}) {
  if (chips.length === 0) {
    return null
  }
  return (
    <div className="flex flex-wrap gap-3">
      {chips.map((chip) => (
        <GuestsRemovableChip
          key={chip.id}
          label={chip.label}
          removeLabel={`Remove ${chip.label}`}
          onRemove={() => onRemove(chip)}
        />
      ))}
    </div>
  )
}

export function ActivityFiltersDialog({
  open,
  session,
  onSessionChange,
  onOpenChange,
  onApply,
}: ActivityFiltersDialogProps) {
  const [typeOpen, setTypeOpen] = useState(false)
  const [dateOpen, setDateOpen] = useState(false)
  const [dateCustomStep, setDateCustomStep] = useState(false)
  const [draftRange, setDraftRange] = useState<DateRange | undefined>()

  if (session == null) {
    return null
  }

  const dirty = isActivityFiltersApplyDirty(session)
  const pendingChips = projectActivityFilterChips(session.pending)
  const typeChips = pendingChips.filter((c) => c.kind === "activity-type")
  const dateChips = pendingChips.filter((c) => c.kind === "date")

  const removeChip = (chip: ActivityFilterChip) => {
    onSessionChange(removePendingActivityFilterChip(session, chip))
  }

  const dateTriggerLabel = (): string => {
    if (dateCustomStep) {
      return "Custom date range…"
    }
    if (session.dateStep === "preset") {
      return "Choose range…"
    }
    return dateChips[0]?.label ?? "Select"
  }

  const draftStartKey =
    draftRange?.from != null ? toLocalDateKey(draftRange.from) : null
  const draftEndKey =
    draftRange?.to != null ? toLocalDateKey(draftRange.to) : null
  const draftComplete = draftStartKey != null && draftEndKey != null
  const draftOverMax =
    draftComplete &&
    !isHomePerformanceCustomSpanAllowed(draftStartKey, draftEndKey)
  const canApplyCustom = draftComplete && !draftOverMax

  const resetPopovers = () => {
    setTypeOpen(false)
    setDateOpen(false)
    setDateCustomStep(false)
    setDraftRange(undefined)
  }

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
              Filter activity
            </DialogTitle>
          </DialogHeader>

          <div className="grid gap-5 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <FieldLabel>Activity type</FieldLabel>
              <Popover open={typeOpen} onOpenChange={setTypeOpen}>
                <PopoverTrigger asChild>
                  <Button
                    type="button"
                    variant="outline"
                    className={cn(
                      SELECT_TRIGGER_CLASS,
                      session.pending.activityTypes.length === 0 &&
                        "text-[#7d7d7d]"
                    )}
                  >
                    <span>
                      {session.pending.activityTypes.length > 0
                        ? `${session.pending.activityTypes.length} selected`
                        : "Select"}
                    </span>
                    <ChevronDownIcon className="size-4 shrink-0 opacity-60" />
                  </Button>
                </PopoverTrigger>
                <PopoverContent align="start" className={SELECT_MENU_CLASS}>
                  <ul className={SELECT_LIST_CLASS} role="listbox">
                    {ACTIVITY_TYPE_OPTIONS.map(([id, label]) => (
                      <li key={id}>
                        <Button
                          type="button"
                          variant="ghost"
                          role="option"
                          aria-selected={session.pending.activityTypes.includes(
                            id
                          )}
                          className={cn(
                            SELECT_ITEM_CLASS,
                            session.pending.activityTypes.includes(id) &&
                              SELECT_ITEM_ACTIVE_CLASS
                          )}
                          onClick={() => {
                            onSessionChange(
                              toggleActivityType(session, id as ActivityTypeId)
                            )
                          }}
                        >
                          {label}
                        </Button>
                      </li>
                    ))}
                  </ul>
                </PopoverContent>
              </Popover>
              <RemovableChips chips={typeChips} onRemove={removeChip} />
            </div>
          </div>

          <Separator />

          <div className="grid gap-5 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <FieldLabel>Date</FieldLabel>
              <Popover
                open={dateOpen}
                onOpenChange={(next) => {
                  setDateOpen(next)
                  if (next) {
                    setDateCustomStep(false)
                    setDraftRange(undefined)
                    onSessionChange(beginActivityDatePick(session))
                  } else {
                    setDateCustomStep(false)
                    setDraftRange(undefined)
                    onSessionChange({
                      ...session,
                      dateStep: null,
                    })
                  }
                }}
              >
                <PopoverTrigger asChild>
                  <Button
                    type="button"
                    variant="outline"
                    className={cn(
                      SELECT_TRIGGER_CLASS,
                      session.pending.date.kind === "none" &&
                        session.dateStep == null &&
                        !dateCustomStep &&
                        "text-[#7d7d7d]"
                    )}
                  >
                    <span className="truncate">{dateTriggerLabel()}</span>
                    <ChevronDownIcon className="size-4 shrink-0 opacity-60" />
                  </Button>
                </PopoverTrigger>
                <PopoverContent
                  align="start"
                  className={cn(
                    "gap-0",
                    dateCustomStep ? "w-auto p-0" : SELECT_MENU_CLASS
                  )}
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
                        <p
                          className={PERFORMANCE_DATE_CUSTOM_HINT_CLASS}
                          role="status"
                        >
                          Choose a range of{" "}
                          {HOME_PERFORMANCE_CUSTOM_MAX_SPAN_DAYS} days or fewer
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
                            onClick={() => setDateOpen(false)}
                          >
                            Cancel
                          </Button>
                          <Button
                            type="button"
                            size="sm"
                            disabled={!canApplyCustom}
                            onClick={() => {
                              if (
                                !canApplyCustom ||
                                draftStartKey == null ||
                                draftEndKey == null
                              ) {
                                return
                              }
                              onSessionChange(
                                pickActivityDatePreset(session, "custom", {
                                  dateFrom: draftStartKey,
                                  dateTo: draftEndKey,
                                })
                              )
                              setDateOpen(false)
                              setDateCustomStep(false)
                              setDraftRange(undefined)
                            }}
                          >
                            Apply
                          </Button>
                        </div>
                      </div>
                    </div>
                  ) : (
                    <ul className={SELECT_LIST_CLASS} role="listbox">
                      <li>
                        <Button
                          type="button"
                          variant="ghost"
                          className={SELECT_ITEM_CLASS}
                          onClick={() => {
                            onSessionChange(
                              pickActivityDatePreset(session, "any-time")
                            )
                            setDateOpen(false)
                          }}
                        >
                          Any time
                        </Button>
                      </li>
                      {(
                        Object.entries(DATE_PRESET_LABELS) as Array<
                          [
                            Exclude<DatePresetId, "any-time" | "custom">,
                            string,
                          ]
                        >
                      ).map(([id, label]) => (
                        <li key={id}>
                          <Button
                            type="button"
                            variant="ghost"
                            className={SELECT_ITEM_CLASS}
                            onClick={() => {
                              onSessionChange(
                                pickActivityDatePreset(session, id)
                              )
                              setDateOpen(false)
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
                            setDateCustomStep(true)
                          }}
                        >
                          Custom date range…
                        </Button>
                      </li>
                      {session.pending.date.kind !== "none" ? (
                        <li>
                          <Button
                            type="button"
                            variant="ghost"
                            className={SELECT_ITEM_MUTED_CLASS}
                            onClick={() => {
                              onSessionChange(clearActivityDate(session))
                              setDateOpen(false)
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
              <RemovableChips chips={dateChips} onRemove={removeChip} />
            </div>
          </div>
        </div>

        <DialogFooter className="gap-3 sm:justify-between">
          <Button
            type="button"
            variant="ghost"
            onClick={() => {
              onSessionChange(clearAllActivityPending(session))
            }}
          >
            Clear all
          </Button>
          <div className="flex items-center gap-3">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                resetPopovers()
                onOpenChange(false)
              }}
            >
              Cancel
            </Button>
            <Button
              type="button"
              disabled={!dirty}
              onClick={() => {
                onApply(applyPendingActivityFilters(session))
                resetPopovers()
              }}
            >
              Apply filters
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

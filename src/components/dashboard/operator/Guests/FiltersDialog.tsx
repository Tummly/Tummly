import { ChevronDownIcon } from "lucide-react"
import { useState, type ReactNode } from "react"
import type { DateRange } from "react-day-picker"

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
  CONTACT_LABELS,
  DATE_AXIS_LABELS,
  DATE_PRESET_LABELS,
  MARKETING_LABELS,
  SENTIMENT_LABELS,
  applyPendingFilters,
  beginDateAxisPick,
  beginLocationIndividual,
  clearAllPending,
  clearDate,
  clearLocation,
  isFiltersApplyDirty,
  pickDateAxis,
  pickDatePreset,
  projectFilterChips,
  removePendingFieldChip,
  setLocationAll,
  toggleContact,
  toggleLocationId,
  toggleMarketing,
  toggleSentiment,
  toggleTag,
  type ContactOptionId,
  type DateAxisId,
  type DatePresetId,
  type FilterChip,
  type FiltersPanelSession,
  type GuestsFilterSelection,
  type MarketingOptionId,
  type SentimentOptionId,
} from "@/lib/operatorGuests/guestsFilterSelection"
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

import { GuestsRemovableChip } from "./GuestsRemovableChip"

export type FiltersDialogLocationOption = {
  id: string
  name: string
}

export type FiltersDialogTagOption = {
  id: string
  name: string
}

type FiltersDialogProps = {
  open: boolean
  session: FiltersPanelSession | null
  locations: readonly FiltersDialogLocationOption[]
  tags: readonly FiltersDialogTagOption[]
  showLocationFilter: boolean
  onSessionChange: (session: FiltersPanelSession) => void
  onOpenChange: (open: boolean) => void
  onApply: (filters: GuestsFilterSelection) => void
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

export function FiltersDialog({
  open,
  session,
  locations,
  tags,
  showLocationFilter,
  onSessionChange,
  onOpenChange,
  onApply,
}: FiltersDialogProps) {
  const [marketingOpen, setMarketingOpen] = useState(false)
  const [contactOpen, setContactOpen] = useState(false)
  const [sentimentOpen, setSentimentOpen] = useState(false)
  const [locationOpen, setLocationOpen] = useState(false)
  const [dateOpen, setDateOpen] = useState(false)
  const [tagsOpen, setTagsOpen] = useState(false)
  const [dateCustomStep, setDateCustomStep] = useState(false)
  const [draftRange, setDraftRange] = useState<DateRange | undefined>()

  const locationName = (id: string) =>
    locations.find((location) => location.id === id)?.name ?? id
  const tagName = (id: string) =>
    tags.find((tag) => tag.id === id)?.name ?? id

  if (session == null) {
    return null
  }

  const dirty = isFiltersApplyDirty(session)
  const pendingChips = projectFilterChips(session.pending, {
    locationName,
    tagName,
  })
  const marketingChips = pendingChips.filter((c) => c.kind === "marketing")
  const contactChips = pendingChips.filter((c) => c.kind === "contact")
  const sentimentChips = pendingChips.filter((c) => c.kind === "sentiment")
  const locationChips = pendingChips.filter(
    (c) => c.kind === "location-all" || c.kind === "location-id"
  )
  const dateChips = pendingChips.filter((c) => c.kind === "date")
  const tagChips = pendingChips.filter((c) => c.kind === "tag")

  const removeChip = (chip: FilterChip) => {
    onSessionChange(removePendingFieldChip(session, chip))
  }

  const dateTriggerLabel = (): string => {
    if (dateCustomStep) {
      return "Custom date range…"
    }
    if (session.dateStep === "axis") {
      return "Choose First captured or Last interaction…"
    }
    if (session.dateStep === "preset") {
      const axis = session.dateDraftAxis
      return axis != null
        ? `${DATE_AXIS_LABELS[axis]} → choose range…`
        : "Choose range…"
    }
    return dateChips[0]?.label ?? "Select"
  }

  const locationTriggerLabel = (): string => {
    const { location } = session.pending
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
    setMarketingOpen(false)
    setContactOpen(false)
    setSentimentOpen(false)
    setLocationOpen(false)
    setDateOpen(false)
    setTagsOpen(false)
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
              Filter guests
            </DialogTitle>
          </DialogHeader>

          <div className="grid gap-5 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <FieldLabel>Marketing status</FieldLabel>
              <Popover open={marketingOpen} onOpenChange={setMarketingOpen}>
                <PopoverTrigger asChild>
                  <Button
                    type="button"
                    variant="outline"
                    className={cn(
                      SELECT_TRIGGER_CLASS,
                      session.pending.marketing.length === 0 && "text-[#7d7d7d]"
                    )}
                  >
                    <span>
                      {session.pending.marketing.length > 0
                        ? `${session.pending.marketing.length} selected`
                        : "Select"}
                    </span>
                    <ChevronDownIcon className="size-4 shrink-0 opacity-60" />
                  </Button>
                </PopoverTrigger>
                <PopoverContent align="start" className={SELECT_MENU_CLASS}>
                  <ul className={SELECT_LIST_CLASS} role="listbox">
                    {(
                      Object.entries(MARKETING_LABELS) as Array<
                        [MarketingOptionId, string]
                      >
                    ).map(([id, label]) => (
                      <li key={id}>
                        <Button
                          type="button"
                          variant="ghost"
                          role="option"
                          aria-selected={session.pending.marketing.includes(id)}
                          className={cn(
                            SELECT_ITEM_CLASS,
                            session.pending.marketing.includes(id) &&
                              SELECT_ITEM_ACTIVE_CLASS
                          )}
                          onClick={() => {
                            onSessionChange(toggleMarketing(session, id))
                          }}
                        >
                          {label}
                        </Button>
                      </li>
                    ))}
                  </ul>
                </PopoverContent>
              </Popover>
              <RemovableChips chips={marketingChips} onRemove={removeChip} />
            </div>

            <div className="flex flex-col gap-2">
              <FieldLabel>Contact channel</FieldLabel>
              <Popover open={contactOpen} onOpenChange={setContactOpen}>
                <PopoverTrigger asChild>
                  <Button
                    type="button"
                    variant="outline"
                    className={cn(
                      SELECT_TRIGGER_CLASS,
                      session.pending.contact.length === 0 && "text-[#7d7d7d]"
                    )}
                  >
                    <span>
                      {session.pending.contact.length > 0
                        ? `${session.pending.contact.length} selected`
                        : "Select"}
                    </span>
                    <ChevronDownIcon className="size-4 shrink-0 opacity-60" />
                  </Button>
                </PopoverTrigger>
                <PopoverContent align="start" className={SELECT_MENU_CLASS}>
                  <ul className={SELECT_LIST_CLASS} role="listbox">
                    {(
                      Object.entries(CONTACT_LABELS) as Array<
                        [ContactOptionId, string]
                      >
                    ).map(([id, label]) => (
                      <li key={id}>
                        <Button
                          type="button"
                          variant="ghost"
                          role="option"
                          className={cn(
                            SELECT_ITEM_CLASS,
                            session.pending.contact.includes(id) &&
                              SELECT_ITEM_ACTIVE_CLASS
                          )}
                          onClick={() => {
                            onSessionChange(toggleContact(session, id))
                          }}
                        >
                          {label}
                        </Button>
                      </li>
                    ))}
                  </ul>
                </PopoverContent>
              </Popover>
              <RemovableChips chips={contactChips} onRemove={removeChip} />
            </div>
          </div>

          <Separator />

          <div className="grid gap-5 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <FieldLabel>Feedback classification</FieldLabel>
              <Popover open={sentimentOpen} onOpenChange={setSentimentOpen}>
                <PopoverTrigger asChild>
                  <Button
                    type="button"
                    variant="outline"
                    className={cn(
                      SELECT_TRIGGER_CLASS,
                      session.pending.sentiment.length === 0 && "text-[#7d7d7d]"
                    )}
                  >
                    <span>
                      {session.pending.sentiment.length > 0
                        ? `${session.pending.sentiment.length} selected`
                        : "Select"}
                    </span>
                    <ChevronDownIcon className="size-4 shrink-0 opacity-60" />
                  </Button>
                </PopoverTrigger>
                <PopoverContent align="start" className={SELECT_MENU_CLASS}>
                  <ul className={SELECT_LIST_CLASS} role="listbox">
                    {(
                      Object.entries(SENTIMENT_LABELS) as Array<
                        [SentimentOptionId, string]
                      >
                    ).map(([id, label]) => (
                      <li key={id}>
                        <Button
                          type="button"
                          variant="ghost"
                          role="option"
                          className={cn(
                            SELECT_ITEM_CLASS,
                            session.pending.sentiment.includes(id) &&
                              SELECT_ITEM_ACTIVE_CLASS
                          )}
                          onClick={() => {
                            onSessionChange(toggleSentiment(session, id))
                          }}
                        >
                          {label}
                        </Button>
                      </li>
                    ))}
                  </ul>
                </PopoverContent>
              </Popover>
              <RemovableChips chips={sentimentChips} onRemove={removeChip} />
            </div>

            {showLocationFilter ? (
              <div className="flex flex-col gap-2">
                <FieldLabel>Location</FieldLabel>
                <Popover
                  open={locationOpen}
                  onOpenChange={(next) => {
                    setLocationOpen(next)
                    if (next) {
                      onSessionChange({
                        ...session,
                        locationStep:
                          session.pending.location.kind === "individual"
                            ? "individual"
                            : "mode",
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
                        session.pending.location.kind === "none" &&
                          "text-[#7d7d7d]"
                      )}
                    >
                      <span className="truncate">{locationTriggerLabel()}</span>
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
                              onSessionChange(setLocationAll(session))
                              setLocationOpen(false)
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
                              onSessionChange(beginLocationIndividual(session))
                            }}
                          >
                            Individual permitted locations
                          </Button>
                        </li>
                        {session.pending.location.kind !== "none" ? (
                          <li>
                            <Button
                              type="button"
                              variant="ghost"
                              className={SELECT_ITEM_MUTED_CLASS}
                              onClick={() => {
                                onSessionChange(clearLocation(session))
                                setLocationOpen(false)
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
                              onSessionChange({
                                ...session,
                                locationStep: "mode",
                              })
                            }}
                          >
                            ← Back
                          </Button>
                        </li>
                        {locations.map((location) => {
                          const selected =
                            session.pending.location.kind === "individual" &&
                            session.pending.location.locationIds.includes(
                              location.id
                            )
                          return (
                            <li key={location.id}>
                              <Button
                                type="button"
                                variant="ghost"
                                role="option"
                                aria-selected={selected}
                                className={cn(
                                  SELECT_ITEM_CLASS,
                                  selected && SELECT_ITEM_ACTIVE_CLASS
                                )}
                                onClick={() => {
                                  onSessionChange(
                                    toggleLocationId(session, location.id)
                                  )
                                }}
                              >
                                {location.name}
                              </Button>
                            </li>
                          )
                        })}
                      </ul>
                    )}
                  </PopoverContent>
                </Popover>
                <RemovableChips chips={locationChips} onRemove={removeChip} />
              </div>
            ) : (
              <div />
            )}
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
                    onSessionChange(beginDateAxisPick(session))
                  } else {
                    setDateCustomStep(false)
                    setDraftRange(undefined)
                    onSessionChange({
                      ...session,
                      dateStep: null,
                      dateDraftAxis:
                        session.pending.date.kind === "none"
                          ? null
                          : session.dateDraftAxis,
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
                    dateCustomStep
                      ? "w-auto p-0"
                      : SELECT_MENU_CLASS
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
                                pickDatePreset(session, "custom", {
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
                  ) : session.dateStep !== "preset" ? (
                    <ul className={SELECT_LIST_CLASS} role="listbox">
                      {(
                        Object.entries(DATE_AXIS_LABELS) as Array<
                          [DateAxisId, string]
                        >
                      ).map(([id, label]) => (
                        <li key={id}>
                          <Button
                            type="button"
                            variant="ghost"
                            className={SELECT_ITEM_CLASS}
                            onClick={() => {
                              onSessionChange(pickDateAxis(session, id))
                            }}
                          >
                            {label}
                          </Button>
                        </li>
                      ))}
                      {session.pending.date.kind !== "none" ? (
                        <li>
                          <Button
                            type="button"
                            variant="ghost"
                            className={SELECT_ITEM_MUTED_CLASS}
                            onClick={() => {
                              onSessionChange(clearDate(session))
                              setDateOpen(false)
                            }}
                          >
                            Any time (clear)
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
                            onSessionChange({
                              ...session,
                              dateStep: "axis",
                              dateDraftAxis: null,
                            })
                          }}
                        >
                          ← Change axis
                        </Button>
                      </li>
                      <li>
                        <Button
                          type="button"
                          variant="ghost"
                          className={SELECT_ITEM_CLASS}
                          onClick={() => {
                            onSessionChange(pickDatePreset(session, "any-time"))
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
                              onSessionChange(pickDatePreset(session, id))
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
                            const existing =
                              session.pending.date.kind === "custom"
                                ? {
                                    from: parseLocalDateKey(
                                      session.pending.date.dateFrom
                                    ),
                                    to: parseLocalDateKey(
                                      session.pending.date.dateTo
                                    ),
                                  }
                                : undefined
                            setDraftRange(existing)
                            setDateCustomStep(true)
                          }}
                        >
                          Custom date range
                        </Button>
                      </li>
                    </ul>
                  )}
                </PopoverContent>
              </Popover>
              <RemovableChips chips={dateChips} onRemove={removeChip} />
            </div>

            <div className="flex flex-col gap-2">
              <FieldLabel>Tags</FieldLabel>
              <Popover open={tagsOpen} onOpenChange={setTagsOpen}>
                <PopoverTrigger asChild>
                  <Button
                    type="button"
                    variant="outline"
                    className={cn(
                      SELECT_TRIGGER_CLASS,
                      session.pending.tagIds.length === 0 && "text-[#7d7d7d]"
                    )}
                  >
                    <span>
                      {session.pending.tagIds.length > 0
                        ? `${session.pending.tagIds.length} selected`
                        : "Select"}
                    </span>
                    <ChevronDownIcon className="size-4 shrink-0 opacity-60" />
                  </Button>
                </PopoverTrigger>
                <PopoverContent align="start" className={SELECT_MENU_CLASS}>
                  <ul className={SELECT_LIST_CLASS} role="listbox">
                    {tags.map((tag) => (
                      <li key={tag.id}>
                        <Button
                          type="button"
                          variant="ghost"
                          role="option"
                          className={cn(
                            SELECT_ITEM_CLASS,
                            session.pending.tagIds.includes(tag.id) &&
                              SELECT_ITEM_ACTIVE_CLASS
                          )}
                          onClick={() => {
                            onSessionChange(toggleTag(session, tag.id))
                          }}
                        >
                          {tag.name}
                        </Button>
                      </li>
                    ))}
                    {tags.length === 0 ? (
                      <li className="rounded-md px-2.5 py-1.5 text-sm text-muted-foreground">
                        No tags yet
                      </li>
                    ) : null}
                  </ul>
                </PopoverContent>
              </Popover>
              <RemovableChips chips={tagChips} onRemove={removeChip} />
            </div>
          </div>
        </div>

        <DialogFooter className="flex-row gap-3 sm:justify-start">
          <Button
            type="button"
            disabled={!dirty}
            className="h-auto min-h-0 rounded-[2px] px-4 py-2.5 text-sm"
            onClick={() => {
              onApply(applyPendingFilters(session))
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
              onSessionChange(clearAllPending(session))
            }}
          >
            Clear all
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

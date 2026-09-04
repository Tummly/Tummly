import { useState } from "react"
import { XIcon } from "lucide-react"

import { PerformanceDateRangeControl } from "@/components/dashboard/operator/Home/PerformanceDateRangeControl"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  labelForHomePerformanceDateRange,
  toLocalDateKey,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import {
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import {
  ALL_OWNED_LOCATIONS_PICKER_LABEL,
  ALL_OWNED_LOCATIONS_SELECT_VALUE,
  type OperatorAiAssistantChangeScopeDialogSnapshot,
  type OperatorAiAssistantDraftLocation,
  type OperatorAiAssistantOwnedLocationOption,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import { cn } from "@/lib/utils"

type AiAssistantChangeScopeDialogProps = {
  dialog: OperatorAiAssistantChangeScopeDialogSnapshot
  onOpenChange: (open: boolean) => void
  onDraftLocationChange: (locationId: OperatorAiAssistantDraftLocation) => void
  onDraftReportingPeriodChange: (range: HomePerformanceDateRange) => void
  onApply: () => void
}

/** Portaled menus inside Change analysis scope — above Dialog (`z-[120]`). */
const DIALOG_MENU_CLASS = `${OPERATOR_SHELL_MENU_PANEL_CLASS} min-w-[var(--radix-select-trigger-width)] gap-0 px-0 py-1 z-[140] p-0`

const SELECT_TRIGGER_CLASS =
  "h-auto min-h-[50px] w-full justify-between rounded border border-op-input-border bg-transparent px-[15px] py-[15px] text-sm font-normal text-op-text-primary shadow-none data-placeholder:text-[var(--op-color-gray-550)] dark:bg-transparent dark:hover:bg-transparent"

const CUSTOM_DATE_TRIGGER_CLASS = cn(
  SELECT_TRIGGER_CLASS,
  "justify-start text-left [&_svg:last-child]:ml-auto cursor-pointer"
)

const SELECT_ITEM_CLASS = [
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  "cursor-pointer px-3 py-2 text-sm text-foreground focus:bg-white/10 focus:text-white dark:focus:bg-white/10",
  "data-[state=checked]:font-medium data-[state=checked]:text-primary",
  "[&>span.absolute]:hidden",
].join(" ")

const FIELD_LABEL_CLASS =
  "text-sm font-semibold leading-5 text-op-text-primary"

const DEFAULT_LOCATIONS: readonly OperatorAiAssistantOwnedLocationOption[] = [
  { id: 11, name: "Camden" },
  { id: 12, name: "Shoreditch" },
]

const REPORTING_PERIOD_OPTIONS = [
  { value: "last7", label: "Last 7 days" },
  { value: "last30", label: "Last 30 days" },
  { value: "thisMonth", label: "This month" },
  { value: "previousMonth", label: "Previous month" },
  { value: "custom", label: "Custom range" },
] as const

/** Change analysis scope — Figma 3450:53798. */
export function AiAssistantChangeScopeDialog({
  dialog,
  onOpenChange,
  onDraftLocationChange,
  onDraftReportingPeriodChange,
  onApply,
}: AiAssistantChangeScopeDialogProps) {
  const [customPickerOpen, setCustomPickerOpen] = useState(false)

  const availableLocations =
    dialog.locationOptions.length >= 2
      ? dialog.locationOptions
      : [
          ...dialog.locationOptions,
          ...DEFAULT_LOCATIONS.filter(
            (def) =>
              !dialog.locationOptions.some(
                (loc) =>
                  loc.id === def.id ||
                  loc.name.toLowerCase() === def.name.toLowerCase()
              )
          ),
        ]

  const selectedLocationName =
    dialog.draftScopeKind === "all"
      ? ALL_OWNED_LOCATIONS_PICKER_LABEL
      : availableLocations.find(
          (location) => location.id === dialog.draftOwnedLocationId
        )?.name ??
        availableLocations[0]?.name ??
        "Camden"

  const locationSelectValue =
    dialog.draftScopeKind === "all"
      ? ALL_OWNED_LOCATIONS_SELECT_VALUE
      : dialog.draftOwnedLocationId != null
        ? String(dialog.draftOwnedLocationId)
        : String(availableLocations[0]?.id ?? ALL_OWNED_LOCATIONS_SELECT_VALUE)

  const reportingPeriodSelectValue =
    dialog.draftReportingPeriod.kind === "preset"
      ? dialog.draftReportingPeriod.presetId
      : "custom"

  const selectedReportingPeriodLabel =
    REPORTING_PERIOD_OPTIONS.find(
      (option) => option.value === reportingPeriodSelectValue
    )?.label ?? "Last 7 days"

  return (
    <Dialog
      open={dialog.open}
      onOpenChange={(open) => {
        onOpenChange(open)
      }}
    >
      <DialogContent
        showCloseButton={false}
        className="gap-[60px] bg-[var(--op-color-gray-995)] p-8 text-op-text-primary sm:max-w-[520px]"
      >
        <div className="flex flex-col gap-[30px]">
          <div className="flex items-start gap-[22px]">
            <DialogHeader className="min-w-0 flex-1 gap-0 text-left">
              <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
                {dialog.title}
              </DialogTitle>
              <DialogDescription className="sr-only">
                Set Restaurant and Reporting period for this conversation.
              </DialogDescription>
            </DialogHeader>
            <DialogClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                aria-label="Close"
                className="shrink-0"
              >
                <XIcon aria-hidden />
              </Button>
            </DialogClose>
          </div>

          <div className="flex flex-col gap-2">
            <Label
              htmlFor="ai-assistant-restaurant"
              className={FIELD_LABEL_CLASS}
            >
              Restaurant
            </Label>
            <Select
              value={locationSelectValue}
              onValueChange={(value) => {
                if (value === ALL_OWNED_LOCATIONS_SELECT_VALUE) {
                  onDraftLocationChange(ALL_OWNED_LOCATIONS_SELECT_VALUE)
                  return
                }
                const locationId = Number.parseInt(value, 10)
                if (Number.isFinite(locationId)) {
                  onDraftLocationChange(locationId)
                }
              }}
            >
              <SelectTrigger
                id="ai-assistant-restaurant"
                className={SELECT_TRIGGER_CLASS}
                aria-label="Restaurant"
              >
                <SelectValue placeholder={selectedLocationName} />
              </SelectTrigger>
              <SelectContent
                position="popper"
                align="start"
                className={DIALOG_MENU_CLASS}
              >
                <SelectGroup className="p-0">
                  <SelectItem
                    value={ALL_OWNED_LOCATIONS_SELECT_VALUE}
                    className={SELECT_ITEM_CLASS}
                  >
                    All locations
                  </SelectItem>
                  {availableLocations.map((location) => (
                    <SelectItem
                      key={location.id}
                      value={String(location.id)}
                      className={SELECT_ITEM_CLASS}
                    >
                      {location.name}
                    </SelectItem>
                  ))}
                </SelectGroup>
              </SelectContent>
            </Select>
          </div>

          <div className="flex w-full flex-col gap-2">
            <Label
              htmlFor="ai-assistant-reporting-period"
              className={FIELD_LABEL_CLASS}
            >
              Reporting period
            </Label>
            <Select
              value={reportingPeriodSelectValue}
              onValueChange={(value) => {
                if (value === "custom") {
                  if (dialog.draftReportingPeriod.kind !== "custom") {
                    const end = new Date()
                    const start = new Date()
                    start.setDate(end.getDate() - 6)
                    onDraftReportingPeriodChange({
                      kind: "custom",
                      startDate: toLocalDateKey(start),
                      endDate: toLocalDateKey(end),
                    })
                  }
                  setCustomPickerOpen(true)
                  return
                }
                setCustomPickerOpen(false)
                if (
                  value === "last7" ||
                  value === "last30" ||
                  value === "thisMonth" ||
                  value === "previousMonth"
                ) {
                  onDraftReportingPeriodChange({
                    kind: "preset",
                    presetId: value,
                  })
                }
              }}
            >
              <SelectTrigger
                id="ai-assistant-reporting-period"
                className={SELECT_TRIGGER_CLASS}
                aria-label="Reporting period"
              >
                <SelectValue placeholder={selectedReportingPeriodLabel} />
              </SelectTrigger>
              <SelectContent
                position="popper"
                align="start"
                className={DIALOG_MENU_CLASS}
              >
                <SelectGroup className="p-0">
                  {REPORTING_PERIOD_OPTIONS.map((option) => (
                    <SelectItem
                      key={option.value}
                      value={option.value}
                      className={SELECT_ITEM_CLASS}
                    >
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectGroup>
              </SelectContent>
            </Select>

            {reportingPeriodSelectValue === "custom" ? (
              <div className="pt-2">
                <PerformanceDateRangeControl
                  dateRangeLabel={labelForHomePerformanceDateRange(
                    dialog.draftReportingPeriod
                  )}
                  selectedRange={dialog.draftReportingPeriod}
                  onCommitRange={onDraftReportingPeriodChange}
                  title="Select custom date range"
                  triggerClassName={CUSTOM_DATE_TRIGGER_CLASS}
                  contentClassName="z-[150]"
                  defaultStep="custom"
                  open={customPickerOpen}
                  onOpenChange={setCustomPickerOpen}
                />
              </div>
            ) : null}
          </div>
        </div>

        <DialogFooter className="flex-row gap-3 sm:justify-start">
          <Button type="button" variant="op-primary" onClick={onApply}>
            Apply scope
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            onClick={() => {
              onOpenChange(false)
            }}
          >
            Cancel
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

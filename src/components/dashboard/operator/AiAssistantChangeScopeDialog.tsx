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
  SelectSeparator,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  labelForHomePerformanceDateRange,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import {
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import {
  ALL_OWNED_LOCATIONS_PICKER_LABEL,
  ALL_OWNED_LOCATIONS_SELECT_VALUE,
  changeScopeLocationSelectValue,
  type OperatorAiAssistantChangeScopeDialogSnapshot,
  type OperatorAiAssistantDraftLocation,
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
const DIALOG_MENU_CLASS = `${OPERATOR_SHELL_MENU_PANEL_CLASS} min-w-40 gap-0 px-0 py-1 z-[130] p-0`

const SELECT_TRIGGER_CLASS =
  "h-auto min-h-[50px] w-full justify-between rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm font-normal text-[var(--op-color-gray-550)] shadow-none dark:bg-transparent dark:hover:bg-transparent"

/** Full-width period field — label left, chevron right (not centered). */
const PERIOD_TRIGGER_CLASS = cn(
  SELECT_TRIGGER_CLASS,
  "justify-start text-left [&_svg:last-child]:ml-auto"
)

const SELECT_ITEM_CLASS = [
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  "pr-3 focus:bg-black/5 focus:text-inherit dark:focus:bg-white/5",
  "data-[state=checked]:bg-transparent data-[state=checked]:font-medium data-[state=checked]:text-primary",
  "data-[state=checked]:focus:bg-transparent data-[state=checked]:focus:text-primary",
  "data-[state=checked]:hover:bg-transparent data-[state=checked]:hover:text-primary",
  "[&>span.absolute]:hidden",
].join(" ")

const FIELD_LABEL_CLASS =
  "text-sm font-semibold leading-5 text-op-text-primary"

/** Change analysis scope — Figma 3450:53798. */
export function AiAssistantChangeScopeDialog({
  dialog,
  onOpenChange,
  onDraftLocationChange,
  onDraftReportingPeriodChange,
  onApply,
}: AiAssistantChangeScopeDialogProps) {
  const selectedLocationName =
    dialog.draftScopeKind === "all"
      ? ALL_OWNED_LOCATIONS_PICKER_LABEL
      : dialog.locationOptions.find(
          (location) => location.id === dialog.draftOwnedLocationId
        )?.name ?? ""

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
                {dialog.showsOwnedLocationField
                  ? "Set Owned location and Reporting period for this conversation."
                  : "Set Reporting period for this conversation."}
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

          {dialog.showsOwnedLocationField ? (
            <div className="flex flex-col gap-2">
              <Label
                htmlFor="ai-assistant-owned-location"
                className={FIELD_LABEL_CLASS}
              >
                Owned location
              </Label>
              <Select
                value={changeScopeLocationSelectValue(dialog)}
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
                  id="ai-assistant-owned-location"
                  className={SELECT_TRIGGER_CLASS}
                  aria-label="Owned location"
                >
                  <SelectValue placeholder={selectedLocationName} />
                </SelectTrigger>
                <SelectContent
                  position="popper"
                  align="start"
                  className={DIALOG_MENU_CLASS}
                >
                  <SelectGroup className="p-0">
                    {dialog.includesAllOwnedLocationsOption ? (
                      <>
                        <SelectItem
                          value={ALL_OWNED_LOCATIONS_SELECT_VALUE}
                          className={SELECT_ITEM_CLASS}
                        >
                          {ALL_OWNED_LOCATIONS_PICKER_LABEL}
                        </SelectItem>
                        <SelectSeparator className="bg-op-input-border" />
                      </>
                    ) : null}
                    {dialog.locationOptions.map((location) => (
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
          ) : null}

          <div className="flex w-full flex-col gap-2">
            <Label className={FIELD_LABEL_CLASS}>Reporting period</Label>
            <div className="w-full">
              <PerformanceDateRangeControl
                dateRangeLabel={labelForHomePerformanceDateRange(
                  dialog.draftReportingPeriod
                )}
                selectedRange={dialog.draftReportingPeriod}
                onCommitRange={onDraftReportingPeriodChange}
                title="Select Reporting period"
                triggerClassName={PERIOD_TRIGGER_CLASS}
                contentClassName="z-[130]"
              />
            </div>
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

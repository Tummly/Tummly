import { useState } from "react"
import { format } from "date-fns"
import type { LucideIcon } from "lucide-react"
import { CalendarIcon, SendIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Calendar } from "@/components/ui/calendar"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import type { CampaignScheduleModeId } from "@/lib/operatorCampaigns/campaignSchedulePresentation"
import { CAMPAIGN_SCHEDULE_COPY } from "@/lib/operatorCampaigns/campaignSchedulePresentation"
import { CAMPAIGN_WIZARD_SELECT_MENU_CLASS } from "@/lib/operatorCampaigns/campaignWizardPresentation"
import type {
  CampaignScheduleOptionViewModel,
  CampaignScheduleViewModel,
} from "@/lib/operatorCampaigns/createCampaignWizardModule"
import {
  FEEDBACK_DIALOG_SELECT_ITEM_CLASS,
  FEEDBACK_FIELD_LABEL_CLASS,
  FEEDBACK_INPUT_CLASS,
  FEEDBACK_RECOVERY_SELECT_MENU_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import {
  parseLocalDateKey,
  toLocalDateKey,
} from "@/lib/operatorHome/homePerformanceDateRange"
import { cn } from "@/lib/utils"

const SCHEDULE_MODE_ICONS: Record<CampaignScheduleModeId, LucideIcon> = {
  "send-now": SendIcon,
  "schedule-later": CalendarIcon,
}

const SCHEDULE_DATE_TRIGGER_CLASS = cn(
  FEEDBACK_INPUT_CLASS,
  "h-[50px] w-full justify-start gap-3 px-[15px] py-[15px] text-left font-normal shadow-none hover:bg-transparent"
)

type CampaignScheduleStepProps = {
  schedule: CampaignScheduleViewModel
  onSelectMode: (modeId: CampaignScheduleModeId) => void
  onScheduleDateChange: (value: string) => void
  onScheduleTimeChange: (value: string) => void
}

function ScheduleModeCard({
  option,
  onSelect,
}: {
  option: CampaignScheduleOptionViewModel
  onSelect: () => void
}) {
  const Icon = SCHEDULE_MODE_ICONS[option.id]

  return (
    <Button
      type="button"
      variant="ghost"
      role="radio"
      aria-checked={option.selected}
      className={cn(
        "h-auto min-h-0 w-full items-center justify-start gap-2.5 rounded-[4px] border px-[18px] py-4 text-left whitespace-normal hover:bg-transparent",
        option.selected
          ? "border-[var(--op-color-gray-550)] bg-op-background-primary"
          : "border-op-card-border bg-op-background-primary hover:border-[var(--op-color-gray-550)]"
      )}
      onClick={onSelect}
    >
      <span className="flex size-9 shrink-0 items-center justify-center rounded-[2px] bg-op-background-secondary p-2.5">
        <Icon className="size-4 text-op-text-primary" aria-hidden />
      </span>
      <span className="flex min-w-0 flex-col gap-1">
        <span className="text-sm font-medium leading-normal text-op-text-primary">
          {option.title}
        </span>
        <span className="text-xs font-medium leading-normal text-[var(--op-color-gray-550)]">
          {option.description}
        </span>
      </span>
    </Button>
  )
}

function EstimatedUsageSummary({
  schedule,
}: {
  schedule: CampaignScheduleViewModel
}) {
  const { usageSummary } = schedule

  return (
    <aside
      className="flex w-full shrink-0 flex-col gap-6 rounded-[4px] border border-op-card-border bg-op-background-primary p-5 lg:w-[min(100%,560px)]"
      aria-label={usageSummary.title}
    >
      <div className="flex flex-col gap-2">
        <h3 className="m-0 text-lg font-semibold leading-normal text-op-text-primary">
          {usageSummary.title}
        </h3>
        <p className="m-0 text-sm font-medium leading-normal text-[var(--op-color-gray-550)]">
          {usageSummary.audienceLine}
        </p>
      </div>
      <dl className="m-0 flex w-full flex-col gap-3.5">
        {usageSummary.rows.map((row, index) => (
          <div key={row.label} className="flex flex-col gap-3.5">
            <div className="flex items-center justify-between gap-3 text-sm">
              <dt className="m-0 font-semibold text-[var(--op-color-gray-550)]">
                {row.label}
              </dt>
              <dd className="m-0 font-medium text-op-text-primary">{row.value}</dd>
            </div>
            {index < usageSummary.rows.length - 1 ? (
              <div className="h-px w-full bg-op-card-border" aria-hidden />
            ) : null}
          </div>
        ))}
      </dl>
    </aside>
  )
}

function ScheduleSendDateField({
  dateLocal,
  onScheduleDateChange,
}: {
  dateLocal: string
  onScheduleDateChange: (value: string) => void
}) {
  const [open, setOpen] = useState(false)
  const selectedDate =
    dateLocal.trim().length > 0 ? parseLocalDateKey(dateLocal) : undefined
  const hasDate = selectedDate != null && !Number.isNaN(selectedDate.getTime())

  return (
    <div className="flex w-full flex-col gap-2">
      <label
        htmlFor="campaign-schedule-date"
        className={FEEDBACK_FIELD_LABEL_CLASS}
      >
        {CAMPAIGN_SCHEDULE_COPY.sendDateLabel}
      </label>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            id="campaign-schedule-date"
            type="button"
            variant="op-ghost"
            className={cn(
              SCHEDULE_DATE_TRIGGER_CLASS,
              "!h-[50px] !min-h-[50px] rounded-[4px]",
              !hasDate && "text-op-input-placeholder"
            )}
          >
            <CalendarIcon
              className="size-4 shrink-0 text-op-text-primary"
              aria-hidden
            />
            <span className="truncate text-sm leading-5">
              {hasDate
                ? format(selectedDate, "d MMM yyyy")
                : CAMPAIGN_SCHEDULE_COPY.sendDatePlaceholder}
            </span>
          </Button>
        </PopoverTrigger>
        <PopoverContent
          align="start"
          className={cn(
            "w-auto p-0",
            FEEDBACK_RECOVERY_SELECT_MENU_CLASS,
            CAMPAIGN_WIZARD_SELECT_MENU_CLASS
          )}
        >
          <Calendar
            mode="single"
            selected={hasDate ? selectedDate : undefined}
            defaultMonth={hasDate ? selectedDate : undefined}
            onSelect={(date) => {
              if (date == null) {
                return
              }
              onScheduleDateChange(toLocalDateKey(date))
              setOpen(false)
            }}
          />
        </PopoverContent>
      </Popover>
    </div>
  )
}

function ScheduleSendTimeField({
  timeLocal,
  timeOptions,
  onScheduleTimeChange,
}: {
  timeLocal: string
  timeOptions: readonly string[]
  onScheduleTimeChange: (value: string) => void
}) {
  return (
    <div className="flex w-full flex-col gap-2">
      <label
        htmlFor="campaign-schedule-time"
        className={FEEDBACK_FIELD_LABEL_CLASS}
      >
        {CAMPAIGN_SCHEDULE_COPY.sendTimeLabel}
      </label>
      <Select
        value={timeLocal.trim().length > 0 ? timeLocal : undefined}
        onValueChange={onScheduleTimeChange}
      >
        <SelectTrigger
          id="campaign-schedule-time"
          className={cn(
            FEEDBACK_INPUT_CLASS,
            "h-[50px] w-full shadow-none data-[size=default]:h-[50px] dark:bg-transparent dark:hover:bg-transparent [&_svg]:text-op-input-placeholder"
          )}
        >
          <SelectValue
            placeholder={CAMPAIGN_SCHEDULE_COPY.sendTimePlaceholder}
          />
        </SelectTrigger>
        <SelectContent
          position="popper"
          align="start"
          className={cn(
            FEEDBACK_RECOVERY_SELECT_MENU_CLASS,
            CAMPAIGN_WIZARD_SELECT_MENU_CLASS
          )}
        >
          {timeOptions.map((time) => (
            <SelectItem
              key={time}
              value={time}
              className={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
            >
              {time}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  )
}

/**
 * Campaign wizard Schedule step — Figma 4751:67079 / ticket 26 / polish 03.
 * Send now vs Schedule for later; stacked Calendar + time select when later.
 */
export function CampaignScheduleStep({
  schedule,
  onSelectMode,
  onScheduleDateChange,
  onScheduleTimeChange,
}: CampaignScheduleStepProps) {
  return (
    <div className="flex w-full flex-col items-start justify-between gap-8 lg:flex-row lg:gap-[42px]">
      <div className="flex min-h-0 w-full max-w-[690px] flex-col gap-7">
        <header className="flex flex-col gap-2">
          <h2 className="m-0 text-xl font-semibold leading-normal text-op-text-primary sm:text-[22px]">
            {schedule.stepHeading}
          </h2>
          <p className="m-0 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
            {schedule.stepDescription}
          </p>
        </header>

        <div
          className="flex w-full flex-col gap-[18px]"
          role="radiogroup"
          aria-label={schedule.stepHeading}
        >
          {schedule.options.map((option) => (
            <ScheduleModeCard
              key={option.id}
              option={option}
              onSelect={() => {
                onSelectMode(option.id)
              }}
            />
          ))}
        </div>

        {schedule.showDatetimeFields ? (
          <div className="flex w-full flex-col gap-4">
            <ScheduleSendDateField
              dateLocal={schedule.dateLocal}
              onScheduleDateChange={onScheduleDateChange}
            />
            <ScheduleSendTimeField
              timeLocal={schedule.timeLocal}
              timeOptions={schedule.timeOptions}
              onScheduleTimeChange={onScheduleTimeChange}
            />
          </div>
        ) : null}
      </div>

      <EstimatedUsageSummary schedule={schedule} />
    </div>
  )
}

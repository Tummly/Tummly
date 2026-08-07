import type { LucideIcon } from "lucide-react"
import { CalendarIcon, SendIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import type { CampaignScheduleModeId } from "@/lib/operatorCampaigns/campaignSchedulePresentation"
import type {
  CampaignScheduleOptionViewModel,
  CampaignScheduleViewModel,
} from "@/lib/operatorCampaigns/createCampaignWizardModule"
import { cn } from "@/lib/utils"

const SCHEDULE_MODE_ICONS: Record<CampaignScheduleModeId, LucideIcon> = {
  "send-now": SendIcon,
  "schedule-later": CalendarIcon,
}

type CampaignScheduleStepProps = {
  schedule: CampaignScheduleViewModel
  onSelectMode: (modeId: CampaignScheduleModeId) => void
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

/**
 * Campaign wizard Schedule step — Figma 4751:67079 / ticket 27.
 * Timing chrome only; no schedule reservation or send API.
 */
export function CampaignScheduleStep({
  schedule,
  onSelectMode,
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
      </div>

      <EstimatedUsageSummary schedule={schedule} />
    </div>
  )
}

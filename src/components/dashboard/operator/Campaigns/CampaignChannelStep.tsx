import type { LucideIcon } from "lucide-react"
import { MailIcon, MessageSquareIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import type { CampaignChannelId } from "@/lib/operatorCampaigns/campaignChannelPresentation"
import type {
  CampaignChannelOptionViewModel,
  CampaignChannelViewModel,
} from "@/lib/operatorCampaigns/createCampaignWizardModule"
import { cn } from "@/lib/utils"

const CHANNEL_ICONS: Record<CampaignChannelId, LucideIcon> = {
  email: MailIcon,
  sms: MessageSquareIcon,
}

type CampaignChannelStepProps = {
  channel: CampaignChannelViewModel
  onSelectChannel: (channelId: CampaignChannelId) => void
  /** Inert until SMS purchase lands (same as overview Messaging usage). */
  onBuySmsCredits?: () => void
}

function ChannelOptionCard({
  option,
  onSelect,
}: {
  option: CampaignChannelOptionViewModel
  onSelect: () => void
}) {
  const Icon = CHANNEL_ICONS[option.id]

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
  channel,
}: {
  channel: CampaignChannelViewModel
}) {
  const { usageSummary } = channel

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
 * Campaign wizard Channel step — Figma 4707:52097.
 * Email / SMS only; usage from shared messaging fixtures (no balance API).
 */
export function CampaignChannelStep({
  channel,
  onSelectChannel,
  onBuySmsCredits,
}: CampaignChannelStepProps) {
  return (
    <div className="flex w-full flex-col items-start justify-between gap-8 lg:flex-row lg:gap-[42px]">
      <div className="flex min-h-0 w-full max-w-[690px] flex-col gap-7">
        <header className="flex flex-col gap-2">
          <h2 className="m-0 text-xl font-semibold leading-normal text-op-text-primary sm:text-[22px]">
            {channel.stepHeading}
          </h2>
          <p className="m-0 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
            {channel.stepDescription}
          </p>
        </header>

        <div
          className="flex w-full flex-col gap-[18px]"
          role="radiogroup"
          aria-label={channel.stepHeading}
        >
          {channel.options.map((option) => (
            <ChannelOptionCard
              key={option.id}
              option={option}
              onSelect={() => {
                onSelectChannel(option.id)
              }}
            />
          ))}
        </div>

        {channel.smsShortfall != null ? (
          <div className="flex w-full flex-col gap-[22px] rounded-[4px] bg-[var(--op-color-gray-995)] p-[18px]">
            <div className="flex flex-col gap-1.5">
              <p className="m-0 text-sm font-medium text-op-text-primary">
                {channel.smsShortfall.title}
              </p>
              <p className="m-0 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
                {channel.smsShortfall.body}
              </p>
            </div>
            <Button
              type="button"
              variant="op-link"
              className="h-auto min-h-0 w-fit p-0"
              onClick={onBuySmsCredits}
            >
              {channel.smsShortfall.buyCreditsLabel}
            </Button>
          </div>
        ) : null}
      </div>

      <EstimatedUsageSummary channel={channel} />
    </div>
  )
}

import { MailIcon, MessageSquareIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import { CAMPAIGN_MESSAGING_BALANCES_LOAD_ERROR } from "@/lib/operatorCampaigns/campaignMessagingBalances"
import type { CampaignsMessagingChromeAction } from "@/lib/operatorCampaigns/campaignsMessagingCreditChrome"
import {
  CAMPAIGNS_MESSAGING_USAGE_ACTIONS_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_METER_FILL_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_METER_ROW_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_METER_TRACK_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_METERS_ROW_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_TILE_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_TILE_DETAIL_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS,
} from "@/lib/operatorCampaigns/campaignsPresentation"
import type { OperatorCampaignsMessagingUsageSection } from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"
import {
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type CampaignsMessagingUsageProps = {
  id?: string
  messagingUsage: OperatorCampaignsMessagingUsageSection
  onRetry?: () => void
  onAction?: (action: CampaignsMessagingChromeAction) => void
}

function UsageMeter({
  fillRatio,
  maxLabel,
}: {
  fillRatio: number
  maxLabel: string
}) {
  const clamped = Math.min(1, Math.max(0, fillRatio))

  return (
    <div className={CAMPAIGNS_MESSAGING_USAGE_METER_ROW_CLASS}>
      <div
        className={CAMPAIGNS_MESSAGING_USAGE_METER_TRACK_CLASS}
        role="meter"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={Math.round(clamped * 100)}
      >
        <div
          className={CAMPAIGNS_MESSAGING_USAGE_METER_FILL_CLASS}
          style={{ width: `${clamped * 100}%` }}
        />
      </div>
      <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS}>{maxLabel}</p>
    </div>
  )
}

function ChannelActions({
  actions,
  onAction,
}: {
  actions: CampaignsMessagingChromeAction[]
  onAction?: (action: CampaignsMessagingChromeAction) => void
}) {
  if (actions.length === 0) {
    return null
  }

  return (
    <div className="flex flex-wrap items-center gap-3">
      {actions.map((action) => (
        <Button
          key={`${action.kind}:${action.label}`}
          type="button"
          variant={action.kind === "view-usage" ? "op-tertiary" : "op-link"}
          className={
            action.kind === "view-usage"
              ? GUESTS_PAGE_SECONDARY_BUTTON_CLASS
              : "h-auto min-h-0 w-fit p-0"
          }
          onClick={() => {
            onAction?.(action)
          }}
        >
          {action.label}
        </Button>
      ))}
    </div>
  )
}

/** Messaging usage — Email + SMS meters; live Billing usage after cutover (ticket 23). */
export function CampaignsMessagingUsage({
  id,
  messagingUsage,
  onRetry,
  onAction,
}: CampaignsMessagingUsageProps) {
  if (messagingUsage.status === "load-failed") {
    return (
      <section
        id={id}
        className={GUESTS_SECTION_CLASS}
        aria-label="Messaging usage"
      >
        <header className="flex flex-col gap-2 leading-0">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>Messaging usage</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            {messagingUsage.errorMessage
              ?? CAMPAIGN_MESSAGING_BALANCES_LOAD_ERROR}
          </p>
        </header>
        {onRetry != null ? (
          <div className={CAMPAIGNS_MESSAGING_USAGE_ACTIONS_CLASS}>
            <Button
              type="button"
              variant="op-tertiary"
              className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
              onClick={onRetry}
            >
              Try again
            </Button>
          </div>
        ) : null}
      </section>
    )
  }

  const viewModel = messagingUsage.viewModel
  if (viewModel == null) {
    return null
  }

  const { email, sms } = viewModel

  return (
    <section
      id={id}
      className={GUESTS_SECTION_CLASS}
      aria-label={viewModel.title}
    >
      <header className="flex flex-col gap-2 leading-0">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{viewModel.title}</h2>
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{viewModel.subtitle}</p>
      </header>

      <div className="flex flex-col gap-3">
        <div className={CAMPAIGNS_MESSAGING_USAGE_METERS_ROW_CLASS}>
          <div className={CAMPAIGNS_MESSAGING_USAGE_TILE_CLASS}>
            <div className="flex flex-col gap-3">
              <MailIcon
                className="size-5 text-op-card-title-color"
                aria-hidden
              />
              <div className="flex flex-col gap-1">
                <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS}>
                  {email.title}
                </p>
                <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS}>
                  {email.headline}
                </p>
              </div>
              <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_DETAIL_CLASS}>
                {email.subline}
              </p>
              {email.purchasedLine != null ? (
                <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_DETAIL_CLASS}>
                  {email.purchasedLine}
                </p>
              ) : null}
              <ChannelActions actions={email.actions} onAction={onAction} />
            </div>
            <UsageMeter
              fillRatio={email.fillRatio}
              maxLabel={email.meterMaxLabel}
            />
          </div>

          <div className={CAMPAIGNS_MESSAGING_USAGE_TILE_CLASS}>
            <div className="flex flex-col gap-3">
              <MessageSquareIcon
                className="size-5 text-op-card-title-color"
                aria-hidden
              />
              <div className="flex flex-col gap-1">
                <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS}>
                  {sms.title}
                </p>
                <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS}>
                  {sms.headline}
                </p>
              </div>
              <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_DETAIL_CLASS}>
                {sms.subline}
              </p>
              {sms.purchasedLine != null ? (
                <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_DETAIL_CLASS}>
                  {sms.purchasedLine}
                </p>
              ) : null}
              <ChannelActions actions={sms.actions} onAction={onAction} />
            </div>
            <UsageMeter
              fillRatio={sms.fillRatio}
              maxLabel={sms.meterMaxLabel}
            />
          </div>
        </div>
      </div>

      <div className={CAMPAIGNS_MESSAGING_USAGE_ACTIONS_CLASS}>
        <ChannelActions
          actions={viewModel.sectionActions}
          onAction={onAction}
        />
      </div>
    </section>
  )
}

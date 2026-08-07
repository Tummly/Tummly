import { MailIcon, MessageSquareIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
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
import type { OperatorCampaignsMessagingUsageViewModel } from "@/lib/operatorCampaigns/messagingUsageFixtures"
import {
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type CampaignsMessagingUsageProps = {
  messagingUsage: OperatorCampaignsMessagingUsageViewModel
  /** Inert until billing / usage destinations land. */
  onViewUsage?: () => void
  /** Inert until SMS purchase lands. */
  onBuySmsCredits?: () => void
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

/** Messaging usage block — fixed Figma fixtures (3462:62679); no balance API. */
export function CampaignsMessagingUsage({
  messagingUsage,
  onViewUsage,
  onBuySmsCredits,
}: CampaignsMessagingUsageProps) {
  const { email, sms, plan } = messagingUsage

  return (
    <section
      className={GUESTS_SECTION_CLASS}
      aria-label={messagingUsage.title}
    >
      <header className="flex flex-col gap-2 leading-0">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{messagingUsage.title}</h2>
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{messagingUsage.subtitle}</p>
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
                  {email.usageLine}
                </p>
              </div>
              <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_DETAIL_CLASS}>
                {email.detailLine}
              </p>
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
                  {sms.usageLine}
                </p>
              </div>
              <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_DETAIL_CLASS}>
                {sms.detailLine}
              </p>
            </div>
            <UsageMeter
              fillRatio={sms.fillRatio}
              maxLabel={sms.meterMaxLabel}
            />
          </div>
        </div>

        <div className={CAMPAIGNS_MESSAGING_USAGE_TILE_CLASS}>
          <div className="flex flex-col gap-3">
            <div className="flex flex-col gap-1">
              <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS}>
                {plan.title}
              </p>
              <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS}>
                {plan.planLine}
              </p>
            </div>
            <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_DETAIL_CLASS}>
              {plan.billingLine}
            </p>
          </div>
        </div>
      </div>

      <div className={CAMPAIGNS_MESSAGING_USAGE_ACTIONS_CLASS}>
        <Button
          type="button"
          variant="op-tertiary"
          className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
          onClick={onViewUsage}
        >
          {messagingUsage.viewUsageLabel}
        </Button>
        <Button
          type="button"
          variant="op-link"
          className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
          onClick={onBuySmsCredits}
        >
          {messagingUsage.buySmsCreditsLabel}
        </Button>
      </div>
    </section>
  )
}

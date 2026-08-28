import { BotIcon, MailIcon, MessageSquareIcon } from "lucide-react"

import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"
import { useBillingCreditsPageModuleApi } from "@/components/dashboard/operator/BillingCredits/utils/billingCreditsPageModuleContext"
import { Button } from "@/components/ui/button"
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group"
import { BILLING_CREDITS_PAGE_COPY as copy } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { CreditTopUpCardViewModel } from "@/lib/operatorBillingCredits/creditTopUpPresentation"
import type { CreditChannelId } from "@/lib/operatorBillingCredits/creditsUsagePresentation"
import {
  CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_TILE_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS,
} from "@/lib/operatorCampaigns/campaignsPresentation"
import {
  GUESTS_SECTION_SUBTITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"

function channelIcon(channel: CreditChannelId) {
  switch (channel) {
    case "email":
      return MailIcon
    case "sms":
      return MessageSquareIcon
    case "ai":
      return BotIcon
  }
}

function CreditTopUpCard({
  card,
  highlighted,
  onSelectPack,
  onBuy,
}: {
  card: CreditTopUpCardViewModel
  highlighted: boolean
  onSelectPack: (quantity: number) => void
  onBuy: () => void
}) {
  const Icon = channelIcon(card.channel)

  return (
    <article
      id={`credit-top-up-${card.channel}`}
      className={cn(
        CAMPAIGNS_MESSAGING_USAGE_TILE_CLASS,
        "gap-5",
        highlighted && "ring-2 ring-primary/20"
      )}
    >
      <div className="flex flex-col gap-3">
        <Icon className="size-5 text-op-card-title-color" aria-hidden />
        <div className="flex flex-col gap-1">
          <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS}>
            {card.title}
          </p>
          <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS}>
            {card.remainingHeadline}
          </p>
        </div>
      </div>

      <ToggleGroup
        type="single"
        variant="outline"
        className="flex flex-wrap justify-start gap-2"
        value={
          card.packs.find((pack) => pack.selected)?.quantity != null
            ? String(card.packs.find((pack) => pack.selected)?.quantity)
            : undefined
        }
        onValueChange={(value) => {
          if (value === "") {
            return
          }
          const quantity = Number(value)
          if (!Number.isFinite(quantity)) {
            return
          }
          onSelectPack(quantity)
        }}
      >
        {card.packs.map((pack) => (
          <ToggleGroupItem
            key={pack.quantity}
            value={String(pack.quantity)}
            disabled={card.chipsDisabled}
            className="h-auto min-h-8 px-3 py-2 text-sm font-medium"
            aria-label={pack.label}
          >
            {pack.label}
          </ToggleGroupItem>
        ))}
      </ToggleGroup>

      {card.selectedNetLabel != null ? (
        <p className="m-0 text-sm font-semibold text-op-text-primary">
          {card.selectedNetLabel}
        </p>
      ) : null}

      {card.showPilotNotice ? (
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{copy.topUpPilotNotice}</p>
      ) : null}

      <Button
        type="button"
        variant="op-secondary"
        className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
        disabled={card.buyDisabled}
        onClick={onBuy}
      >
        {card.buyLabel}
      </Button>
    </article>
  )
}

export function CreditTopUpsSection({
  snap,
  pageModule,
}: {
  snap: ReturnType<
    ReturnType<typeof useBillingCreditsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useBillingCreditsPageModuleApi>
}) {
  const showPilotNotice =
    snap.creditsUsage?.isPilot === true
    && snap.topUpCards.some((card) => card.showPilotNotice)

  return (
    <div className="flex flex-col gap-4">
      {showPilotNotice ? (
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{copy.topUpPilotNotice}</p>
      ) : null}

      <div className="grid grid-cols-1 gap-3 xl:grid-cols-3">
        {snap.topUpCards.map((card) => (
          <CreditTopUpCard
            key={card.channel}
            card={card}
            highlighted={snap.focusedTopUpChannel === card.channel}
            onSelectPack={(quantity) => {
              pageModule.selectTopUpPack(card.channel, quantity)
            }}
            onBuy={() => {
              void pageModule.requestTopUpBuy(card.channel)
            }}
          />
        ))}
      </div>

      <AccountWorkspaceConfirmDialog
        open={snap.topUpConfirm?.open ?? false}
        title={snap.topUpConfirm?.title ?? ""}
        body={snap.topUpConfirm?.body ?? ""}
        primaryLabel={snap.topUpConfirm?.primaryLabel ?? copy.continue}
        busy={snap.topUpConfirm?.busy ?? false}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.cancelTopUpBuy()
          }
        }}
        onPrimary={() => {
          void pageModule.confirmTopUpBuy()
        }}
        onCancel={() => {
          pageModule.cancelTopUpBuy()
        }}
      />
    </div>
  )
}

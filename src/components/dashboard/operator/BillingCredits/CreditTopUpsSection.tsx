import { CoinsIcon, MailIcon, MessageSquareIcon, PackageIcon } from "lucide-react"

import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"
import { useBillingCreditsPageModuleApi } from "@/components/dashboard/operator/BillingCredits/utils/billingCreditsPageModuleContext"
import { Button } from "@/components/ui/button"
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group"
import {
  BILLING_CREDITS_PAGE_COPY as copy,
  BILLING_CREDITS_CTA_BUTTON_CLASS,
  BILLING_CREDIT_TOP_UP_BALANCE_CAPTION_CLASS,
  BILLING_CREDIT_TOP_UP_BALANCE_VALUE_CLASS,
  BILLING_CREDIT_TOP_UP_CARD_CLASS,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { CreditTopUpCardViewModel } from "@/lib/operatorBillingCredits/creditTopUpPresentation"
import type { CreditChannelId } from "@/lib/operatorBillingCredits/creditsUsagePresentation"
import {
  CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS,
} from "@/lib/operatorCampaigns/campaignsPresentation"
import {
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
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
      return CoinsIcon
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
  const selectedQuantity = card.packs.find((pack) => pack.selected)?.quantity

  return (
    <article
      id={`credit-top-up-${card.channel}`}
      className={cn(
        BILLING_CREDIT_TOP_UP_CARD_CLASS,
        highlighted && "ring-2 ring-primary/20"
      )}
    >
      <div className="flex w-full items-start gap-8">
        <div className="flex min-w-0 flex-1 flex-col gap-3">
          <Icon className="size-5 text-op-card-title-color" aria-hidden />
          <div className="flex flex-col gap-2">
            <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS}>
              {card.title}
            </p>
            <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS}>
              {card.detailLine}
            </p>
          </div>
        </div>
        <div className="flex shrink-0 flex-col items-start gap-1">
          <p className={BILLING_CREDIT_TOP_UP_BALANCE_VALUE_CLASS}>
            {card.remainingHeadline}
          </p>
          <p className={BILLING_CREDIT_TOP_UP_BALANCE_CAPTION_CLASS}>
            {copy.creditTopUpsCurrentBalance}
          </p>
        </div>
      </div>

      <div className="flex w-full flex-col items-start gap-5">
        <ToggleGroup
          type="single"
          variant="outline"
          className="flex flex-wrap justify-start gap-3"
          value={selectedQuantity != null ? String(selectedQuantity) : undefined}
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
              className="h-[37px] min-h-[37px] rounded-[2px] px-[13px] py-[11px] text-xs font-normal"
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

        <Button
          type="button"
          variant="op-secondary"
          className={cn(
            GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
            BILLING_CREDITS_CTA_BUTTON_CLASS
          )}
          disabled={card.buyDisabled}
          onClick={onBuy}
        >
          {card.buyLabel}
        </Button>
      </div>
    </article>
  )
}

function QrPrintPacksTopUpCard() {
  return (
    <article
      id="credit-top-up-qr"
      className={BILLING_CREDIT_TOP_UP_CARD_CLASS}
    >
      <div className="flex w-full flex-col gap-3">
        <PackageIcon className="size-5 text-op-card-title-color" aria-hidden />
        <div className="flex flex-col gap-2">
          <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS}>
            {copy.qrPrintPacksTitle}
          </p>
          <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS}>
            {copy.qrPrintPacksDetail}
          </p>
        </div>
      </div>
      <Button
        type="button"
        variant="op-secondary"
        className={cn(
          GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
          BILLING_CREDITS_CTA_BUTTON_CLASS
        )}
        disabled
      >
        {copy.qrPrintPacksShop}
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
    <div className="flex flex-col gap-5">
      {showPilotNotice ? (
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{copy.topUpPilotNotice}</p>
      ) : null}

      <div className="grid grid-cols-1 gap-5 xl:grid-cols-2">
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
        <QrPrintPacksTopUpCard />
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

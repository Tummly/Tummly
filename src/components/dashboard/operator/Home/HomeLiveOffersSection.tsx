import { GuestPreviewEmailChrome } from "@/components/dashboard/operator/Feedback/GuestPreviewOverlay"
import { GuestPreviewOfferCoupon } from "@/components/dashboard/operator/Feedback/GuestPreviewOfferCoupon"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import type { OperatorHomeLiveCard } from "@/lib/operatorHome/buildLiveOffersSectionCards"
import {
  LIVE_OFFERS_CARD_ACTIONS_CLASS,
  LIVE_OFFERS_CARD_CLASS,
  LIVE_OFFERS_CARD_META_CLASS,
  LIVE_OFFERS_CARD_META_TOP_CLASS,
  LIVE_OFFERS_CARD_METRICS_CLASS,
  LIVE_OFFERS_CARD_PREVIEW_CLASS,
  LIVE_OFFERS_CARD_PREVIEW_OVERLAY_CLASS,
  LIVE_OFFERS_CARD_PREVIEW_SCALE_CLASS,
  LIVE_OFFERS_CARD_TITLE_CLASS,
  LIVE_OFFERS_CARDS_STACK_CLASS,
  LIVE_OFFERS_EMPTY_ACTION_BUTTON_CLASS,
  LIVE_OFFERS_EMPTY_ACTIONS,
  LIVE_OFFERS_EMPTY_ACTIONS_CLASS,
  LIVE_OFFERS_EMPTY_COPY,
  LIVE_OFFERS_EMPTY_COPY_CLASS,
  LIVE_OFFERS_EMPTY_HELPER,
  LIVE_OFFERS_EMPTY_HELPER_CLASS,
  LIVE_OFFERS_EMPTY_SHELL_CLASS,
  LIVE_OFFERS_EMPTY_TITLE_CLASS,
  LIVE_OFFERS_HEADER_CLASS,
  LIVE_OFFERS_LOAD_ERROR,
  LIVE_OFFERS_SECTION_CLASS,
  LIVE_OFFERS_SUBTITLE_CLASS,
  LIVE_OFFERS_TITLE_CLASS,
  resolveLiveOffersEmptyActionVariant,
  type LiveOffersEmptyActionId,
} from "@/lib/operatorHome/liveOffersSectionPresentation"
import {
  GUEST_PREVIEW_OFFER_COPY_LABEL,
  GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
} from "@/lib/operatorFeedback/guestPreviewPresentation"
import { OPERATOR_HOME_CARD_CLASS } from "@/lib/operatorHome/operatorHomeSectionPresentation"

export type HomeLiveOffersSectionProps = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  cards: readonly OperatorHomeLiveCard[]
  errorMessage?: string | null
  pauseBusy?: boolean
  brandName?: string | null
  locationName?: string | null
  locationAddress?: string | null
  onEmptyAction?: (actionId: LiveOffersEmptyActionId) => void
  onRetry?: () => void
  onPreview?: (card: OperatorHomeLiveCard) => void
  onViewCampaign?: (campaignId: number) => void
  onViewOffer?: (offerId: number) => void
  onViewRedemptions?: (offerId: number) => void
  onPauseCampaign?: (campaignId: number) => void
}

function liveOfferCouponView(
  coupon: {
    title: string
    description: string
    expiryLabel: string
  }
) {
  return {
    title: coupon.title,
    description: coupon.description,
    redemptionCode: GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
    expiryLabel: coupon.expiryLabel,
    copyLabel: GUEST_PREVIEW_OFFER_COPY_LABEL,
    copyEnabled: false,
  }
}

function LiveCardPreview({
  card,
  brandName,
  locationName,
  locationAddress,
  onPreview,
}: {
  card: OperatorHomeLiveCard
  brandName: string | null
  locationName: string | null
  locationAddress: string | null
  onPreview?: (card: OperatorHomeLiveCard) => void
}) {
  const offerCoupon =
    card.kind === "offer"
      ? liveOfferCouponView({
          title: card.title,
          description: card.description?.trim() ?? "",
          expiryLabel: card.expiryLabel,
        })
      : card.offerCoupon != null
        ? liveOfferCouponView(card.offerCoupon)
        : null

  return (
    <div className={LIVE_OFFERS_CARD_PREVIEW_CLASS}>
      <div className={LIVE_OFFERS_CARD_PREVIEW_SCALE_CLASS} aria-hidden>
        {card.kind === "campaign" ? (
          <GuestPreviewEmailChrome
            brandName={brandName}
            locationName={locationName}
            locationAddress={locationAddress}
            subject={card.messageSubject?.trim() || card.title}
            message={
              card.messageBody?.trim()
              || "Campaign message preview is not available."
            }
            offerCoupon={
              offerCoupon != null ? (
                <GuestPreviewOfferCoupon coupon={offerCoupon} />
              ) : undefined
            }
            className="w-full"
            maxWidthClass="max-w-none"
          />
        ) : offerCoupon != null ? (
          <div className="w-full">
            <GuestPreviewOfferCoupon coupon={offerCoupon} />
          </div>
        ) : null}
      </div>
      <div className={LIVE_OFFERS_CARD_PREVIEW_OVERLAY_CLASS}>
        <Button
          type="button"
          variant="op-secondary"
          size="default"
          onClick={() => {
            onPreview?.(card)
          }}
        >
          Preview
        </Button>
      </div>
    </div>
  )
}

function LiveCardMeta({
  card,
  pauseBusy,
  onViewCampaign,
  onViewOffer,
  onViewRedemptions,
  onPauseCampaign,
}: {
  card: OperatorHomeLiveCard
  pauseBusy: boolean
  onViewCampaign?: (campaignId: number) => void
  onViewOffer?: (offerId: number) => void
  onViewRedemptions?: (offerId: number) => void
  onPauseCampaign?: (campaignId: number) => void
}) {
  return (
    <div className={LIVE_OFFERS_CARD_META_CLASS}>
      <div className={LIVE_OFFERS_CARD_META_TOP_CLASS}>
        <Badge variant="soft">{card.statusLabel}</Badge>
        <div className="flex flex-col gap-2">
          <h3 className={LIVE_OFFERS_CARD_TITLE_CLASS}>{card.title}</h3>
          <p className={LIVE_OFFERS_CARD_METRICS_CLASS}>
            {card.metricParts.map((part, index) => (
              <span key={`${card.kind}-${card.id}-${part}`}>
                {index > 0 ? <span aria-hidden> · </span> : null}
                {part}
              </span>
            ))}
          </p>
        </div>
      </div>
      <div className={LIVE_OFFERS_CARD_ACTIONS_CLASS}>
        {card.kind === "campaign" ? (
          <>
            <Button
              type="button"
              variant="op-secondary"
              onClick={() => {
                onViewCampaign?.(card.id)
              }}
            >
              View campaign
            </Button>
            <Button
              type="button"
              variant="op-tertiary"
              disabled={pauseBusy}
              onClick={() => {
                onPauseCampaign?.(card.id)
              }}
            >
              Pause
            </Button>
          </>
        ) : (
          <>
            <Button
              type="button"
              variant="op-secondary"
              onClick={() => {
                onViewOffer?.(card.id)
              }}
            >
              View offer
            </Button>
            <Button
              type="button"
              variant="op-tertiary"
              onClick={() => {
                onViewRedemptions?.(card.id)
              }}
            >
              View redemptions
            </Button>
          </>
        )}
      </div>
    </div>
  )
}

/** Figma Live offers and campaigns — empty, loading, error, and split live cards. */
export function HomeLiveOffersSection({
  loadStatus,
  cards,
  errorMessage = null,
  pauseBusy = false,
  brandName = null,
  locationName = null,
  locationAddress = null,
  onEmptyAction,
  onRetry,
  onPreview,
  onViewCampaign,
  onViewOffer,
  onViewRedemptions,
  onPauseCampaign,
}: HomeLiveOffersSectionProps) {
  const showEmpty = loadStatus === "loaded" && cards.length === 0
  const showCards = loadStatus === "loaded" && cards.length > 0

  return (
    <section className={LIVE_OFFERS_SECTION_CLASS}>
      <div className={LIVE_OFFERS_HEADER_CLASS}>
        <h2 className={LIVE_OFFERS_TITLE_CLASS}>Live offers and campaigns</h2>
        <p className={LIVE_OFFERS_SUBTITLE_CLASS}>
          See what is currently running and how it is performing.
        </p>
      </div>

      {loadStatus === "idle" || loadStatus === "loading" ? (
        <div
          className={`${OPERATOR_HOME_CARD_CLASS} border-0 bg-transparent px-6 py-10 text-center`}
          role="status"
          aria-live="polite"
          aria-label="Loading live offers and campaigns"
        >
          <div
            className="mx-auto size-6 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
            aria-hidden
          />
        </div>
      ) : null}

      {loadStatus === "error" ? (
        <div className="px-6 py-10 text-center">
          <p className="text-sm text-destructive">
            {errorMessage?.trim() || LIVE_OFFERS_LOAD_ERROR}
          </p>
          {onRetry ? (
            <Button
              type="button"
              variant="link"
              size="link-sm"
              className="mt-3 font-medium underline"
              onClick={onRetry}
            >
              Retry
            </Button>
          ) : null}
        </div>
      ) : null}

      {showEmpty ? (
        <div className={LIVE_OFFERS_EMPTY_SHELL_CLASS}>
          <div className={LIVE_OFFERS_EMPTY_COPY_CLASS}>
            <p className={LIVE_OFFERS_EMPTY_TITLE_CLASS}>{LIVE_OFFERS_EMPTY_COPY}</p>
            <p className={LIVE_OFFERS_EMPTY_HELPER_CLASS}>{LIVE_OFFERS_EMPTY_HELPER}</p>
          </div>
          <div className={LIVE_OFFERS_EMPTY_ACTIONS_CLASS}>
            {LIVE_OFFERS_EMPTY_ACTIONS.map((action) => (
              <Button
                key={action.id}
                type="button"
                variant={resolveLiveOffersEmptyActionVariant(action.id)}
                size="default"
                className={LIVE_OFFERS_EMPTY_ACTION_BUTTON_CLASS}
                onClick={() => {
                  onEmptyAction?.(action.id)
                }}
              >
                {action.label}
              </Button>
            ))}
          </div>
        </div>
      ) : null}

      {showCards ? (
        <div className={LIVE_OFFERS_CARDS_STACK_CLASS}>
          {cards.map((card) => (
            <article
              key={`${card.kind}-${card.id}`}
              className={LIVE_OFFERS_CARD_CLASS}
            >
              <LiveCardPreview
                card={card}
                brandName={brandName}
                locationName={locationName}
                locationAddress={locationAddress}
                onPreview={onPreview}
              />
              <LiveCardMeta
                card={card}
                pauseBusy={pauseBusy}
                onViewCampaign={onViewCampaign}
                onViewOffer={onViewOffer}
                onViewRedemptions={onViewRedemptions}
                onPauseCampaign={onPauseCampaign}
              />
            </article>
          ))}
        </div>
      ) : null}
    </section>
  )
}

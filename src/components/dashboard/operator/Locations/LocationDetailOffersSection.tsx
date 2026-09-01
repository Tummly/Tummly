import { Link } from "react-router-dom"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  LOCATION_DETAIL_ACTION_BUTTON_CLASS,
  LOCATION_DETAIL_CARD_CLASS,
  LOCATION_DETAIL_PAGE_COPY,
  LOCATION_DETAIL_SECTION_SUBTITLE_CLASS,
  LOCATION_DETAIL_SECTION_TITLE_CLASS,
  type LocationDetailOfferCard,
} from "@/lib/operatorLocations/locationDetailPresentation"
import { GUESTS_MARKETING_STATUS_BADGE_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"

type LocationDetailOffersSectionProps = {
  offerCards: LocationDetailOfferCard[]
  createOfferPath: string
  createCampaignPath: string
  showCreateButtonsWhenEmpty?: boolean
}

export function LocationDetailOffersSection({
  offerCards,
  createOfferPath,
  createCampaignPath,
  showCreateButtonsWhenEmpty = true,
}: LocationDetailOffersSectionProps) {
  const copy = LOCATION_DETAIL_PAGE_COPY

  return (
    <section
      className={cn(LOCATION_DETAIL_CARD_CLASS, "gap-[22px]")}
      aria-label={copy.offersTitle}
    >
      <div className="flex flex-col gap-2">
        <h2 className={LOCATION_DETAIL_SECTION_TITLE_CLASS}>
          {copy.offersTitle}
        </h2>
        <p className={LOCATION_DETAIL_SECTION_SUBTITLE_CLASS}>
          {copy.offersSubtitle}
        </p>
      </div>

      {offerCards.length === 0 ? (
        <div className="flex min-h-[160px] flex-col items-start justify-center gap-4">
          <div className="flex flex-col gap-2">
            <p className="m-0 text-base font-medium text-op-text-primary">
              {copy.offersEmptyTitle}
            </p>
            <p className="m-0 max-w-[480px] text-sm font-medium text-op-text-muted">
              {copy.offersEmptyHelper}
            </p>
          </div>
          {showCreateButtonsWhenEmpty ? (
            <div className="flex flex-wrap gap-3">
              <Button
                type="button"
                variant="op-secondary"
                className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
                asChild
              >
                <Link to={createOfferPath}>{copy.createOffer}</Link>
              </Button>
              <Button
                type="button"
                variant="op-secondary"
                className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
                asChild
              >
                <Link to={createCampaignPath}>{copy.createCampaign}</Link>
              </Button>
            </div>
          ) : null}
        </div>
      ) : (
        <>
          <div className="grid grid-cols-1 gap-[30px] lg:grid-cols-2">
            {offerCards.map((card) => (
              <article
                key={card.id}
                className="flex flex-col gap-4 rounded-op-md border border-op-card-border bg-op-background-secondary p-[18px]"
              >
                <Badge
                  variant="soft"
                  className={cn(GUESTS_MARKETING_STATUS_BADGE_CLASS, "w-fit")}
                >
                  {card.statusLabel}
                </Badge>
                <h3 className="m-0 text-base font-semibold text-op-text-primary">
                  {card.title}
                </h3>
                <p className="m-0 text-sm font-medium text-op-text-muted">
                  {card.meta}
                </p>
                <div className="flex flex-wrap gap-3">
                  <Button
                    type="button"
                    variant="op-secondary"
                    className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
                    asChild
                  >
                    <Link to={card.hrefPrimary}>{card.primaryCta}</Link>
                  </Button>
                  <Button
                    type="button"
                    variant="op-tertiary"
                    className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
                    asChild
                  >
                    <Link to={card.hrefSecondary}>{card.secondaryCta}</Link>
                  </Button>
                </div>
              </article>
            ))}
          </div>
          <div className="flex flex-wrap gap-3">
            <Button
              type="button"
              variant="op-secondary"
              className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
              asChild
            >
              <Link to={createOfferPath}>{copy.createOffer}</Link>
            </Button>
            <Button
              type="button"
              variant="op-secondary"
              className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
              asChild
            >
              <Link to={createCampaignPath}>{copy.createCampaign}</Link>
            </Button>
          </div>
        </>
      )}
    </section>
  )
}

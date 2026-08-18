import type { LucideIcon } from "lucide-react"
import {
  BanknoteIcon,
  RefreshCwIcon,
  TagIcon,
  TicketPercentIcon,
} from "lucide-react"

import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  EXISTING_OFFER_PICKER_CARD_ACTIONS_CLASS,
  EXISTING_OFFER_PICKER_CARD_CLASS,
  EXISTING_OFFER_PICKER_CARD_META_CLASS,
  EXISTING_OFFER_PICKER_CARD_TITLE_CLASS,
  EXISTING_OFFER_PICKER_ICON_WELL_CLASS,
  EXISTING_OFFER_PICKER_PANEL_CLASS,
  type CampaignExistingOfferPickerCard,
} from "@/lib/operatorCampaigns/campaignExistingOfferPickerPresentation"
import { CAMPAIGN_TEMPLATE_PICKER_SEARCH_FIELD_CLASS } from "@/lib/operatorCampaigns/campaignTemplatePickerPresentation"
import {
  operatorDashboardOfferDetailsPath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"

const PICKER_TYPE_ICONS: Record<
  CampaignExistingOfferPickerCard["offerTypeIconId"],
  LucideIcon
> = {
  percentage_discount: TicketPercentIcon,
  fixed_discount: BanknoteIcon,
  free_item: TagIcon,
  replacement_item: RefreshCwIcon,
  unknown: TagIcon,
}

export type ExistingOfferPickerModel = {
  searchQuery: string
  searchPlaceholder: string
  loadStatus: "idle" | "loading" | "ready" | "error"
  error: string | null
  retryLabel: string
  cards: CampaignExistingOfferPickerCard[]
  isEmpty: boolean
  emptyHelper: string | null
  createNewOfferLabel: string | null
  selectLabel: string
  viewDetailsLabel: string
  viewDetailsEnabled: boolean
}

type ExistingOfferPickerProps = {
  picker: ExistingOfferPickerModel
  dashboardMode: OperatorDashboardMode
  locationId: number | null
  testId: string
  disabled?: boolean
  onSearchChange: (query: string) => void
  onSelect: (offerId: number) => void
  onRetry: () => void
  onCreateNew?: () => void
}

function ExistingOfferPickerCardRow({
  card,
  selectLabel,
  viewDetailsLabel,
  viewDetailsHref,
  onSelect,
  disabled,
}: {
  card: CampaignExistingOfferPickerCard
  selectLabel: string
  viewDetailsLabel: string
  viewDetailsHref: string | null
  onSelect: () => void
  disabled?: boolean
}) {
  const Icon = PICKER_TYPE_ICONS[card.offerTypeIconId]

  return (
    <article className={EXISTING_OFFER_PICKER_CARD_CLASS}>
      <div className="flex items-center gap-4">
        <span className={EXISTING_OFFER_PICKER_ICON_WELL_CLASS}>
          <Icon className="size-5 text-op-text-primary" aria-hidden />
        </span>
        <div className="flex min-w-0 flex-col gap-2">
          <p className={EXISTING_OFFER_PICKER_CARD_TITLE_CLASS}>
            {card.title}
          </p>
          <p className={EXISTING_OFFER_PICKER_CARD_META_CLASS}>
            <span>{card.validUntilLabel}</span>
            <span aria-hidden>·</span>
            <span>{card.useRuleLabel}</span>
          </p>
        </div>
      </div>
      <div className={EXISTING_OFFER_PICKER_CARD_ACTIONS_CLASS}>
        <Button
          type="button"
          variant="op-secondary"
          disabled={disabled}
          onClick={onSelect}
        >
          {selectLabel}
        </Button>
        {viewDetailsHref != null ? (
          <Button asChild variant="op-tertiary">
            <a
              href={viewDetailsHref}
              target="_blank"
              rel="noopener noreferrer"
            >
              {viewDetailsLabel}
            </a>
          </Button>
        ) : null}
      </div>
    </article>
  )
}

/**
 * Shared Existing-offer list — Campaign Offer step and Recovery Offer step.
 */
export function ExistingOfferPicker({
  picker,
  dashboardMode,
  locationId,
  testId,
  disabled,
  onSearchChange,
  onSelect,
  onRetry,
  onCreateNew,
}: ExistingOfferPickerProps) {
  return (
    <div className={EXISTING_OFFER_PICKER_PANEL_CLASS} data-testid={testId}>
      <div className="relative w-full">
        <OperatorSearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-op-header-search-text" />
        <Input
          value={picker.searchQuery}
          onChange={(event) => {
            onSearchChange(event.target.value)
          }}
          placeholder={picker.searchPlaceholder}
          className={CAMPAIGN_TEMPLATE_PICKER_SEARCH_FIELD_CLASS}
          aria-label={picker.searchPlaceholder}
          disabled={disabled}
        />
      </div>

      {picker.loadStatus === "loading" ? (
        <p className="m-0 text-sm font-medium text-[var(--op-color-gray-550)]">
          Loading offers…
        </p>
      ) : null}

      {picker.loadStatus === "error" ? (
        <div className="flex flex-col gap-3">
          <p className="m-0 text-sm font-medium leading-5 text-op-text-muted">
            {picker.error}
          </p>
          <Button
            type="button"
            variant="op-tertiary"
            className="w-fit"
            disabled={disabled}
            onClick={onRetry}
          >
            {picker.retryLabel}
          </Button>
        </div>
      ) : null}

      {picker.loadStatus === "ready" && picker.isEmpty ? (
        <div className="flex flex-col gap-3">
          <p className="m-0 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
            {picker.emptyHelper}
          </p>
          {picker.createNewOfferLabel != null && onCreateNew != null ? (
            <Button
              type="button"
              variant="op-secondary"
              className="w-fit"
              disabled={disabled}
              onClick={onCreateNew}
            >
              {picker.createNewOfferLabel}
            </Button>
          ) : null}
        </div>
      ) : null}

      {picker.loadStatus === "ready" && !picker.isEmpty
        ? picker.cards.map((card) => (
            <ExistingOfferPickerCardRow
              key={card.id}
              card={card}
              selectLabel={picker.selectLabel}
              viewDetailsLabel={picker.viewDetailsLabel}
              viewDetailsHref={
                picker.viewDetailsEnabled && locationId != null
                  ? operatorDashboardOfferDetailsPath(
                      dashboardMode,
                      card.id,
                      locationId
                    )
                  : null
              }
              disabled={disabled}
              onSelect={() => {
                onSelect(card.id)
              }}
            />
          ))
        : null}
    </div>
  )
}

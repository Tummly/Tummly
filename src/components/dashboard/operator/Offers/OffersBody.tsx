import { useNavigate } from "react-router-dom"

import { OffersListSection } from "@/components/dashboard/operator/Offers/OffersListSection"
import { OffersNeedsAttentionSection } from "@/components/dashboard/operator/Offers/OffersNeedsAttentionSection"
import { OffersPerformanceSection } from "@/components/dashboard/operator/Offers/OffersPerformanceSection"
import { CreateEditOfferDrawer } from "@/components/dashboard/operator/Offers/CreateEditOfferDrawer"
import { Button } from "@/components/ui/button"
import type { OfferRowActionId } from "@/lib/operatorOffers/offerListPresentation"
import type {
  OperatorOffersCreateOfferDrawerViewModel,
  OperatorOffersPageViewModel,
} from "@/lib/operatorOffers/createOperatorOffersPageModule"
import {
  OFFERS_PAGE_COPY,
  OFFERS_PAGE_META_CLASS,
} from "@/lib/operatorOffers/offersPresentation"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import type { FilterChip } from "@/lib/operatorFilterSheet"
import type { CampaignCatalogOfferDetailsDraft } from "@/lib/operatorOffers/offerCatalogPresentation"
import {
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type {
  OperatorOffersListViewId,
  OperatorOffersSortId,
} from "@/types/operatorCampaigns"

type OffersBodyProps = {
  viewModel: OperatorOffersPageViewModel
  createOfferDrawer: OperatorOffersCreateOfferDrawerViewModel | null
  redemptionLogHref: string
  onOpenCreateOffer: () => void
  onCloseCreateOffer: () => void
  onPatchCreateOfferDraft: (
    patch: Partial<CampaignCatalogOfferDetailsDraft>
  ) => void
  onConfirmCreateOffer: () => void
  onOpenStaffRedeem: () => void
  onCommitPerformanceDateRange: (range: HomePerformanceDateRange) => void
  onListViewChange: (viewId: OperatorOffersListViewId) => void
  onSearchQueryChange: (query: string) => void
  onSortChange: (id: OperatorOffersSortId) => void
  onPreviousPage: () => void
  onNextPage: () => void
  onOpenFilters: () => void
  onRemoveFilterChip: (chip: FilterChip) => void
  onViewAllOffers: () => void
  onClearAllFilters: () => void
  onRowAction: (offerId: number, actionId: OfferRowActionId) => void
}

/** Offers page — header, Performance, Needs attention, list chrome, and Create/Edit drawer. */
export function OffersBody({
  viewModel,
  createOfferDrawer,
  redemptionLogHref,
  onOpenCreateOffer,
  onCloseCreateOffer,
  onPatchCreateOfferDraft,
  onConfirmCreateOffer,
  onOpenStaffRedeem,
  onCommitPerformanceDateRange,
  onListViewChange,
  onSearchQueryChange,
  onSortChange,
  onPreviousPage,
  onNextPage,
  onOpenFilters,
  onRemoveFilterChip,
  onViewAllOffers,
  onClearAllFilters,
  onRowAction,
}: OffersBodyProps) {
  const copy = OFFERS_PAGE_COPY
  const navigate = useNavigate()

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <header className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{copy.title}</h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>{copy.subtitle}</p>
          <p className={OFFERS_PAGE_META_CLASS}>{viewModel.locationName}</p>
        </header>

        <div className="flex shrink-0 flex-wrap items-center gap-3">
          <Button
            type="button"
            variant="op-primary"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
            onClick={onOpenCreateOffer}
          >
            {viewModel.header.createOfferLabel}
          </Button>
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={onOpenStaffRedeem}
          >
            {viewModel.header.openStaffRedeemLabel}
          </Button>
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={() => {
              void navigate(redemptionLogHref)
            }}
          >
            {viewModel.header.viewRedemptionLogLabel}
          </Button>
        </div>
      </div>

      <OffersPerformanceSection
        performance={viewModel.performance}
        onCommitRange={onCommitPerformanceDateRange}
      />

      <OffersNeedsAttentionSection
        needsAttention={viewModel.needsAttention}
      />

      <OffersListSection
        list={viewModel.list}
        onViewChange={onListViewChange}
        onSearchQueryChange={onSearchQueryChange}
        onSortChange={onSortChange}
        onPreviousPage={onPreviousPage}
        onNextPage={onNextPage}
        onOpenFilters={onOpenFilters}
        onRemoveFilterChip={onRemoveFilterChip}
        onRowAction={onRowAction}
        onViewAllOffers={onViewAllOffers}
        onClearAllFilters={onClearAllFilters}
      />

      {createOfferDrawer != null ? (
        <CreateEditOfferDrawer
          open={createOfferDrawer.open}
          mode={createOfferDrawer.mode}
          locationSubtitle={createOfferDrawer.locationSubtitle}
          draft={createOfferDrawer.draft}
          canConfirm={createOfferDrawer.canConfirm}
          saveGated={createOfferDrawer.saveGated}
          status={createOfferDrawer.status}
          error={createOfferDrawer.error}
          onOpenChange={(nextOpen) => {
            if (!nextOpen) {
              onCloseCreateOffer()
            }
          }}
          onPatch={onPatchCreateOfferDraft}
          onConfirm={onConfirmCreateOffer}
        />
      ) : null}
    </div>
  )
}

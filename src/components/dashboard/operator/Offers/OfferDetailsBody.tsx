import { Link } from "react-router-dom"
import { ChevronRightIcon } from "lucide-react"

import { GuestProfileDetailRows } from "@/components/dashboard/operator/GuestProfile/GuestProfileDetailRows"
import { OfferDetailsCampaignsPanel } from "@/components/dashboard/operator/Offers/OfferDetailsCampaignsPanel"
import { OfferDetailsClaimsPanel } from "@/components/dashboard/operator/Offers/OfferDetailsClaimsPanel"
import { OfferDetailsHeaderActionsMenu } from "@/components/dashboard/operator/Offers/OfferDetailsHeaderActionsMenu"
import { OfferDetailsOverviewPanel } from "@/components/dashboard/operator/Offers/OfferDetailsOverviewPanel"
import { OfferDetailsRedemptionsPanel } from "@/components/dashboard/operator/Offers/OfferDetailsRedemptionsPanel"
import { OfferDetailsVoidRequestsPanel } from "@/components/dashboard/operator/Offers/OfferDetailsVoidRequestsPanel"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import type { OfferDetailsViewModel } from "@/lib/operatorOffers/createOfferDetailsPageModule"
import type {
  OfferDetailsCampaignsSubTabId,
  OfferDetailsClaimsRowActionId,
  OfferDetailsDateRange,
  OfferDetailsHeaderActionId,
  OfferDetailsRedemptionsRowActionId,
  OfferDetailsTabId,
  OfferDetailsVoidRequestsRowActionId,
} from "@/lib/operatorOffers/offerDetailsPresentation"
import {
  GUESTS_MARKETING_STATUS_BADGE_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_TAB_BUTTON_ACTIVE_CLASS,
  GUESTS_TAB_BUTTON_CLASS,
  GUESTS_TAB_BUTTON_INACTIVE_CLASS,
  GUESTS_TABLIST_CLASS,
  GUESTS_TABLIST_SCROLL_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  GUEST_PROFILE_HEADER_IDENTITY_CLASS,
  GUEST_PROFILE_HEADER_IDENTITY_COPY_CLASS,
  GUEST_PROFILE_HEADER_SUBTITLE_CLASS,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import { cn } from "@/lib/utils"

type OfferDetailsBodyProps = {
  viewModel: OfferDetailsViewModel
  offersHref: string
  onEditOffer: () => void
  onOpenStaffRedeem: () => void
  onHeaderAction: (actionId: OfferDetailsHeaderActionId) => void
  onTabChange: (tabId: OfferDetailsTabId) => void
  onCommitDateRange: (range: OfferDetailsDateRange) => void
  onShareOfferInCampaign?: () => void
  onCampaignsSubTabChange: (subTabId: OfferDetailsCampaignsSubTabId) => void
  onClaimsRowAction: (
    rowId: string,
    actionId: OfferDetailsClaimsRowActionId
  ) => void
  onRedemptionsRowAction: (
    rowId: string,
    actionId: OfferDetailsRedemptionsRowActionId
  ) => void
  onVoidRequestsRowAction: (
    rowId: string,
    actionId: OfferDetailsVoidRequestsRowActionId
  ) => void
}

export function OfferDetailsBody({
  viewModel,
  offersHref,
  onEditOffer,
  onOpenStaffRedeem,
  onHeaderAction,
  onTabChange,
  onCommitDateRange,
  onShareOfferInCampaign,
  onCampaignsSubTabChange,
  onClaimsRowAction,
  onRedemptionsRowAction,
  onVoidRequestsRowAction,
}: OfferDetailsBodyProps) {
  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <nav
        aria-label="Breadcrumb"
        className="flex flex-wrap items-center gap-2.5 text-base font-medium"
      >
        <Link
          to={offersHref}
          className="text-foreground no-underline hover:underline"
        >
          {viewModel.breadcrumbOffersLabel}
        </Link>
        <ChevronRightIcon
          className="size-4 shrink-0 text-op-text-secondary"
          aria-hidden
        />
        <span className="text-op-text-secondary">{viewModel.title}</span>
      </nav>

      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <header className={GUEST_PROFILE_HEADER_IDENTITY_CLASS}>
          <div className={GUEST_PROFILE_HEADER_IDENTITY_COPY_CLASS}>
            <h1 className={GUESTS_PAGE_TITLE_CLASS}>{viewModel.title}</h1>
            {viewModel.subtitle !== "" ? (
              <p className={GUEST_PROFILE_HEADER_SUBTITLE_CLASS}>
                {viewModel.subtitle}
              </p>
            ) : null}
          </div>
          <Badge
            variant="soft"
            className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
          >
            {viewModel.statusLabel}
          </Badge>
        </header>

        <div className="flex shrink-0 flex-wrap items-center gap-3">
          <Button
            type="button"
            variant="op-primary"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
            onClick={onEditOffer}
          >
            {viewModel.editOfferLabel}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={onOpenStaffRedeem}
          >
            {viewModel.openStaffRedeemLabel}
          </Button>
          <OfferDetailsHeaderActionsMenu
            ariaLabel={viewModel.moreActionsAriaLabel}
            items={viewModel.headerMenuItems}
            onAction={onHeaderAction}
          />
        </div>
      </div>

      <section className={GUESTS_SECTION_CLASS}>
        <GuestProfileDetailRows layout="stack" rows={viewModel.metaRows} />
      </section>

      <div className={GUESTS_TABLIST_SCROLL_CLASS}>
        <div
          className={GUESTS_TABLIST_CLASS}
          role="tablist"
          aria-label="Offer details"
        >
          {viewModel.tabs.map((tab) => {
            const selected = tab.id === viewModel.activeTabId
            return (
              <Button
                key={tab.id}
                type="button"
                role="tab"
                aria-selected={selected}
                variant="op-ghost"
                className={cn(
                  GUESTS_TAB_BUTTON_CLASS,
                  selected
                    ? GUESTS_TAB_BUTTON_ACTIVE_CLASS
                    : GUESTS_TAB_BUTTON_INACTIVE_CLASS
                )}
                onClick={() => {
                  onTabChange(tab.id)
                }}
              >
                {tab.label}
              </Button>
            )
          })}
        </div>
      </div>

      {viewModel.activeTabId === "overview" ? (
        <OfferDetailsOverviewPanel
          overview={viewModel.overview}
          onCommitDateRange={onCommitDateRange}
        />
      ) : null}
      {viewModel.activeTabId === "claims" ? (
        <OfferDetailsClaimsPanel
          claims={viewModel.claims}
          onShareOfferInCampaign={onShareOfferInCampaign}
          onRowAction={onClaimsRowAction}
        />
      ) : null}
      {viewModel.activeTabId === "redemptions" ? (
        <OfferDetailsRedemptionsPanel
          redemptions={viewModel.redemptions}
          onRowAction={onRedemptionsRowAction}
        />
      ) : null}
      {viewModel.activeTabId === "campaigns" ? (
        <OfferDetailsCampaignsPanel
          campaigns={viewModel.campaigns}
          onSubTabChange={onCampaignsSubTabChange}
        />
      ) : null}
      {viewModel.activeTabId === "void-requests" ? (
        <OfferDetailsVoidRequestsPanel
          voidRequests={viewModel.voidRequests}
          onRowAction={onVoidRequestsRowAction}
        />
      ) : null}
    </div>
  )
}

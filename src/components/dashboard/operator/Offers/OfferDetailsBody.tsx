import { Link } from "react-router-dom"
import { ChevronRightIcon } from "lucide-react"

import { GuestProfileDetailRows } from "@/components/dashboard/operator/GuestProfile/GuestProfileDetailRows"
import { OfferDetailsHeaderActionsMenu } from "@/components/dashboard/operator/Offers/OfferDetailsHeaderActionsMenu"
import { OfferDetailsOverviewPanel } from "@/components/dashboard/operator/Offers/OfferDetailsOverviewPanel"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import type { OfferDetailsViewModel } from "@/lib/operatorOffers/createOfferDetailsPageModule"
import type {
  OfferDetailsDateRange,
  OfferDetailsHeaderActionId,
  OfferDetailsTabId,
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
  GUESTS_TABLE_EMPTY_COPY_STACK_CLASS,
  GUESTS_TABLE_EMPTY_SHELL_CLASS,
  GUESTS_TABLE_EMPTY_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { GUEST_PROFILE_HEADER_IDENTITY_CLASS, GUEST_PROFILE_HEADER_IDENTITY_COPY_CLASS, GUEST_PROFILE_HEADER_SUBTITLE_CLASS } from "@/lib/operatorGuestProfile/guestProfilePresentation"
import { cn } from "@/lib/utils"

type OfferDetailsBodyProps = {
  viewModel: OfferDetailsViewModel
  offersHref: string
  onEditOffer: () => void
  onOpenStaffRedeem: () => void
  onHeaderAction: (actionId: OfferDetailsHeaderActionId) => void
  onTabChange: (tabId: OfferDetailsTabId) => void
  onCommitDateRange: (range: OfferDetailsDateRange) => void
}

export function OfferDetailsBody({
  viewModel,
  offersHref,
  onEditOffer,
  onOpenStaffRedeem,
  onHeaderAction,
  onTabChange,
  onCommitDateRange,
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
        <GuestProfileDetailRows rows={viewModel.metaRows} />
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
      ) : (
        <div className={GUESTS_TABLE_EMPTY_SHELL_CLASS}>
          <div className={GUESTS_TABLE_EMPTY_COPY_STACK_CLASS}>
            <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>
              {viewModel.activeTabEmptyPlaceholder}
            </p>
          </div>
        </div>
      )}
    </div>
  )
}

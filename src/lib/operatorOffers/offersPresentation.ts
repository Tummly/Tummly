/** Figma Offers main page — node 3498:1587 (header + Performance + Needs attention + list chrome). */

import type {
  OperatorOffersListEmptyStateKind,
  OperatorOffersListViewId,
  OperatorOffersSortId,
} from "@/types/operatorCampaigns"

export const OFFERS_PAGE_COPY = {
  title: "Offers",
  subtitle:
    "Create simple, controlled offers guests can claim after feedback, signup or a campaign. Track claims, redemptions, expiry and misuse in one place.",
  createOffer: "Create offer",
  openStaffRedeem: "Open staff redeem",
  viewRedemptionLog: "View redemption log",
  useTemplate: "Use a template",
  loadError: "Could not load offers for this location.",
  retry: "Retry",
  performanceAriaLabel: "Performance",
  needsAttentionTitle: "Needs attention",
  needsAttentionSubtitle: "Review issues that may require action.",
  viewAllInNeedsAttention: "View all in Needs attention",
  listSectionTitle: "Offers",
  listSectionSubtitle:
    "Review catalog offers, claims and redemptions for the selected location.",
  searchPlaceholder: "Search by offer name, code prefix, campaign",
  filtersLabel: "Filters",
  filterSheetTitle: "Filter offers",
  trueEmptyTitle: "No offers yet",
  trueEmptyHelper:
    "Create your first catalog offer or start with a template.",
  filterSearchTitle: "No results from filters",
  filterSearchHelper: "Try removing a filter or changing your search.",
  viewAllOffers: "View all offers",
  clearAllFilters: "Clear all filters",
  pauseConfirmTitle: "Pause this offer?",
  pauseConfirmDescription:
    "Guests will not be able to claim this offer until you resume it.",
  resumeConfirmTitle: "Resume this offer?",
  resumeConfirmDescription:
    "This offer will be available to claim again where it is attached.",
  archiveConfirmTitle: "Archive this offer?",
  archiveConfirmDescription:
    "Archived offers leave the active catalog. You can still view them later.",
  duplicateConfirmTitle: "Duplicate this offer?",
  duplicateConfirmDescription:
    "Creates a new Draft copy of this offer.",
  confirmAction: "Confirm",
  cancelAction: "Cancel",
} as const

export const OPERATOR_OFFERS_DEFAULT_SORT_ID: OperatorOffersSortId =
  "recent-activity"

export const OPERATOR_OFFERS_SORT_LABELS: Record<OperatorOffersSortId, string> =
  {
    "recent-activity": "Recent activity",
    "title-az": "Title A–Z",
  }

export const OPERATOR_OFFERS_SORT_OPTIONS: readonly [
  OperatorOffersSortId,
  string,
][] = (
  Object.entries(OPERATOR_OFFERS_SORT_LABELS) as [
    OperatorOffersSortId,
    string,
  ][]
)

export const OPERATOR_OFFERS_LIST_VIEW_LABELS: Record<
  OperatorOffersListViewId,
  string
> = {
  all: "All",
  "needs-attention": "Needs attention",
  drafts: "Drafts",
  "in-flight": "In flight",
  sent: "Sent",
}

export const OPERATOR_OFFERS_LIST_VIEW_ORDER: readonly OperatorOffersListViewId[] =
  ["all", "needs-attention", "drafts", "in-flight", "sent"] as const

export const OPERATOR_OFFERS_VIEW_SCOPED_EMPTY_COPY: Record<
  Exclude<OperatorOffersListViewId, "all">,
  { title: string; helper: string }
> = {
  "needs-attention": {
    title: "No offers need attention",
    helper:
      "Offers that need a decision or fix will appear here when attention signals are available.",
  },
  drafts: {
    title: "No drafts yet",
    helper: "Create an offer or use a template to start a draft.",
  },
  "in-flight": {
    title: "No offers in flight",
    helper:
      "Offers with a live campaign, recovery or thank-you attach will appear here.",
  },
  sent: {
    title: "No closed offers yet",
    helper: "Paused, expired and archived offers will appear here.",
  },
}

export function offersListEmptyCopy(input: {
  kind: OperatorOffersListEmptyStateKind
  activeViewId: OperatorOffersListViewId
}): { title: string; helper: string } {
  if (input.kind === "true-empty") {
    return {
      title: OFFERS_PAGE_COPY.trueEmptyTitle,
      helper: OFFERS_PAGE_COPY.trueEmptyHelper,
    }
  }

  if (input.kind === "filter-search") {
    return {
      title: OFFERS_PAGE_COPY.filterSearchTitle,
      helper: OFFERS_PAGE_COPY.filterSearchHelper,
    }
  }

  if (input.activeViewId === "all") {
    return {
      title: OFFERS_PAGE_COPY.trueEmptyTitle,
      helper: OFFERS_PAGE_COPY.trueEmptyHelper,
    }
  }

  return OPERATOR_OFFERS_VIEW_SCOPED_EMPTY_COPY[input.activeViewId]
}

export const OFFERS_PAGE_SIZE = 25

export const OFFERS_PAGE_META_CLASS =
  "m-0 text-sm font-normal leading-5 text-muted-foreground"

/** Helper under KPI value — Figma Main Bg/Subtitle + KPI info size. */
export const OFFERS_KPI_HELPER_CLASS =
  "m-0 text-op-kpi-info-size font-normal leading-normal text-op-card-subtitle-color"

export const OFFERS_KPI_HELPER_ROW_CLASS = "flex items-start pt-3.5"

export const OFFERS_TRUE_EMPTY_ACTIONS_CLASS =
  "mt-[30px] flex items-center justify-center gap-3"

export const OFFERS_TRUE_EMPTY_HELPER_CLASS =
  "m-0 max-w-[306px] text-sm font-medium leading-[18px] text-[var(--op-color-gray-550)]"

export const OFFERS_SEARCH_MISS_CLASS =
  "m-0 text-op-sm font-medium leading-normal text-op-card-title-color"

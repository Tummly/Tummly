import { useState } from "react"
import { Link } from "react-router-dom"
import { ChevronRightIcon, MoreVerticalIcon } from "lucide-react"

import { GuestProfileDetailRows } from "@/components/dashboard/operator/GuestProfile/GuestProfileDetailRows"
import { GuestProfileOverviewPanel } from "@/components/dashboard/operator/GuestProfile/GuestProfileOverviewPanel"
import { GuestProfileSectionEmptyCard } from "@/components/dashboard/operator/GuestProfile/GuestProfileSectionEmptyCard"
import { GuestProfileTableEmptyCard } from "@/components/dashboard/operator/GuestProfile/GuestProfileTableEmptyCard"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  GUEST_PROFILE_BREADCRUMB_GUESTS,
  GUEST_PROFILE_EMPTY_COPY,
  GUEST_PROFILE_TABS,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  operatorDashboardNavPath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"
import {
  GUESTS_MARKETING_STATUS_BADGE_CLASS,
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_HEADER_ROW_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_TAB_BUTTON_ACTIVE_CLASS,
  GUESTS_TAB_BUTTON_CLASS,
  GUESTS_TAB_BUTTON_INACTIVE_CLASS,
  GUESTS_TABLIST_CLASS,
  GUESTS_TABLIST_SCROLL_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"
import type {
  OperatorGuestProfileTabId,
  OperatorGuestProfileViewModel,
} from "@/types/operatorGuestProfile"

type GuestProfileShellProps = {
  mode: OperatorDashboardMode
  selectedLocationId: number
  viewModel: OperatorGuestProfileViewModel
}

const PROFILE_SUMMARY_ROWS: Array<{
  label: string
  value: (vm: OperatorGuestProfileViewModel) => string | number
}> = [
  { label: "Email", value: (vm) => vm.profileSummary.emailDisplay },
  { label: "Mobile", value: (vm) => vm.profileSummary.mobileDisplay },
  {
    label: "First captured date",
    value: (vm) => vm.profileSummary.firstCapturedDisplay,
  },
  {
    label: "Location or locations",
    value: (vm) => vm.profileSummary.locationName,
  },
  {
    label: "Feedback submissions",
    value: (vm) => vm.profileSummary.feedbackSubmissionCount,
  },
  {
    label: "Offer claims and redemptions",
    value: (vm) => vm.profileSummary.offerClaimsAndRedemptions,
  },
  {
    label: "Last interaction",
    value: (vm) => vm.profileSummary.lastInteractionDisplay,
  },
  {
    label: "Guest tags",
    value: (vm) => vm.profileSummary.guestTagsDisplay,
  },
]

function DisabledAddNoteButton() {
  return (
    <Button
      type="button"
      disabled
      aria-disabled
      aria-label="Add note (unavailable)"
      title="Add note is unavailable"
      className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
    >
      Add note
    </Button>
  )
}

function GuestProfileTabPanel({
  activeTabId,
  viewModel,
}: {
  activeTabId: OperatorGuestProfileTabId
  viewModel: OperatorGuestProfileViewModel
}) {
  if (activeTabId === "overview") {
    return <GuestProfileOverviewPanel viewModel={viewModel} />
  }

  if (activeTabId === "feedbacks") {
    const copy = GUEST_PROFILE_EMPTY_COPY.feedbacksTab
    return (
      <GuestProfileTableEmptyCard
        sectionTitle={copy.sectionTitle}
        searchPlaceholder={copy.searchPlaceholder}
        emptyTitle={copy.emptyTitle}
        emptyHelper={copy.emptyHelper}
      />
    )
  }

  if (activeTabId === "offers") {
    const copy = GUEST_PROFILE_EMPTY_COPY.offersTab
    return (
      <GuestProfileTableEmptyCard
        sectionTitle={copy.sectionTitle}
        searchPlaceholder={copy.searchPlaceholder}
        emptyTitle={copy.emptyTitle}
        emptyHelper={copy.emptyHelper}
      />
    )
  }

  if (activeTabId === "campaigns") {
    const copy = GUEST_PROFILE_EMPTY_COPY.campaignsTab
    return (
      <GuestProfileTableEmptyCard
        sectionTitle={copy.sectionTitle}
        searchPlaceholder={copy.searchPlaceholder}
        emptyTitle={copy.emptyTitle}
        emptyHelper={copy.emptyHelper}
        headerAction={
          <Button
            type="button"
            disabled
            aria-disabled
            aria-label="Create campaign (unavailable)"
            title="Create campaign is unavailable"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
          >
            Create campaign
          </Button>
        }
      />
    )
  }

  if (activeTabId === "activity") {
    const copy = GUEST_PROFILE_EMPTY_COPY.activityTab
    return (
      <GuestProfileSectionEmptyCard
        sectionTitle={copy.sectionTitle}
        emptyTitle={copy.emptyTitle}
        emptyHelper={copy.emptyHelper}
      />
    )
  }

  const notes = GUEST_PROFILE_EMPTY_COPY.notesTab
  return (
    <GuestProfileSectionEmptyCard
      sectionTitle={notes.sectionTitle}
      emptyTitle={notes.emptyTitle}
      emptyHelper={notes.emptyHelper}
      headerAction={<DisabledAddNoteButton />}
    />
  )
}

export function GuestProfileShell({
  mode,
  selectedLocationId,
  viewModel,
}: GuestProfileShellProps) {
  const [activeTabId, setActiveTabId] =
    useState<OperatorGuestProfileTabId>("overview")
  const guestsListPath = operatorDashboardNavPath(
    mode,
    "guests",
    selectedLocationId
  )

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <nav
        aria-label="Breadcrumb"
        className="flex flex-wrap items-center gap-1.5 text-sm font-medium text-muted-foreground"
      >
        <Link
          to={guestsListPath}
          className="text-muted-foreground hover:text-foreground"
        >
          {GUEST_PROFILE_BREADCRUMB_GUESTS}
        </Link>
        <ChevronRightIcon className="size-4 shrink-0" aria-hidden />
        <span className="text-foreground">{viewModel.name}</span>
      </nav>

      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <header className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{viewModel.name}</h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
            {viewModel.identitySubtitle}
          </p>
          <Badge
            variant="soft"
            className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
          >
            {viewModel.marketingStatusLabel}
          </Badge>
        </header>

        <div className="flex shrink-0 flex-wrap items-center gap-3">
          <Button
            type="button"
            disabled
            aria-disabled
            aria-label="Create campaign (unavailable)"
            title="Create campaign is unavailable"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
          >
            Create campaign
          </Button>
          <Button
            type="button"
            disabled
            aria-disabled
            aria-label="Edit guest details (unavailable)"
            title="Edit guest details is unavailable"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
          >
            Edit guest details
          </Button>
          <Button
            type="button"
            variant="outline"
            size="icon"
            disabled
            aria-disabled
            aria-label="Actions (unavailable)"
            title="Actions is unavailable"
            className="size-10 rounded-[2px]"
          >
            <MoreVerticalIcon className="size-4" aria-hidden />
          </Button>
        </div>
      </div>

      <section className={GUESTS_SECTION_CLASS}>
        <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>Profile summary</h2>
        </div>
        <GuestProfileDetailRows
          rows={PROFILE_SUMMARY_ROWS.map((row) => ({
            label: row.label,
            value: row.value(viewModel),
          }))}
        />
      </section>

      <div className={GUESTS_TABLIST_SCROLL_CLASS}>
        <div
          role="tablist"
          aria-label="Guest profile sections"
          className={GUESTS_TABLIST_CLASS}
        >
          {GUEST_PROFILE_TABS.map((tab) => {
            const isActive = tab.id === activeTabId
            return (
              <Button
                key={tab.id}
                type="button"
                role="tab"
                variant="ghost"
                aria-selected={isActive}
                className={cn(
                  GUESTS_TAB_BUTTON_CLASS,
                  isActive
                    ? GUESTS_TAB_BUTTON_ACTIVE_CLASS
                    : GUESTS_TAB_BUTTON_INACTIVE_CLASS
                )}
                onClick={() => {
                  setActiveTabId(tab.id)
                }}
              >
                {tab.label}
              </Button>
            )
          })}
        </div>
      </div>

      <GuestProfileTabPanel
        activeTabId={activeTabId}
        viewModel={viewModel}
      />
    </div>
  )
}

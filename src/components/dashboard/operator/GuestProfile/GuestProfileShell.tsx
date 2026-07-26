import { useEffect, useRef, useState } from "react"
import { Link, useLocation, useNavigate } from "react-router-dom"
import { ChevronRightIcon } from "lucide-react"

import { GuestProfileActivityPanel } from "@/components/dashboard/operator/GuestProfile/GuestProfileActivityPanel"
import { GuestProfileAddNoteButton } from "@/components/dashboard/operator/GuestProfile/GuestProfileAddNoteButton"
import { GuestProfileAddNoteDialog } from "@/components/dashboard/operator/GuestProfile/GuestProfileAddNoteDialog"
import { GuestProfileDetailRows } from "@/components/dashboard/operator/GuestProfile/GuestProfileDetailRows"
import { GuestProfileFeedbacksPanel } from "@/components/dashboard/operator/GuestProfile/GuestProfileFeedbacksPanel"
import { GuestProfileHeaderActionsMenu } from "@/components/dashboard/operator/GuestProfile/GuestProfileHeaderActionsMenu"
import { GuestProfileOverviewPanel } from "@/components/dashboard/operator/GuestProfile/GuestProfileOverviewPanel"
import { GuestProfileSectionEmptyCard } from "@/components/dashboard/operator/GuestProfile/GuestProfileSectionEmptyCard"
import { GuestProfileTableEmptyCard } from "@/components/dashboard/operator/GuestProfile/GuestProfileTableEmptyCard"
import { HomeFeedbackDetailsDrawer } from "@/components/dashboard/operator/Home/HomeFeedbackDetailsDrawer"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import {
  GUEST_PROFILE_BREADCRUMB_GUESTS,
  GUEST_PROFILE_EMPTY_COPY,
  GUEST_PROFILE_TABS,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  guestProfileHandoffHasIntent,
  readGuestProfileLocationHandoff,
} from "@/lib/operatorGuestProfile/guestProfileLocationHandoff"
import type {
  OperatorGuestProfileNotesSnapshot,
} from "@/lib/operatorGuestProfile/createOperatorGuestProfilePageModule"
import type { FeedbackDetailsSnapshot } from "@/lib/operatorHome/createFeedbackDetailsModule"
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
  GUESTS_TABLE_LOCATION_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"
import type { FeedbackSentiment } from "@/types/dashboard"
import type {
  OperatorGuestProfileNoteRow,
  OperatorGuestProfileTabId,
  OperatorGuestProfileViewModel,
} from "@/types/operatorGuestProfile"

type GuestProfileShellProps = {
  mode: OperatorDashboardMode
  selectedLocationId: number
  viewModel: OperatorGuestProfileViewModel
  feedbackDetails: FeedbackDetailsSnapshot
  notes: OperatorGuestProfileNotesSnapshot
  editGuestDetailsPath: string
  onOpenFeedback: (feedbackId: number) => void
  onFeedbackDetailsOpenChange: (open: boolean) => void
  onRetryFeedbackDetails: () => void
  onStartClassificationCorrection: () => void
  onClassificationDraftSentimentChange: (sentiment: FeedbackSentiment) => void
  onCancelClassificationCorrection: () => void
  onSaveClassificationCorrection: () => void
  onFeedbackInternalNoteDraftChange: (value: string) => void
  onCreateFeedbackInternalNote: () => void
  onViewGuestProfile: (locationGuestId: number) => void
  onEnsureNotesLoaded: () => void
  onRetryNotesLoad: () => void
  onCreateNote: (body: string) => Promise<boolean>
  onManageTags: () => void
  onExportGuestRecord: () => void
  onDeleteGuestData: () => void
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

function NotesFeedRow({ row }: { row: OperatorGuestProfileNoteRow }) {
  return (
    <article className="flex flex-col gap-2 border-b border-[#e5e5e5] py-5 last:border-b-0 dark:border-[#262626]">
      <p className="text-sm font-semibold tracking-[-0.2px] text-foreground">
        {row.authorDisplayName}
        <span className="font-semibold"> · </span>
        {row.createdAtDisplay}
      </p>
      <p className={`whitespace-pre-wrap ${GUESTS_TABLE_LOCATION_CLASS}`}>
        {row.body}
      </p>
    </article>
  )
}

function NotesTabPanel({
  notes,
  onAddNote,
  onRetry,
}: {
  notes: OperatorGuestProfileNotesSnapshot
  onAddNote: () => void
  onRetry: () => void
}) {
  const copy = GUEST_PROFILE_EMPTY_COPY.notesTab
  const addNote = <GuestProfileAddNoteButton onClick={onAddNote} />

  if (notes.loadStatus === "loading" || notes.loadStatus === "idle") {
    return (
      <section className={GUESTS_SECTION_CLASS} aria-label={copy.sectionTitle}>
        <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.sectionTitle}</h2>
          {addNote}
        </div>
        <div className="flex min-h-32 items-center justify-center">
          <Spinner aria-label="Loading notes" />
        </div>
      </section>
    )
  }

  if (notes.loadStatus === "error") {
    return (
      <section className={GUESTS_SECTION_CLASS} aria-label={copy.sectionTitle}>
        <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.sectionTitle}</h2>
          {addNote}
        </div>
        <div className="flex min-h-32 flex-col items-center justify-center gap-3 text-center">
          <p className="text-sm text-destructive">
            Could not load notes. Please try again.
          </p>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onRetry}
          >
            Retry
          </Button>
        </div>
      </section>
    )
  }

  if (notes.items.length === 0) {
    return (
      <GuestProfileSectionEmptyCard
        sectionTitle={copy.sectionTitle}
        emptyTitle={copy.emptyTitle}
        emptyHelper={copy.emptyHelper}
        headerAction={addNote}
      />
    )
  }

  return (
    <section className={GUESTS_SECTION_CLASS} aria-label={copy.sectionTitle}>
      <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.sectionTitle}</h2>
        {addNote}
      </div>
      <div className="flex flex-col">
        {notes.items.map((row) => (
          <NotesFeedRow key={row.id} row={row} />
        ))}
      </div>
    </section>
  )
}

function GuestProfileTabPanel({
  activeTabId,
  viewModel,
  notes,
  onOpenFeedback,
  onViewAllFeedbacks,
  onAddNote,
  onRetryNotes,
}: {
  activeTabId: OperatorGuestProfileTabId
  viewModel: OperatorGuestProfileViewModel
  notes: OperatorGuestProfileNotesSnapshot
  onOpenFeedback: (feedbackId: number) => void
  onViewAllFeedbacks: () => void
  onAddNote: () => void
  onRetryNotes: () => void
}) {
  if (activeTabId === "overview") {
    return (
      <GuestProfileOverviewPanel
        viewModel={viewModel}
        onOpenFeedback={onOpenFeedback}
        onViewAllFeedbacks={onViewAllFeedbacks}
        onAddNote={onAddNote}
      />
    )
  }

  if (activeTabId === "feedbacks") {
    return (
      <GuestProfileFeedbacksPanel
        guestId={Number(viewModel.id)}
        locationId={viewModel.locationId}
        active
        onOpenFeedback={onOpenFeedback}
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
          <Button variant="op-primary"
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
    return (
      <GuestProfileActivityPanel
        guestId={Number(viewModel.id)}
        locationId={viewModel.locationId}
        active
      />
    )
  }

  return (
    <NotesTabPanel
      notes={notes}
      onAddNote={onAddNote}
      onRetry={onRetryNotes}
    />
  )
}

function readInitialTab(
  state: unknown
): OperatorGuestProfileTabId {
  return readGuestProfileLocationHandoff(state).tab ?? "overview"
}

export function GuestProfileShell({
  mode,
  selectedLocationId,
  viewModel,
  feedbackDetails,
  notes,
  editGuestDetailsPath,
  onOpenFeedback,
  onFeedbackDetailsOpenChange,
  onRetryFeedbackDetails,
  onStartClassificationCorrection,
  onClassificationDraftSentimentChange,
  onCancelClassificationCorrection,
  onSaveClassificationCorrection,
  onFeedbackInternalNoteDraftChange,
  onCreateFeedbackInternalNote,
  onViewGuestProfile,
  onEnsureNotesLoaded,
  onRetryNotesLoad,
  onCreateNote,
  onManageTags,
  onExportGuestRecord,
  onDeleteGuestData,
}: GuestProfileShellProps) {
  const location = useLocation()
  const navigate = useNavigate()
  const handoffConsumedRef = useRef(false)
  const [activeTabId, setActiveTabId] = useState<OperatorGuestProfileTabId>(() =>
    readInitialTab(location.state)
  )
  const [addNoteOpen, setAddNoteOpen] = useState(false)
  const guestsListPath = operatorDashboardNavPath(
    mode,
    "guests",
    selectedLocationId
  )

  useEffect(() => {
    if (activeTabId === "notes") {
      void onEnsureNotesLoaded()
    }
  }, [activeTabId, onEnsureNotesLoaded])

  useEffect(() => {
    if (handoffConsumedRef.current) {
      return
    }
    handoffConsumedRef.current = true

    const handoff = readGuestProfileLocationHandoff(location.state)
    if (!guestProfileHandoffHasIntent(handoff)) {
      return
    }

    if (handoff.tab != null) {
      setActiveTabId(handoff.tab)
    }
    if (handoff.openFeedbackId != null) {
      void onOpenFeedback(handoff.openFeedbackId)
    }

    navigate(".", { replace: true, state: null })
  }, [location.state, navigate, onOpenFeedback])

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
          <Button variant="op-primary"
            type="button"
            disabled
            aria-disabled
            aria-label="Create campaign (unavailable)"
            title="Create campaign is unavailable"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
          >
            Create campaign
          </Button>
          <Button variant="op-secondary"
            type="button"
            asChild
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
          >
            <Link to={editGuestDetailsPath}>Edit guest details</Link>
          </Button>
          <GuestProfileHeaderActionsMenu
            guestName={viewModel.name}
            onManageTags={onManageTags}
            onExportGuestRecord={onExportGuestRecord}
            onDeleteGuestData={onDeleteGuestData}
          />
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
                variant="op-ghost"
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
        notes={notes}
        onOpenFeedback={onOpenFeedback}
        onViewAllFeedbacks={() => {
          setActiveTabId("feedbacks")
        }}
        onAddNote={() => {
          setAddNoteOpen(true)
        }}
        onRetryNotes={onRetryNotesLoad}
      />

      <GuestProfileAddNoteDialog
        open={addNoteOpen}
        onOpenChange={setAddNoteOpen}
        busy={notes.createStatus === "saving"}
        onSave={onCreateNote}
      />

      <HomeFeedbackDetailsDrawer
        snapshot={feedbackDetails}
        onOpenChange={onFeedbackDetailsOpenChange}
        onRetry={onRetryFeedbackDetails}
        onStartCorrection={onStartClassificationCorrection}
        onDraftSentimentChange={onClassificationDraftSentimentChange}
        onCancelCorrection={onCancelClassificationCorrection}
        onSaveCorrection={onSaveClassificationCorrection}
        onViewGuestProfile={onViewGuestProfile}
        onNoteDraftChange={onFeedbackInternalNoteDraftChange}
        onCreateNote={onCreateFeedbackInternalNote}
      />
    </div>
  )
}

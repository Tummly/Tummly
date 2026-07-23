import { Link, useNavigate } from "react-router-dom"
import { toast } from "sonner"

import { useGuestProfilePageModule } from "@/components/dashboard/operator/GuestProfile/utils/useGuestProfilePageModule"
import { GuestProfileShell } from "@/components/dashboard/operator/GuestProfile/GuestProfileShell"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import {
  GUEST_PROFILE_BACK_TO_GUESTS_LABEL,
  GUEST_PROFILE_UNAVAILABLE_HELPER,
  GUEST_PROFILE_UNAVAILABLE_TITLE,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  guestProfileHeaderActionPaths,
  operatorDashboardGuestProfilePath,
  operatorDashboardNavPath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"
import {
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

type GuestProfilePageProps = {
  mode: OperatorDashboardMode
  selectedLocationId: number
}

export function GuestProfilePage({
  mode,
  selectedLocationId,
}: GuestProfilePageProps) {
  const {
    snapshot,
    retryLoad,
    ensureNotesLoaded,
    retryNotesLoad,
    createNote,
    exportGuestRecord,
    openFeedbackDetails,
    closeFeedbackDetails,
    retryFeedbackDetails,
    startClassificationCorrection,
    setClassificationDraftSentiment,
    cancelClassificationCorrection,
    saveClassificationCorrection,
  } = useGuestProfilePageModule()
  const navigate = useNavigate()
  const guestsListPath = operatorDashboardNavPath(
    mode,
    "guests",
    selectedLocationId
  )

  const navigateToGuestProfile = (locationGuestId: number) => {
    navigate(
      operatorDashboardGuestProfilePath(
        mode,
        locationGuestId,
        selectedLocationId
      )
    )
  }

  if (snapshot.loadStatus === "unavailable") {
    return (
      <div className={`${GUESTS_PAGE_STACK_CLASS} items-start`}>
        <header className="flex flex-col gap-2">
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>
            {GUEST_PROFILE_UNAVAILABLE_TITLE}
          </h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
            {GUEST_PROFILE_UNAVAILABLE_HELPER}
          </p>
        </header>
        <Button asChild variant="outline" size="sm">
          <Link to={guestsListPath}>{GUEST_PROFILE_BACK_TO_GUESTS_LABEL}</Link>
        </Button>
      </div>
    )
  }

  if (
    snapshot.viewModel == null &&
    (snapshot.loadStatus === "idle" || snapshot.loadStatus === "loading")
  ) {
    return (
      <div className="flex min-h-48 items-center justify-center">
        <Spinner aria-label="Loading guest profile" />
      </div>
    )
  }

  if (snapshot.viewModel == null && snapshot.loadStatus === "error") {
    return (
      <div className="flex min-h-48 flex-col items-center justify-center gap-3 text-center">
        <p className="text-sm text-destructive">
          Could not load guest profile. Please try again.
        </p>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => {
            void retryLoad()
          }}
        >
          Retry
        </Button>
      </div>
    )
  }

  if (snapshot.viewModel == null) {
    return null
  }

  const headerPaths = guestProfileHeaderActionPaths(
    mode,
    snapshot.viewModel.id,
    selectedLocationId
  )

  return (
    <GuestProfileShell
      mode={mode}
      selectedLocationId={selectedLocationId}
      viewModel={snapshot.viewModel}
      feedbackDetails={snapshot.feedbackDetails}
      notes={snapshot.notes}
      editGuestDetailsPath={headerPaths.editGuestDetails}
      onOpenFeedback={(feedbackId) => {
        void openFeedbackDetails(feedbackId)
      }}
      onFeedbackDetailsOpenChange={(open) => {
        if (!open) {
          closeFeedbackDetails()
        }
      }}
      onRetryFeedbackDetails={() => {
        void retryFeedbackDetails()
      }}
      onStartClassificationCorrection={startClassificationCorrection}
      onClassificationDraftSentimentChange={setClassificationDraftSentiment}
      onCancelClassificationCorrection={cancelClassificationCorrection}
      onSaveClassificationCorrection={() => {
        void saveClassificationCorrection()
      }}
      onViewGuestProfile={navigateToGuestProfile}
      onEnsureNotesLoaded={ensureNotesLoaded}
      onRetryNotesLoad={retryNotesLoad}
      onCreateNote={createNote}
      onManageTags={() => {
        navigate(headerPaths.manageTags)
      }}
      onExportGuestRecord={() => {
        void (async () => {
          const result = await exportGuestRecord()
          if (result.status === "error") {
            toast.error(result.message)
          }
        })()
      }}
      onDeleteGuestData={() => {
        navigate(headerPaths.deleteGuestData)
      }}
    />
  )
}

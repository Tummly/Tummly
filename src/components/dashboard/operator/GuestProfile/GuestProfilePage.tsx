import { Link } from "react-router-dom"

import { useGuestProfilePageModule } from "@/components/dashboard/operator/GuestProfile/utils/useGuestProfilePageModule"
import { GuestProfileShell } from "@/components/dashboard/operator/GuestProfile/GuestProfileShell"
import { Button } from "@/components/ui/button"
import {
  GUEST_PROFILE_BACK_TO_GUESTS_LABEL,
  GUEST_PROFILE_UNAVAILABLE_HELPER,
  GUEST_PROFILE_UNAVAILABLE_TITLE,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
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
  const { snapshot, retryLoad } = useGuestProfilePageModule()
  const guestsListPath = operatorDashboardNavPath(
    mode,
    "guests",
    selectedLocationId
  )

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
      <div
        className="flex min-h-48 items-center justify-center"
        role="status"
        aria-live="polite"
        aria-label="Loading guest profile"
      >
        <div
          className="size-8 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
          aria-hidden
        />
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

  return (
    <GuestProfileShell
      mode={mode}
      selectedLocationId={selectedLocationId}
      viewModel={snapshot.viewModel}
    />
  )
}

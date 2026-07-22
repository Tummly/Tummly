import { HomeBody } from "@/components/dashboard/operator/Home/HomeBody"
import {
  useDashboardUiStore,
} from "@/components/dashboard/operator/DashboardUiStoreProvider"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { useHomePageModule } from "@/components/dashboard/operator/Home/utils/useHomePageModule"
import type { ActivationPeriodBadgeCopy } from "@/lib/operatorHome/activationPeriod"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import { operatorDashboardGuestProfilePath } from "@/lib/operatorHome/operatorDashboardPaths"
import { useNavigate, useOutletContext } from "react-router-dom"

type HomePageProps = {
  activationPeriodBadge: ActivationPeriodBadgeCopy | null
}

export function HomePage({
  activationPeriodBadge,
}: HomePageProps) {
  const home = useHomePageModule()
  const navigate = useNavigate()
  const { mode, selectedLocationId } =
    useOutletContext<DashboardOutletContext>()
  const homePerformanceDateRange = useDashboardUiStore(
    (state) => state.homePerformanceDateRange
  )
  const setHomePerformanceDateRange = useDashboardUiStore(
    (state) => state.setHomePerformanceDateRange
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

  const viewModel = home.snapshot.viewModel
  const feedbackState =
    home.snapshot.loadStatus === "idle" ||
    home.snapshot.loadStatus === "loading" ||
    viewModel == null
      ? "loading"
      : home.snapshot.loadStatus

  const handleCommitHomePerformanceDateRange = (
    range: HomePerformanceDateRange
  ) => {
    setHomePerformanceDateRange(range)
    void home.reloadForHomePerformanceDateRange()
  }

  if (viewModel == null) {
    return (
      <div
        className="flex min-h-48 items-center justify-center"
        role="status"
        aria-live="polite"
        aria-label="Loading home"
      >
        <div
          className="size-8 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
          aria-hidden
        />
      </div>
    )
  }

  return (
    <>
      {home.snapshot.actionError ? (
        <p className="mb-3 text-sm text-destructive" role="alert">
          {home.snapshot.actionError}
        </p>
      ) : null}
      <HomeBody
        viewModel={viewModel}
        activationPeriodBadge={activationPeriodBadge}
        selectedDateRange={homePerformanceDateRange}
        onCommitHomePerformanceDateRange={handleCommitHomePerformanceDateRange}
        feedbackState={feedbackState}
        performanceLoading={home.snapshot.performanceLoadStatus === "loading"}
        onRetryFeedback={() => {
          void home.retryLoad()
        }}
        previewBusy={home.snapshot.previewBusy}
        onPreviewGuestForm={home.previewGuestForm}
        onCopySmartGuestLink={home.copySmartGuestLink}
        feedbackDetails={home.snapshot.feedbackDetails}
        onViewFeedback={(feedbackId) => {
          void home.openFeedbackDetails(feedbackId)
        }}
        onViewGuest={navigateToGuestProfile}
        onViewGuestProfile={navigateToGuestProfile}
        onFeedbackDetailsOpenChange={(open) => {
          if (!open) {
            home.closeFeedbackDetails()
          }
        }}
        onRetryFeedbackDetails={() => {
          void home.retryFeedbackDetails()
        }}
        onStartClassificationCorrection={() => {
          home.startClassificationCorrection()
        }}
        onClassificationDraftSentimentChange={(sentiment) => {
          home.setClassificationDraftSentiment(sentiment)
        }}
        onCancelClassificationCorrection={() => {
          home.cancelClassificationCorrection()
        }}
        onSaveClassificationCorrection={() => {
          void home.saveClassificationCorrection()
        }}
      />
    </>
  )
}

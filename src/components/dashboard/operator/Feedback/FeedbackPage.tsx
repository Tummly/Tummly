import { useEffect, useRef } from "react"
import { useNavigate, useOutletContext } from "react-router-dom"

import { FeedbackBody } from "@/components/dashboard/operator/Feedback/FeedbackBody"
import { useFeedbackPageModule } from "@/components/dashboard/operator/Feedback/utils/useFeedbackPageModule"
import { useDashboardUiStore } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { FEEDBACK_LOAD_ERROR_MESSAGE } from "@/lib/operatorFeedback/createOperatorFeedbackPageModule"
import { Button } from "@/components/ui/button"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import {
  operatorDashboardCaptureLocationPath,
  operatorDashboardNavPath,
} from "@/lib/operatorHome/operatorDashboardPaths"

export function FeedbackPage() {
  const feedback = useFeedbackPageModule()
  const { snapshot } = feedback
  const { mode, selectedLocationId } =
    useOutletContext<DashboardOutletContext>()
  const navigate = useNavigate()
  const inboxRef = useRef<HTMLElement | null>(null)

  const feedbackPageDateRange = useDashboardUiStore(
    (state) => state.feedbackPageDateRange
  )
  const setFeedbackPageDateRange = useDashboardUiStore(
    (state) => state.setFeedbackPageDateRange
  )

  const handleCommitDateRange = (range: HomePerformanceDateRange) => {
    setFeedbackPageDateRange(range)
    void feedback.reloadForFeedbackPageDateRange()
  }

  useEffect(() => {
    if (snapshot.scrollToInboxRequestId === 0) {
      return
    }
    inboxRef.current?.scrollIntoView({ behavior: "smooth", block: "start" })
  }, [snapshot.scrollToInboxRequestId])

  useEffect(() => {
    if (snapshot.openDateRangeRequestId === 0) {
      return
    }
    // Focus the header date-range trigger so Change period is actionable.
    document
      .querySelector<HTMLButtonElement>(
        '[aria-label="' + (snapshot.viewModel?.dateRangeLabel ?? "") + '"]'
      )
      ?.click()
  }, [snapshot.openDateRangeRequestId, snapshot.viewModel?.dateRangeLabel])

  if (
    snapshot.viewModel == null
    && (snapshot.loadStatus === "idle" || snapshot.loadStatus === "loading")
  ) {
    return (
      <div
        className="flex flex-1 items-center justify-center"
        role="status"
        aria-live="polite"
        aria-label="Loading feedback"
      >
        <div
          className="size-8 animate-spin rounded-full border-2 border-muted-foreground border-t-transparent"
          aria-hidden
        />
      </div>
    )
  }

  if (snapshot.loadStatus === "error" && snapshot.viewModel == null) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-3">
        <p className="m-0 text-sm text-muted-foreground">
          {FEEDBACK_LOAD_ERROR_MESSAGE}
        </p>
        <Button
          type="button"
          variant="op-secondary"
          onClick={() => {
            void feedback.retryLoad()
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

  const viewModel = snapshot.viewModel

  return (
    <FeedbackBody
      viewModel={viewModel}
      selectedDateRange={feedbackPageDateRange}
      onCommitDateRange={handleCommitDateRange}
      onReviewNeedsAttention={() => {
        feedback.reviewNeedsAttention()
      }}
      onChangePeriod={() => {
        feedback.requestOpenDateRange()
      }}
      onViewCapture={() => {
        if (selectedLocationId == null) {
          return
        }
        if (mode === "multi") {
          navigate(operatorDashboardCaptureLocationPath(selectedLocationId))
          return
        }
        navigate(
          operatorDashboardNavPath("single", "capture", selectedLocationId)
        )
      }}
      inboxRef={inboxRef}
    />
  )
}

import { OperatorHomeFeedbackDetailsDrawer } from "@/components/dashboard/operator/OperatorHomeFeedbackDetailsDrawer"
import { OperatorHomeHero } from "@/components/dashboard/operator/OperatorHomeHero"
import { OperatorHomeLatestActivity } from "@/components/dashboard/operator/OperatorHomeLatestActivity"
import { OperatorHomeLiveOffersSection } from "@/components/dashboard/operator/OperatorHomeLiveOffersSection"
import { OperatorHomeNeedsAttentionSection } from "@/components/dashboard/operator/OperatorHomeNeedsAttentionSection"
import { OperatorHomePerformanceOverview } from "@/components/dashboard/operator/OperatorHomePerformanceOverview"
import {
  OperatorHomeRecommendedNextStep,
  OperatorHomeWeeklyBriefSection,
} from "@/components/dashboard/operator/OperatorHomeRecommendedAndWeekly"
import { OperatorHomeSetupChecklist } from "@/components/dashboard/operator/OperatorHomeSetupChecklist"
import type { FeedbackDetailsSnapshot } from "@/lib/operatorHome/createFeedbackDetailsModule"
import type { OperatorHomeViewModel } from "@/types/operatorHome"

type OperatorHomeBodyProps = {
  viewModel: OperatorHomeViewModel
  feedbackState?: "loading" | "loaded" | "error"
  onRetryFeedback?: () => void
  previewBusy?: boolean
  downloadBusy?: boolean
  onPreviewGuestForm?: () => void
  onDownloadQr?: () => void
  feedbackDetails: FeedbackDetailsSnapshot
  onViewFeedback?: (feedbackId: number) => void
  onFeedbackDetailsOpenChange?: (open: boolean) => void
  onRetryFeedbackDetails?: () => void
}

/** Home body sections composed from the Operator Home view-model (Figma stack). */
export function OperatorHomeBody({
  viewModel,
  feedbackState = "loaded",
  onRetryFeedback,
  previewBusy = false,
  downloadBusy = false,
  onPreviewGuestForm,
  onDownloadQr,
  feedbackDetails,
  onViewFeedback,
  onFeedbackDetailsOpenChange,
  onRetryFeedbackDetails,
}: OperatorHomeBodyProps) {
  return (
    <div className="flex flex-col gap-5">
      <OperatorHomeHero
        canPreviewGuestForm={viewModel.canPreviewGuestForm}
        canDownloadQr={viewModel.canDownloadQr}
        previewBusy={previewBusy}
        downloadBusy={downloadBusy}
        onPreviewGuestForm={onPreviewGuestForm}
        onDownloadQr={onDownloadQr}
      />

      <OperatorHomeSetupChecklist
        steps={viewModel.setupSteps}
        onPreviewGuestForm={onPreviewGuestForm}
        previewBusy={previewBusy}
      />

      <OperatorHomePerformanceOverview
        kpis={viewModel.kpis}
        feedbackLoading={feedbackState === "loading"}
      />

      <OperatorHomeNeedsAttentionSection />

      <OperatorHomeLiveOffersSection />

      <OperatorHomeRecommendedNextStep />

      {feedbackState === "loading" ? (
        <div
          className="rounded-[10px] bg-[#f8f8f8] px-6 py-10 text-center dark:bg-white/5"
          role="status"
          aria-live="polite"
          aria-label="Loading latest activity"
        >
          <div
            className="mx-auto size-6 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
            aria-hidden
          />
        </div>
      ) : feedbackState === "error" ? (
        <div className="rounded-[10px] bg-[#f8f8f8] px-6 py-10 text-center dark:bg-white/5">
          <p className="text-sm text-destructive">
            Could not load latest activity. Please try again.
          </p>
          {onRetryFeedback ? (
            <button
              type="button"
              className="mt-3 text-sm font-medium text-primary underline"
              onClick={onRetryFeedback}
            >
              Retry
            </button>
          ) : null}
        </div>
      ) : (
        <OperatorHomeLatestActivity
          activityByTab={viewModel.activityByTab}
          activityEmpty={viewModel.activityEmpty}
          dateRangeLabel={viewModel.dateRangeLabel}
          onViewFeedback={onViewFeedback}
        />
      )}

      <OperatorHomeWeeklyBriefSection />

      <OperatorHomeFeedbackDetailsDrawer
        snapshot={feedbackDetails}
        onOpenChange={(open) => {
          onFeedbackDetailsOpenChange?.(open)
        }}
        onRetry={() => {
          onRetryFeedbackDetails?.()
        }}
      />
    </div>
  )
}

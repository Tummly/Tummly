import { OperatorHomeFeedbackDetailsDrawer } from "@/components/dashboard/operator/OperatorHomeFeedbackDetailsDrawer"
import { OperatorHomeHero } from "@/components/dashboard/operator/OperatorHomeHero"
import { OperatorHomeLatestActivity } from "@/components/dashboard/operator/OperatorHomeLatestActivity"
import { OperatorHomeLiveOffersSection } from "@/components/dashboard/operator/OperatorHomeLiveOffersSection"
import { OperatorHomeNeedsAttentionSection } from "@/components/dashboard/operator/OperatorHomeNeedsAttentionSection"
import { OperatorHomePerformanceOverview } from "@/components/dashboard/operator/OperatorHomePerformanceOverview"
import { OperatorHomeRecommendedNextStep } from "@/components/dashboard/operator/OperatorHomeRecommendedNextStep"
import { OperatorHomeWeeklyBriefSection } from "@/components/dashboard/operator/OperatorHomeWeeklyBriefSection"
import { OperatorHomeSetupChecklist } from "@/components/dashboard/operator/OperatorHomeSetupChecklist"
import { Button } from "@/components/ui/button"
import { OPERATOR_HOME_CARD_CLASS } from "@/lib/operatorHome/operatorHomeSectionPresentation"
import type { ActivationPeriodBadgeCopy } from "@/lib/operatorHome/activationPeriod"
import type { FeedbackDetailsSnapshot } from "@/lib/operatorHome/createFeedbackDetailsModule"
import type { FeedbackSentiment } from "@/types/dashboard"
import type { OperatorHomeViewModel } from "@/types/operatorHome"

type OperatorHomeBodyProps = {
  viewModel: OperatorHomeViewModel
  activationPeriodBadge: ActivationPeriodBadgeCopy | null
  feedbackState?: "loading" | "loaded" | "error"
  onRetryFeedback?: () => void
  previewBusy?: boolean
  onPreviewGuestForm?: () => void
  onCopySmartGuestLink?: () => void
  feedbackDetails: FeedbackDetailsSnapshot
  onViewFeedback?: (feedbackId: number) => void
  onFeedbackDetailsOpenChange?: (open: boolean) => void
  onRetryFeedbackDetails?: () => void
  onStartClassificationCorrection?: () => void
  onClassificationDraftSentimentChange?: (sentiment: FeedbackSentiment) => void
  onCancelClassificationCorrection?: () => void
  onSaveClassificationCorrection?: () => void
}

/** Home body sections composed from the Operator Home view-model (Figma stack). */
export function OperatorHomeBody({
  viewModel,
  activationPeriodBadge,
  feedbackState = "loaded",
  onRetryFeedback,
  previewBusy = false,
  onPreviewGuestForm,
  onCopySmartGuestLink,
  feedbackDetails,
  onViewFeedback,
  onFeedbackDetailsOpenChange,
  onRetryFeedbackDetails,
  onStartClassificationCorrection,
  onClassificationDraftSentimentChange,
  onCancelClassificationCorrection,
  onSaveClassificationCorrection,
}: OperatorHomeBodyProps) {
  return (
    <div className="flex flex-col gap-5">
      <OperatorHomeHero
        activationPeriodBadge={activationPeriodBadge}
        canPreviewGuestForm={viewModel.canPreviewGuestForm}
        canCopySmartGuestLink={viewModel.canCopySmartGuestLink}
        previewBusy={previewBusy}
        onPreviewGuestForm={onPreviewGuestForm}
        onCopySmartGuestLink={onCopySmartGuestLink}
      />

      <OperatorHomeSetupChecklist
        steps={viewModel.setupSteps}
        onPreviewGuestForm={onPreviewGuestForm}
        previewBusy={previewBusy}
      />

      <OperatorHomePerformanceOverview
        kpis={viewModel.kpis}
        dateRangeLabel={viewModel.dateRangeLabel}
        feedbackLoading={feedbackState === "loading"}
      />

      <OperatorHomeNeedsAttentionSection />

      <OperatorHomeLiveOffersSection />

      <OperatorHomeRecommendedNextStep />

      {feedbackState === "loading" ? (
        <div
          className={`${OPERATOR_HOME_CARD_CLASS} px-6 py-10 text-center`}
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
        <div className={`${OPERATOR_HOME_CARD_CLASS} px-6 py-10 text-center`}>
          <p className="text-sm text-destructive">
            Could not load latest activity. Please try again.
          </p>
          {onRetryFeedback ? (
            <Button
              type="button"
              variant="link"
              size="link-sm"
              className="mt-3 font-medium underline"
              onClick={onRetryFeedback}
            >
              Retry
            </Button>
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
        onStartCorrection={onStartClassificationCorrection}
        onDraftSentimentChange={onClassificationDraftSentimentChange}
        onCancelCorrection={onCancelClassificationCorrection}
        onSaveCorrection={onSaveClassificationCorrection}
      />
    </div>
  )
}

import { FeedbackDetailsDrawer } from "@/components/dashboard/operator/Feedback/FeedbackDetailsDrawer"
import { HomeHero } from "@/components/dashboard/operator/Home/HomeHero"
import { HomeKpiStrip } from "@/components/dashboard/operator/Home/HomeKpiStrip"
import { HomeLatestActivity } from "@/components/dashboard/operator/Home/HomeLatestActivity"
import { HomeLiveOffersSection } from "@/components/dashboard/operator/Home/HomeLiveOffersSection"
import { HomeNeedsAttentionSection } from "@/components/dashboard/operator/Home/HomeNeedsAttentionSection"
import { HomePerformanceDateRangeControl } from "@/components/dashboard/operator/Home/HomePerformanceDateRangeControl"
import { HomeRecommendedNextStep } from "@/components/dashboard/operator/Home/HomeRecommendedNextStep"
import { HomeWeeklyBriefSection } from "@/components/dashboard/operator/Home/HomeWeeklyBriefSection"
import { HomeSetupChecklist } from "@/components/dashboard/operator/Home/HomeSetupChecklist"
import { Button } from "@/components/ui/button"
import type { OperatorHomeRecommendationViewModel } from "@/lib/operatorHome/createOperatorHomePageModule"
import { OPERATOR_HOME_CARD_CLASS } from "@/lib/operatorHome/operatorHomeSectionPresentation"
import type { ActivationPeriodBadgeCopy } from "@/lib/operatorHome/activationPeriod"
import type { FeedbackDetailsSnapshot } from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import {
  PERFORMANCE_HEADER_COPY_CLASS,
  PERFORMANCE_HEADER_ROW_CLASS,
  PERFORMANCE_SECTION_CLASS,
  PERFORMANCE_SUBTITLE_CLASS,
  PERFORMANCE_TITLE_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import type { FeedbackSentiment, FeedbackWorkflowStatus } from "@/types/dashboard"
import type { FeedbackClassificationCorrectionReason } from "@/lib/operatorFeedback/feedbackClassificationCorrectionPresentation"
import type {
  HomeRecommendation,
  OperatorHomeViewModel,
} from "@/types/operatorHome"

type HomeBodyProps = {
  viewModel: OperatorHomeViewModel
  activationPeriodBadge: ActivationPeriodBadgeCopy | null
  selectedDateRange: HomePerformanceDateRange
  onCommitHomePerformanceDateRange: (range: HomePerformanceDateRange) => void
  feedbackState?: "loading" | "loaded" | "error"
  performanceLoading?: boolean
  onRetryFeedback?: () => void
  previewBusy?: boolean
  guestFormPreviewLocationName?: string
  guestFormPreviewAddress?: string
  onPreviewGuestForm?: () => void
  onCreateOffer?: () => void
  onCreateCampaign?: () => void
  onCopySmartGuestLink?: () => void
  recommendation: OperatorHomeRecommendationViewModel
  onRetryRecommendation?: () => void
  onRecommendationPrimaryAction?: (recommendation: HomeRecommendation) => void
  onDismissRecommendation?: () => void
  feedbackDetails: FeedbackDetailsSnapshot
  onViewFeedback?: (feedbackId: number) => void
  onViewGuest?: (locationGuestId: number) => void
  onViewGuestProfile?: (locationGuestId: number) => void
  onFeedbackDetailsOpenChange?: (open: boolean) => void
  onRetryFeedbackDetails?: () => void
  onStartClassificationCorrection?: () => void
  onClassificationDraftSentimentChange?: (sentiment: FeedbackSentiment) => void
  onClassificationDraftReasonChange?: (reason: FeedbackClassificationCorrectionReason) => void
  onClassificationDraftNoteChange?: (value: string) => void
  onCancelClassificationCorrection?: () => void
  onSaveClassificationCorrection?: () => void
  onStartEditTags?: () => void
  onStageEditTag?: (key: string) => void
  onUnstageEditTag?: (key: string) => void
  onEditTagsSentimentChange?: (sentiment: FeedbackSentiment) => void
  onCancelEditTags?: () => void
  onApplyEditTags?: () => void
  onFeedbackWorkflowStatusChange?: (status: FeedbackWorkflowStatus) => void
  onReopenFeedback?: () => void
  onStartFeedbackMarkResolved?: () => void
  onMarkFeedbackNoActionNeeded?: () => void
  onCancelFeedbackCloseOut?: () => void
  onSetFeedbackCloseOutReason?: (
    reason: import("@/lib/operatorFeedback/feedbackCloseOutPresentation").FeedbackCloseOutReason
  ) => void
  onSetFeedbackCloseOutNoteDraft?: (value: string) => void
  onSetFeedbackCloseOutAcknowledged?: (value: boolean) => void
  onConfirmFeedbackCloseOut?: () => void
  onFeedbackInternalNoteDraftChange?: (value: string) => void
  onCreateFeedbackInternalNote?: () => void
  onStartFeedbackNoteEdit?: (noteId: number) => void
  onFeedbackNoteEditDraftChange?: (value: string) => void
  onCancelFeedbackNoteEdit?: () => void
  onSaveFeedbackNoteEdit?: () => Promise<boolean> | void
  onStartFeedbackNoteDelete?: (noteId: number) => void
  onCancelFeedbackNoteDelete?: () => void
  onConfirmFeedbackNoteDelete?: () => void
}

/** Home body sections composed from the Operator Home view-model (Figma stack). */
export function HomeBody({
  viewModel,
  activationPeriodBadge,
  selectedDateRange,
  onCommitHomePerformanceDateRange,
  feedbackState = "loaded",
  performanceLoading = false,
  onRetryFeedback,
  previewBusy = false,
  guestFormPreviewLocationName = "",
  guestFormPreviewAddress = "",
  onPreviewGuestForm,
  onCreateOffer,
  onCreateCampaign,
  onCopySmartGuestLink,
  recommendation,
  onRetryRecommendation,
  onRecommendationPrimaryAction,
  onDismissRecommendation,
  feedbackDetails,
  onViewFeedback,
  onViewGuest,
  onViewGuestProfile,
  onFeedbackDetailsOpenChange,
  onRetryFeedbackDetails,
  onStartClassificationCorrection,
  onClassificationDraftSentimentChange,
  onClassificationDraftReasonChange,
  onClassificationDraftNoteChange,
  onCancelClassificationCorrection,
  onSaveClassificationCorrection,
  onStartEditTags,
  onStageEditTag,
  onUnstageEditTag,
  onEditTagsSentimentChange,
  onCancelEditTags,
  onApplyEditTags,
  onFeedbackWorkflowStatusChange,
  onReopenFeedback,
  onStartFeedbackMarkResolved,
  onMarkFeedbackNoActionNeeded,
  onCancelFeedbackCloseOut,
  onSetFeedbackCloseOutReason,
  onSetFeedbackCloseOutNoteDraft,
  onSetFeedbackCloseOutAcknowledged,
  onConfirmFeedbackCloseOut,
  onFeedbackInternalNoteDraftChange,
  onCreateFeedbackInternalNote,
  onStartFeedbackNoteEdit,
  onFeedbackNoteEditDraftChange,
  onCancelFeedbackNoteEdit,
  onSaveFeedbackNoteEdit,
  onStartFeedbackNoteDelete,
  onCancelFeedbackNoteDelete,
  onConfirmFeedbackNoteDelete,
}: HomeBodyProps) {
  return (
    <div className="flex flex-col gap-5">
      <HomeHero
        activationPeriodBadge={activationPeriodBadge}
        canPreviewGuestForm={viewModel.canPreviewGuestForm}
        canCopySmartGuestLink={viewModel.canCopySmartGuestLink}
        previewBusy={previewBusy}
        guestFormPreviewLocationName={guestFormPreviewLocationName}
        guestFormPreviewAddress={guestFormPreviewAddress}
        onPreviewGuestForm={onPreviewGuestForm}
        onCopySmartGuestLink={onCopySmartGuestLink}
      />

      <HomeSetupChecklist
        steps={viewModel.setupSteps}
        onPreviewGuestForm={onPreviewGuestForm}
        onCreateOffer={onCreateOffer}
        onCreateCampaign={onCreateCampaign}
        previewBusy={previewBusy}
      />

      <section className={PERFORMANCE_SECTION_CLASS}>
        <div className={PERFORMANCE_HEADER_ROW_CLASS}>
          <div className={PERFORMANCE_HEADER_COPY_CLASS}>
            <div className="leading-[0]">
              <h2 className={PERFORMANCE_TITLE_CLASS}>Performance overview</h2>
            </div>
            <div className="leading-[0]">
              <p className={PERFORMANCE_SUBTITLE_CLASS}>
                See how guests are engaging with Guest Loop.
              </p>
            </div>
          </div>

          <HomePerformanceDateRangeControl
            dateRangeLabel={viewModel.dateRangeLabel}
            selectedRange={selectedDateRange}
            onCommitRange={onCommitHomePerformanceDateRange}
          />
        </div>

        <HomeKpiStrip
          kpis={viewModel.kpis}
          feedbackLoading={performanceLoading}
        />
      </section>

      <HomeNeedsAttentionSection />

      <HomeLiveOffersSection />

      <HomeRecommendedNextStep
        recommendation={recommendation}
        locationName={viewModel.selectedLocationName}
        dateRangeLabel={viewModel.dateRangeLabel}
        onRetry={() => {
          onRetryRecommendation?.()
        }}
        onPrimaryAction={(payload) => {
          onRecommendationPrimaryAction?.(payload)
        }}
        onNotNow={() => {
          onDismissRecommendation?.()
        }}
      />

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
        <HomeLatestActivity
          activityByTab={viewModel.activityByTab}
          activityEmpty={viewModel.activityEmpty}
          onViewFeedback={onViewFeedback}
          onViewGuest={onViewGuest}
        />
      )}

      <HomeWeeklyBriefSection />

      <FeedbackDetailsDrawer
        snapshot={feedbackDetails}
        onOpenChange={(open) => {
          onFeedbackDetailsOpenChange?.(open)
        }}
        onRetry={() => {
          onRetryFeedbackDetails?.()
        }}
        onStartCorrection={onStartClassificationCorrection}
        onDraftSentimentChange={onClassificationDraftSentimentChange}
        onDraftReasonChange={onClassificationDraftReasonChange}
        onDraftNoteChange={onClassificationDraftNoteChange}
        onCancelCorrection={onCancelClassificationCorrection}
        onSaveCorrection={onSaveClassificationCorrection}
        onStartEditTags={onStartEditTags}
        onStageEditTag={onStageEditTag}
        onUnstageEditTag={onUnstageEditTag}
        onEditTagsSentimentChange={onEditTagsSentimentChange}
        onCancelEditTags={onCancelEditTags}
        onApplyEditTags={onApplyEditTags}
        onReopen={onReopenFeedback}
        onStartMarkResolved={onStartFeedbackMarkResolved}
        onMarkNoActionNeeded={onMarkFeedbackNoActionNeeded}
        onCancelCloseOut={onCancelFeedbackCloseOut}
        onSetCloseOutReason={onSetFeedbackCloseOutReason}
        onSetCloseOutNoteDraft={onSetFeedbackCloseOutNoteDraft}
        onSetCloseOutAcknowledged={onSetFeedbackCloseOutAcknowledged}
        onConfirmCloseOut={onConfirmFeedbackCloseOut}
        onViewGuestProfile={onViewGuestProfile}
        onNoteDraftChange={onFeedbackInternalNoteDraftChange}
        onCreateNote={onCreateFeedbackInternalNote}
        onStartNoteEdit={onStartFeedbackNoteEdit}
        onNoteEditDraftChange={onFeedbackNoteEditDraftChange}
        onCancelNoteEdit={onCancelFeedbackNoteEdit}
        onSaveNoteEdit={onSaveFeedbackNoteEdit}
        onStartNoteDelete={onStartFeedbackNoteDelete}
        onCancelNoteDelete={onCancelFeedbackNoteDelete}
        onConfirmNoteDelete={onConfirmFeedbackNoteDelete}
      />
    </div>
  )
}


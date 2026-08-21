import { HomeBody } from "@/components/dashboard/operator/Home/HomeBody"
import {
  useDashboardUiStore,
} from "@/components/dashboard/operator/DashboardUiStoreProvider"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { useHomePageModule } from "@/components/dashboard/operator/Home/utils/useHomePageModule"
import type { ActivationPeriodBadgeCopy } from "@/lib/operatorHome/activationPeriod"
import { buildHomeCampaignWizardHandoff } from "@/lib/operatorHome/buildHomeCampaignWizardHandoff"
import { isHomeRecommendationCampaignType } from "@/lib/operatorHome/homeRecommendationPresentation"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import {
  operatorDashboardCampaignDetailsPath,
  operatorDashboardCampaignPreviewPath,
  operatorDashboardGuestProfilePath,
  operatorDashboardNavPath,
  operatorDashboardOfferDetailsPath,
  operatorDashboardOfferPreviewPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { HomeRecommendation } from "@/types/operatorHome"
import { useNavigate, useOutletContext } from "react-router-dom"

type HomePageProps = {
  activationPeriodBadge: ActivationPeriodBadgeCopy | null
}

function openInNewTab(path: string): void {
  window.open(path, "_blank", "noopener,noreferrer")
}

export function HomePage({
  activationPeriodBadge,
}: HomePageProps) {
  const home = useHomePageModule()
  const navigate = useNavigate()
  const { mode, selectedLocationId, locations } =
    useOutletContext<DashboardOutletContext>()
  const homePerformanceDateRange = useDashboardUiStore(
    (state) => state.homePerformanceDateRange
  )
  const setHomePerformanceDateRange = useDashboardUiStore(
    (state) => state.setHomePerformanceDateRange
  )
  const setCampaignsIntent = useDashboardUiStore(
    (state) => state.setCampaignsIntent
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

  const handleRecommendationPrimaryAction = (
    recommendation: HomeRecommendation
  ) => {
    if (isHomeRecommendationCampaignType(recommendation.type)) {
      const selectedLocation = locations.find(
        (location) => location.id === selectedLocationId
      )
      const handoff = buildHomeCampaignWizardHandoff({
        locationId: selectedLocationId,
        locationName: selectedLocation?.locationName ?? "",
        locationAddress: selectedLocation?.address ?? null,
        recommendation,
      })
      if (handoff != null) {
        setCampaignsIntent({
          openFromRecommendation: {
            draftPrefill: handoff.draftPrefill,
          },
        })
      }
      navigate(
        operatorDashboardNavPath(mode, "campaigns", selectedLocationId)
      )
      return
    }

    const action = recommendation.action
    if (action == null) {
      switch (recommendation.type) {
        case "review-open-feedback":
          navigate(
            operatorDashboardNavPath(mode, "feedback", selectedLocationId)
          )
          return
        case "thank-or-follow-guest":
          navigate(
            operatorDashboardNavPath(mode, "guests", selectedLocationId)
          )
          return
        case "promote-or-fix-offer":
          navigate(
            operatorDashboardNavPath(mode, "offers", selectedLocationId)
          )
          return
        default:
          return
      }
    }

    switch (action.kind) {
      case "open-feedback":
        if (action.feedbackId != null) {
          void home.openFeedbackDetails(action.feedbackId)
        } else {
          navigate(
            operatorDashboardNavPath(mode, "feedback", selectedLocationId)
          )
        }
        return
      case "open-guest":
        if (action.locationGuestId != null) {
          navigateToGuestProfile(action.locationGuestId)
        } else {
          navigate(
            operatorDashboardNavPath(mode, "guests", selectedLocationId)
          )
        }
        return
      case "open-offer":
        if (action.offerId != null) {
          navigate(
            operatorDashboardOfferDetailsPath(
              mode,
              action.offerId,
              selectedLocationId
            )
          )
        } else {
          navigate(
            operatorDashboardNavPath(mode, "offers", selectedLocationId)
          )
        }
        return
    }
  }

  const viewModel = home.snapshot.viewModel
  const selectedLocation = locations.find(
    (location) => location.id === selectedLocationId
  )
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

  const locationId = selectedLocationId ?? locations[0]?.id ?? 0

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
        guestFormPreviewLocationName={viewModel.selectedLocationName}
        guestFormPreviewAddress={selectedLocation?.address ?? ""}
        brandName={viewModel.selectedLocationName}
        liveOffersLoadStatus={home.snapshot.liveOffersLoadStatus}
        liveCards={home.snapshot.liveCards}
        liveOffersError={home.snapshot.liveOffersError}
        liveOffersPauseBusy={home.snapshot.liveOffersPauseBusy}
        onLiveOffersEmptyAction={(actionId) => {
          if (actionId === "create-offer") {
            navigate(operatorDashboardNavPath(mode, "offers", locationId))
            return
          }
          navigate(operatorDashboardNavPath(mode, "campaigns", locationId))
        }}
        onRetryLiveOffers={() => {
          void home.retryLiveOffers()
        }}
        onLiveOfferPreview={(card) => {
          if (card.kind === "campaign") {
            openInNewTab(
              operatorDashboardCampaignPreviewPath(mode, card.id, locationId)
            )
            return
          }
          openInNewTab(
            operatorDashboardOfferPreviewPath(mode, card.id, locationId)
          )
        }}
        onViewLiveCampaign={(campaignId) => {
          openInNewTab(
            operatorDashboardCampaignDetailsPath(mode, campaignId, locationId)
          )
        }}
        onViewLiveOffer={(offerId) => {
          openInNewTab(
            operatorDashboardOfferDetailsPath(mode, offerId, locationId)
          )
        }}
        onViewLiveOfferRedemptions={(offerId) => {
          openInNewTab(
            operatorDashboardOfferDetailsPath(mode, offerId, locationId, {
              tab: "redemptions",
            })
          )
        }}
        onPauseLiveCampaign={(campaignId) => home.pauseLiveCampaign(campaignId)}
        onRetryFeedback={() => {
          void home.retryLoad()
        }}
        previewBusy={home.snapshot.previewBusy}
        onPreviewGuestForm={home.previewGuestForm}
        onCreateOffer={() => {
          navigate(
            operatorDashboardNavPath(mode, "offers", selectedLocationId)
          )
        }}
        onCreateCampaign={() => {
          navigate(
            operatorDashboardNavPath(mode, "campaigns", selectedLocationId)
          )
        }}
        onCopySmartGuestLink={home.copySmartGuestLink}
        recommendation={home.snapshot.recommendation}
        onRetryRecommendation={() => {
          void home.retryRecommendation()
        }}
        onRecommendationPrimaryAction={handleRecommendationPrimaryAction}
        onDismissRecommendation={() => {
          home.dismissRecommendation()
        }}
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
        onClassificationDraftReasonChange={(reason) => {
          home.setClassificationDraftReason(reason)
        }}
        onClassificationDraftNoteChange={(value) => {
          home.setClassificationDraftNote(value)
        }}
        onCancelClassificationCorrection={() => {
          home.cancelClassificationCorrection()
        }}
        onSaveClassificationCorrection={() => {
          void home.saveClassificationCorrection()
        }}
        onStartEditTags={() => {
          home.startEditTags()
        }}
        onStageEditTag={(key) => {
          home.stageEditTag(key)
        }}
        onUnstageEditTag={(key) => {
          home.unstageEditTag(key)
        }}
        onEditTagsSentimentChange={(sentiment) => {
          home.setEditTagsSentiment(sentiment)
        }}
        onCancelEditTags={() => {
          home.cancelEditTags()
        }}
        onApplyEditTags={() => {
          void home.applyEditTags()
        }}
        onFeedbackWorkflowStatusChange={(status) => {
          void home.setFeedbackWorkflowStatus(status)
        }}
        onReopenFeedback={() => {
          void home.reopenFeedback()
        }}
        onStartFeedbackMarkResolved={home.startFeedbackMarkResolved}
        onMarkFeedbackNoActionNeeded={() => {
          home.startFeedbackMarkNoActionNeeded()
        }}
        onCancelFeedbackCloseOut={home.cancelFeedbackCloseOut}
        onSetFeedbackCloseOutReason={home.setFeedbackCloseOutReason}
        onSetFeedbackCloseOutNoteDraft={home.setFeedbackCloseOutNoteDraft}
        onSetFeedbackCloseOutAcknowledged={home.setFeedbackCloseOutAcknowledged}
        onConfirmFeedbackCloseOut={() => {
          void home.confirmFeedbackCloseOut()
        }}
        onFeedbackInternalNoteDraftChange={(value) => {
          home.setFeedbackInternalNoteDraft(value)
        }}
        onCreateFeedbackInternalNote={() => {
          void home.createFeedbackInternalNote()
        }}
        onStartFeedbackNoteEdit={home.startFeedbackNoteEdit}
        onFeedbackNoteEditDraftChange={home.setFeedbackNoteEditDraft}
        onCancelFeedbackNoteEdit={home.cancelFeedbackNoteEdit}
        onSaveFeedbackNoteEdit={() => home.saveFeedbackNoteEdit()}
        onStartFeedbackNoteDelete={home.startFeedbackNoteDelete}
        onCancelFeedbackNoteDelete={home.cancelFeedbackNoteDelete}
        onConfirmFeedbackNoteDelete={() => {
          void home.confirmFeedbackNoteDelete()
        }}
      />
    </>
  )
}

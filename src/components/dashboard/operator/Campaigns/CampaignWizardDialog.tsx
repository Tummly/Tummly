import { useEffect } from "react"
import { toast } from "sonner"

import { CampaignAudienceStep } from "@/components/dashboard/operator/Campaigns/CampaignAudienceStep"
import { CampaignChannelStep } from "@/components/dashboard/operator/Campaigns/CampaignChannelStep"
import { CampaignGoalCards } from "@/components/dashboard/operator/Campaigns/CampaignGoalCards"
import { CampaignMessageStep } from "@/components/dashboard/operator/Campaigns/CampaignMessageStep"
import { CampaignOfferStep } from "@/components/dashboard/operator/Campaigns/CampaignOfferStep"
import { CampaignReviewStep } from "@/components/dashboard/operator/Campaigns/CampaignReviewStep"
import { CampaignScheduleStep } from "@/components/dashboard/operator/Campaigns/CampaignScheduleStep"
import { CampaignSendTestEmailDialog } from "@/components/dashboard/operator/Campaigns/CampaignSendTestEmailDialog"
import { RecoverySuccessStatusList } from "@/components/dashboard/operator/Feedback/RecoverySuccessStatusList"
import { OperatorWizardShell } from "@/components/dashboard/operator/OperatorWizardShell"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { Button } from "@/components/ui/button"
import type { CampaignAudienceId } from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import type { CampaignChannelId } from "@/lib/operatorCampaigns/campaignChannelPresentation"
import { CAMPAIGN_COMMIT_COPY } from "@/lib/operatorCampaigns/campaignCommitPresentation"
import { CAMPAIGN_MESSAGE_COPY } from "@/lib/operatorCampaigns/campaignMessagePresentation"
import type { CampaignOfferStanceId } from "@/lib/operatorCampaigns/campaignOfferPresentation"
import { CREATE_EDIT_OFFER_DRAWER_COPY } from "@/lib/operatorOffers/createEditOfferDrawerPresentation"
import type { CampaignCatalogOfferDetailsDraft } from "@/lib/operatorOffers/offerCatalogPresentation"
import type { CampaignScheduleModeId } from "@/lib/operatorCampaigns/campaignSchedulePresentation"
import { CAMPAIGN_SEND_TEST_COPY } from "@/lib/operatorCampaigns/campaignSendTestPresentation"
import {
  CAMPAIGN_WIZARD_COPY,
  type CampaignGoalId,
} from "@/lib/operatorCampaigns/campaignWizardPresentation"
import type { CampaignWizardSnapshot } from "@/lib/operatorCampaigns/createCampaignWizardModule"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"

type CampaignWizardDialogProps = {
  snapshot: CampaignWizardSnapshot
  dashboardMode: OperatorDashboardMode
  onRequestClose: () => void
  onSaveAndExit: () => void
  onBack: () => void
  onSelectGoal: (goalId: CampaignGoalId) => void
  onSelectAudience: (audienceId: CampaignAudienceId) => void
  onSelectChannel: (channelId: CampaignChannelId) => void
  onSelectOfferStance: (stanceId: CampaignOfferStanceId) => void
  onCloseCreateOfferPanel: () => void
  onEditAttachedOffer: () => void
  onPatchCreateOfferDraft: (
    patch: Partial<CampaignCatalogOfferDetailsDraft>
  ) => void
  onConfirmCreateOffer: () => void
  onExistingOfferSearchChange: (query: string) => void
  onSelectExistingOffer: (offerId: number) => void
  onRetryExistingOfferPicker: () => void
  onCreateNewOfferFromPicker: () => void
  onConfirmPendingEditOfferSave: () => void
  onCancelPendingEditOfferSave: () => void
  onSelectScheduleMode: (modeId: CampaignScheduleModeId) => void
  onScheduleDateChange: (value: string) => void
  onScheduleTimeChange: (value: string) => void
  onWriteManually: () => void
  onPrepareDraft: () => void
  onRewriteSubject: () => void
  onRewriteMessage: () => void
  onRetryAiDraft: () => void
  onDismissPreparingOverlay: () => void
  onRetryMessagingBalances?: () => void
  onSubjectChange: (value: string) => void
  onMessageChange: (value: string) => void
  onOpenGuestPreview: () => void
  onCloseGuestPreview: () => void
  onEditMessageFromReview: () => void
  onOpenSendTest: () => void
  onCloseSendTest: () => void
  onSendTestEmailChange: (value: string) => void
  onConfirmSendTest: () => void
  onContinue: () => void
  onCancelCommitConfirm: () => void
  onConfirmCommit: () => void
  onDismissSuccess: () => void
  onBrowseTemplates: () => void
  onBuyCredits?: () => void
  onChangePlan?: () => void
  onLockHelper?: () => void
}

/**
 * Campaign create wizard — Operator wizard shell chrome + Campaign-owned step bodies.
 * Audience (23); Channel (24/25); Offer (25+22+30); Message (26+33+25); Send test (24);
 * Schedule + Review commit (26).
 */
export function CampaignWizardDialog({
  snapshot,
  dashboardMode,
  onRequestClose,
  onSaveAndExit,
  onBack,
  onSelectGoal,
  onSelectAudience,
  onSelectChannel,
  onSelectOfferStance,
  onCloseCreateOfferPanel,
  onEditAttachedOffer,
  onPatchCreateOfferDraft,
  onConfirmCreateOffer,
  onExistingOfferSearchChange,
  onSelectExistingOffer,
  onRetryExistingOfferPicker,
  onCreateNewOfferFromPicker,
  onConfirmPendingEditOfferSave,
  onCancelPendingEditOfferSave,
  onSelectScheduleMode,
  onScheduleDateChange,
  onScheduleTimeChange,
  onWriteManually,
  onPrepareDraft,
  onRewriteSubject,
  onRewriteMessage,
  onRetryAiDraft,
  onDismissPreparingOverlay,
  onRetryMessagingBalances,
  onSubjectChange,
  onMessageChange,
  onOpenGuestPreview,
  onCloseGuestPreview,
  onEditMessageFromReview,
  onOpenSendTest,
  onCloseSendTest,
  onSendTestEmailChange,
  onConfirmSendTest,
  onContinue,
  onCancelCommitConfirm,
  onConfirmCommit,
  onDismissSuccess,
  onBrowseTemplates,
  onBuyCredits,
  onChangePlan,
  onLockHelper,
}: CampaignWizardDialogProps) {
  const isSuccess = snapshot.stepId === "success" && snapshot.success != null
  const isGoal = snapshot.stepId === "goal"
  const isAudience = snapshot.stepId === "audience" && snapshot.audience != null
  const isChannel = snapshot.stepId === "channel" && snapshot.channel != null
  const isOffer = snapshot.stepId === "offer" && snapshot.offer != null
  const isMessage = snapshot.stepId === "message" && snapshot.message != null
  const isSchedule = snapshot.stepId === "schedule" && snapshot.schedule != null
  const isReview = snapshot.stepId === "review" && snapshot.review != null
  const message = snapshot.message
  const sendTest = snapshot.sendTest
  const commitConfirm = snapshot.commitConfirm
  const commitBusy = commitConfirm?.busy === true
  const aiRunning = message?.aiDraftStatus === "running"
  const sendTestBusy = sendTest?.status === "sending"
  const pendingEditOfferSave = snapshot.offer?.pendingEditOfferSave ?? null

  useEffect(() => {
    if (
      message != null
      && message.aiDraftStatus === "failed"
      && message.aiDraftError != null
    ) {
      toast.error(message.aiDraftError)
    }
  }, [message?.aiDraftStatus, message?.aiDraftError])

  useEffect(() => {
    if (snapshot.saveStatus === "error" && snapshot.saveError != null) {
      toast.error(snapshot.saveError)
    }
  }, [snapshot.saveStatus, snapshot.saveError])

  useEffect(() => {
    if (sendTest?.status === "error" && sendTest.error != null) {
      toast.error(sendTest.error)
    }
    if (sendTest?.status === "success") {
      toast.success(CAMPAIGN_SEND_TEST_COPY.successToast)
    }
  }, [sendTest?.status, sendTest?.error])

  return (
    <>
      <OperatorWizardShell
        isOpen={snapshot.isOpen}
        onRequestClose={isSuccess ? onDismissSuccess : onRequestClose}
        closeDisabled={Boolean(aiRunning) || commitBusy}
        showBackButton={!isSuccess}
        onBack={onBack}
        backDisabled={Boolean(aiRunning) || commitBusy}
        title={snapshot.pageTitle}
        description={snapshot.headerSubtitle}
        stepHeading={snapshot.stepHeading}
        stepDescription={snapshot.stepDescription}
        steps={snapshot.showNumberedStepper ? snapshot.numberedSteps : null}
        activeStepIndex={snapshot.activeNumberedStepIndex}
        isLoading={false}
        footerLayout={snapshot.footerLayout}
        preparingOverlay={
          message != null
            ? {
                open: message.preparingOverlayOpen,
                onDismiss: onDismissPreparingOverlay,
                onWriteManually,
                subtitle: snapshot.headerSubtitle,
                title: CAMPAIGN_MESSAGE_COPY.preparingOverlayTitle,
                description: CAMPAIGN_MESSAGE_COPY.preparingOverlayDescription,
              }
            : null
        }
        confirmDialog={
          commitConfirm != null
            ? {
                open: commitConfirm.open,
                busy: commitConfirm.busy,
                onCancel: onCancelCommitConfirm,
                onConfirm: onConfirmCommit,
                title: commitConfirm.title,
                description: commitConfirm.description,
                error: commitConfirm.error,
                cancelLabel: commitConfirm.cancelLabel,
                confirmLabel: commitConfirm.confirmLabel,
                confirmBusyLabel: commitConfirm.confirmBusyLabel,
              }
            : null
        }
        onSaveAndExit={isSuccess ? undefined : onSaveAndExit}
        saveAndExitDisabled={
          snapshot.saveStatus === "saving"
          || Boolean(aiRunning)
          || commitBusy
        }
        lastSavedAt={snapshot.lastSavedAt}
        footer={
          isSuccess ? (
            <Button
              type="button"
              variant="op-primary"
              onClick={onDismissSuccess}
            >
              {CAMPAIGN_COMMIT_COPY.successDoneLabel}
            </Button>
          ) : (
            <div className="flex flex-col items-end gap-2">
              {snapshot.review?.sendBlockedReason != null ? (
                <p className="m-0 max-w-[360px] text-right text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
                  {snapshot.review.sendBlockedReason}
                </p>
              ) : null}
              {snapshot.review?.channelShortfall != null ? (
                <div className="flex flex-wrap items-center justify-end gap-3">
                  {snapshot.review.channelShortfall.buyCreditsLabel != null ? (
                    <Button
                      type="button"
                      variant="op-link"
                      className="h-auto min-h-0 w-fit p-0"
                      onClick={onBuyCredits}
                    >
                      {snapshot.review.channelShortfall.buyCreditsLabel}
                    </Button>
                  ) : null}
                  {snapshot.review.channelShortfall.changePlanLabel != null ? (
                    <Button
                      type="button"
                      variant="op-link"
                      className="h-auto min-h-0 w-fit p-0"
                      onClick={onChangePlan}
                    >
                      {snapshot.review.channelShortfall.changePlanLabel}
                    </Button>
                  ) : null}
                </div>
              ) : null}
              <div className="flex flex-wrap items-center justify-end gap-3">
                {snapshot.lockHelper != null ? (
                  <Button
                    type="button"
                    variant="op-link"
                    className="h-auto min-h-0 w-fit p-0"
                    onClick={onLockHelper}
                  >
                    {snapshot.lockHelper.label}
                  </Button>
                ) : null}
                <Button
                  type="button"
                  variant="op-primary"
                  disabled={
                    Boolean(aiRunning)
                    || commitBusy
                    || (snapshot.review != null
                      ? !snapshot.review.sendAvailable
                      : !snapshot.canContinue)
                  }
                  onClick={onContinue}
                >
                  {snapshot.primaryActionLabel}
                </Button>
              </div>
            </div>
          )
        }
      >
        {isSuccess && snapshot.success != null ? (
          <RecoverySuccessStatusList rows={snapshot.success.rows} />
        ) : isGoal ? (
          <div className="flex flex-col gap-[42px]">
            <div className="flex flex-wrap items-center gap-3">
              <p className="m-0 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
                {CAMPAIGN_WIZARD_COPY.preferTemplatePrompt}
              </p>
              <Button
                type="button"
                variant="op-link"
                onClick={onBrowseTemplates}
              >
                {CAMPAIGN_WIZARD_COPY.browseTemplates}
              </Button>
            </div>
            <CampaignGoalCards
              goals={snapshot.goals}
              onSelectGoal={onSelectGoal}
            />
          </div>
        ) : isAudience ? (
          <CampaignAudienceStep
            audience={snapshot.audience!}
            onSelectAudience={onSelectAudience}
          />
        ) : isChannel ? (
          <CampaignChannelStep
            channel={snapshot.channel!}
            onSelectChannel={onSelectChannel}
            onRetryMessagingBalances={onRetryMessagingBalances}
            onBuyCredits={onBuyCredits}
            onChangePlan={onChangePlan}
          />
        ) : isOffer ? (
          <CampaignOfferStep
            offer={snapshot.offer!}
            dashboardMode={dashboardMode}
            locationId={snapshot.locationId}
            onSelectStance={onSelectOfferStance}
            onCloseCreatePanel={onCloseCreateOfferPanel}
            onEditAttachedOffer={onEditAttachedOffer}
            onPatchCreateOfferDraft={onPatchCreateOfferDraft}
            onConfirmCreateOffer={onConfirmCreateOffer}
            onExistingOfferSearchChange={onExistingOfferSearchChange}
            onSelectExistingOffer={onSelectExistingOffer}
            onRetryExistingOfferPicker={onRetryExistingOfferPicker}
            onCreateNewOfferFromPicker={onCreateNewOfferFromPicker}
            onBuyCredits={onBuyCredits}
            onChangePlan={onChangePlan}
          />
        ) : isMessage ? (
          <CampaignMessageStep
            message={snapshot.message!}
            onPrepareDraft={onPrepareDraft}
            onWriteManually={onWriteManually}
            onSubjectChange={onSubjectChange}
            onBodyChange={onMessageChange}
            onRewriteSubject={onRewriteSubject}
            onRewriteMessage={onRewriteMessage}
            onRetryAiDraft={onRetryAiDraft}
            onOpenGuestPreview={onOpenGuestPreview}
            onCloseGuestPreview={onCloseGuestPreview}
            onSendTest={onOpenSendTest}
            sendTestBusy={Boolean(sendTestBusy)}
            onBuyCredits={onBuyCredits}
            onChangePlan={onChangePlan}
          />
        ) : isSchedule ? (
          <CampaignScheduleStep
            schedule={snapshot.schedule!}
            onSelectMode={onSelectScheduleMode}
            onScheduleDateChange={onScheduleDateChange}
            onScheduleTimeChange={onScheduleTimeChange}
            onBuyCredits={onBuyCredits}
            onChangePlan={onChangePlan}
          />
        ) : isReview ? (
          <CampaignReviewStep
            review={snapshot.review!}
            onOpenGuestPreview={onOpenGuestPreview}
            onCloseGuestPreview={onCloseGuestPreview}
            onEditMessage={onEditMessageFromReview}
            onSendTest={onOpenSendTest}
            sendTestBusy={Boolean(sendTestBusy)}
          />
        ) : (
          <p className="m-0 text-sm font-medium text-[var(--op-color-gray-550)]">
            {snapshot.placeholderBody}
          </p>
        )}
      </OperatorWizardShell>
      {sendTest != null ? (
        <CampaignSendTestEmailDialog
          sendTest={sendTest}
          onOpenChange={(open) => {
            if (!open) {
              onCloseSendTest()
            }
          }}
          onEmailChange={onSendTestEmailChange}
          onConfirm={onConfirmSendTest}
        />
      ) : null}
      <AlertDialog
        open={pendingEditOfferSave != null}
        onOpenChange={(open) => {
          if (!open) {
            onCancelPendingEditOfferSave()
          }
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {pendingEditOfferSave?.title
                ?? CREATE_EDIT_OFFER_DRAWER_COPY.editSaveConfirmTitle}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {pendingEditOfferSave?.description
                ?? CREATE_EDIT_OFFER_DRAWER_COPY.editSaveConfirmDescription}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>
              {CREATE_EDIT_OFFER_DRAWER_COPY.cancel}
            </AlertDialogCancel>
            <AlertDialogAction onClick={onConfirmPendingEditOfferSave}>
              {CREATE_EDIT_OFFER_DRAWER_COPY.editConfirm}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}

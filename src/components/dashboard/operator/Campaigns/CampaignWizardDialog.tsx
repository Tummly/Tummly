import { CampaignAudienceStep } from "@/components/dashboard/operator/Campaigns/CampaignAudienceStep"
import { CampaignChannelStep } from "@/components/dashboard/operator/Campaigns/CampaignChannelStep"
import { CampaignGoalCards } from "@/components/dashboard/operator/Campaigns/CampaignGoalCards"
import { CampaignMessageStep } from "@/components/dashboard/operator/Campaigns/CampaignMessageStep"
import { CampaignOfferStep } from "@/components/dashboard/operator/Campaigns/CampaignOfferStep"
import { CampaignReviewStep } from "@/components/dashboard/operator/Campaigns/CampaignReviewStep"
import { CampaignScheduleStep } from "@/components/dashboard/operator/Campaigns/CampaignScheduleStep"
import { RecoveryWizardShell } from "@/components/dashboard/operator/Feedback/RecoveryWizardShell"
import { Button } from "@/components/ui/button"
import type { CampaignAudienceId } from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import type { CampaignChannelId } from "@/lib/operatorCampaigns/campaignChannelPresentation"
import type { CampaignOfferStanceId } from "@/lib/operatorCampaigns/campaignOfferPresentation"
import type { CampaignScheduleModeId } from "@/lib/operatorCampaigns/campaignSchedulePresentation"
import {
  CAMPAIGN_WIZARD_COPY,
  type CampaignGoalId,
} from "@/lib/operatorCampaigns/campaignWizardPresentation"
import type { CampaignWizardSnapshot } from "@/lib/operatorCampaigns/createCampaignWizardModule"

type CampaignWizardDialogProps = {
  snapshot: CampaignWizardSnapshot
  onRequestClose: () => void
  onSaveAndExit: () => void
  onBack: () => void
  onSelectGoal: (goalId: CampaignGoalId) => void
  onSelectAudience: (audienceId: CampaignAudienceId) => void
  onSelectSavedGroup: (savedGroupId: string | null) => void
  onSelectChannel: (channelId: CampaignChannelId) => void
  onSelectOfferStance: (stanceId: CampaignOfferStanceId) => void
  onSelectScheduleMode: (modeId: CampaignScheduleModeId) => void
  onWriteManually: () => void
  onPrepareDraftStub: () => void
  onSubjectChange: (value: string) => void
  onMessageChange: (value: string) => void
  onOpenGuestPreview: () => void
  onCloseGuestPreview: () => void
  onEditMessageFromReview: () => void
  onContinue: () => void
  onBrowseTemplates: () => void
}

/**
 * Campaign create wizard — RecoveryWizardShell chrome + Campaign-owned step bodies.
 * Audience (23); Channel (24); Offer (25); Message (26); Schedule + Review (27, no send).
 */
export function CampaignWizardDialog({
  snapshot,
  onRequestClose,
  onSaveAndExit,
  onBack,
  onSelectGoal,
  onSelectAudience,
  onSelectSavedGroup,
  onSelectChannel,
  onSelectOfferStance,
  onSelectScheduleMode,
  onWriteManually,
  onPrepareDraftStub,
  onSubjectChange,
  onMessageChange,
  onOpenGuestPreview,
  onCloseGuestPreview,
  onEditMessageFromReview,
  onContinue,
  onBrowseTemplates,
}: CampaignWizardDialogProps) {
  const isGoal = snapshot.stepId === "goal"
  const isAudience = snapshot.stepId === "audience" && snapshot.audience != null
  const isChannel = snapshot.stepId === "channel" && snapshot.channel != null
  const isOffer = snapshot.stepId === "offer" && snapshot.offer != null
  const isMessage = snapshot.stepId === "message" && snapshot.message != null
  const isSchedule = snapshot.stepId === "schedule" && snapshot.schedule != null
  const isReview = snapshot.stepId === "review" && snapshot.review != null

  return (
    <RecoveryWizardShell
      isOpen={snapshot.isOpen}
      onRequestClose={onRequestClose}
      showBackButton
      onBack={onBack}
      title={snapshot.pageTitle}
      description={snapshot.headerSubtitle}
      stepHeading={snapshot.stepHeading}
      stepDescription={snapshot.stepDescription}
      steps={snapshot.showNumberedStepper ? snapshot.numberedSteps : null}
      activeStepIndex={snapshot.activeNumberedStepIndex}
      isLoading={false}
      preparingOverlay={null}
      onSaveAndExit={onSaveAndExit}
      saveAndExitDisabled={snapshot.saveStatus === "saving"}
      lastSavedAt={snapshot.lastSavedAt}
      footer={
        <Button
          type="button"
          variant="op-primary"
          disabled={
            snapshot.review != null
              ? !snapshot.review.sendAvailable
              : !snapshot.canContinue
          }
          onClick={onContinue}
        >
          {snapshot.primaryActionLabel}
        </Button>
      }
      confirmDialog={{
        open: false,
        busy: false,
        onCancel: () => {},
        onConfirm: () => {},
        title: "",
        description: "",
        confirmLabel: "",
        confirmBusyLabel: "",
      }}
    >
      {isGoal ? (
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
          onSelectSavedGroup={onSelectSavedGroup}
        />
      ) : isChannel ? (
        <CampaignChannelStep
          channel={snapshot.channel!}
          onSelectChannel={onSelectChannel}
        />
      ) : isOffer ? (
        <CampaignOfferStep
          offer={snapshot.offer!}
          onSelectStance={onSelectOfferStance}
        />
      ) : isMessage ? (
        <CampaignMessageStep
          message={snapshot.message!}
          onPrepareDraft={onPrepareDraftStub}
          onWriteManually={onWriteManually}
          onSubjectChange={onSubjectChange}
          onBodyChange={onMessageChange}
          onOpenGuestPreview={onOpenGuestPreview}
          onCloseGuestPreview={onCloseGuestPreview}
        />
      ) : isSchedule ? (
        <CampaignScheduleStep
          schedule={snapshot.schedule!}
          onSelectMode={onSelectScheduleMode}
        />
      ) : isReview ? (
        <CampaignReviewStep
          review={snapshot.review!}
          onOpenGuestPreview={onOpenGuestPreview}
          onCloseGuestPreview={onCloseGuestPreview}
          onEditMessage={onEditMessageFromReview}
        />
      ) : (
        <p className="m-0 text-sm font-medium text-[var(--op-color-gray-550)]">
          {snapshot.placeholderBody}
        </p>
      )}
    </RecoveryWizardShell>
  )
}

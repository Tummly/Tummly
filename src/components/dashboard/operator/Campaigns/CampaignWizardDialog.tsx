import { CampaignAudienceStep } from "@/components/dashboard/operator/Campaigns/CampaignAudienceStep"
import { CampaignChannelStep } from "@/components/dashboard/operator/Campaigns/CampaignChannelStep"
import { CampaignGoalCards } from "@/components/dashboard/operator/Campaigns/CampaignGoalCards"
import { CampaignMessageStep } from "@/components/dashboard/operator/Campaigns/CampaignMessageStep"
import { CampaignOfferStep } from "@/components/dashboard/operator/Campaigns/CampaignOfferStep"
import { RecoveryWizardShell } from "@/components/dashboard/operator/Feedback/RecoveryWizardShell"
import { Button } from "@/components/ui/button"
import type { CampaignAudienceId } from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import type { CampaignChannelId } from "@/lib/operatorCampaigns/campaignChannelPresentation"
import type { CampaignOfferStanceId } from "@/lib/operatorCampaigns/campaignOfferPresentation"
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
  onWriteManually: () => void
  onPrepareDraftStub: () => void
  onSubjectChange: (value: string) => void
  onMessageChange: (value: string) => void
  onOpenGuestPreview: () => void
  onCloseGuestPreview: () => void
  onContinue: () => void
  onBrowseTemplates: () => void
}

/**
 * Campaign create wizard — RecoveryWizardShell chrome + Campaign-owned step bodies.
 * Audience (23); Channel (24); Offer (25); Message (26); Schedule–Review remain placeholders.
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
  onWriteManually,
  onPrepareDraftStub,
  onSubjectChange,
  onMessageChange,
  onOpenGuestPreview,
  onCloseGuestPreview,
  onContinue,
  onBrowseTemplates,
}: CampaignWizardDialogProps) {
  const isGoal = snapshot.stepId === "goal"
  const isAudience = snapshot.stepId === "audience" && snapshot.audience != null
  const isChannel = snapshot.stepId === "channel" && snapshot.channel != null
  const isOffer = snapshot.stepId === "offer" && snapshot.offer != null
  const isMessage = snapshot.stepId === "message" && snapshot.message != null

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
      footer={
        <Button
          type="button"
          variant="op-primary"
          disabled={!snapshot.canContinue}
          onClick={onContinue}
        >
          {CAMPAIGN_WIZARD_COPY.continue}
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
      ) : (
        <p className="m-0 text-sm font-medium text-[var(--op-color-gray-550)]">
          {snapshot.placeholderBody}
        </p>
      )}
    </RecoveryWizardShell>
  )
}

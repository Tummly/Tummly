import { CampaignAudienceStep } from "@/components/dashboard/operator/Campaigns/CampaignAudienceStep"
import { CampaignGoalCards } from "@/components/dashboard/operator/Campaigns/CampaignGoalCards"
import { RecoveryWizardShell } from "@/components/dashboard/operator/Feedback/RecoveryWizardShell"
import { Button } from "@/components/ui/button"
import type { CampaignAudienceId } from "@/lib/operatorCampaigns/campaignAudiencePresentation"
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
  onContinue: () => void
  onBrowseTemplates: () => void
}

/**
 * Campaign create wizard — RecoveryWizardShell chrome + Campaign-owned step bodies.
 * Audience (ticket 23); Channel–Review remain placeholders until later tickets.
 */
export function CampaignWizardDialog({
  snapshot,
  onRequestClose,
  onSaveAndExit,
  onBack,
  onSelectGoal,
  onSelectAudience,
  onSelectSavedGroup,
  onContinue,
  onBrowseTemplates,
}: CampaignWizardDialogProps) {
  const isGoal = snapshot.stepId === "goal"
  const isAudience = snapshot.stepId === "audience" && snapshot.audience != null

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
      ) : (
        <p className="m-0 text-sm font-medium text-[var(--op-color-gray-550)]">
          {snapshot.placeholderBody}
        </p>
      )}
    </RecoveryWizardShell>
  )
}

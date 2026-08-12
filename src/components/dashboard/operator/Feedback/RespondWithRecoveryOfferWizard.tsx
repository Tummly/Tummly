import { Loader2Icon } from "lucide-react"
import { toast } from "sonner"
import { useEffect } from "react"

import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { GuestPreviewOfferCoupon } from "@/components/dashboard/operator/Feedback/GuestPreviewOfferCoupon"
import { GuestPreviewPanel } from "@/components/dashboard/operator/Feedback/GuestPreviewPanel"
import { GuestResponseChooser } from "@/components/dashboard/operator/Feedback/GuestResponseChooser"
import { GuestResponseWriteFields } from "@/components/dashboard/operator/Feedback/GuestResponseWriteFields"
import { RecoveryFeedbackSummaryPanel } from "@/components/dashboard/operator/Feedback/RecoveryFeedbackSummaryPanel"
import { RecoveryOfferStep } from "@/components/dashboard/operator/Feedback/RecoveryOfferStep"
import { RecoveryReviewSummary } from "@/components/dashboard/operator/Feedback/RecoveryReviewSummary"
import { RecoverySuccessStatusList } from "@/components/dashboard/operator/Feedback/RecoverySuccessStatusList"
import { OperatorWizardShell } from "@/components/dashboard/operator/OperatorWizardShell"
import { ResponseSetupFields } from "@/components/dashboard/operator/Feedback/ResponseSetupFields"
import type { PrepareRecoveryDraftRewriteTarget } from "@/lib/operatorFeedback/createRespondToGuestModule"
import type { RespondWithRecoveryOfferSnapshot } from "@/lib/operatorFeedback/createRespondWithRecoveryOfferModule"
import {
  GUEST_RESPONSE_PREPARING_OVERLAY_DESCRIPTION,
  GUEST_RESPONSE_PREPARING_OVERLAY_TITLE,
  GUEST_RESPONSE_STEP_DESCRIPTION,
  GUEST_RESPONSE_STEP_HEADING,
} from "@/lib/operatorFeedback/guestResponseChooserPresentation"
import {
  GUEST_PREVIEW_SEND_TEST_SUCCESS,
  buildGuestPreviewOfferCoupon,
} from "@/lib/operatorFeedback/guestPreviewPresentation"
import {
  labelForRecoveryOfferType,
  labelForRecoveryOfferValidity,
  toConfirmedRecoveryOfferPayload,
  type RecoveryOfferStanceId,
} from "@/lib/operatorFeedback/recoveryOfferPresentation"
import { recoverySendConfirmCopy } from "@/lib/operatorFeedback/recoverySendConfirmPresentation"
import { recoverySuccessChromeForRespondWithRecoveryOffer } from "@/lib/operatorFeedback/recoverySuccessPresentation"
import { RECOVERY_WIZARD_PAGE_TITLE } from "@/lib/operatorFeedback/recoveryWizardChromePresentation"
import {
  RESPONSE_SETUP_STEP_DESCRIPTION,
  RESPONSE_SETUP_STEP_HEADING,
} from "@/lib/operatorFeedback/responseSetupPresentation"
import {
  REVIEW_RESPONSE_STEP_DESCRIPTION,
  REVIEW_RESPONSE_STEP_HEADING,
} from "@/lib/operatorFeedback/reviewResponsePresentation"
import {
  type RespondToGuestChannel,
  type RespondToGuestToneId,
} from "@/lib/operatorFeedback/respondToGuestPresentation"
import type { CampaignCatalogOfferDetailsDraft } from "@/lib/operatorOffers/offerCatalogPresentation"

type RespondWithRecoveryOfferWizardProps = {
  snapshot: RespondWithRecoveryOfferSnapshot
  onSaveAndExit: () => void
  onBack: () => void
  onChannelChange: (channel: RespondToGuestChannel) => void
  onToneChange: (tone: RespondToGuestToneId) => void
  onIncludeNotesChange: (value: string) => void
  onContinueSetup: () => void
  onSelectOfferStance: (stanceId: RecoveryOfferStanceId) => void
  onCloseCreateOfferPanel: () => void
  onEditAttachedOffer: () => void
  onPatchCreateOfferDraft: (
    patch: Partial<CampaignCatalogOfferDetailsDraft>
  ) => void
  onConfirmCreateOffer: () => void
  onExistingOfferSearchChange: (query: string) => void
  onSelectExistingOffer: (offerId: number) => void
  onRetryExistingOfferPicker: () => void
  onContinueOffer: () => void
  onEditOffer: () => void
  onWriteManually: () => void
  onPrepareDraft: () => void
  onRewriteDraft: (target: PrepareRecoveryDraftRewriteTarget) => void
  onRetryAiDraft: () => void
  onDismissPreparingOverlay: () => void
  onSubjectChange: (value: string) => void
  onMessageChange: (value: string) => void
  onContinueWrite: () => void
  onEditText: () => void
  onOpenGuestPreview: () => void
  onCloseGuestPreview: () => void
  onSendGuestPreviewTest: () => void
  onOpenSendConfirm: () => void
  onCancelSendConfirm: () => void
  onConfirmSend: () => void
  onKeepInProgress: () => void
  onMarkResolved: () => void
}

const STEP_LABELS = [
  { id: "action", label: "Action" },
  { id: "setup", label: "Response setup" },
  { id: "offer", label: "Offer details" },
  { id: "write", label: "Guest response" },
  { id: "review", label: "Review and send" },
] as const

function stepIndex(step: RespondWithRecoveryOfferSnapshot["step"]): number {
  if (step === "setup") return 1
  if (step === "offer") return 2
  if (step === "write") return 3
  if (step === "review" || step === "success") return 4
  return 1
}

function OfferSummaryCard({
  snapshot,
  onEditOffer,
}: {
  snapshot: RespondWithRecoveryOfferSnapshot
  onEditOffer: () => void
}) {
  const offer = snapshot.offer
  return (
    <div className="flex flex-col gap-3 rounded-[6px] border border-op-card-border bg-op-background-secondary p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-xs font-medium text-op-text-muted">
            Recovery offer
          </p>
          <p className="mt-1 text-sm font-semibold text-op-text-primary">
            {offer.title.trim() || labelForRecoveryOfferType(offer.offerType)}
          </p>
          <p className="mt-1 text-xs text-op-text-muted">
            {labelForRecoveryOfferValidity(offer.validity)}
            {offer.validity === "choose_expiry_date" && offer.expiryDate
              ? ` · ${offer.expiryDate}`
              : null}
          </p>
        </div>
        <Button
          type="button"
          variant="op-secondary"
          size="sm"
          disabled={snapshot.actionsLocked}
          onClick={onEditOffer}
        >
          Edit offer
        </Button>
      </div>
    </div>
  )
}

/** Full-screen Respond with a recovery offer wizard. */
export function RespondWithRecoveryOfferWizard({
  snapshot,
  onSaveAndExit,
  onBack,
  onChannelChange,
  onToneChange,
  onIncludeNotesChange,
  onContinueSetup,
  onSelectOfferStance,
  onCloseCreateOfferPanel,
  onEditAttachedOffer,
  onPatchCreateOfferDraft,
  onConfirmCreateOffer,
  onExistingOfferSearchChange,
  onSelectExistingOffer,
  onRetryExistingOfferPicker,
  onContinueOffer,
  onEditOffer,
  onWriteManually,
  onPrepareDraft,
  onRewriteDraft,
  onRetryAiDraft,
  onDismissPreparingOverlay,
  onSubjectChange,
  onMessageChange,
  onContinueWrite,
  onEditText,
  onOpenGuestPreview,
  onCloseGuestPreview,
  onSendGuestPreviewTest,
  onOpenSendConfirm,
  onCancelSendConfirm,
  onConfirmSend,
  onKeepInProgress,
  onMarkResolved,
}: RespondWithRecoveryOfferWizardProps) {
  useEffect(() => {
    if (snapshot.sendStatus === "error" && snapshot.sendError != null) {
      toast.error(snapshot.sendError)
    }
  }, [snapshot.sendStatus, snapshot.sendError])

  useEffect(() => {
    if (
      snapshot.completeStatus === "error"
      && snapshot.completeError != null
    ) {
      toast.error(snapshot.completeError)
    }
  }, [snapshot.completeStatus, snapshot.completeError])

  useEffect(() => {
    if (snapshot.aiDraftStatus === "failed" && snapshot.aiDraftError != null) {
      toast.error(snapshot.aiDraftError)
    }
  }, [snapshot.aiDraftStatus, snapshot.aiDraftError])

  useEffect(() => {
    if (
      snapshot.offerDescriptionAiStatus === "failed"
      && snapshot.offerDescriptionAiError != null
    ) {
      toast.error(snapshot.offerDescriptionAiError)
    }
  }, [snapshot.offerDescriptionAiStatus, snapshot.offerDescriptionAiError])

  useEffect(() => {
    if (snapshot.sendTestStatus === "error" && snapshot.sendTestError != null) {
      toast.error(snapshot.sendTestError)
    }
    if (snapshot.sendTestStatus === "success") {
      toast.success(GUEST_PREVIEW_SEND_TEST_SUCCESS)
    }
  }, [snapshot.sendTestStatus, snapshot.sendTestError])

  const activeStep = stepIndex(snapshot.step)
  const isSuccess = snapshot.step === "success"
  const sending = snapshot.sendStatus === "saving"
  const completing = snapshot.completeStatus === "saving"
  const locked = snapshot.actionsLocked
  const sendConfirm = recoverySendConfirmCopy({
    intent: "respond_with_recovery_offer",
    maskedDestination: snapshot.maskedDestination,
    sendStatus: snapshot.sendStatus,
  })
  const successChrome = isSuccess
    ? recoverySuccessChromeForRespondWithRecoveryOffer({
        maskedDestination: snapshot.maskedDestination,
        offerTitle: snapshot.issuedOffer?.title ?? null,
        expiryAt: snapshot.issuedOffer?.expiryAt ?? null,
        claimCode: snapshot.issuedOffer?.redemptionCode ?? null,
      })
    : null
  const onWriteStep = snapshot.step === "write"
  const onWriteEditor =
    snapshot.step === "write" && snapshot.writeEntry === "editor"

  const stepHeading = isSuccess
    ? null
    : snapshot.step === "setup"
      ? RESPONSE_SETUP_STEP_HEADING
      : snapshot.step === "offer"
        ? "Offer details"
        : snapshot.step === "write"
          ? GUEST_RESPONSE_STEP_HEADING
          : REVIEW_RESPONSE_STEP_HEADING

  const stepDescription = isSuccess
    ? null
    : snapshot.step === "setup"
      ? RESPONSE_SETUP_STEP_DESCRIPTION
      : snapshot.step === "write"
        ? GUEST_RESPONSE_STEP_DESCRIPTION
        : snapshot.step === "review"
          ? REVIEW_RESPONSE_STEP_DESCRIPTION
          : null

  const offerCouponView =
    snapshot.step === "review" && snapshot.channel === "email"
      ? buildGuestPreviewOfferCoupon(
          toConfirmedRecoveryOfferPayload(snapshot.offer)
        )
      : null
  const offerCoupon =
    offerCouponView != null ? (
      <GuestPreviewOfferCoupon coupon={offerCouponView} />
    ) : undefined

  return (
    <OperatorWizardShell
      isOpen={snapshot.isOpen}
      onRequestClose={isSuccess ? onKeepInProgress : onSaveAndExit}
      closeDisabled={locked && !isSuccess}
      showBackButton={!isSuccess}
      onBack={onBack}
      backDisabled={locked}
      title={
        isSuccess
          ? successChrome!.title
          : RECOVERY_WIZARD_PAGE_TITLE
      }
      description={
        isSuccess
          ? successChrome!.subtitle
          : (snapshot.headerSubtitle
            ?? "Prepare a recovery offer and guest response.")
      }
      descriptionSrOnly={snapshot.headerSubtitle == null && !isSuccess}
      stepHeading={stepHeading}
      stepDescription={stepDescription}
      steps={isSuccess ? null : STEP_LABELS}
      activeStepIndex={activeStep}
      isLoading={snapshot.loadStatus === "loading"}
      footerLayout={isSuccess ? "end" : "wizard"}
      onSaveAndExit={isSuccess ? undefined : onSaveAndExit}
      saveAndExitDisabled={locked}
      footer={
        !isSuccess ? (
          <>
            {snapshot.step === "setup" ? (
              <Button
                type="button"
                variant="op-primary"
                disabled={!snapshot.canContinueSetup || locked}
                onClick={onContinueSetup}
              >
                Continue
              </Button>
            ) : null}
            {snapshot.step === "offer" ? (
              <Button
                type="button"
                variant="op-primary"
                disabled={!snapshot.canContinueOffer || locked}
                onClick={onContinueOffer}
              >
                Continue
              </Button>
            ) : null}
            {onWriteEditor ? (
              <Button
                type="button"
                variant="op-primary"
                disabled={!snapshot.canContinueWrite || locked}
                onClick={onContinueWrite}
              >
                Continue
              </Button>
            ) : null}
            {snapshot.step === "review" ? (
              <Button
                type="button"
                variant="op-primary"
                disabled={locked}
                onClick={onOpenSendConfirm}
              >
                Send response and issue offer
              </Button>
            ) : null}
          </>
        ) : (
          <>
            <Button
              type="button"
              variant="op-secondary"
              disabled={completing}
              onClick={onKeepInProgress}
            >
              Keep in progress
            </Button>
            <Button
              type="button"
              variant="op-primary"
              disabled={completing}
              onClick={onMarkResolved}
            >
              {completing ? (
                <Loader2Icon className="size-4 animate-spin" aria-hidden />
              ) : null}
              Mark resolved
            </Button>
          </>
        )
      }
      preparingOverlay={{
        open: snapshot.preparingOverlayOpen,
        onDismiss: onDismissPreparingOverlay,
        onWriteManually,
        subtitle: snapshot.headerSubtitle,
        title: GUEST_RESPONSE_PREPARING_OVERLAY_TITLE,
        description: GUEST_RESPONSE_PREPARING_OVERLAY_DESCRIPTION,
      }}
      confirmDialog={{
        open: snapshot.sendConfirmOpen,
        busy: sending,
        onCancel: onCancelSendConfirm,
        onConfirm: onConfirmSend,
        title: sendConfirm.title,
        description: sendConfirm.description,
        error: snapshot.sendError,
        confirmLabel: sendConfirm.confirmLabel,
        confirmBusyLabel: "Sending…",
      }}
    >
      {snapshot.loadStatus === "loaded" && snapshot.summary != null ? (
        <div className="flex flex-col gap-10 lg:flex-row lg:items-start lg:gap-[42px]">
          <div className="flex flex-1 flex-col gap-6">
            {snapshot.step === "setup" ? (
              <ResponseSetupFields
                idPrefix="recovery-offer"
                availableChannels={snapshot.availableChannels}
                channel={snapshot.channel}
                maskedDestination={snapshot.maskedDestination}
                onChannelChange={onChannelChange}
                lockedPurposeLabel={snapshot.purposeLabel}
                purpose={null}
                tone={snapshot.tone}
                onToneChange={onToneChange}
                includeNotes={snapshot.includeNotes}
                onIncludeNotesChange={onIncludeNotesChange}
                disabled={locked}
              />
            ) : null}

            {snapshot.step === "offer" ? (
              <RecoveryOfferStep
                snapshot={snapshot}
                disabled={locked}
                onSelectStance={onSelectOfferStance}
                onCloseCreatePanel={onCloseCreateOfferPanel}
                onEditAttachedOffer={onEditAttachedOffer}
                onPatchCreateOfferDraft={onPatchCreateOfferDraft}
                onConfirmCreateOffer={onConfirmCreateOffer}
                onExistingOfferSearchChange={onExistingOfferSearchChange}
                onSelectExistingOffer={onSelectExistingOffer}
                onRetryExistingOfferPicker={onRetryExistingOfferPicker}
              />
            ) : null}

            {onWriteStep ? (
              <>
                <OfferSummaryCard snapshot={snapshot} onEditOffer={onEditOffer} />
                <GuestResponseChooser
                  disabled={locked}
                  aiDraftFailed={
                    snapshot.aiDraftStatus === "failed"
                    && snapshot.writeEntry === "chooser"
                  }
                  aiDraftRetryable={snapshot.aiDraftRetryable}
                  onPrepareDraft={onPrepareDraft}
                  onWriteManually={onWriteManually}
                  onRetryAiDraft={onRetryAiDraft}
                />
                {onWriteEditor ? (
                  <>
                    <Separator className="bg-op-card-border" />
                    <GuestResponseWriteFields
                      idPrefix="offer"
                      channel={snapshot.channel}
                      subject={snapshot.subject}
                      message={snapshot.message}
                      disabled={locked}
                      aiDraftStatus={snapshot.aiDraftStatus}
                      aiDraftMode={snapshot.aiDraftMode}
                      aiDraftRetryable={snapshot.aiDraftRetryable}
                      onSubjectChange={onSubjectChange}
                      onMessageChange={onMessageChange}
                      onRewriteSubject={() => {
                        onRewriteDraft("subject")
                      }}
                      onRewriteMessage={() => {
                        onRewriteDraft("message")
                      }}
                      onRetryAiDraft={onRetryAiDraft}
                    />
                  </>
                ) : null}
              </>
            ) : null}

            {snapshot.step === "review" ? (
              <>
                <RecoveryReviewSummary
                  idPrefix="recovery-offer-review"
                  guestName={snapshot.summary.guestName}
                  channel={snapshot.channel}
                  maskedDestination={snapshot.maskedDestination}
                  feedbackComment={snapshot.summary.feedbackComment}
                  feedbackId={snapshot.feedbackId}
                  issueTagLabels={snapshot.summary.issueTagLabels}
                  subject={snapshot.subject}
                  message={snapshot.message}
                  aiActionCount={snapshot.aiActionCount}
                />
                <OfferSummaryCard
                  snapshot={snapshot}
                  onEditOffer={onEditOffer}
                />
              </>
            ) : null}

            {isSuccess && successChrome != null ? (
              <RecoverySuccessStatusList rows={successChrome.rows} />
            ) : null}
          </div>

          {isSuccess ? null : snapshot.step === "review" ? (
            <GuestPreviewPanel
              channel={snapshot.channel}
              subject={snapshot.subject}
              message={snapshot.message}
              locationName={snapshot.locationName}
              locationAddress={snapshot.locationAddress}
              disabled={locked}
              guestPreviewOpen={snapshot.guestPreviewOpen}
              onOpenPreview={onOpenGuestPreview}
              onClosePreview={onCloseGuestPreview}
              onEditText={onEditText}
              onSendTest={onSendGuestPreviewTest}
              sendTestBusy={snapshot.sendTestStatus === "sending"}
              offerCoupon={offerCoupon}
            />
          ) : (
            <RecoveryFeedbackSummaryPanel
              guestName={snapshot.summary.guestName}
              classificationStatus={snapshot.summary.classificationStatus}
              classificationSentiment={
                snapshot.summary.classificationSentiment
              }
              contactLabel={snapshot.summary.contactLabel}
              feedbackComment={snapshot.summary.feedbackComment}
              issueTagLabels={snapshot.summary.issueTagLabels}
              extraRows={[
                {
                  label: "Purpose:",
                  children: snapshot.purposeLabel,
                },
                ...(snapshot.summary.toneLabel != null
                  ? [
                      {
                        label: "Tone:",
                        children: snapshot.summary.toneLabel,
                      },
                    ]
                  : []),
                ...(snapshot.summary.offerTitle != null
                  ? [
                      {
                        label: "Offer:",
                        children: snapshot.summary.offerTitle,
                      },
                    ]
                  : []),
              ]}
            />
          )}
        </div>
      ) : null}
    </OperatorWizardShell>
  )
}

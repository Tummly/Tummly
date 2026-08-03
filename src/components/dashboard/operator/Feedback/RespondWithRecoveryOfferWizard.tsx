import { Loader2Icon, SparklesIcon } from "lucide-react"
import { toast } from "sonner"
import { useEffect, type ReactNode } from "react"

import { Button } from "@/components/ui/button"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import { Input } from "@/components/ui/input"
import { Separator } from "@/components/ui/separator"
import { Textarea } from "@/components/ui/textarea"
import { RecoveryWizardShell } from "@/components/dashboard/operator/Feedback/RecoveryWizardShell"
import type { RespondWithRecoveryOfferSnapshot } from "@/lib/operatorFeedback/createRespondWithRecoveryOfferModule"
import {
  RECOVERY_OFFER_DESCRIPTION_MAX,
  RECOVERY_OFFER_PURCHASE_REQUIREMENT_OPTIONS,
  RECOVERY_OFFER_TITLE_MAX,
  RECOVERY_OFFER_TYPE_OPTIONS,
  RECOVERY_OFFER_VALIDITY_OPTIONS,
  labelForRecoveryOfferType,
  labelForRecoveryOfferValidity,
  type RecoveryOfferPurchaseRequirementId,
  type RecoveryOfferTypeId,
  type RecoveryOfferValidityId,
} from "@/lib/operatorFeedback/recoveryOfferPresentation"
import {
  RESPOND_TO_GUEST_TONE_OPTIONS,
  type RespondToGuestChannel,
  type RespondToGuestToneId,
} from "@/lib/operatorFeedback/respondToGuestPresentation"

type RespondWithRecoveryOfferWizardProps = {
  snapshot: RespondWithRecoveryOfferSnapshot
  onSaveAndExit: () => void
  onBack: () => void
  onChannelChange: (channel: RespondToGuestChannel) => void
  onToneChange: (tone: RespondToGuestToneId) => void
  onIncludeNotesChange: (value: string) => void
  onContinueSetup: () => void
  onOfferTypeChange: (offerType: RecoveryOfferTypeId) => void
  onDiscountPercentageChange: (value: string) => void
  onDiscountAmountChange: (value: string) => void
  onFreeItemTextChange: (value: string) => void
  onPurchaseRequirementChange: (
    value: RecoveryOfferPurchaseRequirementId
  ) => void
  onMinimumSpendChange: (value: string) => void
  onAdditionalExclusionsChange: (value: string) => void
  onReplacementItemTextChange: (value: string) => void
  onOfferTitleChange: (value: string) => void
  onOfferDescriptionChange: (value: string) => void
  onOfferValidityChange: (value: RecoveryOfferValidityId) => void
  onExpiryDateChange: (value: string) => void
  onStaffInstructionsChange: (value: string) => void
  onPrepareOfferDescription: () => void
  onContinueOffer: () => void
  onEditOffer: () => void
  onWriteManually: () => void
  onPrepareDraft: () => void
  onRewriteDraft: () => void
  onRetryAiDraft: () => void
  onDismissPreparingOverlay: () => void
  onSubjectChange: (value: string) => void
  onMessageChange: (value: string) => void
  onContinueWrite: () => void
  onEditText: () => void
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

function SummaryRow({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <div className="flex w-full items-start justify-between gap-4">
      <dt className="shrink-0 text-base font-semibold text-op-text-muted">
        {label}
      </dt>
      <dd className="min-w-0 text-right text-base font-medium text-op-text-primary">
        {children}
      </dd>
    </div>
  )
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
    <div className="flex flex-col gap-3 rounded-[6px] border border-op-card-border bg-[var(--op-color-gray-990)] p-4">
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
  onOfferTypeChange,
  onDiscountPercentageChange,
  onDiscountAmountChange,
  onFreeItemTextChange,
  onPurchaseRequirementChange,
  onMinimumSpendChange,
  onAdditionalExclusionsChange,
  onReplacementItemTextChange,
  onOfferTitleChange,
  onOfferDescriptionChange,
  onOfferValidityChange,
  onExpiryDateChange,
  onStaffInstructionsChange,
  onPrepareOfferDescription,
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

  const activeStep = stepIndex(snapshot.step)
  const isSuccess = snapshot.step === "success"
  const sending = snapshot.sendStatus === "saving"
  const completing = snapshot.completeStatus === "saving"
  const locked = snapshot.actionsLocked
  const onWriteChooser =
    snapshot.step === "write" && snapshot.writeEntry === "chooser"
  const onWriteEditor =
    snapshot.step === "write" && snapshot.writeEntry === "editor"
  const offer = snapshot.offer

  const title = isSuccess
    ? "Response and recovery offer sent"
    : snapshot.step === "setup"
      ? "Response setup"
      : snapshot.step === "offer"
        ? "Offer details"
        : snapshot.step === "write"
          ? onWriteChooser
            ? "Guest response"
            : "Write response manually"
          : "Review response and offer"

  return (
    <RecoveryWizardShell
      isOpen={snapshot.isOpen}
      onRequestClose={isSuccess ? onKeepInProgress : onSaveAndExit}
      closeDisabled={locked && !isSuccess}
      showBackButton={!isSuccess}
      onBack={onBack}
      backDisabled={locked}
      title={title}
      description={
        isSuccess
          ? `Sent to ${snapshot.maskedDestination ?? "guest"}${
              snapshot.issuedOffer != null
                ? ` · ${snapshot.issuedOffer.title}`
                : ""
            }`
          : (snapshot.headerSubtitle
            ?? "Prepare a recovery offer and guest response.")
      }
      descriptionSrOnly={snapshot.headerSubtitle == null && !isSuccess}
      descriptionClassName="max-w-[560px]"
      steps={isSuccess ? null : STEP_LABELS}
      activeStepIndex={activeStep}
      isLoading={snapshot.loadStatus === "loading"}
      footer={
        !isSuccess ? (
          <>
            <Button
              type="button"
              variant="op-secondary"
              disabled={locked}
              onClick={onSaveAndExit}
            >
              Save and exit
            </Button>
            {snapshot.step === "setup" ? (
              <Button
                type="button"
                disabled={!snapshot.canContinueSetup || locked}
                onClick={onContinueSetup}
              >
                Continue
              </Button>
            ) : null}
            {snapshot.step === "offer" ? (
              <Button
                type="button"
                disabled={!snapshot.canContinueOffer || locked}
                onClick={onContinueOffer}
              >
                Continue
              </Button>
            ) : null}
            {onWriteEditor ? (
              <Button
                type="button"
                disabled={!snapshot.canContinueWrite || locked}
                onClick={onContinueWrite}
              >
                Continue
              </Button>
            ) : null}
            {snapshot.step === "review" ? (
              <Button
                type="button"
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
      }}
      confirmDialog={{
        open: snapshot.sendConfirmOpen,
        busy: sending,
        onCancel: onCancelSendConfirm,
        onConfirm: onConfirmSend,
        title: "Send response and issue offer?",
        description: (
          <>
            Send to {snapshot.maskedDestination ?? "the guest"} and activate
            this recovery offer. The unique code is created on send.
          </>
        ),
        error: snapshot.sendError,
        confirmLabel: "Send and issue offer",
        confirmBusyLabel: "Sending…",
      }}
    >
      {snapshot.loadStatus === "loaded" && snapshot.summary != null ? (
        <div className="mt-10 flex w-full flex-col gap-10 lg:flex-row lg:items-start">
          <div className="flex w-full max-w-[690px] flex-col gap-6">
            {snapshot.step === "setup" ? (
              <>
                {snapshot.availableChannels.length > 1 ? (
                  <FloatingLabelSelect
                    label="Channel"
                    options={snapshot.availableChannels.map((channel) => ({
                      value: channel,
                      label: channel === "email" ? "Email" : "SMS",
                    }))}
                    value={snapshot.channel ?? undefined}
                    onValueChange={(value) => {
                      onChannelChange(value as RespondToGuestChannel)
                    }}
                    disableFocusRing
                    contentClassName="z-[140]"
                  />
                ) : (
                  <div className="rounded-[4px] border border-op-card-border bg-[var(--op-color-gray-990)] px-4 py-3">
                    <p className="text-xs font-medium text-op-text-muted">
                      Channel
                    </p>
                    <p className="mt-1 text-sm font-medium text-op-text-primary">
                      {snapshot.channel === "sms" ? "SMS" : "Email"}
                      {snapshot.maskedDestination != null
                        ? ` · ${snapshot.maskedDestination}`
                        : null}
                    </p>
                  </div>
                )}

                <div className="rounded-[4px] border border-op-card-border bg-[var(--op-color-gray-990)] px-4 py-3">
                  <p className="text-xs font-medium text-op-text-muted">
                    Purpose
                  </p>
                  <p className="mt-1 text-sm font-medium text-op-text-primary">
                    {snapshot.purposeLabel}
                  </p>
                </div>

                <FloatingLabelSelect
                  label="Tone"
                  options={RESPOND_TO_GUEST_TONE_OPTIONS.map((option) => ({
                    value: option.id,
                    label: option.label,
                  }))}
                  value={snapshot.tone ?? undefined}
                  onValueChange={(value) => {
                    onToneChange(value as RespondToGuestToneId)
                  }}
                  disableFocusRing
                  contentClassName="z-[140]"
                />

                <div className="flex flex-col gap-2">
                  <label
                    htmlFor="offer-include-notes"
                    className="text-sm font-medium text-op-text-primary"
                  >
                    Anything the response should include? (optional)
                  </label>
                  <Textarea
                    id="offer-include-notes"
                    value={snapshot.includeNotes}
                    onChange={(event) => {
                      onIncludeNotesChange(event.target.value)
                    }}
                    className="min-h-[96px] rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                  />
                </div>
              </>
            ) : null}

            {snapshot.step === "offer" ? (
              <>
                <FloatingLabelSelect
                  label="Offer type"
                  options={RECOVERY_OFFER_TYPE_OPTIONS.map((option) => ({
                    value: option.id,
                    label: option.label,
                  }))}
                  value={offer.offerType ?? undefined}
                  onValueChange={(value) => {
                    onOfferTypeChange(value as RecoveryOfferTypeId)
                  }}
                  disableFocusRing
                  contentClassName="z-[140]"
                />

                {offer.offerType === "percentage_discount" ? (
                  <div className="flex flex-col gap-2">
                    <label
                      htmlFor="offer-discount-pct"
                      className="text-sm font-medium text-op-text-primary"
                    >
                      Discount percentage
                    </label>
                    <Input
                      id="offer-discount-pct"
                      type="number"
                      min={0}
                      step="any"
                      value={offer.discountPercentage}
                      onChange={(event) => {
                        onDiscountPercentageChange(event.target.value)
                      }}
                      className="h-12 rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                    />
                  </div>
                ) : null}

                {offer.offerType === "fixed_discount" ? (
                  <div className="flex flex-col gap-2">
                    <label
                      htmlFor="offer-discount-amount"
                      className="text-sm font-medium text-op-text-primary"
                    >
                      Discount amount (£)
                    </label>
                    <Input
                      id="offer-discount-amount"
                      type="number"
                      min={0}
                      step="any"
                      value={offer.discountAmount}
                      onChange={(event) => {
                        onDiscountAmountChange(event.target.value)
                      }}
                      className="h-12 rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                    />
                  </div>
                ) : null}

                {offer.offerType === "free_item" ? (
                  <>
                    <div className="flex flex-col gap-2">
                      <label
                        htmlFor="offer-free-item"
                        className="text-sm font-medium text-op-text-primary"
                      >
                        Free item
                      </label>
                      <Input
                        id="offer-free-item"
                        value={offer.freeItemText}
                        onChange={(event) => {
                          onFreeItemTextChange(event.target.value)
                        }}
                        className="h-12 rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                      />
                    </div>
                    <FloatingLabelSelect
                      label="Purchase requirement"
                      options={RECOVERY_OFFER_PURCHASE_REQUIREMENT_OPTIONS.map(
                        (option) => ({
                          value: option.id,
                          label: option.label,
                        })
                      )}
                      value={offer.purchaseRequirement ?? undefined}
                      onValueChange={(value) => {
                        onPurchaseRequirementChange(
                          value as RecoveryOfferPurchaseRequirementId
                        )
                      }}
                      disableFocusRing
                      contentClassName="z-[140]"
                    />
                    {offer.purchaseRequirement === "with_minimum_spend" ? (
                      <div className="flex flex-col gap-2">
                        <label
                          htmlFor="offer-min-spend"
                          className="text-sm font-medium text-op-text-primary"
                        >
                          Minimum spend (£)
                        </label>
                        <Input
                          id="offer-min-spend"
                          type="number"
                          min={0}
                          step="any"
                          value={offer.minimumSpend}
                          onChange={(event) => {
                            onMinimumSpendChange(event.target.value)
                          }}
                          className="h-12 rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                        />
                      </div>
                    ) : null}
                    <div className="flex flex-col gap-2">
                      <label
                        htmlFor="offer-exclusions"
                        className="text-sm font-medium text-op-text-primary"
                      >
                        Additional exclusions (optional)
                      </label>
                      <Textarea
                        id="offer-exclusions"
                        value={offer.additionalExclusions}
                        onChange={(event) => {
                          onAdditionalExclusionsChange(event.target.value)
                        }}
                        className="min-h-[80px] rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                      />
                    </div>
                  </>
                ) : null}

                {offer.offerType === "replacement_item" ? (
                  <div className="flex flex-col gap-2">
                    <label
                      htmlFor="offer-replacement"
                      className="text-sm font-medium text-op-text-primary"
                    >
                      Replacement item
                    </label>
                    <Input
                      id="offer-replacement"
                      value={offer.replacementItemText}
                      onChange={(event) => {
                        onReplacementItemTextChange(event.target.value)
                      }}
                      className="h-12 rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                    />
                  </div>
                ) : null}

                <div className="flex flex-col gap-2">
                  <label
                    htmlFor="offer-title"
                    className="text-sm font-medium text-op-text-primary"
                  >
                    Offer title
                  </label>
                  <Input
                    id="offer-title"
                    value={offer.title}
                    maxLength={RECOVERY_OFFER_TITLE_MAX}
                    onChange={(event) => {
                      onOfferTitleChange(event.target.value)
                    }}
                    className="h-12 rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                  />
                  <p className="text-xs text-op-text-muted">
                    {offer.title.length}/{RECOVERY_OFFER_TITLE_MAX}
                  </p>
                </div>

                <div className="flex flex-col gap-2">
                  <label
                    htmlFor="offer-description"
                    className="text-sm font-medium text-op-text-primary"
                  >
                    Offer description
                  </label>
                  <Textarea
                    id="offer-description"
                    value={offer.description}
                    maxLength={RECOVERY_OFFER_DESCRIPTION_MAX}
                    onChange={(event) => {
                      onOfferDescriptionChange(event.target.value)
                    }}
                    className="min-h-[120px] rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                  />
                  <div className="flex flex-wrap items-center gap-3">
                    <Button
                      type="button"
                      variant="op-secondary"
                      disabled={locked || offer.offerType == null}
                      onClick={onPrepareOfferDescription}
                    >
                      {snapshot.offerDescriptionAiStatus === "running" ? (
                        <Loader2Icon
                          className="size-4 animate-spin"
                          aria-hidden
                        />
                      ) : (
                        <SparklesIcon className="size-4" aria-hidden />
                      )}
                      Prepare offer description
                    </Button>
                    <span className="text-xs text-op-text-muted">
                      {offer.description.length}/
                      {RECOVERY_OFFER_DESCRIPTION_MAX}
                    </span>
                  </div>
                </div>

                <FloatingLabelSelect
                  label="Offer validity"
                  options={RECOVERY_OFFER_VALIDITY_OPTIONS.map((option) => ({
                    value: option.id,
                    label: option.label,
                  }))}
                  value={offer.validity}
                  onValueChange={(value) => {
                    onOfferValidityChange(value as RecoveryOfferValidityId)
                  }}
                  disableFocusRing
                  contentClassName="z-[140]"
                />

                {offer.validity === "choose_expiry_date" ? (
                  <div className="flex flex-col gap-2">
                    <label
                      htmlFor="offer-expiry"
                      className="text-sm font-medium text-op-text-primary"
                    >
                      Expiry date
                    </label>
                    <Input
                      id="offer-expiry"
                      type="date"
                      value={offer.expiryDate}
                      onChange={(event) => {
                        onExpiryDateChange(event.target.value)
                      }}
                      className="h-12 rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                    />
                    <p className="text-xs text-op-text-muted">
                      Expires end of that date in the restaurant’s timezone.
                    </p>
                  </div>
                ) : null}

                <div className="rounded-[4px] border border-op-card-border bg-[var(--op-color-gray-990)] px-4 py-3">
                  <p className="text-xs font-medium text-op-text-muted">
                    Redemption
                  </p>
                  <p className="mt-1 text-sm font-medium text-op-text-primary">
                    Unique single-use code
                  </p>
                  <p className="mt-1 text-xs text-op-text-muted">
                    Generated when you send and issue the offer.
                  </p>
                </div>

                <div className="flex flex-col gap-2">
                  <label
                    htmlFor="offer-staff"
                    className="text-sm font-medium text-op-text-primary"
                  >
                    Staff instructions (optional)
                  </label>
                  <Textarea
                    id="offer-staff"
                    value={offer.staffInstructions}
                    onChange={(event) => {
                      onStaffInstructionsChange(event.target.value)
                    }}
                    className="min-h-[96px] rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                  />
                </div>
              </>
            ) : null}

            {onWriteChooser || onWriteEditor ? (
              <OfferSummaryCard snapshot={snapshot} onEditOffer={onEditOffer} />
            ) : null}

            {onWriteChooser ? (
              <div className="flex flex-col gap-4">
                <p className="text-sm font-medium text-op-text-muted">
                  Prepare an AI draft or write the response yourself.
                </p>
                {snapshot.aiDraftStatus === "failed" ? (
                  <div className="flex flex-wrap gap-3">
                    {snapshot.aiDraftRetryable ? (
                      <Button
                        type="button"
                        disabled={locked}
                        onClick={onRetryAiDraft}
                      >
                        Try again
                      </Button>
                    ) : null}
                    <Button
                      type="button"
                      variant="op-secondary"
                      disabled={locked}
                      onClick={onWriteManually}
                    >
                      Write manually
                    </Button>
                  </div>
                ) : (
                  <div className="flex flex-wrap gap-3">
                    <Button
                      type="button"
                      disabled={locked}
                      onClick={onPrepareDraft}
                    >
                      <SparklesIcon className="size-4" aria-hidden />
                      Prepare response draft
                    </Button>
                    <Button
                      type="button"
                      variant="op-secondary"
                      disabled={locked}
                      onClick={onWriteManually}
                    >
                      Write response manually
                    </Button>
                  </div>
                )}
              </div>
            ) : null}

            {onWriteEditor ? (
              <>
                {snapshot.channel === "email" ? (
                  <div className="flex flex-col gap-2">
                    <label
                      htmlFor="offer-subject"
                      className="text-sm font-medium text-op-text-primary"
                    >
                      Subject
                    </label>
                    <Input
                      id="offer-subject"
                      value={snapshot.subject}
                      disabled={locked}
                      onChange={(event) => {
                        onSubjectChange(event.target.value)
                      }}
                      className="h-12 rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                    />
                  </div>
                ) : null}
                <div className="flex flex-col gap-2">
                  <label
                    htmlFor="offer-message"
                    className="text-sm font-medium text-op-text-primary"
                  >
                    Message
                  </label>
                  <Textarea
                    id="offer-message"
                    value={snapshot.message}
                    disabled={locked}
                    onChange={(event) => {
                      onMessageChange(event.target.value)
                    }}
                    className="min-h-[220px] rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                  />
                </div>
                <div className="flex flex-wrap items-center gap-3">
                  <Button
                    type="button"
                    variant="op-secondary"
                    disabled={locked}
                    onClick={onRewriteDraft}
                  >
                    <SparklesIcon className="size-4" aria-hidden />
                    Rewrite with AI
                  </Button>
                </div>
              </>
            ) : null}

            {snapshot.step === "review" ? (
              <>
                <div className="flex flex-col gap-4 rounded-[6px] border border-op-card-border bg-[var(--op-color-gray-990)] p-5">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-op-text-primary">
                      Final response
                    </p>
                    <Button
                      type="button"
                      variant="op-secondary"
                      size="sm"
                      onClick={onEditText}
                    >
                      Edit text
                    </Button>
                  </div>
                  {snapshot.channel === "email" ? (
                    <>
                      <div>
                        <p className="text-xs font-medium text-op-text-muted">
                          Subject
                        </p>
                        <p className="mt-1 text-sm font-medium text-op-text-primary">
                          {snapshot.subject}
                        </p>
                      </div>
                      <Separator className="bg-op-card-border" />
                    </>
                  ) : null}
                  <div>
                    <p className="text-xs font-medium text-op-text-muted">
                      Message
                    </p>
                    <p className="mt-1 whitespace-pre-wrap text-sm font-medium text-op-text-primary">
                      {snapshot.message}
                    </p>
                  </div>
                </div>
                <OfferSummaryCard
                  snapshot={snapshot}
                  onEditOffer={onEditOffer}
                />
              </>
            ) : null}

            {isSuccess ? (
              <div className="flex flex-col gap-4 rounded-[6px] border border-op-card-border bg-[var(--op-color-gray-990)] p-5">
                <SummaryRow label="Recovery status">Offer issued</SummaryRow>
                <Separator className="bg-op-card-border" />
                <SummaryRow label="Response status">Sent</SummaryRow>
                <Separator className="bg-op-card-border" />
                <SummaryRow label="Workflow status">In progress</SummaryRow>
                {snapshot.issuedOffer != null ? (
                  <>
                    <Separator className="bg-op-card-border" />
                    <SummaryRow label="Offer">
                      {snapshot.issuedOffer.title}
                    </SummaryRow>
                    <Separator className="bg-op-card-border" />
                    <SummaryRow label="Redemption code">
                      {snapshot.issuedOffer.redemptionCode}
                    </SummaryRow>
                    <Separator className="bg-op-card-border" />
                    <SummaryRow label="Redemption status">
                      Not redeemed
                    </SummaryRow>
                  </>
                ) : null}
              </div>
            ) : null}
          </div>

          <aside className="flex w-full flex-1 flex-col gap-6 rounded-[6px] bg-[var(--op-color-gray-990)] p-5">
            <h2 className="text-lg font-semibold text-op-text-primary">
              Feedback summary
            </h2>
            <dl className="flex flex-col gap-3.5">
              <SummaryRow label="Guest:">
                {snapshot.summary.guestName}
              </SummaryRow>
              <Separator className="bg-op-card-border" />
              <SummaryRow label="Feedback:">
                “{snapshot.summary.feedbackComment}”
              </SummaryRow>
              <Separator className="bg-op-card-border" />
              <SummaryRow label="Purpose:">{snapshot.purposeLabel}</SummaryRow>
              {snapshot.summary.toneLabel != null ? (
                <>
                  <Separator className="bg-op-card-border" />
                  <SummaryRow label="Tone:">
                    {snapshot.summary.toneLabel}
                  </SummaryRow>
                </>
              ) : null}
              {snapshot.summary.offerTitle != null ? (
                <>
                  <Separator className="bg-op-card-border" />
                  <SummaryRow label="Offer:">
                    {snapshot.summary.offerTitle}
                  </SummaryRow>
                </>
              ) : null}
            </dl>
          </aside>
        </div>
      ) : null}
    </RecoveryWizardShell>
  )
}

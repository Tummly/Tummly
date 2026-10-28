import { useState } from "react"
import { XIcon } from "lucide-react"

import {
  CustomPlanQuotePlaceholder,
  CustomPlanSummaryCard,
} from "@/components/marketing/custom-plan/CustomPlanSummaryCard"
import { CustomPlanCalculator } from "@/components/marketing/custom-plan/CustomPlanCalculator"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { CUSTOM_PLAN_QUOTE_COPY } from "@/content/marketing/customPlanQuoteCopy"
import {
  normalizePackCounts,
  quotePlanName,
} from "@/lib/marketingCustomPlan/customPlanEstimate"
import {
  lastQuote,
  reviewGate,
} from "@/lib/marketingCustomPlan/customPlanQuoteState"
import { useServerCustomPlanQuote } from "@/lib/marketingCustomPlan/useServerCustomPlanQuote"
import type { PendingPaymentReviewView } from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"
import { MANAGE_PLAN_COPY } from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  reviewedConfigJsonFromQuote,
  type PendingPaymentSavedChoices,
} from "@/lib/operatorBillingCredits/pendingPaymentReview"
import { CAPTURE_DIALOG_CLOSE_BUTTON_CLASS } from "@/lib/operatorCapture/capturePresentation"

type PendingPaymentReviewDialogProps = {
  review: PendingPaymentReviewView
  onClose: () => void
  onConfirm: (customPlanConfigJson: string) => Promise<void>
}

/**
 * The saved Custom quote can no longer be charged. The owner sees a fresh server
 * quote for the saved choices (or chooses again when an option was retired), ticks
 * the updated total, and only then pays.
 */
export function PendingPaymentReviewDialog({
  review,
  onClose,
  onConfirm,
}: PendingPaymentReviewDialogProps) {
  // Choices made again after an option was retired. They replace the saved ones.
  const [chosen, setChosen] = useState<PendingPaymentSavedChoices | null>(null)
  const choices = chosen ?? review.savedChoices

  return (
    <Dialog
      open={review.open}
      onOpenChange={(next) => {
        if (!next && !review.busy) {
          onClose()
        }
      }}
    >
      <DialogContent
        showCloseButton={false}
        className={
          choices == null
            ? "max-h-[90vh] gap-6 overflow-y-auto rounded-op-md bg-op-surface-secondary p-8 text-op-text-primary sm:max-w-[1200px]"
            : "max-h-[90vh] gap-6 overflow-y-auto rounded-op-md bg-op-surface-secondary p-8 text-op-text-primary sm:max-w-[680px]"
        }
      >
        <DialogHeader className="gap-3">
          <DialogTitle className="pr-10 text-2xl font-bold tracking-normal text-op-text-primary">
            {MANAGE_PLAN_COPY.pendingPaymentReviewTitle}
          </DialogTitle>
          <DialogDescription className="text-base font-medium leading-[22px] text-op-text-muted">
            {review.message}
          </DialogDescription>
        </DialogHeader>
        <DialogClose asChild>
          <Button
            type="button"
            variant="op-collapse"
            disabled={review.busy}
            aria-label="Close"
            className={CAPTURE_DIALOG_CLOSE_BUTTON_CLASS}
            onClick={onClose}
          >
            <XIcon aria-hidden />
          </Button>
        </DialogClose>

        {choices == null ? (
          <CustomPlanCalculator
            initialBasePlan="growth"
            continueLabel={MANAGE_PLAN_COPY.pendingPaymentReviewChoose}
            showHeading={false}
            onContinue={({ config }) =>
              setChosen({
                cadence: config.cadence,
                basePlan: config.basePlan,
                locations: config.locations,
                packs: { ...config.packs } as Record<string, number>,
              })
            }
          />
        ) : (
          <PendingPaymentQuoteReview
            choices={choices}
            busy={review.busy}
            onCancel={onClose}
            onConfirm={onConfirm}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}

function PendingPaymentQuoteReview({
  choices,
  busy,
  onCancel,
  onConfirm,
}: {
  choices: PendingPaymentSavedChoices
  busy: boolean
  onCancel: () => void
  onConfirm: (customPlanConfigJson: string) => Promise<void>
}) {
  const { state, check, retry } = useServerCustomPlanQuote({
    cadence: choices.cadence,
    basePlan: choices.basePlan,
    locations: choices.locations,
    packs: normalizePackCounts(choices.packs),
  })
  const [confirmedHash, setConfirmedHash] = useState<string | null>(null)

  // The saved quote was refused, so the total is always a changed one: it needs the
  // owner's tick for this exact ready quote.
  const gate = reviewGate({
    selectable: true,
    waitingForCatalogue: false,
    check,
    state,
    quoteChanged: true,
    restoredHashChanged: false,
    confirmedHash,
  })
  // A new ready quote ends the old acknowledgement for good.
  if (gate.keptHash !== confirmedHash) {
    setConfirmedHash(gate.keptHash)
  }

  const quote = lastQuote(state)
  const readyQuote = state.status === "ready" ? state.quote : null

  function handlePay() {
    if (readyQuote == null || !gate.continueEnabled) {
      return
    }
    const json = reviewedConfigJsonFromQuote(readyQuote)
    if (json != null) {
      void onConfirm(json)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      {quote != null ? (
        <CustomPlanSummaryCard
          quote={quote}
          cadence={choices.cadence}
          showCadenceToggle={false}
          showActions={false}
          showBreakdown
          statusMessage={gate.view?.message ?? null}
          showRetry={gate.view?.showRetry ?? false}
          onRetry={retry}
          showContact={gate.view?.showContact ?? false}
          taxText={gate.view?.taxText ?? null}
          checkoutNote={CUSTOM_PLAN_QUOTE_COPY.noPaymentTaken}
          planDescription={`You are checking out with ${quotePlanName(quote)}.`}
          className="max-w-none pt-0"
        />
      ) : (
        <CustomPlanQuotePlaceholder
          loading={state.status === "loading"}
          message={gate.view?.message ?? null}
          showRetry={gate.view?.showRetry ?? false}
          onRetry={retry}
          showContact={gate.view?.showContact ?? false}
          className="max-w-none pt-0"
        />
      )}

      {gate.priceChanged && gate.readyHash != null ? (
        <CheckboxLabel
          checked={gate.priceConfirmed}
          onCheckedChange={(checked) =>
            setConfirmedHash(checked ? gate.readyHash : null)
          }
        >
          {CUSTOM_PLAN_QUOTE_COPY.changedConfirm}
        </CheckboxLabel>
      ) : null}

      <div className="flex flex-row flex-wrap justify-start gap-3">
        <Button
          type="button"
          variant="op-primary"
          disabled={busy || !gate.continueEnabled}
          onClick={handlePay}
        >
          {MANAGE_PLAN_COPY.pendingPaymentReviewPay}
        </Button>
        <Button
          type="button"
          variant="op-tertiary"
          disabled={busy}
          onClick={onCancel}
        >
          {MANAGE_PLAN_COPY.pendingPaymentReviewCancel}
        </Button>
      </div>
    </div>
  )
}

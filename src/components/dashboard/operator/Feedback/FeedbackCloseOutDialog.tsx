import { XIcon } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Spinner } from "@/components/ui/spinner"
import { Textarea } from "@/components/ui/textarea"
import type {
  FeedbackDetailsCloseOutEditor,
  FeedbackDetailsLoaded,
} from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import {
  FEEDBACK_CLOSE_OUT_ACKNOWLEDGMENT_LABEL,
  FEEDBACK_CLOSE_OUT_REASONS,
  feedbackCloseOutDialogCopy,
  feedbackCloseOutHighRiskCallout,
  feedbackCloseOutReasonPlaceholder,
  feedbackCloseOutRequiresAcknowledgment,
  feedbackCloseOutShowsHighRiskCallout,
  type FeedbackCloseOutReason,
} from "@/lib/operatorFeedback/feedbackCloseOutPresentation"
import {
  FEEDBACK_DIALOG_BODY_CLASS,
  FEEDBACK_DIALOG_CONTENT_CLASS,
  FEEDBACK_DIALOG_DESCRIPTION_CLASS,
  FEEDBACK_DIALOG_FOOTER_CLASS,
  FEEDBACK_DIALOG_HEADER_ROW_DIVIDED_CLASS,
  FEEDBACK_DIALOG_SELECT_GROUP_CLASS,
  FEEDBACK_DIALOG_SELECT_ITEM_CLASS,
  FEEDBACK_DIALOG_SELECT_MENU_CLASS,
  FEEDBACK_FIELD_LABEL_CLASS,
  FEEDBACK_TEXTAREA_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import { feedbackSentimentLabel } from "@/lib/operatorHome/feedbackSentimentLabel"

type FeedbackCloseOutDialogProps = {
  closeOut: FeedbackDetailsCloseOutEditor
  details: FeedbackDetailsLoaded | null
  onOpenChange: (open: boolean) => void
  onReasonChange: (reason: FeedbackCloseOutReason) => void
  onNoteDraftChange: (value: string) => void
  onAcknowledgedChange: (value: boolean) => void
  onConfirm: () => void
}

const REASON_TRIGGER_CLASS =
  "h-auto min-h-[50px] w-full rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary shadow-none dark:bg-transparent dark:hover:bg-transparent"

const DIVIDER_CLASS = "h-px w-full shrink-0 bg-op-border-default"

const SUMMARY_LABEL_CLASS =
  "font-medium text-base text-op-text-primary"

const SUMMARY_VALUE_CLASS =
  "m-0 font-medium text-sm text-[var(--op-color-gray-550)]"

const HIGH_RISK_CALLOUT_CLASS =
  "rounded-op-md bg-[var(--op-capture-pause-warning-background)] p-[18px] text-base font-medium leading-[22px] text-[var(--op-capture-pause-warning-text)]"

/** Close-out feedback dialog — Mark resolved / Mark no action (Figma 4481:17481 / 4481:18601). */
export function FeedbackCloseOutDialog({
  closeOut,
  details,
  onOpenChange,
  onReasonChange,
  onNoteDraftChange,
  onAcknowledgedChange,
  onConfirm,
}: FeedbackCloseOutDialogProps) {
  if (!closeOut.isOpen || closeOut.intent == null) {
    return null
  }

  const copy = feedbackCloseOutDialogCopy(closeOut.intent)
  const saving = closeOut.saveStatus === "saving"
  const showAcknowledgment = feedbackCloseOutRequiresAcknowledgment(
    closeOut.intent
  )
  const showHighRiskCallout = feedbackCloseOutShowsHighRiskCallout(
    closeOut.intent,
    details?.sentiment
  )

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        onOpenChange(open)
      }}
    >
      <DialogContent
        showCloseButton={false}
        className={FEEDBACK_DIALOG_CONTENT_CLASS}
      >
        <div className={FEEDBACK_DIALOG_HEADER_ROW_DIVIDED_CLASS}>
          <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
            <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
              {copy.title}
            </DialogTitle>
            <DialogDescription className={FEEDBACK_DIALOG_DESCRIPTION_CLASS}>
              {closeOut.intent === "mark_resolved" ? (
                <>
                  Confirm how the feedback was handled. The resolution
                  <br />
                  will be recorded in the activity history.
                </>
              ) : (
                copy.subtitle
              )}
            </DialogDescription>
          </DialogHeader>
          <DialogClose asChild>
            <Button
              type="button"
              variant="op-collapse"
              aria-label="Close"
              className="shrink-0"
              disabled={saving}
            >
              <XIcon aria-hidden />
            </Button>
          </DialogClose>
        </div>

        <div className={FEEDBACK_DIALOG_BODY_CLASS}>
          {details != null ? (
            <>
              <dl className="m-0 flex flex-col gap-5 text-sm">
                <div className="flex items-center justify-between gap-4">
                  <dt className={SUMMARY_LABEL_CLASS}>Guest</dt>
                  <dd className={SUMMARY_VALUE_CLASS}>{details.guestName}</dd>
                </div>
                <div className="flex items-center justify-between gap-4">
                  <dt className={SUMMARY_LABEL_CLASS}>Classification</dt>
                  <dd className="m-0">
                    {details.sentiment != null ? (
                      <Badge variant={details.sentiment}>
                        {feedbackSentimentLabel(details.sentiment)}
                      </Badge>
                    ) : (
                      <span className={SUMMARY_VALUE_CLASS}>—</span>
                    )}
                  </dd>
                </div>
                <div className="flex items-center justify-between gap-4">
                  <dt className={SUMMARY_LABEL_CLASS}>Location</dt>
                  <dd className={SUMMARY_VALUE_CLASS}>
                    {details.locationName}
                  </dd>
                </div>
                <div className="flex items-center justify-between gap-4">
                  <dt className={SUMMARY_LABEL_CLASS}>Feedback ID</dt>
                  <dd className={SUMMARY_VALUE_CLASS}>
                    {details.feedbackReference}
                  </dd>
                </div>
              </dl>

              <div className={DIVIDER_CLASS} aria-hidden />
            </>
          ) : null}

          <div className="flex flex-col gap-2">
            <Label
              htmlFor="feedback-close-out-reason"
              className="font-semibold leading-5 text-op-text-primary"
            >
              Reason
            </Label>
            <Select
              value={closeOut.reason ?? undefined}
              disabled={saving}
              onValueChange={(value) => {
                if (
                  value === "positive_no_follow_up"
                  || value === "duplicate_submission"
                  || value === "test_or_invalid"
                  || value === "already_handled_outside"
                  || value === "no_appropriate_follow_up"
                  || value === "other"
                ) {
                  onReasonChange(value)
                }
              }}
            >
              <SelectTrigger
                id="feedback-close-out-reason"
                className={REASON_TRIGGER_CLASS}
              >
                <SelectValue placeholder={feedbackCloseOutReasonPlaceholder} />
              </SelectTrigger>
              <SelectContent
                position="popper"
                align="start"
                className={FEEDBACK_DIALOG_SELECT_MENU_CLASS}
              >
                <SelectGroup className={FEEDBACK_DIALOG_SELECT_GROUP_CLASS}>
                  {FEEDBACK_CLOSE_OUT_REASONS.map((option) => (
                    <SelectItem
                      key={option.value}
                      value={option.value}
                      className={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
                    >
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectGroup>
              </SelectContent>
            </Select>
          </div>

          {closeOut.reason === "other" ? (
            <div className="flex flex-col gap-2">
              <Label
                htmlFor="feedback-close-out-note"
                className={FEEDBACK_FIELD_LABEL_CLASS}
              >
                Internal note
              </Label>
              <Textarea
                id="feedback-close-out-note"
                value={closeOut.noteDraft}
                disabled={saving}
                rows={4}
                placeholder={copy.notePlaceholder}
                onChange={(event) => {
                  onNoteDraftChange(event.target.value)
                }}
                className={`${FEEDBACK_TEXTAREA_CLASS} min-h-0 resize-none`}
              />
            </div>
          ) : null}

          {showAcknowledgment ? (
            <>
              {showHighRiskCallout ? (
                <>
                  <div className={DIVIDER_CLASS} aria-hidden />
                  <p className={HIGH_RISK_CALLOUT_CLASS} role="status">
                    {feedbackCloseOutHighRiskCallout}
                  </p>
                </>
              ) : null}
              <div className={DIVIDER_CLASS} aria-hidden />
              <CheckboxLabel
                id="feedback-close-out-acknowledgment"
                checked={closeOut.acknowledged}
                disabled={saving}
                onCheckedChange={onAcknowledgedChange}
                labelClassName="text-op-text-primary dark:text-op-text-primary"
              >
                {FEEDBACK_CLOSE_OUT_ACKNOWLEDGMENT_LABEL}
              </CheckboxLabel>
            </>
          ) : null}

          {closeOut.saveError != null ? (
            <p
              role="alert"
              className="m-0 text-sm font-medium text-[var(--op-color-red-550)]"
            >
              {closeOut.saveError}
            </p>
          ) : null}
        </div>

        <DialogFooter className={FEEDBACK_DIALOG_FOOTER_CLASS}>
          <Button
            type="button"
            variant="op-primary"
            disabled={!closeOut.canConfirm || saving}
            aria-disabled={!closeOut.canConfirm || saving}
            onClick={() => {
              onConfirm()
            }}
          >
            {saving ? (
              <>
                <Spinner size="sm" data-icon="inline-start" />
                Saving…
              </>
            ) : (
              copy.confirmLabel
            )}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            disabled={saving}
            onClick={() => {
              onOpenChange(false)
            }}
          >
            Cancel
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

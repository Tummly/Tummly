import { XIcon } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
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
  FeedbackClassificationCorrectionEditor,
  FeedbackDetailsLoaded,
} from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import {
  FEEDBACK_CLASSIFICATION_CORRECTION_COPY,
  FEEDBACK_CLASSIFICATION_CORRECTION_REASONS,
  formatAiClassifiedMetaLine,
  type FeedbackClassificationCorrectionReason,
} from "@/lib/operatorFeedback/feedbackClassificationCorrectionPresentation"
import {
  FEEDBACK_DIALOG_SELECT_GROUP_CLASS,
  FEEDBACK_DIALOG_SELECT_ITEM_CLASS,
  FEEDBACK_DIALOG_SELECT_MENU_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import { formatGuestProfileAbsoluteDateTime } from "@/lib/operatorGuestProfile/mapGuestProfileApiResponseToViewModel"
import { feedbackSentimentLabel } from "@/lib/operatorHome/feedbackSentimentLabel"
import type { FeedbackSentiment } from "@/types/dashboard"

type FeedbackCorrectClassificationDialogProps = {
  correction: FeedbackClassificationCorrectionEditor
  details: FeedbackDetailsLoaded | null
  onOpenChange: (open: boolean) => void
  onSentimentChange: (sentiment: FeedbackSentiment) => void
  onReasonChange: (reason: FeedbackClassificationCorrectionReason) => void
  onNoteDraftChange: (value: string) => void
  onConfirm: () => void
}

const SENTIMENT_OPTIONS: Array<{
  value: FeedbackSentiment
  label: string
}> = [
  { value: "positive", label: "Positive" },
  { value: "neutral", label: "Neutral" },
  { value: "negative", label: "Negative" },
]

const SELECT_TRIGGER_CLASS =
  "h-auto min-h-[50px] w-full rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary shadow-none dark:bg-transparent dark:hover:bg-transparent"

const DIVIDER_CLASS = "h-px w-full shrink-0 bg-op-border-default"

const CALLOUT_CLASS =
  "rounded-op-md bg-[#262626] p-[18px] text-base font-medium leading-[22px] text-[var(--op-color-gray-550)]"

/** Correct AI classification dialog — Figma `4481:20220`. */
export function FeedbackCorrectClassificationDialog({
  correction,
  details,
  onOpenChange,
  onSentimentChange,
  onReasonChange,
  onNoteDraftChange,
  onConfirm,
}: FeedbackCorrectClassificationDialogProps) {
  if (!correction.isEditing) {
    return null
  }

  const copy = FEEDBACK_CLASSIFICATION_CORRECTION_COPY
  const saving = correction.saveStatus === "saving"
  const classifiedAbsolute =
    details != null
      ? formatGuestProfileAbsoluteDateTime(details.classifiedAt)
      : ""

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        onOpenChange(open)
      }}
    >
      <DialogContent
        showCloseButton={false}
        className="max-h-[min(90vh,900px)] gap-[60px] overflow-y-auto bg-[var(--op-color-gray-995)] p-8 text-op-text-primary sm:max-w-[642px]"
      >
        <div className="flex flex-col gap-[30px]">
          <div className="flex items-start gap-[22px]">
            <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
              <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
                {copy.title}
              </DialogTitle>
              <DialogDescription className="text-sm font-medium leading-[18px] text-[var(--op-color-gray-550)]">
                {copy.subtitle}
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

          <div className={DIVIDER_CLASS} aria-hidden />

          {details?.sentiment != null ? (
            <div className="flex flex-col gap-3">
              <p className="m-0 text-base font-medium text-op-text-primary">
                {copy.currentClassificationLabel}
              </p>
              <div className="flex flex-col gap-2">
                <Badge variant={details.sentiment} className="w-fit">
                  {feedbackSentimentLabel(details.sentiment)}
                </Badge>
                <p className="m-0 text-sm font-medium text-[var(--op-color-gray-550)]">
                  {formatAiClassifiedMetaLine(classifiedAbsolute)}
                </p>
              </div>
            </div>
          ) : null}

          <div className="flex flex-col gap-2">
            <Label
              htmlFor="feedback-correct-classification-sentiment"
              className="font-semibold leading-5 text-op-text-primary"
            >
              {copy.newClassificationLabel}
            </Label>
            <Select
              value={correction.draftSentiment ?? undefined}
              disabled={saving}
              onValueChange={(value) => {
                if (
                  value === "positive"
                  || value === "neutral"
                  || value === "negative"
                ) {
                  onSentimentChange(value)
                }
              }}
            >
              <SelectTrigger
                id="feedback-correct-classification-sentiment"
                className={SELECT_TRIGGER_CLASS}
              >
                <SelectValue placeholder={copy.selectPlaceholder} />
              </SelectTrigger>
              <SelectContent
                position="popper"
                align="start"
                className={FEEDBACK_DIALOG_SELECT_MENU_CLASS}
              >
                <SelectGroup className={FEEDBACK_DIALOG_SELECT_GROUP_CLASS}>
                  {SENTIMENT_OPTIONS.map((option) => (
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

          <div className={DIVIDER_CLASS} aria-hidden />

          <div className="flex flex-col gap-2">
            <Label
              htmlFor="feedback-correct-classification-reason"
              className="font-semibold leading-5 text-op-text-primary"
            >
              {copy.reasonLabel}
            </Label>
            <Select
              value={correction.draftReason ?? undefined}
              disabled={saving}
              onValueChange={(value) => {
                if (
                  value === "mixed_or_ambiguous"
                  || value === "context_misunderstood"
                  || value === "language_or_translation"
                  || value === "incorrect_ai_classification"
                  || value === "other"
                ) {
                  onReasonChange(value)
                }
              }}
            >
              <SelectTrigger
                id="feedback-correct-classification-reason"
                className={SELECT_TRIGGER_CLASS}
              >
                <SelectValue placeholder={copy.selectPlaceholder} />
              </SelectTrigger>
              <SelectContent
                position="popper"
                align="start"
                className={FEEDBACK_DIALOG_SELECT_MENU_CLASS}
              >
                <SelectGroup className={FEEDBACK_DIALOG_SELECT_GROUP_CLASS}>
                  {FEEDBACK_CLASSIFICATION_CORRECTION_REASONS.map((option) => (
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

          <div className="flex flex-col gap-2">
            <Label
              htmlFor="feedback-correct-classification-note"
              className="font-semibold leading-5 text-op-text-primary"
            >
              {copy.noteLabel}
            </Label>
            <Textarea
              id="feedback-correct-classification-note"
              value={correction.draftNote}
              disabled={saving}
              rows={4}
              placeholder={copy.notePlaceholder}
              onChange={(event) => {
                onNoteDraftChange(event.target.value)
              }}
              className="min-h-0 resize-none rounded-[4px] border-op-input-border bg-transparent px-[13px] py-[15px] text-sm text-op-text-primary placeholder:text-[var(--op-color-gray-550)]"
            />
          </div>

          <p className={CALLOUT_CLASS} role="status">
            {copy.callout}
          </p>

          {correction.saveError != null ? (
            <p
              role="alert"
              className="m-0 text-sm font-medium text-[var(--op-color-red-550)]"
            >
              {correction.saveError}
            </p>
          ) : null}

          <div className={DIVIDER_CLASS} aria-hidden />
        </div>

        <DialogFooter className="flex-row gap-3 sm:justify-start">
          <Button
            type="button"
            variant="op-primary"
            disabled={!correction.canSave || saving}
            aria-disabled={!correction.canSave || saving}
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
            {copy.cancelLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

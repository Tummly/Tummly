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
  FeedbackDetailsCloseOutEditor,
  FeedbackDetailsLoaded,
} from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import {
  FEEDBACK_CLOSE_OUT_REASONS,
  feedbackCloseOutDialogCopy,
  type FeedbackCloseOutReason,
} from "@/lib/operatorFeedback/feedbackCloseOutPresentation"
import {
  FEEDBACK_DIALOG_SELECT_GROUP_CLASS,
  FEEDBACK_DIALOG_SELECT_ITEM_CLASS,
  FEEDBACK_DIALOG_SELECT_MENU_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import { feedbackSentimentLabel } from "@/lib/operatorHome/feedbackSentimentLabel"

type FeedbackCloseOutDialogProps = {
  closeOut: FeedbackDetailsCloseOutEditor
  details: FeedbackDetailsLoaded | null
  onOpenChange: (open: boolean) => void
  onReasonChange: (reason: FeedbackCloseOutReason) => void
  onNoteDraftChange: (value: string) => void
  onConfirm: () => void
}

const REASON_TRIGGER_CLASS =
  "h-auto min-h-[50px] w-full rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary shadow-none dark:bg-transparent dark:hover:bg-transparent"

const DIVIDER_CLASS = "h-px w-full shrink-0 bg-op-border-default"

/** Close-out feedback dialog — aligned with FeedbackExportDialog chrome. */
export function FeedbackCloseOutDialog({
  closeOut,
  details,
  onOpenChange,
  onReasonChange,
  onNoteDraftChange,
  onConfirm,
}: FeedbackCloseOutDialogProps) {
  if (!closeOut.isOpen || closeOut.intent == null) {
    return null
  }

  const copy = feedbackCloseOutDialogCopy(closeOut.intent)
  const saving = closeOut.saveStatus === "saving"

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
              <DialogDescription className="text-sm font-medium leading-normal text-[var(--op-color-gray-550)]">
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

          {details != null ? (
            <>
              <dl className="m-0 flex flex-col gap-4 text-sm">
                <div className="flex items-center justify-between gap-4">
                  <dt className="font-medium text-[var(--op-color-gray-550)]">
                    Guest
                  </dt>
                  <dd className="m-0 font-medium text-op-text-primary">
                    {details.guestName}
                  </dd>
                </div>
                <div className="flex items-center justify-between gap-4">
                  <dt className="font-medium text-[var(--op-color-gray-550)]">
                    Classification
                  </dt>
                  <dd className="m-0">
                    {details.sentiment != null ? (
                      <Badge variant={details.sentiment}>
                        {feedbackSentimentLabel(details.sentiment)}
                      </Badge>
                    ) : (
                      <span className="font-medium text-op-text-primary">—</span>
                    )}
                  </dd>
                </div>
                <div className="flex items-center justify-between gap-4">
                  <dt className="font-medium text-[var(--op-color-gray-550)]">
                    Location
                  </dt>
                  <dd className="m-0 font-medium text-op-text-primary">
                    {details.locationName}
                  </dd>
                </div>
                <div className="flex items-center justify-between gap-4">
                  <dt className="font-medium text-[var(--op-color-gray-550)]">
                    Feedback reference
                  </dt>
                  <dd className="m-0 font-medium text-op-text-primary">
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
                <SelectValue placeholder="Select a reason" />
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
                className="font-semibold leading-5 text-op-text-primary"
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
                className="min-h-0 resize-none rounded-[4px] border-op-input-border bg-transparent px-[13px] py-[15px] text-sm text-op-text-primary placeholder:text-[var(--op-color-gray-550)]"
              />
            </div>
          ) : null}

          {closeOut.saveError != null ? (
            <p
              role="alert"
              className="m-0 text-sm font-medium text-[var(--op-color-red-550)]"
            >
              {closeOut.saveError}
            </p>
          ) : null}

          <div className={DIVIDER_CLASS} aria-hidden />
        </div>

        <DialogFooter className="flex-row gap-3 sm:justify-start">
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

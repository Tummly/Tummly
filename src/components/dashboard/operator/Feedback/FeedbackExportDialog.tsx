import { XIcon } from "lucide-react"

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
import { cn } from "@/lib/utils"
import type { OperatorFeedbackExportDialogSnapshot } from "@/lib/operatorFeedback/createOperatorFeedbackPageModule"
import {
  FEEDBACK_DIALOG_SELECT_GROUP_CLASS,
  FEEDBACK_DIALOG_SELECT_ITEM_CLASS,
  FEEDBACK_DIALOG_SELECT_MENU_CLASS,
  FEEDBACK_PAGE_COPY,
} from "@/lib/operatorFeedback/feedbackPresentation"
import type {
  FeedbackExportFormat,
  FeedbackExportScope,
} from "@/lib/operatorFeedback/feedbackExportQueryParams"

type FeedbackExportDialogProps = {
  dialog: OperatorFeedbackExportDialogSnapshot
  onOpenChange: (open: boolean) => void
  onScopeChange: (scope: FeedbackExportScope) => void
  onFormatChange: (format: FeedbackExportFormat) => void
  onIncludeGuestContactChange: (include: boolean) => void
  onDownload: () => void
}

const SCOPE_CARD_CLASS =
  "h-auto w-full flex-col items-start gap-1 rounded-op-md border border-op-border-default bg-transparent px-[18px] py-4 text-left font-normal shadow-none hover:bg-transparent disabled:bg-transparent"
const SCOPE_CARD_SELECTED_CLASS =
  "border-[var(--op-color-gray-550)]"

const FORMAT_TRIGGER_CLASS =
  "h-auto min-h-[50px] w-full rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary shadow-none dark:bg-transparent dark:hover:bg-transparent"

const DIVIDER_CLASS = "h-px w-full shrink-0 bg-op-border-default"

/** Export feedback dialog — Figma `4481:17214`. */
export function FeedbackExportDialog({
  dialog,
  onOpenChange,
  onScopeChange,
  onFormatChange,
  onIncludeGuestContactChange,
  onDownload,
}: FeedbackExportDialogProps) {
  const copy = FEEDBACK_PAGE_COPY.exportDialog
  const formatSummary =
    dialog.format === "csv" ? copy.formatCsvShort : copy.formatExcelShort
  const contactSummary = dialog.includeGuestContact
    ? copy.contactIncluded
    : copy.contactNotIncluded

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
                disabled={dialog.isPreparing}
              >
                <XIcon aria-hidden />
              </Button>
            </DialogClose>
          </div>

          <div
            role="radiogroup"
            aria-label="Export scope"
            className="flex flex-col gap-3"
          >
            <Button
              type="button"
              variant="op-secondary"
              role="radio"
              aria-checked={dialog.scope === "current"}
              disabled={dialog.isPreparing}
              className={cn(
                SCOPE_CARD_CLASS,
                dialog.scope === "current" && SCOPE_CARD_SELECTED_CLASS
              )}
              onClick={() => {
                onScopeChange("current")
              }}
            >
              <span className="text-sm font-medium text-op-text-primary">
                {copy.scopeCurrentTitle}
              </span>
              <span className="text-sm font-medium text-[var(--op-color-gray-550)]">
                {copy.scopeCurrentHelper(dialog.currentResultsCount)}
              </span>
            </Button>
            <Button
              type="button"
              variant="op-secondary"
              role="radio"
              aria-checked={dialog.scope === "all-in-period"}
              disabled={dialog.isPreparing}
              className={cn(
                SCOPE_CARD_CLASS,
                dialog.scope === "all-in-period" && SCOPE_CARD_SELECTED_CLASS
              )}
              onClick={() => {
                onScopeChange("all-in-period")
              }}
            >
              <span className="text-sm font-medium text-op-text-primary">
                {copy.scopeAllTitle}
              </span>
              <span className="text-sm font-medium text-[var(--op-color-gray-550)]">
                {copy.scopeAllHelper(
                  dialog.allInPeriodCount,
                  dialog.locationName,
                  dialog.periodLabel
                )}
              </span>
            </Button>
          </div>

          <div className="flex flex-col gap-2">
            <Label
              htmlFor="feedback-export-format"
              className="font-semibold leading-5 text-op-text-primary"
            >
              {copy.fileFormatLabel}
            </Label>
            <Select
              value={dialog.format}
              disabled={dialog.isPreparing}
              onValueChange={(value) => {
                if (value === "csv" || value === "xlsx") {
                  onFormatChange(value)
                }
              }}
            >
              <SelectTrigger
                id="feedback-export-format"
                className={FORMAT_TRIGGER_CLASS}
              >
                <SelectValue />
              </SelectTrigger>
              <SelectContent
                position="popper"
                align="start"
                className={FEEDBACK_DIALOG_SELECT_MENU_CLASS}
              >
                <SelectGroup className={FEEDBACK_DIALOG_SELECT_GROUP_CLASS}>
                  <SelectItem
                    value="xlsx"
                    className={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
                  >
                    {copy.formatExcel}
                  </SelectItem>
                  <SelectItem
                    value="csv"
                    className={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
                  >
                    {copy.formatCsv}
                  </SelectItem>
                </SelectGroup>
              </SelectContent>
            </Select>
          </div>

          <div className={DIVIDER_CLASS} aria-hidden />

          <dl className="m-0 flex flex-col gap-4 text-sm">
            <div className="flex items-center justify-between gap-4">
              <dt className="font-medium text-[var(--op-color-gray-550)]">
                {copy.summaryLocation}
              </dt>
              <dd className="m-0 font-medium text-op-text-primary">
                {dialog.locationName}
              </dd>
            </div>
            <div className="flex items-center justify-between gap-4">
              <dt className="font-medium text-[var(--op-color-gray-550)]">
                {copy.summaryPeriod}
              </dt>
              <dd className="m-0 font-medium text-op-text-primary">
                {dialog.periodLabel}
              </dd>
            </div>
            <div className="flex items-center justify-between gap-4">
              <dt className="font-medium text-[var(--op-color-gray-550)]">
                {copy.summaryItems}
              </dt>
              <dd className="m-0 font-medium text-op-text-primary">
                {dialog.selectedCount}
              </dd>
            </div>
            <div className="flex items-center justify-between gap-4">
              <dt className="font-medium text-[var(--op-color-gray-550)]">
                {copy.summaryFormat}
              </dt>
              <dd className="m-0 font-medium text-op-text-primary">
                {formatSummary}
              </dd>
            </div>
            <div className="flex items-center justify-between gap-4">
              <dt className="font-medium text-[var(--op-color-gray-550)]">
                {copy.summaryContact}
              </dt>
              <dd className="m-0 font-medium text-op-text-primary">
                {contactSummary}
              </dd>
            </div>
          </dl>

          <div className={DIVIDER_CLASS} aria-hidden />

          <div className="flex flex-col gap-0.5">
            <CheckboxLabel
              checked={dialog.includeGuestContact}
              disabled={dialog.isPreparing}
              labelClassName="text-sm font-medium leading-normal text-op-text-primary"
              onCheckedChange={(checked) => {
                onIncludeGuestContactChange(checked)
              }}
            >
              {copy.includeContactLabel}
            </CheckboxLabel>
            <p className="m-0 pl-7 text-xs font-medium leading-normal text-[var(--op-color-gray-550)]">
              {copy.includeContactHelper}
            </p>
          </div>

          {dialog.errorMessage != null ? (
            <p
              role="alert"
              className="m-0 text-sm font-medium text-[var(--op-color-red-550)]"
            >
              {dialog.errorMessage}
            </p>
          ) : null}

          <div className={DIVIDER_CLASS} aria-hidden />
        </div>

        <DialogFooter className="flex-row gap-3 sm:justify-start">
          <Button
            type="button"
            variant="op-primary"
            disabled={!dialog.canDownload}
            aria-disabled={!dialog.canDownload}
            onClick={() => {
              onDownload()
            }}
          >
            {dialog.isPreparing ? (
              <>
                <Spinner size="sm" data-icon="inline-start" />
                {copy.preparing}
              </>
            ) : (
              copy.download
            )}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            disabled={dialog.isPreparing}
            onClick={() => {
              onOpenChange(false)
            }}
          >
            {copy.cancel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

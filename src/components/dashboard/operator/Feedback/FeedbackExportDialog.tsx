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
  "h-auto w-full flex-col items-start gap-1 rounded-[10px] border border-op-border-default bg-op-background-secondary p-4 text-left font-normal shadow-none hover:bg-op-background-secondary"
const SCOPE_CARD_SELECTED_CLASS =
  "border-op-text-primary ring-1 ring-op-text-primary"

/** Export feedback dialog — Figma scopes, formats, soft-max, empty disable. */
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
        className="flex max-h-[min(90vh,720px)] w-full max-w-[560px] flex-col gap-0 overflow-hidden rounded-[16px] border-op-border-default bg-op-surface-secondary p-0 text-op-text-primary sm:max-w-[560px]"
      >
        <div className="flex items-start justify-between gap-3 px-6 pt-6">
          <DialogHeader className="min-w-0 flex-1 gap-2 text-left">
            <DialogTitle className="text-2xl font-bold tracking-normal text-op-text-primary">
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
              className="size-9 shrink-0"
              disabled={dialog.isPreparing}
            >
              <XIcon aria-hidden />
            </Button>
          </DialogClose>
        </div>

        <div className="flex flex-col gap-5 overflow-y-auto px-6 py-5">
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
              <span className="text-sm font-semibold text-op-text-primary">
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
              <span className="text-sm font-semibold text-op-text-primary">
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
            <Label htmlFor="feedback-export-format">{copy.fileFormatLabel}</Label>
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
                className="w-full"
              >
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectGroup>
                  <SelectItem value="xlsx">{copy.formatExcel}</SelectItem>
                  <SelectItem value="csv">{copy.formatCsv}</SelectItem>
                </SelectGroup>
              </SelectContent>
            </Select>
          </div>

          <div className="flex flex-col gap-1">
            <CheckboxLabel
              checked={dialog.includeGuestContact}
              disabled={dialog.isPreparing}
              onCheckedChange={(checked) => {
                onIncludeGuestContactChange(checked)
              }}
            >
              {copy.includeContactLabel}
            </CheckboxLabel>
            <p className="m-0 pl-7 text-sm font-medium text-[var(--op-color-gray-550)]">
              {copy.includeContactHelper}
            </p>
          </div>

          <dl className="m-0 grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
            <dt className="font-medium text-[var(--op-color-gray-550)]">
              {copy.summaryLocation}
            </dt>
            <dd className="m-0 font-semibold text-op-text-primary">
              {dialog.locationName}
            </dd>
            <dt className="font-medium text-[var(--op-color-gray-550)]">
              {copy.summaryPeriod}
            </dt>
            <dd className="m-0 font-semibold text-op-text-primary">
              {dialog.periodLabel}
            </dd>
            <dt className="font-medium text-[var(--op-color-gray-550)]">
              {copy.summaryItems}
            </dt>
            <dd className="m-0 font-semibold text-op-text-primary">
              {dialog.selectedCount}
            </dd>
            <dt className="font-medium text-[var(--op-color-gray-550)]">
              {copy.summaryFormat}
            </dt>
            <dd className="m-0 font-semibold text-op-text-primary">
              {formatSummary}
            </dd>
            <dt className="font-medium text-[var(--op-color-gray-550)]">
              {copy.summaryContact}
            </dt>
            <dd className="m-0 font-semibold text-op-text-primary">
              {contactSummary}
            </dd>
          </dl>

          {dialog.errorMessage != null ? (
            <p
              role="alert"
              className="m-0 text-sm font-medium text-[var(--op-color-red-550)]"
            >
              {dialog.errorMessage}
            </p>
          ) : null}
        </div>

        <DialogFooter className="flex-row gap-3 border-t border-op-border-default px-6 py-4 sm:justify-start">
          <Button
            type="button"
            variant="op-secondary"
            disabled={dialog.isPreparing}
            onClick={() => {
              onOpenChange(false)
            }}
          >
            {copy.cancel}
          </Button>
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
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

import { XIcon } from "lucide-react"

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
import { ACCOUNT_WORKSPACE_PAGE_COPY } from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import type { AccountWorkspaceGuestDataExportFormat } from "@/lib/operatorAccountWorkspace/createOperatorAccountWorkspacePageModule"
import {
  FEEDBACK_DIALOG_BODY_CLASS,
  FEEDBACK_DIALOG_CONTENT_CLASS,
  FEEDBACK_DIALOG_DESCRIPTION_CLASS,
  FEEDBACK_DIALOG_FOOTER_CLASS,
  FEEDBACK_DIALOG_HEADER_ROW_CLASS,
  FEEDBACK_DIALOG_SELECT_GROUP_CLASS,
  FEEDBACK_DIALOG_SELECT_ITEM_CLASS,
  FEEDBACK_DIALOG_SELECT_MENU_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"

const FORMAT_TRIGGER_CLASS =
  "h-auto min-h-[50px] w-full rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary shadow-none dark:bg-transparent dark:hover:bg-transparent"

type GuestDataExportDialogProps = {
  format: AccountWorkspaceGuestDataExportFormat
  isPreparing: boolean
  onOpenChange: (open: boolean) => void
  onFormatChange: (format: AccountWorkspaceGuestDataExportFormat) => void
  onDownload: () => void
}

export function GuestDataExportDialog({
  format,
  isPreparing,
  onOpenChange,
  onFormatChange,
  onDownload,
}: GuestDataExportDialogProps) {
  const copy = ACCOUNT_WORKSPACE_PAGE_COPY

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
        <div className={FEEDBACK_DIALOG_HEADER_ROW_CLASS}>
          <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
            <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
              {copy.exportGuestDataTitle}
            </DialogTitle>
            <DialogDescription className={FEEDBACK_DIALOG_DESCRIPTION_CLASS}>
              {copy.exportGuestDataSubtitle}
            </DialogDescription>
          </DialogHeader>
          <DialogClose asChild>
            <Button
              type="button"
              variant="op-collapse"
              aria-label="Close"
              className="shrink-0"
              disabled={isPreparing}
            >
              <XIcon aria-hidden />
            </Button>
          </DialogClose>
        </div>

        <div className={FEEDBACK_DIALOG_BODY_CLASS}>
          <div className="flex flex-col gap-2">
            <Label
              htmlFor="guest-data-export-format"
              className="font-semibold leading-5 text-op-text-primary"
            >
              {copy.exportGuestDataFileFormat}
            </Label>
            <Select
              value={format}
              disabled={isPreparing}
              onValueChange={(value) => {
                if (value === "csv" || value === "xlsx") {
                  onFormatChange(value)
                }
              }}
            >
              <SelectTrigger
                id="guest-data-export-format"
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
                    {copy.exportGuestDataFormatExcel}
                  </SelectItem>
                  <SelectItem
                    value="csv"
                    className={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
                  >
                    {copy.exportGuestDataFormatCsv}
                  </SelectItem>
                </SelectGroup>
              </SelectContent>
            </Select>
          </div>
        </div>

        <DialogFooter className={FEEDBACK_DIALOG_FOOTER_CLASS}>
          <Button
            type="button"
            variant="op-primary"
            disabled={isPreparing}
            onClick={() => {
              onDownload()
            }}
          >
            {isPreparing ? (
              <>
                <Spinner size="sm" data-icon="inline-start" />
                {copy.exportGuestDataPreparing}
              </>
            ) : (
              copy.exportGuestDataDownload
            )}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            disabled={isPreparing}
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

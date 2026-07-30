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
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  CAPTURE_DIALOG_CLOSE_BUTTON_CLASS,
  CAPTURE_DIALOG_HEADER_ROW_CLASS,
  CAPTURE_DIALOG_SELECT_GROUP_CLASS,
  CAPTURE_DIALOG_SELECT_ITEM_CLASS,
  CAPTURE_DIALOG_SELECT_LABEL_CLASS,
  CAPTURE_DIALOG_SELECT_MENU_CLASS,
  CAPTURE_GUEST_PREVIEW_PICKER_DIALOG_CONTENT_CLASS,
  OPERATOR_CAPTURE_GUEST_PREVIEW_PICKER_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import type { GuestExperiencePreviewPickerSnapshot } from "@/lib/operatorCapture/createOperatorCapturePageModule"

type CaptureGuestExperiencePreviewPickerDialogProps = {
  picker: GuestExperiencePreviewPickerSnapshot
  onOpenChange: (open: boolean) => void
  onSelectOption: (qrCodeId: number | null) => void
  onConfirm: () => void
}

/** Guest experience Preview picker — Figma `4439:54464` with grilling-10 copy. */
export function CaptureGuestExperiencePreviewPickerDialog({
  picker,
  onOpenChange,
  onSelectOption,
  onConfirm,
}: CaptureGuestExperiencePreviewPickerDialogProps) {
  const copy = OPERATOR_CAPTURE_GUEST_PREVIEW_PICKER_COPY
  const selectedValue =
    picker.selectedQrCodeId != null ? String(picker.selectedQrCodeId) : undefined

  return (
    <Dialog
      open={picker.isOpen}
      onOpenChange={(open) => {
        onOpenChange(open)
      }}
    >
      <DialogContent
        showCloseButton={false}
        className={CAPTURE_GUEST_PREVIEW_PICKER_DIALOG_CONTENT_CLASS}
      >
        <div className="flex flex-col gap-5">
          <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
            <DialogHeader className="min-w-0 flex-1 gap-3">
              <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
                {copy.title}
              </DialogTitle>
              <DialogDescription className="max-w-[376px] text-sm font-medium leading-normal text-[var(--op-color-gray-550)]">
                {copy.description}
              </DialogDescription>
            </DialogHeader>
            <DialogClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                aria-label="Close"
                className={CAPTURE_DIALOG_CLOSE_BUTTON_CLASS}
              >
                <XIcon aria-hidden />
              </Button>
            </DialogClose>
          </div>

          <div className="flex flex-col gap-2">
            <span className="text-sm font-semibold leading-5 text-op-text-primary">
              {copy.fieldLabel}
            </span>
            <Select
              value={selectedValue}
              onValueChange={(value) => {
                const nextId = Number(value)
                if (!Number.isFinite(nextId)) {
                  return
                }
                onSelectOption(nextId)
              }}
            >
              <SelectTrigger className="h-auto min-h-[50px] w-full rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm shadow-none dark:bg-transparent dark:hover:bg-transparent">
                <SelectValue placeholder={copy.placeholder} />
              </SelectTrigger>
              <SelectContent
                position="popper"
                align="start"
                className={CAPTURE_DIALOG_SELECT_MENU_CLASS}
              >
                {picker.groups.map((group) => (
                  <SelectGroup
                    key={group.id}
                    className={CAPTURE_DIALOG_SELECT_GROUP_CLASS}
                  >
                    <SelectLabel className={CAPTURE_DIALOG_SELECT_LABEL_CLASS}>
                      {group.label}
                    </SelectLabel>
                    {group.options.map((option) => (
                      <SelectItem
                        key={option.qrCodeId}
                        value={String(option.qrCodeId)}
                        className={CAPTURE_DIALOG_SELECT_ITEM_CLASS}
                      >
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectGroup>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        <DialogFooter className="flex-row gap-3 sm:justify-start">
          <Button
            type="button"
            variant="op-primary"
            disabled={!picker.canConfirm}
            onClick={onConfirm}
          >
            {copy.confirmCta}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            onClick={() => {
              onOpenChange(false)
            }}
          >
            {copy.cancelCta}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

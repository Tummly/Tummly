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
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { CAMPAIGN_SEND_TEST_COPY, CAMPAIGN_SEND_TEST_DIALOG_CONTENT_CLASS, CAMPAIGN_SEND_TEST_DIALOG_OVERLAY_CLASS } from "@/lib/operatorCampaigns/campaignSendTestPresentation"
import type { CampaignSendTestDialogViewModel } from "@/lib/operatorCampaigns/createCampaignWizardModule"
import { FEEDBACK_FIELD_LABEL_CLASS } from "@/lib/operatorFeedback/feedbackPresentation"
import { cn } from "@/lib/utils"

type CampaignSendTestEmailDialogProps = {
  sendTest: CampaignSendTestDialogViewModel
  onOpenChange: (open: boolean) => void
  onEmailChange: (value: string) => void
  onConfirm: () => void
}

const EMAIL_INPUT_CLASS =
  "h-auto min-h-[50px] rounded-[4px] border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary placeholder:text-op-input-placeholder shadow-none dark:bg-transparent"

/**
 * Campaign Send test email dialog — Figma 4752:71297 / ticket 24.
 * Opens above Guest preview; transactional Resend only (no credit burn).
 */
export function CampaignSendTestEmailDialog({
  sendTest,
  onOpenChange,
  onEmailChange,
  onConfirm,
}: CampaignSendTestEmailDialogProps) {
  if (!sendTest.isOpen) {
    return null
  }

  const sending = sendTest.status === "sending"

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (sending && !open) {
          return
        }
        onOpenChange(open)
      }}
    >
      <DialogContent
        showCloseButton={false}
        overlayClassName={CAMPAIGN_SEND_TEST_DIALOG_OVERLAY_CLASS}
        className={CAMPAIGN_SEND_TEST_DIALOG_CONTENT_CLASS}
      >
        <div className="flex items-start gap-[22px]">
          <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
            <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
              {CAMPAIGN_SEND_TEST_COPY.dialogTitle}
            </DialogTitle>
            <DialogDescription className="text-sm font-medium leading-normal text-[var(--op-color-gray-550)]">
              {CAMPAIGN_SEND_TEST_COPY.dialogDescription}
            </DialogDescription>
          </DialogHeader>
          <DialogClose asChild>
            <Button
              type="button"
              variant="op-collapse"
              aria-label="Close"
              className="shrink-0"
              disabled={sending}
            >
              <XIcon aria-hidden />
            </Button>
          </DialogClose>
        </div>

        <div className="flex flex-col gap-2">
          <Label
            htmlFor="campaign-send-test-email"
            className={cn(FEEDBACK_FIELD_LABEL_CLASS, "font-semibold")}
          >
            {CAMPAIGN_SEND_TEST_COPY.emailLabel}
          </Label>
          <Input
            id="campaign-send-test-email"
            type="email"
            autoComplete="email"
            value={sendTest.email}
            placeholder={CAMPAIGN_SEND_TEST_COPY.emailPlaceholder}
            disabled={sending}
            onChange={(event) => {
              onEmailChange(event.target.value)
            }}
            className={EMAIL_INPUT_CLASS}
          />
          {sendTest.error != null ? (
            <p className="m-0 text-sm font-medium text-destructive">
              {sendTest.error}
            </p>
          ) : null}
        </div>

        <DialogFooter className="gap-3 sm:justify-end">
          <Button
            type="button"
            variant="op-secondary"
            disabled={sending}
            onClick={() => {
              onOpenChange(false)
            }}
          >
            {CAMPAIGN_SEND_TEST_COPY.cancelLabel}
          </Button>
          <Button
            type="button"
            variant="op-primary"
            disabled={!sendTest.canSubmit}
            onClick={onConfirm}
          >
            {CAMPAIGN_SEND_TEST_COPY.confirmLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

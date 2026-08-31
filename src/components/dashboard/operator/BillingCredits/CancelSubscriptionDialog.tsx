import { XIcon } from "lucide-react"
import { Link } from "react-router-dom"

import { useBillingCreditsPageModuleApi } from "@/components/dashboard/operator/BillingCredits/utils/billingCreditsPageModuleContext"
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
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
import {
  ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS,
  ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS,
  ACCOUNT_WORKSPACE_SELECT_MENU_CLASS,
  ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import {
  CAPTURE_DIALOG_CLOSE_BUTTON_CLASS,
  CAPTURE_DIALOG_HEADER_ROW_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import { FEEDBACK_TEXTAREA_CLASS } from "@/lib/operatorFeedback/feedbackPresentation"
import {
  CANCEL_PLAN_REASON_OPTIONS,
  CANCEL_SUBSCRIPTION_DIALOG_COPY,
  createInitialCancelPlanDialogState,
  isCancelPlanDialogReady,
  type CancelPlanReason,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import { cn } from "@/lib/utils"

export function CancelSubscriptionDialog({
  cancelPlanConfirm,
  pageModule,
}: {
  cancelPlanConfirm: ReturnType<
    ReturnType<typeof useBillingCreditsPageModuleApi>["getSnapshot"]
  >["cancelPlanConfirm"]
  pageModule: ReturnType<typeof useBillingCreditsPageModuleApi>
}) {
  const open = cancelPlanConfirm?.open ?? false
  const state = cancelPlanConfirm ?? createInitialCancelPlanDialogState()
  const busy = state.busy
  const canContinue = isCancelPlanDialogReady(state)

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (busy) {
          return
        }
        if (!next) {
          pageModule.cancelCancelPlan()
        }
      }}
    >
      <DialogContent
        showCloseButton={false}
        className="gap-15 rounded-op-md bg-op-surface-secondary p-8 text-op-text-primary sm:max-w-[792px]"
      >
        <div className="flex flex-col gap-10">
          <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
            <DialogHeader className="min-w-0 flex-1 gap-3">
              <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
                {CANCEL_SUBSCRIPTION_DIALOG_COPY.title}
              </DialogTitle>
              <DialogDescription asChild className="max-w-none">
                <div className="flex w-full max-w-none flex-col gap-3 text-base font-medium leading-[21px] text-op-text-muted">
                  <p>{CANCEL_SUBSCRIPTION_DIALOG_COPY.bodyPrimary}</p>
                  <p>{CANCEL_SUBSCRIPTION_DIALOG_COPY.bodySecondary}</p>
                </div>
              </DialogDescription>
            </DialogHeader>
            <DialogClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                disabled={busy}
                aria-label="Close"
                className={CAPTURE_DIALOG_CLOSE_BUTTON_CLASS}
                onClick={() => pageModule.cancelCancelPlan()}
              >
                <XIcon aria-hidden />
              </Button>
            </DialogClose>
          </div>

          <div className="flex flex-col gap-5">
            <div className="flex flex-col gap-2">
              <Label className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}>
                {CANCEL_SUBSCRIPTION_DIALOG_COPY.reasonLabel}
              </Label>
              <Select
                value={state.reason === "" ? undefined : state.reason}
                disabled={busy}
                onValueChange={(value) => {
                  pageModule.setCancelPlanReason(value as CancelPlanReason)
                }}
              >
                <SelectTrigger
                  className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
                >
                  <SelectValue
                    placeholder={CANCEL_SUBSCRIPTION_DIALOG_COPY.reasonPlaceholder}
                  />
                </SelectTrigger>
                <SelectContent
                  position="popper"
                  align="start"
                  className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
                >
                  {CANCEL_PLAN_REASON_OPTIONS.map((option) => (
                    <SelectItem
                      key={option.value}
                      value={option.value}
                      className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
                    >
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="flex min-h-49 flex-col gap-2">
              <Label className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}>
                {CANCEL_SUBSCRIPTION_DIALOG_COPY.notesLabel}
              </Label>
              <Textarea
                value={state.additionalNotes}
                disabled={busy}
                placeholder={CANCEL_SUBSCRIPTION_DIALOG_COPY.notesPlaceholder}
                className={cn(
                  FEEDBACK_TEXTAREA_CLASS,
                  "min-h-35 flex-1 resize-none"
                )}
                onChange={(event) => {
                  pageModule.setCancelPlanAdditionalNotes(event.target.value)
                }}
              />
            </div>
          </div>

          <CheckboxLabel
            className="w-full"
            checked={state.acknowledged}
            disabled={busy}
            labelClassName="text-sm font-medium leading-normal text-op-text-muted"
            onCheckedChange={(checked) => {
              pageModule.setCancelPlanAcknowledged(checked === true)
            }}
          >
            {CANCEL_SUBSCRIPTION_DIALOG_COPY.acknowledgment}
          </CheckboxLabel>
        </div>

        <DialogFooter className="flex flex-row flex-wrap justify-start gap-3 sm:justify-start">
          <Button
            type="button"
            variant="op-primary"
            disabled={busy}
            onClick={() => pageModule.cancelCancelPlan()}
          >
            {CANCEL_SUBSCRIPTION_DIALOG_COPY.keepSubscription}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            disabled={busy || !canContinue}
            onClick={() => {
              void pageModule.confirmCancelPlan()
            }}
          >
            {CANCEL_SUBSCRIPTION_DIALOG_COPY.continueCancellation}
          </Button>
          <Button type="button" variant="op-tertiary" disabled={busy} asChild>
            <Link to={HELP_CENTRE_CONTACT_URL}>
              {CANCEL_SUBSCRIPTION_DIALOG_COPY.contactSupport}
            </Link>
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

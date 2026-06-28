import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import type { AdminTrialRequest } from "@/types/admin"

export const ADMIN_FEEDBACK_MAX_LENGTH = 2000

export type TrialRequestFeedbackKind = "decline" | "more-info"

const FEEDBACK_COPY: Record<
  TrialRequestFeedbackKind,
  {
    title: string
    description: (request: AdminTrialRequest) => string
    label: string
    placeholder: string
    confirmLabel: string
    variant: "default" | "destructive-solid"
  }
> = {
  decline: {
    title: "Decline trial request?",
    description: (request) =>
      `This declines ${request.businessName} and notifies ${request.email}. Declined requests cannot be approved again.`,
    label: "Reason for declining",
    placeholder: "Explain why this trial request cannot be approved…",
    confirmLabel: "Decline",
    variant: "destructive-solid",
  },
  "more-info": {
    title: "Request more info?",
    description: (request) =>
      `This emails ${request.email} asking for more information about ${request.businessName}.`,
    label: "Information needed",
    placeholder: "Describe what the applicant should provide or clarify…",
    confirmLabel: "Send email",
    variant: "default",
  },
}

type TrialRequestFeedbackDialogProps = {
  kind: TrialRequestFeedbackKind
  request: AdminTrialRequest
  message: string
  onMessageChange: (value: string) => void
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: () => void
  disabled?: boolean
}

export function TrialRequestFeedbackDialog({
  kind,
  request,
  message,
  onMessageChange,
  open,
  onOpenChange,
  onConfirm,
  disabled = false,
}: TrialRequestFeedbackDialogProps) {
  const copy = FEEDBACK_COPY[kind]
  const trimmedMessage = message.trim()
  const canConfirm =
    trimmedMessage.length > 0 &&
    trimmedMessage.length <= ADMIN_FEEDBACK_MAX_LENGTH &&
    !disabled

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent showCloseButton={!disabled} className="rounded-2xl sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{copy.title}</DialogTitle>
          <DialogDescription>{copy.description(request)}</DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-2">
          <Label htmlFor={`trial-feedback-${kind}`}>{copy.label}</Label>
          <Textarea
            id={`trial-feedback-${kind}`}
            value={message}
            onChange={(event) => onMessageChange(event.target.value)}
            placeholder={copy.placeholder}
            maxLength={ADMIN_FEEDBACK_MAX_LENGTH}
            rows={5}
            disabled={disabled}
            aria-invalid={
              message.length > 0 && trimmedMessage.length === 0
                ? true
                : undefined
            }
          />
          <p className="text-sm text-muted-foreground">
            Required · max {ADMIN_FEEDBACK_MAX_LENGTH.toLocaleString()} characters
          </p>
        </div>

        <DialogFooter className="flex-row justify-end gap-3 sm:flex-row">
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={disabled}
          >
            Cancel
          </Button>
          <Button
            type="button"
            variant={copy.variant}
            onClick={onConfirm}
            disabled={!canConfirm}
          >
            {copy.confirmLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

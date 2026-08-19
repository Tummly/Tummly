import { AiIcon } from "@/components/ui/ai-icon"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import {
  FEEDBACK_FIELD_LABEL_CLASS,
  FEEDBACK_INPUT_CLASS,
  FEEDBACK_TEXTAREA_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import type { PrepareRecoveryDraftMode } from "@/lib/operatorFeedback/createRespondToGuestModule"
import {
  GUEST_RESPONSE_REWRITE_AI_LABEL,
  GUEST_RESPONSE_REWRITE_RETRY_LABEL,
} from "@/lib/operatorFeedback/guestResponseChooserPresentation"
import type { RespondToGuestChannel } from "@/lib/operatorFeedback/respondToGuestPresentation"
import { cn } from "@/lib/utils"

export type GuestResponseWriteFieldsProps = {
  idPrefix: string
  channel: RespondToGuestChannel | null
  subject: string
  message: string
  disabled?: boolean
  aiDraftStatus: "idle" | "running" | "failed"
  aiDraftMode: PrepareRecoveryDraftMode | null
  aiDraftRetryable: boolean
  onSubjectChange: (value: string) => void
  onMessageChange: (value: string) => void
  onRewriteSubject: () => void
  onRewriteMessage: () => void
  onRetryAiDraft: () => void
}

function RewriteAiButton({
  busy,
  failed,
  disabled,
  onClick,
}: {
  busy: boolean
  failed: boolean
  disabled: boolean
  onClick: () => void
}) {
  return (
    <Button
      type="button"
      variant="op-secondary"
      disabled={disabled}
      onClick={onClick}
      className="gap-2 px-[14px] py-2"
    >
      <AiIcon size={18} className={busy ? "animate-spin" : undefined} />
      {failed
        ? GUEST_RESPONSE_REWRITE_RETRY_LABEL
        : GUEST_RESPONSE_REWRITE_AI_LABEL}
    </Button>
  )
}

/** Subject + Message editors with per-field Rewrite with AI (Figma Guest response). */
export function GuestResponseWriteFields({
  idPrefix,
  channel,
  subject,
  message,
  disabled = false,
  aiDraftStatus,
  aiDraftMode,
  aiDraftRetryable,
  onSubjectChange,
  onMessageChange,
  onRewriteSubject,
  onRewriteMessage,
  onRetryAiDraft,
}: GuestResponseWriteFieldsProps) {
  const running = aiDraftStatus === "running"
  const subjectBusy = running && aiDraftMode === "rewrite_subject"
  const messageBusy = running && aiDraftMode === "rewrite_message"
  const showSubjectRetry =
    aiDraftStatus === "failed"
    && aiDraftRetryable
    && aiDraftMode === "rewrite_subject"
  const showMessageRetry =
    aiDraftStatus === "failed"
    && aiDraftRetryable
    && aiDraftMode === "rewrite_message"

  return (
    <div className="flex w-full flex-col gap-3">
      {channel === "email" ? (
        <div className="flex flex-col gap-3">
          <div className="flex items-end gap-3">
            <label
              htmlFor={`${idPrefix}-subject`}
              className={cn(FEEDBACK_FIELD_LABEL_CLASS, "flex-1")}
            >
              Subject
            </label>
            <RewriteAiButton
              busy={subjectBusy}
              failed={showSubjectRetry}
              disabled={disabled}
              onClick={showSubjectRetry ? onRetryAiDraft : onRewriteSubject}
            />
          </div>
          <Input
            id={`${idPrefix}-subject`}
            value={subject}
            disabled={disabled}
            onChange={(event) => {
              onSubjectChange(event.target.value)
            }}
            className={cn(FEEDBACK_INPUT_CLASS, "h-12")}
          />
        </div>
      ) : null}

      <div className="flex flex-col gap-3">
        <div className="flex items-end gap-3">
          <label
            htmlFor={`${idPrefix}-message`}
            className={cn(FEEDBACK_FIELD_LABEL_CLASS, "flex-1")}
          >
            Message
          </label>
          <RewriteAiButton
            busy={messageBusy}
            failed={showMessageRetry}
            disabled={disabled}
            onClick={showMessageRetry ? onRetryAiDraft : onRewriteMessage}
          />
        </div>
        <Textarea
          id={`${idPrefix}-message`}
          value={message}
          disabled={disabled}
          onChange={(event) => {
            onMessageChange(event.target.value)
          }}
          className={cn(FEEDBACK_TEXTAREA_CLASS, "min-h-[220px]")}
        />
      </div>
    </div>
  )
}

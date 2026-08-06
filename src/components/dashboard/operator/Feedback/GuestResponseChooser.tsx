import { CoinsIcon } from "lucide-react"

import { AiIcon } from "@/components/ui/ai-icon"
import { Button } from "@/components/ui/button"
import {
  GUEST_RESPONSE_AI_ACTION_METERING_LABEL,
  GUEST_RESPONSE_PREPARE_ACTION_LABEL,
  GUEST_RESPONSE_PREPARE_DESCRIPTION,
  GUEST_RESPONSE_PREPARE_TITLE,
  GUEST_RESPONSE_WRITE_MANUAL_ACTION_LABEL,
  GUEST_RESPONSE_WRITE_MANUAL_DESCRIPTION,
  GUEST_RESPONSE_WRITE_MANUAL_TITLE,
} from "@/lib/operatorFeedback/guestResponseChooserPresentation"

type GuestResponseChooserProps = {
  disabled?: boolean
  aiDraftFailed: boolean
  aiDraftRetryable: boolean
  onPrepareDraft: () => void
  onWriteManually: () => void
  onRetryAiDraft: () => void
}

/** Figma option cards — Prepare with AI / Write manually (U-06). */
export function GuestResponseChooser({
  disabled = false,
  aiDraftFailed,
  aiDraftRetryable,
  onPrepareDraft,
  onWriteManually,
  onRetryAiDraft,
}: GuestResponseChooserProps) {
  if (aiDraftFailed) {
    return (
      <div className="flex flex-wrap gap-3">
        {aiDraftRetryable ? (
          <Button
            type="button"
            variant="op-primary"
            disabled={disabled}
            onClick={onRetryAiDraft}
          >
            Try again
          </Button>
        ) : null}
        <Button
          type="button"
          variant="op-secondary"
          disabled={disabled}
          onClick={onWriteManually}
        >
          {GUEST_RESPONSE_WRITE_MANUAL_TITLE}
        </Button>
      </div>
    )
  }

  return (
    <div className="flex w-full flex-col gap-[18px]">
      <div className="flex w-full flex-col gap-[22px] rounded-[4px] border border-op-card-border bg-op-background-secondary p-[18px]">
        <div className="flex flex-col gap-1">
          <p className="text-base font-medium text-op-text-primary">
            {GUEST_RESPONSE_PREPARE_TITLE}
          </p>
          <p className="text-sm font-medium text-op-text-muted">
            {GUEST_RESPONSE_PREPARE_DESCRIPTION}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-[18px]">
          <Button
            type="button"
            variant="op-secondary"
            disabled={disabled}
            onClick={onPrepareDraft}
          >
            <AiIcon size={18} />
            {GUEST_RESPONSE_PREPARE_ACTION_LABEL}
          </Button>
          <span className="flex items-center gap-2 text-xs font-medium text-op-text-muted">
            <CoinsIcon className="size-4 shrink-0" aria-hidden />
            {GUEST_RESPONSE_AI_ACTION_METERING_LABEL}
          </span>
        </div>
      </div>

      <div className="flex w-full flex-col gap-[22px] rounded-[4px] border border-op-card-border bg-op-background-secondary p-[18px]">
        <div className="flex flex-col gap-1">
          <p className="text-base font-medium text-op-text-primary">
            {GUEST_RESPONSE_WRITE_MANUAL_TITLE}
          </p>
          <p className="text-sm font-medium text-op-text-muted">
            {GUEST_RESPONSE_WRITE_MANUAL_DESCRIPTION}
          </p>
        </div>
        <div>
          <Button
            type="button"
            variant="op-secondary"
            disabled={disabled}
            onClick={onWriteManually}
          >
            {GUEST_RESPONSE_WRITE_MANUAL_ACTION_LABEL}
          </Button>
        </div>
      </div>
    </div>
  )
}

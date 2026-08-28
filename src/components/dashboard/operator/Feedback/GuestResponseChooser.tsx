import { CoinsIcon } from "lucide-react"
import { Link } from "react-router-dom"

import { AiIcon } from "@/components/ui/ai-icon"
import { Button } from "@/components/ui/button"
import {
  GUEST_RESPONSE_AI_ACTION_METERING_LABEL,
  GUEST_RESPONSE_CHOOSER_CARD_CLASS,
  GUEST_RESPONSE_PREPARE_ACTION_LABEL,
  GUEST_RESPONSE_PREPARE_DESCRIPTION,
  GUEST_RESPONSE_PREPARE_TITLE,
  GUEST_RESPONSE_WRITE_MANUAL_ACTION_LABEL,
  GUEST_RESPONSE_WRITE_MANUAL_DESCRIPTION,
  GUEST_RESPONSE_WRITE_MANUAL_TITLE,
} from "@/lib/operatorFeedback/guestResponseChooserPresentation"
import type { RecoveryAiActionChipChrome } from "@/lib/operatorFeedback/recoveryCreditChromePresentation"

type GuestResponseChooserProps = {
  disabled?: boolean
  aiDraftFailed: boolean
  aiDraftRetryable: boolean
  aiActionChip?: RecoveryAiActionChipChrome
  onPrepareDraft: () => void
  onWriteManually: () => void
  onRetryAiDraft: () => void
}

/** Figma option cards — Prepare with AI / Write manually (U-06). */
export function GuestResponseChooser({
  disabled = false,
  aiDraftFailed,
  aiDraftRetryable,
  aiActionChip,
  onPrepareDraft,
  onWriteManually,
  onRetryAiDraft,
}: GuestResponseChooserProps) {
  const prepareAllowed = aiActionChip?.prepareAllowed ?? true
  const writeManuallyAllowed = aiActionChip?.writeManuallyAllowed ?? true
  const prepareDisabled = disabled || !prepareAllowed

  if (aiDraftFailed) {
    return (
      <div className="flex flex-wrap gap-3">
        {aiDraftRetryable ? (
          <Button
            type="button"
            variant="op-primary"
            disabled={prepareDisabled}
            onClick={onRetryAiDraft}
          >
            Try again
          </Button>
        ) : null}
        <Button
          type="button"
          variant="op-secondary"
          disabled={disabled || !writeManuallyAllowed}
          onClick={onWriteManually}
        >
          {GUEST_RESPONSE_WRITE_MANUAL_TITLE}
        </Button>
      </div>
    )
  }

  return (
    <div className="flex w-full flex-col gap-[18px]">
      <div className={GUEST_RESPONSE_CHOOSER_CARD_CLASS}>
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
            disabled={prepareDisabled}
            onClick={onPrepareDraft}
          >
            <AiIcon size={18} />
            {GUEST_RESPONSE_PREPARE_ACTION_LABEL}
          </Button>
          {aiActionChip?.showMeteringChip !== false ? (
            <span className="flex items-center gap-2 text-xs font-medium text-op-text-muted">
              <CoinsIcon className="size-4 shrink-0" aria-hidden />
              {GUEST_RESPONSE_AI_ACTION_METERING_LABEL}
            </span>
          ) : null}
        </div>
        {aiActionChip?.depletedMessage != null ? (
          <div className="flex flex-col gap-2">
            <p className="m-0 text-sm font-medium text-op-text-muted">
              {aiActionChip.depletedMessage}
            </p>
            <div className="flex flex-wrap items-center gap-3">
              {aiActionChip.buyCta != null ? (
                <Button
                  type="button"
                  variant="op-link"
                  className="h-auto min-h-0 w-fit p-0"
                  asChild
                >
                  <Link to={aiActionChip.buyCta.href}>
                    {aiActionChip.buyCta.label}
                  </Link>
                </Button>
              ) : null}
              {aiActionChip.changePlanCta != null ? (
                <Button
                  type="button"
                  variant="op-link"
                  className="h-auto min-h-0 w-fit p-0"
                  asChild
                >
                  <Link to={aiActionChip.changePlanCta.href}>
                    {aiActionChip.changePlanCta.label}
                  </Link>
                </Button>
              ) : null}
            </div>
          </div>
        ) : null}
      </div>

      <div className={GUEST_RESPONSE_CHOOSER_CARD_CLASS}>
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
            disabled={disabled || !writeManuallyAllowed}
            onClick={onWriteManually}
          >
            {GUEST_RESPONSE_WRITE_MANUAL_ACTION_LABEL}
          </Button>
        </div>
      </div>
    </div>
  )
}

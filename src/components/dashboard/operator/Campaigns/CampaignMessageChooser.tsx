import { CoinsIcon } from "lucide-react"
import { Link } from "react-router-dom"

import { AiIcon } from "@/components/ui/ai-icon"
import { Button } from "@/components/ui/button"
import { CAMPAIGN_MESSAGE_COPY } from "@/lib/operatorCampaigns/campaignMessagePresentation"

type CampaignMessageChooserProps = {
  /** When true, hide the Write manually card (editor path — Figma shows Prepare only). */
  editorMode?: boolean
  /** True when the live message-draft adapter is wired. */
  prepareAiLive?: boolean
  /** Soft-lock / AI credits / balances gate (ticket 25 / 24 shared chip). */
  aiPrepareAllowed?: boolean
  aiPrepareBlockReason?: string | null
  buyAiCreditsHref?: string | null
  changePlanHref?: string | null
  /** Prepare failed while still on the chooser — show Try again. */
  aiDraftFailed?: boolean
  aiDraftRetryable?: boolean
  disabled?: boolean
  onPrepareDraft: () => void
  onWriteManually: () => void
  onRetryAiDraft: () => void
}

/**
 * Campaign Message chooser — Prepare with AI / Write manually.
 * Same card pattern as GuestResponseChooser; Campaign-owned copy (tickets 26 + 33).
 * Shared Uses 1 AI action chip rules: ticket 24 / lock 09.
 */
export function CampaignMessageChooser({
  editorMode = false,
  prepareAiLive = false,
  aiPrepareAllowed = true,
  aiPrepareBlockReason = null,
  buyAiCreditsHref = null,
  changePlanHref = null,
  aiDraftFailed = false,
  aiDraftRetryable = true,
  disabled = false,
  onPrepareDraft,
  onWriteManually,
  onRetryAiDraft,
}: CampaignMessageChooserProps) {
  const prepareDisabled = !prepareAiLive || !aiPrepareAllowed || disabled
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
            {CAMPAIGN_MESSAGE_COPY.retryAiLabel}
          </Button>
        ) : null}
        <Button
          type="button"
          variant="op-secondary"
          disabled={disabled}
          onClick={onWriteManually}
        >
          {CAMPAIGN_MESSAGE_COPY.writeManualTitle}
        </Button>
      </div>
    )
  }

  return (
    <div className="flex w-full flex-col gap-[18px]">
      <div className="flex w-full flex-col gap-[22px] rounded-[4px] border border-op-card-border bg-op-background-secondary p-[18px]">
        <div className="flex flex-col gap-1">
          <p className="text-base font-medium text-op-text-primary">
            {CAMPAIGN_MESSAGE_COPY.prepareTitle}
          </p>
          <p className="text-sm font-medium text-op-text-muted">
            {CAMPAIGN_MESSAGE_COPY.prepareDescription}
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
            {CAMPAIGN_MESSAGE_COPY.prepareActionLabel}
          </Button>
          <span className="flex items-center gap-2 text-xs font-medium text-op-text-muted">
            <CoinsIcon className="size-4 shrink-0" aria-hidden />
            {CAMPAIGN_MESSAGE_COPY.aiActionMeteringLabel}
          </span>
        </div>
        {aiPrepareBlockReason != null ? (
          <div className="flex flex-col gap-2">
            <p className="m-0 text-sm font-medium text-op-text-muted">
              {aiPrepareBlockReason}
            </p>
            <div className="flex flex-wrap items-center gap-3">
              {buyAiCreditsHref != null ? (
                <Button
                  type="button"
                  variant="op-link"
                  className="h-auto min-h-0 w-fit p-0"
                  asChild
                >
                  <Link to={buyAiCreditsHref}>Buy AI credits</Link>
                </Button>
              ) : null}
              {changePlanHref != null ? (
                <Button
                  type="button"
                  variant="op-link"
                  className="h-auto min-h-0 w-fit p-0"
                  asChild
                >
                  <Link to={changePlanHref}>Change plan</Link>
                </Button>
              ) : null}
            </div>
          </div>
        ) : null}
      </div>

      {editorMode ? null : (
        <div className="flex w-full flex-col gap-[22px] rounded-[4px] border border-op-card-border bg-op-background-secondary p-[18px]">
          <div className="flex flex-col gap-1">
            <p className="text-base font-medium text-op-text-primary">
              {CAMPAIGN_MESSAGE_COPY.writeManualTitle}
            </p>
            <p className="text-sm font-medium text-op-text-muted">
              {CAMPAIGN_MESSAGE_COPY.writeManualDescription}
            </p>
          </div>
          <div>
            <Button
              type="button"
              variant="op-secondary"
              disabled={disabled}
              onClick={onWriteManually}
            >
              {CAMPAIGN_MESSAGE_COPY.writeManualActionLabel}
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}

import { CoinsIcon } from "lucide-react"

import { AiIcon } from "@/components/ui/ai-icon"
import { Button } from "@/components/ui/button"
import { CAMPAIGN_MESSAGE_COPY } from "@/lib/operatorCampaigns/campaignMessagePresentation"

type CampaignMessageChooserProps = {
  /** When true, hide the Write manually card (editor path — Figma shows Prepare only). */
  editorMode?: boolean
  /** False until ticket 33 — Prepare does not call a live endpoint. */
  prepareAiLive?: boolean
  /** Prepare stays a non-calling stub until ticket 33. */
  onPrepareDraft: () => void
  onWriteManually: () => void
}

/**
 * Campaign Message chooser — Prepare with AI / Write manually.
 * Same card pattern as GuestResponseChooser; Campaign-owned copy (ticket 26).
 */
export function CampaignMessageChooser({
  editorMode = false,
  prepareAiLive = false,
  onPrepareDraft,
  onWriteManually,
}: CampaignMessageChooserProps) {
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
            disabled={!prepareAiLive}
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

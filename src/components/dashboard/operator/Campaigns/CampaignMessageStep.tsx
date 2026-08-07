import { Loader2Icon } from "lucide-react"

import { CampaignMessageChooser } from "@/components/dashboard/operator/Campaigns/CampaignMessageChooser"
import { GuestPreviewOverlay } from "@/components/dashboard/operator/Feedback/GuestPreviewOverlay"
import { AiIcon } from "@/components/ui/ai-icon"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Separator } from "@/components/ui/separator"
import { Textarea } from "@/components/ui/textarea"
import {
  CAMPAIGN_MESSAGE_COPY,
  CAMPAIGN_MESSAGE_FIELD_LABEL_CLASS,
  CAMPAIGN_MESSAGE_INPUT_CLASS,
  CAMPAIGN_MESSAGE_TEXTAREA_CLASS,
} from "@/lib/operatorCampaigns/campaignMessagePresentation"
import type { CampaignMessageViewModel } from "@/lib/operatorCampaigns/createCampaignWizardModule"
import { cn } from "@/lib/utils"

type CampaignMessageStepProps = {
  message: CampaignMessageViewModel
  onPrepareDraft: () => void
  onWriteManually: () => void
  onSubjectChange: (value: string) => void
  onBodyChange: (value: string) => void
  onRewriteSubject: () => void
  onRewriteMessage: () => void
  onRetryAiDraft: () => void
  onOpenGuestPreview: () => void
  onCloseGuestPreview: () => void
}

function RewriteAiButton({
  busy,
  disabled,
  onClick,
}: {
  busy: boolean
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
      {busy ? (
        <Loader2Icon className="size-[18px] animate-spin" aria-hidden />
      ) : (
        <AiIcon size={18} />
      )}
      {CAMPAIGN_MESSAGE_COPY.rewriteWithAiLabel}
    </Button>
  )
}

function EstimatedUsageSummary({
  message,
}: {
  message: CampaignMessageViewModel
}) {
  const { usageSummary } = message

  return (
    <aside
      className="flex w-full shrink-0 flex-col gap-6 rounded-[4px] border border-op-card-border bg-op-background-primary p-5 lg:w-[min(100%,560px)]"
      aria-label={usageSummary.title}
    >
      <div className="flex flex-col gap-2">
        <h3 className="m-0 text-lg font-semibold leading-normal text-op-text-primary">
          {usageSummary.title}
        </h3>
        <p className="m-0 text-sm font-medium leading-normal text-[var(--op-color-gray-550)]">
          {usageSummary.audienceLine}
        </p>
      </div>
      <dl className="m-0 flex w-full flex-col gap-3.5">
        {usageSummary.rows.map((row, index) => (
          <div key={row.label} className="flex flex-col gap-3.5">
            <div className="flex items-center justify-between gap-3 text-sm">
              <dt className="m-0 font-semibold text-[var(--op-color-gray-550)]">
                {row.label}
              </dt>
              <dd className="m-0 font-medium text-op-text-primary">{row.value}</dd>
            </div>
            {index < usageSummary.rows.length - 1 ? (
              <div className="h-px w-full bg-op-card-border" aria-hidden />
            ) : null}
          </div>
        ))}
      </dl>
    </aside>
  )
}

/**
 * Campaign wizard Message step — Figma 4747:66343 / tickets 26 + 33.
 * Chooser + live AI prepare/rewrite + Write manually + GuestPreviewOverlay (Send test off).
 */
export function CampaignMessageStep({
  message,
  onPrepareDraft,
  onWriteManually,
  onSubjectChange,
  onBodyChange,
  onRewriteSubject,
  onRewriteMessage,
  onRetryAiDraft,
  onOpenGuestPreview,
  onCloseGuestPreview,
}: CampaignMessageStepProps) {
  const isEditor = message.writeEntry === "editor"
  const running = message.aiDraftStatus === "running"
  const fieldsDisabled = running
  const subjectBusy =
    running && message.aiDraftMode === "rewrite_subject"
  const messageBusy =
    running && message.aiDraftMode === "rewrite_message"
  const showSubjectRetry =
    message.aiDraftStatus === "failed"
    && message.aiDraftRetryable
    && message.aiDraftMode === "rewrite_subject"
  const showMessageRetry =
    message.aiDraftStatus === "failed"
    && message.aiDraftRetryable
    && message.aiDraftMode === "rewrite_message"
  const showPrepareRetry =
    message.aiDraftStatus === "failed"
    && message.aiDraftRetryable
    && message.aiDraftMode === "prepare"

  return (
    <>
      <div className="flex w-full flex-col items-start justify-between gap-8 lg:flex-row lg:gap-[42px]">
        <div className="flex min-h-0 w-full max-w-[690px] flex-col gap-7">
          <header className="flex flex-col gap-2">
            <h2 className="m-0 text-xl font-semibold leading-normal text-op-text-primary sm:text-[22px]">
              {message.stepHeading}
            </h2>
            <p className="m-0 max-w-[425px] text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
              {message.stepDescription}
            </p>
          </header>

          <CampaignMessageChooser
            editorMode={isEditor}
            prepareAiLive={message.prepareAiLive}
            aiDraftFailed={
              !isEditor
              && message.aiDraftStatus === "failed"
              && message.aiDraftMode === "prepare"
            }
            aiDraftRetryable={message.aiDraftRetryable}
            disabled={fieldsDisabled}
            onPrepareDraft={onPrepareDraft}
            onWriteManually={onWriteManually}
            onRetryAiDraft={onRetryAiDraft}
          />

          {isEditor ? (
            <>
              <Separator className="bg-op-card-border" />
              <div className="flex w-full flex-col gap-6">
                {message.showSubject ? (
                  <div className="flex flex-col gap-3">
                    <div className="flex items-center gap-3">
                      <label
                        htmlFor="campaign-message-subject"
                        className={cn(
                          CAMPAIGN_MESSAGE_FIELD_LABEL_CLASS,
                          "flex-1"
                        )}
                      >
                        {CAMPAIGN_MESSAGE_COPY.subjectLabel}
                      </label>
                      {message.prepareAiLive ? (
                        <>
                          <RewriteAiButton
                            busy={subjectBusy}
                            disabled={fieldsDisabled}
                            onClick={onRewriteSubject}
                          />
                          {showSubjectRetry ? (
                            <Button
                              type="button"
                              variant="op-primary"
                              size="sm"
                              disabled={fieldsDisabled}
                              onClick={onRetryAiDraft}
                            >
                              {CAMPAIGN_MESSAGE_COPY.retryAiLabel}
                            </Button>
                          ) : null}
                        </>
                      ) : null}
                    </div>
                    <Input
                      id="campaign-message-subject"
                      value={message.subject}
                      disabled={fieldsDisabled}
                      onChange={(event) => {
                        onSubjectChange(event.target.value)
                      }}
                      className={cn(CAMPAIGN_MESSAGE_INPUT_CLASS, "h-12")}
                    />
                  </div>
                ) : null}

                <div className="flex flex-col gap-3">
                  <div className="flex items-center gap-3">
                    <label
                      htmlFor="campaign-message-body"
                      className={cn(
                        CAMPAIGN_MESSAGE_FIELD_LABEL_CLASS,
                        "flex-1"
                      )}
                    >
                      {CAMPAIGN_MESSAGE_COPY.messageLabel}
                    </label>
                    {message.prepareAiLive ? (
                      <>
                        <RewriteAiButton
                          busy={messageBusy}
                          disabled={fieldsDisabled}
                          onClick={onRewriteMessage}
                        />
                        {showMessageRetry || showPrepareRetry ? (
                          <Button
                            type="button"
                            variant="op-primary"
                            size="sm"
                            disabled={fieldsDisabled}
                            onClick={onRetryAiDraft}
                          >
                            {CAMPAIGN_MESSAGE_COPY.retryAiLabel}
                          </Button>
                        ) : null}
                      </>
                    ) : null}
                  </div>
                  <Textarea
                    id="campaign-message-body"
                    value={message.body}
                    disabled={fieldsDisabled}
                    onChange={(event) => {
                      onBodyChange(event.target.value)
                    }}
                    className={cn(
                      CAMPAIGN_MESSAGE_TEXTAREA_CLASS,
                      "min-h-[220px]"
                    )}
                  />
                </div>

                <div>
                  <Button
                    type="button"
                    variant="op-secondary"
                    disabled={fieldsDisabled}
                    onClick={onOpenGuestPreview}
                  >
                    {CAMPAIGN_MESSAGE_COPY.previewControlLabel}
                  </Button>
                </div>
              </div>
            </>
          ) : null}
        </div>

        <EstimatedUsageSummary message={message} />
      </div>

      <GuestPreviewOverlay
        open={message.guestPreviewOpen}
        channel={message.channelId}
        subject={message.subject}
        message={message.body}
        locationName={message.locationName}
        locationAddress={null}
        onClose={onCloseGuestPreview}
        onEditText={onCloseGuestPreview}
        sendTestDisabled={!message.sendTestAvailable}
      />
    </>
  )
}

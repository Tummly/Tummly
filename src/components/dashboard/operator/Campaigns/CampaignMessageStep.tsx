import { CampaignMessageChooser } from "@/components/dashboard/operator/Campaigns/CampaignMessageChooser"
import { GuestPreviewOverlay } from "@/components/dashboard/operator/Feedback/GuestPreviewOverlay"
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
  onOpenGuestPreview: () => void
  onCloseGuestPreview: () => void
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
 * Campaign wizard Message step — Figma 4747:66343 / ticket 26.
 * Chooser + Write manually editor + GuestPreviewOverlay (Send test off).
 */
export function CampaignMessageStep({
  message,
  onPrepareDraft,
  onWriteManually,
  onSubjectChange,
  onBodyChange,
  onOpenGuestPreview,
  onCloseGuestPreview,
}: CampaignMessageStepProps) {
  const isEditor = message.writeEntry === "editor"

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
            onPrepareDraft={onPrepareDraft}
            onWriteManually={onWriteManually}
          />

          {isEditor ? (
            <>
              <Separator className="bg-op-card-border" />
              <div className="flex w-full flex-col gap-6">
                {message.showSubject ? (
                  <div className="flex flex-col gap-2">
                    <label
                      htmlFor="campaign-message-subject"
                      className={CAMPAIGN_MESSAGE_FIELD_LABEL_CLASS}
                    >
                      {CAMPAIGN_MESSAGE_COPY.subjectLabel}
                    </label>
                    <Input
                      id="campaign-message-subject"
                      value={message.subject}
                      onChange={(event) => {
                        onSubjectChange(event.target.value)
                      }}
                      className={cn(CAMPAIGN_MESSAGE_INPUT_CLASS, "h-12")}
                    />
                  </div>
                ) : null}

                <div className="flex flex-col gap-2">
                  <label
                    htmlFor="campaign-message-body"
                    className={CAMPAIGN_MESSAGE_FIELD_LABEL_CLASS}
                  >
                    {CAMPAIGN_MESSAGE_COPY.messageLabel}
                  </label>
                  <Textarea
                    id="campaign-message-body"
                    value={message.body}
                    onChange={(event) => {
                      onBodyChange(event.target.value)
                    }}
                    className={cn(CAMPAIGN_MESSAGE_TEXTAREA_CLASS, "min-h-[220px]")}
                  />
                </div>

                <div>
                  <Button
                    type="button"
                    variant="op-secondary"
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

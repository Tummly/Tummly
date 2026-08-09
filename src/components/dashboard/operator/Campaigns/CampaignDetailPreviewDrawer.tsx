import { CalendarIcon } from "lucide-react"

import { CampaignDetailPreviewShell } from "@/components/dashboard/operator/Campaigns/CampaignDetailPreviewShell"
import { GuestPreviewEmailChrome } from "@/components/dashboard/operator/Feedback/GuestPreviewOverlay"
import { Button } from "@/components/ui/button"
import {
  CAMPAIGN_DETAIL_PREVIEW_COPY,
} from "@/lib/operatorCampaigns/campaignDetailPreviewPresentation"
import {
  CAMPAIGN_TEMPLATE_PREVIEW_FIELD_LABEL_CLASS,
  CAMPAIGN_TEMPLATE_PREVIEW_FIELD_VALUE_CLASS,
  CAMPAIGN_TEMPLATE_PREVIEW_SECTION_CLASS,
  CAMPAIGN_TEMPLATE_PREVIEW_SECTION_TITLE_CLASS,
} from "@/lib/operatorCampaigns/campaignTemplatePreviewPresentation"
import type {
  CampaignDetailPreviewSnapshot,
  CampaignDetailPreviewViewModel,
} from "@/lib/operatorCampaigns/createCampaignDetailPreviewModule"
import type { CampaignTemplatePreviewChannelId } from "@/types/operatorCampaigns"
import { cn } from "@/lib/utils"

type CampaignDetailPreviewDrawerProps = {
  snapshot: CampaignDetailPreviewSnapshot
  brandName?: string | null
  locationName?: string | null
  locationAddress?: string | null
  onOpenChange: (open: boolean) => void
  onRetry: () => void
  onSelectChannel: (channelId: CampaignTemplatePreviewChannelId) => void
}

function SummaryField({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1.5">
      <p className={CAMPAIGN_TEMPLATE_PREVIEW_FIELD_LABEL_CLASS}>{label}</p>
      <p className={CAMPAIGN_TEMPLATE_PREVIEW_FIELD_VALUE_CLASS}>{value}</p>
    </div>
  )
}

/**
 * Campaign Detail / Campaign Preview adapter on shared Detail chrome.
 * Figma 5116:19403 — not catalogue template Preview, not Guest preview overlay.
 */
export function CampaignDetailPreviewDrawer({
  snapshot,
  brandName = null,
  locationName = null,
  locationAddress = null,
  onOpenChange,
  onRetry,
  onSelectChannel,
}: CampaignDetailPreviewDrawerProps) {
  const copy = CAMPAIGN_DETAIL_PREVIEW_COPY
  const viewModel = snapshot.viewModel

  return (
    <CampaignDetailPreviewShell
      open={snapshot.open}
      title={viewModel?.title ?? "Campaign"}
      subtitle={viewModel?.subtitle ?? copy.subtitle}
      closeAriaLabel={copy.closeAriaLabel}
      loadStatus={snapshot.loadStatus}
      loadError={snapshot.loadError ?? copy.loadError}
      retryLabel={copy.retry}
      footerDisclaimer={viewModel?.footerDisclaimer ?? null}
      primaryActionLabel={null}
      secondaryActionLabel={viewModel?.closeLabel ?? null}
      onOpenChange={onOpenChange}
      onRetry={onRetry}
    >
      {viewModel != null ? (
        <PreviewBody
          viewModel={viewModel}
          brandName={brandName}
          locationName={locationName}
          locationAddress={locationAddress}
          onSelectChannel={onSelectChannel}
        />
      ) : null}
    </CampaignDetailPreviewShell>
  )
}

function PreviewBody({
  viewModel,
  brandName,
  locationName,
  locationAddress,
  onSelectChannel,
}: {
  viewModel: CampaignDetailPreviewViewModel
  brandName: string | null
  locationName: string | null
  locationAddress: string | null
  onSelectChannel: (channelId: CampaignTemplatePreviewChannelId) => void
}) {
  const copy = CAMPAIGN_DETAIL_PREVIEW_COPY
  const summary = viewModel.summary
  const message = viewModel.activeMessage
  const isSms = viewModel.selectedChannelId === "sms"

  return (
    <>
      <section className={CAMPAIGN_TEMPLATE_PREVIEW_SECTION_CLASS}>
        <h2 className={CAMPAIGN_TEMPLATE_PREVIEW_SECTION_TITLE_CLASS}>
          {copy.campaignSummary}
        </h2>
        <div className="flex w-full flex-col gap-[22px]">
          <SummaryField label={copy.goal} value={summary.goal} />
          <SummaryField label={copy.audience} value={summary.audience} />
          <SummaryField label={copy.channel} value={summary.channel} />
          <SummaryField label={copy.offer} value={summary.offer} />
        </div>
      </section>

      <section className={CAMPAIGN_TEMPLATE_PREVIEW_SECTION_CLASS}>
        <h2 className={CAMPAIGN_TEMPLATE_PREVIEW_SECTION_TITLE_CLASS}>
          {copy.guestMessage}
        </h2>

        {viewModel.channelTabs.length > 0 ? (
          <div
            className="flex items-center"
            role="tablist"
            aria-label={copy.guestMessage}
          >
            {viewModel.channelTabs.map((tab) => {
              const selected = tab.id === viewModel.selectedChannelId
              return (
                <Button
                  key={tab.id}
                  type="button"
                  role="tab"
                  aria-selected={selected}
                  variant={selected ? "op-secondary" : "op-ghost"}
                  className={cn(
                    "rounded-[2px] px-4 py-2.5 text-sm font-medium",
                    !selected && "text-[var(--op-color-gray-700)]"
                  )}
                  onClick={() => {
                    onSelectChannel(tab.id)
                  }}
                >
                  {tab.label}
                </Button>
              )
            })}
          </div>
        ) : null}

        <div className="relative min-h-[320px] w-full overflow-clip rounded-[4px] bg-[var(--op-color-gray-1000)]">
          {message == null ? (
            <div className="flex items-center justify-center p-6">
              <p className={CAMPAIGN_TEMPLATE_PREVIEW_FIELD_VALUE_CLASS}>
                {copy.emptyValue}
              </p>
            </div>
          ) : isSms ? (
            <div className="flex flex-col items-center gap-4 p-6">
              <div className="mx-auto w-full max-w-[360px] rounded-[4px] border border-[var(--op-color-gray-980)] bg-[var(--op-color-gray-995)] p-6">
                <p className="m-0 whitespace-pre-wrap text-sm font-medium leading-5 text-[var(--op-color-white)]">
                  {message.body}
                </p>
              </div>
            </div>
          ) : (
            <div className="flex justify-center px-4 py-6">
              <GuestPreviewEmailChrome
                brandName={brandName}
                locationName={locationName}
                locationAddress={locationAddress}
                subject={message.subject ?? ""}
                message={message.body}
                className="w-full max-w-[474px]"
              />
            </div>
          )}
        </div>
      </section>

      {viewModel.showOfferLogic ? (
        <section className={CAMPAIGN_TEMPLATE_PREVIEW_SECTION_CLASS}>
          <h2 className={CAMPAIGN_TEMPLATE_PREVIEW_SECTION_TITLE_CLASS}>
            {copy.offerLogic}
          </h2>
          <div className="flex w-full flex-col gap-[22px]">
            {viewModel.offerLogic.map((row) => (
              <SummaryField
                key={`${row.label}-${row.value}`}
                label={row.label}
                value={row.value}
              />
            ))}
          </div>
        </section>
      ) : null}

      <section className={CAMPAIGN_TEMPLATE_PREVIEW_SECTION_CLASS}>
        <h2 className={CAMPAIGN_TEMPLATE_PREVIEW_SECTION_TITLE_CLASS}>
          {copy.sendLogic}
        </h2>
        <div className="flex w-full items-center gap-2.5 rounded-[4px] border border-op-card-border px-[18px] py-4">
          <div className="flex shrink-0 items-center rounded-[2px] bg-[var(--op-color-gray-1000)] p-2.5">
            <CalendarIcon
              className="size-4 text-op-text-primary"
              aria-hidden
            />
          </div>
          <p className="m-0 text-sm font-medium leading-normal text-op-text-primary">
            {viewModel.sendLogicLabel}
          </p>
        </div>
      </section>
    </>
  )
}

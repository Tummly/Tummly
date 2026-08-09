import { CalendarIcon } from "lucide-react"

import { CampaignDetailPreviewShell } from "@/components/dashboard/operator/Campaigns/CampaignDetailPreviewShell"
import { GuestPreviewEmailChrome } from "@/components/dashboard/operator/Feedback/GuestPreviewOverlay"
import { GuestPreviewOfferCoupon } from "@/components/dashboard/operator/Feedback/GuestPreviewOfferCoupon"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  CAMPAIGN_TEMPLATE_PREVIEW_COPY,
  CAMPAIGN_TEMPLATE_PREVIEW_FIELD_LABEL_CLASS,
  CAMPAIGN_TEMPLATE_PREVIEW_FIELD_VALUE_CLASS,
  CAMPAIGN_TEMPLATE_PREVIEW_SECTION_CLASS,
  CAMPAIGN_TEMPLATE_PREVIEW_SECTION_TITLE_CLASS,
} from "@/lib/operatorCampaigns/campaignTemplatePreviewPresentation"
import type {
  CampaignTemplatePreviewSnapshot,
  CampaignTemplatePreviewViewModel,
} from "@/lib/operatorCampaigns/createCampaignTemplatePreviewModule"
import { GUEST_PREVIEW_OFFER_COPY_LABEL } from "@/lib/operatorFeedback/guestPreviewPresentation"
import type { CampaignTemplatePreviewChannelId } from "@/types/operatorCampaigns"
import { cn } from "@/lib/utils"

type CampaignTemplatePreviewDrawerProps = {
  snapshot: CampaignTemplatePreviewSnapshot
  /** Live venue chrome for the embedded guest-message canvas. */
  brandName?: string | null
  locationName?: string | null
  locationAddress?: string | null
  onOpenChange: (open: boolean) => void
  onRetry: () => void
  onSelectChannel: (channelId: CampaignTemplatePreviewChannelId) => void
  onUseThisTemplate: () => void
}

function SummaryField({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1.5">
      <p className={CAMPAIGN_TEMPLATE_PREVIEW_FIELD_LABEL_CLASS}>{label}</p>
      <p className={CAMPAIGN_TEMPLATE_PREVIEW_FIELD_VALUE_CLASS}>{value}</p>
    </div>
  )
}

function EligibilityRow({
  label,
  value,
}: {
  label: string
  value: number
}) {
  return (
    <div className="flex w-full items-center justify-between gap-3">
      <p className={CAMPAIGN_TEMPLATE_PREVIEW_FIELD_VALUE_CLASS}>{label}</p>
      <p className={CAMPAIGN_TEMPLATE_PREVIEW_FIELD_LABEL_CLASS}>{value}</p>
    </div>
  )
}

/**
 * Catalogue Campaign template Preview adapter on shared Detail/Preview chrome.
 * Figma 5116:19403. Stacks above the template picker Dialog.
 */
export function CampaignTemplatePreviewDrawer({
  snapshot,
  brandName = null,
  locationName = null,
  locationAddress = null,
  onOpenChange,
  onRetry,
  onSelectChannel,
  onUseThisTemplate,
}: CampaignTemplatePreviewDrawerProps) {
  const copy = CAMPAIGN_TEMPLATE_PREVIEW_COPY
  const viewModel = snapshot.viewModel

  return (
    <CampaignDetailPreviewShell
      open={snapshot.open}
      title={viewModel?.title ?? "Campaign template"}
      subtitle={viewModel?.subtitle ?? copy.subtitle}
      closeAriaLabel={copy.closeAriaLabel}
      loadStatus={snapshot.loadStatus}
      loadError={snapshot.loadError ?? copy.loadError}
      retryLabel={copy.retry}
      footerDisclaimer={viewModel?.footerDisclaimer ?? null}
      primaryActionLabel={viewModel?.useThisTemplateLabel ?? null}
      secondaryActionLabel={viewModel?.closeLabel ?? null}
      onOpenChange={onOpenChange}
      onRetry={onRetry}
      onPrimaryAction={onUseThisTemplate}
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
  viewModel: CampaignTemplatePreviewViewModel
  brandName: string | null
  locationName: string | null
  locationAddress: string | null
  onSelectChannel: (channelId: CampaignTemplatePreviewChannelId) => void
}) {
  const copy = CAMPAIGN_TEMPLATE_PREVIEW_COPY
  const summary = viewModel.summary
  const message = viewModel.activeMessage
  const isSms = viewModel.selectedChannelId === "sms"
  const offerCoupon =
    message?.offerBlock != null
      ? {
          title: message.offerBlock.title,
          description: message.offerBlock.description,
          redemptionCode: message.offerBlock.redemptionCode,
          expiryLabel: message.offerBlock.expiryLabel,
          copyLabel: GUEST_PREVIEW_OFFER_COPY_LABEL,
        }
      : null

  return (
    <>
      <section className={CAMPAIGN_TEMPLATE_PREVIEW_SECTION_CLASS}>
        <h2 className={CAMPAIGN_TEMPLATE_PREVIEW_SECTION_TITLE_CLASS}>
          {copy.templateSummary}
        </h2>
        <div className="flex w-full flex-col gap-[22px]">
          <SummaryField label={copy.goal} value={summary.goal} />
          <SummaryField label={copy.bestFor} value={summary.bestFor} />
          <SummaryField
            label={copy.suggestedAudience}
            value={summary.suggestedAudience}
          />
          <SummaryField
            label={copy.suggestedChannel}
            value={summary.suggestedChannel}
          />
          <SummaryField label={copy.offer} value={summary.offer} />
        </div>
      </section>

      <section className={CAMPAIGN_TEMPLATE_PREVIEW_SECTION_CLASS}>
        <h2 className={CAMPAIGN_TEMPLATE_PREVIEW_SECTION_TITLE_CLASS}>
          {copy.exampleMessage}
        </h2>

        {viewModel.channelTabs.length > 0 ? (
          <div
            className="flex items-center"
            role="tablist"
            aria-label={copy.exampleMessage}
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

        <div className="flex w-full flex-col gap-4">
          {message != null ? (
            <div className="flex flex-wrap items-center gap-3">
              <p className={CAMPAIGN_TEMPLATE_PREVIEW_FIELD_LABEL_CLASS}>
                {copy.estimatedUsage}
              </p>
              <Badge variant="soft">{message.estimatedUsageLabel}</Badge>
            </div>
          ) : null}

          <div className="relative min-h-[320px] w-full overflow-clip rounded-[4px] bg-[var(--op-color-gray-1000)]">
            {message == null ? null : isSms ? (
              <div className="flex flex-col items-center gap-4 p-6">
                <div className="mx-auto w-full max-w-[360px] rounded-[4px] border border-[var(--op-color-gray-980)] bg-[var(--op-color-gray-995)] p-6">
                  <p className="m-0 whitespace-pre-wrap text-sm font-medium leading-5 text-[var(--op-color-white)]">
                    {message.body}
                  </p>
                </div>
                {offerCoupon != null ? (
                  <div className="w-full max-w-[360px]">
                    <GuestPreviewOfferCoupon coupon={offerCoupon} />
                  </div>
                ) : null}
              </div>
            ) : (
              <div className="flex justify-center px-4 py-6">
                <GuestPreviewEmailChrome
                  brandName={brandName}
                  locationName={locationName}
                  locationAddress={locationAddress}
                  subject={message.subject ?? ""}
                  message={message.body}
                  offerCoupon={
                    offerCoupon != null ? (
                      <GuestPreviewOfferCoupon coupon={offerCoupon} />
                    ) : null
                  }
                  className="w-full max-w-[474px]"
                />
              </div>
            )}
          </div>
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
          {copy.audienceEligibility}
        </h2>
        <div className="flex w-full flex-col gap-[22px]">
          <EligibilityRow
            label={copy.emailEligible}
            value={viewModel.eligibility.emailCount}
          />
          <EligibilityRow
            label={copy.smsEligible}
            value={viewModel.eligibility.smsCount}
          />
          <EligibilityRow
            label={copy.totalUniqueGuests}
            value={viewModel.eligibility.totalUniqueGuests}
          />
        </div>
      </section>

      <section className={CAMPAIGN_TEMPLATE_PREVIEW_SECTION_CLASS}>
        <h2 className={CAMPAIGN_TEMPLATE_PREVIEW_SECTION_TITLE_CLASS}>
          {copy.suggestedTiming}
        </h2>
        <div className="flex w-full items-center gap-2.5 rounded-[4px] border border-op-card-border px-[18px] py-4">
          <div className="flex shrink-0 items-center rounded-[2px] bg-[var(--op-color-gray-1000)] p-2.5">
            <CalendarIcon
              className="size-4 text-op-text-primary"
              aria-hidden
            />
          </div>
          <p className="m-0 text-sm font-medium leading-normal text-op-text-primary">
            {viewModel.suggestedTiming}
          </p>
        </div>
      </section>
    </>
  )
}

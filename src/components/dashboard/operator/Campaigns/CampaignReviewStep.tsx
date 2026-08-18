import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import { GuestPreviewOfferCoupon } from "@/components/dashboard/operator/Feedback/GuestPreviewOfferCoupon"
import { GuestPreviewPanel } from "@/components/dashboard/operator/Feedback/GuestPreviewPanel"
import type {
  CampaignReviewSectionViewModel,
  CampaignReviewViewModel,
} from "@/lib/operatorCampaigns/createCampaignWizardModule"
import { CAMPAIGN_REVIEW_GUEST_PREVIEW_RAIL_CLASS } from "@/lib/operatorCampaigns/campaignReviewPresentation"
import { cn } from "@/lib/utils"

type CampaignReviewStepProps = {
  review: CampaignReviewViewModel
  onOpenGuestPreview: () => void
  onCloseGuestPreview: () => void
  onEditMessage: () => void
  onSendTest: () => void
  sendTestBusy?: boolean
}

function ReviewSectionRows({
  section,
}: {
  section: CampaignReviewSectionViewModel
}) {
  if (section.rows.length === 0) {
    return null
  }

  return (
    <dl className="m-0 flex w-full flex-col gap-3">
      {section.rows.map((row) => (
        <div
          key={`${section.id}-${row.label}`}
          className="flex items-start justify-between gap-4 text-sm"
        >
          <dt className="m-0 shrink-0 font-semibold text-[var(--op-color-gray-550)]">
            {row.label}
          </dt>
          <dd className="m-0 min-w-0 whitespace-pre-wrap text-right font-medium text-op-text-primary">
            {row.value}
          </dd>
        </div>
      ))}
    </dl>
  )
}

/**
 * Campaign wizard Review step — Figma 4752:67706 / tickets 27 + 24.
 * Summary accordion + Guest preview + Send test. No send / schedule-commit path.
 */
export function CampaignReviewStep({
  review,
  onOpenGuestPreview,
  onCloseGuestPreview,
  onEditMessage,
  onSendTest,
  sendTestBusy = false,
}: CampaignReviewStepProps) {
  return (
    <div className="flex w-full flex-col items-start justify-between gap-8 lg:flex-row lg:gap-[42px]">
      <div className="flex min-h-0 w-full max-w-[690px] flex-col gap-7">
        <header className="flex flex-col gap-2">
          <h2 className="m-0 text-xl font-semibold leading-normal text-op-text-primary sm:text-[22px]">
            {review.stepHeading}
          </h2>
          <p className="m-0 max-w-[581px] text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
            {review.stepDescription}
          </p>
        </header>

        <Accordion
          type="multiple"
          className="flex w-full flex-col gap-6"
          defaultValue={[]}
        >
          {review.sections.map((section) => (
            <AccordionItem
              key={section.id}
              value={section.id}
              className="border-b border-op-card-border pb-6 last:border-b-0 last:pb-0"
            >
              <AccordionTrigger
                className={cn(
                  "cursor-pointer rounded-none border-0 py-0 text-left text-lg font-semibold text-op-text-primary hover:no-underline",
                  "**:data-[slot=accordion-trigger-icon]:size-4 **:data-[slot=accordion-trigger-icon]:text-op-text-primary"
                )}
              >
                {section.title}
              </AccordionTrigger>
              <AccordionContent className="pt-4 pb-0">
                <ReviewSectionRows section={section} />
              </AccordionContent>
            </AccordionItem>
          ))}
        </Accordion>
      </div>

      <div className={CAMPAIGN_REVIEW_GUEST_PREVIEW_RAIL_CLASS}>
        <GuestPreviewPanel
          channel={review.guestPreview.channelId}
          subject={review.guestPreview.subject}
          message={review.guestPreview.body}
          locationName={review.guestPreview.locationName}
          locationAddress={review.guestPreview.locationAddress}
          guestPreviewOpen={review.guestPreview.guestPreviewOpen}
          onOpenPreview={onOpenGuestPreview}
          onClosePreview={onCloseGuestPreview}
          onEditText={onEditMessage}
          onSendTest={onSendTest}
          sendTestDisabled={!review.guestPreview.sendTestAvailable}
          sendTestBusy={sendTestBusy}
          offerCoupon={
            review.guestPreview.offerCoupon != null ? (
              <GuestPreviewOfferCoupon coupon={review.guestPreview.offerCoupon} />
            ) : undefined
          }
        />
      </div>
    </div>
  )
}

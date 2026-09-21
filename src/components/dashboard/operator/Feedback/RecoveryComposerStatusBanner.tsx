import { Button } from "@/components/ui/button"
import { RECOVERY_COMPOSER_COPY } from "@/lib/operatorFeedback/recoveryComposerPresentation"
import type { RecoveryComposerStatusBanner as StatusBanner } from "@/lib/operatorFeedback/recoveryComposerPresentation"

/** RC-01 / RC-02 status banner + optional RC-03 eligibility notice. */
export function RecoveryComposerStatusBanner({
  banner,
  eligibilityNotice,
  addOfferEnabled,
  onAddOffer,
  disabled,
}: {
  banner: StatusBanner | null
  eligibilityNotice?: string | null
  addOfferEnabled?: boolean
  onAddOffer?: () => void
  disabled?: boolean
}) {
  if (banner == null && (eligibilityNotice == null || eligibilityNotice === "")) {
    return null
  }

  return (
    <div className="flex w-full flex-col gap-3">
      {banner != null ? (
        <div
          className="flex w-full flex-col gap-3 rounded-[4px] bg-[var(--op-color-gray-995)] p-[18px]"
          role="status"
          data-banner-kind={banner.kind}
        >
          <p className="m-0 text-sm font-medium text-op-text-primary">
            {banner.message}
          </p>
          {addOfferEnabled === true && onAddOffer != null ? (
            <Button
              type="button"
              variant="op-link"
              className="h-auto min-h-0 w-fit p-0"
              disabled={disabled === true}
              onClick={onAddOffer}
            >
              {RECOVERY_COMPOSER_COPY.addOfferCta}
            </Button>
          ) : null}
        </div>
      ) : null}
      {eligibilityNotice != null && eligibilityNotice !== "" ? (
        <div
          className="flex w-full flex-col gap-2 rounded-[4px] border border-op-card-border bg-[var(--op-color-gray-995)] p-[18px]"
          role="alert"
        >
          <p className="m-0 text-sm font-medium text-op-text-primary">
            {eligibilityNotice}
          </p>
        </div>
      ) : null}
    </div>
  )
}

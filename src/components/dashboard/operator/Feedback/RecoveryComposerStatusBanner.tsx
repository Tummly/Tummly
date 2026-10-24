import { Button } from "@/components/ui/button"
import {
  RECOVERY_COMPOSER_COPY,
  RECOVERY_COMPOSER_STATUS_BANNER_CLASS,
} from "@/lib/operatorFeedback/recoveryComposerPresentation"
import type { RecoveryComposerStatusBanner as StatusBanner } from "@/lib/operatorFeedback/recoveryComposerPresentation"

/** RC-01 / RC-02 status banner. */
export function RecoveryComposerStatusBanner({
  banner,
  addOfferEnabled,
  onAddOffer,
  disabled,
}: {
  banner: StatusBanner | null
  addOfferEnabled?: boolean
  onAddOffer?: () => void
  disabled?: boolean
}) {
  if (banner == null) {
    return null
  }

  return (
    <div className="flex w-full flex-col gap-3">
      <div
        className={RECOVERY_COMPOSER_STATUS_BANNER_CLASS}
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
    </div>
  )
}

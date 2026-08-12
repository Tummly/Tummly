import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import { OffersRedemptionLogBody } from "@/components/dashboard/operator/Offers/OffersRedemptionLogBody"
import { useOffersRedemptionLogPageModule } from "@/components/dashboard/operator/Offers/utils/useOffersRedemptionLogPageModule"
import { OFFERS_REDEMPTION_LOG_LOAD_ERROR_MESSAGE } from "@/lib/operatorOffers/createOperatorOffersRedemptionLogModule"
import { GUESTS_PAGE_SECONDARY_BUTTON_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import { OFFERS_REDEMPTION_LOG_COPY } from "@/lib/operatorOffers/offersRedemptionLogPresentation"

type OffersRedemptionLogPageProps = {
  offersHref: string
}

export function OffersRedemptionLogPage({
  offersHref,
}: OffersRedemptionLogPageProps) {
  const { snapshot, retryLoad } = useOffersRedemptionLogPageModule()

  const handleRetry = () => {
    void retryLoad()
  }

  if (
    snapshot.viewModel == null
    && (snapshot.loadStatus === "idle" || snapshot.loadStatus === "loading")
  ) {
    return (
      <div
        className="flex flex-1 items-center justify-center"
        role="status"
        aria-live="polite"
        aria-label="Loading redemption log"
      >
        <Spinner />
      </div>
    )
  }

  if (snapshot.loadStatus === "error" && snapshot.viewModel == null) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-3">
        <p className="m-0 text-sm text-muted-foreground">
          {snapshot.loadError ?? OFFERS_REDEMPTION_LOG_LOAD_ERROR_MESSAGE}
        </p>
        <Button
          type="button"
          variant="op-secondary"
          className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
          onClick={handleRetry}
        >
          {OFFERS_REDEMPTION_LOG_COPY.retry}
        </Button>
      </div>
    )
  }

  if (snapshot.viewModel == null) {
    return null
  }

  return (
    <OffersRedemptionLogBody
      viewModel={snapshot.viewModel}
      offersHref={offersHref}
      onRetry={handleRetry}
    />
  )
}

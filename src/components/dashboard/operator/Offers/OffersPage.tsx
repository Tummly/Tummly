import { OffersBody } from "@/components/dashboard/operator/Offers/OffersBody"
import { useOffersPageModule } from "@/components/dashboard/operator/Offers/utils/useOffersPageModule"
import { Spinner } from "@/components/ui/spinner"
import { OFFERS_LOAD_ERROR_MESSAGE } from "@/lib/operatorOffers/createOperatorOffersPageModule"

export function OffersPage() {
  const { snapshot } = useOffersPageModule()

  if (
    snapshot.viewModel == null
    && (snapshot.loadStatus === "idle" || snapshot.loadStatus === "loading")
  ) {
    return (
      <div
        className="flex flex-1 items-center justify-center"
        role="status"
        aria-live="polite"
        aria-label="Loading offers"
      >
        <Spinner />
      </div>
    )
  }

  if (snapshot.loadStatus === "error" && snapshot.viewModel == null) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-3">
        <p className="m-0 text-sm text-muted-foreground">
          {snapshot.loadError ?? OFFERS_LOAD_ERROR_MESSAGE}
        </p>
      </div>
    )
  }

  if (snapshot.viewModel == null) {
    return null
  }

  return <OffersBody viewModel={snapshot.viewModel} />
}

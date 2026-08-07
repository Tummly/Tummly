import { CampaignsBody } from "@/components/dashboard/operator/Campaigns/CampaignsBody"
import { useCampaignsPageModule } from "@/components/dashboard/operator/Campaigns/utils/useCampaignsPageModule"
import { useDashboardUiStore } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import type { CampaignsOverviewDateRange } from "@/lib/operatorCampaigns/campaignsOverviewDateRange"
import { CAMPAIGNS_LOAD_ERROR_MESSAGE } from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"

export function CampaignsPage() {
  const campaigns = useCampaignsPageModule()
  const { snapshot } = campaigns
  const campaignsOverviewDateRange = useDashboardUiStore(
    (state) => state.campaignsOverviewDateRange
  )
  const setCampaignsOverviewDateRange = useDashboardUiStore(
    (state) => state.setCampaignsOverviewDateRange
  )

  const handleCommitDateRange = (range: CampaignsOverviewDateRange) => {
    setCampaignsOverviewDateRange(range)
    void campaigns.reloadForOverviewDateRange()
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
        aria-label="Loading campaigns"
      >
        <Spinner />
      </div>
    )
  }

  if (snapshot.loadStatus === "error" && snapshot.viewModel == null) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-3">
        <p className="m-0 text-sm text-muted-foreground">
          {snapshot.loadError ?? CAMPAIGNS_LOAD_ERROR_MESSAGE}
        </p>
        <Button
          type="button"
          variant="op-secondary"
          onClick={() => {
            void campaigns.retryLoad()
          }}
        >
          Retry
        </Button>
      </div>
    )
  }

  if (snapshot.viewModel == null) {
    return null
  }

  return (
    <CampaignsBody
      viewModel={snapshot.viewModel}
      selectedDateRange={campaignsOverviewDateRange}
      onCommitDateRange={handleCommitDateRange}
    />
  )
}

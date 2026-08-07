import { useSyncExternalStore, useState } from "react"

import { getCampaignTemplates, getGuests } from "@/api/dashboardApi"
import { CampaignsBody } from "@/components/dashboard/operator/Campaigns/CampaignsBody"
import { CampaignTemplatePickerDialog } from "@/components/dashboard/operator/Campaigns/CampaignTemplatePickerDialog"
import { CampaignWizardDialog } from "@/components/dashboard/operator/Campaigns/CampaignWizardDialog"
import { useCampaignsPageModule } from "@/components/dashboard/operator/Campaigns/utils/useCampaignsPageModule"
import { useDashboardUiStore } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import type { CampaignAudienceSmartGroupCountsInput } from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import type { CampaignsOverviewDateRange } from "@/lib/operatorCampaigns/campaignsOverviewDateRange"
import { createCampaignTemplatePickerModule } from "@/lib/operatorCampaigns/createCampaignTemplatePickerModule"
import { createCampaignWizardModule } from "@/lib/operatorCampaigns/createCampaignWizardModule"
import { CAMPAIGNS_LOAD_ERROR_MESSAGE } from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"
import { emptySelection } from "@/lib/operatorFilterSheet"
import { DEFAULT_GUESTS_OVERVIEW_DATE_RANGE } from "@/lib/operatorGuests/guestsOverviewDateRange"
import { guestsFilterSheetSchema } from "@/lib/operatorGuests/guestsFilterSheetSchema"
import { buildGuestsListQueryParams } from "@/lib/operatorGuests/guestsListQueryParams"
import { OPERATOR_GUEST_DEFAULT_SORT_ID } from "@/lib/operatorGuests/guestsPresentation"
import type { OperatorCampaignsListViewId } from "@/types/operatorCampaigns"
import type { OperatorGuestSmartGroupId } from "@/types/operatorGuests"

const GUESTS_SCHEMA = guestsFilterSheetSchema()

async function loadCampaignTemplatesList() {
  const response = await getCampaignTemplates()
  if (!response.success) {
    throw new Error("Campaign template catalogue request failed.")
  }
  return response.items
}

async function loadAudienceSmartGroupCounts(input: {
  locationId: number
}): Promise<CampaignAudienceSmartGroupCountsInput> {
  const params = buildGuestsListQueryParams({
    locationId: input.locationId,
    smartGroup: "all-guests",
    q: "",
    sort: OPERATOR_GUEST_DEFAULT_SORT_ID,
    page: 1,
    pageSize: 1,
    filters: emptySelection(GUESTS_SCHEMA),
    overviewDateRange: DEFAULT_GUESTS_OVERVIEW_DATE_RANGE,
  })
  const response = await getGuests(params)
  if (!response.success) {
    throw new Error("Audience Smart Group counts request failed.")
  }
  const smartGroupCounts = (response.smartGroupCounts ?? {}) as Partial<
    Record<OperatorGuestSmartGroupId, number>
  >
  return { smartGroupCounts }
}

export function CampaignsPage() {
  const campaigns = useCampaignsPageModule()
  const { snapshot } = campaigns
  const campaignsOverviewDateRange = useDashboardUiStore(
    (state) => state.campaignsOverviewDateRange
  )
  const setCampaignsOverviewDateRange = useDashboardUiStore(
    (state) => state.setCampaignsOverviewDateRange
  )

  const [templatePicker] = useState(() =>
    createCampaignTemplatePickerModule({
      loadTemplates: loadCampaignTemplatesList,
    })
  )
  const templatePickerSnapshot = useSyncExternalStore(
    templatePicker.subscribe,
    templatePicker.getSnapshot,
    templatePicker.getSnapshot
  )

  const [campaignWizard] = useState(() =>
    createCampaignWizardModule({
      loadSmartGroupCounts: loadAudienceSmartGroupCounts,
    })
  )
  const campaignWizardSnapshot = useSyncExternalStore(
    campaignWizard.subscribe,
    campaignWizard.getSnapshot,
    campaignWizard.getSnapshot
  )

  const handleCommitDateRange = (range: CampaignsOverviewDateRange) => {
    setCampaignsOverviewDateRange(range)
    void campaigns.reloadForOverviewDateRange()
  }

  const handleOpenCreateCampaign = () => {
    const viewModel = snapshot.viewModel
    if (viewModel == null) {
      return
    }
    campaignWizard.openBlankCreate({
      locationId: viewModel.locationId,
      locationName: viewModel.locationName,
    })
  }

  const handleOpenTemplatePicker = () => {
    void templatePicker.open()
  }

  const handleBrowseTemplatesFromWizard = () => {
    campaignWizard.close()
    void templatePicker.open()
  }

  const handleTemplatePickerOpenChange = (open: boolean) => {
    if (!open) {
      templatePicker.close()
    }
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
    <>
      <CampaignsBody
        viewModel={snapshot.viewModel}
        selectedDateRange={campaignsOverviewDateRange}
        onCommitDateRange={handleCommitDateRange}
        onListViewChange={(viewId: OperatorCampaignsListViewId) => {
          void campaigns.setListView(viewId)
        }}
        onSearchQueryChange={campaigns.setSearchQuery}
        onViewAllCampaigns={() => {
          void campaigns.viewAllCampaigns()
        }}
        onClearAllFilters={() => {
          void campaigns.clearSearchAndFilters()
        }}
        onCreateCampaign={handleOpenCreateCampaign}
        onUseTemplate={handleOpenTemplatePicker}
      />
      <CampaignTemplatePickerDialog
        snapshot={templatePickerSnapshot}
        onOpenChange={handleTemplatePickerOpenChange}
        onRetry={() => {
          void templatePicker.retryLoad()
        }}
        onSearchQueryChange={templatePicker.setSearchQuery}
      />
      <CampaignWizardDialog
        snapshot={campaignWizardSnapshot}
        onRequestClose={campaignWizard.close}
        onSaveAndExit={campaignWizard.saveAndExit}
        onBack={campaignWizard.back}
        onSelectGoal={campaignWizard.setGoalId}
        onSelectAudience={campaignWizard.setAudienceId}
        onSelectSavedGroup={campaignWizard.setSavedGroupId}
        onSelectChannel={campaignWizard.setChannelId}
        onSelectOfferStance={campaignWizard.setOfferStanceId}
        onWriteManually={campaignWizard.writeManually}
        onPrepareDraftStub={campaignWizard.prepareDraftStub}
        onSubjectChange={campaignWizard.setSubject}
        onMessageChange={campaignWizard.setMessage}
        onOpenGuestPreview={campaignWizard.openGuestPreview}
        onCloseGuestPreview={campaignWizard.closeGuestPreview}
        onContinue={() => {
          void campaignWizard.continue()
        }}
        onBrowseTemplates={handleBrowseTemplatesFromWizard}
      />
    </>
  )
}

import { useSyncExternalStore, useState } from "react"
import { toast } from "sonner"

import {
  createCampaignDraft,
  createCatalogOffer,
  getCampaignDraftById,
  getCampaignTemplateById,
  getCampaignTemplates,
  getCatalogOfferById,
  getGuests,
  patchCampaignDraft,
} from "@/api/dashboardApi"
import { CampaignsBody } from "@/components/dashboard/operator/Campaigns/CampaignsBody"
import { CampaignTemplatePickerDialog } from "@/components/dashboard/operator/Campaigns/CampaignTemplatePickerDialog"
import { CampaignTemplatePreviewDrawer } from "@/components/dashboard/operator/Campaigns/CampaignTemplatePreviewDrawer"
import { CampaignWizardDialog } from "@/components/dashboard/operator/Campaigns/CampaignWizardDialog"
import { useCampaignsPageModule } from "@/components/dashboard/operator/Campaigns/utils/useCampaignsPageModule"
import { useDashboardUiStore } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import type { CampaignAudienceSmartGroupCountsInput } from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import type { CampaignsOverviewDateRange } from "@/lib/operatorCampaigns/campaignsOverviewDateRange"
import { createCampaignTemplatePickerModule } from "@/lib/operatorCampaigns/createCampaignTemplatePickerModule"
import { createCampaignTemplatePreviewModule } from "@/lib/operatorCampaigns/createCampaignTemplatePreviewModule"
import { createCampaignWizardModule } from "@/lib/operatorCampaigns/createCampaignWizardModule"
import { prepareCampaignMessageDraft } from "@/lib/operatorCampaigns/prepareCampaignMessageDraft"
import { CAMPAIGNS_LOAD_ERROR_MESSAGE } from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"
import { emptySelection } from "@/lib/operatorFilterSheet"
import { DEFAULT_GUESTS_OVERVIEW_DATE_RANGE } from "@/lib/operatorGuests/guestsOverviewDateRange"
import { guestsFilterSheetSchema } from "@/lib/operatorGuests/guestsFilterSheetSchema"
import { buildGuestsListQueryParams } from "@/lib/operatorGuests/guestsListQueryParams"
import {
  OPERATOR_GUEST_DEFAULT_SORT_ID,
  OPERATOR_GUEST_PAGE_SIZE,
} from "@/lib/operatorGuests/guestsPresentation"
import type { OperatorCampaignsListViewId } from "@/types/operatorCampaigns"
import type { CampaignRecommendation } from "@/types/operatorCampaigns"
import type { OperatorGuestSmartGroupId } from "@/types/operatorGuests"

const GUESTS_SCHEMA = guestsFilterSheetSchema()

async function loadCampaignTemplatesList() {
  const response = await getCampaignTemplates()
  if (!response.success) {
    throw new Error("Campaign template catalogue request failed.")
  }
  return response.items
}

async function loadCampaignTemplateDetail(id: string) {
  const response = await getCampaignTemplateById(id)
  if (!response.success || response.template == null) {
    throw new Error("Campaign template detail request failed.")
  }
  return response.template
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
    pageSize: OPERATOR_GUEST_PAGE_SIZE,
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

  const [templatePreview] = useState(() =>
    createCampaignTemplatePreviewModule({
      loadTemplateDetail: loadCampaignTemplateDetail,
    })
  )
  const templatePreviewSnapshot = useSyncExternalStore(
    templatePreview.subscribe,
    templatePreview.getSnapshot,
    templatePreview.getSnapshot
  )

  const [campaignWizard] = useState(() =>
    createCampaignWizardModule({
      loadSmartGroupCounts: loadAudienceSmartGroupCounts,
      prepareMessageDraft: prepareCampaignMessageDraft,
      createDraft: async (body) => {
        const response = await createCampaignDraft(body)
        if (!response.success || response.campaign == null) {
          throw new Error("Campaign draft create failed.")
        }
        return response.campaign
      },
      updateDraft: async (id, body) => {
        const response = await patchCampaignDraft(id, body)
        if (!response.success || response.campaign == null) {
          throw new Error("Campaign draft update failed.")
        }
        return response.campaign
      },
      createOffer: async (body) => {
        const response = await createCatalogOffer(body)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog create failed.")
        }
        return response.offer
      },
      getOffer: async (offerId) => {
        const response = await getCatalogOfferById(offerId)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog load failed.")
        }
        return response.offer
      },
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

  const handleReviewRecommendationDraft = (
    recommendation: CampaignRecommendation
  ) => {
    const viewModel = snapshot.viewModel
    if (viewModel == null || recommendation.draftPrefill == null) {
      return
    }
    void campaignWizard.openFromRecommendation({
      locationId: viewModel.locationId,
      locationName: viewModel.locationName,
      draftPrefill: recommendation.draftPrefill,
    })
  }

  const handleOpenTemplatePicker = () => {
    void templatePicker.open()
  }

  const handleBrowseTemplatesFromWizard = () => {
    campaignWizard.close()
    void templatePicker.open()
  }

  const handleContinueEditing = (campaignId: number) => {
    const viewModel = snapshot.viewModel
    if (viewModel == null) {
      return
    }
    void (async () => {
      try {
        const response = await getCampaignDraftById(campaignId)
        if (!response.success || response.campaign == null) {
          throw new Error("Campaign draft load failed.")
        }
        await campaignWizard.openFromDraft({
          locationName: viewModel.locationName,
          draft: response.campaign,
        })
      } catch {
        toast.error("Could not open this campaign draft. Try again.")
      }
    })()
  }

  const handleSaveAndExit = async () => {
    await campaignWizard.saveAndExit()
    if (!campaignWizard.getSnapshot().isOpen) {
      void campaigns.retryLoad()
    }
  }

  const handleTemplatePickerOpenChange = (open: boolean) => {
    if (!open) {
      templatePreview.close()
      templatePicker.close()
    }
  }

  const handleTemplatePreviewOpenChange = (open: boolean) => {
    if (!open) {
      templatePreview.close()
    }
  }

  const handlePreviewTemplate = (templateId: string) => {
    void templatePreview.open(templateId)
  }

  const handleUseTemplate = (templateId: string) => {
    const viewModel = snapshot.viewModel
    if (viewModel == null) {
      return
    }
    void (async () => {
      try {
        const template = await loadCampaignTemplateDetail(templateId)
        templatePreview.close()
        templatePicker.close()
        await campaignWizard.openFromTemplate({
          locationId: viewModel.locationId,
          locationName: viewModel.locationName,
          template,
        })
      } catch {
        // Keep picker open so the operator can retry or dismiss.
      }
    })()
  }

  const handleUseTemplateFromPreview = () => {
    const templateId = templatePreviewSnapshot.viewModel?.templateId ?? null
    if (templateId == null || templatePreviewSnapshot.loadStatus !== "loaded") {
      return
    }
    handleUseTemplate(templateId)
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
        onContinueEditing={handleContinueEditing}
        onCreateCampaign={handleOpenCreateCampaign}
        onUseTemplate={handleOpenTemplatePicker}
        onRetryRecommendation={() => {
          void campaigns.retryRecommendation()
        }}
        onReviewRecommendationDraft={handleReviewRecommendationDraft}
        onViewRecommendationAudience={campaigns.openRecommendationAudience}
        onCloseRecommendationAudience={campaigns.closeRecommendationAudience}
        onDismissRecommendation={campaigns.dismissRecommendation}
        onRetryMessagingUsage={() => {
          void campaigns.retryMessagingUsage()
        }}
      />
      <CampaignTemplatePickerDialog
        snapshot={templatePickerSnapshot}
        onOpenChange={handleTemplatePickerOpenChange}
        onRetry={() => {
          void templatePicker.retryLoad()
        }}
        onSearchQueryChange={templatePicker.setSearchQuery}
        onUseTemplate={handleUseTemplate}
        onPreview={handlePreviewTemplate}
      />
      <CampaignTemplatePreviewDrawer
        snapshot={templatePreviewSnapshot}
        locationName={snapshot.viewModel.locationName}
        onOpenChange={handleTemplatePreviewOpenChange}
        onRetry={() => {
          void templatePreview.retryLoad()
        }}
        onSelectChannel={templatePreview.setSelectedChannel}
        onUseThisTemplate={handleUseTemplateFromPreview}
      />
      <CampaignWizardDialog
        snapshot={campaignWizardSnapshot}
        onRequestClose={campaignWizard.close}
        onSaveAndExit={() => {
          void handleSaveAndExit()
        }}
        onBack={campaignWizard.back}
        onSelectGoal={campaignWizard.setGoalId}
        onSelectAudience={campaignWizard.setAudienceId}
        onSelectChannel={campaignWizard.setChannelId}
        onSelectOfferStance={campaignWizard.setOfferStanceId}
        onCloseCreateOfferPanel={campaignWizard.closeCreateOfferPanel}
        onEditAttachedOffer={campaignWizard.editAttachedOffer}
        onPatchCreateOfferDraft={campaignWizard.patchCreateOfferDraft}
        onConfirmCreateOffer={() => {
          void campaignWizard.confirmCreateOffer()
        }}
        onSelectScheduleMode={campaignWizard.setScheduleModeId}
        onWriteManually={campaignWizard.writeManually}
        onPrepareDraft={() => {
          void campaignWizard.prepareDraft()
        }}
        onRewriteSubject={() => {
          void campaignWizard.rewriteDraft("subject")
        }}
        onRewriteMessage={() => {
          void campaignWizard.rewriteDraft("message")
        }}
        onRetryAiDraft={() => {
          void campaignWizard.retryAiDraft()
        }}
        onDismissPreparingOverlay={campaignWizard.dismissPreparingOverlay}
        onRetryMessagingBalances={() => {
          void campaignWizard.retryMessagingBalances()
        }}
        onSubjectChange={campaignWizard.setSubject}
        onMessageChange={campaignWizard.setMessage}
        onOpenGuestPreview={campaignWizard.openGuestPreview}
        onCloseGuestPreview={campaignWizard.closeGuestPreview}
        onEditMessageFromReview={campaignWizard.editMessageFromReview}
        onContinue={() => {
          void campaignWizard.continue()
        }}
        onBrowseTemplates={handleBrowseTemplatesFromWizard}
      />
    </>
  )
}

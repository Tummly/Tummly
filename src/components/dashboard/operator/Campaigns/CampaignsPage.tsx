import { useEffect, useSyncExternalStore, useState } from "react"
import { useOutletContext } from "react-router-dom"
import { toast } from "sonner"

import {
  cancelCampaign,
  commitCampaignSchedule,
  createCampaignDraft,
  createCatalogOffer,
  duplicateCampaignAsDraft,
  getCampaignDraftById,
  getCampaignEligibility,
  getCampaignTemplateById,
  getCampaignTemplates,
  getCatalogOfferById,
  getGuests,
  listCatalogOffers,
  patchCampaignDraft,
  pauseCampaign,
  resumeCampaign,
  retryRemainingCampaign,
  sendCampaignTest,
  unscheduleCampaign,
  updateCatalogOffer,
} from "@/api/dashboardApi"
import { fetchCurrentUser } from "@/api/loginContextClient"
import { CampaignDetailPreviewDrawer } from "@/components/dashboard/operator/Campaigns/CampaignDetailPreviewDrawer"
import { CampaignsBody } from "@/components/dashboard/operator/Campaigns/CampaignsBody"
import { CampaignTemplatePickerDialog } from "@/components/dashboard/operator/Campaigns/CampaignTemplatePickerDialog"
import { CampaignTemplatePreviewDrawer } from "@/components/dashboard/operator/Campaigns/CampaignTemplatePreviewDrawer"
import { CampaignWizardDialog } from "@/components/dashboard/operator/Campaigns/CampaignWizardDialog"
import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import { useCampaignsPageModule } from "@/components/dashboard/operator/Campaigns/utils/useCampaignsPageModule"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { useDashboardUiStore } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import type {
  CampaignAudienceEligibilityBreakdown,
  CampaignAudienceSmartGroupCountsInput,
} from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import { unavailableCampaignAudienceEligibilityBreakdown } from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import { campaignsFilterSheetSchemaForWorkspace } from "@/lib/operatorCampaigns/campaignsFilterSheetSchema"
import type { CampaignsOverviewDateRange } from "@/lib/operatorCampaigns/campaignsOverviewDateRange"
import { createCampaignDetailPreviewModule } from "@/lib/operatorCampaigns/createCampaignDetailPreviewModule"
import { createCampaignTemplatePickerModule } from "@/lib/operatorCampaigns/createCampaignTemplatePickerModule"
import { createCampaignTemplatePreviewModule } from "@/lib/operatorCampaigns/createCampaignTemplatePreviewModule"
import { createCampaignWizardModule } from "@/lib/operatorCampaigns/createCampaignWizardModule"
import type { CampaignRowActionId } from "@/lib/operatorCampaigns/campaignListPresentation"
import { loadCampaignMessagingBalances } from "@/lib/operatorCampaigns/loadCampaignMessagingBalances"
import { prepareCampaignMessageDraft } from "@/lib/operatorCampaigns/prepareCampaignMessageDraft"
import { CAMPAIGNS_LOAD_ERROR_MESSAGE } from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"
import { catalogOfferWriteSuccessToast } from "@/lib/operatorOffers/createEditOfferDrawerPresentation"
import { emptySelection } from "@/lib/operatorFilterSheet"
import { DEFAULT_GUESTS_OVERVIEW_DATE_RANGE } from "@/lib/operatorGuests/guestsOverviewDateRange"
import { guestsFilterSheetSchema } from "@/lib/operatorGuests/guestsFilterSheetSchema"
import { buildGuestsListQueryParams } from "@/lib/operatorGuests/guestsListQueryParams"
import {
  OPERATOR_GUEST_DEFAULT_SORT_ID,
  OPERATOR_GUEST_PAGE_SIZE,
} from "@/lib/operatorGuests/guestsPresentation"
import { parseOperatorProfile } from "@/lib/operatorHome/parseOperatorProfile"
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

async function loadAudienceEligibility(input: {
  locationId: number
  audienceKey: string
}): Promise<CampaignAudienceEligibilityBreakdown> {
  const response = await getCampaignEligibility({
    locationId: input.locationId,
    audienceKey: input.audienceKey,
  })
  if (!response.success || response.eligibility == null) {
    throw new Error("Campaign eligibility request failed.")
  }
  const eligibility = response.eligibility
  if (!eligibility.evaluable) {
    return unavailableCampaignAudienceEligibilityBreakdown()
  }
  return {
    matched: eligibility.matched,
    currentlyEligible: eligibility.currentlyEligible,
    excluded: eligibility.excluded,
    emailEligible: eligibility.emailEligible,
    smsEligible: eligibility.smsEligible,
    excludedReasons: eligibility.excludedReasons ?? [],
    source: "live",
  }
}

export function CampaignsPage() {
  const campaigns = useCampaignsPageModule()
  const { snapshot, clearTabCache } = campaigns
  const { locations, mode } = useOutletContext<DashboardOutletContext>()
  const campaignsOverviewDateRange = useDashboardUiStore(
    (state) => state.campaignsOverviewDateRange
  )
  const setCampaignsOverviewDateRange = useDashboardUiStore(
    (state) => state.setCampaignsOverviewDateRange
  )
  const campaignsIntent = useDashboardUiStore((state) => state.campaignsIntent)
  const setCampaignsIntent = useDashboardUiStore(
    (state) => state.setCampaignsIntent
  )

  useEffect(
    () => () => {
      clearTabCache()
    },
    [clearTabCache]
  )

  useEffect(() => {
    if (campaignsIntent?.view !== "drafts") {
      return
    }
    setCampaignsIntent(null)
    void campaigns
      .clearSearchAndFilters()
      .then(() => {
        campaigns.setSortId("recent-activity")
        return campaigns.setListView("drafts")
      })
      .then(() => {
        document.getElementById("campaigns-list")?.scrollIntoView({
          behavior: "smooth",
          block: "start",
        })
      })
  }, [campaigns, campaignsIntent, setCampaignsIntent])

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

  const [campaignDetailPreview] = useState(() =>
    createCampaignDetailPreviewModule({
      loadCampaign: async (campaignId) => {
        const response = await getCampaignDraftById(campaignId)
        if (!response.success || response.campaign == null) {
          throw new Error("Campaign preview load failed.")
        }
        return response.campaign
      },
    })
  )
  const campaignDetailPreviewSnapshot = useSyncExternalStore(
    campaignDetailPreview.subscribe,
    campaignDetailPreview.getSnapshot,
    campaignDetailPreview.getSnapshot
  )

  const [campaignWizard] = useState(() =>
    createCampaignWizardModule({
      loadSmartGroupCounts: loadAudienceSmartGroupCounts,
      loadAudienceEligibility,
      // Shared with overview Messaging usage — omit until Billing balances API exists.
      loadMessagingBalances: loadCampaignMessagingBalances,
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
      updateOffer: async (offerId, body) => {
        const response = await updateCatalogOffer(offerId, body)
        if (!response.success || response.offer == null) {
          throw new Error("Offers catalog update failed.")
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
      listCatalogOffers,
      getOperatorAccountEmail: async () => {
        const result = await fetchCurrentUser()
        return parseOperatorProfile(result)?.email ?? null
      },
      sendCampaignTest: async (request) => {
        const response = await sendCampaignTest(request)
        if (!response.success) {
          throw new Error("Campaign send test failed.")
        }
      },
      commitCampaign: async ({ campaignId, body }) => {
        const response = await commitCampaignSchedule(campaignId, body)
        if (!response.success || response.campaign == null) {
          throw new Error("Campaign schedule commit failed.")
        }
        return response.campaign
      },
    })
  )
  const campaignWizardSnapshot = useSyncExternalStore(
    campaignWizard.subscribe,
    campaignWizard.getSnapshot,
    campaignWizard.getSnapshot
  )

  const selectedLocationAddress = (() => {
    const locationId = snapshot.viewModel?.locationId
    if (locationId == null) {
      return null
    }
    const address = locations.find((location) => location.id === locationId)
      ?.address
    if (address == null) {
      return null
    }
    const trimmed = address.trim()
    return trimmed.length > 0 ? trimmed : null
  })()

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
      locationAddress: selectedLocationAddress,
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
      locationAddress: selectedLocationAddress,
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
          locationAddress: selectedLocationAddress,
          draft: response.campaign,
        })
      } catch {
        toast.error("Could not open this campaign draft. Try again.")
      }
    })()
  }

  const handlePreviewCampaign = (campaignId: number) => {
    void campaignDetailPreview.open(campaignId)
  }

  const handleRowAction = (
    campaignId: number,
    rowVersion: string,
    actionId: CampaignRowActionId
  ) => {
    if (actionId === "preview") {
      handlePreviewCampaign(campaignId)
      return
    }
    if (actionId === "continue-editing") {
      handleContinueEditing(campaignId)
      return
    }

    const body = { rowVersion }
    void (async () => {
      try {
        switch (actionId) {
          case "unschedule":
            await unscheduleCampaign(campaignId, body)
            toast.success("Campaign unscheduled.")
            break
          case "pause":
            await pauseCampaign(campaignId, body)
            toast.success("Campaign paused.")
            break
          case "cancel":
          case "cancel-remaining": {
            const response = await cancelCampaign(campaignId, body)
            toast.success(
              response.campaign.status === "partially-sent"
                ? "Remaining sends cancelled."
                : "Campaign cancelled."
            )
            break
          }
          case "resume":
            await resumeCampaign(campaignId, body)
            toast.success("Campaign resumed.")
            break
          case "retry-remaining":
            await retryRemainingCampaign(campaignId, body)
            toast.success("Retrying remaining recipients.")
            break
          case "duplicate":
            await duplicateCampaignAsDraft(campaignId, body)
            toast.success("New draft created.")
            break
        }
        void campaigns.retryLoad()
      } catch (error) {
        const message =
          error instanceof Error && error.message.trim().length > 0
            ? error.message
            : "Could not update this campaign. Try again."
        toast.error(message)
      }
    })()
  }

  const handleCampaignDetailPreviewOpenChange = (open: boolean) => {
    if (!open) {
      campaignDetailPreview.close()
    }
  }

  const handleSaveAndExit = async () => {
    await campaignWizard.saveAndExit()
    if (!campaignWizard.getSnapshot().isOpen) {
      await campaigns.retryLoad()
    }
  }

  const handleDismissCommitSuccess = () => {
    campaignWizard.dismissSuccess()
    void campaigns.retryLoad()
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
          locationAddress: selectedLocationAddress,
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
        tabContentStatus={snapshot.tabContentStatus}
        viewModel={snapshot.viewModel}
        selectedDateRange={campaignsOverviewDateRange}
        onCommitDateRange={handleCommitDateRange}
        onListViewChange={(viewId: OperatorCampaignsListViewId) => {
          void campaigns.setListView(viewId)
        }}
        onSearchQueryChange={campaigns.setSearchQuery}
        onSortChange={campaigns.setSortId}
        onPreviousPage={campaigns.goToPreviousPage}
        onNextPage={campaigns.goToNextPage}
        onOpenFilters={campaigns.openFilters}
        onRemoveFilterChip={campaigns.removeFilterChip}
        onViewAllCampaigns={() => {
          void campaigns.viewAllCampaigns()
        }}
        onClearAllFilters={() => {
          void campaigns.clearSearchAndFilters()
        }}
        onRowAction={handleRowAction}
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
        onViewMessagingUsage={() => {
          const anchorId = snapshot.viewModel?.header.messagingUsageAnchorId
          if (anchorId == null) {
            return
          }
          document.getElementById(anchorId)?.scrollIntoView({
            behavior: "smooth",
            block: "start",
          })
        }}
      />
      <OperatorFilterSheetDialog
        open={snapshot.viewModel.filtersSession != null}
        title="Filter campaigns"
        schema={campaignsFilterSheetSchemaForWorkspace({
          locations: snapshot.viewModel.filterSchemaLocations,
          createdBy: snapshot.viewModel.filterSchemaCreatedBy,
          showLocationFilter: snapshot.viewModel.showLocationFilter,
        })}
        session={snapshot.viewModel.filtersSession}
        chipResolvers={{
          location: (id) =>
            snapshot.viewModel?.filterSchemaLocations.find(
              (location) => location.id === id
            )?.label ?? id,
          createdBy: (id) =>
            snapshot.viewModel?.filterSchemaCreatedBy.find(
              (option) => option.id === id
            )?.label ?? id,
        }}
        onSessionChange={campaigns.setFiltersSession}
        onOpenChange={(open) => {
          if (!open) {
            campaigns.closeFilters()
          }
        }}
        onApply={campaigns.applyFilters}
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
      <CampaignDetailPreviewDrawer
        snapshot={campaignDetailPreviewSnapshot}
        locationName={snapshot.viewModel.locationName}
        onOpenChange={handleCampaignDetailPreviewOpenChange}
        onRetry={() => {
          void campaignDetailPreview.retryLoad()
        }}
        onSelectChannel={campaignDetailPreview.setSelectedChannel}
      />
      <CampaignWizardDialog
        snapshot={campaignWizardSnapshot}
        dashboardMode={mode}
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
          void (async () => {
            const result = await campaignWizard.confirmCreateOffer()
            const message = catalogOfferWriteSuccessToast(result)
            if (message != null) {
              toast.success(message)
            }
          })()
        }}
        onExistingOfferSearchChange={campaignWizard.setExistingOfferSearch}
        onSelectExistingOffer={campaignWizard.selectExistingOffer}
        onRetryExistingOfferPicker={() => {
          void campaignWizard.retryExistingOfferPicker()
        }}
        onCreateNewOfferFromPicker={
          campaignWizard.createNewOfferFromExistingPicker
        }
        onConfirmPendingEditOfferSave={() => {
          void (async () => {
            const result = await campaignWizard.confirmPendingEditOfferSave()
            const message = catalogOfferWriteSuccessToast(result)
            if (message != null) {
              toast.success(message)
            }
          })()
        }}
        onCancelPendingEditOfferSave={campaignWizard.cancelPendingEditOfferSave}
        onSelectScheduleMode={campaignWizard.setScheduleModeId}
        onScheduleDateChange={campaignWizard.setScheduleDateLocal}
        onScheduleTimeChange={campaignWizard.setScheduleTimeLocal}
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
        onOpenSendTest={() => {
          void campaignWizard.openSendTestDialog()
        }}
        onCloseSendTest={campaignWizard.closeSendTestDialog}
        onSendTestEmailChange={campaignWizard.setSendTestEmail}
        onConfirmSendTest={() => {
          void campaignWizard.confirmSendTest()
        }}
        onContinue={() => {
          void campaignWizard.continue()
        }}
        onCancelCommitConfirm={campaignWizard.cancelCommitConfirm}
        onConfirmCommit={() => {
          void campaignWizard.confirmCommit()
        }}
        onDismissSuccess={handleDismissCommitSuccess}
        onBrowseTemplates={handleBrowseTemplatesFromWizard}
      />
    </>
  )
}

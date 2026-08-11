import { useMemo } from "react"
import { useNavigate, useOutletContext } from "react-router-dom"

import { OffersBody } from "@/components/dashboard/operator/Offers/OffersBody"
import { OfferTemplatePickerDialog } from "@/components/dashboard/operator/Offers/OfferTemplatePickerDialog"
import { StaffRedeemDialog } from "@/components/dashboard/operator/Offers/StaffRedeemDialog"
import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import { useOffersPageModule } from "@/components/dashboard/operator/Offers/utils/useOffersPageModule"
import { useStaffRedeemModule } from "@/components/dashboard/operator/Offers/utils/useStaffRedeemModule"
import { CREATE_EDIT_OFFER_DRAWER_COPY } from "@/lib/operatorOffers/createEditOfferDrawerPresentation"
import { OFFERS_LOAD_ERROR_MESSAGE } from "@/lib/operatorOffers/createOperatorOffersPageModule"
import { offersFilterSheetSchema } from "@/lib/operatorOffers/offersFilterSheetSchema"
import { OFFERS_PAGE_COPY } from "@/lib/operatorOffers/offersPresentation"
import {
  operatorDashboardOfferDetailsPath,
  operatorDashboardOffersRedemptionLogPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import type { OperatorOffersListViewId } from "@/types/operatorCampaigns"

export function OffersPage() {
  const {
    snapshot,
    pageModule,
    setPerformanceDateRange,
    openCreateOffer,
    closeCreateOfferDrawer,
    patchCreateOfferDraft,
    confirmCreateOffer,
  } = useOffersPageModule()
  const staffRedeem = useStaffRedeemModule()
  const { mode } = useOutletContext<DashboardOutletContext>()
  const navigate = useNavigate()

  const redemptionLogHref = useMemo(() => {
    if (snapshot.viewModel == null) {
      return operatorDashboardOffersRedemptionLogPath(mode, 0)
    }
    return operatorDashboardOffersRedemptionLogPath(
      mode,
      snapshot.viewModel.locationId
    )
  }, [mode, snapshot.viewModel])

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
        <Button
          type="button"
          variant="op-secondary"
          onClick={() => {
            void pageModule.retryLoad()
          }}
        >
          {OFFERS_PAGE_COPY.retry}
        </Button>
      </div>
    )
  }

  if (snapshot.viewModel == null) {
    return null
  }

  const pending = snapshot.viewModel.pendingLifecycleAction
  const pendingEditSave = snapshot.pendingEditOfferSave

  return (
    <>
      <OffersBody
        viewModel={snapshot.viewModel}
        createOfferDrawer={snapshot.createOfferDrawer}
        redemptionLogHref={redemptionLogHref}
        onOpenCreateOffer={() => {
          void openCreateOffer()
        }}
        onCloseCreateOffer={closeCreateOfferDrawer}
        onPatchCreateOfferDraft={patchCreateOfferDraft}
        onConfirmCreateOffer={() => {
          void confirmCreateOffer()
        }}
        onOpenStaffRedeem={() => {
          staffRedeem.open(snapshot.viewModel!.locationId)
        }}
        onCommitPerformanceDateRange={setPerformanceDateRange}
        onListViewChange={(viewId: OperatorOffersListViewId) => {
          void pageModule.setListView(viewId)
        }}
        onSearchQueryChange={pageModule.setSearchQuery}
        onSortChange={pageModule.setSortId}
        onPreviousPage={pageModule.goToPreviousPage}
        onNextPage={pageModule.goToNextPage}
        onOpenFilters={pageModule.openFilters}
        onRemoveFilterChip={pageModule.removeFilterChip}
        onViewAllOffers={() => {
          void pageModule.viewAllOffers()
        }}
        onClearAllFilters={() => {
          void pageModule.clearSearchAndFilters()
        }}
        onRowAction={(offerId, actionId) => {
          if (actionId === "view") {
            void navigate(
              operatorDashboardOfferDetailsPath(
                mode,
                offerId,
                snapshot.viewModel!.locationId
              )
            )
            return
          }
          if (actionId === "edit") {
            void pageModule.openEditOfferDrawer(offerId)
            return
          }
          pageModule.requestRowAction(offerId, actionId)
        }}
        onNeedsAttentionRowCta={(row) => {
          if (
            row.ctaKind === "review-void-offer"
            && row.offerId != null
          ) {
            void navigate(
              operatorDashboardOfferDetailsPath(
                mode,
                row.offerId,
                snapshot.viewModel!.locationId,
                { tab: "void-requests" }
              )
            )
            return
          }
          void pageModule.selectNeedsAttentionList()
        }}
        onNeedsAttentionViewAll={() => {
          void pageModule.selectNeedsAttentionList()
        }}
      />
      <OfferTemplatePickerDialog
        snapshot={snapshot.offerTemplatePicker}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.closeOfferTemplatePicker()
          }
        }}
        onRetry={() => {
          void pageModule.retryOfferTemplateLoad()
        }}
        onSearchQueryChange={pageModule.setOfferTemplateSearchQuery}
        onUseTemplate={pageModule.useOfferTemplate}
      />
      <OperatorFilterSheetDialog
        open={snapshot.viewModel.filtersSession != null}
        title={OFFERS_PAGE_COPY.filterSheetTitle}
        schema={offersFilterSheetSchema()}
        session={snapshot.viewModel.filtersSession}
        onSessionChange={pageModule.setFiltersSession}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.closeFilters()
          }
        }}
        onApply={pageModule.applyFilters}
      />
      <AlertDialog
        open={pending != null}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.cancelPendingLifecycleAction()
          }
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {pending?.title ?? OFFERS_PAGE_COPY.confirmAction}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {pending?.description ?? ""}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>
              {OFFERS_PAGE_COPY.cancelAction}
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                void pageModule.confirmPendingLifecycleAction()
              }}
            >
              {OFFERS_PAGE_COPY.confirmAction}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
      <AlertDialog
        open={pendingEditSave != null}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.cancelPendingEditOfferSave()
          }
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {pendingEditSave?.title
                ?? CREATE_EDIT_OFFER_DRAWER_COPY.editSaveConfirmTitle}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {pendingEditSave?.description
                ?? CREATE_EDIT_OFFER_DRAWER_COPY.editSaveConfirmDescription}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>
              {CREATE_EDIT_OFFER_DRAWER_COPY.cancel}
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                void pageModule.confirmPendingEditOfferSave()
              }}
            >
              {CREATE_EDIT_OFFER_DRAWER_COPY.editConfirm}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
      <StaffRedeemDialog
        snapshot={staffRedeem.snapshot}
        onOpenChange={(open) => {
          if (!open) {
            staffRedeem.close()
          }
        }}
        onCodeChange={staffRedeem.setCode}
        onCheckOffer={staffRedeem.checkOffer}
        onCancelConfirm={staffRedeem.cancelConfirm}
        onMarkAsRedeemed={staffRedeem.markAsRedeemed}
        onApplyScannedCode={staffRedeem.applyScannedCode}
      />
    </>
  )
}

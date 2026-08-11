import { useNavigate } from "react-router-dom"
import { toast } from "sonner"

import { CreateEditOfferDrawer } from "@/components/dashboard/operator/Offers/CreateEditOfferDrawer"
import { OfferDetailsBody } from "@/components/dashboard/operator/Offers/OfferDetailsBody"
import { StaffRedeemDialog } from "@/components/dashboard/operator/Offers/StaffRedeemDialog"
import { VoidRequestDialog } from "@/components/dashboard/operator/Offers/VoidRequestDialog"
import { useOfferDetailsPageModule } from "@/components/dashboard/operator/Offers/utils/useOfferDetailsPageModule"
import { useOffersPageModule } from "@/components/dashboard/operator/Offers/utils/useOffersPageModule"
import { useStaffRedeemModule } from "@/components/dashboard/operator/Offers/utils/useStaffRedeemModule"
import { useVoidRequestModule } from "@/components/dashboard/operator/Offers/utils/useVoidRequestModule"
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
import {
  OFFER_DETAILS_LOAD_ERROR_MESSAGE,
  type OfferDetailsRedemptionRow,
  type OfferDetailsVoidRequestRow,
} from "@/lib/operatorOffers/createOfferDetailsPageModule"
import { OFFER_DETAILS_COPY } from "@/lib/operatorOffers/offerDetailsPresentation"
import type {
  VoidCreatePreview,
  VoidReviewDetail,
} from "@/lib/operatorOffers/voidRequestAdapters"
import {
  VOID_REQUEST_COPY,
  type VoidRequestCorrectionId,
  type VoidRequestReasonId,
} from "@/lib/operatorOffers/voidRequestPresentation"
import {
  operatorDashboardGuestProfilePath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"

function isRedeemedOutcome(outcomeText: string): boolean {
  return outcomeText.trim().toLowerCase() === "redeemed"
}

function redemptionToVoidCreatePreview(
  row: OfferDetailsRedemptionRow,
  offerId: number,
  locationId: number,
  offerTitle: string
): VoidCreatePreview {
  return {
    passId: row.passId ?? row.id,
    redemptionId: row.id,
    offerId,
    locationId,
    offerTitle: row.offerTitle ?? offerTitle,
    guestName: row.guestName,
    passCodeMasked: row.passCodeMasked ?? row.passReferenceText,
    currentStateText: row.outcomeText,
    expiresText: row.expiresText ?? OFFER_DETAILS_COPY.metricUnavailable,
    locationName: row.locationName,
    linkedCampaignText:
      row.linkedCampaignText ?? "Not issued through a campaign",
  }
}

function resolveReasonId(
  row: OfferDetailsVoidRequestRow
): VoidRequestReasonId {
  if (row.reasonId != null) {
    return row.reasonId
  }
  const match = (
    Object.entries(VOID_REQUEST_COPY.reasons) as Array<
      [VoidRequestReasonId, string]
    >
  ).find(([, label]) => label === row.reasonText)
  return match?.[0] ?? "other"
}

function resolveCorrectionId(
  row: OfferDetailsVoidRequestRow
): VoidRequestCorrectionId {
  if (row.correctionId != null) {
    return row.correctionId
  }
  const keep = VOID_REQUEST_COPY.corrections.keep_unusable.title
  const restore = VOID_REQUEST_COPY.corrections.restore_one_use.title
  if (row.requestedCorrectionText === keep) {
    return "keep_unusable"
  }
  if (
    row.requestedCorrectionText === restore
    || row.requestedCorrectionText.toLowerCase().includes("restore")
  ) {
    return "restore_one_use"
  }
  return "keep_unusable"
}

function voidRowToReviewDetail(
  row: OfferDetailsVoidRequestRow,
  offerId: number,
  locationId: number,
  offerTitle: string
): VoidReviewDetail {
  const reasonId = resolveReasonId(row)
  const correctionId = resolveCorrectionId(row)
  return {
    requestId: row.id,
    passId: row.passId ?? row.id,
    offerId,
    locationId,
    requestedByText: row.requestedByText,
    requestedAtText: row.dateTimeText,
    reasonId,
    reasonText: row.reasonText,
    explanation: row.explanation ?? null,
    correctionId,
    correctionText:
      row.requestedCorrectionText
      || VOID_REQUEST_COPY.corrections[correctionId].title,
    offerTitle: row.offerTitle ?? offerTitle,
    guestName: row.guestName,
    passCodeMasked: row.passCodeMasked ?? row.offerPassText,
    currentStateText: row.currentStateText,
    expiresText: row.expiresText ?? OFFER_DETAILS_COPY.metricUnavailable,
    locationName: row.locationName,
    linkedCampaignText:
      row.linkedCampaignText ?? "Not issued through a campaign",
  }
}

type OfferDetailsPageProps = {
  offersHref: string
  shareOfferInCampaignHref: string
  mode: OperatorDashboardMode
}

export function OfferDetailsPage({
  offersHref,
  shareOfferInCampaignHref,
  mode,
}: OfferDetailsPageProps) {
  const navigate = useNavigate()
  const {
    snapshot,
    retryLoad,
    setActiveTab,
    setCampaignsSubTab,
    setOverviewDateRange,
    requestHeaderAction,
    confirmPendingHeaderAction,
    cancelPendingHeaderAction,
    requestClaimsRowAction,
    confirmPendingRowAction,
    cancelPendingRowAction,
  } = useOfferDetailsPageModule()
  const offersPage = useOffersPageModule()
  const staffRedeem = useStaffRedeemModule()
  const voidRequest = useVoidRequestModule()

  if (
    snapshot.viewModel == null
    && (snapshot.loadStatus === "idle" || snapshot.loadStatus === "loading")
  ) {
    return (
      <div
        className="flex flex-1 items-center justify-center"
        role="status"
        aria-live="polite"
        aria-label="Loading offer details"
      >
        <Spinner />
      </div>
    )
  }

  if (snapshot.loadStatus === "error" && snapshot.viewModel == null) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-3">
        <p className="m-0 text-sm text-muted-foreground">
          {snapshot.loadError ?? OFFER_DETAILS_LOAD_ERROR_MESSAGE}
        </p>
        <Button
          type="button"
          variant="op-secondary"
          onClick={() => {
            void retryLoad()
          }}
        >
          {OFFER_DETAILS_COPY.retry}
        </Button>
      </div>
    )
  }

  if (snapshot.viewModel == null) {
    return null
  }

  const pending =
    snapshot.viewModel.pendingHeaderAction
    ?? snapshot.viewModel.pendingRowAction
  const createOfferDrawer = offersPage.snapshot.createOfferDrawer
  const viewModel = snapshot.viewModel

  return (
    <>
      <OfferDetailsBody
        viewModel={viewModel}
        offersHref={offersHref}
        onEditOffer={() => {
          void offersPage.pageModule.openEditOfferDrawer(viewModel.offerId)
        }}
        onOpenStaffRedeem={() => {
          staffRedeem.open(viewModel.locationId)
        }}
        onHeaderAction={(actionId) => {
          if (actionId === "rename") {
            void offersPage.pageModule.openEditOfferDrawer(viewModel.offerId)
            return
          }
          requestHeaderAction(actionId)
        }}
        onTabChange={setActiveTab}
        onCommitDateRange={(range) => {
          void setOverviewDateRange(range)
        }}
        onShareOfferInCampaign={() => {
          navigate(shareOfferInCampaignHref)
        }}
        onCampaignsSubTabChange={setCampaignsSubTab}
        onClaimsRowAction={(rowId, actionId) => {
          if (actionId === "copy-code") {
            const row = viewModel.claims.rows.find((entry) => entry.id === rowId)
            if (row?.claimCode) {
              void navigator.clipboard.writeText(row.claimCode)
            }
            return
          }
          if (actionId === "view-guest-profile") {
            const row = viewModel.claims.rows.find((entry) => entry.id === rowId)
            if (row?.guestId != null) {
              navigate(
                operatorDashboardGuestProfilePath(
                  mode,
                  row.guestId,
                  viewModel.locationId
                )
              )
            }
            return
          }
          requestClaimsRowAction(rowId, actionId)
        }}
        onRedemptionsRowAction={(rowId, actionId) => {
          if (actionId !== "request-void") {
            return
          }
          const row = viewModel.redemptions.rows.find(
            (entry) => entry.id === rowId
          )
          if (row == null) {
            return
          }
          if (!isRedeemedOutcome(row.outcomeText)) {
            toast.error(VOID_REQUEST_COPY.notRedeemedToast)
            return
          }
          voidRequest.openCreate(
            redemptionToVoidCreatePreview(
              row,
              viewModel.offerId,
              viewModel.locationId,
              viewModel.title
            )
          )
        }}
        onVoidRequestsRowAction={(rowId, actionId) => {
          if (actionId !== "review") {
            return
          }
          const row = viewModel.voidRequests.rows.find(
            (entry) => entry.id === rowId
          )
          if (row == null) {
            return
          }
          voidRequest.openReview(
            voidRowToReviewDetail(
              row,
              viewModel.offerId,
              viewModel.locationId,
              viewModel.title
            )
          )
        }}
      />
      {createOfferDrawer != null ? (
        <CreateEditOfferDrawer
          open={createOfferDrawer.open}
          mode={createOfferDrawer.mode}
          locationSubtitle={createOfferDrawer.locationSubtitle}
          draft={createOfferDrawer.draft}
          status={createOfferDrawer.status}
          error={createOfferDrawer.error}
          saveGated={createOfferDrawer.saveGated}
          canConfirm={createOfferDrawer.canConfirm}
          onOpenChange={(open) => {
            if (!open) {
              offersPage.closeCreateOfferDrawer()
            }
          }}
          onPatch={offersPage.patchCreateOfferDraft}
          onConfirm={() => {
            void offersPage.confirmCreateOffer()
          }}
        />
      ) : null}
      <AlertDialog
        open={pending != null}
        onOpenChange={(open) => {
          if (!open) {
            cancelPendingHeaderAction()
            cancelPendingRowAction()
          }
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {pending?.title ?? OFFER_DETAILS_COPY.confirmAction}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {pending?.description ?? ""}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>
              {OFFER_DETAILS_COPY.cancelAction}
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                if (viewModel.pendingRowAction != null) {
                  confirmPendingRowAction()
                  return
                }
                void confirmPendingHeaderAction()
              }}
            >
              {OFFER_DETAILS_COPY.confirmAction}
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
      <VoidRequestDialog
        snapshot={voidRequest.snapshot}
        onOpenChange={(open) => {
          if (!open) {
            voidRequest.close()
          }
        }}
        onReasonChange={voidRequest.setReason}
        onExplanationChange={voidRequest.setExplanation}
        onCorrectionChange={voidRequest.setCorrection}
        onSendRequest={voidRequest.sendRequest}
        onRequestApprove={voidRequest.requestApprove}
        onRequestReject={voidRequest.requestReject}
        onConfirmApprove={voidRequest.confirmApprove}
        onConfirmReject={voidRequest.confirmReject}
        onGoBack={voidRequest.goBack}
      />
    </>
  )
}

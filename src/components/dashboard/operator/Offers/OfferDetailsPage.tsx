import { CreateEditOfferDrawer } from "@/components/dashboard/operator/Offers/CreateEditOfferDrawer"
import { OfferDetailsBody } from "@/components/dashboard/operator/Offers/OfferDetailsBody"
import { StaffRedeemDialog } from "@/components/dashboard/operator/Offers/StaffRedeemDialog"
import { useOfferDetailsPageModule } from "@/components/dashboard/operator/Offers/utils/useOfferDetailsPageModule"
import { useOffersPageModule } from "@/components/dashboard/operator/Offers/utils/useOffersPageModule"
import { useStaffRedeemModule } from "@/components/dashboard/operator/Offers/utils/useStaffRedeemModule"
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
import { OFFER_DETAILS_LOAD_ERROR_MESSAGE } from "@/lib/operatorOffers/createOfferDetailsPageModule"
import { OFFER_DETAILS_COPY } from "@/lib/operatorOffers/offerDetailsPresentation"

type OfferDetailsPageProps = {
  offersHref: string
}

export function OfferDetailsPage({ offersHref }: OfferDetailsPageProps) {
  const {
    snapshot,
    retryLoad,
    setActiveTab,
    setOverviewDateRange,
    requestHeaderAction,
    confirmPendingHeaderAction,
    cancelPendingHeaderAction,
  } = useOfferDetailsPageModule()
  const offersPage = useOffersPageModule()
  const staffRedeem = useStaffRedeemModule()

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

  const pending = snapshot.viewModel.pendingHeaderAction
  const createOfferDrawer = offersPage.snapshot.createOfferDrawer

  return (
    <>
      <OfferDetailsBody
        viewModel={snapshot.viewModel}
        offersHref={offersHref}
        onEditOffer={() => {
          void offersPage.pageModule.openEditOfferDrawer(
            snapshot.viewModel!.offerId
          )
        }}
        onOpenStaffRedeem={() => {
          staffRedeem.open(snapshot.viewModel!.locationId)
        }}
        onHeaderAction={(actionId) => {
          if (actionId === "rename") {
            void offersPage.pageModule.openEditOfferDrawer(
              snapshot.viewModel!.offerId
            )
            return
          }
          requestHeaderAction(actionId)
        }}
        onTabChange={setActiveTab}
        onCommitDateRange={(range) => {
          void setOverviewDateRange(range)
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
                confirmPendingHeaderAction()
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
    </>
  )
}

import { useNavigate, useOutletContext } from "react-router-dom"

import { CapturePlacementDetailDrawer } from "@/components/dashboard/operator/Capture/CapturePlacementDetailDrawer"
import { useCapturePlacementDetailModule } from "@/components/dashboard/operator/Capture/utils/useCapturePlacementDetailModule"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { operatorDashboardNavPath } from "@/lib/operatorHome/operatorDashboardPaths"

/**
 * Host that crosses only the Placement Detail seam so draft keystrokes do not
 * re-render Capture Body tables/shells subscribed to the live page module.
 */
export function CapturePlacementDetailHost() {
  const navigate = useNavigate()
  const { mode, selectedLocationId } =
    useOutletContext<DashboardOutletContext>()
  const {
    snapshot,
    closePlacementDetail,
    setPlacementDetailDescriptionDraft,
    savePlacementDetailDescription,
    requestPlacementDetailPause,
    requestPlacementDetailActivate,
    requestPlacementDetailRotate,
    requestPlacementDetailArchive,
    copyPlacementDetailLink,
    openPlacementDetailPreview,
    openThankYouOfferDialog,
    getOpenContext,
  } = useCapturePlacementDetailModule()

  return (
    <CapturePlacementDetailDrawer
      snapshot={snapshot}
      onOpenChange={(open) => {
        if (!open) {
          closePlacementDetail()
        }
      }}
      onPreview={openPlacementDetailPreview}
      onCopyLink={copyPlacementDetailLink}
      onOrderPrintMaterials={() => {
        const detailLocationId =
          getOpenContext().locationId ?? selectedLocationId
        navigate(
          operatorDashboardNavPath(mode, "tummly-shop", detailLocationId)
        )
      }}
      onPause={requestPlacementDetailPause}
      onActivate={requestPlacementDetailActivate}
      onRotate={requestPlacementDetailRotate}
      onArchive={requestPlacementDetailArchive}
      onEditGuestForm={openThankYouOfferDialog}
      onEditConnectedOffer={openThankYouOfferDialog}
      onDescriptionDraftChange={setPlacementDetailDescriptionDraft}
      onSaveDescription={() => {
        void savePlacementDetailDescription()
      }}
    />
  )
}

import { CapturePlacementDetailDrawer } from "@/components/dashboard/operator/Capture/CapturePlacementDetailDrawer"
import { useCapturePlacementDetailModule } from "@/components/dashboard/operator/Capture/utils/useCapturePlacementDetailModule"

/**
 * Host that crosses only the Placement Detail seam so draft keystrokes do not
 * re-render Capture Body tables/shells subscribed to the live page module.
 */
export function CapturePlacementDetailHost() {
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
      onPause={requestPlacementDetailPause}
      onActivate={requestPlacementDetailActivate}
      onRotate={requestPlacementDetailRotate}
      onArchive={requestPlacementDetailArchive}
      onDescriptionDraftChange={setPlacementDetailDescriptionDraft}
      onSaveDescription={() => {
        void savePlacementDetailDescription()
      }}
    />
  )
}

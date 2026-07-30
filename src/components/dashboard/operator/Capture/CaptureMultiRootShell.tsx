import { Link, useLocation, useOutletContext } from "react-router-dom"

import { Button } from "@/components/ui/button"
import { CaptureMultiRootBody } from "@/components/dashboard/operator/Capture/CaptureMultiRootBody"
import { CaptureCreateDigitalGuestLinkDialog } from "@/components/dashboard/operator/Capture/CaptureCreateDigitalGuestLinkDialog"
import { useMultiCapturePageModule } from "@/components/dashboard/operator/Capture/utils/useMultiCapturePageModule"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import {
  CAPTURE_PAGE_ACTION_BUTTON_CLASS,
  CAPTURE_PAGE_ACTIONS_CLASS,
  CAPTURE_PAGE_HEADER_COPY_CLASS,
  CAPTURE_PAGE_HEADER_ROW_CLASS,
  CAPTURE_PAGE_STACK_CLASS,
  CAPTURE_PAGE_SUBTITLE_CLASS,
  CAPTURE_PAGE_TITLE_CLASS,
  OPERATOR_CAPTURE_HEADER_ACTIONS_COPY,
  OPERATOR_CAPTURE_MULTI_ROOT_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import { operatorDashboardCaptureArchivePath } from "@/lib/operatorHome/operatorDashboardPaths"

/** Multi Capture root shell — title + Create digital guest link + Archive. */
export function CaptureMultiRootShell() {
  const location = useLocation()
  const { locations } = useOutletContext<DashboardOutletContext>()
  const {
    snapshot,
    openCreateDialog,
    closeCreateDialog,
    setCreateDialogLocationId,
    createDigitalGuestLink,
  } = useMultiCapturePageModule()
  const from = `${location.pathname}${location.search}`
  const archivePath = operatorDashboardCaptureArchivePath("multi", { from })
  const createDialog = snapshot.createDialog

  return (
    <div className={CAPTURE_PAGE_STACK_CLASS}>
      <div className={CAPTURE_PAGE_HEADER_ROW_CLASS}>
        <header className={CAPTURE_PAGE_HEADER_COPY_CLASS}>
          <h1 className={CAPTURE_PAGE_TITLE_CLASS}>
            {OPERATOR_CAPTURE_MULTI_ROOT_COPY.title}
          </h1>
          <p className={CAPTURE_PAGE_SUBTITLE_CLASS}>
            {OPERATOR_CAPTURE_MULTI_ROOT_COPY.description}
          </p>
        </header>
        <div className={CAPTURE_PAGE_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-primary"
            disabled={!snapshot.canCreateDigitalGuestLink}
            className={CAPTURE_PAGE_ACTION_BUTTON_CLASS}
            onClick={() => {
              openCreateDialog()
            }}
          >
            {OPERATOR_CAPTURE_HEADER_ACTIONS_COPY.createDigitalGuestLink}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className={CAPTURE_PAGE_ACTION_BUTTON_CLASS}
            asChild
          >
            <Link to={archivePath} state={{ from }}>
              {OPERATOR_CAPTURE_HEADER_ACTIONS_COPY.archivedPlacements}
            </Link>
          </Button>
        </div>
      </div>
      <CaptureMultiRootBody />
      <CaptureCreateDigitalGuestLinkDialog
        open={createDialog.isOpen}
        onOpenChange={(open) => {
          if (!open) {
            closeCreateDialog()
          }
        }}
        busy={createDialog.busy}
        locationOptions={locations.map((item) => ({
          id: item.id,
          label: item.locationName,
        }))}
        locationBound={createDialog.locationBound}
        selectedLocationId={createDialog.selectedLocationId}
        onLocationIdChange={setCreateDialogLocationId}
        onSubmit={async (input) => createDigitalGuestLink(input)}
      />
    </div>
  )
}

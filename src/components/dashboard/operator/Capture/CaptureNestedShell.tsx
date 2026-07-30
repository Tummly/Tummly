import { ChevronRightIcon } from "lucide-react"
import { Link, useLocation } from "react-router-dom"

import { Button } from "@/components/ui/button"
import { CaptureBody } from "@/components/dashboard/operator/Capture/CaptureBody"
import { CaptureLocationControl } from "@/components/dashboard/operator/Capture/CaptureLocationControl"
import { useCapturePageModule } from "@/components/dashboard/operator/Capture/utils/useCapturePageModule"
import {
  CAPTURE_BREADCRUMB_CURRENT_CLASS,
  CAPTURE_BREADCRUMB_LINK_CLASS,
  CAPTURE_BREADCRUMB_MUTED_LINK_CLASS,
  CAPTURE_BREADCRUMB_NAV_CLASS,
  CAPTURE_NESTED_SUBTITLE_CLASS,
  CAPTURE_PAGE_ACTION_BUTTON_CLASS,
  CAPTURE_PAGE_ACTIONS_CLASS,
  CAPTURE_PAGE_HEADER_COPY_CLASS,
  CAPTURE_PAGE_HEADER_ROW_CLASS,
  CAPTURE_PAGE_STACK_CLASS,
  CAPTURE_PAGE_TITLE_CLASS,
  OPERATOR_CAPTURE_BREADCRUMB_COPY,
  OPERATOR_CAPTURE_HEADER_ACTIONS_COPY,
  OPERATOR_CAPTURE_NESTED_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import { operatorDashboardCaptureArchivePath } from "@/lib/operatorHome/operatorDashboardPaths"

type CaptureNestedShellProps = {
  locationName: string
  selectedLocationId: number
  locations: readonly { id: number; locationName: string }[]
  captureRootPath: string
  onSelectLocation: (locationId: number) => void
}

/** Multi nested Capture shell — breadcrumbs + location name + location control. */
export function CaptureNestedShell({
  locationName,
  selectedLocationId,
  locations,
  captureRootPath,
  onSelectLocation,
}: CaptureNestedShellProps) {
  const { snapshot, openGuestExperiencePreview } = useCapturePageModule()
  const location = useLocation()
  const previewDisabled =
    snapshot.viewModel?.guestExperience.previewEntry.kind === "disabled" ||
    snapshot.viewModel == null
  const from = `${location.pathname}${location.search}`
  const archivePath = operatorDashboardCaptureArchivePath("multi", {
    locationId: selectedLocationId,
    from,
  })

  return (
    <div className={CAPTURE_PAGE_STACK_CLASS}>
      <nav aria-label="Breadcrumb" className={CAPTURE_BREADCRUMB_NAV_CLASS}>
        <Link to={captureRootPath} className={CAPTURE_BREADCRUMB_LINK_CLASS}>
          {OPERATOR_CAPTURE_BREADCRUMB_COPY.capture}
        </Link>
        <ChevronRightIcon
          className="size-4 shrink-0 text-op-text-muted"
          aria-hidden
        />
        <Link
          to={captureRootPath}
          className={CAPTURE_BREADCRUMB_MUTED_LINK_CLASS}
        >
          {OPERATOR_CAPTURE_BREADCRUMB_COPY.allLocations}
        </Link>
        <ChevronRightIcon
          className="size-4 shrink-0 text-op-text-muted"
          aria-hidden
        />
        <span className={CAPTURE_BREADCRUMB_CURRENT_CLASS}>{locationName}</span>
      </nav>

      <div className={CAPTURE_PAGE_HEADER_ROW_CLASS}>
        <header className={CAPTURE_PAGE_HEADER_COPY_CLASS}>
          <h1 className={CAPTURE_PAGE_TITLE_CLASS}>{locationName}</h1>
          <p className={CAPTURE_NESTED_SUBTITLE_CLASS}>
            {OPERATOR_CAPTURE_NESTED_COPY.description}
          </p>
        </header>
        <div className={CAPTURE_PAGE_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-primary"
            disabled
            className={CAPTURE_PAGE_ACTION_BUTTON_CLASS}
          >
            {OPERATOR_CAPTURE_HEADER_ACTIONS_COPY.addPlacement}
          </Button>
          <Button
            type="button"
            variant="op-secondary"
            disabled={previewDisabled}
            className={CAPTURE_PAGE_ACTION_BUTTON_CLASS}
            onClick={openGuestExperiencePreview}
          >
            {OPERATOR_CAPTURE_HEADER_ACTIONS_COPY.previewGuestExperience}
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
          <CaptureLocationControl
            locations={locations}
            selectedLocationId={selectedLocationId}
            selectedLocationName={locationName}
            onSelectLocation={onSelectLocation}
          />
        </div>
      </div>
      <CaptureBody />
    </div>
  )
}

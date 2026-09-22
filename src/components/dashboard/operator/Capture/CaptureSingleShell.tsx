import { ShoppingBag } from "lucide-react"
import { Link, useLocation, useOutletContext } from "react-router-dom"

import { Button } from "@/components/ui/button"
import { CaptureBody } from "@/components/dashboard/operator/Capture/CaptureBody"
import { useCapturePageModule } from "@/components/dashboard/operator/Capture/utils/useCapturePageModule"
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
  OPERATOR_CAPTURE_SINGLE_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import {
  operatorDashboardCaptureArchivePath,
  operatorDashboardNavPath,
} from "@/lib/operatorHome/operatorDashboardPaths"

/** Single-location Capture shell — title + description + header actions. */
export function CaptureSingleShell() {
  const { snapshot, openGuestExperiencePreview } = useCapturePageModule()
  const location = useLocation()
  const { mode, selectedLocationId } =
    useOutletContext<DashboardOutletContext>()
  const previewDisabled =
    snapshot.viewModel?.guestExperience.previewEntry.kind === "disabled" ||
    snapshot.viewModel == null

  const archivePath = operatorDashboardCaptureArchivePath("single", {
    from: `${location.pathname}${location.search}`,
  })
  const shopPath = operatorDashboardNavPath(
    mode,
    "tummly-shop",
    selectedLocationId
  )

  return (
    <div className={CAPTURE_PAGE_STACK_CLASS}>
      <div className={CAPTURE_PAGE_HEADER_ROW_CLASS}>
        <header className={CAPTURE_PAGE_HEADER_COPY_CLASS}>
          <h1 className={CAPTURE_PAGE_TITLE_CLASS}>
            {OPERATOR_CAPTURE_SINGLE_COPY.title}
          </h1>
          <p className={CAPTURE_PAGE_SUBTITLE_CLASS}>
            {OPERATOR_CAPTURE_SINGLE_COPY.description}
          </p>
        </header>
        <div className={CAPTURE_PAGE_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-primary"
            className={CAPTURE_PAGE_ACTION_BUTTON_CLASS}
            asChild
          >
            <Link to={shopPath}>
              <ShoppingBag className="size-4" aria-hidden />
              {OPERATOR_CAPTURE_HEADER_ACTIONS_COPY.orderPrintMaterials}
            </Link>
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
            <Link
              to={archivePath}
              state={{ from: `${location.pathname}${location.search}` }}
            >
              {OPERATOR_CAPTURE_HEADER_ACTIONS_COPY.archivedPlacements}
            </Link>
          </Button>
        </div>
      </div>
      <CaptureBody />
    </div>
  )
}

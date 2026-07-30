import { Button } from "@/components/ui/button"
import { CaptureBody } from "@/components/dashboard/operator/Capture/CaptureBody"
import { useCapturePageModule } from "@/components/dashboard/operator/Capture/utils/useCapturePageModule"
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

/** Single-location Capture shell — title + description + header actions. */
export function CaptureSingleShell() {
  const { snapshot, openGuestExperiencePreview } = useCapturePageModule()
  const previewDisabled =
    snapshot.viewModel?.guestExperience.previewEntry.kind === "disabled" ||
    snapshot.viewModel == null

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
            disabled
            className={CAPTURE_PAGE_ACTION_BUTTON_CLASS}
          >
            {OPERATOR_CAPTURE_HEADER_ACTIONS_COPY.archivedPlacements}
          </Button>
        </div>
      </div>
      <CaptureBody />
    </div>
  )
}

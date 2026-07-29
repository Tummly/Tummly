import { Button } from "@/components/ui/button"
import { CaptureMultiRootBody } from "@/components/dashboard/operator/Capture/CaptureMultiRootBody"
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

/** Multi Capture root shell — title + description + Add QR placement + section shells. */
export function CaptureMultiRootShell() {
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
            disabled
            className={CAPTURE_PAGE_ACTION_BUTTON_CLASS}
          >
            {OPERATOR_CAPTURE_HEADER_ACTIONS_COPY.addPlacement}
          </Button>
        </div>
      </div>
      <CaptureMultiRootBody />
    </div>
  )
}

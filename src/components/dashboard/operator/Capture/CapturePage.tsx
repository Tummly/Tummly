import {
  CAPTURE_EMPTY_HELPER_CLASS,
  CAPTURE_EMPTY_SHELL_CLASS,
  CAPTURE_EMPTY_TITLE_CLASS,
  CAPTURE_PAGE_STACK_CLASS,
  CAPTURE_PAGE_TITLE_CLASS,
  OPERATOR_CAPTURE_COMING_SOON_COPY,
} from "@/lib/operatorCapture/capturePresentation"

/**
 * Shared coming-soon shell for Capture — identical for single- and
 * multi-location dashboards. No lists, filters, KPIs, or per-mode layout.
 */
export function CapturePage() {
  return (
    <div className={CAPTURE_PAGE_STACK_CLASS}>
      <h1 className={CAPTURE_PAGE_TITLE_CLASS}>Capture</h1>
      <div className={CAPTURE_EMPTY_SHELL_CLASS}>
        <div className="flex flex-col items-center gap-2.5 text-center">
          <p className={CAPTURE_EMPTY_TITLE_CLASS}>
            {OPERATOR_CAPTURE_COMING_SOON_COPY.title}
          </p>
          <p className={CAPTURE_EMPTY_HELPER_CLASS}>
            {OPERATOR_CAPTURE_COMING_SOON_COPY.helper}
          </p>
        </div>
      </div>
    </div>
  )
}

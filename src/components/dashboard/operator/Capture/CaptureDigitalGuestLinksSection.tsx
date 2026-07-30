import { Button } from "@/components/ui/button"
import type { OperatorCaptureDigitalGuestLinksView } from "@/lib/operatorCapture/createOperatorCapturePageModule"
import {
  CAPTURE_EMPTY_HELPER_CLASS,
  CAPTURE_EMPTY_TITLE_CLASS,
  CAPTURE_PLACEMENTS_EMPTY_BODY_CLASS,
  CAPTURE_PLACEMENTS_EMPTY_COPY_STACK_CLASS,
  CAPTURE_PLACEMENTS_HEADER_ROW_CLASS,
  CAPTURE_SECTION_CLASS,
  CAPTURE_SECTION_SUBTITLE_CLASS,
  CAPTURE_SECTION_TITLE_CLASS,
  OPERATOR_CAPTURE_SECTION_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import { PERFORMANCE_HEADER_COPY_CLASS } from "@/lib/operatorHome/performanceOverviewPresentation"

type CaptureDigitalGuestLinksSectionProps = {
  digitalGuestLinks: OperatorCaptureDigitalGuestLinksView
}

/** Digital guest links section — empty chrome with header Create (wiring deferred). */
export function CaptureDigitalGuestLinksSection({
  digitalGuestLinks,
}: CaptureDigitalGuestLinksSectionProps) {
  const copy = OPERATOR_CAPTURE_SECTION_COPY.digitalGuestLinks

  return (
    <section className={CAPTURE_SECTION_CLASS}>
      <div className={CAPTURE_PLACEMENTS_HEADER_ROW_CLASS}>
        <div className={PERFORMANCE_HEADER_COPY_CLASS}>
          <div className="leading-[0]">
            <h2 className={CAPTURE_SECTION_TITLE_CLASS}>{copy.title}</h2>
          </div>
          <div className="leading-[0]">
            <p className={CAPTURE_SECTION_SUBTITLE_CLASS}>{copy.description}</p>
          </div>
        </div>

        <Button type="button" variant="op-primary" disabled>
          {copy.createCta}
        </Button>
      </div>

      {digitalGuestLinks.isEmpty ? (
        <div className={CAPTURE_PLACEMENTS_EMPTY_BODY_CLASS}>
          <div className={CAPTURE_PLACEMENTS_EMPTY_COPY_STACK_CLASS}>
            <p className={CAPTURE_EMPTY_TITLE_CLASS}>{copy.emptyTitle}</p>
            <p className={`${CAPTURE_EMPTY_HELPER_CLASS} max-w-[450px]`}>
              {copy.emptyHelper}
            </p>
          </div>
        </div>
      ) : null}
    </section>
  )
}

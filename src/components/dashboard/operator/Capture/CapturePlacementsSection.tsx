import { CapturePlacementsTable } from "@/components/dashboard/operator/Capture/CapturePlacementsTable"
import { Button } from "@/components/ui/button"
import type { OperatorCapturePlacementsView } from "@/lib/operatorCapture/createOperatorCapturePageModule"
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

type CapturePlacementsSectionProps = {
  placements: OperatorCapturePlacementsView
  pauseActivateEnabled?: boolean
  onViewDetails: (qrCodeId: number) => void
  onPausePlacement: (qrCodeId: number) => void
  onResumePlacement: (qrCodeId: number) => void
  onRotatePlacement: (qrCodeId: number) => void
  onCopyPlacementLink: (qrCodeId: number) => void
  onArchivePlacement: (qrCodeId: number) => void
}

/** QR placements section — table or empty state (Add CTA removed above table). */
export function CapturePlacementsSection({
  placements,
  pauseActivateEnabled = true,
  onViewDetails,
  onPausePlacement,
  onResumePlacement,
  onRotatePlacement,
  onCopyPlacementLink,
  onArchivePlacement,
}: CapturePlacementsSectionProps) {
  const copy = OPERATOR_CAPTURE_SECTION_COPY.placements

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
      </div>

      {placements.isEmpty ? (
        <div className={CAPTURE_PLACEMENTS_EMPTY_BODY_CLASS}>
          <div className={CAPTURE_PLACEMENTS_EMPTY_COPY_STACK_CLASS}>
            <p className={CAPTURE_EMPTY_TITLE_CLASS}>{copy.emptyTitle}</p>
            <p className={`${CAPTURE_EMPTY_HELPER_CLASS} max-w-[292px]`}>
              {copy.emptyHelper}
            </p>
          </div>
          <Button type="button" variant="op-primary" disabled>
            {copy.addCta}
          </Button>
        </div>
      ) : (
        <CapturePlacementsTable
          rows={placements.rows}
          pauseActivateEnabled={pauseActivateEnabled}
          onViewDetails={onViewDetails}
          onPausePlacement={onPausePlacement}
          onResumePlacement={onResumePlacement}
          onRotatePlacement={onRotatePlacement}
          onCopyPlacementLink={onCopyPlacementLink}
          onArchivePlacement={onArchivePlacement}
        />
      )}
    </section>
  )
}

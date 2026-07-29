import { CaptureGuestExperiencePreviewOverlay } from "@/components/dashboard/operator/Capture/CaptureGuestExperiencePreviewOverlay"
import { CaptureGuestExperienceSection } from "@/components/dashboard/operator/Capture/CaptureGuestExperienceSection"
import { CaptureMaterialsSection } from "@/components/dashboard/operator/Capture/CaptureMaterialsSection"
import { CapturePerformanceSection } from "@/components/dashboard/operator/Capture/CapturePerformanceSection"
import { CapturePlacementsSection } from "@/components/dashboard/operator/Capture/CapturePlacementsSection"
import { useDashboardUiStore } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { useCapturePageModule } from "@/components/dashboard/operator/Capture/utils/useCapturePageModule"
import {
  CAPTURE_PAGE_STACK_CLASS,
  CAPTURE_SECTION_CLASS,
  CAPTURE_SECTION_HEADER_CLASS,
  CAPTURE_SECTION_PLACEHOLDER_CLASS,
  CAPTURE_SECTION_SUBTITLE_CLASS,
  CAPTURE_SECTION_TITLE_CLASS,
  OPERATOR_CAPTURE_SECTION_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"

function CaptureSectionShell({
  title,
  description,
}: {
  title: string
  description: string
}) {
  return (
    <section className={CAPTURE_SECTION_CLASS}>
      <header className={CAPTURE_SECTION_HEADER_CLASS}>
        <h2 className={CAPTURE_SECTION_TITLE_CLASS}>{title}</h2>
        <p className={CAPTURE_SECTION_SUBTITLE_CLASS}>{description}</p>
      </header>
      <div className={CAPTURE_SECTION_PLACEHOLDER_CLASS} aria-hidden />
    </section>
  )
}

/** Shared per-location Capture body — performance + guest experience + placements + materials. */
export function CaptureBody() {
  const {
    snapshot,
    reloadForCapturePerformanceDateRange,
    pausePlacement,
    resumePlacement,
    copyPlacementLink,
    openGuestExperiencePreview,
    closeGuestExperiencePreview,
  } = useCapturePageModule()
  const capturePerformanceDateRange = useDashboardUiStore(
    (state) => state.capturePerformanceDateRange
  )
  const setCapturePerformanceDateRange = useDashboardUiStore(
    (state) => state.setCapturePerformanceDateRange
  )

  const viewModel = snapshot.viewModel

  const handleCommitCapturePerformanceDateRange = (
    range: HomePerformanceDateRange
  ) => {
    setCapturePerformanceDateRange(range)
    void reloadForCapturePerformanceDateRange()
  }

  return (
    <div className={CAPTURE_PAGE_STACK_CLASS}>
      {viewModel != null ? (
        <CapturePerformanceSection
          dateRangeLabel={viewModel.dateRangeLabel}
          selectedRange={capturePerformanceDateRange}
          performance={viewModel.performance}
          onCommitRange={handleCommitCapturePerformanceDateRange}
        />
      ) : (
        <CaptureSectionShell
          title={OPERATOR_CAPTURE_SECTION_COPY.performance.title}
          description={OPERATOR_CAPTURE_SECTION_COPY.performance.description}
        />
      )}
      {viewModel != null ? (
        <>
          <CaptureGuestExperienceSection
            guestExperience={viewModel.guestExperience}
            onPreviewGuestExperience={openGuestExperiencePreview}
          />
          <CaptureGuestExperiencePreviewOverlay
            open={snapshot.isGuestExperiencePreviewOpen}
            guestExperience={viewModel.guestExperience}
            onClose={closeGuestExperiencePreview}
          />
        </>
      ) : (
        <CaptureSectionShell
          title={OPERATOR_CAPTURE_SECTION_COPY.guestExperience.title}
          description={OPERATOR_CAPTURE_SECTION_COPY.guestExperience.description}
        />
      )}
      {viewModel != null ? (
        <CapturePlacementsSection
          placements={viewModel.placements}
          onPausePlacement={pausePlacement}
          onResumePlacement={resumePlacement}
          onCopyPlacementLink={copyPlacementLink}
        />
      ) : (
        <CaptureSectionShell
          title={OPERATOR_CAPTURE_SECTION_COPY.placements.title}
          description={OPERATOR_CAPTURE_SECTION_COPY.placements.description}
        />
      )}
      <CaptureMaterialsSection />
    </div>
  )
}

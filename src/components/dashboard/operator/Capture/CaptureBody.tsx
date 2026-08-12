import { CaptureDigitalGuestLinksSection } from "@/components/dashboard/operator/Capture/CaptureDigitalGuestLinksSection"
import { CaptureGuestExperiencePreviewOverlay } from "@/components/dashboard/operator/Capture/CaptureGuestExperiencePreviewOverlay"
import { CaptureGuestExperiencePreviewPickerDialog } from "@/components/dashboard/operator/Capture/CaptureGuestExperiencePreviewPickerDialog"
import { CaptureGuestExperienceSection } from "@/components/dashboard/operator/Capture/CaptureGuestExperienceSection"
import { CaptureMaterialsSection } from "@/components/dashboard/operator/Capture/CaptureMaterialsSection"
import { CapturePauseActivateConfirmDialog } from "@/components/dashboard/operator/Capture/CapturePauseActivateConfirmDialog"
import { CapturePerformanceSection } from "@/components/dashboard/operator/Capture/CapturePerformanceSection"
import { CapturePlacementDetailHost } from "@/components/dashboard/operator/Capture/CapturePlacementDetailHost"
import { CapturePlacementsSection } from "@/components/dashboard/operator/Capture/CapturePlacementsSection"
import { CaptureRotateConfirmDialog } from "@/components/dashboard/operator/Capture/CaptureRotateConfirmDialog"
import { CaptureThankYouOfferDialog } from "@/components/dashboard/operator/Capture/CaptureThankYouOfferDialog"
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

/** Shared per-location Capture body — performance → guest experience → placements → digital links → materials. */
export function CaptureBody() {
  const {
    snapshot,
    reloadForCapturePerformanceDateRange,
    requestPauseConfirm,
    requestActivateConfirm,
    cancelPauseActivateConfirm,
    confirmPauseActivate,
    copyPlacementLink,
    createDigitalGuestLink,
    openPlacementPreview,
    archivePlacement,
    requestDigitalGuestLinkArchive,
    openGuestExperiencePreview,
    closeGuestExperiencePreview,
    closeGuestExperiencePreviewPicker,
    selectGuestExperiencePreviewPickerOption,
    confirmGuestExperiencePreviewPicker,
    openThankYouOfferDialog,
    closeThankYouOfferDialog,
    selectThankYouOfferStance,
    patchThankYouCreateDraft,
    confirmThankYouCreateOffer,
    setThankYouExistingSearchQuery,
    selectThankYouExistingOffer,
    retryThankYouExistingPicker,
    backThankYouOfferStances,
    openPlacementDetail,
    requestRotate,
    setRotatePrintMaterialsAcknowledged,
    cancelRotateConfirm,
    confirmRotate,
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
            onViewOffers={openThankYouOfferDialog}
          />
          <CaptureThankYouOfferDialog
            dialog={snapshot.thankYouOfferDialog}
            locationName={viewModel.locationName}
            onOpenChange={(open) => {
              if (!open) {
                closeThankYouOfferDialog()
              }
            }}
            onSelectCreate={() => {
              void selectThankYouOfferStance("create-new-offer")
            }}
            onSelectExisting={() => {
              void selectThankYouOfferStance("existing-offer")
            }}
            onClear={() => {
              void selectThankYouOfferStance("clear-offer")
            }}
            onBackToStances={backThankYouOfferStances}
            onPatchCreateDraft={patchThankYouCreateDraft}
            onConfirmCreate={() => {
              void confirmThankYouCreateOffer()
            }}
            onExistingSearchChange={setThankYouExistingSearchQuery}
            onSelectExistingOffer={(offerId) => {
              void selectThankYouExistingOffer(offerId)
            }}
            onRetryExisting={retryThankYouExistingPicker}
          />
          <CaptureGuestExperiencePreviewOverlay
            open={snapshot.isGuestExperiencePreviewOpen}
            guestExperience={viewModel.guestExperience}
            previewPlacementLabel={
              snapshot.guestExperiencePreviewPlacementLabel
            }
            onClose={closeGuestExperiencePreview}
          />
          <CaptureGuestExperiencePreviewPickerDialog
            picker={snapshot.guestExperiencePreviewPicker}
            onOpenChange={(open) => {
              if (!open) {
                closeGuestExperiencePreviewPicker()
              }
            }}
            onSelectOption={(qrCodeId) => {
              selectGuestExperiencePreviewPickerOption(qrCodeId)
            }}
            onConfirm={() => {
              confirmGuestExperiencePreviewPicker()
            }}
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
          pauseActivateEnabled={!viewModel.perCodePauseActivateLocked}
          onViewDetails={openPlacementDetail}
          onPausePlacement={requestPauseConfirm}
          onResumePlacement={requestActivateConfirm}
          onRotatePlacement={requestRotate}
          onCopyPlacementLink={copyPlacementLink}
          onArchivePlacement={archivePlacement}
        />
      ) : (
        <CaptureSectionShell
          title={OPERATOR_CAPTURE_SECTION_COPY.placements.title}
          description={OPERATOR_CAPTURE_SECTION_COPY.placements.description}
        />
      )}
      {viewModel != null ? (
        <CaptureDigitalGuestLinksSection
          digitalGuestLinks={viewModel.digitalGuestLinks}
          pauseActivateEnabled={!viewModel.perCodePauseActivateLocked}
          onCreate={createDigitalGuestLink}
          onViewDetails={openPlacementDetail}
          onPreview={openPlacementPreview}
          onPause={requestPauseConfirm}
          onActivate={requestActivateConfirm}
          onCopyLink={copyPlacementLink}
          onArchive={requestDigitalGuestLinkArchive}
        />
      ) : (
        <CaptureSectionShell
          title={OPERATOR_CAPTURE_SECTION_COPY.digitalGuestLinks.title}
          description={
            OPERATOR_CAPTURE_SECTION_COPY.digitalGuestLinks.description
          }
        />
      )}
      <CaptureMaterialsSection />
      <CapturePlacementDetailHost />
      <CapturePauseActivateConfirmDialog
        snapshot={snapshot.pauseActivateConfirm}
        onOpenChange={(open) => {
          if (!open) {
            cancelPauseActivateConfirm()
          }
        }}
        onConfirm={confirmPauseActivate}
      />
      <CaptureRotateConfirmDialog
        confirm={snapshot.rotateConfirm}
        onOpenChange={(open) => {
          if (!open) {
            cancelRotateConfirm()
          }
        }}
        onAcknowledgedChange={setRotatePrintMaterialsAcknowledged}
        onConfirm={confirmRotate}
      />
    </div>
  )
}

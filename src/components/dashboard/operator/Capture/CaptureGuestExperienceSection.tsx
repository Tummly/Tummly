import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import type { OperatorCaptureGuestExperienceView } from "@/lib/operatorCapture/buildCaptureGuestExperience"
import {
  CAPTURE_GUEST_EXPERIENCE_ACTIONS_CLASS,
  CAPTURE_GUEST_EXPERIENCE_LABEL_CLASS,
  CAPTURE_GUEST_EXPERIENCE_ROW_CLASS,
  CAPTURE_GUEST_EXPERIENCE_ROWS_CLASS,
  CAPTURE_GUEST_EXPERIENCE_VALUE_CLASS,
  CAPTURE_SECTION_CLASS,
  CAPTURE_SECTION_SUBTITLE_CLASS,
  CAPTURE_SECTION_TITLE_CLASS,
  OPERATOR_CAPTURE_SECTION_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import { PERFORMANCE_HEADER_COPY_CLASS } from "@/lib/operatorHome/performanceOverviewPresentation"

type CaptureGuestExperienceSectionProps = {
  guestExperience: OperatorCaptureGuestExperienceView
  onPreviewGuestExperience: () => void
  onViewOffers: () => void
}

function GuestExperienceRow({
  label,
  value,
}: {
  label: string
  value: string
}) {
  return (
    <div className={CAPTURE_GUEST_EXPERIENCE_ROW_CLASS}>
      <span className={CAPTURE_GUEST_EXPERIENCE_LABEL_CLASS}>{label}</span>
      <span className={CAPTURE_GUEST_EXPERIENCE_VALUE_CLASS}>{value}</span>
    </div>
  )
}

/** Guest experience summary — Figma five-row chrome, preview + thank-you offers. */
export function CaptureGuestExperienceSection({
  guestExperience,
  onPreviewGuestExperience,
  onViewOffers,
}: CaptureGuestExperienceSectionProps) {
  const copy = OPERATOR_CAPTURE_SECTION_COPY.guestExperience
  const previewDisabled = guestExperience.previewEntry.kind === "disabled"

  return (
    <section className={CAPTURE_SECTION_CLASS}>
      <div className="flex flex-col gap-[26px]">
        <header className={PERFORMANCE_HEADER_COPY_CLASS}>
          <div className="leading-[0]">
            <h2 className={CAPTURE_SECTION_TITLE_CLASS}>{copy.title}</h2>
          </div>
          <div className="leading-[0]">
            <p className={CAPTURE_SECTION_SUBTITLE_CLASS}>{copy.description}</p>
          </div>
        </header>

        <div className={CAPTURE_GUEST_EXPERIENCE_ROWS_CLASS}>
          <GuestExperienceRow
            label={copy.guestFormsLabel}
            value={guestExperience.guestFormsText}
          />
          <Separator className="bg-op-border-default" />
          <GuestExperienceRow
            label={copy.qrPlacementsLabel}
            value={
              guestExperience.activeQrPlanUsageText !== ""
                ? guestExperience.activeQrPlanUsageText
                : guestExperience.qrPlacementsText
            }
          />
          <Separator className="bg-op-border-default" />
          <GuestExperienceRow
            label={copy.connectedOffersLabel}
            value={guestExperience.connectedOffersText}
          />
          <Separator className="bg-op-border-default" />
          <GuestExperienceRow
            label={copy.needsAttentionLabel}
            value={guestExperience.needsAttentionText}
          />
          <Separator className="bg-op-border-default" />
          <GuestExperienceRow
            label={copy.lastJourneyUpdateLabel}
            value={guestExperience.lastJourneyUpdateText}
          />
        </div>

        <div className={CAPTURE_GUEST_EXPERIENCE_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-secondary"
            disabled={previewDisabled}
            onClick={onPreviewGuestExperience}
          >
            {copy.previewCta}
          </Button>
          <Button type="button" variant="op-tertiary" disabled>
            {copy.manageGuestFormsCta}
          </Button>
          <Button type="button" variant="op-tertiary" onClick={onViewOffers}>
            {copy.viewOffersCta}
          </Button>
        </div>
      </div>
    </section>
  )
}

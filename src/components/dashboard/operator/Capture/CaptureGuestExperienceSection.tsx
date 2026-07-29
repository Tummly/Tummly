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
}

/** Guest experience summary — live Active QR count, stub offers, preview + disabled CTAs. */
export function CaptureGuestExperienceSection({
  guestExperience,
  onPreviewGuestExperience,
}: CaptureGuestExperienceSectionProps) {
  const copy = OPERATOR_CAPTURE_SECTION_COPY.guestExperience

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
          <div className={CAPTURE_GUEST_EXPERIENCE_ROW_CLASS}>
            <span className={CAPTURE_GUEST_EXPERIENCE_LABEL_CLASS}>
              {copy.activeQrLabel}
            </span>
            <span className={CAPTURE_GUEST_EXPERIENCE_VALUE_CLASS}>
              {guestExperience.activeQrCount ?? "—"}
            </span>
          </div>
          <Separator className="bg-op-border-default" />
          <div className={CAPTURE_GUEST_EXPERIENCE_ROW_CLASS}>
            <span className={CAPTURE_GUEST_EXPERIENCE_LABEL_CLASS}>
              {copy.connectedOffersLabel}
            </span>
            <span className={CAPTURE_GUEST_EXPERIENCE_VALUE_CLASS}>
              {guestExperience.connectedOffersText}
            </span>
          </div>
        </div>

        <div className={CAPTURE_GUEST_EXPERIENCE_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-secondary"
            onClick={onPreviewGuestExperience}
          >
            {copy.previewCta}
          </Button>
          <Button type="button" variant="op-tertiary" disabled>
            {copy.editGuestFormCta}
          </Button>
          <Button type="button" variant="op-tertiary" disabled>
            {copy.viewOfferCta}
          </Button>
        </div>
      </div>
    </section>
  )
}

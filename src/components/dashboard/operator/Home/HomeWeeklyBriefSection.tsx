import { Button } from "@/components/ui/button"
import {
  OPERATOR_HOME_EMPTY_TITLE_SEMIBOLD_CLASS,
  OPERATOR_HOME_HEADER_COPY_CLASS,
  OPERATOR_HOME_SUBTITLE_CLASS,
  OPERATOR_HOME_WHITE_CARD_TITLE_CLASS,
  WEEKLY_BRIEF_CTA_BUTTON_CLASS,
  WEEKLY_BRIEF_CTA_LABEL,
  WEEKLY_BRIEF_CTA_ROW_CLASS,
  WEEKLY_BRIEF_EMPTY_COPY_CLASS,
  WEEKLY_BRIEF_EMPTY_HELPER,
  WEEKLY_BRIEF_EMPTY_TITLE,
  WEEKLY_BRIEF_HEADER_CLASS,
  WEEKLY_BRIEF_SECTION_CLASS,
  WEEKLY_BRIEF_SUBTITLE,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"

/** Figma Weekly brief — component-owned empty full-width card. */
export function HomeWeeklyBriefSection() {
  return (
    <section className={WEEKLY_BRIEF_SECTION_CLASS}>
      <div className={WEEKLY_BRIEF_HEADER_CLASS}>
        <div className={OPERATOR_HOME_HEADER_COPY_CLASS}>
          <h2 className={OPERATOR_HOME_WHITE_CARD_TITLE_CLASS}>Weekly brief</h2>
          <p className={OPERATOR_HOME_SUBTITLE_CLASS}>{WEEKLY_BRIEF_SUBTITLE}</p>
        </div>
      </div>
      <div className={WEEKLY_BRIEF_EMPTY_COPY_CLASS}>
        <p className={OPERATOR_HOME_EMPTY_TITLE_SEMIBOLD_CLASS}>
          {WEEKLY_BRIEF_EMPTY_TITLE}
        </p>
        <p className="m-0 text-sm font-normal leading-normal">
          {WEEKLY_BRIEF_EMPTY_HELPER}
        </p>
      </div>
      <div className={WEEKLY_BRIEF_CTA_ROW_CLASS}>
        <Button
          type="button"
          variant="operator-secondary"
          size="sm"
          className={WEEKLY_BRIEF_CTA_BUTTON_CLASS}
          disabled
          aria-disabled
          aria-label={`${WEEKLY_BRIEF_CTA_LABEL} (unavailable)`}
        >
          {WEEKLY_BRIEF_CTA_LABEL}
        </Button>
      </div>
    </section>
  )
}

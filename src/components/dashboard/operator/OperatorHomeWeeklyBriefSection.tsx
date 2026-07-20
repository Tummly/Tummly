import {
  OPERATOR_HOME_CARD_STACK_CLASS,
  OPERATOR_HOME_EMPTY_TITLE_SEMIBOLD_CLASS,
  OPERATOR_HOME_HEADER_COPY_CLASS,
  OPERATOR_HOME_SUBTITLE_CLASS,
  OPERATOR_HOME_TITLE_CLASS,
  WEEKLY_BRIEF_EMPTY_COPY_CLASS,
  WEEKLY_BRIEF_EMPTY_HELPER,
  WEEKLY_BRIEF_EMPTY_TITLE,
  WEEKLY_BRIEF_HEADER_CLASS,
  WEEKLY_BRIEF_SUBTITLE,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"

/** Figma Weekly brief — component-owned empty full-width card. */
export function OperatorHomeWeeklyBriefSection() {
  return (
    <section className={OPERATOR_HOME_CARD_STACK_CLASS}>
      <div className={WEEKLY_BRIEF_HEADER_CLASS}>
        <div className={OPERATOR_HOME_HEADER_COPY_CLASS}>
          <h2 className={OPERATOR_HOME_TITLE_CLASS}>Weekly brief</h2>
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
    </section>
  )
}

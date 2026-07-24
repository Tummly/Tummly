import {
  OPERATOR_HOME_HEADER_COPY_CLASS,
  OPERATOR_HOME_SUBTITLE_CLASS,
  OPERATOR_HOME_TITLE_CLASS,
  WEEKLY_BRIEF_EMPTY_COPY_CLASS,
  WEEKLY_BRIEF_EMPTY_HELPER,
  WEEKLY_BRIEF_EMPTY_HELPER_CLASS,
  WEEKLY_BRIEF_EMPTY_TITLE,
  WEEKLY_BRIEF_EMPTY_TITLE_CLASS,
  WEEKLY_BRIEF_HEADER_CLASS,
  WEEKLY_BRIEF_SECTION_CLASS,
  WEEKLY_BRIEF_SUBTITLE,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"

/** Figma Weekly brief empty — node 3360:66112. */
export function HomeWeeklyBriefSection() {
  return (
    <section className={WEEKLY_BRIEF_SECTION_CLASS}>
      <div className={WEEKLY_BRIEF_HEADER_CLASS}>
        <div className={OPERATOR_HOME_HEADER_COPY_CLASS}>
          <h2 className={OPERATOR_HOME_TITLE_CLASS}>Weekly brief</h2>
          <p className={OPERATOR_HOME_SUBTITLE_CLASS}>{WEEKLY_BRIEF_SUBTITLE}</p>
        </div>
      </div>
      <div className={WEEKLY_BRIEF_EMPTY_COPY_CLASS}>
        <p className={WEEKLY_BRIEF_EMPTY_TITLE_CLASS}>
          {WEEKLY_BRIEF_EMPTY_TITLE}
        </p>
        <p className={WEEKLY_BRIEF_EMPTY_HELPER_CLASS}>
          {WEEKLY_BRIEF_EMPTY_HELPER}
        </p>
      </div>
    </section>
  )
}

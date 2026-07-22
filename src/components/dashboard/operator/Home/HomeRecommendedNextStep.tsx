import { AiAssistantIcon } from "@/components/ui/ai-assistant-icon"
import {
  OPERATOR_HOME_HEADER_COPY_CLASS,
  OPERATOR_HOME_SUBTITLE_CLASS,
  OPERATOR_HOME_WHITE_CARD_TITLE_CLASS,
  RECOMMENDED_EMPTY_COPY,
  RECOMMENDED_EMPTY_COPY_CLASS,
  RECOMMENDED_HEADER_CLASS,
  RECOMMENDED_INNER_PANEL_CLASS,
  RECOMMENDED_SECTION_CLASS,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"

/** Figma Recommended next step — component-owned empty full-width card. */
export function HomeRecommendedNextStep() {
  return (
    <section className={RECOMMENDED_SECTION_CLASS}>
      <div className={RECOMMENDED_HEADER_CLASS}>
        <AiAssistantIcon size={32} />
        <div className={OPERATOR_HOME_HEADER_COPY_CLASS}>
          <h2 className={OPERATOR_HOME_WHITE_CARD_TITLE_CLASS}>Recommended next step</h2>
          <p className={OPERATOR_HOME_SUBTITLE_CLASS}>
            AI-assisted guidance based on your recent guest activity.
          </p>
        </div>
      </div>
      <div className={RECOMMENDED_INNER_PANEL_CLASS}>
        <p className={RECOMMENDED_EMPTY_COPY_CLASS}>{RECOMMENDED_EMPTY_COPY}</p>
      </div>
    </section>
  )
}

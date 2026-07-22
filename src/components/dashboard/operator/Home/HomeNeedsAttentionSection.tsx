import { ChevronDownIcon } from "lucide-react"

import {
  NEEDS_ATTENTION_EMPTY_COPY,
  OPERATOR_HOME_CARD_PADDED_CLASS,
  OPERATOR_HOME_CHROME_BUTTON_CLASS,
  OPERATOR_HOME_CHROME_ICON_CLASS,
  OPERATOR_HOME_EMPTY_SHELL_CENTERED_CLASS,
  OPERATOR_HOME_EMPTY_TITLE_CLASS,
  OPERATOR_HOME_GRAY_SHELL_TITLE_CLASS,
  OPERATOR_HOME_HEADER_COPY_CLASS,
  OPERATOR_HOME_HEADER_ROW_CLASS,
  OPERATOR_HOME_SUBTITLE_CLASS,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"

/** Figma Needs attention — component-owned empty shell. */
export function HomeNeedsAttentionSection() {
  return (
    <section className={OPERATOR_HOME_CARD_PADDED_CLASS}>
      <div className={OPERATOR_HOME_HEADER_ROW_CLASS}>
        <div className={OPERATOR_HOME_HEADER_COPY_CLASS}>
          <h2 className={OPERATOR_HOME_GRAY_SHELL_TITLE_CLASS}>Needs attention</h2>
          <p className={OPERATOR_HOME_SUBTITLE_CLASS}>
            Review issues that may require action.
          </p>
        </div>
        <span className={OPERATOR_HOME_CHROME_BUTTON_CLASS} aria-hidden>
          <ChevronDownIcon className={OPERATOR_HOME_CHROME_ICON_CLASS} />
        </span>
      </div>
      <div className={OPERATOR_HOME_EMPTY_SHELL_CENTERED_CLASS}>
        <p className={OPERATOR_HOME_EMPTY_TITLE_CLASS}>
          {NEEDS_ATTENTION_EMPTY_COPY}
        </p>
      </div>
    </section>
  )
}

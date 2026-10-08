import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import {
  HOME_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE,
  type OperatorHomeWeeklyBriefViewModel,
} from "@/lib/operatorHome/createOperatorHomePageModule"
import {
  OPERATOR_HOME_HEADER_COPY_CLASS,
  OPERATOR_HOME_SUBTITLE_CLASS,
  OPERATOR_HOME_TITLE_CLASS,
  WEEKLY_BRIEF_BODY_CLASS,
  WEEKLY_BRIEF_DOMAIN_BLOCK_CLASS,
  WEEKLY_BRIEF_DOMAIN_LABEL_CLASS,
  WEEKLY_BRIEF_DOMAIN_SUMMARY_CLASS,
  WEEKLY_BRIEF_EMPTY_COPY_CLASS,
  WEEKLY_BRIEF_EMPTY_HELPER_CLASS,
  WEEKLY_BRIEF_EMPTY_TITLE_CLASS,
  WEEKLY_BRIEF_ERROR_COPY_CLASS,
  WEEKLY_BRIEF_HEADER_CLASS,
  WEEKLY_BRIEF_HEADLINE_CLASS,
  WEEKLY_BRIEF_RETRY_LABEL,
  WEEKLY_BRIEF_SECTION_CLASS,
  WEEKLY_BRIEF_STATUS_SHELL_CLASS,
  WEEKLY_BRIEF_SUBTITLE,
  WEEKLY_BRIEF_WATCH_LIST_CLASS,
  capitalizeWeekStartsOnLabel,
  isWeeklyBriefGenerateDay,
  resolveWeeklyBriefEmptyCopy,
  shouldShowWeeklyBriefWatchNext,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"
import { GUESTS_PAGE_SECONDARY_BUTTON_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import type { WeeklyBriefBody, WeeklyBriefSection } from "@/types/operatorHome"

type HomeWeeklyBriefSectionProps = {
  weeklyBrief: OperatorHomeWeeklyBriefViewModel
  onRetry: () => void
  /** When true, empty copy explains Pilot exclusion. */
  isPilot?: boolean
  /** Workspace week-starts-on; defaults to monday (backend default). */
  weekStartsOn?: string | null
}

function DomainBlock(props: {
  label: string
  section: WeeklyBriefSection
}) {
  return (
    <div className={WEEKLY_BRIEF_DOMAIN_BLOCK_CLASS}>
      <p className={WEEKLY_BRIEF_DOMAIN_LABEL_CLASS}>{props.label}</p>
      <p className={WEEKLY_BRIEF_DOMAIN_SUMMARY_CLASS}>
        {props.section.summary}
      </p>
    </div>
  )
}

function ReadyBody(props: { body: WeeklyBriefBody }) {
  const { body } = props
  return (
    <div className={WEEKLY_BRIEF_BODY_CLASS}>
      <p className={WEEKLY_BRIEF_HEADLINE_CLASS}>{body.headline}</p>
      <DomainBlock label="Capture" section={body.capture} />
      <DomainBlock label="Feedback" section={body.feedback} />
      <DomainBlock label="Offers" section={body.offers} />
      <DomainBlock label="Campaigns" section={body.campaigns} />
      {shouldShowWeeklyBriefWatchNext(body.watchNext) ? (
        <div className={WEEKLY_BRIEF_DOMAIN_BLOCK_CLASS}>
          <p className={WEEKLY_BRIEF_DOMAIN_LABEL_CLASS}>Watch next</p>
          <ul className={WEEKLY_BRIEF_WATCH_LIST_CLASS}>
            {body.watchNext.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </div>
      ) : null}
    </div>
  )
}

/** Figma Weekly brief — node 3360:66112 (empty) + ready / loading / error. */
export function HomeWeeklyBriefSection({
  weeklyBrief,
  onRetry,
  isPilot = false,
  weekStartsOn = null,
}: HomeWeeklyBriefSectionProps) {
  const emptyCopy = resolveWeeklyBriefEmptyCopy({
    isGenerateDay: isWeeklyBriefGenerateDay({ weekStartsOn }),
    isPilot,
    generateWeekdayLabel: capitalizeWeekStartsOnLabel(weekStartsOn),
  })

  return (
    <section className={WEEKLY_BRIEF_SECTION_CLASS} aria-label="Weekly brief">
      <div className={WEEKLY_BRIEF_HEADER_CLASS}>
        <div className={OPERATOR_HOME_HEADER_COPY_CLASS}>
          <h2 className={OPERATOR_HOME_TITLE_CLASS}>Weekly brief</h2>
          <p className={OPERATOR_HOME_SUBTITLE_CLASS}>{WEEKLY_BRIEF_SUBTITLE}</p>
        </div>
      </div>

      {weeklyBrief.status === "empty" ? (
        <div className={WEEKLY_BRIEF_EMPTY_COPY_CLASS}>
          <p className={WEEKLY_BRIEF_EMPTY_TITLE_CLASS}>{emptyCopy.title}</p>
          <p className={WEEKLY_BRIEF_EMPTY_HELPER_CLASS}>{emptyCopy.helper}</p>
        </div>
      ) : null}

      {weeklyBrief.status === "loading" ? (
        <div
          className={WEEKLY_BRIEF_STATUS_SHELL_CLASS}
          role="status"
          aria-live="polite"
          aria-label="Loading weekly brief"
        >
          <Spinner />
        </div>
      ) : null}

      {weeklyBrief.status === "error" ? (
        <div className={WEEKLY_BRIEF_STATUS_SHELL_CLASS}>
          <p className={WEEKLY_BRIEF_ERROR_COPY_CLASS}>
            {weeklyBrief.errorMessage ?? HOME_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE}
          </p>
          {weeklyBrief.errorRetryable ? (
            <Button
              type="button"
              variant="op-secondary"
              className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
              onClick={onRetry}
            >
              {WEEKLY_BRIEF_RETRY_LABEL}
            </Button>
          ) : null}
        </div>
      ) : null}

      {weeklyBrief.status === "ready" && weeklyBrief.body != null ? (
        <ReadyBody body={weeklyBrief.body} />
      ) : null}
    </section>
  )
}

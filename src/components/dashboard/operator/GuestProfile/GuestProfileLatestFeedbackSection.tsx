import { GuestProfileEmptyCopy } from "@/components/dashboard/operator/GuestProfile/GuestProfileEmptyCopy"
import { GuestProfileFeedbackPreviewCell } from "@/components/dashboard/operator/GuestProfile/GuestProfileFeedbackPreviewCell"
import { GuestProfileIssueTagsCell } from "@/components/dashboard/operator/GuestProfile/GuestProfileIssueTagsCell"
import { GuestProfileSectionEmptyCard } from "@/components/dashboard/operator/GuestProfile/GuestProfileSectionEmptyCard"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { TooltipProvider } from "@/components/ui/tooltip"
import {
  GUEST_PROFILE_OPEN_FEEDBACK_LABEL,
  GUEST_PROFILE_START_RECOVERY_LABEL,
  GUEST_PROFILE_VIEW_ALL_FEEDBACKS_LABEL,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGINATION_BUTTON_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_HEADER_ROW_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
  GUESTS_TABLE_LOCATION_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { OperatorGuestProfileLatestFeedbackRow } from "@/types/operatorGuestProfile"

type GuestProfileLatestFeedbackSectionProps = {
  sectionTitle: string
  rows: OperatorGuestProfileLatestFeedbackRow[]
  emptyTitle: string
  emptyHelper: string
  /** When set, empty state keeps an outer section with this helper (edit page). */
  sectionHelper?: string
  onOpenFeedback?: (feedbackId: number) => void
  onStartRecovery?: (feedbackId: number) => void
  onViewAllFeedbacks?: () => void
}

const SECTION_HELPER_CLASS =
  "text-sm leading-5 tracking-[-0.2px] text-muted-foreground"

function ClassificationCell({
  sentiment,
}: {
  sentiment: "positive" | "neutral" | "negative" | null
}) {
  if (sentiment == null) {
    return <span className={GUESTS_TABLE_LOCATION_CLASS}>—</span>
  }

  const label =
    sentiment === "positive"
      ? "Positive"
      : sentiment === "neutral"
        ? "Neutral"
        : "Negative"

  return <Badge variant={sentiment}>{label}</Badge>
}

export function GuestProfileLatestFeedbackSection({
  sectionTitle,
  rows,
  emptyTitle,
  emptyHelper,
  sectionHelper,
  onOpenFeedback,
  onStartRecovery,
  onViewAllFeedbacks,
}: GuestProfileLatestFeedbackSectionProps) {
  if (rows.length === 0) {
    if (sectionHelper != null) {
      return (
        <section className={GUESTS_SECTION_CLASS} aria-label={sectionTitle}>
          <div className="flex flex-col gap-2">
            <h2 className={GUESTS_SECTION_TITLE_CLASS}>{sectionTitle}</h2>
            <p className={SECTION_HELPER_CLASS}>{sectionHelper}</p>
          </div>
          <GuestProfileEmptyCopy title={emptyTitle} helper={emptyHelper} />
        </section>
      )
    }

    return (
      <GuestProfileSectionEmptyCard
        sectionTitle={sectionTitle}
        emptyTitle={emptyTitle}
        emptyHelper={emptyHelper}
      />
    )
  }

  return (
    <section className={GUESTS_SECTION_CLASS} aria-label={sectionTitle}>
      <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{sectionTitle}</h2>
          {sectionHelper != null ? (
            <p className={SECTION_HELPER_CLASS}>{sectionHelper}</p>
          ) : null}
        </div>
      </div>
      <div className={GUESTS_TABLE_FRAME_CLASS}>
        <TooltipProvider delayDuration={200}>
          <Table className={GUESTS_TABLE_CLASS}>
            <TableHeader className="[&_tr]:border-0">
              <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Classification
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Date
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Location
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Source
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Feedback
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Issue tags
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Recovery status
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Actions
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.map((row) => (
                <TableRow key={row.id} className={GUESTS_TABLE_BODY_ROW_CLASS}>
                  <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    <ClassificationCell sentiment={row.classificationDisplay} />
                  </TableCell>
                  <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    <span className={GUESTS_TABLE_LOCATION_CLASS}>
                      {row.dateDisplay}
                    </span>
                  </TableCell>
                  <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    <span className={GUESTS_TABLE_LOCATION_CLASS}>
                      {row.locationName}
                    </span>
                  </TableCell>
                  <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    <Badge variant="tag">{row.sourceDisplay}</Badge>
                  </TableCell>
                  <TableCell
                    className={`${GUESTS_TABLE_BODY_CELL_CLASS} max-w-56`}
                  >
                    <GuestProfileFeedbackPreviewCell
                      text={row.feedbackFullDisplay}
                    />
                  </TableCell>
                  <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    <GuestProfileIssueTagsCell labels={row.issueTagLabels} />
                  </TableCell>
                  <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    <span className={GUESTS_TABLE_LOCATION_CLASS}>
                      {row.recoveryDisplay}
                    </span>
                  </TableCell>
                  <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        type="button"
                        variant="op-tertiary"
                        className={GUESTS_PAGINATION_BUTTON_CLASS}
                        aria-label={GUEST_PROFILE_OPEN_FEEDBACK_LABEL}
                        onClick={() => {
                          onOpenFeedback?.(row.id)
                        }}
                      >
                        {GUEST_PROFILE_OPEN_FEEDBACK_LABEL}
                      </Button>
                      {onStartRecovery != null ? (
                        <Button
                          type="button"
                          variant="op-tertiary"
                          className={GUESTS_PAGINATION_BUTTON_CLASS}
                          aria-label={GUEST_PROFILE_START_RECOVERY_LABEL}
                          onClick={() => {
                            onStartRecovery(row.id)
                          }}
                        >
                          {GUEST_PROFILE_START_RECOVERY_LABEL}
                        </Button>
                      ) : null}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TooltipProvider>
      </div>
      <div>
        <Button
          variant="op-secondary"
          type="button"
          className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
          aria-label={GUEST_PROFILE_VIEW_ALL_FEEDBACKS_LABEL}
          onClick={() => {
            onViewAllFeedbacks?.()
          }}
        >
          {GUEST_PROFILE_VIEW_ALL_FEEDBACKS_LABEL}
        </Button>
      </div>
    </section>
  )
}

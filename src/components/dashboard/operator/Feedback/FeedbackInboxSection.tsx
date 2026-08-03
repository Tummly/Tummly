import type { RefObject } from "react"
import { ChevronDownIcon } from "lucide-react"

import { FeedbackInboxRowActionsMenu } from "@/components/dashboard/operator/Feedback/FeedbackInboxRowActionsMenu"
import { FeedbackInboxTableEmptyState } from "@/components/dashboard/operator/Feedback/FeedbackInboxTableEmptyState"
import { GuestProfileFeedbackPreviewCell } from "@/components/dashboard/operator/GuestProfile/GuestProfileFeedbackPreviewCell"
import { GuestProfileIssueTagsCell } from "@/components/dashboard/operator/GuestProfile/GuestProfileIssueTagsCell"
import { GuestsFilterChipRow } from "@/components/dashboard/operator/Guests/GuestsFilterChipRow"
import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Input } from "@/components/ui/input"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { TooltipProvider } from "@/components/ui/tooltip"
import type { FilterChip } from "@/lib/operatorFilterSheet"
import { feedbackWorkflowStatusLabel } from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import {
  FEEDBACK_PAGE_COPY,
  OPERATOR_FEEDBACK_INBOX_SORT_LABELS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import {
  GUESTS_MARKETING_STATUS_BADGE_CLASS,
  GUESTS_PAGINATION_BUTTON_CLASS,
  GUESTS_PAGINATION_LABEL_CLASS,
  GUESTS_PAGINATION_ROW_CLASS,
  GUESTS_SEARCH_FIELD_CLASS,
  GUESTS_SEARCH_WRAP_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_SMART_GROUPS_STACK_CLASS,
  GUESTS_SORT_BUTTON_CLASS,
  GUESTS_SORT_MENU_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_GUEST_NAME_CLASS,
  GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
  GUESTS_TABLE_ICON_CELL_INNER_CLASS,
  GUESTS_TABLE_LOCATION_CLASS,
  GUESTS_TABLE_ACTIONS_CELL_CLASS,
  GUESTS_TABLE_MENU_ITEM_CLASS,
  GUESTS_TABLE_MENU_ITEM_SELECTED_CLASS,
  GUESTS_TAB_BUTTON_ACTIVE_CLASS,
  GUESTS_TAB_BUTTON_CLASS,
  GUESTS_TAB_BUTTON_INACTIVE_CLASS,
  GUESTS_TAB_COUNT_ACTIVE_CLASS,
  GUESTS_TABLIST_CLASS,
  GUESTS_TABLIST_SCROLL_CLASS,
  GUESTS_TOOLBAR_ACTIONS_CLASS,
  GUESTS_TOOLBAR_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type {
  OperatorFeedbackInboxEmptyStateKind,
  OperatorFeedbackInboxSortId,
  OperatorFeedbackInboxTab,
  OperatorFeedbackInboxTabId,
  OperatorFeedbackInboxTableRow,
} from "@/types/operatorFeedback"
import { cn } from "@/lib/utils"

const SORT_OPTIONS = Object.entries(OPERATOR_FEEDBACK_INBOX_SORT_LABELS) as Array<
  [OperatorFeedbackInboxSortId, string]
>

type FeedbackInboxSectionProps = {
  inboxRef: RefObject<HTMLElement | null>
  tabs: OperatorFeedbackInboxTab[]
  activeTabId: OperatorFeedbackInboxTabId
  onTabChange: (id: OperatorFeedbackInboxTabId) => void
  rows: OperatorFeedbackInboxTableRow[]
  searchQuery: string
  onSearchQueryChange: (query: string) => void
  sortId: OperatorFeedbackInboxSortId
  sortLabel: string
  onSortChange: (id: OperatorFeedbackInboxSortId) => void
  pageRangeLabel: string
  canGoPrevious: boolean
  canGoNext: boolean
  onPreviousPage: () => void
  onNextPage: () => void
  tableEmptyState: OperatorFeedbackInboxEmptyStateKind | null
  onClearSearchAndFilters: () => void
  onChangePeriod: () => void
  filterChips: readonly FilterChip[]
  filterChipCount: number
  onOpenFilters: () => void
  onRemoveFilterChip: (chip: FilterChip) => void
  onOpenFeedbackDetails: (feedbackId: number) => void
  onStartInboxMarkResolved: (feedbackId: number) => void
  onStartInboxMarkNoActionNeeded: (feedbackId: number) => void
}

function SentimentCell({
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

/** Feedback inbox — tabs, toolbar, table, pagination (Guests Smart groups shape). */
export function FeedbackInboxSection({
  inboxRef,
  tabs,
  activeTabId,
  onTabChange,
  rows,
  searchQuery,
  onSearchQueryChange,
  sortId,
  sortLabel,
  onSortChange,
  pageRangeLabel,
  canGoPrevious,
  canGoNext,
  onPreviousPage,
  onNextPage,
  tableEmptyState,
  onClearSearchAndFilters,
  onChangePeriod,
  filterChips,
  filterChipCount,
  onOpenFilters,
  onRemoveFilterChip,
  onOpenFeedbackDetails,
  onStartInboxMarkResolved,
  onStartInboxMarkNoActionNeeded,
}: FeedbackInboxSectionProps) {
  const copy = FEEDBACK_PAGE_COPY.inbox

  return (
    <section
      ref={inboxRef}
      id="feedback-inbox"
      className={GUESTS_SECTION_CLASS}
    >
      <div className={GUESTS_SMART_GROUPS_STACK_CLASS}>
        <header className="flex flex-col gap-2 leading-[0]">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.title}</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{copy.subtitle}</p>
        </header>

        <div className={GUESTS_TABLIST_SCROLL_CLASS}>
          <div
            role="tablist"
            aria-label="Feedback inbox filters"
            className={GUESTS_TABLIST_CLASS}
          >
            {tabs.map((tab) => {
              const selected = tab.id === activeTabId
              return (
                <Button
                  key={tab.id}
                  type="button"
                  variant="op-ghost"
                  role="tab"
                  aria-selected={selected}
                  className={cn(
                    GUESTS_TAB_BUTTON_CLASS,
                    selected
                      ? GUESTS_TAB_BUTTON_ACTIVE_CLASS
                      : GUESTS_TAB_BUTTON_INACTIVE_CLASS
                  )}
                  onClick={() => onTabChange(tab.id)}
                >
                  <span>{tab.label}</span>
                  <span
                    className={cn(selected ? GUESTS_TAB_COUNT_ACTIVE_CLASS : "")}
                  >
                    {tab.count}
                  </span>
                </Button>
              )
            })}
          </div>
        </div>

        <div className={GUESTS_TOOLBAR_ROW_CLASS}>
          <div className={GUESTS_SEARCH_WRAP_CLASS}>
            <OperatorSearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-op-icon-default" />
            <Input
              value={searchQuery}
              onChange={(event) => onSearchQueryChange(event.target.value)}
              aria-label="Search feedback"
              placeholder={copy.searchPlaceholder}
              className={GUESTS_SEARCH_FIELD_CLASS}
            />
          </div>

          <div className={GUESTS_TOOLBAR_ACTIONS_CLASS}>
            <Button
              type="button"
              variant="op-secondary"
              aria-label={
                filterChipCount > 0
                  ? `Filters, ${filterChipCount} applied`
                  : "Filters"
              }
              className="rounded-[2px]"
              onClick={onOpenFilters}
            >
              Filters
              {filterChipCount > 0 ? ` (${filterChipCount})` : null}
            </Button>
            <DropdownMenu modal={false}>
              <DropdownMenuTrigger asChild>
                <Button
                  type="button"
                  variant="op-tertiary"
                  aria-label={`Sort: ${sortLabel}`}
                  className={GUESTS_SORT_BUTTON_CLASS}
                >
                  Sort: {sortLabel}
                  <ChevronDownIcon className="size-3.5 shrink-0" aria-hidden />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className={GUESTS_SORT_MENU_CLASS}>
                {SORT_OPTIONS.map(([id, label]) => (
                  <DropdownMenuItem
                    key={id}
                    className={cn(
                      GUESTS_TABLE_MENU_ITEM_CLASS,
                      id === sortId && GUESTS_TABLE_MENU_ITEM_SELECTED_CLASS
                    )}
                    onClick={() => onSortChange(id)}
                  >
                    {label}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>

        <GuestsFilterChipRow
          chips={filterChips}
          onRemoveChip={onRemoveFilterChip}
        />

        <div role="tabpanel">
          {tableEmptyState ? (
            <FeedbackInboxTableEmptyState
              kind={tableEmptyState}
              onClearSearchAndFilters={
                tableEmptyState === "no-match"
                  ? onClearSearchAndFilters
                  : undefined
              }
              onChangePeriod={
                tableEmptyState === "true-empty" ? onChangePeriod : undefined
              }
            />
          ) : (
            <div className={GUESTS_TABLE_FRAME_CLASS}>
              <TooltipProvider delayDuration={200}>
                <Table className={GUESTS_TABLE_CLASS}>
                  <TableHeader className="[&_tr]:border-0">
                    <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                      <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                        Feedback
                      </TableHead>
                      <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                        Guest response
                      </TableHead>
                      <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                        Issue tags
                      </TableHead>
                      <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                        Guest
                      </TableHead>
                      <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                        Location / source
                      </TableHead>
                      <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                        Submitted
                      </TableHead>
                      <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                        Workflow status
                      </TableHead>
                      <TableHead className={GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS}>
                        <div className={GUESTS_TABLE_ICON_CELL_INNER_CLASS}>
                          Actions
                        </div>
                      </TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {rows.map((row) => (
                      <TableRow
                        key={row.id}
                        className={GUESTS_TABLE_BODY_ROW_CLASS}
                      >
                        <TableCell
                          className={`${GUESTS_TABLE_BODY_CELL_CLASS} max-w-56`}
                        >
                          <Button
                            type="button"
                            variant="link"
                            className="h-auto min-h-0 w-full justify-start p-0 font-semibold"
                            onClick={() => onOpenFeedbackDetails(row.id)}
                          >
                            <GuestProfileFeedbackPreviewCell
                              text={`“${row.feedbackPreview}”`}
                              tooltipText={row.feedbackFull}
                            />
                          </Button>
                        </TableCell>
                        <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                          <SentimentCell sentiment={row.sentiment} />
                        </TableCell>
                        <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                          <GuestProfileIssueTagsCell
                            labels={row.issueTagLabels}
                          />
                        </TableCell>
                        <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                          <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                            {row.guestName}
                          </span>
                        </TableCell>
                        <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                          <span className={GUESTS_TABLE_LOCATION_CLASS}>
                            {row.locationSourceDisplay}
                          </span>
                        </TableCell>
                        <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                          <span className={GUESTS_TABLE_LOCATION_CLASS}>
                            {row.submittedDisplay}
                          </span>
                        </TableCell>
                        <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                          <Badge
                            variant="soft"
                            className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
                          >
                            {feedbackWorkflowStatusLabel(row.workflowStatus)}
                          </Badge>
                        </TableCell>
                        <TableCell className={GUESTS_TABLE_ACTIONS_CELL_CLASS}>
                          <div className={GUESTS_TABLE_ICON_CELL_INNER_CLASS}>
                            <FeedbackInboxRowActionsMenu
                              guestName={row.guestName}
                              workflowStatus={row.workflowStatus}
                              onViewFeedback={() => {
                                onOpenFeedbackDetails(row.id)
                              }}
                              onMarkResolved={() => {
                                onStartInboxMarkResolved(row.id)
                              }}
                              onMarkNoActionNeeded={() => {
                                onStartInboxMarkNoActionNeeded(row.id)
                              }}
                            />
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TooltipProvider>
            </div>
          )}
        </div>

        {!tableEmptyState ? (
          <div className={GUESTS_PAGINATION_ROW_CLASS}>
            <p className={GUESTS_PAGINATION_LABEL_CLASS}>{pageRangeLabel}</p>
            <div className="flex items-center gap-3">
              <Button
                type="button"
                variant="op-secondary"
                disabled={!canGoPrevious}
                aria-disabled={!canGoPrevious}
                aria-label="Previous page"
                className={GUESTS_PAGINATION_BUTTON_CLASS}
                onClick={onPreviousPage}
              >
                Previous
              </Button>
              <Button
                type="button"
                variant="op-secondary"
                disabled={!canGoNext}
                aria-disabled={!canGoNext}
                aria-label="Next page"
                className={GUESTS_PAGINATION_BUTTON_CLASS}
                onClick={onNextPage}
              >
                Next
              </Button>
            </div>
          </div>
        ) : null}
      </div>
    </section>
  )
}

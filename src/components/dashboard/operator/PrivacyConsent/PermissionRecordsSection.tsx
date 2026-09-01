import { GuestsFilterChipRow } from "@/components/dashboard/operator/Guests/GuestsFilterChipRow"
import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import type { FilterChip } from "@/lib/operatorFilterSheet"
import {
  GUESTS_MARKETING_STATUS_BADGE_CLASS,
  GUESTS_SEARCH_FIELD_CLASS,
  GUESTS_SEARCH_WRAP_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_SMART_GROUPS_STACK_CLASS,
  GUESTS_TABLE_ACTIONS_CELL_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS,
  GUESTS_TABLE_EMPTY_COPY_STACK_CLASS,
  GUESTS_TABLE_EMPTY_HELPER_CLASS,
  GUESTS_TABLE_EMPTY_SHELL_CLASS,
  GUESTS_TABLE_EMPTY_TITLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_GUEST_NAME_CLASS,
  GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
  GUESTS_TABLE_ICON_CELL_INNER_CLASS,
  GUESTS_TABLE_LOCATION_CLASS,
  GUESTS_TOOLBAR_ACTIONS_CLASS,
  GUESTS_TOOLBAR_ROW_CLASS,
  GUESTS_PAGINATION_BUTTON_CLASS,
  GUESTS_PAGINATION_LABEL_CLASS,
  GUESTS_PAGINATION_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  PERMISSION_RECORD_CURRENT_STATE_LABELS,
  PRIVACY_CONSENT_CARD_CLASS,
  PRIVACY_CONSENT_PAGE_COPY,
  type PermissionRecordRow,
} from "@/lib/operatorPrivacyConsent/privacyConsentPresentation"
import { cn } from "@/lib/utils"

type PermissionRecordsSectionProps = {
  searchQuery: string
  filterChips: readonly FilterChip[]
  filterChipCount: number
  rows: readonly PermissionRecordRow[]
  empty: boolean
  canViewGuests: boolean
  onSearchQueryChange: (query: string) => void
  onOpenFilters: () => void
  onRemoveFilterChip: (chip: FilterChip) => void
  onClearSearchAndFilters: () => void
  onViewRecord: (recordId: string) => void
  pageRangeLabel: string
  canGoPrevious: boolean
  canGoNext: boolean
  onPreviousPage: () => void
  onNextPage: () => void
}

/** Permission records card — Figma 5746:100788. */
export function PermissionRecordsSection({
  searchQuery,
  filterChips,
  filterChipCount,
  rows,
  empty,
  canViewGuests,
  onSearchQueryChange,
  onOpenFilters,
  onRemoveFilterChip,
  onClearSearchAndFilters,
  onViewRecord,
  pageRangeLabel,
  canGoPrevious,
  canGoNext,
  onPreviousPage,
  onNextPage,
}: PermissionRecordsSectionProps) {
  const copy = PRIVACY_CONSENT_PAGE_COPY

  return (
    <section
      className={cn(PRIVACY_CONSENT_CARD_CLASS, "gap-10")}
      aria-label={copy.permissionRecordsTitle}
    >
      <header className="flex flex-col gap-2 leading-[0]">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>
          {copy.permissionRecordsTitle}
        </h2>
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
          {copy.permissionRecordsSubtitle}
        </p>
      </header>

      <div className={GUESTS_SMART_GROUPS_STACK_CLASS}>
        <div className={GUESTS_TOOLBAR_ROW_CLASS}>
          <div className={GUESTS_SEARCH_WRAP_CLASS}>
            <OperatorSearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-op-icon-default" />
            <Input
              value={searchQuery}
              onChange={(event) => onSearchQueryChange(event.target.value)}
              aria-label={copy.permissionRecordsSearchPlaceholder}
              placeholder={copy.permissionRecordsSearchPlaceholder}
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
                  : copy.permissionRecordsFiltersLabel
              }
              className="rounded-[2px]"
              onClick={onOpenFilters}
            >
              {copy.permissionRecordsFiltersLabel}
              {filterChipCount > 0 ? ` (${filterChipCount})` : null}
            </Button>
          </div>
        </div>

        <GuestsFilterChipRow
          chips={filterChips}
          onRemoveChip={onRemoveFilterChip}
        />

        {empty ? (
          <div className={GUESTS_TABLE_EMPTY_SHELL_CLASS}>
            <div className={GUESTS_TABLE_EMPTY_COPY_STACK_CLASS}>
              <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>
                {copy.permissionRecordsEmptyTitle}
              </p>
              <p className={GUESTS_TABLE_EMPTY_HELPER_CLASS}>
                {copy.permissionRecordsEmptyHelper}
              </p>
            </div>
            <div className="mt-[30px] flex items-center justify-center">
              <Button
                type="button"
                variant="op-tertiary"
                className={GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS}
                onClick={onClearSearchAndFilters}
              >
                {copy.permissionRecordsClearSearchAndFilters}
              </Button>
            </div>
          </div>
        ) : (
          <div className={GUESTS_TABLE_FRAME_CLASS}>
            <Table className={GUESTS_TABLE_CLASS}>
              <TableHeader className="[&_tr]:border-0">
                <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                  <TableHead
                    className={cn(GUESTS_TABLE_HEAD_CELL_CLASS, "min-w-[230px]")}
                  >
                    Guest
                  </TableHead>
                  <TableHead
                    className={cn(GUESTS_TABLE_HEAD_CELL_CLASS, "min-w-[260px]")}
                  >
                    Permission
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Current state
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Location
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Source
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Recorded
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS}>
                    <div className={GUESTS_TABLE_ICON_CELL_INNER_CLASS}>
                      Action
                    </div>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rows.map((row) => (
                  <TableRow key={row.id} className={GUESTS_TABLE_BODY_ROW_CLASS}>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                        {row.guestName}
                      </span>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <Badge
                        variant="soft"
                        className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
                      >
                        {row.permissionLabel}
                      </Badge>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <span className={GUESTS_TABLE_LOCATION_CLASS}>
                        {
                          PERMISSION_RECORD_CURRENT_STATE_LABELS[
                            row.currentState
                          ]
                        }
                      </span>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <span className="text-sm font-normal leading-[19px] text-foreground">
                        {row.locationLabel}
                      </span>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <Badge
                        variant="soft"
                        className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
                      >
                        {row.sourceLabel}
                      </Badge>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <span className={GUESTS_TABLE_LOCATION_CLASS}>
                        {row.recordedLabel}
                      </span>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_ACTIONS_CELL_CLASS}>
                      <div className={GUESTS_TABLE_ICON_CELL_INNER_CLASS}>
                        <Button
                          type="button"
                          variant="op-tertiary"
                          className="h-10 rounded-[2px] px-[17px] py-[11px]"
                          disabled={!canViewGuests}
                          aria-disabled={!canViewGuests}
                          aria-label={
                            canViewGuests
                              ? copy.permissionRecordsView
                              : `${copy.permissionRecordsView} (unavailable)`
                          }
                          onClick={() => {
                            onViewRecord(row.id)
                          }}
                        >
                          {copy.permissionRecordsView}
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}

        {!empty ? (
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

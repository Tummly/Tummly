import { ChevronDownIcon } from "lucide-react"

import { GuestsFilterChipRow } from "@/components/dashboard/operator/Guests/GuestsFilterChipRow"
import { LocationsRowActionsMenu } from "@/components/dashboard/operator/Locations/LocationsRowActionsMenu"
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
import type { FilterChip } from "@/lib/operatorFilterSheet"
import type {
  LocationsSnapshot,
} from "@/lib/operatorLocations/createOperatorLocationsPageModule"
import {
  LOCATION_LIFECYCLE_LABELS,
  LOCATION_SETUP_LABELS,
  LOCATIONS_CARD_CLASS,
  LOCATIONS_PAGE_COPY,
  LOCATIONS_SORT_OPTIONS,
  LOCATIONS_SORT_BUTTON_CLASS,
  LOCATIONS_SORT_MENU_CLASS,
  LOCATIONS_TABLE_MENU_ITEM_CLASS,
  LOCATIONS_TABLE_MENU_ITEM_SELECTED_CLASS,
  locationLifecycleBadgeVariant,
  type LocationRowActionId,
  type LocationsSortId,
} from "@/lib/operatorLocations/locationsPresentation"
import {
  GUESTS_MARKETING_STATUS_BADGE_CLASS,
  GUESTS_PAGINATION_BUTTON_CLASS,
  GUESTS_PAGINATION_LABEL_CLASS,
  GUESTS_PAGINATION_ROW_CLASS,
  GUESTS_SEARCH_FIELD_CLASS,
  GUESTS_SEARCH_WRAP_CLASS,
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
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"

type LocationsTableSectionProps = {
  snap: LocationsSnapshot
  onSearchQueryChange: (query: string) => void
  onSortChange: (id: LocationsSortId) => void
  onOpenFilters: () => void
  onRemoveFilterChip: (chip: FilterChip) => void
  onClearSearchAndFilters: () => void
  onPreviousPage: () => void
  onNextPage: () => void
  onRowAction: (locationId: string, actionId: LocationRowActionId) => void
}

export function LocationsTableSection({
  snap,
  onSearchQueryChange,
  onSortChange,
  onOpenFilters,
  onRemoveFilterChip,
  onClearSearchAndFilters,
  onPreviousPage,
  onNextPage,
  onRowAction,
}: LocationsTableSectionProps) {
  const copy = LOCATIONS_PAGE_COPY

  return (
    <section className={LOCATIONS_CARD_CLASS} aria-label="Locations list">
      <div className={GUESTS_SMART_GROUPS_STACK_CLASS}>
        <div className={GUESTS_TOOLBAR_ROW_CLASS}>
          <div className={GUESTS_SEARCH_WRAP_CLASS}>
            <OperatorSearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-op-icon-default" />
            <Input
              value={snap.searchQuery}
              onChange={(event) => onSearchQueryChange(event.target.value)}
              aria-label={copy.searchPlaceholder}
              placeholder={copy.searchPlaceholder}
              className={GUESTS_SEARCH_FIELD_CLASS}
            />
          </div>

          <div className={GUESTS_TOOLBAR_ACTIONS_CLASS}>
            <Button
              type="button"
              variant="op-secondary"
              aria-label={
                snap.filterChipCount > 0
                  ? `Filters, ${snap.filterChipCount} applied`
                  : copy.filtersLabel
              }
              className="rounded-[2px]"
              onClick={onOpenFilters}
            >
              {copy.filtersLabel}
              {snap.filterChipCount > 0 ? ` (${snap.filterChipCount})` : null}
            </Button>
            <DropdownMenu modal={false}>
              <DropdownMenuTrigger asChild>
                <Button
                  type="button"
                  variant="op-tertiary"
                  aria-label={`Sort: ${snap.sortLabel}`}
                  className={LOCATIONS_SORT_BUTTON_CLASS}
                >
                  Sort: {snap.sortLabel}
                  <ChevronDownIcon className="size-3.5 shrink-0" aria-hidden />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent
                align="end"
                className={LOCATIONS_SORT_MENU_CLASS}
              >
                {LOCATIONS_SORT_OPTIONS.map(([id, label]) => (
                  <DropdownMenuItem
                    key={id}
                    className={cn(
                      LOCATIONS_TABLE_MENU_ITEM_CLASS,
                      id === snap.sortId &&
                        LOCATIONS_TABLE_MENU_ITEM_SELECTED_CLASS
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
          chips={snap.filterChips}
          onRemoveChip={onRemoveFilterChip}
        />

        {snap.empty ? (
          <div className={GUESTS_TABLE_EMPTY_SHELL_CLASS}>
            <div className={GUESTS_TABLE_EMPTY_COPY_STACK_CLASS}>
              <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>{copy.emptyTitle}</p>
              <p className={GUESTS_TABLE_EMPTY_HELPER_CLASS}>
                {copy.emptyHelper}
              </p>
            </div>
            <div className="mt-[30px] flex items-center justify-center">
              <Button
                type="button"
                variant="op-tertiary"
                className={GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS}
                onClick={onClearSearchAndFilters}
              >
                {copy.clearSearchAndFilters}
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
                    Location
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Lifecycle status
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Setup status
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Location manager
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    City / postcode
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    Last activity
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS}>
                    <div className={GUESTS_TABLE_ICON_CELL_INNER_CLASS}>
                      Actions
                    </div>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {snap.rows.map((row) => {
                  const lifecycleVariant = locationLifecycleBadgeVariant(
                    row.lifecycleStatus
                  )
                  const lifecycleLabel =
                    LOCATION_LIFECYCLE_LABELS[row.lifecycleStatus]
                  const setupLabel = LOCATION_SETUP_LABELS[row.setupStatus]
                  const actions = snap.rowActionsById[row.id] ?? []

                  return (
                    <TableRow
                      key={row.id}
                      className={GUESTS_TABLE_BODY_ROW_CLASS}
                    >
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                          {row.name}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {lifecycleVariant == null ? (
                          <span className={GUESTS_TABLE_LOCATION_CLASS}>
                            {lifecycleLabel}
                          </span>
                        ) : (
                          <Badge
                            variant={lifecycleVariant}
                            className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
                          >
                            {lifecycleLabel}
                          </Badge>
                        )}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <Badge
                          variant="soft"
                          className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
                        >
                          {setupLabel}
                        </Badge>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_LOCATION_CLASS}>
                          {row.managerName}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_LOCATION_CLASS}>
                          {row.cityPostcode}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <span className={GUESTS_TABLE_LOCATION_CLASS}>
                          {row.lastActivityLabel}
                        </span>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_ACTIONS_CELL_CLASS}>
                        <div className={GUESTS_TABLE_ICON_CELL_INNER_CLASS}>
                          <LocationsRowActionsMenu
                            locationName={row.name}
                            actions={actions}
                            onAction={(actionId) =>
                              onRowAction(row.id, actionId)
                            }
                          />
                        </div>
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          </div>
        )}

        <div className={GUESTS_PAGINATION_ROW_CLASS}>
          <p className={GUESTS_PAGINATION_LABEL_CLASS}>{snap.pageRangeLabel}</p>
          <div className="flex items-center gap-3">
            <Button
              type="button"
              variant="op-secondary"
              disabled={!snap.canGoPrevious}
              aria-disabled={!snap.canGoPrevious}
              aria-label="Previous page"
              className={GUESTS_PAGINATION_BUTTON_CLASS}
              onClick={onPreviousPage}
            >
              Previous
            </Button>
            <Button
              type="button"
              variant="op-secondary"
              disabled={!snap.canGoNext}
              aria-disabled={!snap.canGoNext}
              aria-label="Next page"
              className={GUESTS_PAGINATION_BUTTON_CLASS}
              onClick={onNextPage}
            >
              Next
            </Button>
          </div>
        </div>
      </div>
    </section>
  )
}

import { useOutletContext } from "react-router-dom"

import { CaptureLocationPerformanceSection } from "@/components/dashboard/operator/Capture/CaptureLocationPerformanceSection"
import { CaptureOverviewSection } from "@/components/dashboard/operator/Capture/CaptureOverviewSection"
import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import { useDashboardUiStore } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { useMultiCapturePageModule } from "@/components/dashboard/operator/Capture/utils/useMultiCapturePageModule"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import {
  CAPTURE_SECTION_CLASS,
  CAPTURE_SECTION_HEADER_CLASS,
  CAPTURE_SECTION_PLACEHOLDER_CLASS,
  CAPTURE_SECTION_SUBTITLE_CLASS,
  CAPTURE_SECTION_TITLE_CLASS,
  OPERATOR_CAPTURE_MULTI_SECTION_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import { captureLocationsFilterSheetSchema } from "@/lib/operatorMultiCapture/captureLocationsFilterSheetSchema"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"

function CaptureMultiSectionShell({
  title,
  description,
}: {
  title: string
  description: string
}) {
  return (
    <section className={CAPTURE_SECTION_CLASS}>
      <header className={CAPTURE_SECTION_HEADER_CLASS}>
        <h2 className={CAPTURE_SECTION_TITLE_CLASS}>{title}</h2>
        <p className={CAPTURE_SECTION_SUBTITLE_CLASS}>{description}</p>
      </header>
      <div className={CAPTURE_SECTION_PLACEHOLDER_CLASS} aria-hidden />
    </section>
  )
}

/** Multi Capture root body — Capture overview + Location performance. */
export function CaptureMultiRootBody() {
  const { locations } = useOutletContext<DashboardOutletContext>()
  const {
    snapshot,
    reloadForMultiCaptureOverviewDateRange,
    navigateToLocationCapture,
    getLocationRowActions,
    setSearchQuery,
    setSortId,
    goToPreviousPage,
    goToNextPage,
    clearSearchAndFilters,
    applyFilters,
    removeFilterChip,
    openFilters,
    closeFilters,
    setFiltersSession,
  } = useMultiCapturePageModule()
  const multiCaptureOverviewDateRange = useDashboardUiStore(
    (state) => state.multiCaptureOverviewDateRange
  )
  const setMultiCaptureOverviewDateRange = useDashboardUiStore(
    (state) => state.setMultiCaptureOverviewDateRange
  )

  const copy = OPERATOR_CAPTURE_MULTI_SECTION_COPY
  const viewModel = snapshot.viewModel

  const handleCommitOverviewDateRange = (range: HomePerformanceDateRange) => {
    setMultiCaptureOverviewDateRange(range)
    void reloadForMultiCaptureOverviewDateRange()
  }

  return (
    <>
      {viewModel != null ? (
        <CaptureOverviewSection
          dateRangeLabel={viewModel.dateRangeLabel}
          selectedRange={multiCaptureOverviewDateRange}
          overview={viewModel.overview}
          onCommitRange={handleCommitOverviewDateRange}
        />
      ) : (
        <CaptureMultiSectionShell
          title={copy.overview.title}
          description={copy.overview.description}
        />
      )}
      {viewModel != null ? (
        <CaptureLocationPerformanceSection
          locationPerformance={viewModel.locationPerformance}
          searchQuery={snapshot.searchQuery}
          sortId={snapshot.sortId}
          filterChips={snapshot.filterChips}
          filterChipCount={snapshot.filterChipCount}
          locationRowActions={getLocationRowActions()}
          onSearchQueryChange={setSearchQuery}
          onSortChange={setSortId}
          onOpenFilters={openFilters}
          onRemoveFilterChip={removeFilterChip}
          onClearSearchAndFilters={clearSearchAndFilters}
          onPreviousPage={goToPreviousPage}
          onNextPage={goToNextPage}
          onNavigateToLocationCapture={navigateToLocationCapture}
        />
      ) : (
        <CaptureMultiSectionShell
          title={copy.locationPerformance.title}
          description={copy.locationPerformance.description}
        />
      )}
      <OperatorFilterSheetDialog
        open={snapshot.filtersSession != null}
        title="Filter locations"
        schema={captureLocationsFilterSheetSchema({
          locations: locations.map((location) => ({
            id: String(location.id),
            label: location.locationName,
          })),
        })}
        session={snapshot.filtersSession}
        chipResolvers={{
          location: (id) =>
            locations.find((location) => String(location.id) === id)
              ?.locationName ?? id,
        }}
        onSessionChange={setFiltersSession}
        onOpenChange={(open) => {
          if (!open) {
            closeFilters()
          }
        }}
        onApply={applyFilters}
      />
    </>
  )
}

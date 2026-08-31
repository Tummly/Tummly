import { useEffect, useSyncExternalStore } from "react"
import { useSearchParams } from "react-router-dom"

import { LocationsActivitySection } from "@/components/dashboard/operator/Locations/LocationsActivitySection"
import { LocationsKpiStrip } from "@/components/dashboard/operator/Locations/LocationsKpiStrip"
import { LocationsSetupReadinessSection } from "@/components/dashboard/operator/Locations/LocationsSetupReadinessSection"
import { LocationsTableSection } from "@/components/dashboard/operator/Locations/LocationsTableSection"
import { useLocationsPageModuleApi } from "@/components/dashboard/operator/Locations/utils/locationsPageModuleContext"
import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import {
  ACCOUNT_WORKSPACE_FULL_BLEED_BOTTOM,
  ACCOUNT_WORKSPACE_FULL_BLEED_X,
  ACCOUNT_WORKSPACE_PAGE_STACK_CLASS,
  ACCOUNT_WORKSPACE_PAGE_SUBTITLE_CLASS,
  ACCOUNT_WORKSPACE_SHELL_PAD_BOTTOM,
  ACCOUNT_WORKSPACE_SHELL_PAD_X,
  ACCOUNT_WORKSPACE_TAB_BODY_CLASS,
  ACCOUNT_WORKSPACE_TAB_LIST_CLASS,
  ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS,
  ACCOUNT_WORKSPACE_TABS_RULE_CLASS,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import {
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { locationsFilterSheetSchema } from "@/lib/operatorLocations/locationsFilterSheetSchema"
import {
  LOCATIONS_PAGE_COPY,
  LOCATIONS_PAGE_PRIMARY_BUTTON_CLASS,
  LOCATIONS_PAGE_SECONDARY_BUTTON_CLASS,
  LOCATIONS_TAB_COUNT_BADGE_CLASS,
  type LocationsTabId,
} from "@/lib/operatorLocations/locationsPresentation"
import { cn } from "@/lib/utils"

export function LocationsPage() {
  const pageModule = useLocationsPageModuleApi()
  const snap = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )
  const [searchParams, setSearchParams] = useSearchParams()
  const copy = LOCATIONS_PAGE_COPY

  useEffect(() => {
    const current = searchParams.get("tab")
    if (current === snap.activeTabId) {
      return
    }
    const next = new URLSearchParams(searchParams)
    next.set("tab", snap.activeTabId)
    setSearchParams(next, { replace: true })
  }, [snap.activeTabId, searchParams, setSearchParams])

  const schema = locationsFilterSheetSchema({
    cities: snap.cityFilterOptions,
  })

  return (
    <div className={ACCOUNT_WORKSPACE_PAGE_STACK_CLASS}>
      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <div className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{copy.title}</h1>
          <p className={ACCOUNT_WORKSPACE_PAGE_SUBTITLE_CLASS}>
            {copy.subtitle}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <Button
            type="button"
            variant="op-primary"
            className={LOCATIONS_PAGE_PRIMARY_BUTTON_CLASS}
          >
            {copy.addLocation}
          </Button>
          <Button
            type="button"
            variant="op-secondary"
            className={LOCATIONS_PAGE_SECONDARY_BUTTON_CLASS}
          >
            {copy.importLocations}
          </Button>
        </div>
      </div>

      <Tabs
        value={snap.activeTabId}
        onValueChange={(value) => {
          pageModule.requestTabChange(value as LocationsTabId)
        }}
        className="flex min-h-0 flex-1 flex-col gap-0"
      >
        <div
          className={cn(
            ACCOUNT_WORKSPACE_FULL_BLEED_X,
            ACCOUNT_WORKSPACE_TABS_RULE_CLASS,
            "shrink-0"
          )}
        >
          <div className={ACCOUNT_WORKSPACE_SHELL_PAD_X}>
            <TabsList
              variant="line"
              className={ACCOUNT_WORKSPACE_TAB_LIST_CLASS}
            >
              {snap.tabs.map((tab) => (
                <TabsTrigger
                  key={tab.id}
                  value={tab.id}
                  className={cn(
                    ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS,
                    "gap-3.5"
                  )}
                >
                  <span>{tab.label}</span>
                  {tab.count != null ? (
                    <span className={LOCATIONS_TAB_COUNT_BADGE_CLASS}>
                      {tab.count}
                    </span>
                  ) : null}
                </TabsTrigger>
              ))}
            </TabsList>
          </div>
        </div>

        <div
          className={cn(
            ACCOUNT_WORKSPACE_FULL_BLEED_X,
            ACCOUNT_WORKSPACE_FULL_BLEED_BOTTOM,
            ACCOUNT_WORKSPACE_SHELL_PAD_X,
            ACCOUNT_WORKSPACE_SHELL_PAD_BOTTOM,
            ACCOUNT_WORKSPACE_TAB_BODY_CLASS
          )}
        >
          <TabsContent value="locations" className="mt-0 flex flex-col gap-5">
            <LocationsKpiStrip kpis={snap.kpis} />
            <LocationsTableSection
              snap={snap}
              onSearchQueryChange={pageModule.setSearchQuery}
              onSortChange={pageModule.setSortId}
              onOpenFilters={pageModule.openFilters}
              onRemoveFilterChip={pageModule.removeFilterChip}
              onClearSearchAndFilters={pageModule.clearSearchAndFilters}
              onPreviousPage={pageModule.goToPreviousPage}
              onNextPage={pageModule.goToNextPage}
              onRowAction={pageModule.onRowAction}
            />
          </TabsContent>

          <TabsContent value="setup-readiness" className="mt-0">
            <LocationsSetupReadinessSection
              items={snap.setupAttentionItems}
              onReviewLocation={pageModule.onReviewSetupAttention}
            />
          </TabsContent>

          <TabsContent value="activity" className="mt-0">
            <LocationsActivitySection items={snap.activityItems} />
          </TabsContent>
        </div>
      </Tabs>

      <OperatorFilterSheetDialog
        open={snap.filtersOpen}
        title={copy.filtersTitle}
        schema={schema}
        session={snap.filtersSession}
        onSessionChange={pageModule.setFiltersSession}
        onOpenChange={(open) => pageModule.setFiltersOpen(open)}
        onApply={() => pageModule.applyFilters()}
      />
    </div>
  )
}

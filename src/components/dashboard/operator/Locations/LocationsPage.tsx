import { useEffect, useState, useSyncExternalStore } from "react"
import { toast } from "sonner"
import { useNavigate, useOutletContext, useSearchParams } from "react-router-dom"

import { LocationsActivitySection } from "@/components/dashboard/operator/Locations/LocationsActivitySection"
import { LocationsAddLocationDialog } from "@/components/dashboard/operator/Locations/LocationsAddLocationDialog"
import { LocationsImportLocationsDialog } from "@/components/dashboard/operator/Locations/LocationsImportLocationsDialog"
import { LocationsKpiStrip } from "@/components/dashboard/operator/Locations/LocationsKpiStrip"
import { LocationsSetManagerDialog } from "@/components/dashboard/operator/Locations/LocationsSetManagerDialog"
import { LocationsSetupReadinessSection } from "@/components/dashboard/operator/Locations/LocationsSetupReadinessSection"
import { LocationsTableSection } from "@/components/dashboard/operator/Locations/LocationsTableSection"
import { useLocationsPageModuleApi } from "@/components/dashboard/operator/Locations/utils/locationsPageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { OperatorDestructiveConfirmDialog } from "@/components/dashboard/operator/OperatorDestructiveConfirmDialog"
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
import {
  operatorDashboardCaptureForLocationPath,
  operatorDashboardLocationDetailPath,
} from "@/lib/operatorHome/operatorDashboardPaths"
import { locationsFilterSheetSchema } from "@/lib/operatorLocations/locationsFilterSheetSchema"
import {
  LOCATIONS_PAGE_COPY,
  LOCATIONS_PAGE_PRIMARY_BUTTON_CLASS,
  LOCATIONS_PAGE_SECONDARY_BUTTON_CLASS,
  LOCATIONS_TAB_COUNT_BADGE_CLASS,
  locationRowActionNeedsConfirm,
  locationRowLifecycleConfirmCopy,
  locationRowLifecycleSuccessToast,
  resolveLocationRowActionNavigation,
  type LocationRowActionId,
  type LocationsSetupAttentionItemId,
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
  const navigate = useNavigate()
  const { mode } = useOutletContext<DashboardOutletContext>()
  const copy = LOCATIONS_PAGE_COPY

  const [addOpen, setAddOpen] = useState(false)
  const [addBusy, setAddBusy] = useState(false)
  const [addError, setAddError] = useState<string | null>(null)

  const [importOpen, setImportOpen] = useState(false)
  const [importBusy, setImportBusy] = useState(false)

  const [deleteTarget, setDeleteTarget] = useState<{
    id: string
    name: string
  } | null>(null)
  const [deleteBusy, setDeleteBusy] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  const [managerTarget, setManagerTarget] = useState<{
    id: string
    name: string
    managerUserId: number | null
  } | null>(null)
  const [managerBusy, setManagerBusy] = useState(false)
  const [managerError, setManagerError] = useState<string | null>(null)

  const [lifecycleConfirm, setLifecycleConfirm] = useState<{
    locationId: string
    name: string
    actionId: "pause-location" | "archive-location" | "restore-location"
  } | null>(null)
  const [lifecycleBusy, setLifecycleBusy] = useState(false)
  const [lifecycleError, setLifecycleError] = useState<string | null>(null)

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

  const lifecycleActionErrorMessage = (error: unknown) =>
    error instanceof Error && error.message.trim().length > 0
      ? error.message
      : copy.lifecycleErrorToast

  const runLifecycleAction = async (
    locationId: string,
    actionId: LocationRowActionId
  ): Promise<boolean> => {
    try {
      await pageModule.onRowAction(locationId, actionId)
      const message = locationRowLifecycleSuccessToast(actionId)
      if (message != null) {
        toast.success(message)
      }
      return true
    } catch (error: unknown) {
      toast.error(lifecycleActionErrorMessage(error))
      return false
    }
  }

  const handleRowAction = (locationId: string, actionId: LocationRowActionId) => {
    const row = snap.rows.find((item) => item.id === locationId)
    if (actionId === "continue-setup") {
      void pageModule
        .activateDraft(locationId)
        .then(() => toast.success("Location activated."))
        .catch((error: unknown) => {
          toast.error(
            error instanceof Error ? error.message : "Could not activate."
          )
        })
      return
    }
    if (actionId === "delete-draft") {
      setDeleteError(null)
      setDeleteTarget({
        id: locationId,
        name: row?.name ?? "this draft",
      })
      return
    }
    if (actionId === "set-manager") {
      setManagerError(null)
      setManagerTarget({
        id: locationId,
        name: row?.name ?? "Location",
        managerUserId: row?.managerUserId ?? null,
      })
      return
    }

    const numericId = Number.parseInt(locationId, 10)
    const navPath = resolveLocationRowActionNavigation(
      mode,
      numericId,
      actionId
    )
    if (navPath != null) {
      navigate(navPath)
      return
    }

    if (actionId === "resume-location") {
      void runLifecycleAction(locationId, actionId)
      return
    }

    if (locationRowActionNeedsConfirm(actionId)) {
      setLifecycleError(null)
      setLifecycleConfirm({
        locationId,
        name: row?.name ?? "this location",
        actionId,
      })
    }
  }

  const handleReviewSetupAttention = (itemId: LocationsSetupAttentionItemId) => {
    if (itemId === "no-active-qr") {
      const item = snap.setupAttentionItems.find(
        (attentionItem) => attentionItem.id === "no-active-qr"
      )
      const firstLocationId = item?.locationIds?.[0]
      if (firstLocationId == null) {
        toast.error(copy.noActiveQrReviewEmptyToast)
        return
      }
      navigate(operatorDashboardCaptureForLocationPath(mode, firstLocationId))
      return
    }

    void pageModule.onReviewSetupAttention(itemId)
  }

  const lifecycleConfirmCopy =
    lifecycleConfirm == null
      ? null
      : locationRowLifecycleConfirmCopy(
          lifecycleConfirm.actionId,
          lifecycleConfirm.name
        )

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
            onClick={() => {
              setAddError(null)
              setAddOpen(true)
            }}
          >
            {copy.addLocation}
          </Button>
          <Button
            type="button"
            variant="op-secondary"
            className={LOCATIONS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={() => setImportOpen(true)}
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
              onRowAction={handleRowAction}
              onOpenLocation={(locationId) => {
                const id = Number.parseInt(locationId, 10)
                if (!Number.isFinite(id)) {
                  return
                }
                navigate(operatorDashboardLocationDetailPath(mode, id))
              }}
            />
          </TabsContent>

          <TabsContent value="setup-readiness" className="mt-0">
            <LocationsSetupReadinessSection
              items={snap.setupAttentionItems}
              onReviewLocation={handleReviewSetupAttention}
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

      <LocationsAddLocationDialog
        open={addOpen}
        busy={addBusy}
        error={addError}
        onOpenChange={setAddOpen}
        onSubmit={async (input) => {
          setAddBusy(true)
          setAddError(null)
          try {
            await pageModule.createDraft(input)
            toast.success("Draft location created.")
          } catch (error) {
            const message =
              error instanceof Error
                ? error.message
                : "Could not create location."
            setAddError(message)
            throw error
          } finally {
            setAddBusy(false)
          }
        }}
      />

      <LocationsImportLocationsDialog
        open={importOpen}
        onOpenChange={setImportOpen}
        isSubmitting={importBusy}
        onConfirm={async (locations) => {
          setImportBusy(true)
          try {
            const result = await pageModule.importDrafts(
              locations.map((location) => ({
                locationName: location.locationName,
                address: location.address,
                city: location.city,
                postcode: location.postcode,
                locationPhone: location.locationPhone,
                localContact: location.localContact,
              }))
            )
            if (result.createdCount > 0 && result.errors.length === 0) {
              toast.success(
                result.createdCount === 1
                  ? "1 draft location imported."
                  : `${result.createdCount} draft locations imported.`
              )
            } else if (result.createdCount > 0) {
              toast.success(
                `Imported ${result.createdCount}; ${result.errors.length} row(s) need attention.`
              )
            }
            if (result.errors.length > 0) {
              toast.error(
                result.errors
                  .slice(0, 3)
                  .map((row) => `Row ${row.rowIndex + 1}: ${row.message}`)
                  .join(" ")
              )
            }
            setImportOpen(false)
          } catch (error) {
            toast.error(
              error instanceof Error
                ? error.message
                : "Could not import locations."
            )
            throw error
          } finally {
            setImportBusy(false)
          }
        }}
      />

      <OperatorDestructiveConfirmDialog
        open={deleteTarget != null}
        busy={deleteBusy}
        error={deleteError}
        title="Delete draft?"
        description={
          deleteTarget == null
            ? ""
            : `Hard-delete “${deleteTarget.name}”? This only works when the draft has no guest or history records.`
        }
        confirmLabel="Delete draft"
        onOpenChange={(open) => {
          if (!open) {
            setDeleteTarget(null)
            setDeleteError(null)
          }
        }}
        onConfirm={async () => {
          if (deleteTarget == null) {
            return
          }
          setDeleteBusy(true)
          setDeleteError(null)
          try {
            await pageModule.deleteDraft(deleteTarget.id)
            toast.success("Draft deleted.")
            setDeleteTarget(null)
          } catch (error) {
            setDeleteError(
              error instanceof Error
                ? error.message
                : "Could not delete draft."
            )
          } finally {
            setDeleteBusy(false)
          }
        }}
      />

      <OperatorDestructiveConfirmDialog
        open={lifecycleConfirm != null}
        busy={lifecycleBusy}
        error={lifecycleError}
        title={lifecycleConfirmCopy?.title ?? ""}
        description={lifecycleConfirmCopy?.description ?? ""}
        confirmLabel={lifecycleConfirmCopy?.confirmLabel ?? "Confirm"}
        busyLabel={lifecycleConfirmCopy?.busyLabel ?? "Updating…"}
        onOpenChange={(open) => {
          if (!open) {
            setLifecycleConfirm(null)
            setLifecycleError(null)
          }
        }}
        onConfirm={async () => {
          if (lifecycleConfirm == null) {
            return
          }
          setLifecycleBusy(true)
          setLifecycleError(null)
          const succeeded = await runLifecycleAction(
            lifecycleConfirm.locationId,
            lifecycleConfirm.actionId
          )
          if (succeeded) {
            setLifecycleConfirm(null)
          } else {
            setLifecycleError(copy.lifecycleErrorToast)
          }
          setLifecycleBusy(false)
        }}
      />

      <LocationsSetManagerDialog
        open={managerTarget != null}
        locationId={managerTarget?.id ?? null}
        locationName={managerTarget?.name ?? "Location"}
        currentManagerUserId={managerTarget?.managerUserId ?? null}
        busy={managerBusy}
        error={managerError}
        onOpenChange={(open) => {
          if (!open) {
            setManagerTarget(null)
            setManagerError(null)
          }
        }}
        onSubmit={async (managerUserId) => {
          if (managerTarget == null) {
            return
          }
          setManagerBusy(true)
          setManagerError(null)
          try {
            await pageModule.setManager(managerTarget.id, managerUserId)
            toast.success(
              managerUserId == null ? "Manager cleared." : "Manager updated."
            )
          } catch (error) {
            const message =
              error instanceof Error
                ? error.message
                : "Could not update manager."
            setManagerError(message)
            throw error
          } finally {
            setManagerBusy(false)
          }
        }}
      />
    </div>
  )
}

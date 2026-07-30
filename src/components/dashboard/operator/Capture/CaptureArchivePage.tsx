import { useEffect, useState } from "react"
import { ChevronDownIcon, SearchIcon } from "lucide-react"
import { Link, useNavigate, useSearchParams } from "react-router-dom"
import { toast } from "sonner"

import { CaptureArchiveTable } from "@/components/dashboard/operator/Capture/CaptureArchiveTable"
import { CaptureCreateDigitalGuestLinkDialog } from "@/components/dashboard/operator/Capture/CaptureCreateDigitalGuestLinkDialog"
import { CaptureGuestExperiencePreviewOverlay } from "@/components/dashboard/operator/Capture/CaptureGuestExperiencePreviewOverlay"
import { CapturePauseActivateConfirmDialog } from "@/components/dashboard/operator/Capture/CapturePauseActivateConfirmDialog"
import { CapturePlacementDetailDrawer } from "@/components/dashboard/operator/Capture/CapturePlacementDetailDrawer"
import { CaptureRestoreConfirmDialog } from "@/components/dashboard/operator/Capture/CaptureRestoreConfirmDialog"
import { CaptureRotateConfirmDialog } from "@/components/dashboard/operator/Capture/CaptureRotateConfirmDialog"
import { useCapturePageModuleApi } from "@/components/dashboard/operator/Capture/utils/capturePageModuleContext"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import {
  CAPTURE_CONNECTED_OFFERS_STUB,
  CAPTURE_PREVIEW_PLACEMENT_LABEL,
} from "@/lib/operatorCapture/buildCaptureGuestExperience"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  CAPTURE_ARCHIVE_PLACEMENT_TYPE_OPTIONS,
  CAPTURE_ARCHIVE_SORT_OPTIONS,
  DEFAULT_CAPTURE_ARCHIVE_FILTERS,
  type CaptureArchiveDatePresetId,
  type CaptureArchiveFilters,
  type CaptureArchiveSortId,
} from "@/lib/operatorCapture/buildCaptureArchive"
import {
  CAPTURE_EMPTY_HELPER_CLASS,
  CAPTURE_EMPTY_TITLE_CLASS,
  CAPTURE_PAGE_ACTION_BUTTON_CLASS,
  CAPTURE_PAGE_HEADER_COPY_CLASS,
  CAPTURE_PAGE_HEADER_ROW_CLASS,
  CAPTURE_PAGE_STACK_CLASS,
  CAPTURE_PAGE_SUBTITLE_CLASS,
  CAPTURE_PAGE_TITLE_CLASS,
  CAPTURE_PAUSE_ACTIVATE_TOAST_DURATION_MS,
  CAPTURE_PLACEMENTS_EMPTY_BODY_CLASS,
  CAPTURE_PLACEMENTS_EMPTY_COPY_STACK_CLASS,
  OPERATOR_CAPTURE_ARCHIVE_COPY,
  OPERATOR_CAPTURE_ROTATE_CONFIRM_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import type { CapturePlacementQrType } from "@/types/dashboard"
import { useSyncExternalStore } from "react"

type CaptureArchivePageProps = {
  mode: "single" | "multi"
  locations: readonly { id: number; locationName: string }[]
  defaultReturnPath: string
}

/** Account-wide Archive screen — list, filters, restore, digital duplicate. */
export function CaptureArchivePage({
  mode,
  locations,
  defaultReturnPath,
}: CaptureArchivePageProps) {
  const pageModule = useCapturePageModuleApi()
  const snapshot = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const copy = OPERATOR_CAPTURE_ARCHIVE_COPY
  const showLocationFilter = mode === "multi"
  const [filtersOpen, setFiltersOpen] = useState(false)
  const [filterDraft, setFilterDraft] = useState<CaptureArchiveFilters>(
    DEFAULT_CAPTURE_ARCHIVE_FILTERS
  )
  const [restoreBusy, setRestoreBusy] = useState(false)
  const [pauseActivateBusy, setPauseActivateBusy] = useState(false)
  const [createBusy, setCreateBusy] = useState(false)
  const [createOpen, setCreateOpen] = useState(false)

  const archive = snapshot.archive
  const returnPath = archive?.returnPath ?? defaultReturnPath
  const drawerDetails = snapshot.placementDetailDrawer.details
  const archivePreviewGuestExperience = {
    guestFormsText: "—",
    qrPlacementsText: "—",
    connectedOffersText: CAPTURE_CONNECTED_OFFERS_STUB,
    needsAttentionText: "—",
    lastJourneyUpdateText: "—",
    previewEntry: { kind: "disabled" as const },
    previewPlacementLabel:
      drawerDetails?.title ?? CAPTURE_PREVIEW_PLACEMENT_LABEL,
    locationName: drawerDetails?.locationName ?? "",
    locationAddress: "",
  }

  useEffect(() => {
    const from = searchParams.get("from") ?? defaultReturnPath
    const locationIdRaw = searchParams.get("locationId")
    const preselectedLocationId =
      locationIdRaw != null && locationIdRaw !== ""
        ? Number(locationIdRaw)
        : null
    void pageModule.enterArchive({
      returnPath: from,
      preselectedLocationId:
        showLocationFilter
        && preselectedLocationId != null
        && !Number.isNaN(preselectedLocationId)
          ? preselectedLocationId
          : null,
      showLocationFilter,
      locations,
    })
  }, [defaultReturnPath, locations, pageModule, searchParams, showLocationFilter])

  useEffect(() => {
    if (archive?.createPrefill != null) {
      setCreateOpen(true)
    }
  }, [archive?.createPrefill])

  const openFilters = () => {
    setFilterDraft(archive?.filters ?? DEFAULT_CAPTURE_ARCHIVE_FILTERS)
    setFiltersOpen(true)
  }

  const applyFilters = () => {
    pageModule.setArchiveFilters(filterDraft)
    setFiltersOpen(false)
  }

  const activeFilterCount = archive?.activeFilterCount ?? 0

  return (
    <div className={CAPTURE_PAGE_STACK_CLASS}>
      <nav
        aria-label="Breadcrumb"
        className="flex items-center gap-2 text-sm text-op-text-muted"
      >
        <Link to={returnPath} className="hover:text-op-text-primary">
          {copy.breadcrumbCapture}
        </Link>
        <span aria-hidden>/</span>
        <span className="text-op-text-primary">{copy.title}</span>
      </nav>

      <div className={CAPTURE_PAGE_HEADER_ROW_CLASS}>
        <header className={CAPTURE_PAGE_HEADER_COPY_CLASS}>
          <h1 className={CAPTURE_PAGE_TITLE_CLASS}>{copy.title}</h1>
          <p className={CAPTURE_PAGE_SUBTITLE_CLASS}>{copy.description}</p>
        </header>
        <Button
          type="button"
          variant="op-tertiary"
          className={CAPTURE_PAGE_ACTION_BUTTON_CLASS}
          onClick={() => {
            navigate(returnPath)
          }}
        >
          {copy.backToCapture}
        </Button>
      </div>

      <div className="flex flex-col gap-6 rounded-op-md border border-op-border-default bg-[var(--op-color-gray-1000)] p-6">
        <div className="flex flex-wrap items-center gap-3">
          <div className="relative min-w-[240px] flex-1">
            <SearchIcon
              className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-op-text-muted"
              aria-hidden
            />
            <Input
              value={archive?.searchQuery ?? ""}
              onChange={(event) => {
                pageModule.setArchiveSearchQuery(event.target.value)
              }}
              placeholder={copy.searchPlaceholder}
              className="h-10 border-0 bg-[var(--op-color-gray-950)] pl-10"
            />
          </div>
          <Button
            type="button"
            variant="op-tertiary"
            className="gap-1"
            onClick={openFilters}
          >
            {activeFilterCount > 0
              ? `${copy.filtersLabel} (${activeFilterCount})`
              : copy.filtersLabel}
            <ChevronDownIcon className="size-3.5" aria-hidden />
          </Button>
          <Select
            value={archive?.sort ?? "recently-archived"}
            onValueChange={(value) => {
              pageModule.setArchiveSort(value as CaptureArchiveSortId)
            }}
          >
            <SelectTrigger className="w-[220px]">
              <SelectValue placeholder="Sort" />
            </SelectTrigger>
            <SelectContent>
              {CAPTURE_ARCHIVE_SORT_OPTIONS.map((option) => (
                <SelectItem key={option.id} value={option.id}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {archive?.isTrueEmpty ? (
          <div className={CAPTURE_PLACEMENTS_EMPTY_BODY_CLASS}>
            <div className={CAPTURE_PLACEMENTS_EMPTY_COPY_STACK_CLASS}>
              <p className={CAPTURE_EMPTY_TITLE_CLASS}>{copy.emptyTitle}</p>
              <p className={`${CAPTURE_EMPTY_HELPER_CLASS} max-w-[320px]`}>
                {copy.emptyHelper}
              </p>
            </div>
          </div>
        ) : archive?.isNoMatch ? (
          <div className={CAPTURE_PLACEMENTS_EMPTY_BODY_CLASS}>
            <div className={CAPTURE_PLACEMENTS_EMPTY_COPY_STACK_CLASS}>
              <p className={CAPTURE_EMPTY_TITLE_CLASS}>{copy.noMatchTitle}</p>
              <p className={`${CAPTURE_EMPTY_HELPER_CLASS} max-w-[320px]`}>
                {copy.noMatchHelper}
              </p>
              <Button
                type="button"
                variant="op-secondary"
                onClick={() => {
                  pageModule.clearArchiveSearchAndFilters()
                }}
              >
                {copy.clearFilters}
              </Button>
            </div>
          </div>
        ) : (
          <CaptureArchiveTable
            rows={archive?.rows ?? []}
            onViewDetails={(qrCodeId) => {
              pageModule.openArchivePlacementDetail(qrCodeId)
            }}
            onRestore={(qrCodeId) => {
              pageModule.requestRestore(qrCodeId)
            }}
            onDuplicateAsNew={(qrCodeId) => {
              pageModule.requestDuplicateAsNew(qrCodeId)
            }}
          />
        )}
      </div>

      <Dialog open={filtersOpen} onOpenChange={setFiltersOpen}>
        <DialogContent className="bg-op-surface-secondary sm:max-w-[480px]">
          <DialogHeader>
            <DialogTitle>{copy.filtersLabel}</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-5">
            {showLocationFilter ? (
              <fieldset className="flex flex-col gap-2">
                <legend className="text-sm font-medium text-op-text-primary">
                  Location
                </legend>
                {locations.map((location) => {
                  const checked = filterDraft.locationIds.includes(location.id)
                  return (
                    <CheckboxLabel
                      key={location.id}
                      checked={checked}
                      onCheckedChange={(next) => {
                        setFilterDraft((prev) => ({
                          ...prev,
                          locationIds: next
                            ? [...prev.locationIds, location.id]
                            : prev.locationIds.filter(
                                (id) => id !== location.id
                              ),
                        }))
                      }}
                      className="text-sm text-op-text-muted"
                    >
                      {location.locationName}
                    </CheckboxLabel>
                  )
                })}
              </fieldset>
            ) : null}

            <fieldset className="flex flex-col gap-2">
              <legend className="text-sm font-medium text-op-text-primary">
                Placement type
              </legend>
              {CAPTURE_ARCHIVE_PLACEMENT_TYPE_OPTIONS.map((option) => {
                const checked = filterDraft.placementTypes.includes(option.id)
                return (
                  <CheckboxLabel
                    key={option.id}
                    checked={checked}
                    onCheckedChange={(next) => {
                      setFilterDraft((prev) => ({
                        ...prev,
                        placementTypes: next
                          ? [...prev.placementTypes, option.id]
                          : prev.placementTypes.filter(
                              (id) => id !== option.id
                            ),
                      }))
                    }}
                    className="text-sm text-op-text-muted"
                  >
                    {option.label}
                  </CheckboxLabel>
                )
              })}
            </fieldset>

            <div className="flex flex-col gap-2">
              <p className="text-sm font-medium text-op-text-primary">Archived date</p>
              <Select
                value={filterDraft.archivedDate.preset}
                onValueChange={(value) => {
                  setFilterDraft((prev) => ({
                    ...prev,
                    archivedDate: {
                      preset: value as CaptureArchiveDatePresetId,
                    },
                  }))
                }}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="any-time">Any time</SelectItem>
                  <SelectItem value="last-7">Last 7 days</SelectItem>
                  <SelectItem value="last-30">Last 30 days</SelectItem>
                  <SelectItem value="this-month">This month</SelectItem>
                  <SelectItem value="custom">Custom</SelectItem>
                </SelectContent>
              </Select>
              {filterDraft.archivedDate.preset === "custom" ? (
                <div className="flex gap-2">
                  <Input
                    type="date"
                    value={filterDraft.archivedDate.dateFrom ?? ""}
                    onChange={(event) => {
                      setFilterDraft((prev) => ({
                        ...prev,
                        archivedDate: {
                          ...prev.archivedDate,
                          preset: "custom",
                          dateFrom: event.target.value,
                        },
                      }))
                    }}
                  />
                  <Input
                    type="date"
                    value={filterDraft.archivedDate.dateTo ?? ""}
                    onChange={(event) => {
                      setFilterDraft((prev) => ({
                        ...prev,
                        archivedDate: {
                          ...prev.archivedDate,
                          preset: "custom",
                          dateTo: event.target.value,
                        },
                      }))
                    }}
                  />
                </div>
              ) : null}
            </div>

            <fieldset className="flex flex-col gap-2">
              <legend className="text-sm font-medium text-op-text-primary">
                Archived by
              </legend>
              {(archive?.archiverOptions ?? []).map((name) => {
                const checked =
                  filterDraft.archivedByDisplayNames.includes(name)
                return (
                  <CheckboxLabel
                    key={name}
                    checked={checked}
                    onCheckedChange={(next) => {
                      setFilterDraft((prev) => ({
                        ...prev,
                        archivedByDisplayNames: next
                          ? [...prev.archivedByDisplayNames, name]
                          : prev.archivedByDisplayNames.filter(
                              (item) => item !== name
                            ),
                      }))
                    }}
                    className="text-sm text-op-text-muted"
                  >
                    {name}
                  </CheckboxLabel>
                )
              })}
            </fieldset>
          </div>
          <DialogFooter>
            <Button type="button" variant="op-primary" onClick={applyFilters}>
              Apply
            </Button>
            <Button
              type="button"
              variant="op-tertiary"
              onClick={() => {
                setFiltersOpen(false)
              }}
            >
              Cancel
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <CaptureRestoreConfirmDialog
        snapshot={snapshot.restoreConfirm}
        busy={restoreBusy}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.cancelRestoreConfirm()
          }
        }}
        onConfirm={() => {
          setRestoreBusy(true)
          void pageModule.confirmRestore().then((result) => {
            setRestoreBusy(false)
            if (result !== "failed" && result !== "noop" && result !== "conflict") {
              toast.success(result.toastMessage, {
                duration: CAPTURE_PAUSE_ACTIVATE_TOAST_DURATION_MS,
              })
            }
          })
        }}
      />

      <CapturePlacementDetailDrawer
        snapshot={snapshot.placementDetailDrawer}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.closePlacementDetail()
          }
        }}
        onCopyLink={() => {
          void pageModule.copyPlacementDetailLink().then((result) => {
            if (result === "copied") {
              toast.success("Link copied")
            }
          })
        }}
        onPause={() => {
          pageModule.requestPlacementDetailPause()
        }}
        onActivate={() => {
          pageModule.requestPlacementDetailActivate()
        }}
        onRotate={() => {
          pageModule.requestPlacementDetailRotate()
        }}
        onArchive={() => {
          void pageModule.requestPlacementDetailArchive().then((result) => {
            if (result !== "failed" && result !== "noop") {
              toast.success(result.toastMessage, {
                duration: CAPTURE_PAUSE_ACTIVATE_TOAST_DURATION_MS,
              })
            }
          })
        }}
        onPreview={() => {
          pageModule.openPlacementDetailPreview()
        }}
        onDescriptionDraftChange={pageModule.setPlacementDetailDescriptionDraft}
        onSaveDescription={() => {
          pageModule.savePlacementDetailDescription()
        }}
      />

      <CapturePauseActivateConfirmDialog
        snapshot={snapshot.pauseActivateConfirm}
        busy={pauseActivateBusy}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.cancelPauseActivateConfirm()
          }
        }}
        onConfirm={() => {
          setPauseActivateBusy(true)
          void pageModule.confirmPauseActivate().then((result) => {
            setPauseActivateBusy(false)
            if (result !== "failed" && result !== "noop") {
              toast.success(result.toastMessage, {
                duration: CAPTURE_PAUSE_ACTIVATE_TOAST_DURATION_MS,
              })
            }
          })
        }}
      />

      <CaptureRotateConfirmDialog
        confirm={snapshot.rotateConfirm}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.cancelRotateConfirm()
          }
        }}
        onAcknowledgedChange={pageModule.setRotatePrintMaterialsAcknowledged}
        onConfirm={() => {
          void pageModule.confirmRotate().then((result) => {
            if (result === "rotated") {
              toast.success(OPERATOR_CAPTURE_ROTATE_CONFIRM_COPY.successToast)
            }
          })
        }}
      />

      <CaptureGuestExperiencePreviewOverlay
        open={snapshot.isGuestExperiencePreviewOpen}
        guestExperience={archivePreviewGuestExperience}
        previewPlacementLabel={snapshot.guestExperiencePreviewPlacementLabel}
        onClose={() => {
          pageModule.closeGuestExperiencePreview()
        }}
      />

      <CaptureCreateDigitalGuestLinkDialog
        open={createOpen}
        busy={createBusy}
        prefill={
          archive?.createPrefill == null
            ? undefined
            : {
                linkName: archive.createPrefill.linkName,
                channel: archive.createPrefill.channel,
                status: archive.createPrefill.status,
              }
        }
        onOpenChange={(open) => {
          setCreateOpen(open)
          if (!open) {
            pageModule.clearCreatePrefill()
          }
        }}
        onSubmit={async (input) => {
          setCreateBusy(true)
          const locationId = archive?.createPrefill?.locationId
          const result = await pageModule.createDigitalGuestLink({
            ...input,
            locationId,
          })
          setCreateBusy(false)
          if (result === "created") {
            toast.success("Digital guest link created")
            setCreateOpen(false)
            pageModule.clearCreatePrefill()
            void pageModule.reloadArchive()
          }
          return result
        }}
      />
    </div>
  )
}

// Keep type import used for filter draft placement types.
export type { CapturePlacementQrType }

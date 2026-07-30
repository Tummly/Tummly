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
import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import { useCapturePageModuleApi } from "@/components/dashboard/operator/Capture/utils/capturePageModuleContext"
import { Button } from "@/components/ui/button"
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
  CAPTURE_ARCHIVE_SORT_OPTIONS,
  DEFAULT_CAPTURE_ARCHIVE_FILTERS,
  type CaptureArchiveSortId,
} from "@/lib/operatorCapture/buildCaptureArchive"
import {
  archiveFiltersFromSelection,
  selectionFromArchiveFilters,
} from "@/lib/operatorCapture/captureArchiveFilterSelection"
import { captureArchiveFilterSheetSchema } from "@/lib/operatorCapture/captureArchiveFilterSheetSchema"
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
import {
  openSession,
  type FilterSheetSession,
} from "@/lib/operatorFilterSheet"
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
  const [filtersSession, setFiltersSession] =
    useState<FilterSheetSession | null>(null)
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

  const archiveFilterCatalog = {
    showLocationFilter,
    locations: (archive?.locationOptions
      ?? locations.map((location) => ({
        id: location.id,
        label: location.locationName,
      }))).map((location) => ({
      id: location.id,
      label: location.label,
    })),
    archivers: archive?.archiverOptions ?? [],
  }

  const archiveFilterSchema = captureArchiveFilterSheetSchema({
    showLocationFilter,
    locations: archiveFilterCatalog.locations.map((location) => ({
      id: String(location.id),
      label: location.label,
    })),
    archivers: archiveFilterCatalog.archivers.map((name) => ({
      id: name,
      label: name,
    })),
  })

  const openFilters = () => {
    setFiltersSession(
      openSession(
        selectionFromArchiveFilters(
          archive?.filters ?? DEFAULT_CAPTURE_ARCHIVE_FILTERS,
          archiveFilterCatalog
        )
      )
    )
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

      <OperatorFilterSheetDialog
        open={filtersSession != null}
        title={copy.filtersLabel}
        schema={archiveFilterSchema}
        session={filtersSession}
        chipResolvers={{
          location: (id) =>
            archiveFilterCatalog.locations.find(
              (location) => String(location.id) === id
            )?.label ?? id,
          archivedBy: (id) => id,
        }}
        onSessionChange={setFiltersSession}
        onOpenChange={(open) => {
          if (!open) {
            setFiltersSession(null)
          }
        }}
        onApply={(selection) => {
          pageModule.setArchiveFilters(
            archiveFiltersFromSelection(selection, { showLocationFilter })
          )
          setFiltersSession(openSession(selection))
        }}
      />
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

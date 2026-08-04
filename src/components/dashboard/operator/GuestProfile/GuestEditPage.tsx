import { useEffect, useState } from "react"
import { Link, useLocation, useNavigate } from "react-router-dom"
import { ChevronDownIcon, ChevronRightIcon } from "lucide-react"
import { toast } from "sonner"

import { GuestProfileEmptyCopy } from "@/components/dashboard/operator/GuestProfile/GuestProfileEmptyCopy"
import { GuestProfileLatestFeedbackSection } from "@/components/dashboard/operator/GuestProfile/GuestProfileLatestFeedbackSection"
import { useGuestProfileEditCommands } from "@/components/dashboard/operator/GuestProfile/utils/useGuestProfileEditCommands"
import { GuestsRemovableChip } from "@/components/dashboard/operator/Guests/GuestsRemovableChip"
import { FeedbackDetailsDrawer } from "@/components/dashboard/operator/Feedback/FeedbackDetailsDrawer"
import { RecoveryWizardsHost } from "@/components/dashboard/operator/Feedback/RecoveryWizardsHost"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import { Label } from "@/components/ui/label"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"
import { Separator } from "@/components/ui/separator"
import { Textarea } from "@/components/ui/textarea"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  GUEST_EDIT_PAGE,
  GUEST_PROFILE_BACK_TO_GUESTS_LABEL,
  GUEST_PROFILE_NOTE_COMPOSE,
  GUEST_PROFILE_UNAVAILABLE_HELPER,
  GUEST_PROFILE_UNAVAILABLE_TITLE,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import { filterCatalogForSearch } from "@/lib/operatorGuests/addTagDialogLogic"
import { cn } from "@/lib/utils"
import {
  operatorDashboardGuestProfilePath,
  operatorDashboardNavPath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"
import {
  GUESTS_MARKETING_STATUS_BADGE_CLASS,
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_GUEST_NAME_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
  GUESTS_TABLE_LOCATION_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type {
  GuestIdentityDraft,
  GuestIdentityFieldErrors,
} from "@/lib/operatorGuestProfile/guestIdentityForm"
import type { OperatorGuestProfileViewModel } from "@/types/operatorGuestProfile"

type GuestEditPageProps = {
  mode: OperatorDashboardMode
  selectedLocationId: number
}

const SECTION_HELPER_CLASS =
  "text-sm leading-5 tracking-[-0.2px] text-muted-foreground"

function scrollToHashTarget(hash: string) {
  const id = hash.replace(/^#/, "")
  if (id !== "tags" && id !== "data-privacy") {
    return
  }
  const el = document.getElementById(id)
  el?.scrollIntoView({ behavior: "smooth", block: "start" })
}

function GuestInformationSection({
  draft,
  fieldErrors,
  viewModel,
  onChange,
}: {
  draft: GuestIdentityDraft
  fieldErrors: GuestIdentityFieldErrors
  viewModel: OperatorGuestProfileViewModel
  onChange: <K extends keyof GuestIdentityDraft>(
    field: K,
    value: GuestIdentityDraft[K]
  ) => void
}) {
  const copy = GUEST_EDIT_PAGE.guestInformation
  const placeholders = GUEST_EDIT_PAGE.provenancePlaceholders

  return (
    <section className={GUESTS_SECTION_CLASS} aria-label={copy.sectionTitle}>
      <div className="flex flex-col gap-2">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.sectionTitle}</h2>
        <p className={SECTION_HELPER_CLASS}>{copy.helper}</p>
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        <FloatingLabelInput
          label="First name"
          value={draft.firstName}
          error={fieldErrors.firstName}
          onChange={(event) => onChange("firstName", event.target.value)}
        />
        <FloatingLabelInput
          label="Last name"
          value={draft.lastName}
          error={fieldErrors.lastName}
          onChange={(event) => onChange("lastName", event.target.value)}
        />
        <FloatingLabelInput
          label="Email address"
          type="email"
          value={draft.email}
          error={fieldErrors.email}
          onChange={(event) => onChange("email", event.target.value)}
        />
        <FloatingLabelInput
          label="Phone number"
          type="tel"
          value={draft.phone}
          error={fieldErrors.phone}
          onChange={(event) => onChange("phone", event.target.value)}
        />
        <FloatingLabelInput
          label="Contact method"
          value={placeholders.contactMethod}
          readOnly
        />
        <FloatingLabelInput
          label="Location first captured"
          value={viewModel.profileSummary.locationName}
          readOnly
        />
        <FloatingLabelInput
          label="Source QR"
          value={placeholders.sourceQr}
          readOnly
        />
        <FloatingLabelInput
          label="First captured"
          value={viewModel.profileSummary.firstCapturedDisplay}
          readOnly
        />
        <FloatingLabelInput
          label="Last interaction"
          value={viewModel.profileSummary.lastInteractionDisplay}
          readOnly
          className="sm:col-span-1"
        />
      </div>
    </section>
  )
}

function ConsentSection({
  viewModel,
}: {
  viewModel: OperatorGuestProfileViewModel
}) {
  const copy = GUEST_EDIT_PAGE.consent

  return (
    <section className={GUESTS_SECTION_CLASS} aria-label={copy.sectionTitle}>
      <div className="flex flex-col gap-2">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.sectionTitle}</h2>
        <p className={SECTION_HELPER_CLASS}>{copy.helper}</p>
      </div>
      <div className={GUESTS_TABLE_FRAME_CLASS}>
        <Table className={GUESTS_TABLE_CLASS}>
          <TableHeader className="[&_tr]:border-0">
            <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                Permission
              </TableHead>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                Status
              </TableHead>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                Source
              </TableHead>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                Date
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {viewModel.contactEligibility.map((row) => (
              <TableRow key={row.channel} className={GUESTS_TABLE_BODY_ROW_CLASS}>
                <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                  <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                    {row.channel === "email"
                      ? "Email marketing"
                      : "SMS marketing"}
                  </span>
                </TableCell>
                <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                  <Badge
                    variant="soft"
                    className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
                  >
                    {copy.statusLabels[row.status]}
                  </Badge>
                </TableCell>
                <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                  <span className={GUESTS_TABLE_LOCATION_CLASS}>
                    {copy.sourcePlaceholder}
                  </span>
                </TableCell>
                <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                  <span className={GUESTS_TABLE_LOCATION_CLASS}>
                    {copy.datePlaceholder}
                  </span>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </section>
  )
}

function TagsSection({
  tagCatalog,
  pendingTagIds,
  tagsDirty,
  tagsApplyStatus,
  tagsApplyError,
  onStageTag,
  onUnstageTag,
  onCancel,
  onApply,
}: {
  tagCatalog: readonly { id: string; name: string; guestCount: number }[]
  pendingTagIds: readonly string[]
  tagsDirty: boolean
  tagsApplyStatus: "idle" | "applying" | "error"
  tagsApplyError: string | null
  onStageTag: (tagId: string) => void
  onUnstageTag: (tagId: string) => void
  onCancel: () => void
  onApply: () => void
}) {
  const copy = GUEST_EDIT_PAGE.tags
  const [listOpen, setListOpen] = useState(false)
  const applying = tagsApplyStatus === "applying"

  const pendingTags = pendingTagIds
    .map((id) => tagCatalog.find((tag) => tag.id === id))
    .filter((tag): tag is NonNullable<typeof tag> => tag != null)

  const availableTags = filterCatalogForSearch(tagCatalog, "", pendingTagIds)

  return (
    <section
      id="tags"
      className={GUESTS_SECTION_CLASS}
      aria-label={copy.sectionTitle}
    >
      <div className="flex flex-col gap-2">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.sectionTitle}</h2>
        <p className={SECTION_HELPER_CLASS}>{copy.helper}</p>
      </div>

      <div className="flex flex-col gap-4">
        <div className="flex flex-col gap-2">
          <label
            htmlFor="guest-edit-tag-select"
            className="text-sm font-semibold leading-5"
          >
            {copy.tagsLabel}
          </label>
          <Popover open={listOpen} onOpenChange={setListOpen}>
            <PopoverTrigger asChild>
              <Button
                type="button"
                id="guest-edit-tag-select"
                variant="outline"
                className={cn(
                  "h-[50px] w-full max-w-md justify-between rounded border border-input bg-transparent px-[15px] text-left text-sm font-normal text-[#7d7d7d] shadow-none hover:bg-transparent"
                )}
                aria-expanded={listOpen}
                aria-haspopup="listbox"
                disabled={applying}
              >
                <span className="truncate">{copy.selectPlaceholder}</span>
                <ChevronDownIcon className="size-4 shrink-0 opacity-60" />
              </Button>
            </PopoverTrigger>
            <PopoverContent
              align="start"
              className="z-[130] w-[var(--radix-popover-trigger-width)] gap-0 rounded-lg p-1"
              onOpenAutoFocus={(event) => event.preventDefault()}
            >
              <ul
                role="listbox"
                className="flex max-h-56 flex-col gap-0.5 overflow-y-auto py-1"
                aria-label="Guest tags"
              >
                {availableTags.map((tag) => (
                  <li key={tag.id}>
                    <Button
                      type="button"
                      variant="ghost"
                      role="option"
                      className="h-auto w-full justify-start rounded-md px-2.5 py-1.5 text-left text-sm font-normal text-foreground"
                      onClick={() => {
                        onStageTag(tag.id)
                        setListOpen(false)
                      }}
                    >
                      {tag.name}
                    </Button>
                  </li>
                ))}
                {availableTags.length === 0 ? (
                  <li className="rounded-md px-2.5 py-1.5 text-sm text-muted-foreground">
                    No tags available
                  </li>
                ) : null}
              </ul>
            </PopoverContent>
          </Popover>
        </div>

        {pendingTags.length > 0 ? (
          <div className="flex flex-wrap gap-3">
            {pendingTags.map((tag) => (
              <GuestsRemovableChip
                key={tag.id}
                label={tag.name}
                removeLabel={`Remove ${tag.name}`}
                onRemove={() => {
                  if (applying) {
                    return
                  }
                  onUnstageTag(tag.id)
                }}
              />
            ))}
          </div>
        ) : null}

        <div className="flex flex-wrap items-center gap-2">
          <Button variant="op-primary"
            type="button"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
            disabled={!tagsDirty || applying}
            onClick={onApply}
          >
            {copy.applyLabel}
          </Button>
          <Button variant="op-secondary"
            type="button"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            disabled={!tagsDirty || applying}
            onClick={onCancel}
          >
            {copy.cancelLabel}
          </Button>
        </div>

        {tagsApplyError != null ? (
          <p className="text-sm text-destructive" role="alert">
            {tagsApplyError}
          </p>
        ) : null}
      </div>

      <Separator />

      <div className="flex flex-col gap-2">
        <h3 className="text-sm font-semibold leading-5">
          {copy.smartGroupsLabel}
        </h3>
        <p className={GUESTS_TABLE_LOCATION_CLASS}>
          {copy.smartGroupsEmptyHelper}
        </p>
      </div>
    </section>
  )
}

function InternalNotesSection({
  noteDraft,
  noteSaveStatus,
  noteSaveError,
  onChange,
  onCancel,
  onSave,
}: {
  noteDraft: string
  noteSaveStatus: "idle" | "saving" | "error"
  noteSaveError: string | null
  onChange: (value: string) => void
  onCancel: () => void
  onSave: () => Promise<boolean>
}) {
  const copy = GUEST_EDIT_PAGE.internalNotes
  const busy = noteSaveStatus === "saving"
  const trimmed = noteDraft.trim()
  const canSave =
    trimmed.length > 0 &&
    trimmed.length <= GUEST_PROFILE_NOTE_COMPOSE.maxLength &&
    !busy

  return (
    <section className={GUESTS_SECTION_CLASS} aria-label={copy.sectionTitle}>
      <div className="flex flex-col gap-2">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.sectionTitle}</h2>
        <p className={SECTION_HELPER_CLASS}>{copy.helper}</p>
      </div>
      <div className="flex flex-col gap-4">
        <div className="flex flex-col gap-2">
          <Label htmlFor="guest-edit-add-note">
            {GUEST_PROFILE_NOTE_COMPOSE.fieldLabel}
          </Label>
          <Textarea
            id="guest-edit-add-note"
            value={noteDraft}
            onChange={(event) => {
              onChange(event.target.value)
            }}
            placeholder={GUEST_PROFILE_NOTE_COMPOSE.placeholder}
            maxLength={GUEST_PROFILE_NOTE_COMPOSE.maxLength}
            disabled={busy}
            rows={5}
          />
        </div>
        {noteSaveError != null ? (
          <p className="text-sm text-destructive" role="alert">
            {noteSaveError}
          </p>
        ) : null}
        <div className="flex flex-wrap items-center gap-3">
          <Button variant="op-primary"
            type="button"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
            disabled={!canSave}
            onClick={() => {
              void onSave()
            }}
          >
            {GUEST_PROFILE_NOTE_COMPOSE.saveLabel}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className="rounded-[2px]"
            disabled={busy || noteDraft.length === 0}
            onClick={onCancel}
          >
            {GUEST_PROFILE_NOTE_COMPOSE.cancelLabel}
          </Button>
        </div>
      </div>
    </section>
  )
}

function DataPrivacySection({
  guestId,
  exportBusy,
  deleteBusy,
  onExport,
  onRequestDeletion,
}: {
  guestId: string
  exportBusy: boolean
  deleteBusy: boolean
  onExport: () => void
  onRequestDeletion: () => void
}) {
  const copy = GUEST_EDIT_PAGE.dataPrivacy

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(guestId)
    } catch {
      // Ignore clipboard failures — chrome still shows the ID.
    }
  }

  return (
    <section
      id="data-privacy"
      className={GUESTS_SECTION_CLASS}
      aria-label={copy.sectionTitle}
    >
      <div className="flex flex-col gap-2">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.sectionTitle}</h2>
        <p className={SECTION_HELPER_CLASS}>{copy.helper}</p>
      </div>
      <div className={GUESTS_TABLE_FRAME_CLASS}>
        <Table className={GUESTS_TABLE_CLASS}>
          <TableHeader className="[&_tr]:border-0">
            <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                Item
              </TableHead>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                Content
              </TableHead>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                Action
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                  {copy.guestIdLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                {guestId}
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <Button variant="op-secondary"
                  type="button"
                  size="sm"
                  className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                  onClick={() => {
                    void handleCopy()
                  }}
                >
                  {copy.copyLabel}
                </Button>
              </TableCell>
            </TableRow>
            <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                  {copy.dataOwnerLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                {copy.dataOwnerPlaceholder}
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS} />
            </TableRow>
            <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                  {copy.exportLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                {copy.exportContent}
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <Button variant="op-secondary"
                  type="button"
                  size="sm"
                  disabled={exportBusy || deleteBusy}
                  className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                  onClick={onExport}
                >
                  {copy.exportAction}
                </Button>
              </TableCell>
            </TableRow>
            <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                  {copy.deleteLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                {copy.deleteContent}
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <Button variant="op-secondary"
                  type="button"
                  size="sm"
                  disabled={exportBusy || deleteBusy}
                  className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                  onClick={onRequestDeletion}
                >
                  {copy.deleteAction}
                </Button>
              </TableCell>
            </TableRow>
            <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                  {copy.auditLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                {copy.auditContent}
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <Button variant="op-secondary"
                  type="button"
                  size="sm"
                  disabled
                  aria-disabled
                  aria-label="View audit log (unavailable)"
                  title="View audit log is unavailable"
                  className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
                >
                  {copy.auditAction}
                </Button>
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </div>
    </section>
  )
}

export function GuestEditPage({
  mode,
  selectedLocationId,
}: GuestEditPageProps) {
  const {
    snapshot,
    retryLoad,
    setDraftField,
    saveChanges,
    stageTag,
    unstageTag,
    cancelTagDraft,
    applyTags,
    setNoteDraft,
    cancelNoteDraft,
    saveNote,
    openFeedbackDetails,
    closeFeedbackDetails,
    retryFeedbackDetails,
    startClassificationCorrection,
    setClassificationDraftSentiment,
    setClassificationDraftReason,
    setClassificationDraftNote,
    cancelClassificationCorrection,
    saveClassificationCorrection,
    setFeedbackWorkflowStatus,
    reopenFeedback,
    startFeedbackMarkNoActionNeeded,
    startFeedbackMarkResolved,
    setFeedbackCloseOutReason,
    setFeedbackCloseOutNoteDraft,
    setFeedbackCloseOutAcknowledged,
    cancelFeedbackCloseOut,
    confirmFeedbackCloseOut,
    setFeedbackInternalNoteDraft,
    createFeedbackInternalNote,
    startFeedbackNoteEdit,
    setFeedbackNoteEditDraft,
    cancelFeedbackNoteEdit,
    saveFeedbackNoteEdit,
    startFeedbackNoteDelete,
    cancelFeedbackNoteDelete,
    confirmFeedbackNoteDelete,
    startRecovery,
    recoveryWizards,
    getViewAllFeedbacksNavigation,
    exportGuestRecord,
    deleteLocationGuest,
  } = useGuestProfileEditCommands()
  const navigate = useNavigate()
  const location = useLocation()
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const guestsListPath = operatorDashboardNavPath(
    mode,
    "guests",
    selectedLocationId
  )
  const profilePath =
    snapshot.viewModel == null
      ? guestsListPath
      : operatorDashboardGuestProfilePath(
          mode,
          snapshot.viewModel.id,
          selectedLocationId
        )

  const navigateToGuestProfile = (locationGuestId: number) => {
    navigate(
      operatorDashboardGuestProfilePath(
        mode,
        locationGuestId,
        selectedLocationId
      )
    )
  }

  const handleViewAllFeedbacks = () => {
    const target = getViewAllFeedbacksNavigation()
    if (target == null) {
      return
    }
    navigate(
      operatorDashboardGuestProfilePath(
        mode,
        target.guestId,
        selectedLocationId
      ),
      { state: { tab: target.tab } }
    )
  }

  useEffect(() => {
    if (snapshot.loadStatus !== "loaded") {
      return
    }
    if (location.hash.length === 0) {
      return
    }
    const frame = window.requestAnimationFrame(() => {
      scrollToHashTarget(location.hash)
    })
    return () => window.cancelAnimationFrame(frame)
  }, [snapshot.loadStatus, location.hash])

  if (snapshot.loadStatus === "unavailable") {
    return (
      <div className={`${GUESTS_PAGE_STACK_CLASS} items-start`}>
        <header className="flex flex-col gap-2">
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>
            {GUEST_PROFILE_UNAVAILABLE_TITLE}
          </h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
            {GUEST_PROFILE_UNAVAILABLE_HELPER}
          </p>
        </header>
        <Button asChild variant="outline" size="sm">
          <Link to={guestsListPath}>{GUEST_PROFILE_BACK_TO_GUESTS_LABEL}</Link>
        </Button>
      </div>
    )
  }

  if (
    snapshot.viewModel == null &&
    (snapshot.loadStatus === "idle" || snapshot.loadStatus === "loading")
  ) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner aria-label="Loading edit guest details" />
      </div>
    )
  }

  if (snapshot.viewModel == null && snapshot.loadStatus === "error") {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-3 text-center">
        <p className="text-sm text-destructive">
          Could not load guest details. Please try again.
        </p>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => {
            void retryLoad()
          }}
        >
          Retry
        </Button>
      </div>
    )
  }

  if (snapshot.viewModel == null) {
    return null
  }

  const viewModel = snapshot.viewModel
  const saving = snapshot.saveStatus === "saving"
  const exportBusy = snapshot.exportStatus === "exporting"
  const deleteBusy = snapshot.deleteStatus === "deleting"
  const privacyCopy = GUEST_EDIT_PAGE.dataPrivacy

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <nav
        aria-label="Breadcrumb"
        className="flex flex-wrap items-center gap-1 text-sm"
      >
        <Link
          to={profilePath}
          className="font-medium text-foreground hover:underline"
        >
          {viewModel.name}
        </Link>
        <ChevronRightIcon className="size-4 text-muted-foreground" aria-hidden />
        <span className="font-medium text-muted-foreground">
          {GUEST_EDIT_PAGE.breadcrumbCurrent}
        </span>
      </nav>

      <header className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <div className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{GUEST_EDIT_PAGE.title}</h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>{GUEST_EDIT_PAGE.subtitle}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button variant="op-primary"
            type="button"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
            disabled={saving || deleteBusy}
            onClick={() => {
              void (async () => {
                const result = await saveChanges()
                if (result.status === "saved") {
                  navigate(profilePath)
                }
              })()
            }}
          >
            {GUEST_EDIT_PAGE.saveLabel}
          </Button>
          <Button variant="op-secondary"
            type="button"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            disabled={saving || deleteBusy}
            onClick={() => {
              navigate(profilePath)
            }}
          >
            {GUEST_EDIT_PAGE.cancelLabel}
          </Button>
        </div>
      </header>

      {snapshot.saveError != null || snapshot.fieldErrors.form != null ? (
        <p className="text-sm text-destructive" role="alert">
          {snapshot.saveError ?? snapshot.fieldErrors.form}
        </p>
      ) : null}

      <GuestInformationSection
        draft={snapshot.draft}
        fieldErrors={snapshot.fieldErrors}
        viewModel={viewModel}
        onChange={setDraftField}
      />
      <ConsentSection viewModel={viewModel} />
      <TagsSection
        tagCatalog={snapshot.tagCatalog}
        pendingTagIds={snapshot.pendingTagIds}
        tagsDirty={snapshot.tagsDirty}
        tagsApplyStatus={snapshot.tagsApplyStatus}
        tagsApplyError={snapshot.tagsApplyError}
        onStageTag={stageTag}
        onUnstageTag={unstageTag}
        onCancel={cancelTagDraft}
        onApply={() => {
          void applyTags()
        }}
      />
      <InternalNotesSection
        noteDraft={snapshot.noteDraft}
        noteSaveStatus={snapshot.noteSaveStatus}
        noteSaveError={snapshot.noteSaveError}
        onChange={setNoteDraft}
        onCancel={cancelNoteDraft}
        onSave={saveNote}
      />
      <GuestProfileLatestFeedbackSection
        sectionTitle={GUEST_EDIT_PAGE.recentFeedback.sectionTitle}
        sectionHelper={GUEST_EDIT_PAGE.recentFeedback.helper}
        rows={viewModel.latestFeedback}
        emptyTitle={GUEST_EDIT_PAGE.recentFeedback.emptyTitle}
        emptyHelper={GUEST_EDIT_PAGE.recentFeedback.emptyHelper}
        onOpenFeedback={(feedbackId) => {
          void openFeedbackDetails(feedbackId)
        }}
        onStartRecovery={(feedbackId) => {
          void startRecovery(feedbackId)
        }}
        onViewAllFeedbacks={handleViewAllFeedbacks}
      />
      <section
        className={GUESTS_SECTION_CLASS}
        aria-label={GUEST_EDIT_PAGE.offers.sectionTitle}
      >
        <div className="flex flex-col gap-2">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>
            {GUEST_EDIT_PAGE.offers.sectionTitle}
          </h2>
          <p className={SECTION_HELPER_CLASS}>{GUEST_EDIT_PAGE.offers.helper}</p>
        </div>
        <GuestProfileEmptyCopy
          title={GUEST_EDIT_PAGE.offers.emptyTitle}
          helper={GUEST_EDIT_PAGE.offers.emptyHelper}
        />
      </section>
      <section
        className={GUESTS_SECTION_CLASS}
        aria-label={GUEST_EDIT_PAGE.campaigns.sectionTitle}
      >
        <div className="flex flex-col gap-2">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>
            {GUEST_EDIT_PAGE.campaigns.sectionTitle}
          </h2>
          <p className={SECTION_HELPER_CLASS}>
            {GUEST_EDIT_PAGE.campaigns.helper}
          </p>
        </div>
        <GuestProfileEmptyCopy
          title={GUEST_EDIT_PAGE.campaigns.emptyTitle}
          helper={GUEST_EDIT_PAGE.campaigns.emptyHelper}
        />
      </section>
      <DataPrivacySection
        guestId={viewModel.id}
        exportBusy={exportBusy}
        deleteBusy={deleteBusy}
        onExport={() => {
          void (async () => {
            const result = await exportGuestRecord()
            if (result.status === "error") {
              toast.error(result.message)
            }
          })()
        }}
        onRequestDeletion={() => {
          setDeleteDialogOpen(true)
        }}
      />

      <FeedbackDetailsDrawer
        snapshot={snapshot.feedbackDetails}
        onOpenChange={(open) => {
          if (!open) {
            closeFeedbackDetails()
          }
        }}
        onRetry={() => {
          void retryFeedbackDetails()
        }}
        onStartCorrection={startClassificationCorrection}
        onDraftSentimentChange={setClassificationDraftSentiment}
        onDraftReasonChange={setClassificationDraftReason}
        onDraftNoteChange={setClassificationDraftNote}
        onCancelCorrection={cancelClassificationCorrection}
        onSaveCorrection={() => {
          void saveClassificationCorrection()
        }}
        onReopen={() => {
          void reopenFeedback()
        }}
        onStartMarkResolved={startFeedbackMarkResolved}
        onMarkNoActionNeeded={() => {
          startFeedbackMarkNoActionNeeded()
        }}
        onCancelCloseOut={cancelFeedbackCloseOut}
        onSetCloseOutReason={setFeedbackCloseOutReason}
        onSetCloseOutNoteDraft={setFeedbackCloseOutNoteDraft}
        onSetCloseOutAcknowledged={setFeedbackCloseOutAcknowledged}
        onConfirmCloseOut={() => {
          void confirmFeedbackCloseOut()
        }}
        onViewGuestProfile={navigateToGuestProfile}
        onStartRecovery={() => {
          const feedbackId = snapshot.feedbackDetails.feedbackId
          if (feedbackId == null) {
            return
          }
          void startRecovery(feedbackId)
        }}
        onNoteDraftChange={setFeedbackInternalNoteDraft}
        onCreateNote={() => {
          void createFeedbackInternalNote()
        }}
        onStartNoteEdit={startFeedbackNoteEdit}
        onNoteEditDraftChange={setFeedbackNoteEditDraft}
        onCancelNoteEdit={cancelFeedbackNoteEdit}
        onSaveNoteEdit={() => saveFeedbackNoteEdit()}
        onStartNoteDelete={startFeedbackNoteDelete}
        onCancelNoteDelete={cancelFeedbackNoteDelete}
        onConfirmNoteDelete={() => {
          void confirmFeedbackNoteDelete()
        }}
      />

      <RecoveryWizardsHost snapshot={snapshot} wizards={recoveryWizards} />

      <AlertDialog
        open={deleteDialogOpen}
        onOpenChange={(open) => {
          if (deleteBusy) {
            return
          }
          setDeleteDialogOpen(open)
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{privacyCopy.deleteDialogTitle}</AlertDialogTitle>
            <AlertDialogDescription>
              {privacyCopy.deleteDialogDescription}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleteBusy}>
              {privacyCopy.deleteDialogCancel}
            </AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              disabled={deleteBusy}
              onClick={(event) => {
                event.preventDefault()
                void (async () => {
                  const result = await deleteLocationGuest()
                  if (result.status === "deleted") {
                    setDeleteDialogOpen(false)
                    navigate(guestsListPath)
                    return
                  }
                  toast.error(result.message)
                })()
              }}
            >
              {privacyCopy.deleteDialogConfirm}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

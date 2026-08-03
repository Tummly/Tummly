import type { ReactNode } from "react"
import {
  ChevronRightIcon,
  EllipsisVerticalIcon,
  SquarePenIcon,
  XIcon,
} from "lucide-react"
import { toast } from "sonner"

import { GuestProfileAddNoteDialog } from "@/components/dashboard/operator/GuestProfile/GuestProfileAddNoteDialog"
import { OperatorNoteDeleteDialog } from "@/components/dashboard/operator/OperatorNoteDeleteDialog"
import { FeedbackCloseOutDialog } from "@/components/dashboard/operator/Feedback/FeedbackCloseOutDialog"
import { AiAssistantIcon } from "@/components/ui/ai-assistant-icon"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Textarea } from "@/components/ui/textarea"
import type {
  FeedbackClassificationCorrectionEditor,
  FeedbackDetailsLoaded,
  FeedbackDetailsSnapshot,
} from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import {
  FEEDBACK_INTERNAL_NOTE_MAX_LENGTH,
  feedbackWorkflowStatusLabel,
} from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import { formatGuestProfileAbsoluteDateTime } from "@/lib/operatorGuestProfile/mapGuestProfileApiResponseToViewModel"
import { feedbackSentimentLabel } from "@/lib/operatorHome/feedbackSentimentLabel"
import { feedbackClosedOutActivityLabel } from "@/lib/operatorFeedback/feedbackCloseOutPresentation"
import {
  FEEDBACK_INTERNAL_NOTE_EDIT,
  OPERATOR_NOTE_ACTIONS,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  OPERATOR_DRAWER_ACTION_ROW_CLASS,
  OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
  OPERATOR_RIGHT_DRAWER_CONTENT_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import type {
  FeedbackSentiment,
} from "@/types/dashboard"
import { cn } from "@/lib/utils"

/** Section chrome — Figma cards border `#262626` in dark mode. */
const FEEDBACK_DRAWER_SECTION_CLASS =
  "flex flex-col gap-4 border-t border-[#dedede] p-[22px] dark:border-[#262626]"

type FeedbackDetailsDrawerProps = {
  snapshot: FeedbackDetailsSnapshot
  onOpenChange: (open: boolean) => void
  onRetry: () => void
  onStartCorrection?: () => void
  onDraftSentimentChange?: (sentiment: FeedbackSentiment) => void
  onCancelCorrection?: () => void
  onSaveCorrection?: () => void
  onReopen?: () => void
  onStartMarkResolved?: () => void
  onMarkNoActionNeeded?: () => void
  onCancelCloseOut?: () => void
  onSetCloseOutReason?: (
    reason: import("@/lib/operatorFeedback/feedbackCloseOutPresentation").FeedbackCloseOutReason
  ) => void
  onSetCloseOutNoteDraft?: (value: string) => void
  onConfirmCloseOut?: () => void
  onViewGuestProfile?: (locationGuestId: number) => void
  onNoteDraftChange?: (value: string) => void
  onCreateNote?: () => void
  onStartNoteEdit?: (noteId: number) => void
  onNoteEditDraftChange?: (value: string) => void
  onCancelNoteEdit?: () => void
  onSaveNoteEdit?: () => void
  onStartNoteDelete?: (noteId: number) => void
  onCancelNoteDelete?: () => void
  onConfirmNoteDelete?: () => void
  canGoPrevious?: boolean
  canGoNext?: boolean
  onPrevious?: () => void
  onNext?: () => void
}

const SENTIMENT_OPTIONS: Array<{
  value: FeedbackSentiment
  label: string
}> = [
  { value: "positive", label: "Positive" },
  { value: "neutral", label: "Neutral" },
  { value: "negative", label: "Negative" },
]

function formatSubmittedAbsolute(iso: string): string {
  return formatGuestProfileAbsoluteDateTime(iso)
}

function formatActivityTime(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return ""
  }

  return date
    .toLocaleTimeString("en-GB", {
      hour: "numeric",
      minute: "2-digit",
      hour12: true,
      timeZone: "Europe/London",
    })
    .replace(/\b(am|pm)\b/i, (match) => match.toUpperCase())
}

function activityLabel(
  event: FeedbackDetailsLoaded["activityHistory"][number]
): string {
  switch (event.kind) {
    case "note_added":
      return "Note added"
    case "note_deleted":
      return "Note deleted"
    case "classification_corrected": {
      const from = event.fromSentiment
        ? feedbackSentimentLabel(event.fromSentiment)
        : null
      const to = event.toSentiment
        ? feedbackSentimentLabel(event.toSentiment)
        : null
      if (from != null && to != null) {
        return `Changed AI classification from ${from} to ${to}`
      }
      return "Changed AI classification"
    }
    case "workflow_status_changed": {
      const from =
        event.fromWorkflowStatus != null
          ? feedbackWorkflowStatusLabel(event.fromWorkflowStatus)
          : null
      const to =
        event.toWorkflowStatus != null
          ? feedbackWorkflowStatusLabel(event.toWorkflowStatus)
          : null
      if (from != null && to != null) {
        return `Changed follow-up status from ${from} to ${to}`
      }
      return "Changed follow-up status"
    }
    case "feedback_closed_out":
      return feedbackClosedOutActivityLabel({
        intent: event.closeOutIntent,
        reason: event.closeOutReason,
        fromWorkflowStatus: event.fromWorkflowStatus,
        toWorkflowStatus: event.toWorkflowStatus,
      })
    case "feedback_received":
    default:
      return "Feedback received"
  }
}

function Section({
  title,
  children,
  className,
}: {
  title: string
  children: ReactNode
  className?: string
}) {
  return (
    <section className={cn(FEEDBACK_DRAWER_SECTION_CLASS, className)}>
      <h3 className="text-lg font-bold text-foreground">{title}</h3>
      {children}
    </section>
  )
}

function PendingEmpty({ children }: { children: ReactNode }) {
  return (
    <p className="text-sm font-medium text-[var(--op-color-gray-550)]">
      {children}
    </p>
  )
}

async function copyFeedbackReference(reference: string): Promise<void> {
  try {
    await navigator.clipboard.writeText(reference)
    toast.success("Feedback reference copied")
  } catch {
    toast.error("Could not copy Feedback reference. Please try again.")
  }
}

function DetailField({
  label,
  value,
  action,
}: {
  label: string
  value: string
  action?: ReactNode
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <p className="text-base font-medium text-foreground">{label}</p>
      <div className="flex flex-wrap items-center gap-2">
        <p className="text-sm font-medium text-[var(--op-color-gray-550)]">{value}</p>
        {action}
      </div>
    </div>
  )
}

function SentimentBadge({
  sentiment,
}: {
  sentiment: "positive" | "neutral" | "negative"
}) {
  const label =
    sentiment === "positive"
      ? "Positive"
      : sentiment === "neutral"
        ? "Neutral"
        : "Negative"

  return <Badge variant={sentiment}>{label}</Badge>
}

function ClassificationSection({
  details,
  correction,
  onStartCorrection,
  onDraftSentimentChange,
  onCancelCorrection,
  onSaveCorrection,
}: {
  details: FeedbackDetailsLoaded
  correction: FeedbackClassificationCorrectionEditor
  onStartCorrection?: () => void
  onDraftSentimentChange?: (sentiment: FeedbackSentiment) => void
  onCancelCorrection?: () => void
  onSaveCorrection?: () => void
}) {
  const status = details.classificationStatus
  const saving = correction.saveStatus === "saving"

  return (
    <section className={FEEDBACK_DRAWER_SECTION_CLASS}>
      <div className="flex items-center gap-3">
        <AiAssistantIcon size={26} />
        <h3 className="text-lg font-bold text-foreground">
          AI classification
        </h3>
      </div>
      {status === "Pending" ? (
        <PendingEmpty>Classification is not available yet.</PendingEmpty>
      ) : null}
      {status === "Failed" ? (
        <PendingEmpty>Classification unavailable.</PendingEmpty>
      ) : null}
      {status === "Succeeded" && details.sentiment != null ? (
        <div className="flex flex-wrap gap-2">
          <SentimentBadge sentiment={details.sentiment} />
        </div>
      ) : null}
      {correction.isEditing ? (
        <div className="flex w-full flex-col gap-3">
          <FloatingLabelSelect
            label="Change classification"
            options={SENTIMENT_OPTIONS}
            value={correction.draftSentiment ?? undefined}
            onValueChange={(value) => {
              onDraftSentimentChange?.(value as FeedbackSentiment)
            }}
            disabled={saving}
            disableFocusRing
            contentClassName={cn(
              "z-[120] p-1",
              OPERATOR_SHELL_MENU_PANEL_CLASS
            )}
            itemClassName={cn(
              "rounded-md px-2.5 py-1.5 text-sm font-normal text-foreground",
              "mb-0.5 last:mb-0",
              "focus:bg-accent data-[state=checked]:bg-accent data-[state=checked]:font-medium"
            )}
          />
          {correction.saveError != null ? (
            <p className="text-sm text-destructive" role="alert">
              {correction.saveError}
            </p>
          ) : null}
          <div className={OPERATOR_DRAWER_ACTION_ROW_CLASS}>
            <Button
              type="button"
              variant="default"
              disabled={!correction.canSave || saving}
              aria-disabled={!correction.canSave || saving}
              onClick={() => {
                onSaveCorrection?.()
              }}
              className={cn(
                OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
                "h-auto w-fit rounded-[2px] px-4 py-2.5 text-sm font-medium leading-5"
              )}
            >
              {saving ? "Saving…" : "Save classification"}
            </Button>
            <Button
              type="button"
              variant="op-tertiary"
              disabled={saving}
              onClick={() => {
                onCancelCorrection?.()
              }}
              className={cn(
                OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
                "w-fit rounded-[2px]"
              )}
            >
              Cancel
            </Button>
          </div>
        </div>
      ) : (
        <Button
          type="button"
          variant="link"
          size="link-sm"
          disabled={!details.canCorrectClassification}
          aria-disabled={!details.canCorrectClassification}
          onClick={() => {
            onStartCorrection?.()
          }}
          className="w-fit gap-1.5 font-medium disabled:opacity-40"
        >
          <SquarePenIcon className="size-5" aria-hidden />
          Correct classification
        </Button>
      )}
    </section>
  )
}

function IssueTagsSection({ details }: { details: FeedbackDetailsLoaded }) {
  const status = details.classificationStatus

  return (
    <Section title="Issue tags" className="gap-3">
      {status === "Pending" ? (
        <PendingEmpty>
          Issue tags will appear when classification is available.
        </PendingEmpty>
      ) : null}
      {status === "Failed" ? (
        <PendingEmpty>Issue tags unavailable.</PendingEmpty>
      ) : null}
      {status === "Succeeded" && details.detectedTags != null ? (
        details.detectedTags.length === 0 ? (
          <PendingEmpty>No issues detected.</PendingEmpty>
        ) : (
          <ul className="flex flex-wrap gap-3">
            {details.detectedTags.map((tag) => (
              <li key={tag.key}>
                <Badge variant="tag">{tag.label}</Badge>
              </li>
            ))}
          </ul>
        )
      ) : null}
      <Button
        type="button"
        variant="link"
        size="link-sm"
        disabled
        aria-disabled
        className="w-fit gap-1.5 font-medium disabled:opacity-40"
        aria-label="Edit tags (unavailable)"
        title="Edit tags is unavailable"
      >
        <SquarePenIcon className="size-5" aria-hidden />
        Edit tags
      </Button>
    </Section>
  )
}

function FeedbackDetailsDrawerHeader({
  venueLine,
  submittedAbsolute,
  isNew,
  needsAttention,
  canReopen,
  canMarkNoActionNeeded,
  canViewGuestProfile,
  feedbackReference,
  locationGuestId,
  onReopen,
  onStartMarkResolved,
  onMarkNoActionNeeded,
  onViewGuestProfile,
  description,
  canGoPrevious,
  canGoNext,
  onPrevious,
  onNext,
}: {
  venueLine?: string
  submittedAbsolute?: string
  isNew?: boolean
  needsAttention?: boolean
  canReopen?: boolean
  canMarkNoActionNeeded?: boolean
  canViewGuestProfile?: boolean
  feedbackReference?: string
  locationGuestId?: number | null
  onReopen?: () => void
  onStartMarkResolved?: () => void
  onMarkNoActionNeeded?: () => void
  onViewGuestProfile?: (locationGuestId: number) => void
  description?: string
  canGoPrevious?: boolean
  canGoNext?: boolean
  onPrevious?: () => void
  onNext?: () => void
}) {
  const showBadges = isNew || needsAttention
  const showListNavigation =
    canGoPrevious != null
    || canGoNext != null
    || onPrevious != null
    || onNext != null

  return (
    <div className="flex shrink-0 items-start justify-between gap-[22px] px-[22px] pb-[22px] pt-8">
      <div className="flex min-w-0 flex-1 flex-col gap-3">
        <div className="flex flex-col gap-1.5">
          <DrawerTitle className="text-2xl font-bold text-foreground">
            Feedback details
          </DrawerTitle>
          {description != null ? (
            <DrawerDescription className="sr-only">
              {description}
            </DrawerDescription>
          ) : venueLine != null ? (
            <DrawerDescription className="text-sm font-medium text-foreground">
              {venueLine}
            </DrawerDescription>
          ) : null}
          {submittedAbsolute ? (
            <p className="text-xs font-medium text-[var(--op-color-gray-550)]">
              Submitted {submittedAbsolute}
            </p>
          ) : null}
        </div>
        {showBadges ? (
          <div className="flex flex-wrap gap-3">
            {isNew ? <Badge variant="soft">New</Badge> : null}
            {needsAttention ? (
              <Badge variant="soft">Needs attention</Badge>
            ) : null}
          </div>
        ) : null}
        {showListNavigation ? (
          <div className="flex items-center gap-3">
            <Button
              type="button"
              variant="op-secondary"
              size="op"
              disabled={!canGoPrevious}
              aria-disabled={!canGoPrevious}
              onClick={() => {
                onPrevious?.()
              }}
            >
              Previous feedback
            </Button>
            <Button
              type="button"
              variant="op-secondary"
              size="op"
              disabled={!canGoNext}
              aria-disabled={!canGoNext}
              onClick={() => {
                onNext?.()
              }}
            >
              Next feedback
            </Button>
            <DropdownMenu modal={false}>
              <DropdownMenuTrigger asChild>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="size-[42px] shrink-0 rounded-[2px]"
                  aria-label="Feedback details actions"
                >
                  <EllipsisVerticalIcon className="size-[18px]" aria-hidden />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent
                align="end"
                className={cn("z-[120] min-w-48", OPERATOR_SHELL_MENU_PANEL_CLASS)}
              >
                {feedbackReference != null ? (
                  <DropdownMenuItem
                    className="rounded-md px-2.5 py-1.5 text-sm"
                    onClick={() => {
                      void copyFeedbackReference(feedbackReference)
                    }}
                  >
                    Copy feedback reference
                  </DropdownMenuItem>
                ) : null}
                <DropdownMenuItem
                  className="rounded-md px-2.5 py-1.5 text-sm"
                  disabled={!canViewGuestProfile || onViewGuestProfile == null}
                  onClick={() => {
                    if (
                      !canViewGuestProfile
                      || locationGuestId == null
                      || onViewGuestProfile == null
                    ) {
                      return
                    }
                    onViewGuestProfile(locationGuestId)
                  }}
                >
                  View guest profile
                </DropdownMenuItem>
                {canReopen ? (
                  <DropdownMenuItem
                    className="rounded-md px-2.5 py-1.5 text-sm"
                    onClick={() => {
                      onReopen?.()
                    }}
                  >
                    Reopen
                  </DropdownMenuItem>
                ) : null}
                {canMarkNoActionNeeded ? (
                  <>
                    <DropdownMenuItem
                      className="rounded-md px-2.5 py-1.5 text-sm"
                      onClick={() => {
                        onStartMarkResolved?.()
                      }}
                    >
                      Mark resolved
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      className="rounded-md px-2.5 py-1.5 text-sm"
                      onClick={() => {
                        onMarkNoActionNeeded?.()
                      }}
                    >
                      Mark no action needed
                    </DropdownMenuItem>
                  </>
                ) : null}
                <DropdownMenuItem
                  className="rounded-md px-2.5 py-1.5 text-sm"
                  disabled
                >
                  Export this feedback
                </DropdownMenuItem>
                <DropdownMenuItem
                  className="rounded-md px-2.5 py-1.5 text-sm"
                  disabled
                >
                  View audit details
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        ) : null}
      </div>
      <DrawerClose asChild>
        <Button
          type="button"
          variant="op-collapse"
          size="icon"
          className="size-[42px] shrink-0"
          aria-label="Close Feedback details"
        >
          <XIcon className="size-[18px]" aria-hidden />
        </Button>
      </DrawerClose>
    </div>
  )
}

function InternalNoteRow({
  note,
  noteEditOpen,
  noteEditBusy,
  onStartNoteEdit,
  onNoteEditDraftChange,
  onCancelNoteEdit,
  onSaveNoteEdit,
  onStartNoteDelete,
}: {
  note: FeedbackDetailsLoaded["internalNotes"][number]
  noteEditOpen: boolean
  noteEditBusy: boolean
  onStartNoteEdit?: (noteId: number) => void
  onNoteEditDraftChange?: (value: string) => void
  onCancelNoteEdit?: () => void
  onSaveNoteEdit?: () => Promise<boolean> | void
  onStartNoteDelete?: (noteId: number) => void
}) {
  return (
    <>
      <li className="flex flex-col gap-2">
        <div className="flex items-start justify-between gap-4">
          <p className="min-w-0 flex-1 whitespace-pre-wrap text-sm font-medium text-foreground">
            {note.body}
          </p>
          <div className="flex shrink-0 gap-2">
            <Button
              type="button"
              variant="op-tertiary"
              size="xs"
              className="rounded-[2px]"
              onClick={() => {
                onStartNoteEdit?.(note.id)
              }}
            >
              {OPERATOR_NOTE_ACTIONS.editLabel}
            </Button>
            <Button
              type="button"
              variant="op-tertiary"
              size="xs"
              className="rounded-[2px]"
              onClick={() => {
                onStartNoteDelete?.(note.id)
              }}
            >
              {OPERATOR_NOTE_ACTIONS.deleteLabel}
            </Button>
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
          <p className="text-xs text-muted-foreground">
            {note.authorDisplayName} · {note.createdAtDisplay}
            {note.isEdited ? (
              <>
                <span className="font-semibold"> · </span>
                {OPERATOR_NOTE_ACTIONS.editedLabel}
              </>
            ) : null}
          </p>
        </div>
      </li>
      <GuestProfileAddNoteDialog
        open={noteEditOpen}
        mode="edit"
        initialBody={note.body}
        editCopy={FEEDBACK_INTERNAL_NOTE_EDIT}
        busy={noteEditBusy}
        onOpenChange={(open) => {
          if (!open) {
            onCancelNoteEdit?.()
          }
        }}
        onSave={async (body) => {
          onNoteEditDraftChange?.(body)
          const result = onSaveNoteEdit?.()
          if (result instanceof Promise) {
            return result
          }
          return false
        }}
      />
    </>
  )
}

function LoadedBody({
  details,
  correction,
  noteDraft,
  noteCreateStatus,
  noteCreateError,
  noteEdit,
  noteDelete,
  onStartCorrection,
  onDraftSentimentChange,
  onCancelCorrection,
  onSaveCorrection,
  onViewGuestProfile,
  onNoteDraftChange,
  onCreateNote,
  onStartNoteEdit,
  onNoteEditDraftChange,
  onCancelNoteEdit,
  onSaveNoteEdit,
  onStartNoteDelete,
  onCancelNoteDelete,
  onConfirmNoteDelete,
}: {
  details: FeedbackDetailsLoaded
  correction: FeedbackClassificationCorrectionEditor
  noteDraft: string
  noteCreateStatus: FeedbackDetailsSnapshot["noteCreateStatus"]
  noteCreateError: string | null
  noteEdit: FeedbackDetailsSnapshot["noteEdit"]
  noteDelete: FeedbackDetailsSnapshot["noteDelete"]
  onStartCorrection?: () => void
  onDraftSentimentChange?: (sentiment: FeedbackSentiment) => void
  onCancelCorrection?: () => void
  onSaveCorrection?: () => void
  onViewGuestProfile?: (locationGuestId: number) => void
  onNoteDraftChange?: (value: string) => void
  onCreateNote?: () => void
  onStartNoteEdit?: (noteId: number) => void
  onNoteEditDraftChange?: (value: string) => void
  onCancelNoteEdit?: () => void
  onSaveNoteEdit?: () => Promise<boolean> | void
  onStartNoteDelete?: (noteId: number) => void
  onCancelNoteDelete?: () => void
  onConfirmNoteDelete?: () => void
}) {
  const noteBusy = noteCreateStatus === "saving"
  const noteEditBusy = noteEdit.saveStatus === "saving"
  const noteDeleteBusy = noteDelete.deleteStatus === "deleting"
  const trimmedNote = noteDraft.trim()
  const canSubmitNote =
    details.canAddInternalNote
    && trimmedNote.length > 0
    && trimmedNote.length <= FEEDBACK_INTERNAL_NOTE_MAX_LENGTH
    && !noteBusy
    && onCreateNote != null

  return (
    <>
      <Section title="Guest feedback" className="gap-4">
        <p className="text-base font-medium text-foreground">
          “{details.comment}”
        </p>
      </Section>

      <IssueTagsSection details={details} />

      <section className={cn(FEEDBACK_DRAWER_SECTION_CLASS, "gap-5")}>
        <h3 className="text-lg font-bold text-foreground">Guest</h3>
        <div className="flex flex-col gap-1.5">
          <p className="text-sm font-semibold text-foreground">
            {details.guestName}
          </p>
          <p className="text-sm font-medium text-[var(--op-color-gray-550)] underline decoration-solid">
            {details.guestContact}
          </p>
        </div>
        <div className="flex flex-col gap-1.5">
          <p className="text-sm font-medium text-[var(--op-color-gray-550)]">
            Contact state:
          </p>
          <p className="text-sm font-medium text-foreground">
            {details.contactAvailability === "Email" || details.contactAvailability === "Phone"
              ? "Contact available"
              : details.contactAvailability}
          </p>
        </div>
        <Button
          type="button"
          variant="link"
          size="link-sm"
          disabled={!details.canViewGuestProfile || onViewGuestProfile == null}
          aria-disabled={!details.canViewGuestProfile || onViewGuestProfile == null}
          className="w-fit gap-1.5 font-medium disabled:opacity-40"
          aria-label={
            details.canViewGuestProfile && onViewGuestProfile != null
              ? "View guest profile"
              : "View guest profile (unavailable)"
          }
          onClick={() => {
            if (
              !details.canViewGuestProfile ||
              details.locationGuestId == null ||
              onViewGuestProfile == null
            ) {
              return
            }
            onViewGuestProfile(details.locationGuestId)
          }}
        >
          View guest profile
          <ChevronRightIcon className="size-4" aria-hidden />
        </Button>
      </section>

      <ClassificationSection
        details={details}
        correction={correction}
        onStartCorrection={onStartCorrection}
        onDraftSentimentChange={onDraftSentimentChange}
        onCancelCorrection={onCancelCorrection}
        onSaveCorrection={onSaveCorrection}
      />

      <section className={cn(FEEDBACK_DRAWER_SECTION_CLASS, "gap-5")}>
        <h3 className="text-lg font-bold text-foreground">
          Submission details
        </h3>
        <DetailField label="Restaurant:" value={details.locationName} />
        <DetailField label="Location:" value={details.address} />
        {details.qrSource != null ? (
          <DetailField label="QR source:" value={details.qrSource} />
        ) : null}
        <DetailField
          label="Submitted:"
          value={formatSubmittedAbsolute(details.createdAt)}
        />
        <DetailField
          label="Feedback ID:"
          value={details.feedbackReference}
        />
      </section>

      <section className={cn(FEEDBACK_DRAWER_SECTION_CLASS, "gap-5")}>
        <div className="flex flex-col gap-2">
          <h3 className="text-lg font-bold text-foreground">Follow-up</h3>
          <p className="text-sm font-medium text-[var(--op-color-gray-550)]">
            Review the available contact options and record any response or operational action taken.
          </p>
        </div>
        <div className="flex flex-col gap-4">
          <div className="flex items-center gap-1.5">
            <p className="text-sm font-medium text-foreground">Recovery status:</p>
            <Badge variant="soft">Not started</Badge>
          </div>
          <div className="flex items-center gap-1.5">
            <p className="text-sm font-medium text-foreground">Workflow status:</p>
            <Badge variant="soft">{feedbackWorkflowStatusLabel(details.workflowStatus)}</Badge>
          </div>
          <div className="flex items-center gap-1.5">
            <p className="text-sm font-medium text-foreground">Contact availability:</p>
            <Badge variant="soft">
              {details.contactAvailability === "Email"
                ? "Email available"
                : details.contactAvailability === "Phone"
                  ? "Phone available"
                  : details.contactAvailability}
            </Badge>
          </div>
          <div className="flex items-center gap-1.5">
            <p className="text-sm font-medium text-foreground">Last follow-up:</p>
            <p className="text-sm font-medium text-[var(--op-color-gray-550)]">
              {details.lastFollowUpDisplay}
            </p>
          </div>
        </div>
        <Button
          type="button"
          variant="op-secondary"
          size="op"
          disabled
          aria-disabled
          aria-label="Start recovery (unavailable)"
          title="Start recovery is unavailable"
          className="w-fit"
        >
          Start recovery
        </Button>
      </section>

      <section className={cn(FEEDBACK_DRAWER_SECTION_CLASS, "gap-[22px]")}>
        <h3 className="text-lg font-bold text-foreground">Internal notes</h3>
        {details.internalNotes.length > 0 ? (
          <ul className="flex flex-col gap-3">
            {details.internalNotes.map((note) => (
              <InternalNoteRow
                key={note.id}
                note={note}
                noteEditOpen={noteEdit.editingNoteId === note.id}
                noteEditBusy={noteEditBusy}
                onStartNoteEdit={onStartNoteEdit}
                onNoteEditDraftChange={onNoteEditDraftChange}
                onCancelNoteEdit={onCancelNoteEdit}
                onSaveNoteEdit={onSaveNoteEdit}
                onStartNoteDelete={onStartNoteDelete}
              />
            ))}
          </ul>
        ) : null}
        <div className="flex flex-col gap-3">
          <Textarea
            value={noteDraft}
            onChange={(event) => {
              onNoteDraftChange?.(event.target.value)
            }}
            disabled={!details.canAddInternalNote || noteBusy}
            aria-disabled={!details.canAddInternalNote || noteBusy}
            maxLength={FEEDBACK_INTERNAL_NOTE_MAX_LENGTH}
            rows={3}
            placeholder="Add details about the feedback or any action taken…"
            className="min-h-0 resize-none rounded-[4px] border-input px-[13px] py-[15px] text-sm placeholder:text-guest-feedback-placeholder disabled:opacity-60 dark:bg-transparent dark:disabled:bg-transparent"
          />
          {noteCreateError != null ? (
            <p className="text-sm text-destructive" role="alert">
              {noteCreateError}
            </p>
          ) : null}
          <Button
            type="button"
            variant="op-secondary"
            disabled={!canSubmitNote}
            aria-disabled={!canSubmitNote}
            className="w-fit rounded-[2px]"
            onClick={() => {
              onCreateNote?.()
            }}
          >
            {noteBusy ? "Adding…" : "Add note"}
          </Button>
        </div>
      </section>

      <Section title="Activity history">
        {details.activityHistory.map((event) => (
          <div
            key={`${event.kind}-${event.at}-${event.actorDisplayName ?? ""}-${event.fromSentiment ?? ""}-${event.toSentiment ?? ""}-${event.fromWorkflowStatus ?? ""}-${event.toWorkflowStatus ?? ""}`}
            className="flex flex-col gap-1.5"
          >
            <p className="text-sm font-semibold text-foreground">
              {formatActivityTime(event.at)}
            </p>
            <p className="text-xs font-normal text-foreground">
              {activityLabel(event)}
            </p>
            {event.actorDisplayName ? (
              <p className="text-xs font-normal text-muted-foreground">
                {event.actorDisplayName}
              </p>
            ) : null}
          </div>
        ))}
      </Section>

      <section
        className={cn(
          FEEDBACK_DRAWER_SECTION_CLASS,
          "border-b-0"
        )}
      >
        <Button
          type="button"
          variant="op-primary"
          size="op"
          disabled
          aria-disabled
          aria-label="Start recovery (unavailable)"
          title="Start recovery is unavailable"
          className="w-fit"
        >
          Start recovery
        </Button>
      </section>

      <OperatorNoteDeleteDialog
        open={noteDelete.deletingNoteId != null}
        busy={noteDeleteBusy}
        error={noteDelete.deleteError}
        onOpenChange={(open) => {
          if (!open) {
            onCancelNoteDelete?.()
          }
        }}
        onConfirm={() => {
          void onConfirmNoteDelete?.()
        }}
      />
    </>
  )
}

/** Feedback details — modal right Drawer (Figma 3549:64178). */
export function FeedbackDetailsDrawer({
  snapshot,
  onOpenChange,
  onRetry,
  onStartCorrection,
  onDraftSentimentChange,
  onCancelCorrection,
  onSaveCorrection,
  onReopen,
  onStartMarkResolved,
  onMarkNoActionNeeded,
  onCancelCloseOut,
  onSetCloseOutReason,
  onSetCloseOutNoteDraft,
  onConfirmCloseOut,
  onViewGuestProfile,
  onNoteDraftChange,
  onCreateNote,
  onStartNoteEdit,
  onNoteEditDraftChange,
  onCancelNoteEdit,
  onSaveNoteEdit,
  onStartNoteDelete,
  onCancelNoteDelete,
  onConfirmNoteDelete,
  canGoPrevious,
  canGoNext,
  onPrevious,
  onNext,
}: FeedbackDetailsDrawerProps) {
  const submittedAbsolute =
    snapshot.details != null
      ? formatSubmittedAbsolute(snapshot.details.createdAt) || undefined
      : undefined

  return (
    <Drawer
      open={snapshot.isOpen}
      onOpenChange={onOpenChange}
      direction="right"
    >
      <DrawerContent
        className={cn(
          OPERATOR_RIGHT_DRAWER_CONTENT_CLASS,
          "dark:bg-[#1b1b1b]"
        )}
      >
        <div className="flex min-h-0 flex-1 flex-col">
          {snapshot.loadStatus === "loading" ||
          snapshot.loadStatus === "idle" ? (
            <>
              <FeedbackDetailsDrawerHeader description="Loading Feedback details" />
              <div
                className={cn(
                  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
                  "px-[22px] pb-[22px]"
                )}
              >
                <div
                  className="flex min-h-48 items-center justify-center"
                  role="status"
                  aria-live="polite"
                  aria-label="Loading Feedback details"
                >
                  <div
                    className="size-8 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
                    aria-hidden
                  />
                </div>
              </div>
            </>
          ) : snapshot.loadStatus === "error" ? (
            <>
              <FeedbackDetailsDrawerHeader description="Feedback details failed to load" />
              <div
                className={cn(
                  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
                  "px-[22px] pb-[22px]"
                )}
              >
                <div className="flex min-h-48 flex-col items-center justify-center gap-3 text-center">
                  <p className="text-sm text-destructive" role="alert">
                    {snapshot.loadError ??
                      "Could not load Feedback details. Please try again."}
                  </p>
                  <Button
                    type="button"
                    variant="link"
                    size="link-sm"
                    className={cn(
                      OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
                      "font-medium underline"
                    )}
                    onClick={onRetry}
                  >
                    Retry
                  </Button>
                </div>
              </div>
            </>
          ) : snapshot.details != null ? (
            <>
              <FeedbackDetailsDrawerHeader
                venueLine={snapshot.details.venueLine}
                submittedAbsolute={submittedAbsolute}
                isNew={snapshot.details.isNew}
                needsAttention={snapshot.details.needsAttention}
                canReopen={snapshot.details.canReopen}
                canMarkNoActionNeeded={snapshot.details.canMarkNoActionNeeded}
                canViewGuestProfile={snapshot.details.canViewGuestProfile}
                feedbackReference={snapshot.details.feedbackReference}
                locationGuestId={snapshot.details.locationGuestId}
                onReopen={onReopen}
                onStartMarkResolved={onStartMarkResolved}
                onMarkNoActionNeeded={onMarkNoActionNeeded}
                onViewGuestProfile={onViewGuestProfile}
                canGoPrevious={canGoPrevious}
                canGoNext={canGoNext}
                onPrevious={onPrevious}
                onNext={onNext}
              />
              <div className={OPERATOR_RIGHT_DRAWER_BODY_CLASS}>
                <LoadedBody
                  details={snapshot.details}
                  correction={snapshot.correction}
                  noteDraft={snapshot.noteDraft}
                  noteCreateStatus={snapshot.noteCreateStatus}
                  noteCreateError={snapshot.noteCreateError}
                  noteEdit={snapshot.noteEdit}
                  noteDelete={snapshot.noteDelete}
                  onStartCorrection={onStartCorrection}
                  onDraftSentimentChange={onDraftSentimentChange}
                  onCancelCorrection={onCancelCorrection}
                  onSaveCorrection={onSaveCorrection}
                  onViewGuestProfile={onViewGuestProfile}
                  onNoteDraftChange={onNoteDraftChange}
                  onCreateNote={onCreateNote}
                  onStartNoteEdit={onStartNoteEdit}
                  onNoteEditDraftChange={onNoteEditDraftChange}
                  onCancelNoteEdit={onCancelNoteEdit}
                  onSaveNoteEdit={onSaveNoteEdit}
                  onStartNoteDelete={onStartNoteDelete}
                  onCancelNoteDelete={onCancelNoteDelete}
                  onConfirmNoteDelete={onConfirmNoteDelete}
                />
              </div>
            </>
          ) : null}
        </div>
      </DrawerContent>
      {snapshot.closeOut.isOpen ? (
        <FeedbackCloseOutDialog
          closeOut={snapshot.closeOut}
          details={snapshot.details}
          onOpenChange={(open) => {
            if (!open) {
              onCancelCloseOut?.()
            }
          }}
          onReasonChange={(reason) => {
            onSetCloseOutReason?.(reason)
          }}
          onNoteDraftChange={(value) => {
            onSetCloseOutNoteDraft?.(value)
          }}
          onConfirm={() => {
            void onConfirmCloseOut?.()
          }}
        />
      ) : null}
    </Drawer>
  )
}

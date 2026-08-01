import type { ReactNode } from "react"
import {
  ChevronRightIcon,
  EllipsisVerticalIcon,
  SquarePenIcon,
  XIcon,
} from "lucide-react"

import { GuestProfileAddNoteDialog } from "@/components/dashboard/operator/GuestProfile/GuestProfileAddNoteDialog"
import { OperatorNoteDeleteDialog } from "@/components/dashboard/operator/OperatorNoteDeleteDialog"
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
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
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
import { feedbackSentimentLabel } from "@/lib/operatorHome/feedbackSentimentLabel"
import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
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
  FeedbackWorkflowStatus,
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
  onWorkflowStatusChange?: (status: FeedbackWorkflowStatus) => void
  onReopen?: () => void
  onMarkNoActionNeeded?: () => void
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
  nowMs?: number
}

const WORKFLOW_STATUS_OPTIONS: Array<{
  value: FeedbackWorkflowStatus
  label: string
}> = (
  ["new", "in_progress", "resolved"] as const
).map((value) => ({
  value,
  label: feedbackWorkflowStatusLabel(value),
}))

const SENTIMENT_OPTIONS: Array<{
  value: FeedbackSentiment
  label: string
}> = [
  { value: "positive", label: "Positive" },
  { value: "neutral", label: "Neutral" },
  { value: "negative", label: "Negative" },
]

function formatSubmittedAbsolute(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return ""
  }

  const datePart = date.toLocaleDateString("en-GB", {
    day: "numeric",
    month: "long",
    year: "numeric",
  })
  const timePart = date.toLocaleTimeString("en-GB", {
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  })
  return `${datePart} at ${timePart}`
}

function formatActivityTime(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return ""
  }

  return date.toLocaleTimeString("en-GB", {
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  })
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
    <p className="text-sm font-medium text-[#7c7c7c]">
      {children}
    </p>
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

function DetectedTagsSection({ details }: { details: FeedbackDetailsLoaded }) {
  const status = details.classificationStatus

  return (
    <Section title="Detected issues">
      {status === "Pending" ? (
        <PendingEmpty>
          Detected issues will appear when classification is available.
        </PendingEmpty>
      ) : null}
      {status === "Failed" ? (
        <PendingEmpty>Detected issues unavailable.</PendingEmpty>
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
    </Section>
  )
}

function FeedbackDetailsDrawerHeader({
  venueLine,
  relativeSubmitted,
  isNew,
  needsAttention,
  canReopen,
  canMarkNoActionNeeded,
  workflowBusy,
  onReopen,
  onMarkNoActionNeeded,
  description,
}: {
  venueLine?: string
  relativeSubmitted?: string
  isNew?: boolean
  needsAttention?: boolean
  canReopen?: boolean
  canMarkNoActionNeeded?: boolean
  workflowBusy?: boolean
  onReopen?: () => void
  onMarkNoActionNeeded?: () => void
  description?: string
}) {
  const showBadges = isNew || needsAttention
  const showMenu =
    onReopen != null || onMarkNoActionNeeded != null

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
          {relativeSubmitted ? (
            <p className="text-xs font-medium text-[#7c7c7c]">
              Submitted {relativeSubmitted}
            </p>
          ) : null}
        </div>
        {showBadges ? (
          <div className="flex flex-wrap gap-3">
            {isNew ? <Badge variant="soft">New</Badge> : null}
            {needsAttention ? (
              <Badge variant="negative">Needs attention</Badge>
            ) : null}
          </div>
        ) : null}
      </div>
      <div className="flex shrink-0 items-start gap-2">
        {showMenu ? (
          <DropdownMenu modal={false}>
            <DropdownMenuTrigger asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="size-[42px] shrink-0 rounded-[2px] bg-[#f1f1f1] hover:bg-[#e8e8e8] dark:bg-[#2c2c2c] dark:hover:bg-[#2c2c2c]"
                aria-label="Feedback details actions"
                disabled={workflowBusy}
              >
                <EllipsisVerticalIcon className="size-[18px]" aria-hidden />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              align="end"
              className={cn("z-[120] min-w-48", OPERATOR_SHELL_MENU_PANEL_CLASS)}
            >
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
                <DropdownMenuItem
                  className="rounded-md px-2.5 py-1.5 text-sm"
                  onClick={() => {
                    onMarkNoActionNeeded?.()
                  }}
                >
                  Mark no action needed
                </DropdownMenuItem>
              ) : null}
            </DropdownMenuContent>
          </DropdownMenu>
        ) : null}
        <DrawerClose asChild>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="size-[42px] shrink-0 rounded-[2px] bg-[#f1f1f1] hover:bg-[#e8e8e8] dark:bg-[#2c2c2c] dark:hover:bg-[#2c2c2c]"
            aria-label="Close Feedback details"
          >
            <XIcon className="size-[18px]" aria-hidden />
          </Button>
        </DrawerClose>
      </div>
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
  workflowSaveStatus,
  workflowSaveError,
  onStartCorrection,
  onDraftSentimentChange,
  onCancelCorrection,
  onSaveCorrection,
  onWorkflowStatusChange,
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
  workflowSaveStatus: FeedbackDetailsSnapshot["workflowSaveStatus"]
  workflowSaveError: string | null
  onStartCorrection?: () => void
  onDraftSentimentChange?: (sentiment: FeedbackSentiment) => void
  onCancelCorrection?: () => void
  onSaveCorrection?: () => void
  onWorkflowStatusChange?: (status: FeedbackWorkflowStatus) => void
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
  const workflowBusy = workflowSaveStatus === "saving"
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

      <ClassificationSection
        details={details}
        correction={correction}
        onStartCorrection={onStartCorrection}
        onDraftSentimentChange={onDraftSentimentChange}
        onCancelCorrection={onCancelCorrection}
        onSaveCorrection={onSaveCorrection}
      />

      <DetectedTagsSection details={details} />

      <section className={cn(FEEDBACK_DRAWER_SECTION_CLASS, "gap-5")}>
        <h3 className="text-lg font-bold text-foreground">Guest</h3>
        <div className="flex flex-col gap-1.5">
          <p className="text-sm font-semibold text-foreground">
            {details.guestName}
          </p>
          <p className="text-sm font-medium text-[#7c7c7c] underline decoration-solid">
            {details.guestContact}
          </p>
        </div>
        <Button
          type="button"
          variant="link"
          size="link-sm"
          disabled={!details.canViewGuestProfile}
          aria-disabled={!details.canViewGuestProfile}
          className="w-fit gap-1.5 font-medium disabled:opacity-40"
          aria-label={
            details.canViewGuestProfile
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

      <section className={cn(FEEDBACK_DRAWER_SECTION_CLASS, "gap-5")}>
        <h3 className="text-lg font-bold text-foreground">
          Submission details
        </h3>
        <div className="flex flex-col gap-1.5">
          <p className="text-base font-medium text-foreground">Restaurant</p>
          <p className="text-sm font-medium text-[#7c7c7c]">
            {details.locationName}
          </p>
        </div>
        <div className="flex flex-col gap-1.5">
          <p className="text-base font-medium text-foreground">Location</p>
          <p className="text-sm font-medium text-[#7c7c7c]">
            {details.address}
          </p>
        </div>
        <div className="flex flex-col gap-1.5">
          <p className="text-base font-medium text-foreground">Submitted</p>
          <p className="text-sm font-medium text-[#7c7c7c]">
            {formatSubmittedAbsolute(details.createdAt)}
          </p>
        </div>
      </section>

      <section className={cn(FEEDBACK_DRAWER_SECTION_CLASS, "gap-5")}>
        <h3 className="text-lg font-bold text-foreground">Follow-up</h3>
        <FloatingLabelSelect
          label="Status"
          options={WORKFLOW_STATUS_OPTIONS}
          value={details.workflowStatus}
          onValueChange={(value) => {
            onWorkflowStatusChange?.(value as FeedbackWorkflowStatus)
          }}
          disabled={workflowBusy || onWorkflowStatusChange == null}
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
        {workflowSaveError != null ? (
          <p className="text-sm text-destructive" role="alert">
            {workflowSaveError}
          </p>
        ) : null}
      </section>

      <section className={cn(FEEDBACK_DRAWER_SECTION_CLASS, "gap-[22px]")}>
        <h3 className="text-lg font-bold text-foreground">
          Add an internal note
        </h3>
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

/** Feedback details — modal right Drawer (Figma 3714:23508). */
export function FeedbackDetailsDrawer({
  snapshot,
  onOpenChange,
  onRetry,
  onStartCorrection,
  onDraftSentimentChange,
  onCancelCorrection,
  onSaveCorrection,
  onWorkflowStatusChange,
  onReopen,
  onMarkNoActionNeeded,
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
  nowMs = Date.now(),
}: FeedbackDetailsDrawerProps) {
  const relativeSubmitted =
    snapshot.details != null
      ? formatRelativeTime(snapshot.details.createdAt, nowMs) || undefined
      : undefined
  const workflowBusy = snapshot.workflowSaveStatus === "saving"

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
                relativeSubmitted={relativeSubmitted}
                isNew={snapshot.details.isNew}
                needsAttention={snapshot.details.needsAttention}
                canReopen={snapshot.details.canReopen}
                canMarkNoActionNeeded={snapshot.details.canMarkNoActionNeeded}
                workflowBusy={workflowBusy}
                onReopen={onReopen}
                onMarkNoActionNeeded={onMarkNoActionNeeded}
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
                  workflowSaveStatus={snapshot.workflowSaveStatus}
                  workflowSaveError={snapshot.workflowSaveError}
                  onStartCorrection={onStartCorrection}
                  onDraftSentimentChange={onDraftSentimentChange}
                  onCancelCorrection={onCancelCorrection}
                  onSaveCorrection={onSaveCorrection}
                  onWorkflowStatusChange={onWorkflowStatusChange}
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
    </Drawer>
  )
}

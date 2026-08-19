import { CalendarIcon, MailIcon, MessageSquareIcon } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Spinner } from "@/components/ui/spinner"
import { Textarea } from "@/components/ui/textarea"
import {
  FEEDBACK_FIELD_LABEL_CLASS,
  FEEDBACK_INPUT_CLASS,
  FEEDBACK_TEXTAREA_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import type {
  ManageMarketingPreferencesSaveResult,
  ManageMarketingPreferencesSnapshot,
} from "@/lib/operatorGuests/createManageMarketingPreferencesSessionModule"
import { GUESTS_PAGE_PRIMARY_BUTTON_CLASS } from "@/lib/operatorGuests/guestsPresentation"
import {
  MANAGE_MARKETING_PREFERENCES_CARDS_CLASS,
  MANAGE_MARKETING_PREFERENCES_CARD_DISABLED_CLASS,
  MANAGE_MARKETING_PREFERENCES_CARD_IDLE_CLASS,
  MANAGE_MARKETING_PREFERENCES_CARD_SELECTED_CLASS,
  MANAGE_MARKETING_PREFERENCES_CARD_SURFACE_CLASS,
  MANAGE_MARKETING_PREFERENCES_CHANNEL_ICON_CLASS,
  MANAGE_MARKETING_PREFERENCES_COPY,
  MANAGE_MARKETING_PREFERENCES_DIALOG_CLASS,
  MANAGE_MARKETING_PREFERENCES_DIVIDER_CLASS,
  MANAGE_MARKETING_PREFERENCES_EVIDENCE_FIELD_CLASS,
  MANAGE_MARKETING_PREFERENCES_SECTION_CLASS,
} from "@/lib/operatorGuests/manageMarketingPreferencesPresentation"
import { cn } from "@/lib/utils"
import type { LocationGuestMarketingPreference } from "@/types/dashboard"

const MUTED_HELPER_CLASS = "text-[var(--op-color-gray-550)]"

type ManageMarketingPreferencesDialogProps = {
  snapshot: ManageMarketingPreferencesSnapshot
  onOpenChange: (open: boolean) => void
  onDraftPreferenceChange: (
    preference: LocationGuestMarketingPreference
  ) => void
  onDraftNoteChange: (note: string) => void
  onSave: () => Promise<ManageMarketingPreferencesSaveResult>
  onNoteSaveFailure: (message: string) => void
}

function ChannelSection({
  channel,
  available,
  snapshot,
  busy,
  onDraftPreferenceChange,
}: {
  channel: "email" | "sms"
  available: boolean
  snapshot: ManageMarketingPreferencesSnapshot
  busy: boolean
  onDraftPreferenceChange: (
    preference: LocationGuestMarketingPreference
  ) => void
}) {
  const copy = MANAGE_MARKETING_PREFERENCES_COPY
  const Icon = channel === "email" ? MailIcon : MessageSquareIcon
  const label =
    channel === "email" ? copy.emailChannelLabel : copy.smsChannelLabel

  return (
    <section className={MANAGE_MARKETING_PREFERENCES_SECTION_CLASS}>
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <span className={MANAGE_MARKETING_PREFERENCES_CHANNEL_ICON_CLASS}>
            <Icon className="size-4 text-op-text-primary" aria-hidden />
          </span>
          <p className="m-0 text-sm font-medium leading-5 text-op-text-primary">
            {label}
          </p>
        </div>
        {available ? (
          <Badge variant="positive">{copy.availableBadge}</Badge>
        ) : (
          <Badge variant="soft" className={MUTED_HELPER_CLASS}>
            {copy.unavailableBadge}
          </Badge>
        )}
      </div>

      {available ? (
        <div
          role="radiogroup"
          aria-label={label}
          className={MANAGE_MARKETING_PREFERENCES_CARDS_CLASS}
        >
          {snapshot.statusCards.map((card) => {
            const cannotSelect = card.disabled || busy
            return (
              <Button
                key={`${channel}-${card.id}`}
                type="button"
                variant="op-ghost"
                role="radio"
                aria-checked={card.selected}
                aria-disabled={cannotSelect}
                className={cn(
                  MANAGE_MARKETING_PREFERENCES_CARD_SURFACE_CLASS,
                  card.selected
                    ? MANAGE_MARKETING_PREFERENCES_CARD_SELECTED_CLASS
                    : MANAGE_MARKETING_PREFERENCES_CARD_IDLE_CLASS,
                  cannotSelect
                    ? MANAGE_MARKETING_PREFERENCES_CARD_DISABLED_CLASS
                    : null
                )}
                onClick={() => {
                  if (cannotSelect) {
                    return
                  }
                  onDraftPreferenceChange(card.id)
                }}
              >
                <span className="flex flex-col items-start gap-1">
                  <span className="text-sm font-medium text-op-text-primary">
                    {card.label}
                  </span>
                  <span className={cn("text-xs font-medium", MUTED_HELPER_CLASS)}>
                    {card.helper}
                  </span>
                </span>
              </Button>
            )
          })}
        </div>
      ) : null}
    </section>
  )
}

/** Figma Manage marketing preferences — node 5014:49795. */
export function ManageMarketingPreferencesDialog({
  snapshot,
  onOpenChange,
  onDraftPreferenceChange,
  onDraftNoteChange,
  onSave,
  onNoteSaveFailure,
}: ManageMarketingPreferencesDialogProps) {
  const copy = MANAGE_MARKETING_PREFERENCES_COPY
  const busy = snapshot.saveStatus === "saving"

  return (
    <Dialog
      open={snapshot.isOpen}
      onOpenChange={(open) => {
        if (!open) {
          onOpenChange(false)
        }
      }}
    >
      <DialogContent
        showCloseButton
        className={MANAGE_MARKETING_PREFERENCES_DIALOG_CLASS}
      >
        <div className="flex flex-col gap-10">
          <DialogHeader className="gap-3 pr-10">
            <DialogTitle className="text-2xl font-bold tracking-normal text-op-text-primary">
              {copy.dialogTitle}
            </DialogTitle>
            <DialogDescription
              className={cn(
                "max-w-none text-sm font-medium leading-normal",
                MUTED_HELPER_CLASS
              )}
            >
              {snapshot.subtitle}
            </DialogDescription>
          </DialogHeader>

          {snapshot.loadStatus === "loading" || snapshot.loadStatus === "idle" ? (
            <div
              className="flex min-h-[200px] items-center justify-center"
              role="status"
              aria-live="polite"
              aria-label="Loading marketing preferences"
            >
              <Spinner />
            </div>
          ) : null}

          {snapshot.loadStatus === "error" ? (
            <p className="m-0 text-sm text-destructive">
              {snapshot.loadError ?? copy.loadError}
            </p>
          ) : null}

          {snapshot.loadStatus === "loaded" ? (
            <div className="flex flex-col gap-[30px]">
              <ChannelSection
                channel="email"
                available={snapshot.emailAvailable}
                snapshot={snapshot}
                busy={busy}
                onDraftPreferenceChange={onDraftPreferenceChange}
              />
              <div
                className={MANAGE_MARKETING_PREFERENCES_DIVIDER_CLASS}
                aria-hidden
              />
              <ChannelSection
                channel="sms"
                available={snapshot.smsAvailable}
                snapshot={snapshot}
                busy={busy}
                onDraftPreferenceChange={onDraftPreferenceChange}
              />
              {snapshot.consequenceHelper != null ? (
                <p className={cn("m-0 text-xs font-medium", MUTED_HELPER_CLASS)}>
                  {snapshot.consequenceHelper}
                </p>
              ) : null}
              <div className="flex w-full flex-col gap-5 sm:flex-row">
                <div className="flex min-w-0 flex-1 flex-col gap-2">
                  <label
                    htmlFor="manage-marketing-preferences-source"
                    className={FEEDBACK_FIELD_LABEL_CLASS}
                  >
                    {copy.permissionSourceLabel}
                  </label>
                  <Input
                    id="manage-marketing-preferences-source"
                    value={snapshot.permissionSourceDisplay}
                    readOnly
                    disabled
                    className={cn(
                      FEEDBACK_INPUT_CLASS,
                      MANAGE_MARKETING_PREFERENCES_EVIDENCE_FIELD_CLASS
                    )}
                  />
                </div>
                <div className="flex min-w-0 flex-1 flex-col gap-2">
                  <label
                    htmlFor="manage-marketing-preferences-recorded-on"
                    className={FEEDBACK_FIELD_LABEL_CLASS}
                  >
                    {copy.recordedOnLabel}
                  </label>
                  <div className="relative">
                    <CalendarIcon
                      className="pointer-events-none absolute top-1/2 left-[15px] size-3.5 -translate-y-1/2 text-[var(--op-color-gray-550)]"
                      aria-hidden
                    />
                    <Input
                      id="manage-marketing-preferences-recorded-on"
                      value={snapshot.recordedOnDisplay}
                      readOnly
                      disabled
                      className={cn(
                        FEEDBACK_INPUT_CLASS,
                        MANAGE_MARKETING_PREFERENCES_EVIDENCE_FIELD_CLASS,
                        "pl-10"
                      )}
                    />
                  </div>
                </div>
              </div>
              <div
                className={MANAGE_MARKETING_PREFERENCES_DIVIDER_CLASS}
                aria-hidden
              />
              <div className="flex min-h-[169px] flex-col gap-2">
                <label
                  htmlFor="manage-marketing-preferences-note"
                  className={FEEDBACK_FIELD_LABEL_CLASS}
                >
                  {copy.noteLabel}
                </label>
                <Textarea
                  id="manage-marketing-preferences-note"
                  value={snapshot.draftNote}
                  onChange={(event) => {
                    onDraftNoteChange(event.target.value)
                  }}
                  placeholder={copy.notePlaceholder}
                  maxLength={5000}
                  disabled={busy}
                  className={cn(
                    FEEDBACK_TEXTAREA_CLASS,
                    "min-h-[120px] flex-1 shadow-none"
                  )}
                />
              </div>
            </div>
          ) : null}
        </div>

        {snapshot.saveError != null ? (
          <p className="m-0 text-sm text-destructive" role="alert">
            {snapshot.saveError}
          </p>
        ) : null}

        <DialogFooter className="flex-row gap-3 sm:justify-start">
          <Button
            variant="op-primary"
            type="button"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
            disabled={!snapshot.canSave || busy}
            onClick={() => {
              void (async () => {
                const result = await onSave()
                if (result.status === "saved_with_note_error") {
                  onNoteSaveFailure(result.message)
                }
              })()
            }}
          >
            {copy.saveLabel}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className="rounded-[2px]"
            onClick={() => {
              onOpenChange(false)
            }}
          >
            {copy.cancelLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

import { MailIcon, MessageSquareIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import { Textarea } from "@/components/ui/textarea"
import { cn } from "@/lib/utils"
import {
  FEEDBACK_DIALOG_SELECT_ITEM_CLASS,
  FEEDBACK_RECOVERY_SELECT_MENU_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import {
  RESPONSE_SETUP_INCLUDE_NOTES_HELPER,
  RESPONSE_SETUP_INCLUDE_NOTES_LABEL,
  RESPONSE_SETUP_INCLUDE_NOTES_PLACEHOLDER,
  RESPONSE_SETUP_PURPOSE_LABEL,
  RESPONSE_SETUP_TONE_LABEL,
  buildResponseSetupChannelCards,
} from "@/lib/operatorFeedback/responseSetupPresentation"
import {
  RESPOND_TO_GUEST_PURPOSE_OPTIONS,
  RESPOND_TO_GUEST_TONE_OPTIONS,
  type RespondToGuestChannel,
  type RespondToGuestPurposeId,
  type RespondToGuestToneId,
} from "@/lib/operatorFeedback/respondToGuestPresentation"

type ResponseSetupFieldsProps = {
  idPrefix: string
  availableChannels: readonly RespondToGuestChannel[]
  channel: RespondToGuestChannel | null
  maskedDestination: string | null
  onChannelChange: (channel: RespondToGuestChannel) => void
  /** When set, purpose is a locked read-only row (recovery offer). */
  lockedPurposeLabel?: string | null
  purpose: RespondToGuestPurposeId | null
  onPurposeChange?: (purpose: RespondToGuestPurposeId) => void
  tone: RespondToGuestToneId | null
  onToneChange: (tone: RespondToGuestToneId) => void
  includeNotes: string
  onIncludeNotesChange: (value: string) => void
  disabled?: boolean
}

/** Shared Response setup form chrome for guest-messaging recovery intents. */
export function ResponseSetupFields({
  idPrefix,
  availableChannels,
  channel,
  maskedDestination,
  onChannelChange,
  lockedPurposeLabel = null,
  purpose,
  onPurposeChange,
  tone,
  onToneChange,
  includeNotes,
  onIncludeNotesChange,
  disabled = false,
}: ResponseSetupFieldsProps) {
  const channelCards = buildResponseSetupChannelCards({
    availableChannels,
    selectedChannel: channel,
    maskedDestination,
  })
  const includeNotesId = `${idPrefix}-include-notes`

  return (
    <div className="flex w-full flex-col gap-7">
      <div
        className="flex flex-col gap-[18px]"
        role="radiogroup"
        aria-label="How should the guest be contacted?"
      >
        {channelCards.map((card) => {
          const Icon = card.channel === "email" ? MailIcon : MessageSquareIcon
          return (
            <Button
              key={card.channel}
              type="button"
              variant="ghost"
              role="radio"
              aria-checked={card.selected}
              disabled={disabled}
              className={cn(
                "h-auto w-full items-center justify-start gap-2.5 rounded-[4px] border px-[18px] py-4 text-left whitespace-normal hover:bg-transparent",
                card.selected
                  ? "border-op-text-muted bg-[var(--op-color-gray-995)]"
                  : "border-op-card-border bg-[var(--op-color-gray-995)] hover:border-op-text-muted"
              )}
              onClick={() => {
                onChannelChange(card.channel)
              }}
            >
              <span className="flex size-9 shrink-0 items-center justify-center rounded-[2px] bg-[var(--op-color-gray-990)] p-2.5">
                <Icon className="size-4 text-op-text-primary" aria-hidden />
              </span>
              <span className="flex min-w-0 flex-col gap-1">
                <span className="text-sm font-medium text-op-text-primary">
                  {card.title}
                </span>
                <span className="text-xs font-medium text-op-text-muted">
                  {card.availabilityLine}
                </span>
              </span>
            </Button>
          )
        })}
      </div>

      <div className="h-0 w-full border-t-2 border-op-card-border" aria-hidden />

      {lockedPurposeLabel != null ? (
        <div className="flex flex-col gap-2">
          <p className="text-sm font-semibold leading-5 text-op-text-primary">
            {RESPONSE_SETUP_PURPOSE_LABEL}
          </p>
          <div className="rounded-[4px] border border-op-card-border px-[15px] py-[15px]">
            <p className="text-sm font-normal text-op-text-primary">
              {lockedPurposeLabel}
            </p>
          </div>
        </div>
      ) : (
        <FloatingLabelSelect
          label={RESPONSE_SETUP_PURPOSE_LABEL}
          options={RESPOND_TO_GUEST_PURPOSE_OPTIONS.map((option) => ({
            value: option.id,
            label: option.label,
          }))}
          value={purpose ?? undefined}
          onValueChange={(value) => {
            onPurposeChange?.(value as RespondToGuestPurposeId)
          }}
          disableFocusRing
          contentClassName={FEEDBACK_RECOVERY_SELECT_MENU_CLASS}
          itemClassName={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
        />
      )}

      <FloatingLabelSelect
        label={RESPONSE_SETUP_TONE_LABEL}
        options={RESPOND_TO_GUEST_TONE_OPTIONS.map((option) => ({
          value: option.id,
          label: option.label,
        }))}
        value={tone ?? undefined}
        onValueChange={(value) => {
          onToneChange(value as RespondToGuestToneId)
        }}
        disableFocusRing
        contentClassName={FEEDBACK_RECOVERY_SELECT_MENU_CLASS}
        itemClassName={FEEDBACK_DIALOG_SELECT_ITEM_CLASS}
      />

      <div className="flex flex-col gap-3">
        <div className="flex flex-col gap-2">
          <label
            htmlFor={includeNotesId}
            className="text-sm font-semibold leading-5 text-op-text-primary"
          >
            {RESPONSE_SETUP_INCLUDE_NOTES_LABEL}
          </label>
          <Textarea
            id={includeNotesId}
            value={includeNotes}
            disabled={disabled}
            placeholder={RESPONSE_SETUP_INCLUDE_NOTES_PLACEHOLDER}
            onChange={(event) => {
              onIncludeNotesChange(event.target.value)
            }}
            className="min-h-[120px] rounded-[4px] border-op-input-border bg-transparent placeholder:text-op-text-muted"
          />
        </div>
        <p className="max-w-[484px] text-xs font-medium leading-4 text-op-text-muted">
          {RESPONSE_SETUP_INCLUDE_NOTES_HELPER}
        </p>
      </div>
    </div>
  )
}

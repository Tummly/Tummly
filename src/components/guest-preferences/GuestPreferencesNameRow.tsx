import { useId, type FormEvent } from "react"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import { GUEST_FEEDBACK_FIELD_INPUT_CLASS } from "@/lib/guestFeedback/guestFeedbackLayoutPresentation"
import {
  GUEST_PREFERENCES_FIELD_LABEL_CLASS,
  GUEST_PREFERENCES_FIELD_VALUE_CLASS,
  GUEST_PREFERENCES_LINK_CLASS,
  GUEST_PREFERENCES_PILL_BUTTON_CLASS,
} from "@/lib/guestPreferences/guestPreferencesPresentation"

type GuestPreferencesNameRowProps = {
  firstName: string
  /** Null while showing the saved name; the editable value while editing. */
  draft: string | null
  busy: boolean
  onEdit: () => void
  onDraftChange: (value: string) => void
  onSave: () => void
}

/** First name in the details card: read-only with Edit, or input with Save. */
export function GuestPreferencesNameRow({
  firstName,
  draft,
  busy,
  onEdit,
  onDraftChange,
  onSave,
}: GuestPreferencesNameRowProps) {
  const inputId = useId()

  if (draft === null) {
    return (
      <div className="flex w-full items-center justify-between gap-4">
        <div className="flex min-w-0 flex-col gap-1">
          <span className={GUEST_PREFERENCES_FIELD_LABEL_CLASS}>
            {GUEST_PREFERENCES_COPY.firstNameLabel}
          </span>
          <span className={GUEST_PREFERENCES_FIELD_VALUE_CLASS}>
            {firstName}
          </span>
        </div>
        <Button
          type="button"
          variant="link"
          size="link-sm"
          onClick={onEdit}
          className={GUEST_PREFERENCES_LINK_CLASS}
        >
          {GUEST_PREFERENCES_COPY.editLabel}
        </Button>
      </div>
    )
  }

  const canSave = !busy && draft.trim() !== ""

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (canSave) {
      onSave()
    }
  }

  return (
    <form className="flex w-full flex-col gap-3.5" onSubmit={handleSubmit}>
      <div className="flex w-full flex-col gap-1.5">
        <Label
          htmlFor={inputId}
          className="font-sans text-xs font-medium leading-normal text-guest-feedback-placeholder"
        >
          {GUEST_PREFERENCES_COPY.firstNameLabel}
        </Label>
        <Input
          id={inputId}
          value={draft}
          disabled={busy}
          autoComplete="given-name"
          onChange={(event) => onDraftChange(event.target.value)}
          className={GUEST_FEEDBACK_FIELD_INPUT_CLASS}
        />
      </div>
      <Button
        type="submit"
        disabled={!canSave}
        className={GUEST_PREFERENCES_PILL_BUTTON_CLASS}
      >
        {GUEST_PREFERENCES_COPY.saveNameLabel}
      </Button>
    </form>
  )
}

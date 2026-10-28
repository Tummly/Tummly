import type { FormEvent } from "react"

import { Button } from "@/components/ui/button"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import { GUEST_PREFERENCES_PILL_BUTTON_CLASS } from "@/lib/guestPreferences/guestPreferencesPresentation"

type GuestPreferencesContactInputProps = {
  value: string
  error: string | null
  busy: boolean
  onChange: (value: string) => void
  onSubmit: () => void
}

/** Email or UK mobile input and Continue. Shared by the entry form and add-contact. */
export function GuestPreferencesContactInput({
  value,
  error,
  busy,
  onChange,
  onSubmit,
}: GuestPreferencesContactInputProps) {
  const canContinue = !busy && value.trim() !== ""

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (canContinue) {
      onSubmit()
    }
  }

  return (
    <form
      className="flex w-full flex-col gap-3.5"
      noValidate
      onSubmit={handleSubmit}
    >
      <FloatingLabelInput
        label={GUEST_PREFERENCES_COPY.contactInputLabel}
        variant="dark"
        value={value}
        disabled={busy}
        error={error ?? undefined}
        onChange={(event) => onChange(event.target.value)}
      />
      <Button
        type="submit"
        disabled={!canContinue}
        className={GUEST_PREFERENCES_PILL_BUTTON_CLASS}
      >
        {GUEST_PREFERENCES_COPY.continueLabel}
      </Button>
    </form>
  )
}

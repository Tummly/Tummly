import { GuestPreferencesHeader } from "@/components/guest-preferences/GuestPreferencesHeader"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"

/**
 * Stand-in for the entry form. Task 11 replaces this with the real
 * email / mobile entry flow.
 */
export function GuestPreferencesEntryPlaceholder() {
  return <GuestPreferencesHeader lead={GUEST_PREFERENCES_COPY.entryLine} />
}

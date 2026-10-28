import { useSearchParams } from "react-router-dom"

import { resolveGuestPreferencesPageMode } from "@/lib/guestPreferences/guestPreferencesPageMode"
import { readGuestPreferencesSession } from "@/lib/guestPreferences/guestPreferencesSession"

// Placeholder. Task 10 replaces the body with the real preference centre.
export default function GuestPreferencesPage() {
  const [searchParams] = useSearchParams()
  const mode = resolveGuestPreferencesPageMode(
    searchParams,
    readGuestPreferencesSession() !== null
  )

  return (
    <div data-page-mode={mode.kind}>
      <h1>Manage your Tummly marketing preferences</h1>
    </div>
  )
}

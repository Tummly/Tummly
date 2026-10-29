const STORAGE_KEY = "tummly.guestPreferences.session"

export function readGuestPreferencesSession(): string | null {
  return sessionStorage.getItem(STORAGE_KEY)
}

export function writeGuestPreferencesSession(token: string): void {
  sessionStorage.setItem(STORAGE_KEY, token)
}

export function clearGuestPreferencesSession(): void {
  sessionStorage.removeItem(STORAGE_KEY)
}

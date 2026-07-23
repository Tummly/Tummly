/** Parse `:guestId` route param as a Location Guest id. */
export function parseLocationGuestRouteId(
  raw: string | undefined
): number | null {
  if (raw == null || raw.trim() === "") {
    return null
  }

  const parsed = Number(raw)
  if (!Number.isInteger(parsed) || parsed <= 0) {
    return null
  }

  return parsed
}

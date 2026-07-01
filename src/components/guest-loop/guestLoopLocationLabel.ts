export function getGuestLoopLocationLabel(
  locationName: string | undefined,
  index: number
): string {
  const trimmed = locationName?.trim()

  if (trimmed) {
    return trimmed
  }

  return `Location ${index + 1}`
}

/** Initials for the Operator Profile avatar from display name. */
export function getOperatorInitials(displayName: string): string {
  const parts = displayName
    .trim()
    .split(/\s+/)
    .filter(Boolean)

  if (parts.length === 0) {
    return "?"
  }

  if (parts.length === 1) {
    return parts[0]!.charAt(0).toUpperCase()
  }

  const first = parts[0]!.charAt(0)
  const last = parts[parts.length - 1]!.charAt(0)
  return `${first}${last}`.toUpperCase()
}

/** First name for the compact Profile chip in the Operator navbar. */
export function getOperatorFirstName(displayName: string): string {
  const first = displayName.trim().split(/\s+/).filter(Boolean)[0]
  return first || "Operator"
}

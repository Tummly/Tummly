import { ROLE_OPTIONS } from "@/components/home/hero-trial-options"

/**
 * Normalize Self role for Operator account chrome.
 * Slash-joined labels → first segment; Other / missing → null (name only).
 */
export function formatSelfRoleSubtitle(
  selfRole: string | null | undefined
): string | null {
  if (selfRole == null) {
    return null
  }

  const trimmed = selfRole.trim()

  if (!trimmed) {
    return null
  }

  const option = ROLE_OPTIONS.find(
    (entry) =>
      entry.value === trimmed ||
      entry.label.localeCompare(trimmed, undefined, {
        sensitivity: "accent",
      }) === 0
  )

  if (option?.value === "other") {
    return null
  }

  if (trimmed.localeCompare("Other", undefined, { sensitivity: "accent" }) === 0) {
    return null
  }

  const label = option?.label ?? trimmed
  const slashIndex = label.indexOf("/")

  if (slashIndex === -1) {
    return label
  }

  const firstSegment = label.slice(0, slashIndex).trim()

  return firstSegment || null
}

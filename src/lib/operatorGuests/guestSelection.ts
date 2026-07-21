export function formatGuestSelectionLabel(count: number): string | null {
  if (count <= 0) {
    return null
  }

  return count === 1 ? "1 guest selected" : `${count} guests selected`
}

export function toggleGuestInSelection(
  selectedIds: ReadonlySet<string>,
  guestId: string
): Set<string> {
  const next = new Set(selectedIds)

  if (next.has(guestId)) {
    next.delete(guestId)
  } else {
    next.add(guestId)
  }

  return next
}

export function toggleAllVisibleInSelection(
  selectedIds: ReadonlySet<string>,
  visibleGuestIds: readonly string[]
): Set<string> {
  const next = new Set(selectedIds)
  const allVisibleSelected =
    visibleGuestIds.length > 0 &&
    visibleGuestIds.every((id) => next.has(id))

  if (allVisibleSelected) {
    for (const id of visibleGuestIds) {
      next.delete(id)
    }
  } else {
    for (const id of visibleGuestIds) {
      next.add(id)
    }
  }

  return next
}

export function computeVisibleSelectionState(
  selectedIds: ReadonlySet<string>,
  visibleGuestIds: readonly string[]
): {
  isAllVisibleSelected: boolean
  isSomeVisibleSelected: boolean
} {
  if (visibleGuestIds.length === 0) {
    return {
      isAllVisibleSelected: false,
      isSomeVisibleSelected: false,
    }
  }

  let selectedVisibleCount = 0

  for (const id of visibleGuestIds) {
    if (selectedIds.has(id)) {
      selectedVisibleCount += 1
    }
  }

  return {
    isAllVisibleSelected: selectedVisibleCount === visibleGuestIds.length,
    isSomeVisibleSelected:
      selectedVisibleCount > 0 &&
      selectedVisibleCount < visibleGuestIds.length,
  }
}

export function sortedSelectionIds(selectedIds: ReadonlySet<string>): string[] {
  return [...selectedIds].sort()
}

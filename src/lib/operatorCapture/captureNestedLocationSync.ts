export type CaptureNestedLocationSyncInput = {
  pathLocationId: number | null
  previousPathLocationId: number | null
  selectedLocationId: number
  ownedLocationIds: readonly number[]
}

export type CaptureNestedLocationSyncDecision =
  | { action: "noop" }
  | { action: "sync_workspace_to_path"; locationId: number }
  | { action: "sync_path_to_workspace"; locationId: number }
  | { action: "redirect_invalid_path"; locationId: number }

function isOwnedLocationId(
  ownedLocationIds: readonly number[],
  locationId: number | null
): locationId is number {
  return (
    locationId != null && ownedLocationIds.includes(locationId)
  )
}

/**
 * Resolves nested Capture location sync: path wins on path change;
 * workspace selection wins when the path segment is unchanged.
 */
export function decideCaptureNestedLocationSync(
  input: CaptureNestedLocationSyncInput
): CaptureNestedLocationSyncDecision {
  const {
    pathLocationId,
    previousPathLocationId,
    selectedLocationId,
    ownedLocationIds,
  } = input

  if (!isOwnedLocationId(ownedLocationIds, pathLocationId)) {
    return {
      action: "redirect_invalid_path",
      locationId: selectedLocationId,
    }
  }

  const pathChanged = pathLocationId !== previousPathLocationId
  if (pathChanged) {
    if (pathLocationId !== selectedLocationId) {
      return {
        action: "sync_workspace_to_path",
        locationId: pathLocationId,
      }
    }
    return { action: "noop" }
  }

  if (selectedLocationId !== pathLocationId) {
    return {
      action: "sync_path_to_workspace",
      locationId: selectedLocationId,
    }
  }

  return { action: "noop" }
}

export function parseCaptureNestedLocationId(
  raw: string | undefined
): number | null {
  if (raw == null || raw.trim() === "") {
    return null
  }
  const parsed = Number.parseInt(raw, 10)
  return Number.isFinite(parsed) ? parsed : null
}

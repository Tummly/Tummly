/** One-shot router location state: multi-root create → nested Capture Detail drawer. */
export type CaptureLocationHandoff = {
  openPlacementDetailQrCodeId?: number
}

export const CAPTURE_PLACEMENT_DETAIL_OPEN_QUERY_KEY = "qrCodeId"

/**
 * Reads Capture nested handoff intent from router location state.
 * Invalid / missing fields are ignored.
 */
export function readCaptureLocationHandoff(
  state: unknown
): CaptureLocationHandoff {
  if (state == null || typeof state !== "object") {
    return {}
  }

  const raw = state as Record<string, unknown>
  const handoff: CaptureLocationHandoff = {}

  if (
    typeof raw.openPlacementDetailQrCodeId === "number"
    && Number.isFinite(raw.openPlacementDetailQrCodeId)
    && raw.openPlacementDetailQrCodeId > 0
  ) {
    handoff.openPlacementDetailQrCodeId = raw.openPlacementDetailQrCodeId
  }

  return handoff
}

export function captureLocationHandoffHasIntent(
  handoff: CaptureLocationHandoff
): boolean {
  return handoff.openPlacementDetailQrCodeId != null
}

export function buildCaptureLocationHandoffState(
  openPlacementDetailQrCodeId: number
): CaptureLocationHandoff {
  return { openPlacementDetailQrCodeId }
}

/**
 * Reads Placement Detail open query (`qrCodeId`) from Capture search params.
 * Invalid / missing values yield null.
 */
export function parseCapturePlacementDetailOpenQuery(
  params: { get: (key: string) => string | null }
): number | null {
  const raw = params.get(CAPTURE_PLACEMENT_DETAIL_OPEN_QUERY_KEY)
  if (raw == null || raw.trim() === "") {
    return null
  }
  const parsed = Number.parseInt(raw, 10)
  if (!Number.isFinite(parsed) || parsed <= 0) {
    return null
  }
  return parsed
}

/** Removes the Placement Detail open query without mutating the input. */
export function stripCapturePlacementDetailOpenQuery(
  params: URLSearchParams
): URLSearchParams {
  const next = new URLSearchParams(params)
  next.delete(CAPTURE_PLACEMENT_DETAIL_OPEN_QUERY_KEY)
  return next
}

/**
 * Path for replace-navigation after consuming the Placement Detail open query.
 * Shared by single and nested Capture routes.
 */
export function capturePlacementDetailOpenReplacePath(
  pathname: string,
  params: URLSearchParams
): string {
  const search = stripCapturePlacementDetailOpenQuery(params).toString()
  return `${pathname}${search === "" ? "" : `?${search}`}`
}

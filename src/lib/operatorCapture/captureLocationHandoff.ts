/** One-shot router location state: multi-root create → nested Capture Detail drawer. */
export type CaptureLocationHandoff = {
  openPlacementDetailQrCodeId?: number
}

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

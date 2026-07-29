import type { CapturePlacementItem } from "@/types/dashboard"

/** Stub Connected Offers copy until an offers API exists. */
export const CAPTURE_CONNECTED_OFFERS_STUB = "No active offers" as const

/** Default preview target until per-QR preview exists. */
export const CAPTURE_PREVIEW_PLACEMENT_LABEL = "Smart Guest" as const

export type CaptureGuestExperienceFacts = {
  /** Null means placements facts are unavailable (e.g. load failure) — distinct from a true empty list. */
  placements: readonly Pick<CapturePlacementItem, "status">[] | null
  locationName: string
  locationAddress: string
}

export type OperatorCaptureGuestExperienceView = {
  /**
   * Count of Active QR codes at the location, including Smart Guest.
   * Null when placements facts are unavailable — an honest "unknown", not a false zero.
   */
  activeQrCount: number | null
  connectedOffersText: typeof CAPTURE_CONNECTED_OFFERS_STUB
  previewPlacementLabel: typeof CAPTURE_PREVIEW_PLACEMENT_LABEL
  locationName: string
  locationAddress: string
}

/** Build Guest experience summary fields from Active/Paused placement list facts. */
export function buildCaptureGuestExperience(
  facts: CaptureGuestExperienceFacts
): OperatorCaptureGuestExperienceView {
  const activeQrCount =
    facts.placements == null
      ? null
      : facts.placements.reduce(
          (count, placement) =>
            placement.status === "Active" ? count + 1 : count,
          0
        )

  return {
    activeQrCount,
    connectedOffersText: CAPTURE_CONNECTED_OFFERS_STUB,
    previewPlacementLabel: CAPTURE_PREVIEW_PLACEMENT_LABEL,
    locationName: facts.locationName,
    locationAddress: facts.locationAddress,
  }
}

import { describe, expect, it } from "vitest"

import {
  buildCaptureGuestExperience,
  CAPTURE_CONNECTED_OFFERS_STUB,
  CAPTURE_PREVIEW_PLACEMENT_LABEL,
} from "./buildCaptureGuestExperience"

describe("buildCaptureGuestExperience", () => {
  it("counts Active QR codes including Smart Guest", () => {
    const result = buildCaptureGuestExperience({
      locationName: "Camden",
      locationAddress: "12 High St",
      placements: [
        { status: "Active" },
        { status: "Paused" },
        { status: "Active" },
        { status: "Active" },
      ],
    })

    expect(result).toEqual({
      activeQrCount: 3,
      connectedOffersText: CAPTURE_CONNECTED_OFFERS_STUB,
      previewPlacementLabel: CAPTURE_PREVIEW_PLACEMENT_LABEL,
      locationName: "Camden",
      locationAddress: "12 High St",
    })
  })

  it("returns zero when no placements are Active", () => {
    const result = buildCaptureGuestExperience({
      locationName: "Soho",
      locationAddress: "",
      placements: [{ status: "Paused" }, { status: "Paused" }],
    })

    expect(result.activeQrCount).toBe(0)
    expect(result.connectedOffersText).toBe("No active offers")
  })

  it("returns null Active QR count when placements facts are unavailable (load failure), not a false zero", () => {
    const result = buildCaptureGuestExperience({
      locationName: "Soho",
      locationAddress: "",
      placements: null,
    })

    expect(result.activeQrCount).toBe(null)
  })

  it("returns zero (not null) for a true empty placements list", () => {
    const result = buildCaptureGuestExperience({
      locationName: "Soho",
      locationAddress: "",
      placements: [],
    })

    expect(result.activeQrCount).toBe(0)
  })
})

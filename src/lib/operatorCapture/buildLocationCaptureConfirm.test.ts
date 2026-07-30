import { describe, expect, it } from "vitest"

import { buildLocationCaptureConfirm } from "./buildLocationCaptureConfirm"

describe("buildLocationCaptureConfirm", () => {
  it("builds Pause location capture copy with count and warning", () => {
    expect(
      buildLocationCaptureConfirm({
        locationId: 7,
        locationName: "Camden",
        action: "pause",
        codesCount: 4,
      })
    ).toEqual({
      locationId: 7,
      action: "pause",
      title: "Pause location capture?",
      body: "Guests at this location will not be able to open guest forms or submit feedback from any Active placements, Smart Guest, or digital guest links until location capture is activated again. Historical performance will remain available. Codes already paused stay paused.",
      locationName: "Camden",
      currentStatus: "Active",
      codesCountLabel: "Active codes to pause",
      codesCount: 4,
      warningText:
        "Printed materials at this location will remain in circulation but will not work while location capture is paused.",
      primaryLabel: "Pause location capture",
      cancelLabel: "Cancel",
      successToastMessage: "Camden capture is now paused.",
    })
  })

  it("builds Activate location capture copy with restore count", () => {
    expect(
      buildLocationCaptureConfirm({
        locationId: 7,
        locationName: "Camden",
        action: "activate",
        codesCount: 3,
      })
    ).toMatchObject({
      action: "activate",
      title: "Activate location capture?",
      currentStatus: "Paused",
      codesCountLabel: "Codes to activate",
      codesCount: 3,
      warningText: null,
      primaryLabel: "Activate location capture",
      successToastMessage: "Camden capture is now active.",
    })
  })
})

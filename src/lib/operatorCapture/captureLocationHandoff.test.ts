import { describe, expect, it } from "vitest"

import {
  buildCaptureLocationHandoffState,
  captureLocationHandoffHasIntent,
  readCaptureLocationHandoff,
} from "./captureLocationHandoff"

describe("captureLocationHandoff", () => {
  it("returns empty handoff for nullish or non-object state", () => {
    expect(readCaptureLocationHandoff(null)).toEqual({})
    expect(readCaptureLocationHandoff(undefined)).toEqual({})
    expect(readCaptureLocationHandoff("x")).toEqual({})
  })

  it("reads a positive openPlacementDetailQrCodeId", () => {
    expect(
      readCaptureLocationHandoff({ openPlacementDetailQrCodeId: 55 })
    ).toEqual({ openPlacementDetailQrCodeId: 55 })
  })

  it("ignores invalid openPlacementDetailQrCodeId values", () => {
    expect(
      readCaptureLocationHandoff({ openPlacementDetailQrCodeId: 0 })
    ).toEqual({})
    expect(
      readCaptureLocationHandoff({ openPlacementDetailQrCodeId: -1 })
    ).toEqual({})
    expect(
      readCaptureLocationHandoff({ openPlacementDetailQrCodeId: "55" })
    ).toEqual({})
  })

  it("reports whether handoff carries intent", () => {
    expect(captureLocationHandoffHasIntent({})).toBe(false)
    expect(
      captureLocationHandoffHasIntent({ openPlacementDetailQrCodeId: 9 })
    ).toBe(true)
  })

  it("builds router state for create → drawer signal", () => {
    expect(buildCaptureLocationHandoffState(42)).toEqual({
      openPlacementDetailQrCodeId: 42,
    })
  })
})

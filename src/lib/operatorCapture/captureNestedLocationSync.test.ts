import { describe, expect, it } from "vitest"

import { decideCaptureNestedLocationSync } from "./captureNestedLocationSync"

describe("decideCaptureNestedLocationSync", () => {
  const owned = [5, 7, 9] as const

  it("syncs workspace to path when the nested path location changes", () => {
    expect(
      decideCaptureNestedLocationSync({
        pathLocationId: 5,
        previousPathLocationId: 7,
        selectedLocationId: 7,
        ownedLocationIds: owned,
      })
    ).toEqual({ action: "sync_workspace_to_path", locationId: 5 })
  })

  it("navigates nested path when workspace selection changes without a path change", () => {
    expect(
      decideCaptureNestedLocationSync({
        pathLocationId: 5,
        previousPathLocationId: 5,
        selectedLocationId: 9,
        ownedLocationIds: owned,
      })
    ).toEqual({ action: "sync_path_to_workspace", locationId: 9 })
  })

  it("noops when path and workspace already agree", () => {
    expect(
      decideCaptureNestedLocationSync({
        pathLocationId: 5,
        previousPathLocationId: 5,
        selectedLocationId: 5,
        ownedLocationIds: owned,
      })
    ).toEqual({ action: "noop" })
  })

  it("redirects invalid nested path ids to the selected Owned location", () => {
    expect(
      decideCaptureNestedLocationSync({
        pathLocationId: 99,
        previousPathLocationId: null,
        selectedLocationId: 7,
        ownedLocationIds: owned,
      })
    ).toEqual({ action: "redirect_invalid_path", locationId: 7 })
  })

  it("redirects non-numeric nested path segments to the selected Owned location", () => {
    expect(
      decideCaptureNestedLocationSync({
        pathLocationId: null,
        previousPathLocationId: null,
        selectedLocationId: 5,
        ownedLocationIds: owned,
      })
    ).toEqual({ action: "redirect_invalid_path", locationId: 5 })
  })
})

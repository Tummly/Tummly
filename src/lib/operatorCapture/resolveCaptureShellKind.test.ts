import { describe, expect, it } from "vitest"

import { resolveCaptureShellKind } from "./resolveCaptureShellKind"

describe("resolveCaptureShellKind", () => {
  it("maps single-dashboard Capture to the single shell", () => {
    expect(
      resolveCaptureShellKind("single", "/single-dashboard/capture")
    ).toBe("single")
  })

  it("maps multi Capture root to the multi root shell", () => {
    expect(
      resolveCaptureShellKind("multi", "/multi-dashboard/capture")
    ).toBe("multi-root")
  })

  it("maps multi nested Capture to the nested shell", () => {
    expect(
      resolveCaptureShellKind(
        "multi",
        "/multi-dashboard/capture/locations/42"
      )
    ).toBe("nested")
  })

  it("does not treat single Capture as nested even with a locations segment", () => {
    expect(
      resolveCaptureShellKind(
        "single",
        "/single-dashboard/capture/locations/42"
      )
    ).toBe("single")
  })
})

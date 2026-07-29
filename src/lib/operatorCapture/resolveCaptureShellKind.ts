import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"

export type CaptureShellKind = "single" | "nested" | "multi-root"

/**
 * Highest practical seam for Capture route composition — which shell a
 * pathname should mount (no React Router / DOM).
 */
export function resolveCaptureShellKind(
  mode: OperatorDashboardMode,
  pathname: string
): CaptureShellKind {
  const segments = pathname.split("/").filter(Boolean)

  if (mode === "single") {
    return "single"
  }

  const captureIndex = segments.indexOf("capture")
  if (captureIndex < 0) {
    return "multi-root"
  }

  const afterCapture = segments.slice(captureIndex + 1)
  if (
    afterCapture[0] === "locations" &&
    afterCapture[1] != null &&
    afterCapture[1].length > 0
  ) {
    return "nested"
  }

  return "multi-root"
}

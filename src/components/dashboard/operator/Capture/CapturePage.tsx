import { CaptureLoadingState } from "@/components/dashboard/operator/Capture/CaptureLoadingState"
import { CaptureSingleShell } from "@/components/dashboard/operator/Capture/CaptureSingleShell"
import { useCapturePageModule } from "@/components/dashboard/operator/Capture/utils/useCapturePageModule"

/** Single-location Capture page — spinner until the Capture module is ready. */
export function CapturePage() {
  const { snapshot } = useCapturePageModule()

  if (
    snapshot.viewModel == null &&
    (snapshot.loadStatus === "idle" || snapshot.loadStatus === "loading")
  ) {
    return <CaptureLoadingState label="Loading Capture" />
  }

  if (snapshot.viewModel == null) {
    return null
  }

  return <CaptureSingleShell />
}

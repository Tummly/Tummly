import { CaptureLoadingState } from "@/components/dashboard/operator/Capture/CaptureLoadingState"
import { CaptureMultiRootShell } from "@/components/dashboard/operator/Capture/CaptureMultiRootShell"
import { useMultiCapturePageModule } from "@/components/dashboard/operator/Capture/utils/useMultiCapturePageModule"

/** Multi Capture root page — spinner until the Multi Capture module is ready. */
export function CaptureMultiRootPage() {
  const { snapshot } = useMultiCapturePageModule()

  if (
    snapshot.viewModel == null &&
    (snapshot.loadStatus === "idle" || snapshot.loadStatus === "loading")
  ) {
    return <CaptureLoadingState label="Loading Capture" />
  }

  if (snapshot.viewModel == null) {
    return null
  }

  return <CaptureMultiRootShell />
}

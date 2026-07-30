import type { CaptureLocationStatus } from "@/types/dashboard"

export type LocationCaptureConfirmAction = "pause" | "activate"

export type LocationCaptureConfirmView = {
  locationId: number
  action: LocationCaptureConfirmAction
  title: string
  body: string
  locationName: string
  currentStatus: CaptureLocationStatus
  codesCountLabel: string
  codesCount: number
  warningText: string | null
  primaryLabel: string
  cancelLabel: "Cancel"
  successToastMessage: string
}

export type BuildLocationCaptureConfirmInput = {
  locationId: number
  locationName: string
  action: LocationCaptureConfirmAction
  /** Active codes to pause, or restore-set size to activate. */
  codesCount: number
}

const PAUSE_WARNING =
  "Printed materials at this location will remain in circulation but will not work while location capture is paused."

/** Build Pause / Activate location capture confirm dialogue copy. */
export function buildLocationCaptureConfirm(
  input: BuildLocationCaptureConfirmInput
): LocationCaptureConfirmView {
  const { locationId, locationName, action, codesCount } = input

  if (action === "pause") {
    return {
      locationId,
      action: "pause",
      title: "Pause location capture?",
      body: "Guests at this location will not be able to open guest forms or submit feedback from any Active placements, Smart Guest, or digital guest links until location capture is activated again. Historical performance will remain available. Codes already paused stay paused.",
      locationName,
      currentStatus: "Active",
      codesCountLabel: "Active codes to pause",
      codesCount,
      warningText: PAUSE_WARNING,
      primaryLabel: "Pause location capture",
      cancelLabel: "Cancel",
      successToastMessage: `${locationName} capture is now paused.`,
    }
  }

  return {
    locationId,
    action: "activate",
    title: "Activate location capture?",
    body: "Activating location capture will restore the codes that were paused by the last location pause. Codes that were already paused individually stay paused.",
    locationName,
    currentStatus: "Paused",
    codesCountLabel: "Codes to activate",
    codesCount,
    warningText: null,
    primaryLabel: "Activate location capture",
    cancelLabel: "Cancel",
    successToastMessage: `${locationName} capture is now active.`,
  }
}

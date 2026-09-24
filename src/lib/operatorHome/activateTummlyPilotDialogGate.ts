export const ACTIVATE_TUMMLY_PILOT_DIALOG_DISMISS_KEY =
  "tummly.operator.activate-tummly-dialog.dismissed"

export function shouldOpenActivateTummlyPilotDialog(input: {
  status: string
  subscriptionPlan: string
  isDismissed: boolean
}): boolean {
  if (input.status !== "loaded") {
    return false
  }
  if (input.subscriptionPlan !== "Pilot" && input.subscriptionPlan !== "Free") {
    return false
  }
  if (input.isDismissed) {
    return false
  }
  return true
}

export function readActivateTummlyPilotDialogDismissed(
  storage: Pick<Storage, "getItem"> = sessionStorage
): boolean {
  return storage.getItem(ACTIVATE_TUMMLY_PILOT_DIALOG_DISMISS_KEY) === "1"
}

export function markActivateTummlyPilotDialogDismissed(
  storage: Pick<Storage, "setItem"> = sessionStorage
): void {
  storage.setItem(ACTIVATE_TUMMLY_PILOT_DIALOG_DISMISS_KEY, "1")
}

/** Clear dismiss on sign-out so the next sign-in can show the dialog again. */
export function clearActivateTummlyPilotDialogDismissed(
  storage: Pick<Storage, "removeItem"> = sessionStorage
): void {
  storage.removeItem(ACTIVATE_TUMMLY_PILOT_DIALOG_DISMISS_KEY)
}

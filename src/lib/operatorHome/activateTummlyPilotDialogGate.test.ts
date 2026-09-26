import { describe, expect, it } from "vitest"

import {
  ACTIVATE_TUMMLY_PILOT_DIALOG_DISMISS_KEY,
  clearActivateTummlyPilotDialogDismissed,
  markActivateTummlyPilotDialogDismissed,
  readActivateTummlyPilotDialogDismissed,
  shouldOpenActivateTummlyPilotDialog,
} from "./activateTummlyPilotDialogGate"

describe("shouldOpenActivateTummlyPilotDialog", () => {
  it("opens when loaded Pilot and not dismissed", () => {
    expect(
      shouldOpenActivateTummlyPilotDialog({
        status: "loaded",
        subscriptionPlan: "Pilot",
        isDismissed: false,
      })
    ).toBe(true)
  })

  it("does not open while loading", () => {
    expect(
      shouldOpenActivateTummlyPilotDialog({
        status: "loading",
        subscriptionPlan: "Pilot",
        isDismissed: false,
      })
    ).toBe(false)
  })

  it("opens when loaded Free and not dismissed", () => {
    expect(
      shouldOpenActivateTummlyPilotDialog({
        status: "loaded",
        subscriptionPlan: "Free",
        isDismissed: false,
      })
    ).toBe(true)
  })

  it("does not open for non-Pilot plans", () => {
    expect(
      shouldOpenActivateTummlyPilotDialog({
        status: "loaded",
        subscriptionPlan: "Starter",
        isDismissed: false,
      })
    ).toBe(false)
  })

  it("does not open when dismissed", () => {
    expect(
      shouldOpenActivateTummlyPilotDialog({
        status: "loaded",
        subscriptionPlan: "Pilot",
        isDismissed: true,
      })
    ).toBe(false)
  })
})

describe("activate Tummly dialog session dismiss", () => {
  it("reads and writes the session key", () => {
    const store = new Map<string, string>()
    const storage = {
      getItem: (key: string) => store.get(key) ?? null,
      setItem: (key: string, value: string) => {
        store.set(key, value)
      },
      removeItem: (key: string) => {
        store.delete(key)
      },
    }

    expect(readActivateTummlyPilotDialogDismissed(storage)).toBe(false)
    markActivateTummlyPilotDialogDismissed(storage)
    expect(store.get(ACTIVATE_TUMMLY_PILOT_DIALOG_DISMISS_KEY)).toBe("1")
    expect(readActivateTummlyPilotDialogDismissed(storage)).toBe(true)
  })

  it("clears the session dismiss key", () => {
    const store = new Map<string, string>()
    const storage = {
      getItem: (key: string) => store.get(key) ?? null,
      setItem: (key: string, value: string) => {
        store.set(key, value)
      },
      removeItem: (key: string) => {
        store.delete(key)
      },
    }

    markActivateTummlyPilotDialogDismissed(storage)
    clearActivateTummlyPilotDialogDismissed(storage)
    expect(store.has(ACTIVATE_TUMMLY_PILOT_DIALOG_DISMISS_KEY)).toBe(false)
    expect(readActivateTummlyPilotDialogDismissed(storage)).toBe(false)
  })
})

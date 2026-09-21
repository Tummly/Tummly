import { describe, expect, it } from "vitest"

import {
  ACTIVATE_TUMMLY_PILOT_DIALOG_BODY_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_CONTENT_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_COPY,
  ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_CLASS,
} from "./activateTummlyPilotDialogPresentation"

describe("Activate Tummly Pilot dialog presentation", () => {
  it("uses Figma copy", () => {
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_COPY.title).toBe(
      "Activate Tummly to publish offers"
    )
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_COPY.startCta).toBe("Start 30-day Pilot")
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_COPY.viewPlansCta).toBe("View plans")
  })

  it("uses Operator light/dark surface and text tokens", () => {
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_CONTENT_CLASS).toContain(
      "bg-op-surface-secondary"
    )
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_CONTENT_CLASS).not.toMatch(/bg-\[#/)
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_CLASS).toContain(
      "text-op-text-primary"
    )
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_BODY_CLASS).toContain(
      "text-op-text-primary"
    )
  })

  it("keeps Figma 30px gap between CTAs and hero", () => {
    expect(ACTIVATE_TUMMLY_PILOT_DIALOG_CONTENT_CLASS).toContain("gap-[30px]")
  })
})

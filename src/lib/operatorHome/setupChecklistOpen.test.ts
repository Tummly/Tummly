import { afterEach, describe, expect, it } from "vitest"

import {
  OPERATOR_SETUP_CHECKLIST_OPEN_KEY,
  readSetupChecklistOpen,
  writeSetupChecklistOpen,
} from "./setupChecklistOpen"

describe("setupChecklistOpen persistence", () => {
  afterEach(() => {
    localStorage.removeItem(OPERATOR_SETUP_CHECKLIST_OPEN_KEY)
  })

  it("defaults to expanded when unset", () => {
    expect(readSetupChecklistOpen()).toBe(true)
  })

  it("round-trips open preference", () => {
    writeSetupChecklistOpen(false)
    expect(readSetupChecklistOpen()).toBe(false)
    writeSetupChecklistOpen(true)
    expect(readSetupChecklistOpen()).toBe(true)
  })
})

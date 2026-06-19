import { describe, expect, it } from "vitest"

import { accountSetupMultiDefaultValues } from "@/schemas/accountSetupMulti"
import { accountSetupSingleDefaultValues } from "@/schemas/accountSetupSingle"

import { PROTOTYPE_QA_PASSWORD } from "./guestLoopAccountSetupPrototypeData"
import {
  clearPrototypeFormDraft,
  loadPrototypeFormDraft,
  savePrototypeFormDraft,
} from "./guestLoopAccountSetupPrototypeStorage"

describe("guestLoopAccountSetupPrototypeStorage", () => {
  it("round-trips password fields for multi drafts", () => {
    savePrototypeFormDraft("multi", {
      ...accountSetupMultiDefaultValues,
      token: "prototype-token",
      email: "qa@tummly.test",
      password: PROTOTYPE_QA_PASSWORD,
      confirmPassword: PROTOTYPE_QA_PASSWORD,
      agree: true,
    })

    expect(loadPrototypeFormDraft("multi")).toMatchObject({
      password: PROTOTYPE_QA_PASSWORD,
      confirmPassword: PROTOTYPE_QA_PASSWORD,
      agree: true,
    })
  })

  it("round-trips password fields for single drafts", () => {
    savePrototypeFormDraft("single", {
      ...accountSetupSingleDefaultValues,
      token: "prototype-token",
      email: "qa@tummly.test",
      password: PROTOTYPE_QA_PASSWORD,
      confirmPassword: PROTOTYPE_QA_PASSWORD,
      agree: true,
    })

    expect(loadPrototypeFormDraft("single")).toMatchObject({
      password: PROTOTYPE_QA_PASSWORD,
      confirmPassword: PROTOTYPE_QA_PASSWORD,
    })
  })

  it("clears stored drafts", () => {
    savePrototypeFormDraft("multi", {
      ...accountSetupMultiDefaultValues,
      password: PROTOTYPE_QA_PASSWORD,
    })
    clearPrototypeFormDraft("multi")
    expect(loadPrototypeFormDraft("multi")).toBeNull()
  })
})
